# Welfare Management System

A role-based web application for employee welfare administration, built with Next.js, ASP.NET Core, and PostgreSQL. The platform supports application tracking, staged approvals, account administration, rule-based eligibility assessment, and anomaly detection.

**Current status — 2 November 2026:** functional local development platform with employee, HR, welfare committee, management, and administrator dashboards. The latest verified revision passes frontend lint, a production frontend build, backend compilation, and 94 database-backed regression checks. See [Known limitations](#known-limitations) before planning a deployment.

## Core capabilities

### Registration and account access

- Three-step email registration: personal details, requested account type, and review with explicit confirmation.
- Public registration creates an Employee account. Organizational roles require administrator approval.
- Email/password authentication with BCrypt hashing and JWT access/refresh tokens.
- Role-based navigation backed by API authorization.
- Account suspension blocks subsequent authenticated requests and new sign-ins. Reactivation requires a fresh login; previously invalidated sessions remain invalid.

### Welfare applications and approvals

- Browse active welfare programs and create application drafts.
- Submit drafts from **My applications** to enter the review workflow.
- HR reviews applications within its department, records notes, and approves or rejects them.
- HR approval forwards the application to the Welfare Committee.
- Committee endorsement forwards the application to Management for a final decision.
- Review forms show application details, prior reviews, recorded supporting-document metadata, and anomaly flags.
- Backend checks enforce stage order, department scope, and review permissions, and prevent self-review and duplicate stage decisions.
- Decisions record review history, status history, audit events, and employee notifications.

The normal approval sequence is:

**Employee submits → HR approves → Committee endorses → Management gives final approval.**

Rejection at an authorized stage stops the application. Committee endorsement is not final approval.

### Dashboards and updates

- Employees track their own pending, approved, and rejected applications.
- HR sees its department's review queue and anomalies.
- Committee members see applications forwarded by HR and their recorded endorsements/rejections.
- Management sees pending final approvals, approved amounts, and processing-time metrics.
- Administrators see platform totals and pending applications across all review stages.
- Visible dashboards refresh after successful writes, on tab focus, and every 10 seconds. Updates also propagate between tabs through browser storage events.

### Administrator user management

- Create accounts; edit names, email addresses, employee IDs, roles, departments, and hire dates.
- Suspend and reactivate accounts while preserving applications and historical records.
- Search users and filter by role or account status.
- Require a change reason and confirmation, with administrator attribution and before/after audit records.
- Reject duplicate account details, invalid roles/departments, future hire dates, and conflicting edits.
- Prevent administrator self-suspension, self-demotion, and removal of the last active administrator.
- Invalidate existing sessions after role, department, or account-status changes.

See [Administrator user management](docs/ADMIN_USER_MANAGEMENT.md) for operating instructions and safeguards.

## Decision support

This is a **rule-based decision-support system**, not a trained or self-learning machine-learning model. Flags and assessments assist authorized reviewers; they do not establish fraud or automatically make every approval decision.

### Eligibility assessment

When an application is created, the backend evaluates:

- Whether the employee and welfare program are active.
- Whether the employee's recorded hire date meets the program's minimum service period.
- Whether the requested amount is within the program maximum.
- Existing pending or recently decided applications for the same program.
- Similar applications for the same program and amount.

The assessment and individual rule results are stored with the application. Most failed checks produce an ineligible assessment for review rather than automatically blocking submission. Existing assessments are not automatically recalculated after a draft edit or hire-date correction. The current service-period calculation compares calendar months rather than exact completed months.

### Anomaly detection

Detection runs on submission and persists application-linked flags:

- **Duplicate application:** another submitted application for the same program within 7 days.
- **Application frequency:** more than 3 submissions in 30 days; severity increases above 6.
- **Amount outlier:** a request more than 50% above other submitted requests' program average; more than 100% above average is critical.
- **Cross-program claims:** applications to different programs in the same category within 30 days.

Drafts and cancelled applications are excluded from these comparisons. An amount flag needs a usable historical baseline. Authorized staff can investigate, resolve, or dismiss flags with recorded notes. Repeated detection preserves existing flags and decisions.

[ADHOC-ANOMALY-DETECTION.md](ADHOC-ANOMALY-DETECTION.md) describes a broader proposed feature set. Not every capability in that document is implemented; see the limitations below.

## Technology

- **Frontend:** Next.js 16.4.3, React 19, TypeScript 5, Tailwind CSS 4, Axios.
- **Backend:** ASP.NET Core Web API targeting .NET 10, Entity Framework Core 10, Npgsql, BCrypt, JWT bearer authentication, Swagger.
- **Database:** PostgreSQL, managed through EF Core migrations.
- **Styling:** shared red brand tokens, reusable form fields, buttons, cards, alerts, and a common role-based application shell.

The frontend uses Webpack for development and production builds because Turbopack encountered a CSS worker startup failure in the tested Windows environment.

## Repository structure

```text
backend/
  WelfareSystem.Api/                 API, business services, repositories, entities, migrations
  WelfareSystem.RegressionChecks/    Transaction-rollback integration checks
frontend/
  app/                               Authentication, dashboards, applications, reviews, administration
  components/                        Shared UI, layouts, registration, anomaly components
  lib/                               API clients, authentication, live-data hook
  package.json
  package-lock.json
docs/                                Architecture, requirements, administration, deployment references
start-backend.ps1
start-frontend.ps1
```

Controllers handle HTTP requests; services implement business rules; repositories and EF Core access PostgreSQL. API authorization remains the access-control boundary.

## Local setup

### Prerequisites

- .NET 10 SDK.
- Node.js 20.9 or later and npm; the local verification used Node.js 20.18.
- A running PostgreSQL instance and permission to create/use a development database.
- Git and the EF Core command-line tool.

### 1. Clone and install dependencies

```powershell
git clone https://github.com/MornylyneMutilah/WELFARE-SYSTEM2.git
cd WELFARE-SYSTEM2

dotnet restore backend/WelfareSystem.Api
cd frontend
npm ci
cd ..
```

If `dotnet ef` is not installed:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
```

### 2. Configure the database and API

Create a database named `WelfareDb` using your PostgreSQL administration tool. For example, with PostgreSQL command-line tools available:

```powershell
createdb -U postgres WelfareDb
```

In the terminal that will run migrations and the API, supply your local connection and a private signing key:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=WelfareDb;Username=postgres;Password=YOUR_DATABASE_PASSWORD"
$env:Jwt__SecretKey = "YOUR_RANDOM_SIGNING_KEY_OF_AT_LEAST_32_CHARACTERS"
```

Replace both placeholders. Environment variables override checked-in defaults. Development also supports `backend/WelfareSystem.Api/appsettings.Local.json`, which is ignored by Git; never commit real credentials or signing keys.

Apply migrations, then start the API:

```powershell
cd backend/WelfareSystem.Api
dotnet ef database update
dotnet run --launch-profile http
```

The API runs at `http://localhost:5249`; development Swagger is available at `http://localhost:5249/swagger`.

On a fresh migrated database, development startup seeds demonstration data if departments are absent. On an existing database it preserves existing data. Use migrations for schema updates; do not run reset scripts against a database whose records you want to keep.

### 3. Configure and start the frontend

Create `frontend/.env.local`:

```dotenv
NEXT_PUBLIC_API_URL=http://localhost:5249/api
```

In a separate terminal:

```powershell
cd frontend
npm run dev
```

Open **http://localhost:3000/login**. The API's development CORS policy permits `http://localhost:3000`.

On Windows, `./start-backend.ps1` and `./start-frontend.ps1` are convenience launch scripts. Configure the backend environment in its terminal before launching it.

## Demonstration accounts

On a freshly seeded development database, these accounts use the password **`Password123!`**:

- **Administrator:** `admin@welfare.edu`
- **HR Officer, Flight Operations:** `hr1@welfare.edu`
- **Welfare Committee:** `committee1@welfare.edu`
- **Management:** `management@welfare.edu`
- **Employee, Flight Operations:** `employee1@welfare.edu`

These are development fixtures, not production credentials. Existing databases may have different account details. Use the employee and HR accounts in the same department to demonstrate departmental review. Sign out before switching roles, or use separate browser profiles.

A complete walkthrough:

1. Sign in as the employee, create a draft, and submit it from **My applications**.
2. Sign in as HR, open **Reviews**, select the application, enter notes, confirm, and approve it.
3. Sign in as committee, open **Reviews**, and endorse the HR-approved application.
4. Sign in as management, open **Approvals**, and record final approval or rejection.
5. Return to the employee account to inspect its status and notifications. Dashboards refresh within 10 seconds while visible or when the tab regains focus.

## Verification

The latest local implementation was verified with:

- Backend compilation with zero warnings/errors.
- Frontend ESLint and a production build, including TypeScript checking.
- **94 PostgreSQL-backed regression checks**, covering registration/login, privileged-role requests, application ownership, review stage order, cross-role dashboard counts, anomaly detection, account administration, audit records, and token revocation.
- Live HTTP authorization checks and browser checks of the registration wizard, HR dashboard, and administrator user-management controls.

Run frontend checks:

```powershell
cd frontend
npm run lint
npm run build
```

Run the backend build from the repository root:

```powershell
dotnet build backend/WelfareSystem.Api
```

The regression runner reads the connection from an ignored development configuration file. Create `backend/WelfareSystem.Api/appsettings.Local.json` using your own local credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=WelfareDb;Username=postgres;Password=YOUR_DATABASE_PASSWORD"
  }
}
```

Use a migrated development database, then run from the repository root:

```powershell
dotnet run --project backend/WelfareSystem.RegressionChecks -- backend/WelfareSystem.Api/appsettings.Local.json
```

The runner rolls back its fixture records. PostgreSQL identity sequences may advance. It does not create or migrate the database and is not a substitute for full browser, accessibility, concurrency, or deployment testing. Stop the API before rebuilding it on Windows if its executable is locked.

## Known limitations

- **Social sign-in:** Google, GitHub, and X buttons remain disabled without provider credentials. The inherited OAuth implementation still needs provider testing and security hardening before activation, including browser-bound state, Google nonce/ID-token verification, exchange-code expiry, and configured callback-origin checks. No provider is claimed as production-ready.
- **Eligibility:** failed assessments are generally advisory; assessments are not automatically rerun after later data edits. Registration may default an unspecified hire date to one year ago; administrators should correct it from verified employment records.
- **Documents:** the current upload endpoint records metadata; it does not persist the uploaded file contents. Production document storage, protected downloads, content verification, and malware scanning remain outstanding.
- **Financial reporting:** approved requested amounts are tracked, not actual payment/disbursement transactions. Budget utilization and payment reconciliation are not implemented.
- **Anomaly roadmap:** document-content duplication, login/location/session anomaly models, configurable threshold management, whitelisting, scheduled email digests, and advanced anomaly reports are not implemented or verified.
- **Account provisioning:** automatic invitations, email verification, password recovery, and mandatory first-login password changes are not implemented.
- **Updates:** dashboards use polling and browser events, not a server-push messaging service.

## Deployment considerations

Treat this repository as a development platform requiring deployment preparation. Use private signing keys and database credentials, HTTPS, explicit allowed origins, database backups, and appropriate document storage. Replace or remove demonstration accounts before exposing a deployment. Apply EF migrations before starting a new backend revision.

Build the frontend with `npm run build`, then run it with `npm start`. The current browser API URL is a build-time public setting; configure it for your deployment. Review the development assumptions in CORS, provider callbacks, seed data, and authentication configuration before hosting externally.

## Documentation

- [Administrator user management](docs/ADMIN_USER_MANAGEMENT.md)
- [Environment setup](ENV_SETUP.md)
- [Requirements](docs/REQUIREMENTS.md)
- [Design](docs/DESIGN.md)
- [Database](docs/DATABASE.md)
- [API reference](docs/API.md)
- [Deployment notes](docs/DEPLOYMENT.md)
- [Implementation continuation and limitations](DEVIN_HANDOFF_CONTINUATION.md)

Some older planning and status documents describe targets rather than completed features. Use this README, the administration guide, and the current source/tests as the status reference.
