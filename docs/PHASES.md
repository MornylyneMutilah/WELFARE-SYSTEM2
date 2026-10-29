# Welfare Management System - Development Phases

**Project:** Intelligent Welfare Management System for University Employee Welfare Administration  
**Project Type:** Final Year Computer Science Project II  
**Version:** 1.0  
**Date:** 2026-08-13

---

## Table of Contents

1. [Phase 1: Project Setup and Architecture](#phase-1-project-setup-and-architecture)
2. [Phase 2: Database and Entity Framework Core](#phase-2-database-and-entity-framework-core)
3. [Phase 3: Authentication and Authorization](#phase-3-authentication-and-authorization)
4. [Phase 4: User and Department Management](#phase-4-user-and-department-management)
5. [Phase 5: Welfare Program Management](#phase-5-welfare-program-management)
6. [Phase 6: Employee Welfare Applications](#phase-6-employee-welfare-applications)
7. [Phase 7: Application Workflow and Reviews](#phase-7-application-workflow-and-reviews)
8. [Phase 8: Rule-Based Eligibility Engine](#phase-8-rule-based-eligibility-engine)
9. [Phase 9: Anomaly Detection](#phase-9-anomaly-detection)
10. [Phase 10: Notifications](#phase-10-notifications)
11. [Phase 11: Dashboards and Reporting](#phase-11-dashboards-and-reporting)
12. [Phase 12: Testing, Security Review, and Documentation](#phase-12-testing-security-review-and-documentation)

---

## Phase 1: Project Setup and Architecture

**Duration:** Week 1  
**Goal:** Establish project structure, configure development environment, and set up basic infrastructure

### Tasks

#### 1.1 Repository Setup
- [ ] Initialize Git repository
- [ ] Create .gitignore file (exclude node_modules, bin, obj, .env, etc.)
- [ ] Create README.md with project overview
- [ ] Set up repository structure (backend/, frontend/, docs/)
- [ ] Create LICENSE file (if applicable)

#### 1.2 Backend Setup (ASP.NET Core)
- [ ] Create new ASP.NET Core Web API project
  ```bash
  dotnet new webapi -n WelfareSystem.Api
  ```
- [ ] Configure project to use .NET 8/9
- [ ] Install required NuGet packages:
  - Entity Framework Core
  - Npgsql (PostgreSQL provider)
  - JWT (System.IdentityModel.Tokens.Jwt)
  - BCrypt.Net-Next
  - Serilog
  - FluentValidation
- [ ] Configure appsettings.json
- [ ] Configure appsettings.Development.json
- [ ] Set up CORS configuration
- [ ] Configure HTTPs for development
- [ ] Create basic folder structure (Controllers, Services, Repositories, Models, Data, Middleware)

#### 1.3 Frontend Setup (Next.js)
- [ ] Create new Next.js project with TypeScript
  ```bash
  npx create-next-app@latest frontend --typescript --tailwind --app
  ```
- [ ] Install required dependencies:
  - axios
  - chart.js or recharts
  - react-hook-form
  - zod
- [ ] Configure Tailwind CSS
- [ ] Create basic folder structure (app/, components/, lib/, types/, styles/)
- [ ] Set up environment variables (.env.local)
- [ ] Configure API base URL

#### 1.4 Database Setup
- [ ] Install PostgreSQL locally or use Docker
- [ ] Create database `welfare_system`
- [ ] Test database connection
- [ ] Document database setup in README

#### 1.5 Development Environment
- [ ] Configure VS Code for Next.js development
- [ ] Configure Visual Studio for ASP.NET Core development
- [ ] Set up ESLint and Prettier for frontend
- [ ] Set up code style rules for backend
- [ ] Create development launch configurations

#### 1.6 Documentation
- [ ] Update README.md with:
  - Project overview
  - Technology stack
  - Prerequisites
  - Setup instructions
  - Environment variables template
- [ ] Create ARCHITECTURE.md (summarized from DESIGN.md)
- [ ] Create API.md (placeholder for API documentation)
- [ ] Create DATABASE.md (placeholder for database documentation)

#### 1.7 Initial Build Test
- [ ] Build backend project successfully
- [ ] Run backend and verify it starts
- [ ] Build frontend project successfully
- [ ] Run frontend and verify it loads
- [ ] Test basic API call from frontend to backend

### Deliverables
- Working repository structure
- Running ASP.NET Core backend
- Running Next.js frontend
- PostgreSQL database ready
- Comprehensive README

---

## Phase 2: Database and Entity Framework Core

**Duration:** Week 1-2  
**Goal:** Design and implement database schema with Entity Framework Core

### Tasks

#### 2.1 Entity Models
- [ ] Create User entity
- [ ] Create Role entity
- [ ] Create Department entity
- [ ] Create WelfareProgram entity
- [ ] Create EligibilityRule entity
- [ ] Create WelfareApplication entity
- [ ] Create ApplicationDocument entity
- [ ] Create ApplicationReview entity
- [ ] Create ApplicationStatusHistory entity
- [ ] Create Notification entity
- [ ] Create AuditLog entity
- [ ] Create enum classes (UserRole, ApplicationStatus, RuleType, AnomalySeverity)

#### 2.2 DbContext Configuration
- [ ] Create AppDbContext class
- [ ] Configure all DbSets
- [ ] Configure entity relationships (Fluent API)
- [ ] Configure indexes for performance
- [ ] Configure constraints (unique, required, etc.)
- [ ] Configure decimal precision for financial fields

#### 2.3 Entity Framework Configuration
- [ ] Add DbContext to DI container in Program.cs
- [ ] Configure connection string in appsettings.json
- [ ] Configure PostgreSQL provider
- [ ] Configure logging for EF Core (development)

#### 2.4 Migrations
- [ ] Create initial migration
  ```bash
  dotnet ef migrations add InitialCreate
  ```
- [ ] Review generated migration SQL
- [ ] Apply migration to database
  ```bash
  dotnet ef database update
  ```
- [ ] Verify database schema in PostgreSQL

#### 2.5 Seed Data
- [ ] Create SeedData class
- [ ] Define seed roles (Employee, HROfficer, CommitteeMember, Management, Administrator)
- [ ] Define seed departments (5-10 departments)
- [ ] Define seed welfare programs (5-8 programs)
- [ ] Define seed eligibility rules for each program
- [ ] Create seed users (20-30 users across roles)
- [ ] Configure seed data to run on development

#### 2.6 Repository Pattern Setup
- [ ] Create repository interfaces (IUserRepository, IDepartmentRepository, etc.)
- [ ] Create base repository interface with common methods
- [ ] Implement base repository class
- [ ] Implement specific repositories
- [ ] Register repositories in DI container

#### 2.7 Testing
- [ ] Test database connection
- [ ] Test entity creation
- [ ] Test relationship navigation
- [ ] Test seed data insertion
- [ ] Verify indexes are created

### Deliverables
- Complete entity models
- Configured DbContext
- Applied database migrations
- Working seed data
- Repository pattern implementation

---

## Phase 3: Authentication and Authorization

**Duration:** Week 2-3  
**Goal:** Implement JWT-based authentication and role-based authorization

### Tasks

#### 3.1 Authentication Models and DTOs
- [ ] Create LoginRequest DTO
- [ ] Create LoginResponse DTO
- [ ] Create RegisterRequest DTO
- [ ] Create RegisterResponse DTO
- [ ] Create RefreshTokenRequest DTO
- [ ] Create UserResponse DTO

#### 3.2 Password Hashing
- [ ] Create PasswordHasher service
- [ ] Implement password hashing using BCrypt
- [ ] Implement password verification
- [ ] Add password complexity validation

#### 3.3 JWT Token Service
- [ ] Create ITokenService interface
- [ ] Implement TokenService
- [ ] Implement access token generation
- [ ] Implement refresh token generation
- [ ] Implement token validation
- [ ] Configure JWT settings (issuer, audience, secret, expiration)

#### 3.4 Authentication Service
- [ ] Create IAuthService interface
- [ ] Implement AuthService
- [ ] Implement login logic
- [ ] Implement registration logic
- [ ] Implement token refresh logic
- [ ] Implement logout logic
- [ ] Add user existence validation
- [ ] Add credential validation

#### 3.5 JWT Middleware
- [ ] Create JwtMiddleware
- [ ] Implement token extraction from Authorization header
- [ ] Implement token validation
- [ ] Implement user context attachment to HttpContext
- [ ] Configure middleware in Program.cs

#### 3.6 Authorization Configuration
- [ ] Configure authentication in Program.cs
- [ ] Configure authorization policies (by role)
- [ ] Configure JWT bearer options
- [ ] Add role claim configuration

#### 3.7 Auth Controller
- [ ] Create AuthController
- [ ] Implement POST /api/auth/login
- [ ] Implement POST /api/auth/register
- [ ] Implement POST /api/auth/refresh
- [ ] Implement POST /api/auth/logout
- [ ] Add input validation using FluentValidation
- [ ] Add error handling

#### 3.8 Frontend Authentication Setup
- [ ] Create AuthContext for React
- [ ] Create AuthProvider component
- [ ] Implement login form component
- [ ] Implement register form component
- [ ] Create token utility functions (storage, retrieval, clearing)
- [ ] Create API client with token interceptor
- [ ] Implement protected route wrapper
- [ ] Create logout functionality

#### 3.9 Frontend Auth Pages
- [ ] Create /login page
- [ ] Create /register page
- [ ] Add form validation
- [ ] Add error handling
- [ ] Add loading states
- [ ] Add redirect after successful login

#### 3.10 Testing
- [ ] Test login with valid credentials
- [ ] Test login with invalid credentials
- [ ] Test registration with new user
- [ ] Test registration with duplicate email
- [ ] Test token refresh
- [ ] Test logout
- [ ] Test protected route access
- [ ] Test unauthorized access rejection

### Deliverables
- Working authentication system
- JWT token implementation
- Role-based authorization configuration
- Login and register pages
- Protected route system

---

## Phase 4: User and Department Management

**Duration:** Week 3  
**Goal:** Implement user and department CRUD operations

### Tasks

#### 4.1 User DTOs
- [ ] CreateUserRequest DTO
- [ ] UpdateUserRequest DTO
- [ ] UserResponse DTO
- [ ] UserListResponse DTO

#### 4.2 Department DTOs
- [ ] CreateDepartmentRequest DTO
- [ ] UpdateDepartmentRequest DTO
- [ ] DepartmentResponse DTO
- [ ] DepartmentListResponse DTO

#### 4.3 User Service
- [ ] Create IUserService interface
- [ ] Implement UserService
- [ ] Implement get current user
- [ ] Implement get user by ID
- [ ] Implement get all users (with pagination)
- [ ] Implement create user
- [ ] Implement update user
- [ ] Implement deactivate user
- [ ] Implement assign role
- [ ] Implement assign department

#### 4.4 Department Service
- [ ] Create IDepartmentService interface
- [ ] Implement DepartmentService
- [ ] Implement get all departments
- [ ] Implement get department by ID
- [ ] Implement create department
- [ ] Implement update department
- [ ] Implement deactivate department

#### 4.5 User Controller
- [ ] Create UsersController
- [ ] Implement GET /api/users/me
- [ ] Implement PUT /api/users/me
- [ ] Implement GET /api/users (Admin only)
- [ ] Implement GET /api/users/{id} (Admin/HR)
- [ ] Implement PUT /api/users/{id} (Admin only)
- [ ] Implement DELETE /api/users/{id} (Admin only)
- [ ] Add role-based authorization attributes
- [ ] Add input validation

#### 4.6 Department Controller
- [ ] Create DepartmentsController
- [ ] Implement GET /api/departments
- [ ] Implement GET /api/departments/{id}
- [ ] Implement POST /api/departments (Admin only)
- [ ] Implement PUT /api/departments/{id} (Admin only)
- [ ] Add role-based authorization attributes
- [ ] Add input validation

#### 4.7 Frontend User Components
- [ ] Create UserProfile component
- [ ] Create UserList component (Admin)
- [ ] CreateUserForm component (Admin)
- [ ] UpdateUserForm component (Admin)
- [ ] Create API client functions for users

#### 4.8 Frontend Department Components
- [ ] Create DepartmentList component (Admin)
- [ ] CreateDepartmentForm component (Admin)
- [ ] UpdateDepartmentForm component (Admin)
- [ ] Create API client functions for departments

#### 4.9 Frontend Pages
- [ ] Create /employee/profile page
- [ ] Create /admin/users page
- [ ] Create /admin/departments page

#### 4.10 Testing
- [ ] Test user profile view
- [ ] Test user profile update
- [ ] Test user creation (Admin)
- [ ] Test user update (Admin)
- [ ] Test user deactivation (Admin)
- [ ] Test department listing
- [ ] Test department creation (Admin)
- [ ] Test department update (Admin)
- [ ] Test role-based access control

### Deliverables
- User management API
- Department management API
- User profile page
- Admin user management pages
- Admin department management pages

---

## Phase 5: Welfare Program Management

**Duration:** Week 4  
**Goal:** Implement welfare program CRUD and configuration

### Tasks

#### 5.1 Welfare Program DTOs
- [ ] CreateWelfareProgramRequest DTO
- [ ] UpdateWelfareProgramRequest DTO
- [ ] WelfareProgramResponse DTO
- [ ] WelfareProgramListResponse DTO

#### 5.2 Eligibility Rule DTOs
- [ ] CreateEligibilityRuleRequest DTO
- [ ] UpdateEligibilityRuleRequest DTO
- [ ] EligibilityRuleResponse DTO

#### 5.3 Welfare Program Service
- [ ] Create IWelfareProgramService interface
- [ ] Implement WelfareProgramService
- [ ] Implement get all programs
- [ ] Implement get active programs only
- [ ] Implement get program by ID
- [ ] Implement create program
- [ ] Implement update program
- [ ] Implement deactivate program

#### 5.4 Eligibility Rule Service
- [ ] Create IEligibilityRuleService interface
- [ ] Implement EligibilityRuleService
- [ ] Implement get rules by program
- [ ] Implement create rule
- [ ] Implement update rule
- [ ] Implement deactivate rule

#### 5.5 Welfare Program Controller
- [ ] Create WelfareProgramsController
- [ ] Implement GET /api/welfare-programs (public)
- [ ] Implement GET /api/welfare-programs/active (public)
- [ ] Implement GET /api/welfare-programs/{id} (public)
- [ ] Implement POST /api/welfare-programs (Admin only)
- [ ] Implement PUT /api/welfare-programs/{id} (Admin only)
- [ ] Implement DELETE /api/welfare-programs/{id} (Admin only)
- [ ] Add role-based authorization

#### 5.6 Eligibility Rule Controller
- [ ] Create EligibilityRulesController
- [ ] Implement GET /api/welfare-programs/{id}/rules
- [ ] Implement POST /api/welfare-programs/{id}/rules (Admin only)
- [ ] Implement PUT /api/eligibility-rules/{id} (Admin only)
- [ ] Add role-based authorization

#### 5.7 Frontend Welfare Program Components
- [ ] Create WelfareProgramList component
- [ ] Create WelfareProgramCard component
- [ ] CreateWelfareProgramForm component (Admin)
- [ ] UpdateWelfareProgramForm component (Admin)
- [ ] Create EligibilityRuleForm component (Admin)
- [ ] Create API client functions for programs

#### 5.8 Frontend Pages
- [ ] Create program listing page (public - for viewing)
- [ ] Create /admin/welfare-programs page
- [ ] Create /admin/eligibility-rules page

#### 5.9 Testing
- [ ] Test program listing
- [ ] Test program creation (Admin)
- [ ] Test program update (Admin)
- [ ] Test program deactivation (Admin)
- [ ] Test rule creation (Admin)
- [ ] Test rule update (Admin)
- [ ] Test active programs filter
- [ ] Test role-based access control

### Deliverables
- Welfare program management API
- Eligibility rule management API
- Program listing page
- Admin program management pages
- Admin rule management pages

---

## Phase 6: Employee Welfare Applications

**Duration:** Week 4-5  
**Goal:** Implement application creation, document upload, and listing

### Tasks

#### 6.1 Application DTOs
- [ ] CreateApplicationRequest DTO
- [ ] UpdateApplicationRequest DTO
- [ ] ApplicationResponse DTO
- [ ] ApplicationListResponse DTO
- [ ] ApplicationDetailResponse DTO

#### 6.2 Document DTOs
- [ ] DocumentUploadResponse DTO
- [ ] DocumentResponse DTO

#### 6.3 Welfare Application Service
- [ ] Create IWelfareApplicationService interface
- [ ] Implement WelfareApplicationService
- [ ] Implement get application by ID
- [ ] Implement get applications by user
- [ ] Implement get all applications (with filters)
- [ ] Implement create draft application
- [ ] Implement update draft application
- [ ] Implement submit application
- [ ] Implement cancel draft application

#### 6.4 Document Service
- [ ] Create IDocumentService interface
- [ ] Implement DocumentService
- [ ] Implement file upload validation
- [ ] Implement file storage
- [ ] Implement document upload
- [ ] Implement document deletion
- [ ] Implement document retrieval

#### 6.5 Welfare Application Controller
- [ ] Create ApplicationsController
- [ ] Implement GET /api/applications (filtered by role)
- [ ] Implement GET /api/applications/{id}
- [ ] Implement POST /api/applications
- [ ] Implement PUT /api/applications/{id} (draft only)
- [ ] Implement POST /api/applications/{id}/submit
- [ ] Implement DELETE /api/applications/{id} (draft only)
- [ ] Add role-based authorization

#### 6.6 Document Controller
- [ ] Implement POST /api/applications/{id}/documents
- [ ] Implement GET /api/applications/{id}/documents
- [ ] Implement DELETE /api/applications/{id}/documents/{docId}
- [ ] Add file upload handling
- [ ] Add role-based authorization

#### 6.7 Frontend Application Components
- [ ] Create ApplicationForm component
- [ ] Create ProgramSelector component
- [ ] Create DocumentUpload component
- [ ] Create ApplicationList component
- [ ] Create ApplicationCard component
- [ ] Create ApplicationDetail component
- [ ] Create StatusBadge component
- [ ] Create API client functions for applications

#### 6.8 Frontend Pages
- [ ] Create /employee/applications page
- [ ] Create /employee/applications/new page
- [ ] Create /employee/applications/[id] page

#### 6.9 Testing
- [ ] Test draft application creation
- [ ] Test draft application update
- [ ] Test application submission
- [ ] Test document upload
- [ ] Test document deletion
- [ ] Test application listing
- [ ] Test application detail view
- [ ] Test draft cancellation
- [ ] Test role-based access control

### Deliverables
- Application management API
- Document upload API
- Application form
- Application listing page
- Application detail page
- Document upload component

---

## Phase 7: Application Workflow and Reviews

**Duration:** Week 5-6  
**Goal:** Implement review workflow for HR, Committee, and Management

### Tasks

#### 7.1 Review DTOs
- [ ] CreateReviewRequest DTO
- [ ] UpdateReviewRequest DTO
- [ ] ReviewResponse DTO
- [ ] StatusUpdateRequest DTO

#### 7.2 Status History Service
- [ ] Create IStatusHistoryService interface
- [ ] Implement StatusHistoryService
- [ ] Implement status change logging
- [ ] Implement status history retrieval

#### 7.3 Review Service
- [ ] Create IReviewService interface
- [ ] Implement ReviewService
- [ ] Implement get reviews by application
- [ ] Implement create review
- [ ] Implement update review
- [ ] Implement status transition logic
- [ ] Implement request additional info

#### 7.4 Workflow Service
- [ ] Create IWorkflowService interface
- [ ] Implement WorkflowService
- [ ] Implement automatic status transitions
- [ ] Implement routing logic (HR → Committee → Management)
- [ ] Implement workflow validation

#### 7.5 Review Controller
- [ ] Create ReviewsController
- [ ] Implement GET /api/applications/{id}/reviews
- [ ] Implement POST /api/applications/{id}/reviews
- [ ] Implement PUT /api/applications/{id}/reviews/{reviewId}
- [ ] Add role-based authorization (HR/Committee/Management)

#### 7.6 Status Update Controller
- [ ] Implement POST /api/applications/{id}/status (Management only)
- [ ] Implement status change validation
- [ ] Add audit logging

#### 7.7 Frontend Review Components
- [ ] Create ReviewForm component (HR)
- [ ] Create ReviewForm component (Committee)
- [ ] Create ApprovalForm component (Management)
- [ ] Create ReviewHistory component
- [ ] Create StatusTimeline component
- [ ] Create AdditionalInfoRequest component
- [ ] Create API client functions for reviews

#### 7.8 Frontend Pages
- [ ] Create /hr/applications page
- [ ] Create /hr/applications/[id] page
- [ ] Create /committee/applications page
- [ ] Create /committee/applications/[id] page
- [ ] Create /management/applications page
- [ ] Create /management/applications/[id] page

#### 7.9 Testing
- [ ] Test HR review submission
- [ ] Test Committee review submission
- [ ] Test Management approval
- [ ] Test Management rejection
- [ ] Test status transitions
- [ ] Test additional info request
- [ ] Test review history display
- [ ] Test workflow routing
- [ ] Test role-based access control

### Deliverables
- Review workflow API
- Status management API
- HR review pages
- Committee review pages
- Management approval pages
- Status timeline component

---

## Phase 8: Rule-Based Eligibility Engine

**Duration:** Week 6-7  
**Goal:** Implement configurable rule engine for eligibility assessment

### Tasks

#### 8.1 Rule Engine Models
- [ ] Create IRule interface
- [ ] Create RuleResult class
- [ ] Create RuleEvaluationResult class
- [ ] Create RuleEvaluation class
- [ ] Create rule configuration classes (ServicePeriodConfig, AmountLimitConfig, etc.)

#### 8.2 Rule Implementations
- [ ] Implement ServicePeriodRule
- [ ] Implement AmountLimitRule
- [ ] Implement FrequencyRule
- [ ] Implement DocumentRequirementRule
- [ ] Implement EligibilityCriteriaRule
- [ ] Implement DuplicateApplicationRule

#### 8.3 Rule Engine Service
- [ ] Create IRuleEngineService interface
- [ ] Implement RuleEngineService
- [ ] Implement rule registration
- [ ] Implement rule evaluation logic
- [ ] Implement result compilation
- [ ] Add rule dependency resolution

#### 8.4 Integration with Application Service
- [ ] Call rule engine on application submission
- [ ] Store rule evaluation results
- [ ] Update application eligibility flag
- [ ] Return rule results in API response

#### 8.5 Frontend Rule Display Components
- [ ] Create RuleEvaluationResult component
- [ ] Create RuleCard component
- [ ] Create PassedRulesList component
- [ ] Create FailedRulesList component
- [ ] Create WarningBanner component

#### 8.6 Frontend Integration
- [ ] Display rule results in application detail
- [ ] Display rule results in review pages
- [ ] Add rule result indicators in application list

#### 8.7 Testing
- [ ] Test service period rule
- [ ] Test amount limit rule
- [ ] Test frequency rule
- [ ] Test document requirement rule
- [ ] Test eligibility criteria rule
- [ ] Test duplicate application rule
- [ ] Test rule engine integration
- [ ] Test rule result display
- [ ] Test rule configuration changes

### Deliverables
- Rule engine service
- Rule implementations
- Rule evaluation API integration
- Rule result display components
- Configurable rule system

---

## Phase 9: Anomaly Detection

**Duration:** Week 7  
**Goal:** Implement deterministic anomaly detection

### Tasks

#### 9.1 Anomaly Detection Models
- [ ] Create IAnomalyDetector interface
- [ ] Create AnomalyFlag class
- [ ] Create AnomalySeverity enum

#### 9.2 Anomaly Detector Implementations
- [ ] Implement DuplicateDetector
- [ ] Implement FrequencyAbuseDetector
- [ ] Implement AmountAnomalyDetector
- [ ] Implement DocumentAnomalyDetector
- [ ] Implement PatternAnomalyDetector

#### 9.3 Anomaly Detection Service
- [ ] Create IAnomalyDetectionService interface
- [ ] Implement AnomalyDetectionService
- [ ] Implement detector registration
- [ ] Implement anomaly detection orchestration
- [ ] Implement severity calculation
- [ ] Implement explanation generation

#### 9.4 Integration with Application Service
- [ ] Call anomaly detection on application submission
- [ ] Store anomaly flags
- [ ] Return anomaly flags in API response
- [ ] Add anomaly severity filtering

#### 9.5 Frontend Anomaly Display Components
- [ ] Create AnomalyFlag component
- [ ] Create AnomalyList component
- [ ] Create AnomalyBanner component
- [ ] Create severity badges (High/Medium/Low)

#### 9.6 Frontend Integration
- [ ] Display anomaly flags in application detail
- [ ] Display anomaly flags in review pages
- [ ] Add anomaly indicators in application list
- [ ] Add anomaly filtering

#### 9.7 Testing
- [ ] Test duplicate detection
- [ ] Test frequency abuse detection
- [ ] Test amount anomaly detection
- [ ] Test document anomaly detection
- [ ] Test pattern anomaly detection
- [ ] Test anomaly detection integration
- [ ] Test anomaly display
- [ ] Test severity calculation

### Deliverables
- Anomaly detection service
- Anomaly detector implementations
- Anomaly detection API integration
- Anomaly display components
- Anomaly filtering

---

## Phase 10: Notifications

**Duration:** Week 7-8  
**Goal:** Implement in-system notification system

### Tasks

#### 10.1 Notification DTOs
- [ ] NotificationResponse DTO
- [ ] NotificationListResponse DTO
- [ ] MarkAsReadRequest DTO

#### 10.2 Notification Service
- [ ] Create INotificationService interface
- [ ] Implement NotificationService
- [ ] Implement get user notifications
- [ ] Implement get unread notifications
- [ ] Implement create notification
- [ ] Implement mark as read
- [ ] Implement mark all as read
- [ ] Implement notification triggers

#### 10.3 Notification Triggers
- [ ] Trigger on application submission (to employee)
- [ ] Trigger on HR assignment (to HR)
- [ ] Trigger on committee forwarding (to committee)
- [ ] Trigger on management forwarding (to management)
- [ ] Trigger on status change (to employee)
- [ ] Trigger on additional info request (to employee)
- [ ] Trigger on approval (to employee)
- [ ] Trigger on rejection (to employee)

#### 10.4 Notification Controller
- [ ] Create NotificationsController
- [ ] Implement GET /api/notifications
- [ ] Implement GET /api/notifications/unread-count
- [ ] Implement PUT /api/notifications/{id}/read
- [ ] Implement PUT /api/notifications/read-all
- [ ] Add user-specific authorization

#### 10.5 Frontend Notification Components
- [ ] Create NotificationIcon component
- [ ] Create NotificationDropdown component
- [ ] Create NotificationList component
- [ ] Create NotificationItem component
- [ ] Create NotificationBadge component
- [ ] Create API client functions for notifications

#### 10.6 Frontend Integration
- [ ] Add notification icon to header
- [ ] Add unread count display
- [ ] Implement notification polling or websockets
- [ ] Create /notifications page
- [ ] Add notification link in sidebar

#### 10.7 Testing
- [ ] Test notification creation
- [ ] Test notification retrieval
- [ ] Test mark as read
- [ ] Test mark all as read
- [ ] Test unread count
- [ ] Test notification triggers
- [ ] Test notification display
- [ ] Test notification filtering

### Deliverables
- Notification service
- Notification API
- Notification triggers
- Notification components
- Notification display

---

## Phase 11: Dashboards and Reporting

**Duration:** Week 8-9  
**Goal:** Implement role-specific dashboards and reporting

### Tasks

#### 11.1 Dashboard DTOs
- [ ] EmployeeDashboardStats DTO
- [ ] HRDashboardStats DTO
- [ ] CommitteeDashboardStats DTO
- [ ] ManagementDashboardStats DTO
- [ ] AdminDashboardStats DTO

#### 11.2 Report DTOs
- [ ] ApplicationsByStatusReport DTO
- [ ] ApplicationsByDepartmentReport DTO
- [ ] ApplicationsByProgramReport DTO
- [ ] ExpenditureReport DTO
- [ ] ProcessingTimeReport DTO
- [ ] FlaggedApplicationsReport DTO

#### 11.3 Dashboard Service
- [ ] Create IDashboardService interface
- [ ] Implement DashboardService
- [ ] Implement get employee dashboard stats
- [ ] Implement get HR dashboard stats
- [ ] Implement get committee dashboard stats
- [ ] Implement get management dashboard stats
- [ ] Implement get admin dashboard stats

#### 11.4 Report Service
- [ ] Create IReportService interface
- [ ] Implement ReportService
- [ ] Implement applications by status report
- [ ] Implement applications by department report
- [ ] Implement applications by program report
- [ ] Implement expenditure report
- [ ] Implement processing time report
- [ ] Implement flagged applications report
- [ ] Implement CSV export functionality

#### 11.5 Dashboard Controller
- [ ] Create DashboardsController
- [ ] Implement GET /api/dashboard/employee
- [ ] Implement GET /api/dashboard/hr
- [ ] Implement GET /api/dashboard/committee
- [ ] Implement GET /api/dashboard/management
- [ ] Implement GET /api/dashboard/admin
- [ ] Add role-based authorization

#### 11.6 Report Controller
- [ ] Create ReportsController
- [ ] Implement GET /api/reports/applications-by-status
- [ ] Implement GET /api/reports/applications-by-department
- [ ] Implement GET /api/reports/applications-by-program
- [ ] Implement GET /api/reports/expenditure
- [ ] Implement GET /api/reports/processing-time
- [ ] Implement GET /api/reports/flagged-applications
- [ ] Add date range filtering
- [ ] Add CSV export endpoints

#### 11.7 Frontend Dashboard Components
- [ ] Create DashboardCard component
- [ ] Create StatCard component
- [ ] Create ChartContainer component
- [ ] Create EmployeeDashboard component
- [ ] Create HRDashboard component
- [ ] Create CommitteeDashboard component
- [ ] Create ManagementDashboard component
- [ ] Create AdminDashboard component

#### 11.8 Frontend Chart Components
- [ ] Set up Chart.js or Recharts
- [ ] Create PieChart component
- [ ] Create BarChart component
- [ ] Create LineChart component
- [ ] Create DoughnutChart component

#### 11.9 Frontend Report Components
- [ ] Create ReportFilters component
- [ ] Create ReportTable component
- [ ] Create ExportButton component
- [ ] Create DateRangePicker component

#### 11.10 Frontend Pages
- [ ] Create /employee/dashboard page
- [ ] Create /hr/dashboard page
- [ ] Create /hr/reports page
- [ ] Create /committee/dashboard page
- [ ] Create /committee/reports page
- [ ] Create /management/dashboard page
- [ ] Create /management/reports page
- [ ] Create /admin/dashboard page

#### 11.11 Testing
- [ ] Test dashboard stats accuracy
- [ ] Test chart rendering
- [ ] Test report generation
- [ ] Test date range filtering
- [ ] Test CSV export
- [ ] Test role-based dashboard access
- [ ] Test dashboard performance

### Deliverables
- Dashboard service
- Report service
- Dashboard API
- Report API
- Dashboard pages for all roles
- Report pages
- Chart components
- CSV export functionality

---

## Phase 12: Testing, Security Review, and Documentation

**Duration:** Week 9-10  
**Goal:** Comprehensive testing, security review, UI refinement, and documentation

### Tasks

#### 12.1 Backend Unit Tests
- [ ] Set up xUnit test project
- [ ] Write unit tests for AuthService
- [ ] Write unit tests for UserService
- [ ] Write unit tests for WelfareService
- [ ] Write unit tests for RuleEngineService
- [ ] Write unit tests for AnomalyDetectionService
- [ ] Write unit tests for NotificationService
- [ ] Write unit tests for ReportService

#### 12.2 Backend Integration Tests
- [ ] Set up integration test infrastructure
- [ ] Write integration tests for AuthController
- [ ] Write integration tests for UsersController
- [ ] Write integration tests for ApplicationsController
- [ ] Write integration tests for ReviewsController
- [ ] Write integration tests for DashboardsController
- [ ] Write integration tests for ReportsController

#### 12.3 Frontend Component Tests
- [ ] Set up Jest and React Testing Library
- [ ] Write tests for LoginForm
- [ ] Write tests for ApplicationForm
- [ ] Write tests for ReviewForm
- [ ] Write tests for Dashboard components
- [ ] Write tests for common components

#### 12.4 End-to-End Tests
- [ ] Set up Playwright or Cypress
- [ ] Write E2E test for login flow
- [ ] Write E2E test for application submission
- [ ] Write E2E test for HR review
- [ ] Write E2E test for Committee review
- [ ] Write E2E test for Management approval
- [ ] Write E2E test for complete workflow

#### 12.5 Security Review
- [ ] Review all endpoints for SQL injection vulnerabilities
- [ ] Review all endpoints for XSS vulnerabilities
- [ ] Review authentication implementation
- [ ] Review authorization implementation
- [ ] Review input validation
- [ ] Review error handling (no information leakage)
- [ ] Review file upload security
- [ ] Review dependency vulnerabilities
- [ ] Review CORS configuration
- [ ] Review rate limiting implementation

#### 12.6 Performance Testing
- [ ] Test API response times
- [ ] Test database query performance
- [ ] Test dashboard load times
- [ ] Test file upload performance
- [ ] Add database indexes if needed
- [ ] Optimize slow queries

#### 12.7 UI/UX Refinement
- [ ] Review all pages for consistency
- [ ] Improve responsive design
- [ ] Add loading states where missing
- [ ] Improve error messages
- [ ] Add form validation feedback
- [ ] Improve accessibility (ARIA labels, keyboard navigation)
- [ ] Test on different screen sizes
- [ ] Test on different browsers

#### 12.8 Documentation Completion
- [ ] Update README with final setup instructions
- [ ] Complete API.md with all endpoints
- [ ] Complete DATABASE.md with schema documentation
- [ ] Create DEPLOYMENT.md
- [ ] Create TESTING.md with test instructions
- [ ] Create USER_GUIDE.md for end users
- [ ] Add code comments where complex
- [ ] Document environment variables
- [ ] Document seed data

#### 12.9 Seed Data Enhancement
- [ ] Enhance seed data with realistic examples
- [ ] Add edge case applications
- [ ] Add flagged applications
- [ ] Add applications at various workflow stages
- [ ] Add sample reviews and notifications
- [ ] Add sample audit logs

#### 12.10 Demo Preparation
- [ ] Prepare demo script
- [ ] Create demo user accounts
- [ ] Verify all features work for demo
- [ ] Prepare backup/restore instructions
- [ ] Create demo data reset script

#### 12.11 Manual Testing Checklist
- [ ] Complete user acceptance testing checklist
- [ ] Test all role-based workflows
- [ ] Test all error scenarios
- [ ] Test all edge cases
- [ ] Cross-browser testing
- [ ] Mobile testing

#### 12.12 Final Review
- [ ] Code review all changes
- [ ] Architecture review
- [ ] Security sign-off
- [ ] Performance sign-off
- [ ] Documentation review
- [ ] Project scope verification

### Deliverables
- Comprehensive test suite
- Security review report
- Performance optimization
- Refined UI/UX
- Complete documentation
- Enhanced seed data
- Demo-ready system
- User acceptance testing checklist

---

## Summary

This phase breakdown provides a structured approach to building the Welfare Management System over approximately 10 weeks. Each phase builds upon the previous one, ensuring:

1. **Incremental Progress:** Working functionality at each phase end
2. **Testable Components:** Each phase can be tested independently
3. **Clear Deliverables:** Specific outputs for each phase
4. **Risk Mitigation:** Early detection of issues
5. **Academic Appropriateness:** Scope suitable for student project

**Key Principles:**
- Complete each phase before moving to the next
- Test thoroughly within each phase
- Document as you go
- Keep architecture consistent
- Prioritize working features over perfect code
- Seek feedback at phase completions

---

## Document Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-13 | Initial | Initial phase breakdown |
