# Implemented requirements and scope

This is the maintained implementation scope. Older sprint plans and unchecked wish lists are not the current feature contract. The approved academic problem and objectives remain in the author's submission document; this file describes the repository's implemented behaviour.

## Account and access requirements

- Preserve working email/password login and three-step registration with explicit review/confirmation.
- Public registration grants Employee access only. Organizational roles require trusted approval/provisioning.
- Enforce current server-side roles, account activity and token versions, plus record ownership, HR department and workflow stage.
- Support administrative account maintenance, verified employment dates, suspension/reactivation, recovery/setup challenges and forced initial password replacement.
- Isolate browser-tab sessions so demonstrations with different roles do not replace another tab's credentials.

## Welfare workflow requirements

- Maintain active/inactive programs with predefined configurable limits, minimum service and evidence categories.
- Support owned editable drafts, consecutive private evidence uploads and explicit final submission confirmation.
- Show current draft eligibility independently from saved historical assessments; refresh current program data and retain the same draft after a blocked attempt.
- Recheck active status, verified employment, completed service, amount and required stored evidence on submission.
- Retain attributable HR review, committee endorsement/rejection and management decisions with stage/order/self-review safeguards.
- Preserve decision registers, application history, notifications, independent appeals and correction addenda.

## Exploration and operational requirements

- Provide role-scoped dashboards, decision searches, supported date/status filters, audit exploration, record details, CSV exports and printable views.
- Record explained anomaly flags and attributed investigation outcomes; keep mandatory policy checks distinct from advisory concerns.
- Track annual budget proposals/approval, allocation and approved commitments without claiming payment processing.
- Provide shared readable red UI components, keyboard focus, responsive layouts, reduced-motion support and clear success/error feedback.
- Support protected evidence, quotas/checksums, scanner integration, health/readiness, maintenance and coordinated backup/isolated restore checks.

## Boundaries and outstanding evaluation

Dynamic rules configure predefined mechanisms; there is no unrestricted expression builder. Data exploration combines supported fields rather than arbitrary SQL or a general report designer. Advisory anomalies are not proof of fraud. The optional amount model is experimental and disabled by default; no chatbot is implemented.

Real provider linking/sign-in, SMTP delivery, an operational private scanner, production hosting/security, representative institutional UAT, broad accessibility and sustained concurrent-load evaluation require further configuration/testing. Targeted HR-assignment notification and a complete additional-information-request workflow remain incomplete. Banks, payroll/ERP integrations and payment/disbursement recording are outside scope.

See [verification](VERIFICATION.md) for the evidence and [operating guide](OPERATING_GUIDE.md) for practical controls. Local test success is not a production certification or a measured institutional benefit.
