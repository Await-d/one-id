# OneID Test Runner Script (PowerShell)
# This script runs all unit and integration tests with code coverage

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "OneID Test Runner" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: .NET SDK is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Navigate to test directory
$TEST_DIR = $PSScriptRoot
$BACKEND_DIR = Split-Path -Parent $TEST_DIR

Write-Host "Building solution..." -ForegroundColor Yellow
Set-Location $BACKEND_DIR
dotnet build --configuration Release

Write-Host ""
Write-Host "Running Identity Tests..." -ForegroundColor Yellow
Set-Location "$TEST_DIR\OneID.Identity.Tests"
dotnet test --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"

Write-Host ""
Write-Host "Running Admin API Tests..." -ForegroundColor Yellow
Set-Location "$TEST_DIR\OneID.AdminApi.Tests"
dotnet test --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "All tests completed successfully!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Code coverage reports are available in:"
Write-Host "  - $TEST_DIR\OneID.Identity.Tests\TestResults\"
Write-Host "  - $TEST_DIR\OneID.AdminApi.Tests\TestResults\"
Write-Host ""
Write-Host "To view detailed coverage report, use a tool like ReportGenerator:"
Write-Host "  dotnet tool install -g dotnet-reportgenerator-globaltool"
Write-Host "  reportgenerator -reports:**\coverage.cobertura.xml -targetdir:coverage-report"

