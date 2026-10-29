-- =====================================================
-- Manual Database Population for Welfare System
-- =====================================================
-- Run these queries in your PostgreSQL database (WelfareDb)
-- =====================================================

-- 1. INSERT DEPARTMENTS
-- =====================================================
INSERT INTO "Departments" ("Name", "Code", "Description")
VALUES 
    ('Flight Operations', 'OPS', 'Flight deck crew and flight operations'),
    ('Cabin Crew', 'CABIN', 'In-flight service and cabin operations'),
    ('Engineering & Maintenance', 'ENG', 'Aircraft maintenance and engineering'),
    ('Finance & Accounts', 'FIN', 'Financial planning and accounting'),
    ('Human Resources', 'HR', 'Personnel management and employee relations'),
    ('IT & Digital', 'IT', 'Information technology and digital services'),
    ('Commercial & Marketing', 'COM', 'Sales, marketing and commercial operations'),
    ('Ground Operations', 'GND', 'Airport ground services and operations');

-- 2. VERIFY DEPARTMENTS
-- =====================================================
SELECT * FROM "Departments" ORDER BY "Id";

-- 3. INSERT WELFARE PROGRAMS
-- =====================================================
INSERT INTO "WelfarePrograms" ("Name", "Description", "Category", "MaxAmount", "MinServicePeriod", "RequiredDocuments", "IsActive", "CreatedAt", "UpdatedAt")
VALUES 
    ('Medical Assistance', 
     'Financial assistance for medical expenses for employees and immediate family',
     'Medical',
     5000.00,
     6,
     '["Medical Report", "Receipts", "Doctor''s Recommendation"]',
     true,
     NOW(),
     NOW()),
     
    ('Emergency Financial Assistance',
     'Emergency financial support for unexpected situations',
     'Emergency',
     3000.00,
     12,
     '["Proof of Emergency", "Supporting Documentation"]',
     true,
     NOW(),
     NOW()),
     
    ('Professional Development Support',
     'Support for aviation training, certifications, and professional development',
     'Education',
     2000.00,
     12,
     '["Course Details", "Fee Structure", "Training Approval"]',
     true,
     NOW(),
     NOW()),
     
    ('Employee Loan',
     'Low-interest loans for personal needs',
     'Loan',
     10000.00,
     24,
     '["Loan Application", "Repayment Plan"]',
     true,
     NOW(),
     NOW()),
     
    ('Bereavement Support',
     'Support for bereaved employees and their families',
     'Bereavement',
     1500.00,
     3,
     '["Death Certificate", "Relationship Proof"]',
     true,
     NOW(),
     NOW()),
     
    ('Housing Allowance Support',
     'Additional support for housing and accommodation',
     'Housing',
     4000.00,
     18,
     '["Lease Agreement", "Proof of Residence"]',
     true,
     NOW(),
     NOW());

-- 4. VERIFY WELFARE PROGRAMS (Optional)
-- =====================================================
SELECT "Id", "Name", "Category", "MaxAmount", "IsActive" 
FROM "WelfarePrograms" 
ORDER BY "Id";

-- 5. CHECK ROLES (Should already exist from migrations)
-- =====================================================
-- Roles are seeded in the AppDbContext.OnModelCreating
-- They should already exist. Verify:
SELECT * FROM "Roles" ORDER BY "Id";

-- If Roles don't exist, run this:
-- =====================================================
INSERT INTO "Roles" ("Id", "Name", "Description")
VALUES 
    (1, 'Employee', 'Regular employee'),
    (2, 'HROfficer', 'HR Officer'),
    (3, 'CommitteeMember', 'Welfare Committee Member'),
    (4, 'Management', 'Institutional Management'),
    (5, 'Administrator', 'System Administrator')
ON CONFLICT ("Id") DO NOTHING;

-- 6. CREATE A TEST USER (Optional)
-- =====================================================
-- Password: Password123! (hashed with BCrypt)
-- This creates an employee account you can login with
INSERT INTO "Users" 
    ("Email", "PasswordHash", "FirstName", "LastName", "EmployeeId", "HireDate", "DepartmentId", "RoleId", "IsActive", "CreatedAt", "UpdatedAt")
VALUES 
    ('employee@welfare.edu',
     '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.8TJGUy',
     'Test',
     'Employee',
     'EMP999',
     NOW() - INTERVAL '1 year',
     1,
     1,
     true,
     NOW(),
     NOW())
ON CONFLICT ("Email") DO NOTHING;

-- 7. VERIFY TEST USER (Optional)
-- =====================================================
SELECT "Id", "Email", "FirstName", "LastName", "EmployeeId", "RoleId", "DepartmentId", "IsActive"
FROM "Users"
WHERE "Email" = 'employee@welfare.edu';

-- =====================================================
-- SUMMARY OF WHAT WAS INSERTED
-- =====================================================
-- Run this to see counts:
SELECT 
    'Departments' as "Table", COUNT(*) as "Count" FROM "Departments"
UNION ALL
SELECT 
    'Roles' as "Table", COUNT(*) as "Count" FROM "Roles"
UNION ALL
SELECT 
    'Welfare Programs' as "Table", COUNT(*) as "Count" FROM "WelfarePrograms"
UNION ALL
SELECT 
    'Users' as "Table", COUNT(*) as "Count" FROM "Users";
