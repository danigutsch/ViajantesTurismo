#!/bin/bash
# ViajantesTurismo Development Environment Setup Script
# This script verifies required tooling and points to optional local tools

set -e

NOT_FOUND="not found"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        *)
            echo "Unknown option: $1"
            echo "Usage: $0"
            exit 1
            ;;
    esac
done

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_header() {
    printf "\n%b" "${CYAN}🚀 ViajantesTurismo Development Setup${NC}\n"
    printf "%b" "${CYAN}====================================${NC}\n\n"
    return 0
}

check_dotnet_sdk_version() {
    local installed_sdk
    local required_version

    printf "%b" "${YELLOW}📦 Checking .NET SDK...${NC}\n"

    if [[ ! -f "global.json" ]]; then
        printf "%b" "   ${RED}❌ global.json not found in the current directory${NC}\n"
        exit 1
    fi

    # Prefer jq for parsing global.json when available; otherwise fall back to python3.
    if command -v jq >/dev/null 2>&1; then
        required_version=$(jq -r '.sdk.version' global.json 2>/dev/null || echo "")
    else
        required_version=$(python3 - <<'EOF' 2>/dev/null || echo ""
import json
import sys

try:
    with open("global.json", "r", encoding="utf-8") as f:
        data = json.load(f)
    print(data.get("sdk", {}).get("version", ""))
except Exception:
    sys.exit(1)
EOF
)
    fi

    if [[ -z "${required_version}" ]]; then
        printf "%b" "   ${RED}❌ Unable to determine required .NET SDK version from global.json${NC}\n"
        exit 1
    fi

    if ! command -v dotnet > /dev/null 2>&1; then
        printf "%b" "   ${RED}❌ .NET SDK not found${NC}\n"
        printf "%b" "   ${CYAN}💡 Download .NET ${required_version} from: https://dotnet.microsoft.com/download/dotnet/10.0${NC}\n"
        exit 1
    fi

    installed_sdk=$(dotnet --version 2>&1 || echo "${NOT_FOUND}")

    if [[ "${installed_sdk}" != "${NOT_FOUND}" ]]; then
        printf "%b" "   ${GREEN}✅ .NET SDK installed: ${installed_sdk}${NC}\n"
        if [[ "${installed_sdk}" != "${required_version}" ]]; then
            printf "%b" "   ${RED}❌ Required version: ${required_version}${NC}\n"
            printf "%b" "   ${CYAN}💡 Install the exact SDK from: https://dotnet.microsoft.com/download/dotnet/10.0${NC}\n"
            printf "%b" "   ${CYAN}💡 Re-run setup after the exact SDK is installed so locked restore uses the same toolchain as CI.${NC}\n"
            exit 1
        fi
    fi

    return 0
}

restore_dotnet_dependencies() {
    printf "\n%b" "${YELLOW}📦 Restoring .NET dependencies...${NC}\n"
    if dotnet restore --locked-mode > /dev/null 2>&1; then
        printf "%b" "   ${GREEN}✅ .NET dependencies restored${NC}\n"
    else
        printf "%b" "   ${RED}❌ Failed to restore .NET dependencies in locked mode${NC}\n"
        exit 1
    fi

    return 0
}

restore_dotnet_local_tools() {
    printf "\n%b" "${YELLOW}🔧 Restoring .NET local tools...${NC}\n"
    if dotnet tool restore > /dev/null 2>&1; then
        printf "%b" "   ${GREEN}✅ .NET tools restored (dotnet-ef, reportgenerator, Aspire CLI)${NC}\n"
        printf "%b" "   ${CYAN}💡 Run the repo-pinned Aspire CLI with: dotnet tool run aspire run${NC}\n"
    else
        printf "%b" "   ${YELLOW}⚠️ Failed to restore .NET tools${NC}\n"
    fi

    return 0
}

check_aspnet_core_development_certificate_trust() {
    local dev_cert_trust_dir
    local os_name

    os_name="$(uname -s)"
    if [[ "${os_name}" != "Linux" ]]; then
        return
    fi

    printf "\n%b" "${YELLOW}🔐 Checking ASP.NET Core development certificate trust...${NC}\n"

    dev_cert_trust_dir="${HOME}/.aspnet/dev-certs/trust"

    if dotnet dev-certs https --check --trust > /dev/null 2>&1; then
        printf "%b" "   ${GREEN}✅ ASP.NET Core development certificate is trusted${NC}\n"
    else
        printf "%b" "   ${YELLOW}⚠️ ASP.NET Core development certificate is not trusted${NC}\n"
        printf "%b" "   ${CYAN}💡 Trust it with:${NC}\n"
        printf "%b" "   ${CYAN}   dotnet dev-certs https --clean${NC}\n"
        printf "%b" "   ${CYAN}   dotnet dev-certs https --trust${NC}\n"
    fi

    case ":${SSL_CERT_DIR:-}:" in
        *":${dev_cert_trust_dir}:"*)
            printf "%b" "   ${GREEN}✅ SSL_CERT_DIR includes Aspire development certificate trust path${NC}\n"
            ;;
        *)
            printf "%b" "   ${YELLOW}⚠️ SSL_CERT_DIR does not include ${dev_cert_trust_dir}${NC}\n"
            printf "%b" "   ${CYAN}💡 Add this to ~/.zshrc or ~/.bashrc:${NC}\n"
            printf "%b" "   ${CYAN}   if [ -z \"\$SSL_CERT_DIR\" ]; then${NC}\n"
            printf "%b" "   ${CYAN}     export SSL_CERT_DIR=\"/usr/lib/ssl/certs:${dev_cert_trust_dir}\"${NC}\n"
            printf "%b" "   ${CYAN}   else${NC}\n"
            printf "%b" "   ${CYAN}     export SSL_CERT_DIR=\"\$SSL_CERT_DIR:${dev_cert_trust_dir}\"${NC}\n"
            printf "%b" "   ${CYAN}   fi${NC}\n"
            printf "%b" "   ${CYAN}   Then restart your shell before running Aspire or E2E tests.${NC}\n"
            ;;
    esac

    return 0
}

