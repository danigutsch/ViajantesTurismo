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
        printf "%b" "   ${YELLOW}⚠️  Required version: ${REQUIRED_VERSION}${NC}\n"
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
    printf "%b" "   ${YELLOW}⚠️  Failed to restore .NET tools${NC}\n"
fi

# Check for PowerShell (optional - only for PSScriptAnalyzer)
printf "\n%b" "${YELLOW}🔍 Checking PowerShell for script analysis...${NC}\n"
if command -v pwsh > /dev/null 2>&1; then
    printf "%b" "   ${GREEN}✅ PowerShell installed${NC}\n"
    printf "%b" "   ${CYAN}💡 To install PSScriptAnalyzer:${NC}\n"
    printf "%b" "   ${CYAN}   pwsh -Command 'Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force'${NC}\n"
else
    printf "%b" "   ${YELLOW}⚠️  PowerShell not available - PowerShell script linting will be skipped${NC}\n"
    printf "%b" "   ${CYAN}💡 Install from: https://github.com/PowerShell/PowerShell${NC}\n"
fi

# Check for Go (optional - only for shfmt shell formatter)
printf "\n%b" "${YELLOW}🔍 Checking Go for shell script formatting...${NC}\n"
if command -v go > /dev/null 2>&1; then
    GO_VERSION=$(go version | awk '{print $3}')
    printf "%b" "   ${GREEN}✅ Go installed: ${GO_VERSION}${NC}\n"

    # Check if shfmt is installed
    if command -v shfmt > /dev/null 2>&1; then
        SHFMT_VERSION=$(shfmt --version)
        printf "%b" "   ${GREEN}✅ shfmt installed: v${SHFMT_VERSION}${NC}\n"
    else
        printf "%b" "   ${CYAN}💡 Installing Go tools (shfmt)...${NC}\n"
        if go generate -tags tools tools.go > /dev/null 2>&1; then
            printf "%b" "   ${GREEN}✅ shfmt installed${NC}\n"
        else
            printf "%b" "   ${YELLOW}⚠️  Failed to install Go tools${NC}\n"
            printf "%b" "   ${CYAN}💡 Manual install: go install mvdan.cc/sh/v3/cmd/shfmt@latest${NC}\n"
        fi
    fi
else
    printf "%b" "   ${YELLOW}⚠️  Go not available - shell script auto-formatting will be skipped${NC}\n"
    printf "%b" "   ${CYAN}💡 Install from: https://go.dev/dl/${NC}\n"
    printf "%b" "   ${CYAN}   After installing Go, run: ./setup-dev.sh${NC}\n"
fi

# Check Node.js and npm
if [ "${SKIP_NPM}" = false ]; then
    printf "\n%b" "${YELLOW}📦 Checking Node.js and npm...${NC}\n"
    NODE_VERSION=$(node --version 2>&1 || echo "not found")
    if [ "${NODE_VERSION}" != "not found" ]; then
        printf "%b" "   ${GREEN}✅ Node.js installed: ${NODE_VERSION}${NC}\n"

        printf "\n%b" "${YELLOW}📦 Installing npm dependencies...${NC}\n"
        if npm install > /dev/null 2>&1; then
            printf "%b" "   ${GREEN}✅ npm dependencies installed (markdownlint-cli, shellcheck)${NC}\n"
        else
            printf "%b" "   ${RED}❌ Failed to install npm dependencies${NC}\n"
            exit 1
        fi
    else
        printf "%b" "   ${YELLOW}⚠️  Node.js not found - markdown and shell linting will not be available${NC}\n"
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
            printf "%b" "   ${YELLOW}⚠️  Pre-commit hook script not found at scripts/pre-commit${NC}\n"
        fi
    else
        printf "%b" "   ${YELLOW}⚠️  Not a git repository - skipping hook installation${NC}\n"
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
