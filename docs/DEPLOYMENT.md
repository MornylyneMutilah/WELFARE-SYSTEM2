# Deployment and release notes

The current verified environment is local. This repository does not claim a public production deployment, institutional pilot, formal UAT or certified concurrent-user capacity.

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

## Verify the deployment

1. Confirm the exact tested source commit and build/configuration used by the processes.
2. Check the login page, `GET /health/live` and `GET /health/ready`.
3. Verify an employee draft, evidence upload and current eligibility preview. Confirm that an invalid amount is blocked and a corrected current limit is reflected without replacing history.
4. Exercise authorized HR → committee → management decisions, employee tracking, decision retrieval and matching exports.
5. Check suspension/revocation, SMTP inbox delivery, scanner health, storage permissions and a database/evidence restoration drill.

Readiness checks database/schema/maintenance conditions. It does not certify email delivery, scanner-definition currency or backup recoverability.

## Release and recovery

The existing GitHub source branch is `master`; use the remote branch actually reviewed and tested. If the academic/release process requires `main`, create and review that release source explicitly and configure hosting accordingly; do not assume a branch rename or automatic deployment has occurred.

Record a final commit or tag, test completed changes before publishing, and verify the running build matches that identifier. Local startup is manual; no automatic hosting integration is configured by these instructions.

Retain a previously verified build/configuration and coordinated database/evidence backup. Restore only through an approved recovery procedure with integrity and access checks. Never point an isolated restore test at the live source database. The provided scripts create a uniquely named test database and can remove only that test target.

OAuth pending-flow state and maintenance coordination currently target one API instance. Shared atomic state, shared private files and suitable maintenance coordination are prerequisites for multiple replicas.
