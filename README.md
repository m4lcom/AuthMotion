# AuthMotion 🛡️
### Enterprise-Grade Identity & Access Management (IAM) System

**`AuthMotion`** is a high-performance, secure, and scalable authentication engine built with **`.NET 10`**. It implements **`Clean Architecture`** to provide a robust identity solution, ranging from OAuth2 integrations to advanced multi-factor security.

---

## 💎 Features
The system manages the entire user lifecycle with a focus on modern security standards:

- **Core Authentication:**
    - **`Register & Login:`** Secure flows with **BCrypt** password hashing.
      
    - **`Google OAuth2:`** Native integration for social authentication.
    
    - **`Logout:`** Complete session and cookie invalidation.

- **Token Management:**
    - **`Dual Token Strategy:`** Access Tokens (JWT) + Refresh Tokens.
    
    - **`Security-First Delivery:`** All tokens are delivered via **HttpOnly, Secure, and SameSite** cookies to eliminate XSS and mitigate CSRF risks.

- **Advanced Security:**
    - **`Two-Factor Authentication (2FA):`** TOTP support (Authenticator Apps) with QR Code generation.
    
    - **`Role-Based Access Control (RBAC):`** Granular permission management across endpoints.

- **Account Recovery & Lifecycle:**
    - **`Email Verification:`** Built-in flow for account activation.
    
    - **`Forgot/Reset Password:`** Secure token-based password recovery.

---

## 🏗️ Architecture & Tech Stack

This project follows the **Onion Architecture** pattern, ensuring the business logic remains independent of external frameworks.

- **`Framework:`** .NET 10 (C# 14/15 features)
  
- **`Architecture:`** Clean Architecture (Domain, Application, Infrastructure, API)

- **`Database:`** SQL Server via **Entity Framework Core 10**

- **`Documentation:`** **Scalar** (Interactive API reference)

- **`Environment:`** Full **Docker** orchestration

---

## 🏗️ Solution Structure

```text
AuthMotion/
├── src/
│   ├── AuthMotion.API/            # Entry point, Middleware & Controllers
│   ├── AuthMotion.Application/    # Use Cases, DTOs & Service Interfaces
│   ├── AuthMotion.Infrastructure/ # EF Core, Identity Providers & 2FA Logic
│   └── AuthMotion.Domain/         # Entities & Core Business Rules
├── tests/
│   └── AuthMotion.UnitTests/      # Logic validation with xUnit & Moq
└── docker-compose.yml
```

---

## 🧪 Quality Assurance
We don't guess; we verify. The system includes a comprehensive testing suite focusing on business logic reliability.

- **`Unit Testing:`** xUnit

- **`Mocking:`** Moq

- **`Assertions:`** FluentAssertions

- **`Pattern:`** AAA (Arrange, Act, Assert)

```Bash
# Execute the test suite
dotnet test
```

---

## 🐳 Getting Started with Docker
The entire ecosystem is containerized for immediate deployment.

- Clone the repository:
```Bash
git clone [https://github.com/your-user/AuthMotion.git](https://github.com/your-user/AuthMotion.git)
cd AuthMotion
```

- Spin up the infrastructure:
```Bash
docker-compose up --build
```

- Explore the API with Scalar: </br>
**http://localhost:8080/scalar/v1**

---

## 🔒 Security Best Practices Implemented

> [!NOTE]
> **Zero Client-Side Storage:** Unlike many common implementations, **AuthMotion** does not store JWTs in `localStorage` or `sessionStorage`. By leveraging **`HttpOnly Cookies`**, we ensure that sensitive tokens remain inaccessible to JavaScript. This architectural choice provides a robust defense against **`Cross-Site Scripting (XSS)`** attacks, as tokens cannot be stolen via malicious client-side scripts.

---

## 🛠️ API Reference

| Endpoint | Method | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `/api/auth/register` | `POST` | Registers a new user in the system. | No |
| `/api/auth/login` | `POST` | Authenticates user & issues tokens via HttpOnly cookies. | No |
| `/api/auth/me` | `GET` | Retrieves the current authenticated user's information from claims. | **Yes** |
| `/api/auth/refresh` | `POST` | Generates a new pair of tokens reading the old ones from cookies. | No (Cookie) |
| `/api/auth/login-google` | `GET` | Redirects the user to the Google sign-in page. | No |
| `/api/auth/google-response`| `GET` | Handles Google callback and issues AuthMotion tokens. | No |
| `/api/auth/admin-only` | `GET` | Restricted test endpoint for users with **Admin** role. | **Yes (Admin)** |
| `/api/auth/verify-email` | `POST` | Verifies the user's email using the 6-digit OTP code. | No |
| `/api/auth/setup-2fa` | `POST` | Initiates 2FA setup by generating a QR code URI. | **Yes** |
| `/api/auth/confirm-2fa` | `POST` | Confirms 2FA setup and activates it on the account. | **Yes** |
| `/api/auth/login-2fa` | `POST` | Validates 2FA code during login and issues tokens. | No |
| `/api/auth/forgot-password`| `POST` | Sends a recovery email with a 6-digit token (Rate Limited). | No |
| `/api/auth/reset-password` | `POST` | Validates the token and resets the user's password. | No |
| `/api/auth/logout` | `POST` | Clears authentication cookies to log the user out. | No |
