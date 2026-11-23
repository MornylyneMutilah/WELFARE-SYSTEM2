# Welfare Management System

A web application for employee welfare administration, built with **Next.js and TypeScript**, an **ASP.NET Core .NET 10 API**, and **PostgreSQL**. Its central capabilities are configurable welfare rules, a controlled review workflow, and role-scoped record exploration.

The application runs locally and as an Azure presentation deployment, supporting five roles: Employee, HR Officer, Welfare Committee Member, Institutional Management, and Administrator. The Azure API runs in Production mode with private PostgreSQL and malware scanning. Institutional acceptance and production capacity are not claimed.

**Presentation site:** [Open the Welfare Portal](https://welfare-presentation.thankfulforest-64e9f250.uaenorth.azurecontainerapps.io/login). It uses dummy data and restricted accounts. Idle scale-to-zero can delay the first request; open the site before presenting. See the [Azure deployment and offline recovery runbook](deployment/azure/README.md).

## What the system does

### Applications and decisions

**Employee submission → departmental HR review → committee endorsement → management decision.**

- Employees prepare drafts, upload required evidence, review current eligibility checks, explicitly confirm submission and track outcomes.
- HR reviews its department's applications. Committee members consider HR-forwarded applications; management records permitted final decisions.
- Backend checks enforce current roles, ownership, department scope, stage order, and restrictions on self-review and repeated decisions.
- Decision registers retain completed reviews, endorsements and approvals after applications leave pending queues. New confirmations include decision-time snapshots and evidence manifests.
- Independent appeals and correction addenda retain the original decision. An accepted appeal creates a reconsideration draft; it does not automatically award support.

### Dynamic rules and eligibility

Administrators maintain program limits, minimum service months, evidence categories and active status without changing the comparison code. Supported rule types are predefined; this is not an arbitrary expression or visual rule builder.

Submission requires an active account/program, completion of any required initial password change, an administrator-verified employment date, sufficient completed service, a positive amount within the current limit, and stored evidence in every required category.

Draft pages show **Current eligibility checks**, with a **Refresh checks** action and automatic refresh on focus, relevant changes and ten-second polling. The new-application form also refreshes active programs. Raising a limit from 2,000 to 5,000 clears the current amount failure for a 3,000 draft without recreating it. Other mandatory requirements must still pass. Historical assessments remain separately labelled and submitted snapshots are never silently rewritten.

### Ad-hoc exploration and audit

- Combine supported search, stage, outcome, actor and date filters in reports, decision registers and audit views.
- Open individual records and their timelines, inspect saved decisions, export matching CSV results and print supported views.
- Exports and record reads enforce backend permissions. CSV output mitigates spreadsheet formula injection.
- Administrator audit filters accept exact actions such as `ProgramUpdated` and entity types such as `WelfareProgram`.

These tools provide structured exploration, not unrestricted SQL, a general report designer or a separate enterprise BI deployment.

### Accounts, evidence and administration

- Three-step email registration with review and explicit confirmation. Public accounts receive Employee access; privileged roles require trusted approval or provisioning.
- BCrypt passwords, signed JWT access/refresh tokens, and server-side account/role/session-version checks.
- **Tab-specific sign-in:** Administrator and Employee sessions can coexist in different tabs. Refresh preserves each tab's session; logging out in one tab does not sign out the others. A new tab may require sign-in, and closing a tab normally ends that tab's session.
- Administrator account creation, role/department/detail changes, verified employment dates, suspension, reactivation and guarded role requests. Suspension and authority changes invalidate affected sessions.
- Expiring, single-use recovery, verification and setup-invitation links. Initial administrator-provisioned passwords must be changed.
- Private evidence files, named categories, checksums, quotas, download authorization and ClamAV integration. Consecutive uploads remain available after a blocked submission; feedback scrolls into view.
- Administrator-proposed annual budgets with independent management decisions. Figures track allocations and approved commitments, **not payments or bank transfers**.
- Shared red design tokens and components, responsive role navigation, keyboard focus, reduced-motion controls and optional 2D/3D welfare illustrations.

### Anomaly support

Submission can flag repeated applications, unusually frequent submissions, amounts above a historical program baseline, related claims across programs and repeated evidence content. Configurable windows and thresholds are maintained through administrator detection controls. Flags support investigation; they neither prove fraud nor automatically decide awards.

The optional nearest-neighbour amount model is disabled by default, needs at least 50 prior approvals per program, and has synthetic tests rather than validated institutional accuracy. A conversational AI assistant is not implemented.

## Technology and layout

- Frontend: Next.js 16.4.6, React 19, TypeScript 5, Tailwind CSS 4 and Axios.
- Backend: ASP.NET Core Web API on .NET 10, EF Core 10, Npgsql, BCrypt and JWT bearer authentication.
- Database: PostgreSQL; the local checks used PostgreSQL 18 and additive EF migrations.

```text
backend/
  WelfareSystem.Api/                 Controllers, services, repositories, entities, migrations
  WelfareSystem.RegressionChecks/    Database-backed checks with fixture rollback
frontend/
  app/                              Authentication, dashboards, forms and administration
  components/                       Shared fields, feedback, evidence and layout
  lib/                              API clients, tab sessions and live-data hooks
  tests/                            Session-isolation and HTTP-client regression tests
docs/                               Maintained technical and operating documentation
scripts/                            Coordinated backup and isolated restore verification
start-backend.ps1
start-frontend.ps1
```

The frontend uses the existing Webpack configuration for development and builds. Controllers expose HTTP operations; services implement business rules; EF Core and repositories access PostgreSQL. UI visibility is not the authorization boundary.

## Run locally

Prerequisites: the .NET 10 SDK, npm with Node.js 20.19+, 22.13+ or 24+, PostgreSQL, and Git. This matches the current lint-tool engine requirements; verification used Node.js 24.22.1. Use a development database whose schema has been migrated.

### 1. Clone and restore

```powershell
git clone https://github.com/MornylyneMutilah/WELFARE-SYSTEM2.git
cd WELFARE-SYSTEM2
dotnet restore backend/WelfareSystem.Api
cd frontend
npm ci
cd ..
```

### 2. Configure and start the API

Create `WelfareDb` using your PostgreSQL administration tool. Set private values in the terminal used for migrations and API startup:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=WelfareDb;Username=postgres;Password=YOUR_DATABASE_PASSWORD"
$env:Jwt__SecretKey = "YOUR_RANDOM_SIGNING_KEY_OF_AT_LEAST_32_CHARACTERS"
dotnet tool install --global dotnet-ef --version 10.0.11
dotnet ef database update --project backend/WelfareSystem.Api
dotnet run --project backend/WelfareSystem.Api --launch-profile http
```

If the EF tool is already installed, check that it targets EF Core 10 rather than reinstalling it. Development also loads the ignored `backend/WelfareSystem.Api/appsettings.Local.json`; environment variables take precedence. Never put real passwords or signing keys in tracked files.

The API runs at **http://localhost:5249**. Development Swagger is at `/swagger`; `/health/live` checks process liveness and `/health/ready` checks database/schema/maintenance readiness. Development seeds demonstration records only when departments are absent. Existing records are preserved; do not use a reset script on a database you need to keep.

### 3. Start the frontend

Create the ignored `frontend/.env.local`:

```dotenv
NEXT_PUBLIC_API_URL=http://localhost:5249/api
```

In a second terminal:

```powershell
cd frontend
npm run dev
```

Open **http://localhost:3000/login**. Use `localhost` consistently for the browser and configured OAuth origins. On Windows, `start-backend.ps1` and `start-frontend.ps1` are convenience development launchers.

For a production-build frontend locally, run `npm run build` followed by `npm start`. This does not turn the development API into a production deployment. The public API URL is embedded at frontend build time.

## Demonstrate the workflow

Fresh development seed accounts use `Password123!`: `admin@welfare.edu`, `hr1@welfare.edu`, `committee1@welfare.edu`, `management@welfare.edu` and `employee1@welfare.edu`. These are public development fixtures, not production credentials. Existing databases may have changed accounts or passwords.

1. As Administrator, verify the employee's genuine hire date, program settings and required document categories.
2. In a separate tab, sign in as Employee. Create a draft, upload every required document and review the current checks.
3. To demonstrate dynamic rules, change the limit in the Administrator tab and return to the Employee tab. Use **Refresh checks** or allow the live refresh to complete.
4. Submit a valid draft. Review it through HR in the same department, then committee, then management.
5. Inspect the employee status, decision register, date-filtered reports and administrator audit events. Export or print a matching record.

Only server-confirmed actions display success. Submitted evidence remains locked and later policy changes do not rewrite historical decisions.

## Verification

The latest recorded verification is documented in [Verification and evidence](docs/VERIFICATION.md): **279 backend regression checks**, **6 frontend session tests**, ESLint, backend compilation and a production frontend build. Formal institutional UAT, independent penetration testing and production load certification are not claimed.

```powershell
# Repository root
dotnet build backend/WelfareSystem.Api
dotnet run --project backend/WelfareSystem.RegressionChecks -- backend/WelfareSystem.Api/appsettings.Local.json

# Frontend directory
cd frontend
npm run test:auth
npm run lint
npm run build
```

For regression checks, the ignored local JSON file must contain your development `ConnectionStrings:DefaultConnection`. The runner requires the current migrated schema, rolls back fixture records and removes temporary evidence. PostgreSQL identity sequences can advance. On Windows, stop a running API before rebuilding its locked binaries.

The dated [15 November timing dataset](docs/verification/performance-2026-11-15.json) contains 30 measured runs per operation after three warm-ups. It measured a small local sequential workload before later session/live-preview fixes; it is not a benchmark of the latest revision or a concurrent-load result.

## Remaining hosted configuration

- Google and GitHub integrations require private provider credentials and matching callbacks. Unconfigured buttons stay disabled. X is hidden from the interface; inherited backend support remains. Local authorization redirects were checked, but complete live linking/sign-in remains to be verified. See [social sign-in](docs/SOCIAL_SIGN_IN.md).
- Recovery, verification and invitations use a private development outbox without SMTP. Real email delivery must be configured and tested.
- The Azure presentation deployment uses private persistent storage and an operational ClamAV scanner. Two consecutive clean uploads and rejection of a canonical harmless EICAR archive were checked on 23 November 2026. Development evidence can still be explicitly unscanned when no local daemon is configured; Production uploads fail closed without scanning.
- Use private production secrets, HTTPS, explicit origins, migrated schema, protected database/evidence/mail volumes, verified backups and changed or disabled demonstration credentials.
- OAuth pending-flow state and maintenance coordination currently assume one API instance. Shared state is required before adding replicas.
- Targeted HR-assignment notifications and a complete additional-information-request workflow remain incomplete. Administrator dashboard service labels are partly static; use Readiness and health endpoints for actual checks.
- Dashboards use polling and browser events, not server push. Institutional usability, broad accessibility, sustained load and real-world anomaly effectiveness require further evaluation.

## Documentation

- [User guide](docs/USER_GUIDE.md) — role tasks, live rules, audit filters and common errors.
- [Operating guide](docs/OPERATING_GUIDE.md) — employment, recovery, scanning, budgets and backups.
- [Administrator users](docs/ADMIN_USER_MANAGEMENT.md) — provisioning, suspension and role safeguards.
- [API reference](docs/API.md) — current route groups and authorization boundaries.
- [Architecture](docs/DESIGN.md) and [database](docs/DATABASE.md) — implemented components and persistence.
- [Requirements and scope](docs/REQUIREMENTS.md) — implemented requirements and explicit limits.
- [Evidence and decisions](docs/EVIDENCE_AND_DECISION_RECORDS.md) — files, snapshots and exports.
- [Deployment](docs/DEPLOYMENT.md) — build, migration, health and recovery steps.
- [Social sign-in](docs/SOCIAL_SIGN_IN.md) — server configuration and exact callbacks.
- [Verification](docs/VERIFICATION.md) — executed checks, repeatable commands and dated evidence.

Superseded planning/status notes have been removed from the current source tree. Academic Word reports, presentation files, private logs and credentials remain local and are excluded from Git. Git history still retains earlier committed documentation.
