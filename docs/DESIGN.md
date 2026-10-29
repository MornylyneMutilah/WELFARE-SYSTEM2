# Welfare Management System - Design Document

**Project:** Intelligent Welfare Management System for University Employee Welfare Administration  
**Project Type:** Final Year Computer Science Project II  
**Version:** 1.0  
**Date:** 2026-08-13

---

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Technology Stack](#2-technology-stack)
3. [Repository Structure](#3-repository-structure)
4. [Database Design](#4-database-design)
5. [API Design](#5-api-design)
6. [Frontend Design](#6-frontend-design)
7. [Authentication Design](#7-authentication-design)
8. [Rule Engine Design](#8-rule-engine-design)
9. [Anomaly Detection Design](#9-anomaly-detection-design)
10. [Security Design](#10-security-design)
11. [UI/UX Design](#11-uiux-design)

---

## 1. System Architecture

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Next.js Frontend                          │
│  (React + TypeScript + Tailwind CSS + Chart.js/Recharts)    │
│                                                              │
│  - Pages/Routes for each role                               │
│  - Authentication context                                   │
│  - API client layer                                          │
│  - Role-based UI components                                  │
└─────────────────────────────┬───────────────────────────────┘
                              │ HTTPS + JWT
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core Web API Backend                    │
│                    (C# + .NET 8/9)                          │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Controllers (API Endpoints)                         │  │
│  └──────────────────────┬───────────────────────────────┘  │
│                         ↓                                   │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Services (Business Logic)                           │  │
│  │  - AuthService                                       │  │
│  │  - UserService                                       │  │
│  │  - WelfareService                                    │  │
│  │  - RuleEngineService                                 │  │
│  │  - AnomalyDetectionService                           │  │
│  │  - NotificationService                               │  │
│  │  - ReportService                                     │  │
│  └──────────────────────┬───────────────────────────────┘  │
│                         ↓                                   │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Repositories (Data Access)                          │  │
│  └──────────────────────┬───────────────────────────────┘  │
│                         ↓                                   │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Entity Framework Core (ORM)                         │  │
│  └──────────────────────┬───────────────────────────────┘  │
└─────────────────────────┼───────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│                  PostgreSQL Database                         │
│                                                              │
│  - Users, Roles, Departments                                 │
│  - WelfarePrograms, EligibilityRules                         │
│  - WelfareApplications, ApplicationDocuments                 │
│  - ApplicationReviews, StatusHistory                         │
│  - Notifications, AuditLogs                                 │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 Architectural Principles

**Separation of Concerns:**
- Frontend: UI and user interaction
- Backend: Business logic and data access
- Database: Data persistence

**Clean Architecture:**
- Controllers handle HTTP requests/responses
- Services contain business logic
- Repositories handle data access
- Entities represent database models
- DTOs represent API contracts

**Statelessness:**
- API is stateless using JWT tokens
- No session state on server
- Scalable horizontal deployment

**RESTful Design:**
- Resource-based URLs
- Proper HTTP methods (GET, POST, PUT, DELETE)
- Appropriate HTTP status codes
- Consistent response format

### 1.3 Communication Flow

```
User Action (Frontend)
        ↓
React Component
        ↓
API Client Function
        ↓
HTTP Request (with JWT)
        ↓
ASP.NET Core Controller
        ↓
Service Layer (Business Logic)
        ↓
Repository Layer (Data Access)
        ↓
Entity Framework Core
        ↓
PostgreSQL Database
        ↓
Response (DTO)
        ↓
Frontend Display
```

---

## 2. Technology Stack

### 2.1 Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| Next.js | 14+ | React framework with App Router |
| React | 18+ | UI library |
| TypeScript | 5+ | Type safety |
| Tailwind CSS | 3+ | Styling |
| Chart.js / Recharts | Latest | Data visualization |
| Axios | Latest | HTTP client |

### 2.2 Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| ASP.NET Core | 8/9 | Web API framework |
| C# | 12+ | Programming language |
| Entity Framework Core | 8+ | ORM |
| JWT (System.IdentityModel.Tokens.Jwt) | Latest | Authentication |
| BCrypt.Net-Next | Latest | Password hashing |
| Serilog | Latest | Logging |
| FluentValidation | Latest | Input validation |

### 2.3 Database

| Technology | Version | Purpose |
|------------|---------|---------|
| PostgreSQL | 15+ | Relational database |
| Npgsql | Latest | PostgreSQL driver for .NET |

### 2.4 Development Tools

| Tool | Purpose |
|------|---------|
| Visual Studio Community | ASP.NET Core development |
| VS Code | Next.js development |
| PostgreSQL | Database management |
| Git | Version control |
| GitHub | Code hosting |

---

## 3. Repository Structure

### 3.1 Overall Structure

```
WelfareSystem/
├── backend/                          # ASP.NET Core Web API
│   ├── WelfareSystem.Api/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Repositories/
│   │   ├── Models/
│   │   ├── Data/
│   │   ├── Middleware/
│   │   └── Program.cs
│   └── WelfareSystem.Tests/
│
├── frontend/                         # Next.js Frontend
│   ├── src/
│   │   ├── app/
│   │   ├── components/
│   │   ├── lib/
│   │   ├── types/
│   │   └── styles/
│   ├── public/
│   └── package.json
│
├── docs/                           # Documentation
├── README.md
└── .gitignore
```

### 3.2 Backend Structure

```
backend/WelfareSystem.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── DepartmentsController.cs
│   ├── WelfareProgramsController.cs
│   ├── ApplicationsController.cs
│   ├── ReviewsController.cs
│   ├── NotificationsController.cs
│   ├── ReportsController.cs
│   └── AdminController.cs
│
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IUserService.cs
│   │   ├── IWelfareService.cs
│   │   ├── IRuleEngineService.cs
│   │   ├── IAnomalyDetectionService.cs
│   │   ├── INotificationService.cs
│   │   └── IReportService.cs
│   ├── AuthService.cs
│   ├── UserService.cs
│   ├── WelfareService.cs
│   ├── RuleEngineService.cs
│   ├── AnomalyDetectionService.cs
│   ├── NotificationService.cs
│   └── ReportService.cs
│
├── Repositories/
│   ├── Interfaces/
│   │   ├── IUserRepository.cs
│   │   ├── IDepartmentRepository.cs
│   │   ├── IWelfareProgramRepository.cs
│   │   ├── IWelfareApplicationRepository.cs
│   │   ├── INotificationRepository.cs
│   │   └── IAuditLogRepository.cs
│   ├── UserRepository.cs
│   ├── DepartmentRepository.cs
│   ├── WelfareProgramRepository.cs
│   ├── WelfareApplicationRepository.cs
│   ├── NotificationRepository.cs
│   └── AuditLogRepository.cs
│
├── Models/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Role.cs
│   │   ├── Department.cs
│   │   ├── WelfareProgram.cs
│   │   ├── EligibilityRule.cs
│   │   ├── WelfareApplication.cs
│   │   ├── ApplicationDocument.cs
│   │   ├── ApplicationReview.cs
│   │   ├── ApplicationStatusHistory.cs
│   │   ├── Notification.cs
│   │   └── AuditLog.cs
│   ├── DTOs/
│   │   ├── Auth/
│   │   ├── User/
│   │   ├── Welfare/
│   │   └── Common/
│   └── Enums/
│       ├── UserRole.cs
│       ├── ApplicationStatus.cs
│       ├── RuleType.cs
│       └── AnomalySeverity.cs
│
├── Data/
│   ├── AppDbContext.cs
│   └── SeedData.cs
│
├── Middleware/
│   ├── JwtMiddleware.cs
│   └── ExceptionMiddleware.cs
│
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

### 3.3 Frontend Structure

```
frontend/src/
├── app/
│   ├── (auth)/
│   │   ├── login/
│   │   │   └── page.tsx
│   │   └── register/
│   │       └── page.tsx
│   ├── (dashboard)/
│   │   ├── employee/
│   │   │   ├── dashboard/
│   │   │   ├── applications/
│   │   │   ├── notifications/
│   │   │   └── profile/
│   │   ├── hr/
│   │   │   ├── dashboard/
│   │   │   ├── applications/
│   │   │   └── reports/
│   │   ├── committee/
│   │   │   ├── dashboard/
│   │   │   ├── applications/
│   │   │   └── reports/
│   │   ├── management/
│   │   │   ├── dashboard/
│   │   │   ├── applications/
│   │   │   └── reports/
│   │   └── admin/
│   │       ├── dashboard/
│   │       ├── users/
│   │       ├── departments/
│   │       ├── welfare-programs/
│   │       └── audit-logs/
│   ├── layout.tsx
│   └── page.tsx
│
├── components/
│   ├── common/
│   │   ├── Button.tsx
│   │   ├── Input.tsx
│   │   ├── Card.tsx
│   │   ├── Modal.tsx
│   │   ├── Table.tsx
│   │   ├── Badge.tsx
│   │   └── LoadingSpinner.tsx
│   ├── auth/
│   │   ├── LoginForm.tsx
│   │   └── RegisterForm.tsx
│   ├── employee/
│   │   ├── ApplicationForm.tsx
│   │   ├── ApplicationList.tsx
│   │   └── DocumentUpload.tsx
│   ├── hr/
│   │   ├── ReviewForm.tsx
│   │   └── ApplicationCard.tsx
│   ├── committee/
│   │   ├── ReviewForm.tsx
│   │   └── ApplicationCard.tsx
│   ├── management/
│   │   ├── ApprovalForm.tsx
│   │   └── ApplicationCard.tsx
│   └── admin/
│       ├── UserForm.tsx
│       └── ProgramForm.tsx
│
├── lib/
│   ├── api/
│   │   ├── auth.ts
│   │   ├── users.ts
│   │   ├── applications.ts
│   │   └── reports.ts
│   ├── auth/
│   │   ├── AuthContext.tsx
│   │   └── tokenUtils.ts
│   └── utils/
│       ├── validation.ts
│       └── formatting.ts
│
├── types/
│   ├── index.ts
│   ├── user.ts
│   ├── application.ts
│   └── common.ts
│
└── styles/
    └── globals.css
```

---

## 4. Database Design

### 4.1 Entity Relationship Diagram (Text)

```
┌─────────────────┐
│     User        │
├─────────────────┤
│ Id (PK)         │
│ Email           │
│ PasswordHash    │
│ FirstName       │
│ LastName        │
│ EmployeeId      │
│ DepartmentId    │───┐
│ RoleId          │───┼──┐
│ IsActive        │   │  │
│ CreatedAt       │   │  │
│ UpdatedAt       │   │  │
└─────────────────┘   │  │
                      │  │
┌─────────────────┐   │  │
│     Role        │   │  │
├─────────────────┤   │  │
│ Id (PK)         │◄──┘  │
│ Name            │      │
│ Description     │      │
└─────────────────┘      │
                         │
┌─────────────────┐      │
│  Department     │      │
├─────────────────┤      │
│ Id (PK)         │◄─────┘
│ Name            │
│ Code            │
│ Description     │
└─────────────────┘

┌─────────────────────┐
│  WelfareProgram     │
├─────────────────────┤
│ Id (PK)             │
│ Name                │
│ Description         │
│ Category            │
│ MaxAmount           │
│ MinServicePeriod    │
│ RequiredDocuments   │
│ IsActive            │
│ CreatedAt           │
│ UpdatedAt           │
└─────────────────────┘
          │
          │ 1:N
          ↓
┌─────────────────────┐
│  EligibilityRule    │
├─────────────────────┤
│ Id (PK)             │
│ WelfareProgramId    │───┐
│ RuleName            │   │
│ RuleType            │   │
│ RuleValue           │   │
│ Description         │   │
│ IsActive            │   │
└─────────────────────┘   │
                          │
┌─────────────────────┐   │
│ WelfareApplication  │   │
├─────────────────────┤   │
│ Id (PK)             │   │
│ UserId              │───┼──┐
│ WelfareProgramId    │───┘  │
│ Status              │      │
│ RequestedAmount     │      │
│ Reason              │      │
│ SubmittedAt         │      │
│ UpdatedAt           │      │
│ IsEligible          │      │
│ AnomalyFlags        │      │
└─────────────────────┘      │
          │                  │
          │ 1:N              │
          ↓                  │
┌─────────────────────┐      │
│ ApplicationDocument │      │
├─────────────────────┤      │
│ Id (PK)             │      │
│ ApplicationId       │───┐  │
│ FileName            │   │  │
│ FilePath            │   │  │
│ UploadedAt          │   │  │
└─────────────────────┘   │  │
                          │  │
┌─────────────────────┐   │  │
│ ApplicationReview   │   │  │
├─────────────────────┤   │  │
│ Id (PK)             │   │  │
│ ApplicationId       │───┘  │
│ ReviewerId          │──────┘
│ ReviewerRole        │
│ Recommendation      │
│ Comments            │
│ ReviewedAt          │
└─────────────────────┘

┌─────────────────────┐
│ApplicationStatusHist│
├─────────────────────┤
│ Id (PK)             │
│ ApplicationId       │───┐
│ Status              │   │
│ ChangedBy           │   │
│ ChangedAt           │   │
│ Notes               │   │
└─────────────────────┘   │
                          │
┌─────────────────────┐   │
│   Notification      │   │
├─────────────────────┤   │
│ Id (PK)             │   │
│ UserId              │───┘
│ Title               │
│ Message             │
│ IsRead              │
│ CreatedAt           │
└─────────────────────┘

┌─────────────────────┐
│     AuditLog        │
├─────────────────────┤
│ Id (PK)             │
│ UserId              │
│ Action              │
│ EntityType          │
│ EntityId            │
│ Timestamp           │
│ Details             │
└─────────────────────┘
```

### 4.2 Entity Definitions

#### User
```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmployeeId { get; set; }
    public int DepartmentId { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Department Department { get; set; }
    public Role Role { get; set; }
    public ICollection<WelfareApplication> Applications { get; set; }
    public ICollection<ApplicationReview> Reviews { get; set; }
    public ICollection<Notification> Notifications { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; }
}
```

#### Role
```csharp
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Navigation properties
    public ICollection<User> Users { get; set; }
}
```

#### Department
```csharp
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    
    // Navigation properties
    public ICollection<User> Users { get; set; }
}
```

#### WelfareProgram
```csharp
public class WelfareProgram
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public decimal MaxAmount { get; set; }
    public int MinServicePeriod { get; set; } // in months
    public string RequiredDocuments { get; set; } // JSON array
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<EligibilityRule> EligibilityRules { get; set; }
    public ICollection<WelfareApplication> Applications { get; set; }
}
```

#### EligibilityRule
```csharp
public class EligibilityRule
{
    public int Id { get; set; }
    public int WelfareProgramId { get; set; }
    public string RuleName { get; set; }
    public RuleType RuleType { get; set; }
    public string RuleValue { get; set; } // JSON configuration
    public string Description { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    public WelfareProgram WelfareProgram { get; set; }
}
```

#### WelfareApplication
```csharp
public class WelfareApplication
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int WelfareProgramId { get; set; }
    public ApplicationStatus Status { get; set; }
    public decimal RequestedAmount { get; set; }
    public string Reason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEligible { get; set; }
    public string AnomalyFlags { get; set; } // JSON array of anomalies
    public string RuleEvaluationResult { get; set; } // JSON
    
    // Navigation properties
    public User User { get; set; }
    public WelfareProgram WelfareProgram { get; set; }
    public ICollection<ApplicationDocument> Documents { get; set; }
    public ICollection<ApplicationReview> Reviews { get; set; }
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; }
}
```

#### ApplicationDocument
```csharp
public class ApplicationDocument
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public DateTime UploadedAt { get; set; }
    
    // Navigation properties
    public WelfareApplication Application { get; set; }
}
```

#### ApplicationReview
```csharp
public class ApplicationReview
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int ReviewerId { get; set; }
    public string ReviewerRole { get; set; }
    public string Recommendation { get; set; } // Approve/Reject/RequestInfo
    public string Comments { get; set; }
    public DateTime ReviewedAt { get; set; }
    
    // Navigation properties
    public WelfareApplication Application { get; set; }
    public User Reviewer { get; set; }
}
```

#### ApplicationStatusHistory
```csharp
public class ApplicationStatusHistory
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public ApplicationStatus Status { get; set; }
    public int ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Notes { get; set; }
    
    // Navigation properties
    public WelfareApplication Application { get; set; }
}
```

#### Notification
```csharp
public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; }
}
```

#### AuditLog
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } // JSON
    
    // Navigation properties
    public User User { get; set; }
}
```

### 4.3 Enums

```csharp
public enum UserRole
{
    Employee = 1,
    HROfficer = 2,
    CommitteeMember = 3,
    Management = 4,
    Administrator = 5
}

public enum ApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    UnderHRReview = 3,
    HRRecommended = 4,
    CommitteeReview = 5,
    CommitteeRecommended = 6,
    PendingManagementApproval = 7,
    Approved = 8,
    Rejected = 9,
    RequiresAdditionalInfo = 10,
    FlaggedForReview = 11,
    Cancelled = 12
}

public enum RuleType
{
    ServicePeriod = 1,
    AmountLimit = 2,
    Frequency = 3,
    DocumentRequirement = 4,
    EligibilityCriteria = 5,
    DuplicateApplication = 6
}

public enum AnomalySeverity
{
    Low = 1,
    Medium = 2,
    High = 3
}
```

### 4.4 Database Indexes

Recommended indexes for performance:

```csharp
// User
builder.HasIndex(u => u.Email).IsUnique();
builder.HasIndex(u => u.EmployeeId).IsUnique();

// WelfareApplication
builder.HasIndex(a => a.UserId);
builder.HasIndex(a => a.WelfareProgramId);
builder.HasIndex(a => a.Status);
builder.HasIndex(a => a.SubmittedAt);

// Notification
builder.HasIndex(n => n.UserId);
builder.HasIndex(n => n.IsRead);

// AuditLog
builder.HasIndex(a => a.UserId);
builder.HasIndex(a => a.Timestamp);
builder.HasIndex(a => a.EntityType);
```

---

## 5. API Design

### 5.1 API Response Format

**Success Response:**
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation successful"
}
```

**Error Response:**
```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Error description",
    "details": { ... }
  }
}
```

### 5.2 Authentication Endpoints

#### POST /api/auth/login
Request:
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```
Response:
```json
{
  "success": true,
  "data": {
    "token": "jwt_token_here",
    "refreshToken": "refresh_token_here",
    "user": {
      "id": 1,
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "Employee"
    }
  }
}
```

