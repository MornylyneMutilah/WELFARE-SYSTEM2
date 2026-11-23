param(
    [Parameter(Mandatory=$true)][string]$AzureCli,
    [string]$ResourceGroup = 'rg-welfare-demo',
    [string]$Location = 'uaenorth',
    [string]$Prefix = 'welfare-morny-demo',
    [string]$StateDirectory = 'logs/azure-deployment'
)
$ErrorActionPreference = 'Stop'
$AzureCli = (Resolve-Path -LiteralPath $AzureCli).Path
$stateRoot = [IO.Path]::GetFullPath($StateDirectory)
New-Item -ItemType Directory -Force -Path $stateRoot | Out-Null

function Invoke-Azure([string[]]$CliArgs) {
    & $AzureCli @CliArgs --only-show-errors --output none
    if ($LASTEXITCODE -ne 0) { throw "Azure command failed: $($CliArgs[0..1] -join ' ')" }
}
function Read-Azure([string[]]$CliArgs) {
    $result = & $AzureCli @CliArgs --only-show-errors --output json
    if ($LASTEXITCODE -ne 0) { throw "Azure read failed: $($CliArgs[0..1] -join ' ')" }
    return ($result -join "`n" | ConvertFrom-Json)
}
function Exists-Azure([string[]]$CliArgs) {
    & $AzureCli @CliArgs --only-show-errors --output none 2>$null
    return $LASTEXITCODE -eq 0
}
function Get-PrivateValue([string]$Name) {
    $file = Join-Path $stateRoot ($Name + '.protected')
    if (-not (Test-Path -LiteralPath $file)) {
        $value = 'Wf!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)) + 'a9'
        ConvertTo-SecureString $value -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath $file
    }
    $secure = (Get-Content -LiteralPath $file -Raw).Trim() | ConvertTo-SecureString
    return [Net.NetworkCredential]::new('', $secure).Password
}

$account = Read-Azure @('account','show')
if ($account.name -ne 'Azure for Students') { throw 'Select the intended Azure for Students subscription before provisioning.' }
$assignments = Read-Azure @('policy','assignment','list')
$regionPolicies = @($assignments | Where-Object { $_.parameters.listOfAllowedLocations })
foreach ($policy in $regionPolicies) {
    if ($Location -notin $policy.parameters.listOfAllowedLocations.value) { throw 'The selected region is outside the subscription policy.' }
}
$rg = Read-Azure @('group','show','-n',$ResourceGroup)
$suffix = $account.id.Replace('-','').Substring(0,8)
$vnet = "$Prefix-vnet"
$environment = "$Prefix-env"
$database = "$Prefix-$suffix"
$registry = "welfareacr$suffix"
$storage = "welfaredata$suffix"
$identity = "$Prefix-pull"
$dns = "$Prefix.private.postgres.database.azure.com"

