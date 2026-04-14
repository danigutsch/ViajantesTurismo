#!/usr/bin/env bash

set -euo pipefail

append_summary() {
    local summary_target="${GITHUB_STEP_SUMMARY:-}"

    if [[ -z "${summary_target}" ]]; then
        return 0
    fi

    {
        echo "## SonarCloud configuration"
        echo
        echo "CI could not start SonarCloud analysis because required GitHub configuration is missing."
        echo
        echo "### Required repository settings"
        echo
        echo "- Secret \`SONAR_TOKEN\`"
        echo "- Variable \`SONAR_ORGANIZATION\`"
        echo "- Variable \`SONAR_PROJECT_KEY\`"
        echo
        echo "See \`docs/ci/sonarcloud.md\` for the repository-owned configuration contract."
    } >> "${summary_target}"

    return 0
}

format_missing_setting() {
    local variable_name="$1"

    case "${variable_name}" in
        SONAR_TOKEN)
            printf '%s' 'secret SONAR_TOKEN'
            ;;
        SONAR_ORGANIZATION)
            printf '%s' 'variable SONAR_ORGANIZATION'
            ;;
        SONAR_PROJECT_KEY)
            printf '%s' 'variable SONAR_PROJECT_KEY'
            ;;
        *)
            printf '%s' "${variable_name}"
            ;;
    esac
}

main() {
    local -a missing_vars=()
    local -a missing_settings=()

    local variable_name
    for variable_name in SONAR_TOKEN SONAR_ORGANIZATION SONAR_PROJECT_KEY; do
        if [[ -z "${!variable_name:-}" ]]; then
            missing_vars+=("${variable_name}")
            missing_settings+=("$(format_missing_setting "${variable_name}")")
        fi
    done

    if [[ ${#missing_vars[@]} -eq 0 ]]; then
        return 0
    fi

    local joined_settings
    joined_settings=$(IFS=', '; echo "${missing_settings[*]}")

    local message="Missing required SonarCloud configuration: ${joined_settings}."

    if [[ "${GITHUB_ACTIONS:-}" == "true" ]]; then
        echo "::error title=Missing SonarCloud configuration::${message} Configure the missing GitHub repository settings before running build, test, and analysis."
    fi

    echo "${message}" >&2
    echo "See docs/ci/sonarcloud.md for the required repository settings." >&2

    append_summary

    return 1
}

main "$@"