#### POST /api/auth/register
Request:
```json
{
  "email": "user@example.com",
  "password": "password123",
  "firstName": "John",
  "lastName": "Doe",
  "employeeId": "EMP001",
  "departmentId": 1
}
```

#### POST /api/auth/refresh
Request:
```json
{
  "refreshToken": "refresh_token_here"
}
```

### 5.3 User Endpoints

#### GET /api/users/me
Headers: `Authorization: Bearer {token}`

Response:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "employeeId": "EMP001",
    "department": {
      "id": 1,
      "name": "Computer Science"
    },
    "role": "Employee"
  }
}
```

#### GET /api/users
Query params: `?page=1&pageSize=10&search=john&role=Employee`

#### PUT /api/users/me
Request:
```json
{
  "firstName": "John",
  "lastName": "Smith"
}
```

### 5.4 Welfare Application Endpoints

#### GET /api/applications
Query params: `?status=Pending&page=1&pageSize=10`

#### POST /api/applications
Request:
```json
{
  "welfareProgramId": 1,
  "requestedAmount": 1500.00,
  "reason": "Medical assistance for surgery"
}
```

#### POST /api/applications/{id}/submit

#### POST /api/applications/{id}/documents
Request: `multipart/form-data` with file

#### GET /api/applications/{id}
Response:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "status": "UnderHRReview",
    "requestedAmount": 1500.00,
    "reason": "Medical assistance for surgery",
    "submittedAt": "2026-08-13T10:00:00Z",
    "isEligible": true,
    "ruleEvaluationResult": {
      "isEligible": true,
      "passedRules": [...],
      "failedRules": []
    },
    "anomalyFlags": [],
    "documents": [...],
    "reviews": [...]
  }
}
```

