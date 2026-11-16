# Database and retained records

PostgreSQL is accessed through EF Core 10 and Npgsql. The authoritative schema is `backend/WelfareSystem.Api/Data/AppDbContext.cs`, its entity classes and the additive migrations under `Migrations/`; this summary does not replace those definitions.

## Principal associations

- `User` references `Role` and `Department`; account fields include active status, token version, hire date and verification/security flags.
- `RoleAccessRequest` records requests for organizational roles; trusted approval changes authority.
- `WelfareApplication` references its owner and `WelfareProgram`, with amount, reason, current stage and recorded assessment.
- `ApplicationDocument` references an application and stores requirement category, private storage key, metadata, checksum and scan status. File bytes are outside PostgreSQL in protected storage.
- `ApplicationReview` and `ApplicationStatusHistory` retain reviewer decisions and transitions. New review snapshots preserve decision-time context.
- `DecisionAmendment` retains appeals/addenda and resolution links without replacing original reviews.
- `ProgramBudget` holds annual allocation/proposal information per program and year. It does not represent a payment ledger.
- `DetectionSettings`, anomaly records and related rules/alerts support configured investigation mechanisms.
- `Notification` and `AuditLog` retain recipient/actor-related events and suitable details.
- `ExternalLogin` stores explicitly linked provider subjects; `AccountActionToken` stores hashed, expiring recovery/setup challenges.

Foreign keys, unique constraints, amount precision, current-account checks and transaction/locking boundaries protect supported operations. These controls are not a cryptographically tamper-proof ledger; database operators remain trusted.

## Migrations

Run `dotnet ef database update --project backend/WelfareSystem.Api` with the intended environment and private connection configured. Existing migrations include the initial schema, document/review/history updates, hire dates, anomalies, `AddUserTokenVersion`, `CompleteOperationalWorkflows`, and `RecordEvidenceScanStatus`.

No new database migration is required for tab-specific sessions, consecutive-upload feedback or the current-eligibility preview. Those changes use the existing schema. Migrations do not manufacture verified employment dates, historical snapshots, file bytes or completed scan results.

Development seeding runs only when departments are absent. Do not delete a populated database to obtain fresh demonstration accounts. Correct users and configuration through the supported administration workflow instead.

## Historical versus current data

Draft previews use current program and employment information. Saved assessments preserve the rules used at assessment/submission, and later changes do not silently rewrite submitted applications. Original decisions and independent amendments remain separately inspectable.

Rollback-based tests restore fixture records but identity sequences can advance. Backup and restore database records **together with private evidence**; metadata-only backups cannot recreate missing file bytes. See the operating guide and `scripts/` for coordinated verification and isolated restore procedures.
