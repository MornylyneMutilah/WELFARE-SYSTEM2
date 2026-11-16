# Verification and evidence

Latest recorded local verification: **17 November 2026**. These checks establish behaviour under their stated local conditions; they are not institutional UAT, independent penetration certification or production load testing.

## Executed checks

- **279 PostgreSQL-backed regression checks passed**, with fixture rollback. Coverage includes registration/login, role provisioning, ownership/department/stage restrictions, decision records, evidence, program rules, account revocation, budgets, independent amendments, maintenance, scanner-protocol simulations and experimental-model fixtures.
- **6 frontend session tests passed**: role isolation, request-token selection, refresh/reload, tab-specific logout and rejected sessions, one-time legacy migration, and password-change-required response handling.
- Frontend ESLint and production build passed, including TypeScript checks.
- API compilation passed with no warnings or errors.
- Next.js and its lint configuration were patched to 16.4.6. The compatible npm dependency fixes reported zero known vulnerabilities at verification time; this is an advisory scan, not a security certification.

The current-rule regression exercises the same 3,000 draft under a 2,000 maximum and then a 5,000 maximum. It checks blocked submission, a refreshed read-only preview, successful resubmission, current-rule snapshots, preserved failed history, updated service/evidence/active requirements and preview access restrictions. Reading previews must not create audit rows or overwrite historical assessments.

Earlier targeted HTTP verification in an isolated database accepted Administrator program saves after Employee login, rejected Employee program edits with HTTP 403, and retained the Administrator session after Employee logout. Prior browser walkthroughs exercised consecutive uploads, invalid-file retry, blocked submission feedback and selected role screens. Those recorded checks are not represented as new formal participant testing.

## Repeatable commands

From the repository root, with a migrated development database and an ignored local JSON file containing your private connection:

```powershell
dotnet build backend/WelfareSystem.Api
dotnet run --project backend/WelfareSystem.RegressionChecks -- backend/WelfareSystem.Api/appsettings.Local.json
cd frontend
npm run test:auth
npm run lint
npm run build
```

The regression runner rolls back fixture records and removes temporary private evidence. Identity sequences can advance. It does not reset, create or migrate the source database. Stop a running Windows API before rebuilding its locked binaries, or build a separate configuration/output.

## Dated performance baseline

The [15 November 2026 summary](verification/performance-2026-11-15.json) and [150 raw samples](verification/performance-2026-11-15.csv) preserve the actual timing evidence. Each operation had three excluded warm-ups and 30 measured runs; all measured trials completed successfully.

- Login: average 292.31 ms; maximum 310.93 ms.
- Final application submission: average 31.18 ms; maximum 44.59 ms.
- One application retrieval: average 4.37 ms; maximum 6.00 ms.
- Employee dashboard data-ready navigation: average 186.30 ms; maximum 249.64 ms.
- Administrator application CSV export: average 5.65 ms; maximum 8.61 ms.

This was one sequential local test user, an isolated PostgreSQL snapshot, a Release API in Development mode and a production-build frontend with warm browser cache. Drafts/evidence were prepared before timing final submission. The snapshot began with 18 users and 7 applications; fixture additions brought it to 40 applications. Dashboard timing waited for data and rendering, not decorative animation. CSV export timing included complete file receipt and its audit write.

The measurements predate the tab-session and live-eligibility fixes and therefore are **not a fresh benchmark of this revision**. No advance latency SLA, cold-start result, remote-network result, large-dataset throughput or concurrent-user capacity is claimed. Temporary timing databases were removed and source record counts were checked unchanged after that exercise.

## Outstanding evaluation

Formal role-representative UAT, accessibility/assistive-technology coverage, live provider account linking, actual SMTP inbox delivery, an operational private malware scanner, representative anomaly labels, sustained/concurrent load and production recovery objectives require further evidence. Synthetic scanner/model tests do not substitute for those operational evaluations.