### 5.5 Review Endpoints

#### POST /api/applications/{id}/reviews
Request:
```json
{
  "recommendation": "Approve",
  "comments": "Application meets all requirements"
}
```

### 5.6 Notification Endpoints

#### GET /api/notifications
Query params: `?unreadOnly=true`

#### PUT /api/notifications/{id}/read

### 5.7 Report Endpoints

#### GET /api/reports/applications-by-status
Query params: `?startDate=2026-01-01&endDate=2026-12-31`

Response:
```json
{
  "success": true,
  "data": {
    "approved": 45,
    "rejected": 12,
    "pending": 8
  }
}
```

### 5.8 HTTP Status Codes

| Code | Usage |
|------|-------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 409 | Conflict |
| 500 | Internal Server Error |

---

## 6. Frontend Design

### 6.1 Routing Structure

```
/                           → Redirect to login or role dashboard
/login                      → Login page
/register                   → Registration page

/employee/dashboard         → Employee dashboard
/employee/applications      → Employee's applications
/employee/applications/new  → Create new application
/employee/applications/[id] → Application details
/employee/notifications     → Notifications
/employee/profile           → Profile settings

/hr/dashboard               → HR dashboard
/hr/applications            → Applications for review
/hr/applications/[id]       → Review application
/hr/reports                 → HR reports

/committee/dashboard        → Committee dashboard
/committee/applications     → Applications for review
/committee/applications/[id] → Review application
/committee/reports          → Committee reports

/management/dashboard       → Management dashboard
/management/applications    → Pending approvals
/management/applications/[id] → Review and approve
/management/reports         → Management reports

/admin/dashboard            → Admin dashboard
/admin/users                → User management
/admin/departments          → Department management
/admin/welfare-programs     → Program management
/admin/eligibility-rules    → Rule configuration
/admin/audit-logs           → Audit logs
/admin/settings             → System settings
```

