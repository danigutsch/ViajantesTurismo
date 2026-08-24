#!/usr/bin/env bash

# Detect, and optionally repair, an orphaned rootless aardvark-dns before a
# test run.
#
# Rootless Podman keeps ONE network namespace shared by every container
# (`networks/rootless-netns/netns`), and runs aardvark-dns inside it. The
# namespace is reference counted: when the last container exits it is torn
# down, and the next container creates a fresh one. aardvark-dns is only
# started and reconfigured by netavark, and on the update path Podman rewrites
# the config file and sends SIGHUP WITHOUT entering the rootless namespace --
# so nothing re-checks which namespace the daemon is actually in.
#
# When a teardown does not complete (containers/podman#22103 is the same fault
# reported as a CI flake), aardvark-dns survives into a namespace that no
# longer belongs to anything. Every container started afterwards is handed a
# DNS server it cannot reach, so name resolution between containers fails while
# the daemon, the networks and the containers all look healthy. netavark 1.16
# detects this and logs `aardvark-dns runs in a different netns`; it
# deliberately does not repair it, because -- upstream's words on
# containers/netavark#856 -- "there is really no good way to fix this by
# ourself as we do not know if other containers are still up and running".
#
# This script is the missing half of that sentence: it repairs the case where
# nothing IS still running, and refuses when something is.

set -euo pipefail

MODE="repair"
QUIET=0

usage() {
    cat >&2 << 'EOF'
Usage: bash scripts/podman-dns-preflight.sh [--check] [--quiet]

  --check   report only; exit 1 if the daemon is orphaned, 0 if healthy
  --quiet   say nothing when already healthy
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --check) MODE="check" ;;
        --quiet) QUIET=1 ;;
        -h | --help)
            usage
            exit 0
            ;;
        *)
            printf 'unknown argument: %s\n' "$1" >&2
            usage
            exit 1
            ;;
    esac
    shift
done

# Healthy is the common case and this runs before every test slice, so it is
# silent unless asked otherwise. Anything abnormal always prints.
healthy() {
    if [[ ${QUIET} -eq 0 ]]; then
        printf 'podman dns preflight: %s\n' "$1"
    fi
    exit 0
}

if ! command -v podman > /dev/null 2>&1; then
    healthy "podman is not installed; nothing to check"
fi

# Rootful Podman does not share a namespace this way, so the fault cannot occur.
rootless="$(podman info --format '{{.Host.Security.Rootless}}' 2> /dev/null || true)"
if [[ ${rootless} != "true" ]]; then
    healthy "podman is rootful; the shared rootless namespace does not exist"
fi

run_root="$(podman info --format '{{.Store.RunRoot}}' 2> /dev/null || true)"
if [[ -z ${run_root} ]]; then
    healthy "podman did not report a run root; nothing to check"
fi
networks_dir="${run_root}/networks"
aardvark_dir="${networks_dir}/aardvark-dns"
# Podman reference counts the shared namespace here. The file exists for as
# long as the namespace does and is removed with it, so its ABSENCE beside a
# live daemon is the fault: the namespace aardvark-dns is serving in was torn
# down and the daemon did not go with it. Measured on podman 5.7.0 rootless:
# one container -> ref-count 1, two -> 2, none -> the whole directory is
# emptied and aardvark-dns exits.
#
# Two cheaper-looking checks were tried and are WRONG here, recorded so they
# are not reintroduced: `rootless-netns/netns` is not the file's name, and
# `stat` on the pinned `rootless-netns` file returns its tmpfs inode rather
# than the namespace inode. The process named by `rootless-netns-conn.pid` is
# pasta, which runs in the HOST namespace as the outside end of the tunnel --
# comparing its netns to the daemon's reports a mismatch on a healthy host.
refcount_file="${networks_dir}/rootless-netns/ref-count"

