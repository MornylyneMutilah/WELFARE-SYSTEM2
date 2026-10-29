# Welfare Management System Database Schema

## Database Overview

**Database Name:** welfare_system  
**Database Type:** PostgreSQL 15+  
**ORM:** Entity Framework Core 8+

## Tables

### Users

Stores user account information.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| Email | string(256) | Unique, Required | User email address |
| PasswordHash | string | Required | BCrypt hashed password |
| FirstName | string(100) | Required | User first name |
| LastName | string(100) | Required | User last name |
| EmployeeId | string(50) | Unique, Required | Employee ID number |
| HireDate | datetime | Required | Date of hire |
| DepartmentId | int | FK, Required | Department reference |
| RoleId | int | FK, Required | Role reference |
| IsActive | bool | Required | Account active status |
| CreatedAt | datetime | Required | Account creation date |
| UpdatedAt | datetime | Required | Last update date |

**Indexes:**
- Email (Unique)
- EmployeeId (Unique)

### Roles

Stores user roles for authorization.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| Name | string(50) | Required, Unique | Role name (Employee, HROfficer, CommitteeMember, Management, Administrator) |
| Description | string(200) | Optional | Role description |

**Seed Data:**
- Employee
- HROfficer
- CommitteeMember
- Management
- Administrator

### Departments

Stores organizational departments.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| Name | string(100) | Required, Unique | Department name |
| Description | string(500) | Optional | Department description |

**Seed Data:**
- Engineering
- Finance
- HR
- IT
- Administration
- Academic Affairs
- Student Services
- Research

### WelfarePrograms

Stores welfare program definitions.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| Name | string(200) | Required | Program name |
| Description | string(1000) | Required | Program description |
| Category | string(50) | Required | Program category (Medical, Education, Loan, etc.) |
| MaxAmount | decimal(18,2) | Required | Maximum amount per application |
| MinServicePeriod | int | Required | Minimum service period in months |
| RequiredDocuments | string | Required | JSON array of required documents |
| IsActive | bool | Required | Program active status |
| CreatedAt | datetime | Required | Creation date |
| UpdatedAt | datetime | Required | Last update date |

### WelfareApplications

Stores welfare application submissions.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| UserId | int | FK, Required | User reference |
| WelfareProgramId | int | FK, Required | Program reference |
| Status | int | Required | Application status (1-10) |
| RequestedAmount | decimal(18,2) | Required | Amount requested |
| Reason | string(2000) | Required | Reason for application |
| IsEligible | bool | Required | Eligibility flag from rule engine |
| AnomalyFlags | string | Required | JSON array of anomaly warnings |
| RuleEvaluationResult | string | Required | JSON of rule evaluation results |
| SubmittedAt | datetime | Required | Submission date |
| UpdatedAt | datetime | Required | Last update date |

**Status Values:**
- 1: Draft
- 2: Submitted
- 3: Under HR Review
- 4: Under Committee Review
- 5: Approved by Committee
- 6: Rejected by Committee
- 7: Pending Management Approval
- 8: Approved
- 9: Rejected
- 10: Cancelled

### ApplicationDocuments

Stores application document attachments.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| ApplicationId | int | FK, Required | Application reference |
| FileName | string(255) | Required | Original file name |
| FileType | string(100) | Required | File MIME type |
| FileSize | long | Required | File size in bytes |
| FilePath | string(500) | Required | Storage path |
| UploadedAt | datetime | Required | Upload date |

### ApplicationReviews

Stores application reviews by HR, Committee, and Management.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| ApplicationId | int | FK, Required | Application reference |
| ReviewerId | int | FK, Required | Reviewer user reference |
| ReviewerRole | string(50) | Required | Reviewer role |
| Recommendation | string(50) | Required | Approve/Reject/RequestInfo |
| Comments | string(2000) | Required | Review comments |
| ReviewedAt | datetime | Required | Review date |

### ApplicationStatusHistory

Stores status change history for audit trail.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| ApplicationId | int | FK, Required | Application reference |
| Status | int | Required | New status value |
| ChangedBy | int | FK, Required | User who made change |
| ChangedAt | datetime | Required | Change date |
| Comments | string(500) | Optional | Change notes |

### EligibilityRules

Stores eligibility rule configurations.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| WelfareProgramId | int | FK, Required | Program reference |
| RuleType | string(50) | Required | Rule type |
| RuleConfig | string | Required | JSON rule configuration |
| IsActive | bool | Required | Rule active status |
| CreatedAt | datetime | Required | Creation date |

### Notifications

Stores user notifications.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| UserId | int | FK, Required | User reference |
| Title | string(200) | Required | Notification title |
| Message | string(1000) | Required | Notification message |
| Type | string(50) | Required | Notification type (Info, Success, Warning, Error) |
| EntityId | string(50) | Optional | Related entity ID |
| EntityType | string(50) | Optional | Related entity type |
| IsRead | bool | Required | Read status |
| CreatedAt | datetime | Required | Creation date |

**Indexes:**
- UserId
- IsRead

### AuditLogs

Stores system audit logs.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| UserId | int | FK, Optional | User reference |
| Action | string(100) | Required | Action performed |
| EntityId | string(50) | Optional | Affected entity ID |
| EntityType | string(50) | Optional | Affected entity type |
| Timestamp | datetime | Required | Action timestamp |
| Details | string(2000) | Optional | Additional details |

## Relationships

### User Relationships
- User → Department (Many-to-One)
- User → Role (Many-to-One)
- User → WelfareApplications (One-to-Many)
- User → Notifications (One-to-Many)
- User → AuditLogs (One-to-Many)

### WelfareApplication Relationships
- WelfareApplication → User (Many-to-One)
- WelfareApplication → WelfareProgram (Many-to-One)
- WelfareApplication → ApplicationDocuments (One-to-Many)
- WelfareApplication → ApplicationReviews (One-to-Many)
- WelfareApplication → ApplicationStatusHistory (One-to-Many)

### ApplicationReview Relationships
- ApplicationReview → WelfareApplication (Many-to-One)
- ApplicationReview → User (Many-to-One, as Reviewer)

### WelfareProgram Relationships
- WelfareProgram → WelfareApplications (One-to-Many)
- WelfareProgram → EligibilityRules (One-to-Many)

## Database Migrations

The following migrations have been created:

1. **InitialCreate** - Initial schema setup
2. **UpdateApplicationDocument** - Added FileType and FileSize to ApplicationDocuments
3. **AddUserHireDate** - Added HireDate to Users
4. **UpdateApplicationReview** - Added Reviewer navigation to ApplicationReviews
5. **UpdateNotificationFields** - Added Type, EntityId, EntityType to Notifications

**Note:** Migrations need to be applied to the database using:
```bash
dotnet ef database update
```

## Seed Data

The system includes comprehensive seed data for development:

- **Departments:** 8 departments across the organization
- **Roles:** 5 roles for authorization
- **Users:** 10+ users across all roles with varied hire dates
- **Welfare Programs:** 6+ programs with different categories
- **Eligibility Rules:** Sample rules for programs
- **Applications:** 3+ sample applications in various statuses
- **Notifications:** 3+ sample notifications

Seed data is automatically applied in development mode.
