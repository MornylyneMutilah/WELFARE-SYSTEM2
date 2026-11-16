# Administrator user management

Open **Administrator → Users**. Administrative writes require the current server-verified Administrator role, a reason and the relevant confirmation; hiding navigation is not the security boundary.

## Create and maintain accounts

- Add an account with valid details, a unique email and employee ID, a permitted role and department, and an employment start date.
- Verify the hire date against authoritative institutional records. Service eligibility uses completed calendar months; migration and public registration do not invent verification.
- Edit details, role, department, hire date and active status. Conflicting updates require refresh rather than silently overwriting a newer change.
- New administrator-provisioned accounts must replace their initial password. Eligible accounts can receive an expiring setup invitation. Development delivery uses the private outbox unless SMTP is configured.
- Public self-registration defaults to Employee; organizational access requests require trusted approval. Browser-supplied role names are not authority.

Changing a hire date clears its previous verification unless the Administrator explicitly confirms the corrected date. Keep real institutional data separate from demonstration fixtures.

## Suspend and reactivate

Suspension changes active status without deleting applications or historical decisions. It blocks subsequent authenticated requests and new sign-ins, invalidates the affected session version and prevents mandatory submission checks from passing. Reactivation permits a fresh sign-in; it does not revive previously revoked tokens.

Role and department changes also invalidate affected sessions. The API prevents self-suspension, self-demotion and removal of the last active Administrator through the supported management flow. Other accounts' original records remain retained.

## Sessions during demonstrations

Sessions are isolated per browser tab. Sign in as Administrator in one tab and Employee in another; they retain separate access/refresh tokens and profile state. Refresh preserves a tab's current session, while logout clears only that tab. A newly opened tab can require sign-in. Server-side account revocation still applies across all affected tokens.

If an older tab shows `403` after switching accounts, hard-refresh and sign in to its intended role once. Program saves require Administrator access; HR, committee and management do not inherit that permission.

## Audit and recovery

Account changes record actor, reason and suitable before/after values. User DTOs and audit snapshots exclude passwords, password hashes and authentication tokens. Use **Audit** to filter exact actions and the `User` entity type.

**Forgot your password?**, verification and setup invitations use expiring, hashed, single-use challenges. See the operating guide for SMTP, outbox, lifetime and deployment configuration. An administrator does not need to delete an account to suspend it or reset its access workflow.

## Implementation and checks

- Frontend: `app/admin/users/page.tsx`, `lib/api/users.ts`, `lib/auth/tokenUtils.ts`.
- API: `UsersController`, `RoleRequestsController`, `UserAdministrationService`, `AccountTokenValidator`, token services and JWT validation.
- Persistence: `User.TokenVersion`, employment-verification/security flags and additive migrations.
- Verification: the backend regression runner covers role requests, validation, stale edits, authority revocation, last-administrator safeguards and reactivation. Frontend session tests cover tab isolation, refresh, migration, logout and rejected-session handling.

Google and GitHub credentials are private deployment settings; complete live provider linking/sign-in still requires verification. See [social sign-in](SOCIAL_SIGN_IN.md).
