# Welfare system operating guide

Verified local implementation: 3 November 2026. Social-provider activation is deliberately excluded. Financial scope is budgets and approved application commitments only; there are no payment records or bank transfers.

## Employment verification and mandatory submission requirements

An administrator must open **Users → Edit**, check the hire date against authoritative employment records, tick **I checked this hire date…**, supply a reason, and confirm the change. Changing a hire date clears its previous verification unless the administrator explicitly confirms the new date. Do not invent dates to make a demonstration application eligible. Existing accounts are not automatically marked verified by migration.

Self-registration no longer invents a year of service when the date is omitted. Its date remains unverified until an administrator corrects and verifies it. The Users page shows records needing verification; **Readiness** shows the outstanding count.

The backend blocks submission unless all of these pass:

- Active account and welfare program, and any required initial password change completed.
- Administrator-verified employment date.
- Minimum completed calendar months of service.
- Positive requested amount within the program maximum.
- Actual stored evidence assigned to every required document category.

Employees choose the required category on each upload. **Additional evidence** does not satisfy a named requirement. A matching category establishes that a file was supplied, not that its contents are authentic; reviewers still examine the evidence. Duplicate/frequency concerns remain advisory and never establish fraud automatically. Submission locks the application and evidence and retains the assessment inputs and rules used.

Blocked attempts retain the draft and assessment for correction. Existing submitted applications and historic decisions are not silently reassessed or rewritten.

## Account security and invitations

**Account security** is available in each role's navigation. It supports password change, verification requests, and employment/email status. Password changes require the current password, a different password with 12–128 characters including letters and numbers, and revoke previous sessions. Existing valid email/password sign-in is preserved.

New administrator-created accounts must replace their temporary password before accessing the rest of the API. The restriction is enforced on the server, with an allowed security screen and profile hydration route. Administrators can use **Send setup invitation** for eligible initial accounts. Accepting an invitation chooses a private password; it does not grant a browser-selected role.

**Forgot your password?** requests an expiring reset link without disclosing whether an address exists. Reset links expire after 30 minutes; verification and invitation links after 24 hours. Tokens are hashed in the database, bound to the current account/email/session version, and single-use. Account suspension or identity/session changes invalidate outstanding links. Failed/weak/password-mismatch submissions preserve form values where appropriate. Reset and verification pages require an explicit action; opening a link does not change the account.

### Development email versus real delivery

Without SMTP configuration, development messages are written to the private, ignored `backend/WelfareSystem.Api/App_Data/MailOutbox` folder as `.eml` files. The UI explicitly reports **Development outbox**. This is a local demonstration facility, not delivery to a real inbox. Treat these files as secrets: they contain account-action links. Never commit or publicly serve them.

For real email, configure these API-only environment variables or equivalent private settings:

```text
Mail__Host
Mail__Port=587
Mail__Username
Mail__Password
Mail__From
Frontend__BaseUrl=https://your-frontend-origin
Authentication__BackendBaseUrl=https://your-api-origin
```

The implementation uses authenticated SMTP with STARTTLS. Choose a server that supports this flow; provider-specific modern authentication may need an additional mail adapter. Do not disable TLS or place mail passwords in frontend variables. SMTP messages are queued privately in `App_Data/MailPending` and retried by a background worker, keeping provider latency outside the recovery response. The pending count is exposed to administrators in the operations API; delivery failures are logged without link contents. A queued/configured message is not proof of inbox delivery. Test real delivery, sender authorization, spam handling, and mailbox links before deployment.

Production has no development-outbox fallback: requests report unavailable mail until it is configured. No SMTP credentials or successful real SMTP delivery are claimed by this revision.

## Supporting evidence and scanning

Private storage defaults to `backend/WelfareSystem.Api/App_Data/Documents`, excluded from Git and static serving. Override with `Documents__StoragePath` pointing to a private persistent volume. Keep it outside any public web directory and limit access to the API service account and backup operator.

