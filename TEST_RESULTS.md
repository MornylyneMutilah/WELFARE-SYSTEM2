# Test Results - Sprint II

## Test Execution Log

### Test Case 1: User Registration
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Navigate to `http://localhost:3000/register`
2. Fill in registration form with valid data
3. Click "Create account" button
4. Verify user is created in database
5. Verify login is possible with new credentials

**Expected Result**: User successfully registered and redirected to dashboard

**Actual Result**: [PENDING - To be executed]

**Status**: ⏳ Pending

**Corrective Action**: [N/A]

---

### Test Case 2: User Login
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Navigate to `http://localhost:3000/login`
2. Enter valid email and password
3. Click "Sign in" button
4. Verify JWT token is received
5. Verify redirect to employee dashboard
6. Verify user details displayed in header

**Expected Result**: User successfully logged in and redirected to dashboard

**Actual Result**: [PENDING - To be executed]

**Status**: ⏳ Pending

**Corrective Action**: [N/A]

---

### Test Case 3: Invalid Login Credentials
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Navigate to `http://localhost:3000/login`
2. Enter invalid email or password
3. Click "Sign in" button
4. Verify error message displayed
5. Verify no redirect occurs

**Expected Result**: Appropriate error message shown, user stays on login page

**Actual Result**: [PENDING - To be executed]

**Status**: ⏳ Pending

**Corrective Action**: [N/A]

---

### Test Case 4: Database Connectivity
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Start backend server
2. Check database connection logs
3. Verify data persists after restart
4. Test CRUD operations on Users table

**Expected Result**: Database connection successful, data persists

**Actual Result**: ✅ PASSED - Backend connects to WelfareDb successfully, seed data inserted

**Status**: ✅ Passed

**Corrective Action**: [N/A]

---

### Test Case 5: API Communication
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Test GET /api/departments
2. Test GET /api/welfare-programs
3. Test POST /api/auth/login
4. Test POST /api/auth/register
5. Verify response format and status codes

**Expected Result**: All endpoints return correct responses

**Actual Result**: [PENDING - To be executed]

**Status**: ⏳ Pending

**Corrective Action**: [N/A]

---

### Test Case 6: Role-Based Access Control
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Login as Employee (role: Employee)
2. Try to access admin-only endpoints
3. Login as Admin (role: Administrator)
4. Verify admin can access restricted endpoints

**Expected Result**: Employee denied access, Admin granted access

**Actual Result**: [PENDING - To be executed]

**Status**: ⏳ Pending

**Corrective Action**: [N/A]

---

### Test Case 7: Welfare Programs Display
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Login as employee
2. Navigate to /employee/programs
3. Verify programs are displayed
4. Verify program details (name, description, max amount)

**Expected Result**: All welfare programs displayed correctly

**Actual Result**: [PENDING - To be executed]

**Status**: ⏳ Pending

**Corrective Action**: [N/A]

---

### Test Case 8: Application Submission
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Login as employee
2. Navigate to /employee/applications/new
3. Select welfare program
4. Enter request amount and reason
5. Submit application
6. Verify application saved in database

**Expected Result**: Application successfully created and visible in applications list

**Actual Result**: [PENDING - To be executed]

**Status**: ⏳ Pending

**Corrective Action**: [N/A]

---

### Test Case 9: Invalid Input Validation
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Try to register with invalid email format
2. Try to register with password < 8 characters
3. Try to submit empty required fields
4. Verify validation errors displayed

**Expected Result**: Validation errors shown for each invalid input

**Actual Result**: [PENDING - To be executed]

**Status**: ⏳ Pending

**Corrective Action**: [N/A]

---

### Test Case 10: UI Responsiveness
**Date**: 2026-10-19
**Tester**: System

**Test Steps**:
1. Open application in browser
2. Test on different screen sizes (mobile, tablet, desktop)
3. Verify buttons are visible and clickable
4. Verify text is readable

**Expected Result**: UI responsive across all screen sizes

**Actual Result**: [PENDING - To be executed]

**Status**: ⏳ Pending

**Corrective Action**: [N/A]

---

## Test Summary

**Total Test Cases**: 10
**Passed**: 1
**Failed**: 0
**Pending**: 9

**Pass Rate**: 10%

## Defects Found
None yet (testing in progress)

## Known Limitations
1. Social login buttons are UI only (no OAuth implementation)
2. No unit/integration tests
3. Passwords in appsettings (needs environment variables)
4. No HTTPS configured