### 6.2 Component Hierarchy

```
Layout
├── Sidebar (role-based navigation)
├── Header (user info, notifications)
└── Main Content
    └── Page Components
        ├── Dashboard Cards
        ├── Tables
        ├── Forms
        └── Charts
```

### 6.3 State Management

**Context API for:**
- Authentication (user, token, role)
- Notifications (unread count)

**Local State for:**
- Form data
- UI toggles
- Temporary data

**Server State for:**
- API data (applications, users, etc.)
- Cached with React Query or SWR (optional)

### 6.4 API Client

```typescript
// lib/api/client.ts
import axios from 'axios';

const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - add token
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor - handle errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Redirect to login
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

---

## 7. Authentication Design

### 7.1 JWT Token Structure

**Access Token (15-60 minutes):**
```json
{
  "sub": "user_id",
  "email": "user@example.com",
  "role": "Employee",
  "exp": 1234567890,
  "iat": 1234567890
}
```

**Refresh Token (7 days):**
- Stored in HTTP-only cookie or secure storage
- Used to obtain new access tokens

### 7.2 Authentication Flow

```
1. User submits login credentials
        ↓
2. Server validates credentials
        ↓
3. Server generates JWT tokens
        ↓
4. Server returns tokens to client
        ↓
5. Client stores access token in memory/localStorage
        ↓
