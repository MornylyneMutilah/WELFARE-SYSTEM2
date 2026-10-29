# Welfare Management System Deployment Guide

## Prerequisites

- **Server:** Linux/Windows Server with .NET 8.0 Runtime
- **Database:** PostgreSQL 15+ (can be on same or separate server)
- **Web Server:** IIS (Windows) or Nginx/Apache (Linux)
- **SSL Certificate:** For HTTPS (required for production)
- **Domain Name:** Optional but recommended

## Production Deployment Steps

### 1. Database Setup

#### Create Production Database

```sql
CREATE DATABASE welfare_system_prod;
CREATE USER welfare_user WITH PASSWORD 'strong_password_here';
GRANT ALL PRIVILEGES ON DATABASE welfare_system_prod TO welfare_user;
```

#### Configure Connection String

Update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-db-host;Port=5432;Database=welfare_system_prod;Username=welfare_user;Password=strong_password_here"
  },
  "Jwt": {
    "SecretKey": "REPLACE_WITH_SECURE_RANDOM_KEY_AT_LEAST_32_CHARS",
    "Issuer": "WelfareSystem",
    "Audience": "WelfareSystemUsers",
    "ExpirationMinutes": 60
  }
}
```

**IMPORTANT:** 
- Use a strong, randomly generated JWT secret key
- Store secrets in environment variables or secret management system
- Never commit production secrets to source control

### 2. Backend Deployment

#### Build for Production

```bash
cd backend/WelfareSystem.Api
dotnet publish -c Release -o ./publish
```

#### Option A: Windows with IIS

1. Install ASP.NET Core Hosting Bundle
2. Create IIS site pointing to `publish` folder
3. Configure web.config (auto-generated)
4. Enable HTTPS with SSL certificate
5. Set appropriate application pool identity

#### Option B: Linux with Nginx

1. Copy `publish` folder to server
2. Create systemd service:

```ini
[Unit]
Description=Welfare System API
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/var/www/welfare-system
ExecStart=/usr/bin/dotnet /var/www/welfare-system/WelfareSystem.Api.dll
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

3. Configure Nginx:

```nginx
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### 3. Frontend Deployment

#### Build for Production

```bash
cd frontend
npm run build
```

#### Option A: Serve from Backend

Copy `.next` folder to backend `wwwroot` and configure static file serving in `Program.cs`:

```csharp
app.UseStaticFiles();
app.UseFileServer();
```

#### Option B: Separate Web Server

1. Upload `.next` folder to web server
2. Use Node.js server or serve with Nginx:

```nginx
server {
    listen 443 ssl http2;
    server_name your-domain.com;

    root /var/www/welfare-system/.next;
    try_files $uri $uri/ /index.html;
}
```

#### Option C: Vercel/Netlify (Recommended for Next.js)

1. Connect repository to Vercel/Netlify
2. Configure build command: `npm run build`
3. Configure output directory: `.next`
4. Set environment variables
5. Deploy automatically on push

### 4. Environment Variables

#### Backend Environment Variables

```bash
# Connection String
ConnectionStrings__DefaultConnection=Host=your-db-host;Database=welfare_system_prod;...

# JWT Configuration
Jwt__SecretKey=your-secure-secret-key
Jwt__Issuer=WelfareSystem
Jwt__Audience=WelfareSystemUsers
Jwt__ExpirationMinutes=60

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://your-domain.com
```

#### Frontend Environment Variables

```bash
NEXT_PUBLIC_API_URL=https://your-domain.com/api
```

### 5. Database Migration

Run migrations on production database:

```bash
dotnet ef database update --connection "YourProductionConnectionString"
```

### 6. SSL/TLS Configuration

#### Self-Signed Certificate (Development Only)

```bash
dotnet dev-certs https -ep $HOME/.aspnet/https/aspnetapp.pfx -p YourPassword123
```

#### Production Certificate

- Obtain SSL certificate from Let's Encrypt (free) or commercial CA
- Configure web server to use certificate
- Enforce HTTPS redirect
- Set appropriate security headers

### 7. Security Hardening

#### Web Server Security

- Enable HSTS (HTTP Strict Transport Security)
- Configure security headers
- Disable unnecessary HTTP methods
- Implement rate limiting
- Enable request size limits

#### Application Security

- Ensure JWT secret is strong
- Enable CORS only for trusted origins
- Implement rate limiting on auth endpoints
- Enable request logging
- Monitor for suspicious activity

### 8. Monitoring and Logging

#### Application Logging

Configure Serilog in `appsettings.Production.json`:

```json
{
  "Serilog": {
    "MinimumLevel": "Warning",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/welfare-system/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

#### Health Checks

Add health check endpoint in `Program.cs`:

```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

### 9. Backup Strategy

#### Database Backups

```bash
# Daily backup
pg_dump welfare_system_prod > backup_$(date +%Y%m%d).sql

# Automated with cron job
0 2 * * * pg_dump welfare_system_prod > /backups/daily/backup_$(date +\%Y\%m\%d).sql
```

#### Application Backups

- Backup compiled application code
- Backup configuration files (separately)
- Keep version control of all code

### 10. Post-Deployment Checklist

- [ ] Database connection successful
- [ ] All migrations applied
- [ ] SSL certificate valid
- [ ] HTTPS enforced
- [ ] CORS configured correctly
- [ ] Seed data (if needed)
- [ ] Test login functionality
- [ ] Test application submission
- [ ] Test all user roles
- [ ] Verify notifications working
- [ ] Check dashboard functionality
- [ ] Test file uploads
- [ ] Monitor error logs
- [ ] Verify backup strategy

## Troubleshooting

### Common Issues

**Database Connection Failed:**
- Check connection string
- Verify database server is running
- Check firewall rules
- Verify user permissions

**CORS Errors:**
- Check CORS configuration in Program.cs
- Verify frontend URL is allowed
- Check preflight requests

**JWT Token Issues:**
- Verify secret key matches
- Check token expiration
- Verify issuer/audience match

**File Upload Fails:**
- Check file size limits
- Verify upload directory permissions
- Check storage space

## Maintenance

### Regular Tasks

- **Daily:** Monitor error logs, check database backups
- **Weekly:** Review security logs, check disk space
- **Monthly:** Update dependencies, review performance metrics
- **Quarterly:** Security audit, backup restore test

### Updates

1. Update dependencies in `csproj` and `package.json`
2. Test thoroughly in staging environment
3. Create database backup before migrations
4. Deploy during low-traffic hours
5. Monitor for issues after deployment

## Rollback Procedure

If deployment fails:

1. Revert to previous application version
2. Restore database from backup if schema changed
3. Verify system functionality
4. Investigate failure cause
5. Fix and re-deploy

## Support

For deployment issues:
- Check application logs
- Review database logs
- Consult this guide
- Check official .NET and Next.js documentation
