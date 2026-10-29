# 🚀 Welfare System - Quick Start Guide

## ✅ What's Ready

### Complete Features:
1. ✅ **Registration** - Beautiful 2-step process with validation
2. ✅ **Login** - Split-screen design with animations
3. ✅ **Admin Dashboard** - Matching your design image
4. ✅ **Employee Dashboard** - Status tracking with rejection reasons
5. ✅ **Review System** - Approve/Reject modals
6. ✅ **Database Schema** - Ready to populate

## 🎯 Start Testing in 3 Steps

### Step 1: Setup Database (5 minutes)

**Option A: Run SQL File**
```bash
# Open psql
psql -U postgres -d WelfareDb

# Run the SQL file
\i C:/Users/morny/Desktop/WelfareSystem/database-setup.sql
```

**Option B: Manual Copy-Paste**
1. Open pgAdmin or psql
2. Connect to WelfareDb
3. Open `database-setup.sql`
4. Copy all and execute

**What gets created:**
- 8 Departments (Flight Ops, Cabin Crew, etc.)
- 6 Welfare Programs (Medical, Emergency, Education, etc.)
- 5 Roles (Employee, HR, Committee, Management, Admin)
- 1 Test User (employee@welfare.edu / Password123!)

### Step 2: Start Backend (2 minutes)

```bash
# Navigate to backend
cd C:\Users\morny\Desktop\WelfareSystem\backend\WelfareSystem.Api

# Run the API
dotnet run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5249
info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

**Leave this terminal open!**

### Step 3: Start Frontend (2 minutes)

Open a **NEW terminal**:

```bash
# Navigate to frontend
cd C:\Users\morny\Desktop\WelfareSystem\frontend

# Run Next.js
npm run dev
```

**Expected Output:**
```
▲ Next.js 16.4.3 (Turbopack)
- Local:        http://localhost:3000
✓ Ready in 2s
```

## 🎉 Test the System

### Test 1: Registration (New User)

1. Open: **http://localhost:3000/register**

2. **Step 1 - Personal Info:**
   - First Name: `John`
   - Last Name: `Doe`
   - Email: `john.doe@example.com`
   - Employee ID: `EMP123` (must be unique)
   - Department: `Flight Operations`
   - Password: `Password123!`
   - Confirm Password: `Password123!`
   - Hire Date: Leave blank or fill
   - Click **Continue**

3. **Step 2 - Review:**
   - Check your information
   - Check "I agree to Terms & Conditions"
   - Click **Create Account**

4. **Success!**
   - See red circle animation
   - White checkmark draws
   - Click **"Open my dashboard"**

### Test 2: Login (Test Account)

1. Open: **http://localhost:3000/login**

2. Use test account:
   - Email: `employee@welfare.edu`
   - Password: `Password123!`
   - Click **"Open my account"**

3. **Redirects to Employee Dashboard**

### Test 3: Employee Dashboard

**What you'll see:**
- Welcome banner with your name
- Quick stats (Applications, Approved, Pending)
- List of your applications
- Status badges with colors
- **Rejection reasons** (if any rejected applications)
- Apply button

### Test 4: Admin Dashboard (Coming Soon)

To test admin features:
1. You'll need an admin account
2. Or manually update your role in database:

```sql
-- Update your user to admin role
UPDATE "Users" 
SET "RoleId" = 5 
WHERE "Email" = 'john.doe@example.com';
```

3. Logout and login again
4. Should redirect to `/admin/dashboard`

**Admin Dashboard Features:**
- Statistics cards (Members, Balance, Claims, Paid)
- Claims table with Review/Reject buttons
- Filter by category
- Search functionality
- Contributions chart

## 📋 URL Reference

| Page | URL | Who Can Access |
|------|-----|----------------|
| Registration | http://localhost:3000/register | Everyone |
| Login | http://localhost:3000/login | Everyone |
| Employee Dashboard | http://localhost:3000/employee/dashboard | Employee role |
| Admin Dashboard | http://localhost:3000/admin/dashboard | Admin role |
| HR Dashboard | http://localhost:3000/hr/dashboard | HR role (TBD) |

## 🎨 Test Accounts

After running `database-setup.sql`:

| Email | Password | Role |
|-------|----------|------|
| employee@welfare.edu | Password123! | Employee |
| admin@welfare.edu | Password123! | Administrator |
| hr@welfare.edu | Password123! | HR Officer |

*Note: Only employee account is guaranteed in the SQL file*

## 🔧 Troubleshooting

### Backend won't start
**Error:** `Port 5249 already in use`
```bash
# Find and kill the process
netstat -ano | findstr :5249
taskkill /PID <PID_NUMBER> /F

# Then restart
dotnet run
```

### Frontend won't start
**Error:** `Port 3000 already in use`
```bash
# Kill existing Next.js
taskkill /F /IM node.exe

# Then restart
npm run dev
```

### Registration fails (400 error)
**Most likely:** Departments don't exist in database

**Fix:** Run the `database-setup.sql` file

### Login fails
**Check:**
1. Backend is running (http://localhost:5249)
2. Email/password are correct
3. User exists in database

### Can't see rejection reasons
**Make sure:**
1. Application status is 9 (Rejected)
2. `rejectionReason` field has data
3. Check employee dashboard, not admin

## 📁 Project Structure

```
WelfareSystem/
├── backend/
│   └── WelfareSystem.Api/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── ApplicationsController.cs
│       │   └── ...
│       ├── Models/
│       ├── Services/
│       └── Program.cs
│
├── frontend/
│   ├── app/
│   │   ├── register/
│   │   │   └── page.tsx ✨ NEW
│   │   ├── login/
│   │   │   └── page.tsx ✨ UPDATED
│   │   ├── employee/
│   │   │   └── dashboard/
│   │   │       └── page.tsx ✨ NEW
│   │   └── admin/
│   │       └── dashboard/
│   │           └── page.tsx ✨ NEW
│   ├── components/
│   ├── lib/
│   │   ├── api/
│   │   └── auth/
│   └── ...
│
└── database-setup.sql ✨ READY
```

## 🎯 What Works Right Now

### ✅ Registration
- 2-step process
- Real-time validation
- Success animation
- Redirects to login

### ✅ Login
- Beautiful design
- Test account works
- Redirects to dashboard

### ✅ Employee Dashboard
- Shows applications
- Status tracking
- Rejection reasons display
- Apply button

### ✅ Admin Dashboard
- Statistics cards
- Claims table
- Review modal
- Reject modal with reason
- Search and filters
- Charts

## 🚧 What's Mock Data (For Now)

The dashboards use **mock data** currently. To connect real data:

1. Implement API endpoints in backend
2. Update frontend to call APIs
3. Handle loading states
4. Add error handling

**Files to update:**
- `frontend/app/admin/dashboard/page.tsx` (lines 40-70)
- `frontend/app/employee/dashboard/page.tsx` (lines 25-50)

## 📞 Need Help?

**Common Issues:**
1. White screen → Check browser console (F12)
2. API errors → Check backend terminal
3. 404 errors → Check URLs and routes
4. Database errors → Run `database-setup.sql`

**Check These:**
- ✅ PostgreSQL is running
- ✅ Database `WelfareDb` exists
- ✅ Backend is running on port 5249
- ✅ Frontend is running on port 3000
- ✅ No console errors

## 🎉 You're All Set!

The system is ready to test. Start with registration, then explore the dashboards!

**Recommended Flow:**
1. Register new user
2. Login
3. See employee dashboard
4. Apply for program (if available)
5. Test admin dashboard (after role change)
6. Review/Reject applications
7. Check employee sees rejection reason

Enjoy! 🚀
