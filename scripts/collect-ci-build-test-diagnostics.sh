#!/usr/bin/env bash

set -euo pipefail

build_outcome="${1:-unknown}"
playwright_outcome="${2:-unknown}"
test_outcome="${3:-unknown}"

diagnostics_dir="TestResults/ci-diagnostics"
summary_file="${diagnostics_dir}/build-and-test-summary.txt"

mkdir -p "${diagnostics_dir}"

timestamp_utc=$(date -u +'%Y-%m-%dT%H:%M:%SZ')
working_directory=$(pwd)

if git_commit=$(git rev-parse HEAD 2> /dev/null); then
    :
else
    git_commit="unavailable"
fi

if node_version=$(node --version 2> /dev/null); then
    :
else
    node_version="unavailable"
fi

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
