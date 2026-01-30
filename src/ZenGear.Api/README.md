# ZenGear API

REST API for ZenGear e-commerce platform built with .NET 10, PostgreSQL, and Clean Architecture.

---

## 🚀 Quick Start

### Prerequisites

- .NET 10 SDK
- PostgreSQL 17+
- SMTP server (or use MailHog for development)

### Run API

```bash
# From solution root
dotnet run --project src/ZenGear.Api

# Or with watch mode
dotnet watch --project src/ZenGear.Api
```

API runs at: `https://localhost:5001`

---

## 📚 API Documentation

### Interactive Documentation

- **Scalar UI**: `https://localhost:5001/scalar/v1` (Modern, beautiful)
- **Swagger UI**: `https://localhost:5001/swagger` (Classic)
- **OpenAPI Spec**: `https://localhost:5001/openapi/v1.json`

### Versioning

API uses URL-based versioning: `/api/v1/...`

---

## 🔐 Authentication Endpoints

Base URL: `/api/v1/auth`

### 1. Register

**POST** `/register`

```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

Response: `200 OK`
```json
{
  "succeeded": true,
  "errors": [],
  "errorCode": null,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### 2. Verify Email

**POST** `/verify-email`

```json
{
  "email": "user@example.com",
  "otpCode": "123456"
}
```

Response: `200 OK`

---

### 3. Resend Verification Email

**POST** `/resend-verification-email`

```json
{
  "email": "user@example.com"
}
```

Response: `200 OK`

---

### 4. Login

**POST** `/login`

```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

Response: `200 OK`
```json
{
  "succeeded": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "base64-encoded-token",
    "expiresAt": "2024-01-15T11:30:00Z",
    "user": {
      "id": "usr_V1StGXR8Z5jdHi6B",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "fullName": "John Doe",
      "avatarUrl": null,
      "roles": ["Customer"],
      "emailConfirmed": true
    }
  },
  "errors": [],
  "errorCode": null,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### 5. Refresh Token

**POST** `/refresh-token`

```json
{
  "refreshToken": "base64-encoded-token"
}
```

Response: `200 OK`
```json
{
  "succeeded": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "new-base64-encoded-token",
    "expiresAt": "2024-01-15T11:30:00Z"
  },
  "errors": [],
  "errorCode": null,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Note:** Old refresh token is revoked (token rotation for security).

---

### 6. Logout

**POST** `/logout`

```json
{
  "refreshToken": "base64-encoded-token"
}
```

Response: `200 OK`

---

### 7. Change Password (Authenticated)

**POST** `/change-password`

**Headers:**
```
Authorization: Bearer <access-token>
```

**Body:**
```json
{
  "currentPassword": "OldPass123!",
  "newPassword": "NewSecurePass456!"
}
```

Response: `200 OK`

**Note:** All tokens are invalidated. User must login again.

---

### 8. Forgot Password

**POST** `/forgot-password`

```json
{
  "email": "user@example.com"
}
```

Response: `200 OK` (always, to prevent email enumeration)

---

### 9. Reset Password

**POST** `/reset-password`

```json
{
  "email": "user@example.com",
  "otpCode": "123456",
  "newPassword": "NewSecurePass456!"
}
```

Response: `200 OK`

**Note:** All tokens are invalidated.

---

### 10. Get Current User (Authenticated)

**GET** `/me`

**Headers:**
```
Authorization: Bearer <access-token>
```

Response: `200 OK`
```json
{
  "succeeded": true,
  "data": {
    "id": "usr_V1StGXR8Z5jdHi6B",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "fullName": "John Doe",
    "avatarUrl": null,
    "roles": ["Customer"],
    "emailConfirmed": true
  },
  "errors": [],
  "errorCode": null,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## 🔒 Security Features

### Password Requirements

- ✅ Minimum 8 characters
- ✅ At least 1 uppercase letter
- ✅ At least 1 lowercase letter
- ✅ At least 1 digit
- ✅ At least 1 special character

### Email Verification

- ✅ Required before first login
- ✅ 6-digit OTP sent to email
- ✅ OTP expires after 10 minutes
- ✅ Rate limited (5 requests per 15 minutes)

### Lockout Policy

- ✅ 5 failed login attempts → account locked
- ✅ Lockout duration: 15 minutes
- ✅ Counter resets on successful login

### Token Security

- ✅ JWT access tokens (60 min expiry)
- ✅ Refresh tokens (7 days expiry)
- ✅ Token rotation on refresh
- ✅ Revoke all tokens on password change

---

## ⚙️ Configuration

### Required Settings (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=zengear_dev;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-must-be-at-least-32-characters",
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "EmailSettings": {
    "FromEmail": "noreply@zengear.com",
    "FromName": "ZenGear",
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "EnableSsl": true
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000"
    ]
  }
}
```

---

## 🧪 Development Tools

### MailHog (Email Testing)

For development, use MailHog to capture emails locally:

```bash
# Run MailHog with Docker
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog

