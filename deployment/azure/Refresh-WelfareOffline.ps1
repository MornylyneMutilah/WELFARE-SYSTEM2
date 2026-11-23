param(
    [Parameter(Mandatory=$true)][string]$AzureCli,
    [string]$StateDirectory='logs/azure-deployment',
    [string]$PostgresBin='C:/Program Files/PostgreSQL/18/bin'
)
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $root
$AzureCli=(Resolve-Path -LiteralPath $AzureCli).Path
$state=Get-Content (Join-Path $StateDirectory 'infrastructure.json') -Raw | ConvertFrom-Json
if (-not $state.publicOrigin -or ([Uri]$state.publicOrigin).Scheme -ne 'https') { throw 'Deploy and verify the hosted HTTPS application first.' }
if (-not $env:WELFARE_ADMIN_TOKEN) { throw 'Set WELFARE_ADMIN_TOKEN to a current hosted administrator token in this terminal.' }
function Az([string[]]$Arguments) {
    $raw=& $AzureCli @Arguments --only-show-errors -o json
    if ($LASTEXITCODE -ne 0) { throw "Azure operation failed: $($Arguments[0..1] -join ' ')" }
    if ($raw) { return ($raw -join "`n" | ConvertFrom-Json) }
}
$headers=@{Authorization="Bearer $($env:WELFARE_ADMIN_TOKEN)"}
$api=$state.publicOrigin+'/api'
$id=[Guid]::NewGuid().ToString('N')
$snapshot=Join-Path $root ('backups/azure-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+$id.Substring(0,8))
$paused=$false
$scalePinned=$false
try {
    $status=Invoke-RestMethod "$api/operations/status" -Headers $headers
    $text="$($state.databaseHost.ToLowerInvariant())|5432|WelfareDb|welfare_app"
    $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text)))
    if ($status.databaseIdentityHash -ne $hash -or $status.maintenance -or $status.pendingMigrations.Count -gt 0) { throw 'Hosted database identity, schema or maintenance state is unsuitable for this snapshot.' }
    # Pin a single replica before maintenance so scale-to-zero cannot reset the gate.
    Az @('containerapp','update','-g',$state.resourceGroup,'-n','welfare-presentation','--min-replicas','1','--max-replicas','1') | Out-Null
    $scalePinned=$true
    $deadline=[DateTime]::UtcNow.AddMinutes(10)
    do {
        $app=Az @('containerapp','show','-g',$state.resourceGroup,'-n','welfare-presentation')
        if ($app.properties.latestReadyRevisionName -eq $app.properties.latestRevisionName) { break }
        if ([DateTime]::UtcNow -gt $deadline) { throw 'The pinned revision did not become ready.' }
        Start-Sleep -Seconds 5
    } while ($true)
    $paused=$true
    $maintenance=Invoke-RestMethod "$api/operations/maintenance" -Headers $headers -Method Post -ContentType 'application/json' -Body '{"enabled":true,"reason":"Coordinated Azure to laptop offline snapshot"}'
    if (-not $maintenance.enabled -or $maintenance.activeWrites -ne 0) { throw 'Application writes did not drain.' }
    $env:WELFARE_AZURE_WRITES_QUIESCED='true'
    & (Join-Path $PSScriptRoot 'Deploy-WelfareContainers.ps1') -AzureCli $AzureCli -StateDirectory $StateDirectory -Stage Export -SnapshotId $id
    $execution=Get-Content (Join-Path $StateDirectory 'export-execution.json') -Raw | ConvertFrom-Json
    $deadline=[DateTime]::UtcNow.AddMinutes(15)
    do {
        $run=Az @('containerapp','job','execution','show','-g',$state.resourceGroup,'-n','welfare-presentation-export','--job-execution-name',$execution.name)
        if ($run.properties.status -eq 'Succeeded') { break }
        if ($run.properties.status -in @('Failed','Stopped') -or [DateTime]::UtcNow -gt $deadline) { throw 'Azure snapshot job did not succeed.' }
        Start-Sleep -Seconds 5
    } while ($true)
    $keys=Az @('storage','account','keys','list','-g',$state.resourceGroup,'-n',$state.storageAccount)
    $env:AZURE_STORAGE_ACCOUNT=$state.storageAccount; $env:AZURE_STORAGE_KEY=$keys[0].value
    New-Item -ItemType Directory -Path $snapshot | Out-Null
    Az @('storage','file','download-batch','--source','transfer','--destination',$snapshot,'--pattern',("snapshot-$id/*"),'--no-progress') | Out-Null
    # download-batch preserves the snapshot folder beneath the destination.
    $snapshot=Join-Path $snapshot "snapshot-$id"
    & (Join-Path $root 'scripts/Verify-WelfareBackup.ps1') -BackupDirectory $snapshot
} finally {
    Remove-Item Env:WELFARE_AZURE_WRITES_QUIESCED,Env:AZURE_STORAGE_KEY,Env:AZURE_STORAGE_ACCOUNT -ErrorAction SilentlyContinue
    if ($paused) {
        Invoke-RestMethod "$api/operations/maintenance" -Headers $headers -Method Post -ContentType 'application/json' -Body '{"enabled":false,"reason":"Azure snapshot finished or stopped; resume normal service"}' | Out-Null
    }
    if ($scalePinned) { Az @('containerapp','update','-g',$state.resourceGroup,'-n','welfare-presentation','--min-replicas','0') | Out-Null }
}
# Restore to a NEW local database. Neither the original WelfareDb nor earlier offline copies are overwritten.
$settings=Get-Content backend/WelfareSystem.Api/appsettings.Local.json -Raw | ConvertFrom-Json
$connection=[System.Data.Common.DbConnectionStringBuilder]::new()
$connection.set_ConnectionString([string]$settings.ConnectionStrings.DefaultConnection)
$previous=@{}
foreach($name in @('PGHOST','PGPORT','PGUSER','PGPASSWORD','PGDATABASE')) { $previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process') }
try {
    $env:PGHOST=[string]$connection['Host']; $env:PGPORT=if($connection.ContainsKey('Port')){[string]$connection['Port']}else{'5432'}
    $env:PGUSER=[string]$connection['Username']; $env:PGPASSWORD=[string]$connection['Password']; $env:PGDATABASE='postgres'
    $offlineDb='welfare_offline_'+$id
    & (Join-Path $PostgresBin 'createdb.exe') --no-password $offlineDb
    if($LASTEXITCODE -ne 0){throw 'Could not create isolated offline database.'}
    & (Join-Path $PostgresBin 'pg_restore.exe') --no-password --exit-on-error --single-transaction --no-owner --no-acl --dbname $offlineDb (Join-Path $snapshot 'WelfareDb.dump')
    if($LASTEXITCODE -ne 0){throw 'Offline restore failed; previous configuration remains selected.'}
    $env:PGDATABASE=$offlineDb
    $raw=& (Join-Path $PostgresBin 'psql.exe') --no-password -At -c 'SELECT json_build_object(''Users'',(SELECT count(*) FROM "Users"),''WelfareApplications'',(SELECT count(*) FROM "WelfareApplications"),''ApplicationReviews'',(SELECT count(*) FROM "ApplicationReviews"),''ApplicationDocuments'',(SELECT count(*) FROM "ApplicationDocuments"),''AuditLogs'',(SELECT count(*) FROM "AuditLogs"));'
    if($LASTEXITCODE -ne 0){throw 'Restored database verification failed.'}
    $counts=$raw|ConvertFrom-Json; $manifest=Get-Content (Join-Path $snapshot 'manifest.json') -Raw | ConvertFrom-Json
    foreach($item in $manifest.rowCounts.PSObject.Properties){if($counts.($item.Name) -ne $item.Value){throw "Offline count mismatch: $($item.Name)"}}
    $files=Join-Path $root ('logs/offline-data-'+$id+'/Documents')
    New-Item -ItemType Directory -Path $files | Out-Null
    foreach($entry in $manifest.evidence){Copy-Item -LiteralPath (Join-Path (Join-Path $snapshot 'Documents') $entry.file) -Destination $files; if((Get-FileHash (Join-Path $files $entry.file) -Algorithm SHA256).Hash -ne $entry.sha256){throw 'Working evidence checksum mismatch.'}}
    $connection['Database']=$offlineDb
    @{connectionString=$connection.ConnectionString;documentsPath=$files;snapshotPath=$snapshot;verifiedAtUtc=[DateTime]::UtcNow.ToString('O')} | ConvertTo-Json | Set-Content logs/azure-deployment/offline.private.json -Encoding utf8
    Write-Output "Verified offline copy ready: $offlineDb. Original local database preserved."
} finally { foreach($name in $previous.Keys){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')} }
