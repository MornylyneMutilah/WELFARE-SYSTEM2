# Deployment and release notes

Local verification is supplemented by an Azure presentation deployment checked on 23 November 2026. Hosted HTTPS pages, all five role logins/dashboard APIs, employee denial from administrator APIs, and authorized access to all six imported evidence files passed. Production ClamAV accepted consecutive clean uploads and rejected a canonical harmless EICAR archive. Institutional pilot, formal UAT and certified concurrent-user capacity are not claimed.

A coordinated hosted snapshot was downloaded, hash-verified and restored into a new local database. All five offline role logins/dashboard APIs, the restored database identity, frontend CORS and six offline evidence downloads passed. The original local database and application were preserved. The first export attempt exposed an Azure Files timestamp-copy restriction; the corrected transfer image copies content and verifies hashes without trying to preserve unsupported POSIX metadata.

## Build and run

Use the .NET 10 SDK to build the API and a .NET 10 runtime for a published API. Use Node.js 20.19+, 22.13+ or 24+ with npm for the application/tooling, PostgreSQL, private persistent evidence/mail storage and a suitable HTTPS/reverse-proxy arrangement for external hosting. The development notebook is not a justified minimum server specification.

```powershell
# From repository root, with private environment values already configured
dotnet restore backend/WelfareSystem.Api
dotnet ef database update --project backend/WelfareSystem.Api
dotnet publish backend/WelfareSystem.Api -c Release -o ./out/api
cd frontend
npm ci
npm run test:auth
npm run lint
npm run build
npm start
```

Stop a running Windows API before rebuilding locked binaries. Publish output is generated and ignored by Git. A frontend production build can be demonstrated locally alongside a Development API; that combination is not a hardened production deployment.

## Private server settings

- `ConnectionStrings__DefaultConnection`: restricted PostgreSQL connection.
- `Jwt__SecretKey`: private random signing secret. Production startup explicitly requires this environment setting and at least 32 bytes.
- `ASPNETCORE_ENVIRONMENT=Production` for an actual production API.
- `Frontend__BaseUrl` and `Authentication__BackendBaseUrl`: explicit HTTPS origins.
- `Documents__StoragePath`: private persistent volume, never a public web directory.
- `Documents__ScannerHost` / `Documents__ScannerPort`: private operational scanner.
- `Mail__Host`, `Mail__Port`, `Mail__Username`, `Mail__Password`, `Mail__From`: real mail delivery, as described in the operating guide.
- `Authentication__Google__*` and `Authentication__GitHub__*`: optional provider configuration, server-side only.

`appsettings.Local.json` is ignored and loaded only in Development. Environment variables override development JSON. Do not assume a private local file is read in Production. Keep signing/database/provider/mail credentials out of source, reports, client variables and logs.

The browser setting `NEXT_PUBLIC_API_URL` is public and embedded during frontend build. Rebuild when changing it. Configure the API's permitted frontend origin consistently; OAuth browser cookies require compatible same-site deployment and exact callbacks. Review trusted reverse-proxy handling before external hosting.

Production startup requires a migrated schema and rejects active default demonstration passwords. Replace or disable those accounts before exposure. Production uploads fail closed without configured scanning; production account delivery has no development-outbox fallback.

## Portable container builds

Container definitions are supplied for each application, with the build context set to that application's directory:

```sh
docker build -t welfare-api ./backend/WelfareSystem.Api
docker build --build-arg NEXT_PUBLIC_API_URL=https://api.example.org/api -t welfare-frontend ./frontend
```

Replace the example API address with the real HTTPS origin before building the frontend. The frontend image uses Node.js 24 and Next.js standalone output; ordinary local builds continue to support `npm start`. The API image uses the .NET 10 runtime, Production mode and port 8080. Both runtime images run as an unprivileged user. Private local settings, local evidence and environment files are excluded from their build contexts by the application-specific `.dockerignore` files.

The images alone do not provision a database or constitute a complete deployment. Set the API secrets listed above through the hosting provider. Attach a private persistent volume at `/app/App_Data` with write permission for the API container user (UID 1654), preserving evidence and queued mail together. Provision PostgreSQL and a private ClamAV service, and apply migrations to the intended hosting database before API startup. Keep one API replica until pending OAuth flows, maintenance coordination and storage support multiple instances.

