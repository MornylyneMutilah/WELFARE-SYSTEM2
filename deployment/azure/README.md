# Azure presentation deployment

These scripts deploy the existing application; they do not replace its login, approval workflow or original local database. Run from the repository root in PowerShell 7. Azure CLI must be signed into the intended Azure for Students subscription. Private state, CLI authentication and generated credentials belong under ignored `logs/` and `backups/` directories.

## Deployment order

1. Take a coordinated database/evidence backup with `scripts/Backup-Welfare.ps1` and verify an isolated restore with `scripts/Test-WelfareRestore.ps1`.
2. Restore that backup into a new `welfare_azure_prepare_<32 hex digits>` local staging database. Run `PreparePresentation` only against that isolated copy to replace inherited default demonstration passwords. Keep the generated login file private. Export `AzurePrepared.dump` and an `import-manifest.json` with its SHA-256, prepared record counts and evidence hashes.
3. Run `New-WelfareInfrastructure.ps1`. The selected region must be permitted by the subscription. The legacy B1ms size was rejected in UAE North and Switzerland North on 23 November 2026; the deployment therefore uses `-DatabaseSku Standard_B2ats_v2` in UAE North. This newer size consumes credit rather than the B1ms free compute allowance. Do not assume the free legacy size is available merely because it appears in the capabilities list.
4. Build the four images through the manual **Build Azure presentation images** GitHub workflow. Its managed identity has `AcrPush` only on this project's registry, with federation restricted to this repository's `master` branch. Its immutable-ID subject must match the actual GitHub OIDC subject. Runtime image pulls use a separate identity with `AcrPull` on that registry.
5. Run `Deploy-WelfareContainers.ps1 -Stage Storage`, then `-Stage Import`. Wait for the import job to succeed and verify its checksums/counts. Import refuses to replace an existing application schema.
6. Run `Deploy-WelfareContainers.ps1 -Stage Application`. Verify HTTPS, readiness, all five roles, the approval sequence, private evidence downloads and clean/malicious upload handling before calling the deployment complete.

Supply `-AzureCli <path-to-az.cmd>` to every script. `logs/azure-deployment/infrastructure.json` records resource identifiers and the public origin, without passwords. Secrets are generated locally, protected with Windows DPAPI, and passed as Container Apps secrets; never copy private JSON or `.protected` files to GitHub.

## Hosting arrangement

One public HTTPS origin routes through Caddy to Next.js and the API over loopback. The PostgreSQL server uses a delegated private subnet and private DNS, with public network access disabled. The API uses its own database role; import uses the separate provisioning administrator. TLS certificate validation is enabled for application database connections.

Private Azure Files shares retain evidence, queued mail, scanner signatures and transfer snapshots. No evidence share is served as a public website. ClamAV is private and production uploads fail closed if scanning is unavailable. The scanner needs substantial memory; the app allocates 4 GiB to it and scales the complete single-replica application to zero while idle. The first request after idle can be slow. Pre-warm the site before presenting.

The API enforces the existing database/schema/default-password startup safeguards. Only loopback forwarded protocol headers are trusted. Keep `allowInsecure: false`, the single public gateway and `maxReplicas: 1`; maintenance and pending OAuth state are currently process-local. Container readiness uses liveness so an administrator can still resume maintenance; `/health/ready` remains the operator check for database/schema/maintenance readiness.

## Refresh the laptop from Azure

Set `WELFARE_ADMIN_TOKEN` in the current terminal to a fresh **hosted** administrator access token. Do not paste it into chat or scripts. Run:

```powershell
./deployment/azure/Refresh-WelfareOffline.ps1 -AzureCli <path-to-az.cmd>
```

The coordinator checks database identity, pins a single replica, drains writes through administrator maintenance, runs a private snapshot job and downloads the database together with hashed evidence. It resumes hosted writes in `finally`. It restores into a new `welfare_offline_<id>` database, checks record counts and evidence, and only then selects that copy in an ignored local configuration file. Each snapshot and earlier local database is retained. If an export/restore fails, investigate the logs; an incomplete copy must not be used for presentation.

Stop any already running local application, then run:

```powershell
./deployment/azure/Start-WelfareOffline.ps1
```

The local app opens at `http://localhost:3000` with its API on port 5249. This Development-only offline mode uses installed local dependencies and permits the existing unscanned development upload behaviour. It must not be exposed publicly. Use the credentials current at the snapshot time. Offline changes are not merged into Azure. Refresh again while online before presenting; test the offline login and documents with the internet disconnected.

If the original local application is already running, keep it and choose unused ports:

```powershell
./deployment/azure/Start-WelfareOffline.ps1 -FrontendPort 3001 -ApiPort 5253
```

The completed 23 November 2026 recovery drill used these alternate ports. The Azure snapshot restored into a new local database, all five role logins/dashboard APIs passed, and all six evidence downloads succeeded. The original application remained available on port 3000. The offline frontend binds to loopback; this does not turn the Development API into an externally hardened service.

## Remaining configuration and cost controls

- Set real SMTP settings before relying on password resets, verification and account invitations. Production deliberately has no development-outbox fallback.
- Google/GitHub require exact hosted HTTPS callbacks and server-side provider credentials. The deployment does not copy local OAuth secrets automatically. Provider enablement is a separate configuration/verification step.
- Student credit is finite. Registry, persistent storage, PostgreSQL and usage beyond Container Apps allowances can incur charges. PostgreSQL compute continues while the server is running, even if the web app scales to zero. Stop the database between presentation sessions when appropriate; Azure can restart stopped servers after its maximum stop period. Budget alerts do not impose a hard spending cap.
- Keep the original source backup and dated Azure snapshots private. A successful build is not evidence of a successful import, hosted role walkthrough or recovery drill; record each separately.
