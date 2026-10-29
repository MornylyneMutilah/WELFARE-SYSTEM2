# Authentication Fix Checklist

## âœ… Fixes Applied

The following issues have been fixed in the backend code:

### 1. Navigation Properties Not Loading âœ…
- **File:** `backend/WelfareSystem.Api/Repositories/UserRepository.cs`
- **Fix:** Added override for `GetByIdAsync` to include Role and Department
- **Impact:** Prevents NullReferenceException when accessing user.Role.Name

### 2. Registration Not Loading Relations âœ…
- **File:** `backend/WelfareSystem.Api/Services/AuthService.cs`
- **Fix:** Reloads user with navigation properties after creation
- **Impact:** Registration now returns complete user data with role and department

### 3. Unnecessary Department Lookup âœ…
- **File:** `backend/WelfareSystem.Api/Services/AuthService.cs`
- **Fix:** Uses already-loaded Department navigation property in RefreshTokenAsync
- **Impact:** Performance improvement, prevents extra database queries

### 4. Login Password Validation âœ…
- **File:** `backend/WelfareSystem.Api/Models/DTOs/Auth/LoginRequest.cs`
- **Fix:** Removed MinLength validation (only needed for registration)
- **Impact:** Allows login with any password (even short ones from before validation)

---

## ðŸš€ Next Steps to Test

### Step 1: Restart the Backend
```bash
# Stop the current backend process (Ctrl+C if running in terminal)
# Then restart it:
cd backend/WelfareSystem.Api
dotnet run
```

**Expected Output:**
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5249
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Step 2: Verify Database Has Roles
```bash
# Open PostgreSQL and check roles exist
psql -U postgres -d WelfareDb

# In psql:
SELECT * FROM "Roles";
```

**Expected Output:**
```
 Id |     Name        |        Description        
----+-----------------+---------------------------
  1 | Employee        | Regular employee
  2 | HROfficer       | HR Officer
  3 | CommitteeMember | Welfare Committee Member
  4 | Management      | Institutional Management
  5 | Administrator   | System Administrator
```

If roles don't exist, run:
```bash
cd backend/WelfareSystem.Api
dotnet ef database update
```

### Step 3: Test Registration Flow
1. Open frontend: http://localhost:3000/register
2. Fill in the form:
   - **Email:** test@example.com
   - **Password:** Password123!
   - **First Name:** Test
   - **Last Name:** User
   - **Employee ID:** EMP001
   - **Department:** Flight Operations (or any)
   - **Hire Date:** (leave blank or fill)
3. Click "Create account"
4. **Expected:** Redirect to `/employee/dashboard`

**Check Browser Console:** Should see no errors

**Check Browser DevTools > Application > Local Storage:**
- `token`: Should contain JWT token
- `refreshToken`: Should contain refresh token
- `user`: Should contain user object with role and department

### Step 4: Test Login Flow
1. Navigate to: http://localhost:3000/login
2. Enter credentials:
   - **Email:** test@example.com
   - **Password:** Password123!
3. Click "Sign in"
4. **Expected:** Redirect to `/employee/dashboard`

**Check Browser Console:** Should see no errors

---

## ðŸ› If Issues Persist

### Check Backend Logs
Look for errors in the terminal where `dotnet run` is executing:
- NullReferenceException â†’ Navigation properties still not loading
- 401 Unauthorized â†’ JWT configuration issue
- 500 Internal Server Error â†’ Check exception details

### Check Frontend Network Tab
Open DevTools > Network tab and look for:
- `/api/auth/register` or `/api/auth/login` request
- Status code (should be 200)
- Response body (should contain token and user)
- Error messages in response

### Common Issues and Solutions

#### Issue: "Registration failed. Email or Employee ID may already exist."
**Solution:** The user already exists. Try a different email/employee ID, or delete the existing user:
```sql
DELETE FROM "Users" WHERE "Email" = 'test@example.com';
```

#### Issue: "Invalid email or password"
**Solution:** 
- Check if user exists in database
- Verify password was hashed correctly
- Check if user.IsActive is true

#### Issue: CORS error in browser console
**Solution:** Backend CORS is configured for http://localhost:3000
- Verify frontend is running on port 3000
- Check `Program.cs` CORS configuration

#### Issue: "Cannot read properties of null (reading 'Name')"
**Solution:** Navigation properties still not loading
- Restart backend to ensure new code is running
- Check database has foreign key relationships set up correctly

---

## ðŸ“ Testing Matrix

| Test Case | Status | Notes |
|-----------|--------|-------|
| Register new user | â¬œ Pending | Test with unique email/employee ID |
| Login with registered user | â¬œ Pending | Use credentials from registration |
| Login with invalid credentials | â¬œ Pending | Should show error message |
| Register with duplicate email | â¬œ Pending | Should show error message |
| Refresh token | â¬œ Pending | Token should be refreshed successfully |
| Access protected route | â¬œ Pending | Dashboard should be accessible after login |

---

## ðŸ”§ Backend Configuration Check

Verify these settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=WelfareDb;Username=postgres"
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-at-least-32-characters-long-for-security",
    "Issuer": "WelfareSystem",
    "Audience": "WelfareSystemUsers",
    "ExpirationMinutes": 60
  }
}
```

Verify frontend `.env.local`:
```
NEXT_PUBLIC_API_URL=http://localhost:5249/api
```

---

## ðŸ“ž Support

If you encounter issues:
1. Check backend terminal for error logs
2. Check browser console for frontend errors
3. Check Network tab for API response details
4. Verify database contains roles and departments
5. Ensure both frontend and backend are running

**All backend code fixes have been applied!** ðŸŽ‰

Now restart the backend and test the registration and login flows.
