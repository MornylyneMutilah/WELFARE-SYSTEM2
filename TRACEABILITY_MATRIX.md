# Traceability Matrix - Objectives to Components

## Project Objectives

### Objective 1: Employee Welfare Management System
Enable employees to submit welfare applications with HR/Committee/Management review workflow.

| Objective | Component | Test Case | Status | Evidence |
|-----------|-----------|------------|--------|----------|
| User Registration | frontend/app/register/page.tsx | TC1 | ⏳ Pending | [TC1 Result] |
| User Registration | backend/Controllers/AuthController.cs | TC1 | ⏳ Pending | [TC1 Result] |
| User Registration | backend/Services/AuthService.cs | TC1 | ⏳ Pending | [TC1 Result] |
| User Login | frontend/app/login/page.tsx | TC2 | ⏳ Pending | [TC2 Result] |
| User Login | backend/Controllers/AuthController.cs | TC2 | ⏳ Pending | [TC2 Result] |
| User Login | backend/Services/AuthService.cs | TC2 | ⏳ Pending | [TC2 Result] |
| Invalid Login Handling | backend/Services/AuthService.cs | TC3 | ⏳ Pending | [TC3 Result] |
| Employee Dashboard | frontend/app/employee/dashboard/page.tsx | TC2 | ⏳ Pending | [TC2 Result] |
| Welfare Programs Display | frontend/app/employee/programs/page.tsx | TC7 | ⏳ Pending | [TC7 Result] |
| Welfare Programs API | backend/Controllers/WelfareProgramsController.cs | TC7 | ⏳ Pending | [TC7 Result] |
| Application Submission | frontend/app/employee/applications/new/page.tsx | TC8 | ⏳ Pending | [TC8 Result] |
| Application API | backend/Controllers/ApplicationsController.cs | TC8 | ⏳ Pending | [TC8 Result] |
| Application Workflow | backend/Services/WelfareApplicationService.cs | TC8 | ⏳ Pending | [TC8 Result] |
| Database Persistence | backend/Data/AppDbContext.cs | TC4 | ✅ Complete | Database connected, seed data inserted |
| Department Management | backend/Controllers/DepartmentsController.cs | TC5 | ⏳ Pending | [TC5 Result] |
| Role-Based Access | backend/Program.cs (Authorization) | TC6 | ⏳ Pending | [TC6 Result] |

### Objective 2: Intelligent Features
Rule-based eligibility checks, anomaly detection, and automated notifications.

| Objective | Component | Test Case | Status | Evidence |
|-----------|-----------|------------|--------|----------|
| Eligibility Rules | backend/Services/RuleEngineService.cs | - | ✅ Complete | Implemented in backend |
| Eligibility Rules | backend/Models/Entities/EligibilityRule.cs | - | ✅ Complete | Entity defined |
| Eligibility Evaluation | backend/Services/WelfareApplicationService.cs | TC8 | ⏳ Pending | [TC8 Result] |
| Anomaly Detection | backend/Services/AnomalyDetectionService.cs | - | ✅ Complete | Implemented in backend |
| Anomaly Detectors | backend/Services/AnomalyDetectors/ | - | ✅ Complete | 5 detectors implemented |
| Notifications | backend/Services/NotificationService.cs | - | ✅ Complete | Service implemented |
| Notification Triggers | backend/Services/NotificationTriggerService.cs | - | ✅ Complete | Triggers implemented |
| Notification API | backend/Controllers/NotificationsController.cs | - | ✅ Complete | Endpoints available |
| Notification Entity | backend/Models/Entities/Notification.cs | - | ✅ Complete | Entity with Type, EntityId, EntityType |

### Objective 3: Professional UI
Red and white themed interface with 3D effects and social login options.

