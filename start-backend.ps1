# Start Backend Script
Write-Host "Starting Welfare System Backend..." -ForegroundColor Green
Write-Host ""

Set-Location "$PSScriptRoot\backend\WelfareSystem.Api"

Write-Host "Building and starting backend..." -ForegroundColor Yellow
dotnet run

# The script will continue running until Ctrl+C is pressed