6. Client includes token in Authorization header
        ↓
7. Server validates token on each request
        ↓
8. Token expires → Client uses refresh token
        ↓
9. Server issues new access token
        ↓
10. Refresh token expires → User must login again
```

### 7.3 Authorization Middleware

```csharp
public class JwtMiddleware
{
    public async Task InvokeAsync(HttpContext context, IUserService userService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        
        if (token != null)
        {
            var userId = validateToken(token);
            if (userId != null)
            {
                context.Items["User"] = await userService.GetById(userId.Value);
            }
        }
        
        await _next(context);
    }
}
```

### 7.4 Role-Based Authorization

```csharp
[Authorize]
[HttpGet]
public IActionResult GetMyApplications()
{
    // Only authenticated users
}

[Authorize(Roles = "HROfficer,Administrator")]
[HttpGet]
public IActionResult GetAllApplications()
{
    // Only HR and Admin
}
```

---

## 8. Rule Engine Design

### 8.1 Rule Engine Architecture

```
WelfareApplication
        ↓
RuleEngineService.Evaluate(application)
        ↓
┌─────────────────────────────────────┐
│  Load active rules for program       │
└─────────────────────────────────────┘
        ↓
┌─────────────────────────────────────┐
│  For each rule:                      │
│  - Parse rule configuration          │
│  - Evaluate against application      │
│  - Collect result (pass/fail)        │
└─────────────────────────────────────┘
        ↓
