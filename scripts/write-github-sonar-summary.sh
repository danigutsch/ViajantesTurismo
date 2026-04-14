#!/usr/bin/env bash

set -euo pipefail

main() {
    local validation_outcome="${1:-unknown}"
    local sonar_log_file="TestResults/sonar-analysis.log"
    local summary_target="${GITHUB_STEP_SUMMARY:-}"

    if [[ -z "${summary_target}" ]]; then
        echo "GITHUB_STEP_SUMMARY is not set; skipping GitHub summary generation."
        return 0
    fi

    {
        echo "## Build, test, coverage, and SonarCloud"
        echo
        echo "- Validation step outcome: \`${validation_outcome}\`"

        if [[ ! -f "${sonar_log_file}" ]]; then
            echo "- Sonar analysis log: not generated"
            echo
            echo "No Sonar analysis log was found at \`${sonar_log_file}\`."
        else
            local quality_gate_line
            quality_gate_line=$(grep -F 'QUALITY GATE STATUS:' "${sonar_log_file}" | tail -n 1 || true)

            local details_url
            details_url=$(grep -oE 'https://sonarcloud\.io/[^[:space:]]+' "${sonar_log_file}" | tail -n 1 || true)

            local warning_count
            warning_count=$(grep -Ec 'WARN:|WARNING:' "${sonar_log_file}" || true)

            if [[ -n "${quality_gate_line}" ]]; then
                echo "- ${quality_gate_line#*QUALITY GATE STATUS: }"
            else
                echo "- Quality gate status: not found in Sonar log"
            fi

            if [[ -n "${details_url}" ]]; then
                echo "- SonarCloud details: ${details_url}"
            fi

            echo "- Sonar warnings in log: ${warning_count}"
            echo "- Uploaded artifacts: \`test-results\`, \`coverage-report\`, \`sonar-coverage\`, \`sonar-analysis-log\`"
            echo

            local -a parse_warnings=()
            mapfile -t parse_warnings < <(grep -E 'WARN: Cannot parse ' "${sonar_log_file}" || true)

            if [[ ${#parse_warnings[@]} -gt 0 ]]; then
                echo "### Parse warnings"
                echo
                echo "The following files could not be parsed by Sonar during hosted analysis:"
                echo

                local warning_line
                for warning_line in "${parse_warnings[@]}"; do
                    local cleaned_warning
                    cleaned_warning="${warning_line#*WARN: Cannot parse }"
                    cleaned_warning="${cleaned_warning#\'}"
                    cleaned_warning="${cleaned_warning%\'}"
                    printf -- "%s\n" "- \`${cleaned_warning}\`"
                done

                echo
            fi
        fi

        if [[ "${validation_outcome}" != "success" ]]; then
            echo "### Next place to look"
            echo
            echo "Open the \`sonar-analysis-log\` artifact or the step log for the full scanner output."
            echo "If the quality gate failed, the SonarCloud details link above is the fastest route to the reported issues."
        fi
    } >> "${summary_target}"

    if [[ "${validation_outcome}" != "success" ]]; then
        echo "::error title=Build and Test failed::Build, test, coverage, or SonarCloud validation failed. See the job summary and sonar-analysis-log artifact."
    fi
}

main "$@"
