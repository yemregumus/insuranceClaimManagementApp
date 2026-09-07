# Insurance Claim Management

An ASP.NET Core Razor Pages application for insurance-company staff to manage
customers and claims. The application supports multiple organizations and keeps
each organization's staff, customers, claims, status history, and audit events
isolated from other organizations.

## Features

- Administrator, claims-adjuster, and read-only staff roles
- ASP.NET Core Identity password hashing, account lockout, and password recovery
- Optional authenticator-app two-factor authentication with QR-code setup
- Organization-scoped staff, customer, claim, and audit data
- Customer and staff account management
- Controlled claim statuses and claim status history
- Claim archiving and concurrent-update protection
- Security-sensitive activity auditing

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL Server 8.x
- Git
- MySQL Workbench or another MySQL client is optional

## Set up the application

### 1. Clone the repository

```powershell
git clone https://github.com/yemregumus/insuranceClaimManagementApp.git
cd insuranceClaimManagementApp
```

### 2. Restore dependencies and tools

```powershell
dotnet restore
dotnet tool restore
```

### 3. Create the MySQL database

Start MySQL and create an empty database named `insuranceclaimdb`. For example,
run this in MySQL Workbench:

```sql
CREATE DATABASE IF NOT EXISTS insuranceclaimdb
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
```

The MySQL account used by the application must have permission to create and
modify tables in this database.

### 4. Configure the connection string

Store the connection string with .NET User Secrets so database credentials are
not written to the repository:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=insuranceclaimdb;User=YOUR_USER;Password=YOUR_PASSWORD"
```

Replace `YOUR_USER` and `YOUR_PASSWORD` with the credentials for your local
MySQL account.

### 5. Create the database tables

```powershell
dotnet ef database update
```

This applies all required Entity Framework Core migrations to the empty database.

### 6. Create the first organization and administrator

Use temporary environment variables so the administrator password is not stored
in a tracked configuration file:

```powershell
$env:InitialAdmin__Email = Read-Host "Admin email"
$securePassword = Read-Host "Admin password" -AsSecureString
$env:InitialAdmin__Password = [System.Net.NetworkCredential]::new("", $securePassword).Password
$env:InitialOrganization__Name = "Your Insurance Company"
$env:InitialOrganization__Slug = "your-company"

dotnet run -- --provision-organization

Remove-Item Env:InitialAdmin__Email
Remove-Item Env:InitialAdmin__Password
Remove-Item Env:InitialOrganization__Name
Remove-Item Env:InitialOrganization__Slug
Remove-Variable securePassword
```

The password must contain at least 12 characters, uppercase and lowercase
letters, a number, and a symbol. The organization slug may contain lowercase
letters, numbers, and single hyphens.

The provisioning command exits after creating the organization and administrator.
Run the application separately afterward.

### 7. Trust the local HTTPS certificate

```powershell
dotnet dev-certs https --trust
```

### 8. Run the application

```powershell
dotnet run
```

Open [https://localhost:5001](https://localhost:5001) and sign in with the
administrator account. The HTTP endpoint at `http://localhost:5000` redirects
to HTTPS.

## Staff account setup

Administrators create staff accounts from the **Staff** page and assign one of
the following roles:

| Role | Access |
| --- | --- |
| Administrator | Manage staff, customers, claims, and audit events |
| ClaimsAdjuster | View and manage claims |
| ReadOnly | View the dashboard and claims |

No email provider is configured yet. After creating a staff account, the
administrator must privately share the generated password setup link. The link
expires after two hours. Staff can enable authenticator-app two-factor
authentication from the **Security** page after signing in.

## Run the tests

Run all automated tests:

```powershell
dotnet test InsuranceClaimManagement.Tests\InsuranceClaimManagement.Tests.csproj
```

Run the tests and enforce the 80% application line-coverage requirement:

```powershell
& .\InsuranceClaimManagement.Tests\Test-WithCoverage.ps1
```

The test project groups tests by account, claim, customer, staff, data,
authorization, and tenant-isolation behavior. Files inside `bin` and `obj` are
generated build artifacts and should not be edited or committed.

## Before public deployment

The included configuration is intended for local development. Before deploying
publicly, configure production secrets, an email provider, persistent data
protection keys, HTTPS/reverse-proxy settings, database backups, monitoring, and
appropriate claim and audit-data retention policies.
