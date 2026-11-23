param()
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$settings=Get-Content (Join-Path $root 'logs/azure-deployment/offline.private.json') -Raw | ConvertFrom-Json
foreach($port in @(3000,5249)){if(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue){throw "Port $port is already in use. Stop the existing local application before selecting the offline copy."}}
$names=@('ConnectionStrings__DefaultConnection','Documents__StoragePath','ASPNETCORE_ENVIRONMENT','ASPNETCORE_URLS','NEXT_PUBLIC_API_URL','Frontend__BaseUrl','Authentication__BackendBaseUrl')
$previous=@{}; foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    $env:ConnectionStrings__DefaultConnection=$settings.connectionString
    $env:Documents__StoragePath=$settings.documentsPath
    $env:ASPNETCORE_ENVIRONMENT='Development'; $env:ASPNETCORE_URLS='http://localhost:5249'
    $env:Frontend__BaseUrl='http://localhost:3000'; $env:Authentication__BackendBaseUrl='http://localhost:5249'
    $env:NEXT_PUBLIC_API_URL='http://localhost:5249/api'
    $api=Start-Process dotnet -ArgumentList @('run','--no-launch-profile') -WorkingDirectory (Join-Path $root 'backend/WelfareSystem.Api') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $root 'logs/azure-deployment/offline-api.out.log') -RedirectStandardError (Join-Path $root 'logs/azure-deployment/offline-api.err.log') -PassThru
    $front=Start-Process node -ArgumentList @('node_modules/next/dist/bin/next','dev','--webpack') -WorkingDirectory (Join-Path $root 'frontend') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $root 'logs/azure-deployment/offline-frontend.out.log') -RedirectStandardError (Join-Path $root 'logs/azure-deployment/offline-frontend.err.log') -PassThru
    @{api=$api.Id;frontend=$front.Id}|ConvertTo-Json|Set-Content (Join-Path $root 'logs/azure-deployment/offline-processes.json')
    Write-Output 'Offline copy is starting at http://localhost:3000. Use the passwords from the Azure snapshot. Offline edits are not merged into Azure.'
} finally {foreach($name in $previous.Keys){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}}
