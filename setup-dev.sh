#!/bin/bash
# ViajantesTurismo Development Environment Setup Script
# This script installs all required and optional tools for development

set -e

SKIP_GIT_HOOK=false
SKIP_NPM=false
NOT_FOUND="not found"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-git-hook)
            SKIP_GIT_HOOK=true
            shift
            ;;
        --skip-npm)
            SKIP_NPM=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--skip-git-hook] [--skip-npm]"
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
}

check_dotnet_sdk_version() {
    printf "%b" "${YELLOW}📦 Checking .NET SDK...${NC}\n"

    if [[ ! -f "global.json" ]]; then
        printf "%b" "   ${RED}❌ global.json not found in the current directory${NC}\n"
        exit 1
    fi

    # Prefer jq for parsing global.json when available; otherwise fall back to python3.
    if command -v jq >/dev/null 2>&1; then
        REQUIRED_VERSION=$(jq -r '.sdk.version' global.json 2>/dev/null || echo "")
    else
        REQUIRED_VERSION=$(python3 - <<'EOF' 2>/dev/null || echo ""
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

    if [[ -z "${REQUIRED_VERSION}" ]]; then
        printf "%b" "   ${RED}❌ Unable to determine required .NET SDK version from global.json${NC}\n"
        exit 1
    fi

    INSTALLED_SDK=$(dotnet --version 2>&1 || echo "${NOT_FOUND}")

    if [[ "${INSTALLED_SDK}" != "${NOT_FOUND}" ]]; then
        printf "%b" "   ${GREEN}✅ .NET SDK installed: ${INSTALLED_SDK}${NC}\n"
        if [[ "${INSTALLED_SDK}" != "${REQUIRED_VERSION}" ]]; then
            printf "%b" "   ${YELLOW}⚠️ Required version: ${REQUIRED_VERSION}${NC}\n"
            printf "%b" "   ${CYAN}💡 Download from: https://dotnet.microsoft.com/download/dotnet/10.0${NC}\n"
        fi
    else
        printf "%b" "   ${RED}❌ .NET SDK not found${NC}\n"
        printf "%b" "   ${CYAN}💡 Download .NET ${REQUIRED_VERSION} from: https://dotnet.microsoft.com/download/dotnet/10.0${NC}\n"
        exit 1
    fi
}

restore_dotnet_dependencies() {
    printf "\n%b" "${YELLOW}📦 Restoring .NET dependencies...${NC}\n"
    if dotnet restore > /dev/null 2>&1; then
        printf "%b" "   ${GREEN}✅ .NET dependencies restored${NC}\n"
    else
        printf "%b" "   ${RED}❌ Failed to restore .NET dependencies${NC}\n"
        exit 1
    fi
}

restore_dotnet_local_tools() {
    printf "\n%b" "${YELLOW}🔧 Restoring .NET local tools...${NC}\n"
    if dotnet tool restore > /dev/null 2>&1; then
        printf "%b" "   ${GREEN}✅ .NET tools restored (dotnet-ef, reportgenerator, Aspire CLI)${NC}\n"
        printf "%b" "   ${CYAN}💡 Run the repo-pinned Aspire CLI with: dotnet tool run aspire run${NC}\n"
    else
        printf "%b" "   ${YELLOW}⚠️ Failed to restore .NET tools${NC}\n"
    fi
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
}

check_powershell() {
    printf "\n%b" "${YELLOW}🔍 Checking pwsh (PowerShell 7+) and Playwright prerequisites...${NC}\n"
    if command -v pwsh > /dev/null 2>&1; then
        printf "%b" "   ${GREEN}✅ pwsh (PowerShell 7+) installed${NC}\n"
        printf "%b" "   ${GREEN}✅ Playwright browser installation can use scripts/install-playwright.sh after build${NC}\n"
        printf "%b" "   ${CYAN}💡 To install PSScriptAnalyzer:${NC}\n"
        printf "%b" "   ${CYAN}   pwsh -Command 'Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force'${NC}\n"
        printf "%b" "   ${CYAN}💡 After dotnet build, install Playwright browsers with: bash scripts/install-playwright.sh${NC}\n"
    else
        printf "%b" "   ${YELLOW}⚠️ pwsh (PowerShell 7+) not available - PowerShell script linting and Playwright browser installation will be skipped${NC}\n"
        printf "%b" "   ${CYAN}💡 Install pwsh (PowerShell 7+) from: https://github.com/PowerShell/PowerShell${NC}\n"
    fi
}

check_nodejs_and_npm() {
    local install_linters
    local node_version
    local required_node_version

    if [[ "${SKIP_NPM}" = true ]]; then
        return
    fi

    printf "\n%b" "${YELLOW}📦 Checking Node.js and npm...${NC}\n"

    required_node_version=""
    if [[ -f ".nvmrc" ]]; then
        read -r required_node_version < .nvmrc
    else
        printf "%b" "   ${YELLOW}⚠️ .nvmrc file not found${NC}\n"
    fi

    node_version=$(node --version 2>&1 || echo "${NOT_FOUND}")

    if [[ "${node_version}" != "${NOT_FOUND}" ]]; then
        printf "%b" "   ${GREEN}✅ Node.js installed: ${node_version}${NC}\n"

        if [[ -n "${required_node_version}" ]]; then
            if echo "${node_version}" | grep -q "v${required_node_version}"; then
                printf "%b" "   ${GREEN}✅ Version matches .nvmrc (${required_node_version})${NC}\n"
            else
                printf "%b" "   ${YELLOW}⚠️ Version mismatch!${NC}\n"
                printf "%b" "   ${YELLOW}   Required: v${required_node_version} (from .nvmrc)${NC}\n"
                printf "%b" "   ${YELLOW}   Installed: ${node_version}${NC}\n"
                printf "%b" "   ${CYAN}💡 Switch with: nvm use${NC}\n"
            fi
        fi

        printf "\n%b" "${CYAN}Code quality linters available (optional):${NC}\n"
        printf "%b" "  • markdownlint-cli - Markdown file linting\n"
        printf "%b" "  • shellcheck - Shell script linting\n"
        printf "%b" "  • shfmt - Shell script formatting\n"
        printf "%b" "  • gherkin-lint - BDD feature file linting\n"
        printf "%b" "  • ESLint - JSON file linting\n"
        printf "\n"
        printf "%b" "Install code quality linters? (y/N): "
        read -r install_linters

        if [[ "${install_linters}" = "y" ]] || [[ "${install_linters}" = "Y" ]]; then
            printf "\n%b" "${YELLOW}📦 Installing npm dependencies...${NC}\n"
            if npm install > /dev/null 2>&1; then
                printf "%b" "   ${GREEN}✅ npm dependencies installed (markdownlint-cli, shellcheck, shfmt, gherkin-lint, ESLint)${NC}\n"
            else
                printf "%b" "   ${RED}❌ Failed to install npm dependencies${NC}\n"
                exit 1
            fi
        else
            printf "%b" "   ${YELLOW}⏭️ Skipping linter installation${NC}\n"
            printf "%b" "   ${CYAN}💡 Install later with: npm install${NC}\n"
        fi
    else
        printf "%b" "   ${YELLOW}⚠️ Node.js not found - code quality linters will not be available${NC}\n"
        if [[ -n "${required_node_version}" ]]; then
            printf "%b" "   ${CYAN}💡 Expected version: v${required_node_version} (from .nvmrc)${NC}\n"
        fi
        printf "%b" "   ${CYAN}💡 Download from: https://nodejs.org/${NC}\n"
    fi
}

install_pre_commit_hook() {
    if [[ "${SKIP_GIT_HOOK}" = true ]]; then
        return
    fi

    printf "\n%b" "${YELLOW}🪝 Setting up git hooks...${NC}\n"
    if [[ -d ".git/hooks" ]]; then
        if [[ -f "scripts/pre-commit" && -f "scripts/commit-msg" ]]; then
            cp scripts/pre-commit .git/hooks/pre-commit
            cp scripts/commit-msg .git/hooks/commit-msg
            chmod +x .git/hooks/pre-commit .git/hooks/commit-msg
            printf "%b" "   ${GREEN}✅ Git hooks installed (pre-commit, commit-msg)${NC}\n"
            printf "%b" "   ${CYAN}💡 Bypass with: git commit --no-verify${NC}\n"
        else
            printf "%b" "   ${YELLOW}⚠️ Hook scripts not found at scripts/pre-commit and scripts/commit-msg${NC}\n"
        fi
    else
        printf "%b" "   ${YELLOW}⚠️ Not a git repository - skipping hook installation${NC}\n"
    fi
}

print_summary() {
    printf "\n%b" "${GREEN}✨ Setup Complete!${NC}\n"
    printf "%b" "${GREEN}==================${NC}\n\n"

    printf "%b" "${CYAN}Next steps:${NC}\n"
    printf "%b" "  1. Run the application: ${NC}dotnet tool run aspire run\n"
    printf "%b" "  2. Run tests: ${NC}dotnet test\n"
    printf "%b" "  3. Install Playwright browsers after build: ${NC}bash scripts/install-playwright.sh\n"
    printf "%b" "  4. Check markdown: ${NC}npm run lint:md\n"
    printf "%b" "     ${CYAN}(If Aspire CLI is installed globally or via the official install script, 'aspire run' also works.)${NC}\n"
    printf "\n"
}

print_header
check_dotnet_sdk_version
restore_dotnet_dependencies
restore_dotnet_local_tools
check_aspnet_core_development_certificate_trust
check_powershell
check_nodejs_and_npm
install_pre_commit_hook
print_summary
