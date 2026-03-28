#!/usr/bin/env bash

set -euo pipefail

main() {
    local build_outcome="${1:-unknown}"
    local playwright_outcome="${2:-unknown}"
    local test_outcome="${3:-unknown}"

    local diagnostics_dir="TestResults/ci-diagnostics"
    local summary_file="${diagnostics_dir}/build-and-test-summary.txt"

    mkdir -p "${diagnostics_dir}"

    local timestamp_utc
    timestamp_utc=$(date -u +'%Y-%m-%dT%H:%M:%SZ')

    local working_directory
    working_directory=$(pwd)

    local git_commit
    if git_commit=$(git rev-parse HEAD 2> /dev/null); then
        :
    else
        git_commit="unavailable"
    fi

    local node_version
    if node_version=$(node --version 2> /dev/null); then
        :
    else
        node_version="unavailable"
    fi

    local npm_version
    if npm_version=$(npm --version 2> /dev/null); then
        :
    else
        npm_version="unavailable"
    fi

    {
        echo "CI build/test diagnostics"
        echo "========================="
        echo
        echo "UTC timestamp: ${timestamp_utc}"
        echo "Working directory: ${working_directory}"
        echo "Git commit: ${git_commit}"
        echo
        echo "Step outcomes"
        echo "-------------"
        echo "Build solution: ${build_outcome}"
        echo "Install Playwright browsers: ${playwright_outcome}"
        echo "Run tests: ${test_outcome}"
        echo
        echo "dotnet --info"
        echo "-------------"
        dotnet --info || true
        echo
        echo "Node toolchain"
        echo "--------------"
        echo "node: ${node_version}"
        echo "npm: ${npm_version}"
        echo
        echo "TestResults inventory"
        echo "---------------------"

        if [[ -d TestResults ]]; then
            find TestResults -maxdepth 4 -type f | sort || true
        else
            echo "TestResults directory was not created."
        fi
        echo
        echo "Per-project TestResults inventory"
        echo "--------------------------------"

        if [[ -d tests ]]; then
            find tests -path '*/TestResults/*' -type f | sort || true
        else
            echo "tests directory was not found."
        fi
    } > "${summary_file}"

    echo "Wrote ${summary_file}"

    return 0
}

main "$@"
