# Devin continuation — 2 November 2026

The shared Devin conversation ended after anomaly implementation, with requests to finish setup and run the application. This continuation builds on that source state. The older PLATFORM_ENHANCEMENT_REPORT.md is historical and its completion claims should not be read as verification of every feature in ADHOC-ANOMALY-DETECTION.md.

## Running locally

- Frontend: http://localhost:3000/login
- API: http://localhost:5249
- Swagger: http://localhost:5249/swagger
- Start from the repository root with `./start-backend.ps1` and `./start-frontend.ps1` in separate terminals.
- Next.js development and production builds now use its supported Webpack bundler. Turbopack failed while starting its CSS worker in this Windows environment; the Webpack build passed.
- Development reads `backend/WelfareSystem.Api/appsettings.Local.json`, ignored by Git. It contains the local database connection; no credentials are included in this report. Environment variables override that file. Production does not load it.

## Completed in this continuation

- Applied the existing additive migration `20261029121826_AddAnomalyDetection` to WelfareDb. No existing records were reset or deleted.
- Registered the existing external authentication service and HTTP client; its status endpoint previously could not activate its controller.
- Integrated anomaly detection into application submission in the same database transaction as the submission and notification. A conditional database update prevents repeated submission. Detection checks serialize per employee using a PostgreSQL advisory transaction lock.
- Corrected duplicate self-matches, inclusion of drafts/cancelled applications in baselines, amount baselines including the tested application, and cross-program flags incorrectly matching repeated claims to a single program.
- Repeated detection preserves existing flags and investigator decisions.
- Scoped anomaly queries and summaries using the current database user's role: employees see their own; HR sees its department; committee, management, and administrators see organization-wide records.
- Limited investigation/resolve/dismiss operations to authorized staff, derived resolver identity from the authenticated user, required decision notes, recorded decisions in the audit log, and rejected overwriting closed decisions.
- Corrected summary open counts, combined severity/status filtering, employee read-only cards, mutation loading states, retry feedback, and decision confirmation with preserved notes.
- Enforced application ownership for read/edit/submit/cancel and current database permissions for staff listings.
- Wired profile updates to the existing restricted profile DTO/service so role, department, employee ID, and active status cannot be supplied through self-service profile updates. Added email conflict checks.
- Added a public registration department lookup exposing only IDs and names; retained protected department management.
- Fixed existing React/TypeScript lint issues and the missing small-button prop. Preserved the working email/password registration and login service behavior and hashing.
- Restored sessions using server-verified profile data before rendering role navigation. Prevented repeated social callback exchange caused by changing function identities/React effect replays.
- Continued using the existing red tokens (`--primary`, `--primary-dark`, surface/text tokens), light-theme input contrast fix, Button, Card, Field, Alert, and AppShell. No duplicate UI framework was added.

## Verification

The regression runner is `backend/WelfareSystem.RegressionChecks`. Run from the repository root:

```powershell
dotnet run --project backend/WelfareSystem.RegressionChecks -- backend/WelfareSystem.Api/appsettings.Local.json
```

All 36 checks passed in the final run. The runner uses the existing PostgreSQL schema and rolls back all test fixtures. PostgreSQL identity sequences may advance. It does not reset or migrate the database. Checks cover detection thresholds and idempotence, department/employee authorization, authenticated resolver identity, auditing, closed decisions, profile privilege injection, application ownership, submission integration, pending role requests, duplicate registration, and valid/invalid password login.

Other executed checks:

- Backend compilation and frontend production build.
- Frontend ESLint and TypeScript checking.
- HTTP checks: registration departments 200 (8 departments), provider status 200, unauthenticated anomalies 401, invalid login 401, frontend login/register 200.
- Browser: registration advances through all three panels; Back preserves fields; department choices load; review excludes passwords; missing confirmation blocks submission. No browser account was created or terms accepted during this test. Successful registration/login were exercised by the rollback-only backend checks.
- Social callback/provider success was not tested against real providers.
- Full mobile, assistive technology, all-role workflow, and welfare-document upload tests were not performed in this continuation.

## Social provider setup and limits

Google, GitHub, and X are **unconfigured and disabled**, as verified from the running API and browser. No social provider is reported as fully working. The inherited integration uses server environment variables `Authentication__Google__ClientId`, `Authentication__Google__ClientSecret`, `Authentication__GitHub__ClientId`, `Authentication__GitHub__ClientSecret`, `Authentication__X__ClientId`, and `Authentication__X__ClientSecret`. Frontend uses `NEXT_PUBLIC_API_URL=http://localhost:5249/api`; backend uses `Frontend__BaseUrl=http://localhost:3000`.

Provider callback URLs are:

- http://localhost:5249/api/auth/external/google/callback
- http://localhost:5249/api/auth/external/github/callback
- http://localhost:5249/api/auth/external/x/callback

The frontend callback is http://localhost:3000/auth/callback. Provider consoles may require HTTPS or approved loopback callback URLs; use the exact permitted URL consistently in deployment.

Before enabling social providers for production, complete a separate verification/hardening pass on the inherited flow: bind state to the originating browser, validate Google OIDC nonce/ID-token claims, expire one-time frontend exchange codes, validate configured callback origins, and test denial, linking, and provider token errors. Existing provider identities are not automatically linked just by email.

## Remaining anomaly specification scope

Working persisted detection in this continuation covers application frequency, duplicate applications, amounts relative to program average, and cross-program category claims. The document describes a broader product roadmap than the implemented service: configurable threshold administration/whitelists, login/location/session analysis, budget/disbursement analysis, content-hash document duplication, document information verification, reviewer statistics, trend/report exports, escalations, and scheduled email digests remain unimplemented or unverified. The pre-existing detector classes and rule/alert tables do not by themselves establish those features as working.

## Files changed

- `.gitignore`, `ENV_SETUP.md`, this report.
- `backend/WelfareSystem.Api/Program.cs`.
- Controllers: `AnomaliesController.cs`, `ApplicationsController.cs`, `AuthController.cs`, `UsersController.cs`.
- Services: `AnomalyDetectionService.cs`, `WelfareApplicationService.cs`, `UserService.cs`, `Interfaces/IUserService.cs`.
- `backend/WelfareSystem.RegressionChecks/Program.cs` and its project file.
- `frontend/package.json`, `frontend/lib/auth/AuthContext.tsx`.
- API clients: `anomalies.ts`, `client.ts`, `departments.ts`.
- Pages: `app/auth/callback/page.tsx`, `app/employee/applications/page.tsx`, `app/employee/profile/page.tsx`.
- Components: `anomaly/AnomalyCard.tsx`, `AnomalyList.tsx`, `AnomalySummaryCard.tsx`, `common/RegistrationWizard.tsx`, `SocialAuthButtons.tsx`, `ui/Button.tsx`.
- Next.js regenerated the heading in `frontend/AGENTS.md`; its instructions remain intact.
- Local ignored configuration: `backend/WelfareSystem.Api/appsettings.Local.json`.

No new migration was authored; the existing pending migration was applied.