┌─────────────────────────────────────┐
│  Compile results:                    │
│  - Overall eligibility               │
│  - Passed rules                      │
│  - Failed rules                      │
│  - Warnings                          │
└─────────────────────────────────────┘
        ↓
RuleEvaluationResult
```

### 8.2 Rule Interface

```csharp
public interface IRule
{
    string Name { get; }
    string Description { get; }
    RuleResult Evaluate(WelfareApplication application, EligibilityRule rule);
}

public class RuleResult
{
    public bool Passed { get; set; }
    public string Explanation { get; set; }
}
```

### 8.3 Rule Implementations

#### ServicePeriodRule
```csharp
public class ServicePeriodRule : IRule
{
    public string Name => "Service Period";
    public string Description => "Verifies minimum employment duration";
    
    public RuleResult Evaluate(WelfareApplication application, EligibilityRule rule)
    {
        var config = JsonSerializer.Deserialize<ServicePeriodConfig>(rule.RuleValue);
        var employmentDuration = CalculateEmploymentDuration(application.User);
        
        var passed = employmentDuration.TotalMonths >= config.MinMonths;
        
        return new RuleResult
        {
            Passed = passed,
            Explanation = passed 
                ? $"Employee has {employmentDuration.TotalMonths} months of service (required: {config.MinMonths})"
                : $"Employee has only {employmentDuration.TotalMonths} months of service (required: {config.MinMonths})"
        };
    }
}
```

#### AmountLimitRule
```csharp
public class AmountLimitRule : IRule
{
    public string Name => "Amount Limit";
    public string Description => "Verifies requested amount is within program limit";
    
    public RuleResult Evaluate(WelfareApplication application, EligibilityRule rule)
    {
        var config = JsonSerializer.Deserialize<AmountLimitConfig>(rule.RuleValue);
        var passed = application.RequestedAmount <= config.MaxAmount;
        
        return new RuleResult
        {
            Passed = passed,
            Explanation = passed
                ? $"Requested amount ${application.RequestedAmount} is within limit ${config.MaxAmount}"
                : $"Requested amount ${application.RequestedAmount} exceeds limit ${config.MaxAmount}"
        };
    }
}
```

### 8.4 Rule Evaluation Result

```csharp
public class RuleEvaluationResult
{
    public bool IsEligible { get; set; }
    public List<RuleEvaluation> PassedRules { get; set; }
    public List<RuleEvaluation> FailedRules { get; set; }
    public List<string> Warnings { get; set; }
}

public class RuleEvaluation
{
    public string RuleName { get; set; }
    public string Description { get; set; }
    public bool Passed { get; set; }
    public string Explanation { get; set; }
}
```

### 8.5 Rule Engine Service

```csharp
public class RuleEngineService : IRuleEngineService
{
    private readonly Dictionary<RuleType, IRule> _rules;
    
    public RuleEngineService()
    {
        _rules = new Dictionary<RuleType, IRule>
        {
            { RuleType.ServicePeriod, new ServicePeriodRule() },
            { RuleType.AmountLimit, new AmountLimitRule() },
            { RuleType.Frequency, new FrequencyRule() },
            { RuleType.DocumentRequirement, new DocumentRequirementRule() },
            { RuleType.EligibilityCriteria, new EligibilityCriteriaRule() },
            { RuleType.DuplicateApplication, new DuplicateApplicationRule() }
        };
    }
    
