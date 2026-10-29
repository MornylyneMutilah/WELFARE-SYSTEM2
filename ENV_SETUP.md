# Environment Setup Instructions

## Frontend Environment Variables

Create a `.env.local` file in the `frontend` directory with the following content:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

## Backend Environment Variables

The backend uses `appsettings.json` and `appsettings.Development.json` for non-secret configuration. Supply credentials through environment variables so they are not committed to source control.

### Backend Environment Variables

In PowerShell, set the connection string and a development JWT signing key before starting the API:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=welfare_system;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
$env:Jwt__SecretKey = "replace-with-a-random-key-of-at-least-32-characters"
```

The environment variables override the corresponding settings in both appsettings files. Use a secret manager or deployment environment variables outside local development.

## PostgreSQL Setup

### Option 1: Local PostgreSQL Installation

1. Install PostgreSQL from [https://www.postgresql.org/download/](https://www.postgresql.org/download/)
2. Create a database:
   ```sql
   CREATE DATABASE welfare_system;
   ```
3. Update the connection string in appsettings.json with your PostgreSQL credentials

### Option 2: Docker (Recommended for Development)

```bash
docker run --name welfare-postgres -e POSTGRES_PASSWORD=your_password -e POSTGRES_DB=welfare_system -p 5432:5432 -d postgres:15
```

## Important Notes

- Set `ConnectionStrings__DefaultConnection` to your PostgreSQL connection string
- Set `Jwt__SecretKey` to a secure random string of at least 32 characters
- Never commit actual `.env.local` files to version control
- Use different databases for development and production
