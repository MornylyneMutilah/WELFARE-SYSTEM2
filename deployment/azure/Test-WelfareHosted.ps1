param(
    [string]$StateDirectory='logs/azure-deployment',
    [string]$CredentialFile='backups/azure-presentation-initial/Hosted-demo-logins.private.json'
)
$ErrorActionPreference='Stop'
$state=Get-Content (Join-Path $StateDirectory 'infrastructure.json') -Raw | ConvertFrom-Json
$origin=$state.publicOrigin
if (-not $origin -or ([Uri]$origin).Scheme -ne 'https') { throw 'A verified HTTPS deployment origin is required.' }
$accounts=Get-Content $CredentialFile -Raw | ConvertFrom-Json
$report=[Collections.Generic.List[object]]::new()
function Check([bool]$Passed,[string]$Description) {
    $report.Add(@{check=$Description;passed=$Passed})
    if (-not $Passed) { throw "Hosted check failed: $Description" }
    Write-Output "PASS: $Description"
}
Check ((Invoke-RestMethod "$origin/health/live").status -eq 'live') 'Public HTTPS API liveness'
Check ((Invoke-RestMethod "$origin/health/ready").status -eq 'ready') 'Database and schema readiness'
foreach($page in @('/login','/register')) { Check ((Invoke-WebRequest ($origin+$page)).StatusCode -eq 200) "Frontend $page" }
$routes=@{Employee='employee';HROfficer='hr';CommitteeMember='committee';Management='management';Administrator='admin'}
$sessions=@{}
foreach($role in $routes.Keys) {
    $account=$accounts | Where-Object Role -eq $role | Select-Object -First 1
    if (-not $account) { throw "No private test credential supplied for $role" }
    $login=Invoke-RestMethod "$origin/api/auth/login" -Method Post -ContentType 'application/json' -Body (@{email=$account.Email;password=$account.Password}|ConvertTo-Json -Compress)
    Check ($login.user.role -eq $role -and -not [string]::IsNullOrWhiteSpace($login.token)) "$role server-verified login"
    $sessions[$role]=@{Authorization="Bearer $($login.token)"}
    $dashboard=Invoke-WebRequest "$origin/api/dashboards/$($routes[$role])" -Headers $sessions[$role]
    Check ($dashboard.StatusCode -eq 200) "$role dashboard data"
}
$denied=Invoke-WebRequest "$origin/api/users" -Headers $sessions.Employee -SkipHttpErrorCheck
Check ([int]$denied.StatusCode -eq 403) 'Employee denied administrator user management'
$denied=Invoke-WebRequest "$origin/api/dashboards/admin" -Headers $sessions.Employee -SkipHttpErrorCheck
Check ([int]$denied.StatusCode -eq 403) 'Employee denied administrator dashboard API'
$status=Invoke-RestMethod "$origin/api/operations/status" -Headers $sessions.Administrator
Check ($status.environment -eq 'Production') 'API running in Production mode'
Check ($status.scanner -like 'Configured:*') 'Private scanner configuration present (daemon still needs an upload check)'
$providers=Invoke-RestMethod "$origin/api/auth/external/providers"
$report.Add(@{check='Hosted provider configuration';providers=$providers})
$applications=Invoke-RestMethod "$origin/api/applications/all" -Headers $sessions.Administrator
$downloaded=0
foreach($application in $applications) {
    $documents=Invoke-RestMethod "$origin/api/documents/application/$($application.id)" -Headers $sessions.Administrator
    foreach($document in $documents) {
        Check ([bool]$document.contentAvailable) 'Imported private evidence content available'
        $denied=Invoke-WebRequest "$origin/api/documents/$($document.id)/content" -SkipHttpErrorCheck
        Check ([int]$denied.StatusCode -eq 401) 'Anonymous evidence download denied'
        $download=Invoke-WebRequest "$origin/api/documents/$($document.id)/content" -Headers $sessions.Administrator
        Check ($download.StatusCode -eq 200 -and $download.RawContentLength -gt 0) 'Authorized evidence download'
        $downloaded++
    }
}
Check ($downloaded -ge 6) 'All six imported evidence files downloaded through authorization'
@{testedAtUtc=[DateTime]::UtcNow.ToString('O');origin=$origin;checks=$report;documentsDownloaded=$downloaded} | ConvertTo-Json -Depth 15 | Set-Content (Join-Path $StateDirectory 'hosted-checks.json')
Write-Output 'Hosted smoke checks completed. No tokens or passwords were written to the report.'
