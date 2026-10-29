# Welfare Management System API Documentation

## Base URL
```
http://localhost:5000/api
```

## Authentication

All endpoints except `/api/auth/*` require JWT authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Endpoints

### Authentication

#### POST /api/auth/login
Login to the system.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response:**
```json
{
  "token": "jwt-token",
  "refreshToken": "refresh-token",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Employee"
  }
}
```

#### POST /api/auth/register
Register a new user.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "Password123!",
  "firstName": "John",
  "lastName": "Doe",
  "employeeId": "EMP001",
  "departmentId": 1
}
```

**Response:**
```json
{
  "token": "jwt-token",
  "refreshToken": "refresh-token",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Employee"
  }
}
```

#### POST /api/auth/refresh
Refresh an expired JWT token.

**Request Body:**
```json
{
  "refreshToken": "refresh-token"
}
```

**Response:**
```json
{
  "token": "new-jwt-token",
  "refreshToken": "new-refresh-token"
}
```

### Users

#### GET /api/users/me
Get current user profile.

**Response:**
```json
{
  "id": 1,
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "employeeId": "EMP001",
  "departmentId": 1,
  "role": "Employee"
}
```

#### GET /api/users
Get all users (Admin only).

**Response:**
```json
[
  {
    "id": 1,
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "employeeId": "EMP001",
    "departmentId": 1,
    "role": "Employee"
  }
]
```

### Departments

#### GET /api/departments
Get all departments.

**Response:**
```json
[
  {
    "id": 1,
    "name": "Engineering",
    "description": "Engineering Department"
  }
]
```

### Welfare Programs

#### GET /api/welfare-programs
Get all welfare programs.

**Response:**
```json
[
  {
    "id": 1,
    "name": "Medical Assistance",
    "description": "Financial assistance for medical expenses",
    "category": "Medical",
    "maxAmount": 5000.00,
    "minServicePeriod": 6,
    "requiredDocuments": "[\"Medical Report\", \"Receipts\"]",
    "isActive": true
  }
]
```

#### GET /api/welfare-programs/{id}
Get a specific welfare program.

#### GET /api/welfare-programs/public
Get all active public programs (no auth required).

### Applications

#### GET /api/applications
Get current user's applications.

**Response:**
```json
[
  {
    "id": 1,
    "userId": 1,
    "userName": "John Doe",
    "welfareProgramId": 1,
    "programName": "Medical Assistance",
    "status": 2,
    "statusName": "Submitted",
    "requestedAmount": 2500.00,
    "reason": "Medical treatment",
    "isEligible": true,
    "submittedAt": "2024-01-15T10:00:00Z"
  }
]
```

#### GET /api/applications/all
Get all applications (HR and above).

#### GET /api/applications/{id}
Get a specific application.

#### POST /api/applications
Create a new application.

**Request Body:**
```json
{
  "welfareProgramId": 1,
  "requestedAmount": 2500.00,
  "reason": "Medical treatment"
}
```

#### PUT /api/applications/{id}
Update an application (draft only).

**Request Body:**
```json
{
  "requestedAmount": 3000.00,
  "reason": "Updated reason"
}
```

#### POST /api/applications/{id}/submit
Submit an application for review.

#### DELETE /api/applications/{id}
Cancel an application (draft only).

#### PUT /api/applications/{id}/status
Update application status (Management and above).

**Request Body:**
```json
{
  "status": 8,
  "comments": "Approved for final payout"
}
```

#### GET /api/applications/{id}/anomalies
Get anomaly flags for an application.

### Reviews

#### GET /api/reviews/{id}
Get a specific review.

#### GET /api/reviews/application/{applicationId}
Get all reviews for an application.

#### POST /api/reviews/application/{applicationId}
Create a review (HR and above).

**Request Body:**
```json
{
  "recommendation": "Approve",
  "comments": "Application meets all criteria"
}
```

#### PUT /api/reviews/{id}
Update a review (HR and above).

**Request Body:**
```json
{
  "recommendation": "Reject",
  "comments": "Insufficient documentation"
}
```

### Notifications

#### GET /api/notifications
Get all user notifications.

**Response:**
```json
{
  "notifications": [
    {
      "id": 1,
      "userId": 1,
      "title": "Application Approved",
      "message": "Your application has been approved",
      "type": "Success",
      "isRead": false,
      "createdAt": "2024-01-15T10:00:00Z"
    }
  ],
  "totalCount": 10,
  "unreadCount": 3
}
```

#### GET /api/notifications/unread
Get unread notifications.

#### GET /api/notifications/unread-count
Get unread notification count.

**Response:**
```json
{
  "count": 3
}
```

#### PUT /api/notifications/{id}/read
Mark notification as read.

#### PUT /api/notifications/read-all
Mark all notifications as read.

### Dashboards

#### GET /api/dashboard/employee
Get employee dashboard (Employee only).

#### GET /api/dashboard/hr
Get HR dashboard (HR only).

#### GET /api/dashboard/committee
Get committee dashboard (Committee only).

#### GET /api/dashboard/management
Get management dashboard (Management only).

#### GET /api/dashboard/admin
Get admin dashboard (Admin only).

### Reports

#### GET /api/reports/applications-by-status
Get applications by status report (HR and above).

**Query Parameters:**
- `startDate` (optional): Filter from date
- `endDate` (optional): Filter to date

#### GET /api/reports/applications-by-department
Get applications by department report.

#### GET /api/reports/applications-by-program
Get applications by program report.

#### GET /api/reports/expenditure
Get expenditure report.

#### GET /api/reports/processing-time
Get processing time report.

#### GET /api/reports/flagged-applications
Get flagged applications report.

### Eligibility

#### POST /api/eligibility/check
Check eligibility before submitting application.

**Request Body:**
```json
{
  "welfareProgramId": 1,
  "requestedAmount": 2500.00
}
```

**Response:**
```json
{
  "isEligible": true,
  "ruleResults": [
    {
      "isPassed": true,
      "ruleName": "Service Period",
      "message": "Service period meets requirement",
      "details": {}
    }
  ],
  "warningMessages": []
}
```

## Status Codes

- `200 OK` - Request successful
- `201 Created` - Resource created successfully
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required or invalid
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## Error Response Format

```json
{
  "message": "Error description"
}
```

## Rate Limiting

Currently no rate limiting is implemented. Consider adding for production.

## CORS

CORS is configured to allow requests from `http://localhost:3000` in development.