Configure HTTPS termination and trusted forwarding for the selected host before public exposure; forwarded headers must not be trusted from arbitrary internet clients. Frontend and API origins must also satisfy the OAuth cookie same-site requirements. Configure exact Google/GitHub HTTPS callback URLs in the provider applications after the public origin is known.

A fresh Production database is not seeded with demonstration accounts. Administrator provisioning or a controlled migration of the intended accounts must be planned before launch. Existing local records and evidence must not be copied to a public host without deciding which demonstration data should be included. Never deploy the ignored local configuration file or reuse the development demonstration password.

Hosting account, domain/origins, data migration, proxy configuration, scanner and email delivery must be resolved before the hosted end-to-end workflow can be verified. Container builds require Docker; do not represent a local Next.js or .NET build as a tested container build.

## Azure presentation configuration

See [the Azure deployment runbook](../deployment/azure/README.md) for the current scripts, build workflow, private migration and Azure-to-laptop refresh procedure. The legacy free B1ms size was rejected in two permitted regions during provisioning; the selected newer `Standard_B2ats_v2` size consumes student credit. Verify actual resource state and the hosted smoke checks before declaring the site ready.

The presentation database uses 32 GiB storage, seven-day local backup retention and no high-availability standby. PostgreSQL, registry, persistent storage and application usage can consume student credit. This is a demonstration sizing choice, not a capacity guarantee for institutional production.

The proposed Container Apps deployment groups the frontend, API, private ClamAV scanner and a small Caddy gateway in a single replica. Only gateway port 8080 is exposed through HTTPS-enforced Azure ingress. The frontend uses port 3000, the API is overridden to port 8081, and the scanner uses port 3310. The gateway definition is in `deployment/azure/gateway`. Using a single public origin for `/api` and the frontend preserves browser-bound social-authentication cookies; build the frontend with `NEXT_PUBLIC_API_URL=/api` and configure both API public-origin settings to that same HTTPS origin.

The API processes forwarded HTTPS information only from the framework's default trusted loopback proxies. The gateway supplies the HTTPS scheme after Azure ingress enforces it. Do not expose the API port directly or enable unrestricted forwarded-header trust. The gateway configuration assumes the containers share one Container Apps replica; it is not a configuration for separate machines.

Mount private persistent Azure Files storage for API documents and pending mail. Configure startup/readiness probes for all containers, including the scanner; downloading scanner definitions can delay cold starts. Scale-to-zero reduces unused compute, but prepare and test the app before a presentation and retain only one replica because OAuth flow state is in memory. Registry, file operations, logs and compute beyond free allowances may still consume student credit.

Azure becomes the primary database only after a verified migration. The controlled Azure-to-laptop refresh retains dated snapshots, restores into a separate local database, verifies record and file integrity, and switches the offline application only after verification. The original local database and earlier snapshots remain available. Offline changes are not automatically merged back. The scripts require a successful hosted recovery drill before relying on this process.

## Verify the deployment

1. Confirm the exact tested source commit and build/configuration used by the processes.
2. Check the login page, `GET /health/live` and `GET /health/ready`.
3. Verify an employee draft, evidence upload and current eligibility preview. Confirm that an invalid amount is blocked and a corrected current limit is reflected without replacing history.
4. Exercise authorized HR → committee → management decisions, employee tracking, decision retrieval and matching exports.
5. Check suspension/revocation, SMTP inbox delivery, scanner health, storage permissions and a database/evidence restoration drill.

Readiness checks database/schema/maintenance conditions. It does not certify email delivery, scanner-definition currency or backup recoverability.

## Release and recovery

The existing GitHub source branch is `master`; use the remote branch actually reviewed and tested. If the academic/release process requires `main`, create and review that release source explicitly and configure hosting accordingly; do not assume a branch rename or automatic deployment has occurred.

Record a final commit or tag, test completed changes before publishing, and verify the running build matches that identifier. The Azure image build workflow is manually dispatched; source pushes do not automatically replace the running site. Local startup remains manual.

Retain a previously verified build/configuration and coordinated database/evidence backup. Restore only through an approved recovery procedure with integrity and access checks. Never point an isolated restore test at the live source database. The provided scripts create a uniquely named test database and can remove only that test target.

OAuth pending-flow state and maintenance coordination currently target one API instance. Shared atomic state, shared private files and suitable maintenance coordination are prerequisites for multiple replicas.