if (-not (Exists-Azure @('network','vnet','show','-g',$ResourceGroup,'-n',$vnet))) {
    Invoke-Azure @('network','vnet','create','-g',$ResourceGroup,'-n',$vnet,'-l',$Location,'--address-prefixes','10.43.0.0/16')
}
foreach ($spec in @(
    @{Name='apps';Address='10.43.0.0/23';Delegation='Microsoft.App/environments'},
    @{Name='postgres';Address='10.43.2.0/28';Delegation='Microsoft.DBforPostgreSQL/flexibleServers'}
)) {
    if (-not (Exists-Azure @('network','vnet','subnet','show','-g',$ResourceGroup,'--vnet-name',$vnet,'-n',$spec.Name))) {
        Invoke-Azure @('network','vnet','subnet','create','-g',$ResourceGroup,'--vnet-name',$vnet,'-n',$spec.Name,'--address-prefixes',$spec.Address,'--delegations',$spec.Delegation)
    }
}
$network = Read-Azure @('network','vnet','show','-g',$ResourceGroup,'-n',$vnet)
if (-not (Exists-Azure @('network','private-dns','zone','show','-g',$ResourceGroup,'-n',$dns))) {
    Invoke-Azure @('network','private-dns','zone','create','-g',$ResourceGroup,'-n',$dns)
    Invoke-Azure @('network','private-dns','link','vnet','create','-g',$ResourceGroup,'-z',$dns,'-n','welfare-vnet-link','--virtual-network',$network.id,'--registration-enabled','false')
}
if (-not (Exists-Azure @('acr','show','-g',$ResourceGroup,'-n',$registry))) {
    Invoke-Azure @('acr','create','-g',$ResourceGroup,'-n',$registry,'-l',$Location,'--sku','Basic','--admin-enabled','false')
}
if (-not (Exists-Azure @('storage','account','show','-g',$ResourceGroup,'-n',$storage))) {
    Invoke-Azure @('storage','account','create','-g',$ResourceGroup,'-n',$storage,'-l',$Location,'--sku','Standard_LRS','--kind','StorageV2','--https-only','true','--min-tls-version','TLS1_2','--allow-blob-public-access','false')
}
if (-not (Exists-Azure @('identity','show','-g',$ResourceGroup,'-n',$identity))) {
    Invoke-Azure @('identity','create','-g',$ResourceGroup,'-n',$identity,'-l',$Location)
}
$pull = Read-Azure @('identity','show','-g',$ResourceGroup,'-n',$identity)
$acr = Read-Azure @('acr','show','-g',$ResourceGroup,'-n',$registry)
$roles = Read-Azure @('role','assignment','list','--scope',$acr.id,'--assignee-object-id',$pull.principalId,'--role','AcrPull')
if (-not $roles) {
    Invoke-Azure @('role','assignment','create','--scope',$acr.id,'--assignee-object-id',$pull.principalId,'--assignee-principal-type','ServicePrincipal','--role','AcrPull')
}
if (-not (Exists-Azure @('postgres','flexible-server','show','-g',$ResourceGroup,'-n',$database))) {
    $password = Get-PrivateValue 'postgres-admin'
    $zone = Read-Azure @('network','private-dns','zone','show','-g',$ResourceGroup,'-n',$dns)
    Invoke-Azure @('postgres','flexible-server','create','-g',$ResourceGroup,'-n',$database,'-l',$Location,'--sku-name','Standard_B1ms','--tier','Burstable','--storage-size','32','--version','18','--backup-retention','7','--geo-redundant-backup','Disabled','--zonal-resiliency','Disabled','--subnet',($network.id+'/subnets/postgres'),'--private-dns-zone',$zone.id,'--admin-user','welfare_admin','--admin-password',$password,'--yes')
    $password = $null
}
$server = Read-Azure @('postgres','flexible-server','show','-g',$ResourceGroup,'-n',$database)
if (-not (Exists-Azure @('containerapp','env','show','-g',$ResourceGroup,'-n',$environment))) {
    Invoke-Azure @('containerapp','env','create','-g',$ResourceGroup,'-n',$environment,'-l',$Location,'--enable-workload-profiles','true','--infrastructure-subnet-resource-id',($network.id+'/subnets/apps'),'--logs-destination','none')
}
Write-Output 'Container Apps environment is available.'
if (-not (Exists-Azure @('postgres','flexible-server','db','show','-g',$ResourceGroup,'-s',$database,'-d','WelfareDb'))) {
    Invoke-Azure @('postgres','flexible-server','db','create','-g',$ResourceGroup,'-s',$database,'-d','WelfareDb')
}
$envInfo = Read-Azure @('containerapp','env','show','-g',$ResourceGroup,'-n',$environment)
$state = [ordered]@{
    subscriptionId=$account.id;resourceGroup=$ResourceGroup;location=$Location
    environment=$environment;environmentId=$envInfo.id;defaultDomain=$envInfo.properties.defaultDomain
    registry=$registry;registryServer=$acr.loginServer;storageAccount=$storage
    pullIdentity=$pull.id;databaseServer=$database;databaseHost=$server.fullyQualifiedDomainName
    databaseName='WelfareDb';databaseAdministrator='welfare_admin';databasePublicAccess=$server.network.publicNetworkAccess
    schemaImported=$false;documentsImported=$false;applicationDeployed=$false
}
$state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $stateRoot 'infrastructure.json') -Encoding utf8
Write-Output 'Infrastructure created; database is private. Migration and application deployment are still required.'
