# API reference

Development base: `http://localhost:5249/api`. Development Swagger at `http://localhost:5249/swagger` describes DTOs and request schemas. Route names below follow the implemented controllers; methods and access remain enforced by the API.

## Authentication and accounts

- `POST /auth/register`: validated self-registration; creates Employee access. A privileged role string creates, at most, a pending request rather than trusted authority.
- `POST /auth/login`, `/auth/refresh`, `/auth/logout`: credential/session operations. Access and refresh tokens are not interchangeable.
- `GET /auth/registration-departments`: permitted registration choices.
- `/account/*`: password recovery, initial account completion, verification, security status and password change. Inspect Swagger for the individual DTOs.
- `/users/me`: current authenticated profile; administrative fields are protected from self-service changes.
- `/users`, `/users/options`, `/role-requests`: trusted administrator management. Roles, departments and active status are checked against current database records.
- `/auth/external/providers`, `/links`, `/{provider}/start`, `/{provider}/link`, `/{provider}/callback`, `/exchange`: external-provider configuration, explicit linking and authorization-code exchange. See the social-sign-in guide.

JWT validation checks active status, current role and token version. A required initial password change restricts the API until account setup is completed. Browser sessions are tab-specific; this does not grant any extra server permission.

## Programs and applications

- `GET /welfareprograms`, `/welfareprograms/active`, `/welfareprograms/{id}`: program reads.
- `POST /welfareprograms`, `PUT /welfareprograms/{id}`, `DELETE /welfareprograms/{id}`: Administrator-only creation, editing and deactivation. Updates support `expectedUpdatedAt` conflict protection and an audit reason.
- `GET /applications`: the authenticated user's applications.
- `GET /applications/all`: privileged scope; HR remains restricted to its department.
- `GET /applications/{id}`: permitted application details and saved assessment.
- `GET /applications/{id}/eligibility-preview`: authorized **draft-only**, read-only assessment using current program, employment and evidence data. Returns `ruleEvaluationResult`, `canSubmit` and `blockingIssues`. It does not overwrite a saved assessment or create audit rows. Submitted applications use their recorded snapshots.
- `POST /applications`: create an owned draft.
- `PUT /applications/{id}`: edit an owned draft and record reassessment.
- `POST /applications/{id}/submit`: recheck current mandatory requirements and commit submission; blocked requests preserve the draft and return explanatory issues.
- `DELETE /applications/{id}`: permitted draft cancellation.
- `/reviews/*`: stage-specific HR/committee/management actions, guarded against stage skipping, cross-department HR decisions, self-review and repeated decisions.

Amounts are not validated only by the browser. Draft previews are advisory reads; final submission independently enforces the current server-side requirements inside the coordinated write path.

## Documents, records and reports

- `POST /documents/application/{applicationId}`: multipart upload with `file` and `requirement`, restricted to the active draft owner.
- `GET /documents/application/{applicationId}`, `/documents/{id}`, `/documents/{id}/content`: authorized metadata and download.
- `DELETE /documents/{id}`: draft-owner removal; submitted evidence remains retained.
- `GET /records/applications/{id}`: authorized timeline and supporting history.
- `GET /records/applications.csv`: role-scoped application export with supported filters.
- `GET /records/decisions`, `/records/decisions/{id}`, `/records/decisions.csv`: decision register, confirmation and export; privileged scope and record visibility apply.
- `/reports/*`: scoped status, department, program, commitments, processing-time and flagged-application summaries.
- `/auditlogs` and `/auditlogs/export.csv`: read-only Administrator audit access. Supported filters include exact action/entity type, actor and date range; paginated listing prevents loading every event at once.
- `/notifications/*`: authenticated notifications and read state.

Private document content uses authorization, attachment disposition and `nosniff`. CSV generation mitigates spreadsheet formula injection. Export and download activity is audited.

## Other role-scoped operations

- `/dashboards/employee`, `/hr`, `/committee`, `/management`, `/admin`: role-specific statistics.
- `/anomalies/*`: authorized listing, explanations and attributed investigation/closure actions.
- `/detectionsettings`: Administrator detection parameters; changes are audited.
- `/budgets/*`: Administrator proposals and independent management decisions; commitments only.
- `/amendments/*`: independent appeals and correction addenda preserving original reviews.
- `GET /operations/status`, `POST /operations/maintenance`: Administrator readiness and maintenance coordination.
- `GET /health/live`, `GET /health/ready`: anonymous process/readiness checks outside the `/api` prefix.

## Error handling

- `400`: invalid fields, unmet mandatory submission requirements or an operation unsupported for the current draft/state.
- `401`: absent, expired, revoked or otherwise invalid credentials. Refresh tokens cannot be used as bearer access tokens.
- `403`: authenticated role is not permitted, or initial password replacement is required. `PASSWORD_CHANGE_REQUIRED` routes the frontend to Account security.
- `404`: missing or non-visible records, including protected previews.
- `409`: conflicting or stale edits/decisions.
- `429`: protected account/OAuth endpoint rate limit.
- `503`: maintenance/readiness or configured service unavailability, as applicable.

Read the response's `message`/validation issues rather than treating every failed request as the same error. Do not retry an irreversible decision blindly; retrieve current state first.
