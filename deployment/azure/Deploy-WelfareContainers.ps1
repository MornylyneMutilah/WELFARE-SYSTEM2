param(
    [Parameter(Mandatory=$true)][string]$AzureCli,
    [ValidateSet('Storage','Import','Application','Export')][string]$Stage,
    [string]$SnapshotId,
    [string]$StateDirectory='logs/azure-deployment',
    [string]$ImageTag='demo-20261123-1',
    [string]$SnapshotDirectory='backups/azure-presentation-initial'
)
$ErrorActionPreference='Stop'
$AzureCli=(Resolve-Path -LiteralPath $AzureCli).Path
$stateRoot=[IO.Path]::GetFullPath($StateDirectory)
$state=Get-Content (Join-Path $stateRoot 'infrastructure.json') -Raw | ConvertFrom-Json
function Az([string[]]$Arguments) {
    $result=& $AzureCli @Arguments --only-show-errors -o json
    if ($LASTEXITCODE -ne 0) { throw "Azure operation failed: $($Arguments[0..1] -join ' ')" }
    if ($result) { return ($result -join "`n" | ConvertFrom-Json) }
}
function PrivateValue([string]$Name) {
    $path=Join-Path $stateRoot ($Name+'.protected')
    if (-not (Test-Path -LiteralPath $path)) {
        $value='Wf!'+[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))+'a9'
        ConvertTo-SecureString $value -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath $path
    }
    return [Net.NetworkCredential]::new('',((Get-Content $path -Raw).Trim() | ConvertTo-SecureString)).Password
}
function PutResource([string]$Id,$Body) {
    $file=Join-Path $stateRoot ('request-'+[Guid]::NewGuid().ToString('N')+'.private.json')
    try {
        $Body | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath $file -Encoding utf8
        return Az @('rest','--method','put','--url',("https://management.azure.com${Id}?api-version=2025-07-01"),'--body',('@'+$file))
    } finally { Remove-Item -LiteralPath $file -ErrorAction SilentlyContinue }
}
function EnvValue([string]$Name,[string]$Value) { return @{name=$Name;value=$Value} }
function EnvSecret([string]$Name,[string]$Secret) { return @{name=$Name;secretRef=$Secret} }
function Probe([string]$Type,[int]$Port) { return @{type=$Type;tcpSocket=@{port=$Port};initialDelaySeconds=5;periodSeconds=10;timeoutSeconds=3;failureThreshold=if($Type -eq 'Startup'){120}else{3}} }
$rg=$state.resourceGroup
$base="/subscriptions/$($state.subscriptionId)/resourceGroups/$rg/providers/Microsoft.App"
$identity=@{type='UserAssigned';userAssignedIdentities=@{($state.pullIdentity)=@{}}}
$registry=@{server=$state.registryServer;identity=$state.pullIdentity}
if ($Stage -eq 'Storage') {
    $keys=Az @('storage','account','keys','list','-g',$rg,'-n',$state.storageAccount)
    $env:AZURE_STORAGE_ACCOUNT=$state.storageAccount
    $env:AZURE_STORAGE_KEY=$keys[0].value
    try {
        foreach ($share in @('appdata','transfer','signatures')) {
            Az @('storage','share','create','--name',$share,'--quota','5') | Out-Null
            Az @('containerapp','env','storage','set','-g',$rg,'-n',$state.environment,'--storage-name',$share,'--azure-file-account-name',$state.storageAccount,'--azure-file-account-key',$env:AZURE_STORAGE_KEY,'--azure-file-share-name',$share,'--access-mode','ReadWrite') | Out-Null
        }
        Az @('storage','directory','create','--share-name','appdata','--name','Documents') | Out-Null
        Az @('storage','file','upload','--share-name','transfer','--source',(Join-Path $SnapshotDirectory 'AzurePrepared.dump'),'--path','AzurePrepared.dump') | Out-Null
        Az @('storage','file','upload','--share-name','transfer','--source',(Join-Path $SnapshotDirectory 'import-manifest.json'),'--path','import-manifest.json') | Out-Null
        $manifest=Get-Content (Join-Path $SnapshotDirectory 'import-manifest.json') -Raw | ConvertFrom-Json
        foreach ($entry in $manifest.evidence) {
            if ($entry.file -notmatch '^[a-f0-9]{32}\.(pdf|png|jpg|jpeg|doc|docx)$') { throw 'Invalid evidence storage key' }
            $file=Join-Path (Join-Path $SnapshotDirectory 'Documents') $entry.file
            if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.sha256) { throw 'Evidence checksum mismatch' }
            Az @('storage','file','upload','--share-name','appdata','--source',$file,'--path',('Documents/'+$entry.file)) | Out-Null
        }
    } finally { Remove-Item Env:AZURE_STORAGE_KEY -ErrorAction SilentlyContinue; Remove-Item Env:AZURE_STORAGE_ACCOUNT -ErrorAction SilentlyContinue }
    Write-Output 'Private shares and verified presentation import files prepared.'
    exit
}
$appPassword=PrivateValue 'postgres-app'
if ($Stage -in @('Import','Export')) {
    if ($Stage -eq 'Export' -and ($SnapshotId -notmatch '^[a-f0-9]{32}$' -or $env:WELFARE_AZURE_WRITES_QUIESCED -ne 'true')) { throw 'Use the coordinated refresh script to pause writes before export.' }
    $jobName=if($Stage -eq 'Import'){'welfare-presentation-import'}else{'welfare-presentation-export'}
    $job=@{location=$state.location;identity=$identity;properties=@{
        environmentId=$state.environmentId
        configuration=@{triggerType='Manual';replicaTimeout=1200;replicaRetryLimit=0;manualTriggerConfig=@{parallelism=1;replicaCompletionCount=1};registries=@($registry);secrets=@(@{name='pg-admin';value=(PrivateValue 'postgres-admin')},@{name='pg-app';value=$appPassword})}
        template=@{containers=@(@{name='transfer';image="$($state.registryServer)/welfare-transfer:$ImageTag";resources=@{cpu=0.5;memory='1Gi'};env=@(
            (EnvValue 'MODE' $Stage.ToLowerInvariant()),(EnvValue 'SNAPSHOT_ID' $SnapshotId),(EnvValue 'WRITES_QUIESCED' $env:WELFARE_AZURE_WRITES_QUIESCED),(EnvValue 'PGHOST' $state.databaseHost),(EnvValue 'PGPORT' '5432'),(EnvValue 'PGDATABASE' 'WelfareDb'),(EnvValue 'PGUSER' $state.databaseAdministrator),(EnvSecret 'PGPASSWORD' 'pg-admin'),(EnvSecret 'WELFARE_APP_PASSWORD' 'pg-app')
        );volumeMounts=@(@{volumeName='transfer';mountPath='/transfer'},@{volumeName='appdata';mountPath='/appdata'})});volumes=@(@{name='transfer';storageName='transfer';storageType='AzureFile'},@{name='appdata';storageName='appdata';storageType='AzureFile'})}
    }}
    $jobId="$base/jobs/$jobName"
    PutResource $jobId $job | Out-Null
    $execution=Az @('containerapp','job','start','-g',$rg,'-n',$jobName)
    $execution | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $stateRoot ($Stage.ToLowerInvariant()+'-execution.json'))
    Write-Output "Private $Stage job started; execution success must be verified."
    exit
}
# The operator must first verify the import job; this does not overwrite a database.
$history=Az @('containerapp','job','execution','list','-g',$rg,'-n','welfare-presentation-import')
if (-not ($history | Where-Object { $_.properties.status -eq 'Succeeded' })) { throw 'No successful import execution found.' }
$appName='welfare-presentation'
$origin="https://$appName.$($state.defaultDomain)"
$connection="Host=$($state.databaseHost);Port=5432;Database=WelfareDb;Username=welfare_app;Password=$appPassword;SSL Mode=VerifyFull"
$app=@{location=$state.location;identity=$identity;properties=@{
    environmentId=$state.environmentId;workloadProfileName='Consumption'
    configuration=@{activeRevisionsMode='Single';registries=@($registry);secrets=@(@{name='database';value=$connection},@{name='jwt';value=(PrivateValue 'jwt')});ingress=@{external=$true;targetPort=8080;transport='http';allowInsecure=$false}}
    template=@{scale=@{minReplicas=0;maxReplicas=1};containers=@(
        @{name='gateway';image="$($state.registryServer)/welfare-gateway:$ImageTag";resources=@{cpu=0.25;memory='0.5Gi'};probes=@((Probe 'Startup' 8080),(Probe 'Readiness' 8080))},
        @{name='frontend';image="$($state.registryServer)/welfare-frontend:$ImageTag";resources=@{cpu=0.25;memory='0.5Gi'};probes=@((Probe 'Startup' 3000),(Probe 'Readiness' 3000))},
        @{name='api';image="$($state.registryServer)/welfare-api:$ImageTag";resources=@{cpu=0.5;memory='1Gi'};env=@(
            (EnvValue 'ASPNETCORE_HTTP_PORTS' '8081'),(EnvValue 'ASPNETCORE_ENVIRONMENT' 'Production'),(EnvSecret 'ConnectionStrings__DefaultConnection' 'database'),(EnvSecret 'Jwt__SecretKey' 'jwt'),(EnvValue 'Frontend__BaseUrl' $origin),(EnvValue 'Authentication__BackendBaseUrl' $origin),(EnvValue 'Documents__ScannerHost' '127.0.0.1'),(EnvValue 'Documents__ScannerPort' '3310')
        );probes=@((Probe 'Startup' 8081),@{type='Readiness';httpGet=@{path='/health/live';port=8081;httpHeaders=@(@{name='X-Forwarded-Proto';value='https'})};periodSeconds=10;timeoutSeconds=5;failureThreshold=3});volumeMounts=@(@{volumeName='appdata';mountPath='/app/App_Data'})},
        @{name='scanner';image='clamav/clamav:stable';resources=@{cpu=2;memory='4Gi'};probes=@((Probe 'Startup' 3310),(Probe 'Readiness' 3310));volumeMounts=@(@{volumeName='signatures';mountPath='/var/lib/clamav'})}
    );volumes=@(@{name='appdata';storageName='appdata';storageType='AzureFile'},@{name='signatures';storageName='signatures';storageType='AzureFile'})}
}}
PutResource "$base/containerApps/$appName" $app | Out-Null
$state.schemaImported=$true; $state.documentsImported=$true; $state.applicationDeployed=$true
$state | Add-Member -NotePropertyName publicOrigin -NotePropertyValue $origin -Force
$state | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $stateRoot 'infrastructure.json')
Write-Output "Application configuration submitted: $origin. HTTP and role checks still required."
