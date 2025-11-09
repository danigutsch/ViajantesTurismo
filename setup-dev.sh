#!/usr/bin/env bash
# ViajantesTurismo Development Environment Setup Script
# This script installs all required and optional tools for development

set -e

SKIP_GIT_HOOK=false
SKIP_NPM=false

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

echo -e "\n${CYAN}🚀 ViajantesTurismo Development Setup${NC}"
echo -e "${CYAN}====================================${NC}\n"

# Check .NET SDK version
echo -e "${YELLOW}📦 Checking .NET SDK...${NC}"
REQUIRED_VERSION=$(grep -oP '(?<="version": ")[^"]*' global.json)
INSTALLED_SDK=$(dotnet --version 2>&1 || echo "not found")

if [ "$INSTALLED_SDK" != "not found" ]; then
    echo -e "   ${GREEN}✅ .NET SDK installed: $INSTALLED_SDK${NC}"
    if [ "$INSTALLED_SDK" != "$REQUIRED_VERSION" ]; then
        echo -e "   ${YELLOW}⚠️  Required version: $REQUIRED_VERSION${NC}"
        echo -e "   ${CYAN}💡 Download from: https://dotnet.microsoft.com/download/dotnet/10.0${NC}"
    fi
else
    echo -e "   ${RED}❌ .NET SDK not found${NC}"
    echo -e "   ${CYAN}💡 Download .NET $REQUIRED_VERSION from: https://dotnet.microsoft.com/download/dotnet/10.0${NC}"
    exit 1
fi

# Restore .NET dependencies
echo -e "\n${YELLOW}📦 Restoring .NET dependencies...${NC}"
if dotnet restore > /dev/null 2>&1; then
    echo -e "   ${GREEN}✅ .NET dependencies restored${NC}"
else
    echo -e "   ${RED}❌ Failed to restore .NET dependencies${NC}"
    exit 1
fi

# Restore .NET local tools
echo -e "\n${YELLOW}🔧 Restoring .NET local tools...${NC}"
if dotnet tool restore > /dev/null 2>&1; then
    echo -e "   ${GREEN}✅ .NET tools restored (dotnet-ef, reportgenerator, Aspire CLI)${NC}"
else
    echo -e "   ${YELLOW}⚠️  Failed to restore .NET tools${NC}"
fi

# Check for PowerShell (optional - only for PSScriptAnalyzer)
echo -e "\n${YELLOW}🔍 Checking PowerShell for script analysis...${NC}"
if command -v pwsh > /dev/null 2>&1; then
    echo -e "   ${GREEN}✅ PowerShell installed${NC}"
    echo -e "   ${CYAN}💡 To install PSScriptAnalyzer:${NC}"
    echo -e "   ${CYAN}   pwsh -Command 'Install-Module -Name PSScriptAnalyzer -Scope CurrentUser -Force'${NC}"
else
    echo -e "   ${YELLOW}⚠️  PowerShell not available - PowerShell script linting will be skipped${NC}"
    echo -e "   ${CYAN}💡 Install from: https://github.com/PowerShell/PowerShell${NC}"
fi

# Check Node.js and npm
if [ "$SKIP_NPM" = false ]; then
    echo -e "\n${YELLOW}📦 Checking Node.js and npm...${NC}"
    NODE_VERSION=$(node --version 2>&1 || echo "not found")
    if [ "$NODE_VERSION" != "not found" ]; then
        echo -e "   ${GREEN}✅ Node.js installed: $NODE_VERSION${NC}"
        
        echo -e "\n${YELLOW}📦 Installing npm dependencies...${NC}"
        if npm install > /dev/null 2>&1; then
            echo -e "   ${GREEN}✅ npm dependencies installed (markdownlint-cli, shellcheck, shfmt)${NC}"
        else
            echo -e "   ${RED}❌ Failed to install npm dependencies${NC}"
            exit 1
        fi
    else
        echo -e "   ${YELLOW}⚠️  Node.js not found - markdown and shell linting will not be available${NC}"
        echo -e "   ${CYAN}💡 Download from: https://nodejs.org/${NC}"
    fi
fi

# Install pre-commit hook (optional)
if [ "$SKIP_GIT_HOOK" = false ]; then
    echo -e "\n${YELLOW}🪝 Setting up git pre-commit hook...${NC}"
    if [ -d ".git/hooks" ]; then
        if [ -f "scripts/pre-commit" ]; then
            cp scripts/pre-commit .git/hooks/pre-commit
            chmod +x .git/hooks/pre-commit
            echo -e "   ${GREEN}✅ Pre-commit hook installed${NC}"
            echo -e "   ${CYAN}💡 Bypass with: git commit --no-verify${NC}"
        else
            echo -e "   ${YELLOW}⚠️  Pre-commit hook script not found at scripts/pre-commit${NC}"
        fi
    else
        echo -e "   ${YELLOW}⚠️  Not a git repository - skipping hook installation${NC}"
    fi
fi

# Summary
echo -e "\n${GREEN}✨ Setup Complete!${NC}"
echo -e "${GREEN}==================${NC}\n"

echo -e "${CYAN}Next steps:${NC}"
echo -e "  1. Run the application: ${NC}dotnet run --project src/ViajantesTurismo.AppHost"
echo -e "  2. Run tests: ${NC}dotnet test"
echo -e "  3. Check markdown: ${NC}npm run lint:md"
echo ""
