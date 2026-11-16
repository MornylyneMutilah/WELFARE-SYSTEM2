# User guide

Open `http://localhost:3000/login` in the local setup. Sign in to the required role. Email registration uses personal/account details, account-type explanation and a final review/confirmation; selecting an organizational role is a request, not self-assigned authority.

## Using more than one role

Administrator and Employee can sign in in separate tabs. Each tab keeps its own session through refresh; logging out of one does not replace the other's credentials. A new tab can require sign-in and closing the tab normally ends its session. After upgrading from the older shared-session implementation, hard-refresh old tabs and sign in to each intended role once.

## Employee: prepare and submit

1. Browse active programs and read amount limits, minimum service, instructions and required evidence.
2. Create a draft with the requested amount and reason. Open its application workspace.
3. Upload a real supported file for each named requirement. Additional evidence does not substitute for a required category. Wait for the success message, then choose the next file; the picker remains available after successful uploads and blocked submissions.
4. Read **Current eligibility checks**. Use **Refresh checks** after an administrator changes policy; the page also refreshes on focus and within ten seconds while visible. **Previous recorded assessment (historical)** describes an earlier check and is not the current policy result.
5. Resolve mandatory failures: inactive account/program, required initial password change, unverified employment, insufficient completed service, over-limit amount or missing evidence. Administrator verification must use genuine employment information.
6. Review details/evidence, tick the confirmation and submit once. Success appears only after the backend confirms submission. The application and evidence then become locked for review.
7. Track status, notifications and the recorded review/decision timeline. Authorized printable views support browser Print / Save as PDF.

Example: a 3,000 draft is blocked under a 2,000 maximum. If an Administrator saves 5,000 for the same program, refresh the current checks; the amount concern clears without recreating the draft. Other mandatory conditions still apply. Old failed assessments remain in history.

## HR, committee and management

- HR opens its departmental review queue and records permitted recommendations/decisions with notes.
- Committee members review HR-forwarded cases and endorse or reject them. Endorsement forwards to management; it is not final approval.
- Management records a permitted final decision. Stage skipping, self-review, cross-department HR access and duplicate decisions are blocked.
- The **Decision register** retains completed work after it leaves the pending queue. Search and combine supported stage, outcome, scope and date filters; open a confirmation or export matching CSV results.
- Independent appeals and addenda retain original decisions. An accepted appeal creates a linked reconsideration draft, which follows the normal process again.

## Administrator

**Users:** create and maintain accounts, roles/departments, verified employment dates and active status. Use reasons/confirmation. Suspend without deleting history, reactivate for a fresh sign-in, and use setup invitations where applicable.

**Programs:** create, edit, deactivate/reactivate and configure program limits, service months and required categories. Supply a reason and confirm the reviewed change. A conflict means another change occurred; refresh and reopen rather than overwriting it.

**Audit:** fields filter existing read-only events. For a program-edit demonstration use `ProgramUpdated` in **Exact action** and `WelfareProgram` in **Exact entity type**. **Actor user ID** takes a numeric ID, not a name; dates are UTC. Blank filters include all permitted dates/actors. Expand **Recorded details** for the retained change.

**Detection controls:** adjust supported thresholds and record a reason. Anomaly flags support investigation and do not automatically prove misconduct.

**Budgets:** propose annual allocations. Management independently approves/rejects them. Displayed approved commitments are not payments.

**Readiness:** inspect actual schema/configuration/account checks. Some overview dashboard service labels remain static; do not treat them as live monitoring.

## Recovery and common errors

- Use **Forgot your password?** or Account security for supported recovery/verification. Without SMTP, development messages are in the private API outbox rather than a real inbox.
- `401`: sign in again when the session is expired/revoked.
- `403`: the tab's account cannot perform the action, or an initial password must be replaced. Program editing requires Administrator access.
- `409`: refresh a stale edit/decision and inspect current state.
- A missing-evidence or over-limit message preserves the draft. Read **Current eligibility checks** before relying on an old historical result.
- Wait for uploads to finish before final submission. A rejected file can be corrected and uploaded again; successful uploads do not disable the picker permanently.

For operators, scanners, mail, private files and recovery drills, see the [operating guide](OPERATING_GUIDE.md).