# `pgrep -x` matches the executable name exactly. A pattern match on the full
# command line would also match this script and any editor holding it open,
# which is how a repair script kills the shell that invoked it.
aardvark_pid="$(pgrep -x aardvark-dns 2> /dev/null | head -n 1 || true)"

running_containers="$(podman ps --quiet 2> /dev/null | grep -c . || true)"

# Networks aardvark still holds config for, that Podman no longer knows about.
# In the incident this script exists for, two of these outlived their networks
# by 40 minutes while the daemon kept answering for them.
orphan_configs=()
if [[ -d ${aardvark_dir} ]]; then
    known_networks="$(podman network ls --format '{{.Name}}' 2> /dev/null || true)"
    for path in "${aardvark_dir}"/*; do
        [[ -f ${path} ]] || continue
        entry="$(basename "${path}")"
        [[ ${entry} == "aardvark.pid" ]] && continue
        if ! printf '%s\n' "${known_networks}" | grep -qxF "${entry}"; then
            orphan_configs+=("${entry}")
        fi
    done
fi

reasons=()
if [[ -n ${aardvark_pid} && ! -e ${refcount_file} ]]; then
    reasons+=("aardvark-dns (pid ${aardvark_pid}) is running but ${refcount_file} does not exist: the shared namespace it serves in was torn down without it")
fi
if [[ -n ${aardvark_pid} && -e ${refcount_file} ]]; then
    refcount="$(cat "${refcount_file}" 2> /dev/null || true)"
    if [[ ${refcount} == "0" ]]; then
        reasons+=("aardvark-dns (pid ${aardvark_pid}) is running while the shared namespace reference count is 0")
    fi
fi
if [[ ${#orphan_configs[@]} -gt 0 ]]; then
    reasons+=("aardvark-dns holds config for networks Podman no longer has: ${orphan_configs[*]}")
fi

if [[ ${#reasons[@]} -eq 0 ]]; then
    healthy "healthy (aardvark-dns pid ${aardvark_pid:-none}, ${running_containers} container(s) running)"
fi

printf 'podman dns preflight: aardvark-dns looks orphaned\n' >&2
for reason in "${reasons[@]}"; do
    printf '  - %s\n' "${reason}" >&2
done

if [[ ${MODE} == "check" ]]; then
    printf 'Re-run without --check to repair, or by hand:\n' >&2
    printf '  podman ps --quiet | xargs -r podman stop\n' >&2
    printf '  pkill -x aardvark-dns && rm -rf %s\n' "${aardvark_dir}" >&2
    exit 1
fi

# The one case upstream says cannot be decided automatically. Killing the
# daemon here would break name resolution for whatever is still up.
if [[ ${running_containers} -gt 0 ]]; then
    printf 'REFUSING to repair: %s container(s) are running and may be using this daemon.\n' "${running_containers}" >&2
    printf 'Stop them first, then re-run this script.\n' >&2
    exit 1
fi

if [[ -n ${aardvark_pid} ]]; then
    kill -TERM "${aardvark_pid}" 2> /dev/null || true
    for _ in $(seq 1 50); do
        [[ -d "/proc/${aardvark_pid}" ]] || break
        sleep 0.1
    done
    if [[ -d "/proc/${aardvark_pid}" ]]; then
        kill -KILL "${aardvark_pid}" 2> /dev/null || true
        sleep 0.5
    fi
    printf 'stopped aardvark-dns (pid %s)\n' "${aardvark_pid}"
fi

# Podman recreates whatever it needs on the next container; leaving a stale
# entry behind is what keeps the daemon answering for a dead network.
if [[ -d ${aardvark_dir} ]]; then
    rm -f "${aardvark_dir}/aardvark.pid"
    for entry in "${orphan_configs[@]}"; do
        rm -f "${aardvark_dir:?}/${entry}"
    done
    printf 'cleared %s stale aardvark entr(ies)\n' "${#orphan_configs[@]}"
fi

printf 'podman dns preflight: repaired. aardvark-dns restarts with the next container.\n'
