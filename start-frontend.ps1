# Start Frontend Script
Write-Host "Starting Welfare System Frontend..." -ForegroundColor Green
Write-Host ""

Set-Location "$PSScriptRoot\frontend"

Write-Host "Starting Next.js development server..." -ForegroundColor Yellow
npm run dev

# The script will continue running until Ctrl+C is pressed
