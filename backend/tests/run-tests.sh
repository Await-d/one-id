#!/bin/bash

# OneID Test Runner Script
# This script runs all unit and integration tests with code coverage

set -e

echo "========================================="
echo "OneID Test Runner"
echo "========================================="
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed or not in PATH"
    exit 1
fi

# Navigate to test directory
cd "$(dirname "$0")"
TEST_DIR=$(pwd)
BACKEND_DIR=$(dirname "$TEST_DIR")

echo "${YELLOW}Building solution...${NC}"
cd "$BACKEND_DIR"
dotnet build --configuration Release

echo ""
echo "${YELLOW}Running Identity Tests...${NC}"
cd "$TEST_DIR/OneID.Identity.Tests"
dotnet test --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"

echo ""
echo "${YELLOW}Running Admin API Tests...${NC}"
cd "$TEST_DIR/OneID.AdminApi.Tests"
dotnet test --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"

echo ""
echo "${GREEN}=========================================${NC}"
echo "${GREEN}All tests completed successfully!${NC}"
echo "${GREEN}=========================================${NC}"
echo ""
echo "Code coverage reports are available in:"
echo "  - $TEST_DIR/OneID.Identity.Tests/TestResults/"
echo "  - $TEST_DIR/OneID.AdminApi.Tests/TestResults/"
echo ""
echo "To view detailed coverage report, use a tool like ReportGenerator:"
echo "  dotnet tool install -g dotnet-reportgenerator-globaltool"
echo "  reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report"