    public async Task<RuleEvaluationResult> EvaluateAsync(WelfareApplication application)
    {
        var result = new RuleEvaluationResult
        {
            PassedRules = new List<RuleEvaluation>(),
            FailedRules = new List<RuleEvaluation>(),
            Warnings = new List<string>()
        };
        
        var activeRules = await GetActiveRulesForProgram(application.WelfareProgramId);
        
        foreach (var rule in activeRules)
        {
            if (_rules.TryGetValue(rule.RuleType, out var ruleImplementation))
            {
                var evaluation = ruleImplementation.Evaluate(application, rule);
                
                if (evaluation.Passed)
                {
                    result.PassedRules.Add(new RuleEvaluation
                    {
                        RuleName = rule.RuleName,
                        Description = rule.Description,
                        Passed = true,
                        Explanation = evaluation.Explanation
                    });
                }
                else
                {
                    result.FailedRules.Add(new RuleEvaluation
                    {
                        RuleName = rule.RuleName,
                        Description = rule.Description,
                        Passed = false,
                        Explanation = evaluation.Explanation
                    });
                }
            }
        }
        
        result.IsEligible = result.FailedRules.Count == 0;
        
        return result;
    }
}
```

---

## 9. Anomaly Detection Design

### 9.1 Anomaly Detection Architecture

```
WelfareApplication
        ↓
AnomalyDetectionService.DetectAnomalies(application)
        ↓
┌─────────────────────────────────────┐
│  Run anomaly detectors:              │
│  1. DuplicateDetector                │
│  2. FrequencyAbuseDetector           │
│  3. AmountAnomalyDetector            │
│  4. DocumentAnomalyDetector          │
│  5. PatternAnomalyDetector           │
└─────────────────────────────────────┘
        ↓
┌─────────────────────────────────────┐
│  Collect anomalies:                  │
│  - Anomaly type                      │
│  - Severity (Low/Medium/High)        │
│  - Human-readable explanation        │
└─────────────────────────────────────┘
        ↓
List<AnomalyFlag>
```

### 9.2 Anomaly Detector Interface

```csharp
public interface IAnomalyDetector
{
    string Name { get; set; }
    Task<List<AnomalyFlag>> DetectAsync(WelfareApplication application);
}

public class AnomalyFlag
{
    public string AnomalyType { get; set; }
    public AnomalySeverity Severity { get; set; }
    public string Explanation { get; set; }
    public string Recommendation { get; set; }
}
```

### 9.3 Anomaly Detector Implementations

#### DuplicateDetector
```csharp
public class DuplicateDetector : IAnomalyDetector
{
    public string Name => "Duplicate Application";
    
    public async Task<List<AnomalyFlag>> DetectAsync(WelfareApplication application)
    {
        var flags = new List<AnomalyFlag>();
        
        // Check for identical applications
        var identical = await _repository.FindIdenticalApplicationAsync(application);
        if (identical != null)
        {
            flags.Add(new AnomalyFlag
            {
                AnomalyType = "Duplicate",
                Severity = AnomalySeverity.High,
                Explanation = "An identical application already exists",
                Recommendation = "Review with employee to confirm this is not a duplicate submission"
            });
        }
        
        // Check for similar applications within short timeframe
        var similar = await _repository.FindSimilarApplicationsAsync(application, TimeSpan.FromDays(7));
        if (similar.Any())
        {
            flags.Add(new AnomalyFlag
            {
                AnomalyType = "Similar Application",
                Severity = AnomalySeverity.Medium,
                Explanation = $"Employee submitted {similar.Count} similar application(s) in the last 7 days",
                Recommendation = "Verify if multiple applications are necessary"
            });
        }
        
        return flags;
    }
}
```

#### FrequencyAbuseDetector
```csharp
public class FrequencyAbuseDetector : IAnomalyDetector
{
    public string Name => "Frequency Abuse";
    
    public async Task<List<AnomalyFlag>> DetectAsync(WelfareApplication application)
    {
        var flags = new List<AnomalyFlag>();
        
        var program = await _programRepository.GetByIdAsync(application.WelfareProgramId);
        var recentApplications = await _repository.GetRecentApplicationsByUserAndProgramAsync(
            application.UserId, 
            application.WelfareProgramId, 
            program.MinServicePeriod
        );
        
        if (recentApplications.Count >= 3)
        {
            flags.Add(new AnomalyFlag
            {
                AnomalyType = "High Frequency",
                Severity = AnomalySeverity.High,
                Explanation = $"Employee has submitted {recentApplications.Count} applications for this program within the restricted period",
                Recommendation = "Review if this pattern indicates potential abuse"
            });
        }
        
        return flags;
    }
}
```

### 9.4 Anomaly Detection Service

```csharp
public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly List<IAnomalyDetector> _detectors;
    
    public AnomalyDetectionService()
    {
        _detectors = new List<IAnomalyDetector>
        {
            new DuplicateDetector(),
            new FrequencyAbuseDetector(),
            new AmountAnomalyDetector(),
            new DocumentAnomalyDetector(),
            new PatternAnomalyDetector()
        };
    }
    
    public async Task<List<AnomalyFlag>> DetectAnomaliesAsync(WelfareApplication application)
    {
        var allFlags = new List<AnomalyFlag>();
        
        foreach (var detector in _detectors)
        {
            var flags = await detector.DetectAsync(application);
            allFlags.AddRange(flags);
        }
        
        return allFlags.OrderByDescending(f => f.Severity).ToList();
    }
}
```

---

## 10. Security Design

### 10.1 Authentication Security

**Password Hashing:**
```csharp
public class PasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

