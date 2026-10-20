#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
subject="${repo_root}/scripts/ensure-dotnet-sdk.sh"
test_root="$(mktemp -d)"
trap 'rm -rf "${test_root}"' EXIT

fail() {
    echo "FAIL: $1" >&2
    exit 1
}

assert_contains() {
    local value="$1"
    local expected="$2"
    local message="$3"

    [[ "${value}" == *"${expected}"* ]] || fail "${message}: expected '${expected}' in '${value}'"
}

write_global_json() {
    local path="$1"
    local version="$2"

    cat > "${path}" <<EOF
{
  "sdk": {
    "version": "${version}",
    "rollForward": "disable"
  }
}
EOF
}

fake_bin="${test_root}/bin"
mkdir -p "${fake_bin}"

cat > "${fake_bin}/dotnet" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

if [[ "${1:-}" == "--list-sdks" ]]; then
    cat "${FAKE_DOTNET_SDKS_FILE}"
    exit 0
fi

exit 64
EOF
chmod +x "${fake_bin}/dotnet"

cat > "${fake_bin}/curl" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

output=""
url=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        --output | -o)
            shift
            output="${1:-}"
            ;;
        https://*)
            url="$1"
            ;;
    esac
    shift
done

[[ -n "${output}" && -n "${url}" ]]
printf '%s\n' "${url}" >> "${FAKE_CURL_LOG}"

case "${url}" in
    https://dotnetcli.blob.core.windows.net/*)
        cp "${FAKE_METADATA_FILE}" "${output}"
        ;;
    https://builds.dotnet.microsoft.com/*)
        cp "${FAKE_ARCHIVE_FILE}" "${output}"
        ;;
    *)
        exit 65
        ;;
esac
EOF
chmod +x "${fake_bin}/curl"

global_json="${test_root}/global.json"
sdk_list="${test_root}/sdks.txt"
curl_log="${test_root}/curl.log"
metadata_file="${test_root}/releases.json"
archive_file="${test_root}/dotnet-sdk-10.0.303-linux-x64.tar.gz"
archive_root="${test_root}/archive-root"
install_dir="${test_root}/dotnet"

write_global_json "${global_json}" "10.0.303"
mkdir -p "${archive_root}/sdk/10.0.303"
printf 'new sdk\n' > "${archive_root}/sdk/10.0.303/marker"
printf 'fixture\n' > "${archive_root}/sdk/10.0.303/dotnet.dll"
tar -czf "${archive_file}" -C "${archive_root}" .
hash_output="$(sha512sum "${archive_file}")"
archive_hash="${hash_output%% *}"

write_metadata() {
    local hash="$1"

    cat > "${metadata_file}" <<EOF
{
  "releases": [
    {
      "sdks": [
        {
          "version": "10.0.303",
          "files": [
            {
              "name": "dotnet-sdk-linux-x64.tar.gz",
              "rid": "linux-x64",
              "url": "https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.303/dotnet-sdk-10.0.303-linux-x64.tar.gz",
              "hash": "${hash}"
            }
          ]
        }
      ]
    }
  ]
}
EOF
}

run_subject() {
    PATH="${fake_bin}:${PATH}" \
        FAKE_DOTNET_SDKS_FILE="${sdk_list}" \
        FAKE_CURL_LOG="${curl_log}" \
        FAKE_METADATA_FILE="${metadata_file}" \
        FAKE_ARCHIVE_FILE="${archive_file}" \
        bash "${subject}" "$@"
}

printf '10.0.302 [%s/sdk]\n10.0.303 [%s/sdk]\n' "${install_dir}" "${install_dir}" > "${sdk_list}"
rm -f "${curl_log}"
output="$(run_subject --global-json "${global_json}" --install-dir "${install_dir}")"
assert_contains "${output}" "10.0.303 is already installed" "installed SDK result"
[[ ! -e "${curl_log}" ]] || fail "installed SDK must not access the network"

printf '10.0.302 [%s/sdk]\n' "${install_dir}" > "${sdk_list}"
rm -f "${curl_log}"
set +e
output="$(run_subject --global-json "${global_json}" --install-dir "${install_dir}" 2>&1)"
status=$?
set -e
[[ ${status} -ne 0 ]] || fail "missing SDK without opt-in should fail"
assert_contains "${output}" "--install" "missing SDK guidance"
[[ ! -e "${curl_log}" ]] || fail "missing SDK without opt-in must not access the network"

mkdir -p "${install_dir}/sdk/10.0.302"
printf 'old sdk\n' > "${install_dir}/sdk/10.0.302/marker"
write_metadata "${archive_hash}"
rm -f "${curl_log}"
output="$(run_subject --install --global-json "${global_json}" --install-dir "${install_dir}")"
assert_contains "${output}" "Installed .NET SDK 10.0.303" "successful install result"
[[ -f "${install_dir}/sdk/10.0.303/marker" ]] || fail "exact SDK was not installed"
[[ -f "${install_dir}/sdk/10.0.302/marker" ]] || fail "existing SDK was removed"
curl_calls="$(cat "${curl_log}")"
assert_contains "${curl_calls}" "/Sdk/10.0.303/" "exact SDK download"

rm -rf "${install_dir}/sdk/10.0.303"
write_metadata "$(printf '0%.0s' {1..128})"
set +e
output="$(run_subject --install --global-json "${global_json}" --install-dir "${install_dir}" 2>&1)"
status=$?
set -e
[[ ${status} -ne 0 ]] || fail "checksum mismatch should fail"
assert_contains "${output}" "SHA-512 verification failed" "checksum failure"
[[ ! -e "${install_dir}/sdk/10.0.303" ]] || fail "unverified SDK must not be installed"

printf 'not an SDK archive\n' > "${archive_file}"
hash_output="$(sha512sum "${archive_file}")"
invalid_archive_hash="${hash_output%% *}"
write_metadata "${invalid_archive_hash}"
set +e
output="$(run_subject --install --global-json "${global_json}" --install-dir "${install_dir}" 2>&1)"
status=$?
set -e
[[ ${status} -ne 0 ]] || fail "invalid verified archive should fail"
assert_contains "${output}" "Failed to extract the verified .NET SDK archive" "extraction failure"
[[ ! -e "${install_dir}/sdk/10.0.303" ]] || fail "invalid SDK archive must not be installed"

alternate_global_json="${test_root}/alternate-global.json"
write_global_json "${alternate_global_json}" "10.0.304"
printf '10.0.304 [%s/sdk]\n' "${install_dir}" > "${sdk_list}"
rm -f "${curl_log}"
output="$(run_subject --global-json "${alternate_global_json}" --install-dir "${install_dir}")"
assert_contains "${output}" "10.0.304 is already installed" "selected global.json result"
[[ ! -e "${curl_log}" ]] || fail "selected installed SDK must not access the network"

echo "dotnet SDK bootstrap tests passed"
