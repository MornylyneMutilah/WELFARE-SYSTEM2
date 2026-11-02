# Administrator user management

Updated 2 November 2026.

## Using the page

Sign in with an Administrator account and open **Users** at http://localhost:3000/admin/users.

- Search by name, email, employee ID, or department; filter by role or account status.
- **Add user** creates an active account with a selected role, department, verified hire date, and initial password. Initial passwords must be shared privately; automated invitations and forced first-login password changes are not implemented.
- **Edit** changes personal details, employee ID, role, department, hire date, or active status. Enter a reason, verify the details, check the confirmation box, then save.
- **Suspend** prevents new sign-ins and invalidates existing access and refresh tokens. The account, welfare applications, reviews, and history remain stored.
- Filter to **Suspended**, then choose **Reactivate** to restore access. The user must sign in again; old sessions remain invalid.
- **History** shows administrative actions, the actor, timestamp, reason, and before/after field changes. Older changes made before auditing was added may not have entries.
- The page refreshes after successful changes, on tab focus, and within 10 seconds while visible. An open form retains its original snapshot and rejects a conflicting save rather than overwriting another administrator's changes.

Use the real employment record for hire-date corrections. New application eligibility assessments use that stored date. Already-saved application assessments are not automatically recalculated by an account edit.

## Safeguards

- Only administrators can use administrative mutation endpoints; the backend rechecks the current database role.
- Administrators cannot suspend themselves or remove their own administrator role.
- The last active administrator cannot be suspended or demoted. User administration uses a shared database transaction lock so concurrent changes cannot bypass the checks.
- Role requests use the same guarded update service for approval; role assignment invalidates the affected user's previous sessions.
- Unknown departments/roles, duplicate email addresses/employee IDs, invalid or future hire dates, and missing change reasons are rejected.
- Ordinary profile editing cannot alter role, department, account status, employee ID, or hire date.
- Passwords, tokens, and password hashes are excluded from user DTOs and audit snapshots.
- Suspension is a reversible status change, not account deletion. No bulk or permanent delete operation is provided.

## Session handling and migration

Migration `20261102081019_AddUserTokenVersion` adds a default-zero `TokenVersion` column to Users. It was applied locally and preserves existing accounts and records. Existing active version-zero access tokens without a version claim remain compatible.

The JWT bearer validation event checks the current user's active status, session version, and role on each authenticated API request. Suspension or a role/department change increments the version. Refresh token exchange also checks the version and active status. Refresh tokens cannot be used as access tokens, and access tokens cannot be used as refresh tokens.

Apply migrations to another environment before starting the updated backend:

```powershell
dotnet ef database update --project backend/WelfareSystem.Api
```

## Verification performed

- Backend compilation passed with zero warnings/errors.
- Frontend lint and production build passed.
- All **94 PostgreSQL regression checks** passed. Fixtures were rolled back, including the temporary account created/suspended/reactivated by tests; existing accounts were not administratively changed by those tests.
- HTTP checks: administrator can list users/options; the live response includes 5 roles and 8 departments with no missing role names. Anonymous options access returns 401, HR user management returns 403, and presenting a refresh token as a bearer token returns 401.
- Browser checks: live user table, populated edit form, search filtering, disabled self-suspension, and reason/confirmation requirements for status changes. Edit and suspension panels were cancelled without saving changes to existing users.

Regression command, from the repository root:

```powershell
dotnet run --project backend/WelfareSystem.RegressionChecks -- backend/WelfareSystem.Api/appsettings.Local.json
```

The ignored local configuration supplies the local database connection. Do not commit credentials. The runner rolls back fixture data but PostgreSQL identity sequences may advance.

## Main implementation files

- `frontend/app/admin/users/page.tsx` and `frontend/lib/api/users.ts`.
- `Controllers/UsersController.cs`, `Controllers/RoleRequestsController.cs`.
- `Services/UserAdministrationService.cs`, `Services/AccountTokenValidator.cs`, and JWT validation in `Program.cs`.
- User DTOs, User.TokenVersion, UserRepository eager loading, and the token-version migration.
- `AuthService`, `TokenService`, and the existing external-auth token issuance include session versions. External providers remain unconfigured; successful provider sign-in has not been verified.