- Each file is at most 10 MB; a draft holds at most 20 files and 50 MB total.
- PDF, DOC, DOCX, PNG and JPEG undergo basic signature checks. DOCX has archive-entry/expansion limits and rejects VBA-project entries. These checks alone do not establish malware safety; legacy DOC may contain active content.
- SHA-256 checksums support repeated-content investigation and retained evidence manifests. Uploads are categorized against server-defined requirements.
- Only the active draft owner changes evidence. Downloads enforce owner/HR department/authorized staff access, use attachment disposition and `nosniff`, and record the reader for audit.
- **Unscanned development** and **Legacy/Unknown** are displayed honestly. A file is labelled **ClamAV clean** only after a successful scanner response. This label describes a scan result, not a guarantee against every threat.

To connect a private ClamAV daemon:

```text
Documents__ScannerHost=your-private-clamd-host
Documents__ScannerPort=3310
```

The API uses the official [ClamAV INSTREAM protocol](https://docs.clamav.net/manual/Usage/ClamdProtocol.html), limits response size and time, rejects detections/errors, and never writes accepted evidence before the configured scan succeeds. Keep the daemon and definitions updated and the scanner port on a private trusted network. This protocol is not an Internet-facing authenticated transport.

Without a scanner, development remains explicitly unscanned; production uploads fail closed. Automated tests use a local simulated daemon for clean/rejected responses. No installed, up-to-date ClamAV service or live malware-detection test is claimed on this device. Deploying/configuring that external service is still an operator step.

## Decision records, correction addenda and appeals

The **Decision register** retains HR reviews, committee endorsements, management decisions and rejections after an application moves on. New decisions have stable references and snapshots; old records disclose missing historic snapshots. See the evidence guide for access scope, timelines, printing and CSV exports.

**Appeals & addenda** provides an attributed workflow:

- An application owner can appeal the rejection on a currently rejected application.
- The original reviewer can request a correction addendum with proposed notes.
- An independent current manager or administrator accepts or rejects a request with a reason. Requesters and original decision makers cannot resolve their own request.
- Accepted addenda append clarification; the original recommendation and notes remain unchanged.
- Accepting an appeal creates a linked reconsideration **draft**. The employee supplies evidence and submits it through the normal eligibility and HR → Committee → Management process. Acceptance is not an award or final welfare approval.
- Resolution records and notifications are retained. Confirmations show the original record and subsequent addenda separately.

This preserves application-level history, not a cryptographically tamper-proof ledger. Database administrators remain trusted operators.

## Budgets only

Administrators propose annual allocations per welfare program. Management independently approves or rejects proposals. A pending/rejected revision does not replace the previous approved allocation. Repeated decisions on a finalized proposal are rejected, and changes are audited.

The **Budgets** page shows allocation, current approved application commitments for the approval year, and remaining allocation. Oversubscription is a warning for management, not automatic rejection. Commitments are calculated from approved requested amounts; no payments or disbursements are recorded. This is the scope selected for this implementation.

## Anomaly controls and experimental model

Administrators use **Detection controls** to change the submission-frequency window/limit, duplicate window and amount-above-average threshold with an audit reason. Changes affect subsequent detection, preserving existing flags and investigator decisions. SHA-256 duplication flags identify identical bytes across submitted applications without disclosing another employee's identity in the flag.

The optional **k-nearest-neighbour amount novelty model** is disabled by default. It learns an amount-distance threshold from up to 500 earlier approved applications for the same program, using leave-one-out neighbour distances. It abstains below 50 prior approvals and excludes the candidate and approvals finalized after its submission time. The score is a distance, not a fraud probability. It does not inspect personal narratives or send data to an external AI provider.

Tests demonstrate deterministic behavior on synthetic normal/outlier fixtures. They do not establish real-world precision, recall, fairness, or institutional suitability. Keep the model disabled until a representative, authorized historical evaluation is completed. Other rule-based detection remains available without it.

## Interactive dashboard design

Dashboards have an interactive welfare support explorer, selectable topics, and a 2D/3D depth toggle. The graphics use native CSS transforms, require no external model service, are decorative to assistive technology, and respect reduced-motion preferences. Login and registration reuse the 3D support-card illustration on their red panels. Decorative hearts orbit the cards with a pause/resume control. The personalized dashboard card flips on dashboard load, including refresh, and replays when the card is hovered or Replay welcome is selected. Each greeting returns to the welfare message after 6.5 seconds; token refresh alone does not replay it. Reduced-motion preferences retain the greeting without the flip or orbit. Application workspaces show a readable 2D journey alongside the authoritative timeline. Approval and eligibility decisions remain ordinary labelled, confirmed controls.

## Deployment, health and maintenance

**Administrator → Readiness** reports schema, account-email mode, scan configuration, unverified employment, and initial-password counts. It distinguishes configuration from operational proof. `/health/live` reports process liveness; `/health/ready` checks database connectivity, schema status and maintenance, not SMTP deliverability, scan-signature currency or backup recoverability.

Production startup requires a private `Jwt__SecretKey` environment value of at least 32 bytes, a fully migrated database, and removal/change of active demonstration passwords. Development seeding is not run in production. Use HTTPS, explicit frontend/backend origins, trusted proxy settings appropriate to the deployment, protected logs, persistent database/evidence/mail volumes, and service-account permissions. Certificates, hosting accounts and real service credentials must be supplied by the deployment operator; this change has not published a public deployment.

Maintenance rejects new data writes and waits for in-flight writes to finish. Login/refresh remain available so an administrator can resume operation. The gate is per API instance and resets on process restart. It is suitable for the current single-instance local deployment. A multi-replica deployment needs coordinated orchestration/shared maintenance before using this backup procedure.

### Coordinated backup and isolated restore verification

The scripts require PostgreSQL client tools, a private configuration file for the same database as the API, and an administrator token supplied privately in the terminal. The backup script checks the API/database identity fingerprint before maintenance, uses non-interactive clients, drains writes, captures a custom-format database dump, copies private evidence, and records checksums and record counts. It resumes operation in `finally`; it never overwrites an existing backup directory.

```powershell
# Set WELFARE_ADMIN_TOKEN privately in this terminal; never commit it.
./scripts/Backup-Welfare.ps1 -BackupDirectory ./backups/my-new-backup
./scripts/Verify-WelfareBackup.ps1 -BackupDirectory ./backups/my-new-backup
./scripts/Test-WelfareRestore.ps1 -BackupDirectory ./backups/my-new-backup -RemoveTestDatabase
```

Use `-ConfigurationFile` for a different private configuration and `-PgDumpPath` / `-PostgresBin` if tools are not discoverable. The restore test always generates a new `welfare_restore_check_<random>` database, never restores over the source database, checks restored counts, copies/verifies evidence into a temporary location, and can remove only its own newly created test database. A successful drill writes `restore-test.json` alongside the backup.

Local verification created private backups and successfully restored into isolated databases with matching record counts. A separate synthetic private-file fixture exercised file copy/restore and checksum validation; the test database was removed and the source database preserved. Retain backups privately, encrypt off-device copies, define retention and recovery objectives, and repeat drills on the actual deployment. The scripts do not configure a cloud backup destination or schedule.

## Database migrations and validation

Applied additive migrations:

- `20261102202751_CompleteOperationalWorkflows`
- `20261102204850_RecordEvidenceScanStatus`

They add security flags/challenges, document metadata, budgets, amendment records and detection settings. Existing records are preserved; employment verification, email ownership, malware scans and historic decisions are not fabricated.

The final local regression runner passes **261 checks**, rolls back fixture records and removes its temporary evidence directories. It covers existing authentication/authorization, mandatory eligibility, evidence, immutable decisions, accounts, budgets, appeals, scanner protocol, maintenance and model fixtures. Frontend lint/build and backend compilation pass. Live role checks confirm budget/readiness/detection restrictions, and the dashboard topic/depth controls were checked in the browser. These checks are not a full penetration test or proof of production readiness.

### First operator actions

1. Verify genuine employment dates from authoritative records in **Users**.
2. Configure and test real SMTP delivery when leaving local demonstration mode.
3. Configure an updated private scanner and durable storage before production uploads.
4. Choose production origins/certificates/secrets and replace demonstration credentials before hosting.
5. Evaluate representative institutional history before enabling the experimental model.

Social sign-in activation remains deliberately excluded.
