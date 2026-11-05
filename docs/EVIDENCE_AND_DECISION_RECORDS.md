# Evidence, eligibility, decision records and reports

Implemented on 2 November 2026. This extends the existing workflow; it does not change HR → Committee → Management stage ordering.

## Employee application workspace

Open **Applications → an application ID / View timeline**. Newly created drafts also offer **Add documents and review draft**.

- Edit the requested amount and reason while the application is a draft.
- Read the recorded eligibility results, upload supporting evidence, and review the timeline.
- Confirm the application details and evidence before submitting. The server rechecks eligibility using current employment and program data.
- After submission, application details and supporting documents are locked against replacement or removal.
- Track submission and each review stage, reviewer, date, notes, and decision references. Authorized users can print the application and individual decision confirmations using the browser's Print / Save as PDF action.

Old uploads that previously recorded metadata without saving file contents are explicitly marked unavailable. Their missing bytes cannot be recovered by this change. Draft owners can remove such metadata and supply the original file again. Submitted legacy documents remain recorded; there is no unaudited replacement bypass.

## Private document storage

New uploads save actual file bytes. Default location: `backend/WelfareSystem.Api/App_Data/Documents`, ignored by Git and outside static web content. A random server-generated storage key is used; filenames and browser MIME types do not determine filesystem paths or trusted content types.

- Maximum file size: 10 MB. Supported formats: PDF, DOC, DOCX, PNG, JPEG.
- Basic signatures must match the file extension. DOCX archives have expansion/entry limits, require Word document entries, and reject VBA project entries. These checks do not establish that a document is malware-free.
- Downloads require bearer authentication and server-side application access: owner, HR within its department, or an authorized committee, management, or administrator account.
- Only an active draft owner can upload or remove evidence. Application row locks coordinate evidence changes with submission.
- Downloads use attachment disposition and `nosniff`; files are not publicly linked or served through a static directory. Storage paths are not returned to the browser.
- Uploads record SHA-256 checksums in audit details; uploads, removals, and downloads record actor-attributed audit events. Checksums are recorded evidence metadata, not a digital signature or an automatic antivirus scan.

Optional server setting: `Documents:StoragePath` (environment equivalent `Documents__StoragePath`). Set an absolute path on a private persistent volume; never point it at a publicly served directory. The API service account needs read/write permission. Back up **both PostgreSQL and this directory** together. Container/local disk without persistence is unsuitable for retained evidence. Multi-instance deployments need shared private storage. Quotas and ClamAV integration are now implemented; a real updated scanner and deployment volumes still require operator setup.

## Eligibility assessments

Service duration uses completed calendar months with `AddMonths` anniversary semantics, including end-of-month dates. Draft creation, draft editing, and submission retain separate assessment events in the existing audit table. Submission uses current hire date, account status, program limits, service requirement, and other applications, excluding the application being assessed.

The submitted assessment includes its timestamp, stage, rule results, and employment/program inputs. Later source-data changes do not rewrite this assessment. Historical applications are not silently reprocessed or backfilled.

Inactive programs/accounts cannot submit. Employment verification, service, amount and required evidence are now mandatory; duplicate/frequency concerns remain advisory. Required evidence is now categorized and checked on the backend; reviewers still determine authenticity. See OPERATING_GUIDE.md for the current mandatory policy.

## HR, committee and management decision register

Use **Decision register** in the role navigation, including from the dashboards. Filter by employee/program/application or numeric decision ID, date range, decision, or **My decisions**. Administrators can filter all three stages. HR team records are scoped to the current department; authorized HR users retain their own historical decisions even after a department transfer. Committee and management registers show their respective stages. Opening unrelated application details still follows current application permissions.

Every new decision records:

- A stable reference, such as `WEL-DEC-000123`, application ID, stage, recommendation, reviewer, timestamp, and full notes.
- A decision-time snapshot of employee identity, department ID, program name, requested amount, application reason, submitted eligibility assessment, resulting status, and document manifest.
- The application review, status transition, audit event, and existing notification in the same database transaction.

Completed decisions and notes cannot be overwritten through the update endpoint. The Appeals & addenda workflow now supports independent, attributed corrections and appeals without overwriting this record; see OPERATING_GUIDE.md. This is application-level immutability, not a cryptographically tamper-proof ledger: database administrators still control the database.

HR approval and committee endorsement remain recorded when management later finalizes the application. Committee endorsement is labelled as forwarding to management, not final approval. Rejections are retained too.

Earlier reviews remain searchable. Confirmations explicitly disclose when the old record has no decision-time snapshot. The register and CSV show current employee/program names; individual confirmations use the historical snapshot when one exists. A receipt is not proof of payment, a legally certified document, or a digital signature.

Employees can view confirmations for decisions on their own applications. Organization-wide registers remain restricted to authorized staff; historical confirmation access does not grant new document permissions.

## Administrator audit log

Open **Administrator → Audit log**. Filter by exact action, entity type, actor ID, and UTC date range. Events are paginated with expandable structured details. Secret-like JSON fields are redacted. No edit/delete API is provided.

Useful actions include `ApplicationReview`, `EligibilityAssessed`, `DocumentUploaded`, `DocumentDownloaded`, `DraftDocumentRemoved`, `UserSuspended`, `ProgramCreated`, and `ProgramUpdated`. Older events are preserved. This covers recorded business events, not every HTTP request or a full forensic security log.

## Reports and exports

All roles have **Reports** navigation. Employee reports contain their own applications; HR reports follow department scope; committee, management, and administrator reports follow existing organizational permissions. Existing aggregate report endpoints now also use database-verified application access. Stage processing averages use recorded review timestamps rather than demonstration constants.

- Application CSV exports include reference, employee, program, requested amount, status, eligibility, and date.
- Decision CSV exports include reference, application, stage, decision, reviewer, timestamp, notes, and current application status.
- Administrator audit CSV exports include actor, event, entity, timestamp, and redacted details.
- Exports follow the selected filters, are capped at 10,000 rows, escape CSV syntax, neutralize formula-like cell values, and record export audit events.
- Print the report/current decision page or a single confirmation using the browser. Paginated register/audit printing prints the current page; CSV exports all matching rows within the limit.

Approved amounts represent requested amounts approved by reviewers. Actual payments, budgets, and disbursement reconciliation remain unimplemented.

## Schema and verification

This initial feature set required no migration; the later operational workflows add two additive migrations documented in OPERATING_GUIDE.md. No data reset is performed. Existing document, review, status-history and audit tables hold these changes; file bytes are stored privately outside PostgreSQL. Old decisions and assessments are not fabricated or rewritten.

Regression checks use a PostgreSQL transaction that is rolled back and a unique temporary document directory that is cleaned after the run. Coverage includes byte persistence, file validation, access restrictions, evidence locking, completed-month boundaries, reassessment, snapshots, immutable notes, historical decision retrieval, CSV escaping, audit restrictions, and scoped aggregate reports. See the README for the verification commands and most recent results.