| Objective | Component | Test Case | Status | Evidence |
|-----------|-----------|------------|--------|----------|
| Red/White Theme | frontend/app/globals.css | TC10 | ⏳ Pending | [TC10 Result] |
| 3D Card Effects | frontend/app/globals.css (.card-3d) | TC10 | ⏳ Pending | [TC10 Result] |
| 3D Button Effects | frontend/app/globals.css (.button-3d) | TC10 | ⏳ Pending | [TC10 Result] |
| Floating Animations | frontend/app/globals.css (.float) | TC10 | ⏳ Pending | [TC10 Result] |
| Icon 3D Flip | frontend/app/globals.css (.icon-3d) | TC10 | ⏳ Pending | [TC10 Result] |
| Neon Glow | frontend/app/globals.css (.neon-glow) | TC10 | ⏳ Pending | [TC10 Result] |
| Professional Header | frontend/components/common/Header.tsx | TC10 | ⏳ Pending | [TC10 Result] |
| Homepage Design | frontend/app/page.tsx | TC10 | ⏳ Pending | [TC10 Result] |
| Login Page Design | frontend/app/login/page.tsx | TC10 | ⏳ Pending | [TC10 Result] |
| Register Page Design | frontend/app/register/page.tsx | TC10 | ⏳ Pending | [TC10 Result] |
| Social Login Buttons | frontend/app/login/page.tsx | - | ⏸️ Partial | UI implemented, no OAuth |
| Social Login Buttons | frontend/app/register/page.tsx | - | ⏸️ Partial | UI implemented, no OAuth |
| Button Visibility | frontend/app/globals.css | TC10 | ⏳ Pending | [TC10 Result] |

## Sprint I Completed Components

### Development Environment
- ✅ .NET SDK 10.0 installed
- ✅ Node.js and npm installed
- ✅ PostgreSQL configured
- ✅ Visual Studio Code configured

### Codebase Organization
- ✅ Backend: ASP.NET Core with Controllers, Services, Repositories, Models
- ✅ Frontend: Next.js with App Router, Components, API clients
- ✅ Database: PostgreSQL with Entity Framework Core
- ✅ Clear folder structure documented in README

### Git Version Control
- ✅ Local Git repository initialized
- ✅ Multiple meaningful commits
- ⚠️ Passwords in appsettings (needs env vars for production)

### Data Preparation
- ✅ Database schema designed (11 tables)
- ✅ Seed data created (roles, departments, programs, users)
- ✅ Connection configured in appsettings
- ✅ Data source documented

### Automation
- ✅ Build scripts (dotnet build, npm run build)
- ✅ Dev server scripts (dotnet run, npm run dev)
- ✅ Database migration (dotnet ef database update)

## Sprint II Requirements Status

### Core Workflow
- ⏳ Complete registration flow (TC1)
- ⏳ Complete login flow (TC2)
- ⏳ Employee dashboard (TC2)
- ⏳ Welfare programs listing (TC7)
- ⏳ Application submission (TC8)

### Integration
- ✅ Database connectivity (TC4)
- ⏳ API communication (TC5)
- ⏳ Authentication token handling (TC2)
- ⏳ Role-based access control (TC6)

### Testing
- ⏳ Expected behavior tests (TC1, TC2, TC7, TC8)
- ⏳ Invalid input tests (TC3, TC9)
- ⏳ Failure condition tests (TC3)
- ⏳ Usability tests (TC10)

### Evaluation
- ⏳ Traceability matrix (this document)
- ⏳ Usability observations (TC10)
- ⏳ Test results documentation (TEST_RESULTS.md)

### Git & Documentation
- ⏳ Sprint II commit
- ⏳ README update
- ⏳ Limitations documentation

## Unmet Requirements & Impact

### Social Login OAuth
**Status**: UI only, no backend implementation
**Impact**: Users cannot actually authenticate with Google/GitHub
**Justification**: Out of scope for core workflow, would require OAuth provider setup
**Mitigation**: Email/password authentication fully functional

### Unit/Integration Tests
**Status**: Not implemented
**Impact**: No automated test coverage
**Justification**: Time constraints in Sprint I
**Mitigation**: Manual testing documented in TEST_RESULTS.md

### HTTPS Configuration
**Status**: Not configured
**Impact**: Data transmitted over HTTP (not secure for production)
**Justification**: Development environment only
**Mitigation**: HTTPS can be added before production deployment

### Environment Variables
**Status**: Passwords in appsettings.json
**Impact**: Sensitive data in repository (not production-ready)
**Justification**: Development convenience
**Mitigation**: Will use environment variables before production deployment
