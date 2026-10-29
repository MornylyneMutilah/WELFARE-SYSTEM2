# Welfare Management System

**Intelligent Welfare Management System for Employee Welfare Administration**

A centralized web-based Welfare Management System designed to streamline and automate welfare administration processes for mid-sized universities and institutions.

---

## Project Overview

This system replaces fragmented manual processes (paper forms, spreadsheets, emails) with a unified digital platform that enables:

- Employees to submit welfare applications electronically
- Authorized personnel to review, recommend, approve/reject, and monitor applications
- Rule-based decision support for eligibility assessment
- Anomaly detection for applications requiring additional review
- Comprehensive dashboards and reporting
- Complete audit trails for compliance

---

## Technology Stack

### Frontend
- **Next.js 14+** - React framework with App Router
- **React 18+** - UI library
- **TypeScript 5+** - Type safety
- **Tailwind CSS 3+** - Styling
- **Chart.js / Recharts** - Data visualization
- **Axios** - HTTP client

### Backend
- **ASP.NET Core 8/9** - Web API framework
- **C# 12+** - Programming language
- **Entity Framework Core 8+** - ORM
- **PostgreSQL 15+** - Relational database
- **JWT (System.IdentityModel.Tokens.Jwt)** - Authentication
- **BCrypt.Net-Next** - Password hashing
- **Serilog** - Logging
- **FluentValidation** - Input validation

### Development Tools
- **Visual Studio Community** - ASP.NET Core development
- **VS Code** - Next.js development
- **PostgreSQL** - Database management
- **Git** - Version control

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Next.js Frontend                          │
│              (React + TypeScript + Tailwind)                 │
└─────────────────────────────┬───────────────────────────────┘
                              │ HTTPS + JWT
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core Web API Backend                    │
│                    (C# + .NET 8/9)                          │
│                                                              │
│  Controllers → Services → Repositories → EF Core → DB        │
└─────────────────────────────┬───────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                  PostgreSQL Database                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Prerequisites

### Required Software
- **.NET 8.0 SDK** or later
- **Node.js 18+** and npm
- **PostgreSQL 15+**
- **Git**
- **Visual Studio Community 2022** (for backend development)
- **VS Code** (for frontend development)

### Optional Software
- **Docker** (for PostgreSQL container)
- **Postman** (for API testing)

---

## Installation & Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd WelfareSystem
```

### 2. Database Setup

#### Option A: Local PostgreSQL Installation

1. Install PostgreSQL from [https://www.postgresql.org/download/](https://www.postgresql.org/download/)
2. Create a database:

```sql
CREATE DATABASE welfare_system;
```

#### Option B: Docker (Recommended for Development)

```bash
docker run --name welfare-postgres -e POSTGRES_PASSWORD=your_password -e POSTGRES_DB=welfare_system -p 5432:5432 -d postgres:15
```

### 3. Backend Setup

#### Navigate to Backend Directory

```bash
cd backend
```

#### Restore NuGet Packages

```bash
cd WelfareSystem.Api
dotnet restore
```

#### Configure Backend Secrets

Set these environment variables before starting the API. In PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=welfare_system;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
$env:Jwt__SecretKey = "replace-with-a-random-key-of-at-least-32-characters"
```

These settings override the appsettings files. Never commit actual credentials or signing keys.

#### Run Database Migrations

```bash
dotnet ef database update
```

#### Run the Backend

```bash
dotnet run
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`)

### 4. Frontend Setup

#### Navigate to Frontend Directory

```bash
cd ../../frontend
```

#### Install Dependencies

```bash
npm install
```

#### Configure Environment Variables

Create `frontend/.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

#### Run the Frontend

```bash
npm run dev
```

The frontend will be available at `http://localhost:3000`

### 5. Initial Setup Complete

Once both backend and frontend are running:
1. Open browser to `http://localhost:3000`
2. Register a new account or use seed data users
3. Login with your credentials
4. Explore the system based on your role

---

## Environment Variables

### Backend (appsettings.json)

| Variable | Description | Example |
|----------|-------------|---------|
| ConnectionStrings:DefaultConnection | PostgreSQL connection string | Host=localhost;Database=welfare_system;... |
| Jwt:SecretKey | JWT signing key | your-secret-key-min-32-chars |
| Jwt:Issuer | JWT issuer | WelfareSystem |
| Jwt:Audience | JWT audience | WelfareSystemUsers |
| Jwt:ExpirationMinutes | Token expiration time | 60 |

### Frontend (.env.local)

