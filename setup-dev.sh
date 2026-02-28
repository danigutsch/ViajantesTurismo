#!/bin/sh
# ViajantesTurismo Development Environment Setup Script
# This script installs all required and optional tools for development

set -e

SKIP_GIT_HOOK=false
SKIP_NPM=false

# Parse arguments
while [ $# -gt 0 ]; do
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

printf "\n%b" "${CYAN}🚀 ViajantesTurismo Development Setup${NC}\n"
printf "%b" "${CYAN}====================================${NC}\n\n"

# Check .NET SDK version
printf "%b" "${YELLOW}📦 Checking .NET SDK...${NC}\n"
REQUIRED_VERSION=$(grep -oP '(?<="version": ")[^"]*' global.json)
INSTALLED_SDK=$(dotnet --version 2>&1 || echo "not found")

if [ "${INSTALLED_SDK}" != "not found" ]; then
    printf "%b" "   ${GREEN}✅ .NET SDK installed: ${INSTALLED_SDK}${NC}\n"
    if [ "${INSTALLED_SDK}" != "${REQUIRED_VERSION}" ]; then
        printf "%b" "   ${YELLOW}⚠️ Required version: ${REQUIRED_VERSION}${NC}\n"
        printf "%b" "   ${CYAN}💡 Download from: https://dotnet.microsoft.com/download/dotnet/10.0${NC}\n"
    fi
else
    printf "%b" "   ${RED}❌ .NET SDK not found${NC}\n"
    printf "%b" "   ${CYAN}💡 Download .NET ${REQUIRED_VERSION} from: https://dotnet.microsoft.com/download/dotnet/10.0${NC}\n"
    exit 1
fi

# Restore .NET dependencies
printf "\n%b" "${YELLOW}📦 Restoring .NET dependencies...${NC}\n"
if dotnet restore > /dev/null 2>&1; then
    printf "%b" "   ${GREEN}✅ .NET dependencies restored${NC}\n"
else
    printf "%b" "   ${RED}❌ Failed to restore .NET dependencies${NC}\n"
    exit 1
fi

# Restore .NET local tools
printf "\n%b" "${YELLOW}🔧 Restoring .NET local tools...${NC}\n"
if dotnet tool restore > /dev/null 2>&1; then
    printf "%b" "   ${GREEN}✅ .NET tools restored (dotnet-ef, reportgenerator, Aspire CLI)${NC}\n"
else
    printf "%b" "   ${YELLOW}⚠️ Failed to restore .NET tools${NC}\n"
fi

# Check for PowerShell (optional - only for PSScriptAnalyzer)
printf "\n%b" "${YELLOW}🔍 Checking PowerShell for script analysis...${NC}\n"
if command -v pwsh > /dev/null 2>&1; then
    printf "%b" "   ${GREEN}✅ PowerShell installed${NC}\n"
    printf "%b" "   ${CYAN}💡 To install PSScriptAnalyzer:${NC}\n"
    printf "%b" "   ${CYAN}   pwsh -Command 'Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force'${NC}\n"
else
    printf "%b" "   ${YELLOW}⚠️ PowerShell not available - PowerShell script linting will be skipped${NC}\n"
    printf "%b" "   ${CYAN}💡 Install from: https://github.com/PowerShell/PowerShell${NC}\n"
fi

# Check Node.js and npm
if [ "${SKIP_NPM}" = false ]; then
    printf "\n%b" "${YELLOW}📦 Checking Node.js and npm...${NC}\n"

    # Read required Node.js version from .nvmrc
    if [ -f ".nvmrc" ]; then
        read -r REQUIRED_NODE_VERSION < .nvmrc
    else
        printf "%b" "   ${YELLOW}⚠️ .nvmrc file not found${NC}\n"
        REQUIRED_NODE_VERSION=""
    fi

    NODE_VERSION=$(node --version 2>&1 || echo "not found")

    if [ "${NODE_VERSION}" != "not found" ]; then
        printf "%b" "   ${GREEN}✅ Node.js installed: ${NODE_VERSION}${NC}\n"

        # Validate version matches .nvmrc
        if [ -n "${REQUIRED_NODE_VERSION}" ]; then
            if echo "${NODE_VERSION}" | grep -q "v${REQUIRED_NODE_VERSION}"; then
                printf "%b" "   ${GREEN}✅ Version matches .nvmrc (${REQUIRED_NODE_VERSION})${NC}\n"
            else
                printf "%b" "   ${YELLOW}⚠️ Version mismatch!${NC}\n"
                printf "%b" "   ${YELLOW}   Required: v${REQUIRED_NODE_VERSION} (from .nvmrc)${NC}\n"
                printf "%b" "   ${YELLOW}   Installed: ${NODE_VERSION}${NC}\n"
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
        read -r INSTALL_LINTERS

        if [ "${INSTALL_LINTERS}" = "y" ] || [ "${INSTALL_LINTERS}" = "Y" ]; then
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
        if [ -n "${REQUIRED_NODE_VERSION}" ]; then
            printf "%b" "   ${CYAN}💡 Expected version: v${REQUIRED_NODE_VERSION} (from .nvmrc)${NC}\n"
        fi
        printf "%b" "   ${CYAN}💡 Download from: https://nodejs.org/${NC}\n"
    fi
fi

# Install pre-commit hook (optional)
if [ "${SKIP_GIT_HOOK}" = false ]; then
    printf "\n%b" "${YELLOW}🪝 Setting up git pre-commit hook...${NC}\n"
    if [ -d ".git/hooks" ]; then
        if [ -f "scripts/pre-commit" ]; then
            cp scripts/pre-commit .git/hooks/pre-commit
            chmod +x .git/hooks/pre-commit
            printf "%b" "   ${GREEN}✅ Pre-commit hook installed${NC}\n"
            printf "%b" "   ${CYAN}💡 Bypass with: git commit --no-verify${NC}\n"
        else
            printf "%b" "   ${YELLOW}⚠️ Pre-commit hook script not found at scripts/pre-commit${NC}\n"
        fi
    else
        printf "%b" "   ${YELLOW}⚠️ Not a git repository - skipping hook installation${NC}\n"
    fi
fi

# Summary
printf "\n%b" "${GREEN}✨ Setup Complete!${NC}\n"
printf "%b" "${GREEN}==================${NC}\n\n"

printf "%b" "${CYAN}Next steps:${NC}\n"
printf "%b" "  1. Run the application: ${NC}dotnet run --project src/ViajantesTurismo.AppHost\n"
printf "%b" "  2. Run tests: ${NC}dotnet test\n"
printf "%b" "  3. Check markdown: ${NC}npm run lint:md\n"
printf "\n"
