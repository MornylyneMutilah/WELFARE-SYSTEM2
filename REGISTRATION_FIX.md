# Registration 400 Error - Debugging Guide

## Current Issue

Registration is failing with HTTP 400 Bad Request error.

## Possible Causes

### 1. Department Not Found (Most Likely)
The registration might be failing because the department with ID 1 doesn't exist in the database yet.

**Solution:** The backend needs to be running to seed the database.

### 2. Validation Errors
The request might have validation errors.

## How to Debug

### Step 1: Check Backend is Running

Open a new terminal and run:
```bash
cd backend/WelfareSystem.Api
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5249
info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

The backend will automatically seed:
- Departments
- Roles  
- Welfare Programs
- Test users

### Step 2: Verify Departments Exist

Open your browser and navigate to:
```
http://localhost:5249/api/departments
```

You should see a JSON response with 8 departments.

### Step 3: Test Registration Manually

Use a tool like Postman or curl:

```bash
curl -X POST http://localhost:5249/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@example.com",
    "password": "Password123!",
    "firstName": "Test",
    "lastName": "User",
    "employeeId": "TEST123",
    "departmentId": 1,
    "hireDate": null
  }'
```

**If successful, you'll get:**
```json
{
  "token": "...",
  "refreshToken": "...",
  "user": {
    "id": 13,
    "email": "newuser@example.com",
    ...
  }
}
```

**If it fails, you'll see validation errors:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "DepartmentId": ["The value '1' is not valid."]
  }
}
```

### Step 4: Check Browser Console

In the browser console, you should now see detailed error messages:
```
Sending registration data: {...}
Registration error: ...
Response data: {...}
```

This will tell you exactly what's wrong.

## Common Solutions

### Solution 1: Ensure Backend is Running
```bash
cd backend/WelfareSystem.Api
dotnet run
```

Wait for: "Now listening on: http://localhost:5249"

### Solution 2: Check Department IDs

If departments don't exist, they will be created when the backend starts.

The seeded departments are:
1. Flight Operations (ID: 1)
2. Cabin Crew (ID: 2)
3. Engineering & Maintenance (ID: 3)
4. Finance & Accounts (ID: 4)
5. Human Resources (ID: 5)
6. IT & Digital (ID: 6)
7. Commercial & Marketing (ID: 7)
8. Ground Operations (ID: 8)

### Solution 3: Use Existing Test User

Instead of registering, try logging in with one of the seeded test users:

**Employee Account:**
- Email: `employee@welfare.edu`
- Password: `Password123!`

**Admin Account:**
- Email: `admin@welfare.edu`
- Password: `Password123!`

## Updated Registration Flow

With the latest changes:

1. ✅ Fill registration form
2. ✅ Submit (with better error messages)
3. ✅ See detailed error if it fails (check console)
4. ✅ If successful: Alert + Redirect to login
5. ✅ Login manually
6. ✅ Access dashboard

## Testing Steps

1. **Start Backend:**
   ```bash
   cd backend/WelfareSystem.Api
   dotnet run
   ```

2. **Wait for seeding to complete** (check console logs)

3. **Open Frontend:** http://localhost:3000/register

4. **Fill in form:**
   - Email: unique@example.com
   - Password: Password123!
   - First Name: Test
   - Last Name: User
   - Employee ID: UNIQUE123 (must be unique)
   - Department: Select any
   - Hire Date: Leave blank or fill

5. **Submit and check browser console for errors**

6. **If still failing, check:**
   - Backend logs for errors
   - Browser Network tab for response details
   - Browser Console for detailed error message

## Quick Fix - Use Test Account

If registration keeps failing, just login with a test account:

Go to: http://localhost:3000/login

Use:
- Email: `employee@welfare.edu`
- Password: `Password123!`

This will let you test the rest of the system while we debug registration.

## Next Steps

1. Ensure backend is running on port 5249
2. Check browser console for detailed error message
3. Share the console output to debug further
4. Or use test accounts to access the system