check_powershell() {
    printf "\n%b" "${YELLOW}🔍 Checking pwsh (PowerShell 7+) and Playwright prerequisites...${NC}\n"
    if command -v pwsh > /dev/null 2>&1; then
        printf "%b" "   ${GREEN}✅ pwsh (PowerShell 7+) installed${NC}\n"
        printf "%b" "   ${GREEN}✅ Playwright browser installation can use scripts/install-playwright.sh after build${NC}\n"
        if pwsh -NoLogo -NoProfile -Command "if (Get-Module -ListAvailable -Name PSScriptAnalyzer) { exit 0 } else { exit 1 }" > /dev/null 2>&1; then
            printf "%b" "   ${GREEN}✅ PSScriptAnalyzer already installed${NC}\n"
        else
            printf "%b" "   ${YELLOW}⚠️ PSScriptAnalyzer not available - local PowerShell linting will be skipped${NC}\n"
            printf "%b" "   ${CYAN}💡 Install it only if you plan to lint PowerShell scripts:${NC}\n"
            printf "%b" "   ${CYAN}   pwsh -Command 'Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force'${NC}\n"
        fi
        printf "%b" "   ${CYAN}💡 After dotnet build, install Playwright browsers with: bash scripts/install-playwright.sh${NC}\n"
    else
        printf "%b" "   ${YELLOW}⚠️ pwsh (PowerShell 7+) not available - PowerShell script linting and Playwright browser installation will be skipped${NC}\n"
        printf "%b" "   ${CYAN}💡 Install pwsh (PowerShell 7+) from: https://github.com/PowerShell/PowerShell${NC}\n"
    fi

    return 0
}

check_k6() {
    printf "\n%b" "${YELLOW}📈 Checking optional performance testing tooling...${NC}\n"

    if command -v k6 > /dev/null 2>&1; then
        printf "%b" "   ${GREEN}✅ k6 installed${NC}\n"
        printf "%b" "   ${CYAN}💡 Run the Admin smoke scenario with:${NC}\n"
        printf "%b" "   ${CYAN}   VT_API_BASE_URL=<admin-api-url> scripts/run-admin-performance-smoke.sh${NC}\n"
    else
        printf "%b" "   ${YELLOW}⚠️ k6 not available - performance/load testing scripts will be skipped locally${NC}\n"
        printf "%b" "   ${CYAN}💡 Install k6 only if you plan to run tests under tests/performance/:${NC}\n"
        printf "%b" "   ${CYAN}   Linux: https://grafana.com/docs/k6/latest/set-up/install-k6/${NC}\n"
        printf "%b" "   ${CYAN}   macOS: brew install k6${NC}\n"
        printf "%b" "   ${CYAN}   Windows: winget install k6 --source winget${NC}\n"
    fi

    return 0
}

print_summary() {
    printf "\n%b" "${GREEN}✨ Setup Complete!${NC}\n"
    printf "%b" "${GREEN}==================${NC}\n\n"

    printf "%b" "${CYAN}Next steps:${NC}\n"
    printf "%b" "  1. Run the application: ${NC}dotnet tool run aspire run\n"
    printf "%b" "  2. Run tests: ${NC}dotnet test\n"
    printf "%b" "  3. Install Playwright browsers after build: ${NC}bash scripts/install-playwright.sh\n"
    printf "%b" "  4. Optional performance smoke run: ${NC}VT_API_BASE_URL=<admin-api-url> scripts/run-admin-performance-smoke.sh\n"
    printf "%b" "  5. Validate a commit message: ${NC}bash scripts/validate-commit-message.sh /path/to/message.txt\n"
    printf "%b" "     ${CYAN}(If Aspire CLI is installed globally or via the official install script, 'aspire run' also works.)${NC}\n"
    printf "%b" "  6. Tool inventory reference: ${NC}README.md (required local, optional local, CI-only, and devcontainer-provided tools)\n"
    printf "\n"
    return 0
}

print_header
check_dotnet_sdk_version
restore_dotnet_dependencies
restore_dotnet_local_tools
check_aspnet_core_development_certificate_trust
check_powershell
check_k6
print_summary
