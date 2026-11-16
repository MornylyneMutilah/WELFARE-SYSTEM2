# Implemented architecture

## Request and persistence boundaries

The Next.js/TypeScript frontend sends Axios requests to an ASP.NET Core .NET 10 API. Controllers expose HTTP operations, services implement business rules, and EF Core/repositories access PostgreSQL. Private file bytes are retained separately from relational document metadata and can be downloaded only through authorized endpoints.

Client components share the application shell, red design tokens, fields, buttons, cards, alerts, confirmation controls, evidence workspace and action feedback. The existing styling approach remains Tailwind CSS plus shared CSS tokens; no second component framework is required.

## Identity and authorization

Email/password authentication uses BCrypt and signed JWT access/refresh tokens. The API validates current account activity, role and token version. Ownership, department and stage checks protect records and writes. Browser role labels and navigation are presentation only.

Access/refresh tokens and profile state live in `sessionStorage`, isolated per tab. The one-time legacy migration removes shared credential keys from `localStorage`. Non-sensitive cross-tab change signals still use browser storage events. This storage choice permits role demonstrations without replacing another tab's credentials; it is not a substitute for XSS prevention or server authorization.

## Drafts, live rules and recorded decisions

Draft creation/editing records eligibility assessments. A separate read-only live-preview endpoint evaluates the latest account, program and evidence conditions for draft display without writing audit rows or replacing a historical snapshot. The frontend refreshes current program data and previews on focus, relevant events and ten-second polling.

Submission independently checks mandatory requirements in the coordinated write path. Application row locks coordinate evidence editing with submission; selected advisory locks coordinate administrative policy writes. Successful submission transitions into HR review. HR approval forwards to committee; endorsement forwards to management. Authorized rejection stops the normal approval path.

Reviews retain attribution, comments, transitions, notifications and new decision-time snapshots. Appeals and correction addenda are separate attributed records. An accepted appeal creates a reconsideration draft rather than overwriting the original decision.

## Detection and exploration

Mandatory rules constrain active status, verified employment, completed service, amount and named evidence categories. Configurable anomaly thresholds support investigation. The optional amount model has minimum-history and disabled-by-default controls; no automatic fraud conclusion or autonomous award is implemented.

Reports, audit filters and decision registers expose supported fields, record details, CSV exports and printing. They are structured exploration rather than unrestricted database queries. Budget summaries compare allocation with approved commitments rather than cash payments.

## Operational design and limits

Health/readiness endpoints, maintenance gating, private mail queues and coordinated database/evidence backup scripts support local operations. OAuth pending state and maintenance coordination currently assume one API instance. Real SMTP, live scanning, production origins, persistent storage and representative load/acceptance testing require deployment-specific work.

Dashboard refresh uses polling/browser events rather than server push. Static administrator service labels are distinct from actual Readiness and health checks. See [deployment](DEPLOYMENT.md) and [verification](VERIFICATION.md) for the operating boundaries and tested conditions.