| Variable | Description | Example |
|----------|-------------|---------|
| NEXT_PUBLIC_API_URL | Backend API URL | http://localhost:5000/api |

---

## Seed Data

The system includes seed data for development and testing. To populate the database with sample data:

```bash
cd backend/WelfareSystem.Api
dotnet run --seed-data
```

This will create:
- 5-10 departments
- 20-30 users across various roles
- 5-8 welfare programs
- 10-15 eligibility rules
- 30-50 sample applications
- Sample reviews and notifications

**Note:** All seed data is synthetic and clearly identified as demo/test data. No real employee welfare data is used.

---

## Project Structure

```
WelfareSystem/
├── backend/                          # ASP.NET Core Web API
│   ├── WelfareSystem.Api/
│   │   ├── Controllers/              # API endpoints
│   │   ├── Services/                 # Business logic
│   │   ├── Repositories/             # Data access
│   │   ├── Models/                   # Entities, DTOs, Enums
│   │   ├── Data/                     # DbContext, SeedData
│   │   ├── Middleware/               # Custom middleware
│   │   └── Program.cs                # Application entry point
│   └── WelfareSystem.Tests/          # Tests
│
├── frontend/                         # Next.js Frontend
│   ├── src/
│   │   ├── app/                      # Next.js App Router
│   │   ├── components/              # React components
│   │   ├── lib/                      # Utilities, API client
│   │   ├── types/                    # TypeScript types
│   │   └── styles/                   # Global styles
│   └── public/                       # Static assets
│
├── docs/                             # Documentation
│   ├── REQUIREMENTS.md               # Requirements document
│   ├── DESIGN.md                     # Design document
│   ├── PHASES.md                     # Development phases
│   ├── API.md                        # API documentation
│   └── DATABASE.md                   # Database documentation
│
├── README.md
└── .gitignore
```

---

## User Roles

The system supports five user roles:

1. **Employee** - Submit and track welfare applications
2. **HR Officer** - Review and recommend applications
3. **Welfare Committee Member** - Review HR recommendations
4. **Institutional Management** - Final approval decisions
5. **Administrator** - System configuration and management

---

## Default Users (After Seed Data)

| Role | Email | Password |
|------|-------|----------|
| Administrator | admin@welfare.edu | Admin123! |
| HR Officer | hr@welfare.edu | Hr123! |
| Committee Member | committee@welfare.edu | Committee123! |
| Management | management@welfare.edu | Management123! |
| Employee | employee@welfare.edu | Employee123! |

---

## API Documentation

API documentation is available in `docs/API.md`. Key endpoints include:

- **Authentication:** `/api/auth/login`, `/api/auth/register`
- **Users:** `/api/users/me`, `/api/users`
- **Departments:** `/api/departments`
- **Welfare Programs:** `/api/welfare-programs`
- **Applications:** `/api/applications`
- **Reviews:** `/api/applications/{id}/reviews`
- **Notifications:** `/api/notifications`
- **Reports:** `/api/reports/*`

---

## Testing

### Backend Tests

```bash
cd backend/WelfareSystem.Tests
dotnet test
```

### Frontend Tests

```bash
cd frontend
npm test
```

### E2E Tests

```bash
cd frontend
npm run test:e2e
```

---

## Development Workflow

1. **Feature Development:** Work on features according to the phased approach in `docs/PHASES.md`
2. **Testing:** Write and run tests for each feature
3. **Documentation:** Update relevant documentation as features are implemented
4. **Code Review:** Ensure code follows project standards and conventions

---

## Documentation

- **[REQUIREMENTS.md](docs/REQUIREMENTS.md)** - Detailed requirements specification
- **[DESIGN.md](docs/DESIGN.md)** - System design and architecture
- **[PHASES.md](docs/PHASES.md)** - Development phases and task breakdown
- **[API.md](docs/API.md)** - API endpoint documentation
- **[DATABASE.md](docs/DATABASE.md)** - Database schema documentation

---

## Security Considerations

- All passwords are hashed using BCrypt
- JWT tokens for authentication
- Role-based authorization on all endpoints
- Input validation on all API inputs
- SQL injection prevention via parameterized queries
- XSS prevention through output encoding
- No secrets committed to source code
- Environment variables for sensitive configuration

---

## Contributing

This is a final-year university project. Contributions should follow the phased development approach outlined in `docs/PHASES.md`.

---

## License

This project is developed for academic purposes.

---

## Contact

For questions or support, please refer to the project documentation or contact the development team.

---

## Acknowledgments

This project is developed as a Final Year Computer Science Project II for university academic requirements.
