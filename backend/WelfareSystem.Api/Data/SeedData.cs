using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Models.Entities;

namespace WelfareSystem.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if data already exists
        if (context.Departments.Any())
        {
            return; // Database has been seeded
        }

        // Seed Departments - Kenya Airways Departments
        var departments = new List<Department>
        {
            new Department { Name = "Flight Operations", Code = "OPS", Description = "Flight deck crew and flight operations" },
            new Department { Name = "Cabin Crew", Code = "CABIN", Description = "In-flight service and cabin operations" },
            new Department { Name = "Engineering & Maintenance", Code = "ENG", Description = "Aircraft maintenance and engineering" },
            new Department { Name = "Finance & Accounts", Code = "FIN", Description = "Financial planning and accounting" },
            new Department { Name = "Human Resources", Code = "HR", Description = "Personnel management and employee relations" },
            new Department { Name = "IT & Digital", Code = "IT", Description = "Information technology and digital services" },
            new Department { Name = "Commercial & Marketing", Code = "COM", Description = "Sales, marketing and commercial operations" },
            new Department { Name = "Ground Operations", Code = "GND", Description = "Airport ground services and operations" }
        };

        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();

        // Note: Roles are already seeded in AppDbContext.OnModelCreating

        // Get role IDs
        var employeeRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Employee");
        var hrRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "HROfficer");
        var committeeRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "CommitteeMember");
        var managementRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Management");
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");

        // Seed Users
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        var users = new List<User>
        {
            // Administrator
            new User
            {
                Email = "admin@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "Admin",
                LastName = "User",
                EmployeeId = "ADMIN001",
                HireDate = DateTime.UtcNow.AddYears(-5),
                DepartmentId = departments[0].Id,
                RoleId = adminRole!.Id,
                IsActive = true
            },
            // HR Officers
            new User
            {
                Email = "hr1@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "Jane",
                LastName = "Smith",
                EmployeeId = "HR001",
                HireDate = DateTime.UtcNow.AddYears(-3),
                DepartmentId = departments[0].Id,
                RoleId = hrRole!.Id,
                IsActive = true
            },
            new User
            {
                Email = "hr2@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "John",
                LastName = "Doe",
                EmployeeId = "HR002",
                HireDate = DateTime.UtcNow.AddYears(-2),
                DepartmentId = departments[1].Id,
                RoleId = hrRole!.Id,
                IsActive = true
            },
            // Committee Members
            new User
            {
                Email = "committee1@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "Mary",
                LastName = "Johnson",
                EmployeeId = "COM001",
                HireDate = DateTime.UtcNow.AddYears(-4),
                DepartmentId = departments[2].Id,
                RoleId = committeeRole!.Id,
                IsActive = true
            },
            new User
            {
                Email = "committee2@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "Robert",
                LastName = "Williams",
                EmployeeId = "COM002",
                HireDate = DateTime.UtcNow.AddYears(-5),
                DepartmentId = departments[3].Id,
                RoleId = committeeRole!.Id,
                IsActive = true
            },
            // Management
            new User
            {
                Email = "management@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "David",
                LastName = "Brown",
                EmployeeId = "MGT001",
                HireDate = DateTime.UtcNow.AddYears(-6),
                DepartmentId = departments[0].Id,
                RoleId = managementRole!.Id,
                IsActive = true
            },
            // Employees
            new User
            {
                Email = "employee1@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "Emily",
                LastName = "Davis",
                EmployeeId = "EMP001",
                HireDate = DateTime.UtcNow.AddYears(-2),
                DepartmentId = departments[0].Id,
                RoleId = employeeRole!.Id,
                IsActive = true
            },
            new User
            {
                Email = "employee2@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "Michael",
                LastName = "Miller",
                EmployeeId = "EMP002",
                HireDate = DateTime.UtcNow.AddMonths(-6),
                DepartmentId = departments[1].Id,
                RoleId = employeeRole!.Id,
                IsActive = true
            },
            new User
            {
                Email = "employee3@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "Sarah",
                LastName = "Wilson",
                EmployeeId = "EMP003",
                HireDate = DateTime.UtcNow.AddMonths(-18),
                DepartmentId = departments[2].Id,
                RoleId = employeeRole!.Id,
                IsActive = true
            },
            new User
            {
                Email = "employee4@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "James",
                LastName = "Taylor",
                EmployeeId = "EMP004",
                HireDate = DateTime.UtcNow.AddMonths(-3),
                DepartmentId = departments[3].Id,
                RoleId = employeeRole!.Id,
                IsActive = true
            },
            new User
            {
                Email = "employee5@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "Lisa",
                LastName = "Anderson",
                EmployeeId = "EMP005",
                HireDate = DateTime.UtcNow.AddMonths(-12),
                DepartmentId = departments[0].Id,
                RoleId = employeeRole!.Id,
                IsActive = true
            },
            // Simple test employee
            new User
            {
                Email = "employee@welfare.edu",
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "Employee",
                EmployeeId = "EMP999",
                HireDate = DateTime.UtcNow.AddYears(-1),
                DepartmentId = departments[0].Id,
                RoleId = employeeRole!.Id,
                IsActive = true
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Seed Welfare Programs - Kenya Airways Employee Welfare Programs
        var welfarePrograms = new List<WelfareProgram>
        {
            new WelfareProgram
            {
                Name = "Medical Assistance",
                Description = "Financial assistance for medical expenses for employees and immediate family",
                Category = "Medical",
                MaxAmount = 5000.00m,
                MinServicePeriod = 6,
                RequiredDocuments = "[\"Medical Report\", \"Receipts\", \"Doctor's Recommendation\"]",
                IsActive = true
            },
            new WelfareProgram
            {
                Name = "Emergency Financial Assistance",
                Description = "Emergency financial support for unexpected situations",
                Category = "Emergency",
                MaxAmount = 3000.00m,
                MinServicePeriod = 12,
                RequiredDocuments = "[\"Proof of Emergency\", \"Supporting Documentation\"]",
                IsActive = true
            },
            new WelfareProgram
            {
                Name = "Professional Development Support",
                Description = "Support for aviation training, certifications, and professional development",
                Category = "Education",
                MaxAmount = 2000.00m,
                MinServicePeriod = 12,
                RequiredDocuments = "[\"Course Details\", \"Fee Structure\", \"Training Approval\"]",
                IsActive = true
            },
            new WelfareProgram
            {
                Name = "Employee Loan",
                Description = "Low-interest loans for personal needs",
                Category = "Loan",
                MaxAmount = 10000.00m,
                MinServicePeriod = 24,
                RequiredDocuments = "[\"Loan Application\", \"Repayment Plan\"]",
                IsActive = true
            },
            new WelfareProgram
            {
                Name = "Bereavement Support",
                Description = "Support for bereaved employees and their families",
                Category = "Bereavement",
                MaxAmount = 1500.00m,
                MinServicePeriod = 3,
                RequiredDocuments = "[\"Death Certificate\", \"Relationship Proof\"]",
                IsActive = true
            },
            new WelfareProgram
            {
                Name = "Housing Allowance Support",
                Description = "Additional support for housing and accommodation",
                Category = "Housing",
                MaxAmount = 4000.00m,
                MinServicePeriod = 18,
                RequiredDocuments = "[\"Lease Agreement\", \"Proof of Residence\"]",
                IsActive = true
            }
        };

        await context.WelfarePrograms.AddRangeAsync(welfarePrograms);
        await context.SaveChangesAsync();

        // Seed Eligibility Rules
        var eligibilityRules = new List<EligibilityRule>
        {
            // Medical Assistance Rules
            new EligibilityRule
            {
                WelfareProgramId = welfarePrograms[0].Id,
                RuleName = "Minimum Service Period",
                RuleType = 1, // ServicePeriod
                RuleValue = "{\"months\": 6}",
                Description = "Employee must have at least 6 months of service",
                IsActive = true
            },
            new EligibilityRule
            {
                WelfareProgramId = welfarePrograms[0].Id,
                RuleName = "Amount Limit",
                RuleType = 2, // AmountLimit
                RuleValue = "{\"maxAmount\": 5000}",
                Description = "Requested amount must not exceed $5,000",
                IsActive = true
            },
            new EligibilityRule
            {
                WelfareProgramId = welfarePrograms[0].Id,
                RuleName = "Frequency Restriction",
                RuleType = 3, // Frequency
                RuleValue = "{\"months\": 12, \"maxApplications\": 1}",
                Description = "Maximum 1 application per 12 months",
                IsActive = true
            },
            // Emergency Financial Assistance Rules
            new EligibilityRule
            {
                WelfareProgramId = welfarePrograms[1].Id,
                RuleName = "Minimum Service Period",
                RuleType = 1, // ServicePeriod
                RuleValue = "{\"months\": 12}",
                Description = "Employee must have at least 12 months of service",
                IsActive = true
            },
            new EligibilityRule
            {
                WelfareProgramId = welfarePrograms[1].Id,
                RuleName = "Amount Limit",
                RuleType = 2, // AmountLimit
                RuleValue = "{\"maxAmount\": 3000}",
                Description = "Requested amount must not exceed $3,000",
                IsActive = true
            },
            // Education/Training Support Rules
            new EligibilityRule
            {
                WelfareProgramId = welfarePrograms[2].Id,
                RuleName = "Minimum Service Period",
                RuleType = 1, // ServicePeriod
                RuleValue = "{\"months\": 12}",
                Description = "Employee must have at least 12 months of service",
                IsActive = true
            },
            new EligibilityRule
            {
                WelfareProgramId = welfarePrograms[2].Id,
                RuleName = "Amount Limit",
                RuleType = 2, // AmountLimit
                RuleValue = "{\"maxAmount\": 2000}",
                Description = "Requested amount must not exceed $2,000",
                IsActive = true
            }
        };

        await context.EligibilityRules.AddRangeAsync(eligibilityRules);
        await context.SaveChangesAsync();

        // Seed Sample Applications
        var sampleApplications = new List<WelfareApplication>
        {
            new WelfareApplication
            {
                UserId = users[6].Id, // employee1
                WelfareProgramId = welfarePrograms[0].Id, // Medical Assistance
                Status = 8, // Approved
                RequestedAmount = 2500.00m,
                Reason = "Surgery for medical condition",
                SubmittedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-25),
                IsEligible = true,
                AnomalyFlags = "[]",
                RuleEvaluationResult = "{\"isEligible\": true, \"passedRules\": [...], \"failedRules\": []}"
            },
            new WelfareApplication
            {
                UserId = users[7].Id, // employee2
                WelfareProgramId = welfarePrograms[1].Id, // Emergency Financial Assistance
                Status = 9, // Rejected
                RequestedAmount = 3500.00m,
                Reason = "Emergency home repair",
                SubmittedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-15),
                IsEligible = false,
                AnomalyFlags = "[{\"anomalyType\": \"Amount\", \"severity\": \"High\", \"explanation\": \"Amount exceeds program limit\"}]",
                RuleEvaluationResult = "{\"isEligible\": false, \"passedRules\": [], \"failedRules\": [{\"rule\": \"Amount Limit\", \"explanation\": \"Requested amount $3,500 exceeds limit $3,000\"}]}"
            },
            new WelfareApplication
            {
                UserId = users[8].Id, // employee3
                WelfareProgramId = welfarePrograms[2].Id, // Education/Training Support
                Status = 3, // Under HR Review
                RequestedAmount = 1500.00m,
                Reason = "Professional certification course",
                SubmittedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                IsEligible = true,
                AnomalyFlags = "[]",
                RuleEvaluationResult = "{\"isEligible\": true, \"passedRules\": [...], \"failedRules\": []}"
            }
        };

        await context.WelfareApplications.AddRangeAsync(sampleApplications);
        await context.SaveChangesAsync();

        // Seed Sample Notifications
        var sampleNotifications = new List<Notification>
        {
            new Notification
            {
                UserId = users[6].Id,
                Title = "Application Approved",
                Message = "Your Medical Assistance application has been approved.",
                Type = "Success",
                EntityId = "1",
                EntityType = "Application",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Notification
            {
                UserId = users[7].Id,
                Title = "Application Rejected",
                Message = "Your Emergency Financial Assistance application has been rejected due to amount exceeding limit.",
                Type = "Error",
                EntityId = "2",
                EntityType = "Application",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Notification
            {
                UserId = users[1].Id, // HR
                Title = "New Application for Review",
                Message = "A new application requires your review.",
                Type = "Info",
                EntityId = "3",
                EntityType = "Application",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        await context.Notifications.AddRangeAsync(sampleNotifications);
        await context.SaveChangesAsync();

        // Seed Sample Audit Logs
        var sampleAuditLogs = new List<AuditLog>
        {
            new AuditLog
            {
                UserId = users[6].Id,
                Action = "ApplicationSubmitted",
                EntityType = "WelfareApplication",
                EntityId = "1",
                Timestamp = DateTime.UtcNow.AddDays(-30),
                Details = "{\"applicationId\": 1, \"program\": \"Medical Assistance\", \"amount\": 2500}"
            },
            new AuditLog
            {
                UserId = users[1].Id, // HR
                Action = "ApplicationApproved",
                EntityType = "WelfareApplication",
                EntityId = "1",
                Timestamp = DateTime.UtcNow.AddDays(-25),
                Details = "{\"applicationId\": 1, \"approvedBy\": \"Jane Smith\"}"
            },
            new AuditLog
            {
                UserId = users[0].Id, // Admin
                Action = "UserCreated",
                EntityType = "User",
                EntityId = "10",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Details = "{\"userId\": 10, \"email\": \"employee4@welfare.edu\"}"
            }
        };

        await context.AuditLogs.AddRangeAsync(sampleAuditLogs);
        await context.SaveChangesAsync();
    }
}
