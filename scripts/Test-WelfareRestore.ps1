param(
    [Parameter(Mandatory=$true)][string]$BackupDirectory,
    [string]$ConfigurationFile = '',
    [string]$PostgresBin = '',
    [switch]$RemoveTestDatabase
)
$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path $PSScriptRoot -Parent
if (-not $ConfigurationFile) { $ConfigurationFile = Join-Path $workspaceRoot 'backend/WelfareSystem.Api/appsettings.Local.json' }
& (Join-Path $PSScriptRoot 'Verify-WelfareBackup.ps1') -BackupDirectory $BackupDirectory
$backupRoot = [IO.Path]::GetFullPath($BackupDirectory)
$manifest = Get-Content -LiteralPath (Join-Path $backupRoot 'manifest.json') -Raw | ConvertFrom-Json
$settings = Get-Content -LiteralPath $ConfigurationFile -Raw | ConvertFrom-Json
$connection = [System.Data.Common.DbConnectionStringBuilder]::new(); $connection.set_ConnectionString([string]$settings.ConnectionStrings.DefaultConnection)
$previousEnvironment = @{}
foreach ($variable in @('PGHOST','PGPORT','PGDATABASE','PGUSER','PGPASSWORD')) { $previousEnvironment[$variable] = [Environment]::GetEnvironmentVariable($variable, 'Process') }
$testDatabase = 'welfare_restore_check_' + [Guid]::NewGuid().ToString('N')
$created = $false
$restoreFilesRoot = $null
try {
    $env:PGHOST = [string]$connection['Host']; $env:PGPORT = if ($connection.ContainsKey('Port')) { [string]$connection['Port'] } else { '5432' }
    $env:PGUSER = [string]$connection['Username']; $env:PGPASSWORD = [string]$connection['Password']; $env:PGDATABASE = 'postgres'
    if (-not $PostgresBin) { $command = Get-Command pg_restore -ErrorAction SilentlyContinue; if ($command) { $PostgresBin = Split-Path $command.Source -Parent } }
    if (-not $PostgresBin -and $env:ProgramFiles) { $commandPath = Get-ChildItem -Path (Join-Path $env:ProgramFiles 'PostgreSQL/*/bin/pg_restore.exe') -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName; if ($commandPath) { $PostgresBin = Split-Path $commandPath -Parent } }
    if (-not $PostgresBin) { throw 'Specify the PostgreSQL client binary directory.' }
    $suffix = if ($IsWindows) { '.exe' } else { '' }
    $create = Join-Path $PostgresBin ('createdb' + $suffix); $restore = Join-Path $PostgresBin ('pg_restore' + $suffix); $psql = Join-Path $PostgresBin ('psql' + $suffix); $drop = Join-Path $PostgresBin ('dropdb' + $suffix)
    & $create '--no-password' '--maintenance-db=postgres' $testDatabase
    if ($LASTEXITCODE -ne 0) { throw 'The new isolated test database could not be created. No restore has been attempted.' }
    $created = $true
    & $restore '--no-password' '--exit-on-error' '--no-owner' '--dbname' $testDatabase (Join-Path $backupRoot 'WelfareDb.dump')
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed in the isolated test database; the source database was not changed.' }
    $countsJson = & $psql '--no-password' '--dbname' $testDatabase '--tuples-only' '--no-align' '--command' 'SELECT json_build_object(''Users'', (SELECT count(*) FROM "Users"), ''WelfareApplications'', (SELECT count(*) FROM "WelfareApplications"), ''ApplicationReviews'', (SELECT count(*) FROM "ApplicationReviews"), ''ApplicationDocuments'', (SELECT count(*) FROM "ApplicationDocuments"), ''AuditLogs'', (SELECT count(*) FROM "AuditLogs"));'
    if ($LASTEXITCODE -ne 0) { throw 'Restored database queries failed.' }
    $restored = $countsJson | ConvertFrom-Json
    foreach ($entry in $manifest.rowCounts.PSObject.Properties) { if ($restored.($entry.Name) -ne $entry.Value) { throw "Restored record count mismatch: $($entry.Name)" } }
    $restoreFilesRoot = Join-Path $backupRoot ('restore-files-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $restoreFilesRoot | Out-Null
    foreach ($entry in $manifest.evidence) {
        $sourceFile = Join-Path (Join-Path $backupRoot 'Documents') $entry.file
        Copy-Item -LiteralPath $sourceFile -Destination $restoreFilesRoot
        if ((Get-FileHash -LiteralPath (Join-Path $restoreFilesRoot $entry.file) -Algorithm SHA256).Hash -ne $entry.sha256) { throw 'Restored evidence checksum mismatch.' }
    }
    $report = @{ testedAtUtc = [DateTime]::UtcNow.ToString('O'); testDatabase = $testDatabase; sourceDatabaseChanged = $false; databaseRestoreSucceeded = $true; rowCountsMatched = $true; evidenceChecksumsMatched = $true; testDatabaseRemovalRequested = [bool]$RemoveTestDatabase }
    $report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $backupRoot 'restore-test.json') -Encoding utf8
    Write-Output 'Isolated database restore passed, record counts match, and private evidence checksums match. The source database was not modified.'
} finally {
    if ($restoreFilesRoot -and [IO.Path]::GetFullPath($restoreFilesRoot).StartsWith($backupRoot + [IO.Path]::DirectorySeparatorChar) -and (Test-Path -LiteralPath $restoreFilesRoot)) {
        Get-ChildItem -LiteralPath $restoreFilesRoot -File | ForEach-Object { Remove-Item -LiteralPath $_.FullName }
        Remove-Item -LiteralPath $restoreFilesRoot
    }
    if ($created -and $RemoveTestDatabase -and $testDatabase -match '^welfare_restore_check_[a-f0-9]{32}$' -and $testDatabase -ne [string]$connection['Database']) {
        & $drop '--no-password' '--maintenance-db=postgres' $testDatabase
        if ($LASTEXITCODE -ne 0) { Write-Warning "The isolated test database remains: $testDatabase" }
    }
    foreach ($variable in $previousEnvironment.Keys) { [Environment]::SetEnvironmentVariable($variable, $previousEnvironment[$variable], 'Process') }
}