**JWT Configuration:**
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]))
        };
    });
```

### 10.2 Authorization Security

**Role-Based Policy:**
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee"));
    options.AddPolicy("HROnly", policy => policy.RequireRole("HROfficer"));
    options.AddPolicy("CommitteeOnly", policy => policy.RequireRole("CommitteeMember"));
    options.AddPolicy("ManagementOnly", policy => policy.RequireRole("Management"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
});
```

**Resource-Based Authorization:**
```csharp
public class ApplicationAuthorizationHandler
    : AuthorizationHandler<ApplicationRequirement, WelfareApplication>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApplicationRequirement requirement,
        WelfareApplication application)
    {
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        
        // Users can only view their own applications unless they have reviewer role
        if (userRole == "Employee" && application.UserId != GetCurrentUserId(context))
        {
            context.Fail();
        }
        
        return Task.CompletedTask;
    }
}
```

### 10.3 Input Validation

**FluentValidation:**
```csharp
public class CreateApplicationDtoValidator : AbstractValidator<CreateApplicationDto>
{
    public CreateApplicationDtoValidator()
    {
        RuleFor(x => x.WelfareProgramId).GreaterThan(0);
        RuleFor(x => x.RequestedAmount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
```

### 10.4 SQL Injection Prevention

- Use parameterized queries via Entity Framework Core
- Never concatenate user input into SQL queries
- Use stored procedures where appropriate

### 10.5 XSS Prevention

- React automatically escapes output by default
- Use DOMPurify for any HTML content
- Validate and sanitize all user inputs

### 10.6 CSRF Protection

- Use Anti-Forgery tokens for state-changing operations
- Validate tokens on POST/PUT/DELETE requests

### 10.7 Rate Limiting

```csharp
services.AddRateLimiter(options =>
{
    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2
            }));
});
```

---

## 11. UI/UX Design

### 11.1 Design Principles

- **Clean and Professional:** Academic institutional aesthetic
- **Consistent:** Unified design system across all pages
- **Accessible:** WCAG 2.1 AA compliance where practical
- **Responsive:** Works on desktop and mobile
- **Clear:** Unambiguous status indicators and feedback

### 11.2 Color Palette

```
Primary: #1e40af (University Blue)
Secondary: #3b82f6 (Lighter Blue)
Success: #10b981 (Green)
Warning: #f59e0b (Amber)
Error: #ef4444 (Red)
Neutral: #6b7280 (Gray)
Background: #f9fafb (Light Gray)
Surface: #ffffff (White)
```

### 11.3 Typography

```
Headings: Inter, system-ui, sans-serif
Body: Inter, system-ui, sans-serif
Mono: JetBrains Mono, monospace (for code/IDs)
```

### 11.4 Component Design

**Status Badges:**
```
Approved: Green badge
Rejected: Red badge
Pending: Yellow badge
Under Review: Blue badge
```

**Dashboard Cards:**
- Icon + Label + Value
- Consistent spacing
- Hover effects

**Tables:**
- Sortable columns
- Pagination
- Row actions
- Status indicators

**Forms:**
- Clear labels
- Validation messages
- Required field indicators
- Submit/Cancel buttons

### 11.5 Layout Structure

```
┌─────────────────────────────────────────────────┐
│ Header (Logo + User Menu + Notifications)        │
├──────────┬──────────────────────────────────────┤
│          │                                      │
│ Sidebar  │         Main Content                 │
│          │                                      │
│ - Dash   │  ┌────────────────────────────────┐ │
│ - Apps   │  │                                │ │
│ - Reports│  │      Page Content              │ │
│ - Profile│  │                                │ │
│          │  └────────────────────────────────┘ │
│          │                                      │
└──────────┴──────────────────────────────────────┘
```

### 11.6 Responsive Design

- Desktop: Sidebar navigation
- Tablet: Collapsible sidebar
- Mobile: Bottom navigation or hamburger menu

---

## Document Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-13 | Initial | Initial design document |
