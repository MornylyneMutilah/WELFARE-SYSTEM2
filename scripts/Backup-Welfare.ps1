param(
    [string]$ApiBase = 'http://localhost:5249/api',
    [string]$BackupDirectory = '',
    [string]$ConfigurationFile = '',
    [string]$PgDumpPath = ''
)
$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path $PSScriptRoot -Parent
if (-not $env:WELFARE_ADMIN_TOKEN) { throw 'Set WELFARE_ADMIN_TOKEN to a current administrator access token in this terminal. Do not paste it into shared logs.' }
$destinationApi = [Uri]$ApiBase
if ($destinationApi.Scheme -ne 'https' -and -not ($destinationApi.Scheme -eq 'http' -and $destinationApi.IsLoopback)) { throw 'Use HTTPS or a loopback API endpoint.' }
if (-not $ConfigurationFile) { $ConfigurationFile = Join-Path $workspaceRoot 'backend/WelfareSystem.Api/appsettings.Local.json' }
$settings = Get-Content -LiteralPath $ConfigurationFile -Raw | ConvertFrom-Json
$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
$connection.set_ConnectionString([string]$settings.ConnectionStrings.DefaultConnection)
$previousEnvironment = @{}
foreach ($variable in @('PGHOST','PGPORT','PGDATABASE','PGUSER','PGPASSWORD')) { $previousEnvironment[$variable] = [Environment]::GetEnvironmentVariable($variable, 'Process') }
$headers = @{ Authorization = "Bearer $($env:WELFARE_ADMIN_TOKEN)" }
$resume = $false
try {
    $env:PGHOST = [string]$connection['Host']; $env:PGPORT = if ($connection.ContainsKey('Port')) { [string]$connection['Port'] } else { '5432' }
    $env:PGDATABASE = [string]$connection['Database']; $env:PGUSER = [string]$connection['Username']; $env:PGPASSWORD = [string]$connection['Password']
    if (-not $PgDumpPath) { $dumpCommand = Get-Command pg_dump -ErrorAction SilentlyContinue; if ($dumpCommand) { $PgDumpPath = $dumpCommand.Source } }
    if (-not $PgDumpPath -and $env:ProgramFiles) { $PgDumpPath = Get-ChildItem -Path (Join-Path $env:ProgramFiles 'PostgreSQL/*/bin/pg_dump.exe') -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName }
    if (-not $PgDumpPath -or -not (Test-Path -LiteralPath $PgDumpPath -PathType Leaf)) { throw 'Install PostgreSQL client tools or supply PgDumpPath.' }
    $status = Invoke-RestMethod "$ApiBase/operations/status" -Headers $headers
    $identityText = "$($env:PGHOST.ToLowerInvariant())|$env:PGPORT|$env:PGDATABASE|$env:PGUSER"
    $identityHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($identityText)))
    if (-not $status.databaseIdentityHash -or $identityHash -ne $status.databaseIdentityHash) { throw 'The backup connection does not match the API database. No maintenance or backup has started.' }
    if ($status.pendingMigrations.Count -gt 0) { throw 'Apply pending schema migrations before taking this release backup.' }
    $resume = -not $status.maintenance
    $maintenance = Invoke-RestMethod "$ApiBase/operations/maintenance" -Headers $headers -Method Post -ContentType 'application/json' -Body '{"enabled":true,"reason":"Coordinated database and private evidence backup"}'
    if (-not $maintenance.enabled -or $maintenance.activeWrites -ne 0) { throw 'Write requests have not drained; backup aborted.' }
    if (-not $BackupDirectory) { $BackupDirectory = Join-Path $workspaceRoot ('backups/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-ffff')) }
    $backupRoot = [IO.Path]::GetFullPath($BackupDirectory)
    if (Test-Path -LiteralPath $backupRoot) { throw 'Choose a new backup directory; existing backups are never overwritten.' }
    New-Item -ItemType Directory -Path $backupRoot | Out-Null
    $dumpFile = Join-Path $backupRoot 'WelfareDb.dump'
    & $PgDumpPath '--no-password' '--format=custom' '--no-owner' '--file' $dumpFile
    if ($LASTEXITCODE -ne 0) { throw 'pg_dump failed. Keep this incomplete backup separate from verified backups.' }
    $psqlPath = Join-Path (Split-Path $PgDumpPath -Parent) $(if ($IsWindows) { 'psql.exe' } else { 'psql' })
    $countsJson = & $psqlPath '--no-password' '--tuples-only' '--no-align' '--command' 'SELECT json_build_object(''Users'', (SELECT count(*) FROM "Users"), ''WelfareApplications'', (SELECT count(*) FROM "WelfareApplications"), ''ApplicationReviews'', (SELECT count(*) FROM "ApplicationReviews"), ''ApplicationDocuments'', (SELECT count(*) FROM "ApplicationDocuments"), ''AuditLogs'', (SELECT count(*) FROM "AuditLogs"));'
    if ($LASTEXITCODE -ne 0) { throw 'Could not capture backup record counts.' }
    $documentRoot = if ($settings.Documents.StoragePath) { [IO.Path]::GetFullPath($settings.Documents.StoragePath) } else { Join-Path $workspaceRoot 'backend/WelfareSystem.Api/App_Data/Documents' }
    $evidenceDestination = Join-Path $backupRoot 'Documents'
    New-Item -ItemType Directory -Path $evidenceDestination | Out-Null
    if (Test-Path -LiteralPath $documentRoot) { Get-ChildItem -LiteralPath $documentRoot -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $evidenceDestination } }
    $manifest = [ordered]@{ createdAtUtc = [DateTime]::UtcNow.ToString('O'); databaseFile = 'WelfareDb.dump'; databaseSha256 = (Get-FileHash -LiteralPath $dumpFile -Algorithm SHA256).Hash; rowCounts = ($countsJson | ConvertFrom-Json); evidence = @(Get-ChildItem -LiteralPath $evidenceDestination -File | ForEach-Object { @{ file = $_.Name; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } }); restoreTested = $false; note = 'Keep this backup private. Verify checksums and restore into a separate test database before treating recovery as proven.' }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backupRoot 'manifest.json') -Encoding utf8
    Write-Output "Coordinated backup created: $backupRoot"
} finally {
    if ($resume) {
        try { Invoke-RestMethod "$ApiBase/operations/maintenance" -Headers $headers -Method Post -ContentType 'application/json' -Body '{"enabled":false,"reason":"Coordinated backup finished or aborted; resume normal operation"}' | Out-Null }
        catch { Write-Warning 'Could not resume writes automatically. An administrator must disable maintenance from Readiness.' }
    }
    foreach ($variable in $previousEnvironment.Keys) { [Environment]::SetEnvironmentVariable($variable, $previousEnvironment[$variable], 'Process') }
}
