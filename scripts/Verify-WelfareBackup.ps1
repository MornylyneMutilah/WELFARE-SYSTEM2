param([Parameter(Mandatory=$true)][string]$BackupDirectory)
$ErrorActionPreference = 'Stop'
$backupRoot = [IO.Path]::GetFullPath($BackupDirectory)
$manifest = Get-Content -LiteralPath (Join-Path $backupRoot 'manifest.json') -Raw | ConvertFrom-Json
if ((Get-FileHash -LiteralPath (Join-Path $backupRoot 'WelfareDb.dump') -Algorithm SHA256).Hash -ne $manifest.databaseSha256) { throw 'Database checksum mismatch.' }
foreach ($entry in $manifest.evidence) {
    if ([IO.Path]::GetFileName($entry.file) -ne $entry.file) { throw 'Invalid file name in backup manifest.' }
    if ((Get-FileHash -LiteralPath (Join-Path (Join-Path $backupRoot 'Documents') $entry.file) -Algorithm SHA256).Hash -ne $entry.sha256) { throw "Evidence checksum mismatch: $($entry.file)" }
}
Write-Output 'Backup checksums match. A separate database/file restore drill is still required.'