# Update appsettings.Development.json
{
  "EmailSettings": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "EnableSsl": false
  }
}
```

View emails at: `http://localhost:8025`

---

## 🔍 Error Codes

Common error codes returned by authentication endpoints:

| Code | Description |
|------|-------------|
| `VALIDATION_ERROR` | Invalid request data |
| `USER_NOT_FOUND` | User does not exist |
| `USER_INVALID_CREDENTIALS` | Wrong email or password |
| `USER_EMAIL_NOT_VERIFIED` | Email verification required |
| `USER_EMAIL_ALREADY_VERIFIED` | Email already confirmed |
| `USER_ACCOUNT_LOCKED` | Too many failed login attempts |
| `USER_INVALID_OTP_CODE` | Wrong or expired OTP |
| `USER_OTP_RATE_LIMIT_EXCEEDED` | Too many OTP requests |
| `USER_INVALID_REFRESH_TOKEN` | Invalid refresh token |
| `USER_REFRESH_TOKEN_EXPIRED` | Refresh token expired or revoked |
| `USER_REGISTRATION_FAILED` | Registration failed (e.g., weak password) |
| `USER_PASSWORD_CHANGE_FAILED` | Password change failed |
| `USER_PASSWORD_RESET_FAILED` | Password reset failed |
| `UNAUTHORIZED` | Not authenticated |
| `FORBIDDEN` | Not authorized |

---

## 📝 Example Flow

### Complete Registration & Login Flow

```bash
# 1. Register
curl -X POST https://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe"
  }'

# 2. Check email for OTP (or use MailHog at http://localhost:8025)

# 3. Verify Email
curl -X POST https://localhost:5001/api/v1/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "otpCode": "123456"
  }'

# 4. Login
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!"
  }'

# 5. Use access token
curl -X GET https://localhost:5001/api/v1/auth/me \
  -H "Authorization: Bearer <access-token>"

# 6. Refresh token when access token expires
curl -X POST https://localhost:5001/api/v1/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "<refresh-token>"
  }'
```

---

## 🛠️ Database Migrations

```bash
# Add migration
cd src/ZenGear.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../ZenGear.Api

# Apply migration
dotnet ef database update --startup-project ../ZenGear.Api

# View SQL script
dotnet ef migrations script --startup-project ../ZenGear.Api
```

---

## 🏗️ Architecture

```
API Layer (Controllers)
    ↓ ISender (MediatR)
Application Layer (Commands/Queries/Handlers)
    ↓ Interfaces (IIdentityService, ITokenService, etc.)
Infrastructure Layer (Services, Repositories)
    ↓ EF Core
Database (PostgreSQL)
```

**Key Principles:**
- ✅ Clean Architecture - dependencies flow inward
- ✅ CQRS - Commands mutate, Queries read
- ✅ Hybrid ID Strategy - `long` internally, `string` NanoId externally
- ✅ Repository Pattern - abstraction over data access
- ✅ Domain Events - side effects decoupled
- ✅ Result Pattern - explicit error handling

---

## 📖 Related Documentation

- [PROJECT_BRIEF.md](../../docs/PROJECT_BRIEF.md)
- [ARCHITECTURE.md](../../docs/ARCHITECTURE.md)
- [ID_CONVENTION.md](../../docs/ID_CONVENTION.md)
- [F002_UserAuthentication.md](../../docs/features/F002_UserAuthentication.md)

---

## 🎯 Next Steps

1. ✅ Authentication API complete
2. ⬜ Seed initial data (roles, test users)
3. ⬜ Product Management API
4. ⬜ Shopping Cart API
5. ⬜ Order Management API
6. ⬜ Payment Integration

---

**Version:** 1.0
**Last Updated:** 2024-01-29
