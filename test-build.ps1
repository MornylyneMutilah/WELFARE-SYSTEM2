Set-Location "c:\Users\morny\Desktop\WelfareSystem\backend\WelfareSystem.Api"
dotnet build --no-incremental
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}
