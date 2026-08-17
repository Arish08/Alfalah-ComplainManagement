# Banking Platform — Tomorrow Setup Guide

Use this guide after cloning the project onto the office laptop.

> **Current stack:** React + Vite, ASP.NET Core / .NET 10, Entity Framework Core, SQL Server, Elsa Workflow Host.

---

# 1. Clone the project

Open **PowerShell**:

```powershell
cd C:\
mkdir Projects -ErrorAction SilentlyContinue
cd C:\Projects

git clone YOUR_GITHUB_REPO_URL
cd banking-platform-fullstack
```

If the repository was already cloned:

```powershell
cd C:\Projects\banking-platform-fullstack
git pull
```

---

# 2. Check required software

## .NET

```powershell
dotnet --list-sdks
```

The project currently targets **.NET 10**.

If you only have .NET 8, install .NET 10 if permitted:

```powershell
winget install Microsoft.DotNet.SDK.10
```

Close and reopen PowerShell, then check again:

```powershell
dotnet --list-sdks
```

It is fine to have both:

```text
8.0.xxx
10.0.xxx
```

## Node / npm

```powershell
node --version
npm --version
```

If Node is missing, install the approved Node.js version for the office machine. Node 22 is a good choice for this project.

## Git

```powershell
git --version
```

---

# 3. Restore and build the backend

From:

```text
C:\Projects\banking-platform-fullstack
```

run:

```powershell
dotnet restore
dotnet build
```

You want:

```text
Build succeeded.
```

`NU1903` messages are package vulnerability warnings. They should be reviewed/upgraded, but a warning by itself is different from a compiler error.

If the build fails, use:

```powershell
dotnet build 2>&1 | Select-String -Pattern "error CS|error NETSDK|error MSB|error NU"
```

---

# 4. Check Entity Framework CLI

```powershell
dotnet ef --version
```

If it is not installed:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.3
```

If an older version is installed:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.3
```

Check again:

```powershell
dotnet ef --version
```

---

# 5. Get the office SQL Server details

You need:

```text
SQL Server / instance name
SQL username
SQL password
```

Because the office uses **SQL Server Authentication**, do not use:

```text
Trusted_Connection=True
```

Connect through SSMS and run:

```sql
SELECT
    @@SERVERNAME AS ServerName,
    SERVERPROPERTY('MachineName') AS MachineName,
    SERVERPROPERTY('InstanceName') AS InstanceName;
```

Keep the exact server/instance name.

---

# 6. Configure the API SQL connection

## Recommended: .NET User Secrets

Do not save the real SQL password in GitHub.

From the repository root:

```powershell
dotnet user-secrets init --project .\src\BankingPlatform.Api
```

Then:

```powershell
dotnet user-secrets set `
  "ConnectionStrings:BankingDatabase" `
  "Server=YOUR_SQL_SERVER;Database=BankingPlatform;User Id=YOUR_SQL_USERNAME;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" `
  --project .\src\BankingPlatform.Api
```

Check that it was saved:

```powershell
dotnet user-secrets list --project .\src\BankingPlatform.Api
```

### Example

If SSMS server is:

```text
OFFICE-SQL\DEV
```

the command is:

```powershell
dotnet user-secrets set `
  "ConnectionStrings:BankingDatabase" `
  "Server=OFFICE-SQL\DEV;Database=BankingPlatform;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" `
  --project .\src\BankingPlatform.Api
```

---

# 7. Create/update the BankingPlatform database

The EF migration files should already be in GitHub.

## DO NOT create InitialBusinessSchema again

Do **not** run:

```text
dotnet ef migrations add InitialBusinessSchema
```

Instead run the existing migrations:

```powershell
dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

Expected:

```text
Build started...
Build succeeded.
...
Done.
```

Then refresh **Databases** in SSMS.

You should see:

```text
BankingPlatform
```

Verify the migration:

```sql
USE BankingPlatform;

SELECT *
FROM dbo.__EFMigrationsHistory;
```

You can also check the main tables:

```sql
USE BankingPlatform;

SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

---

# 8. If SQL says you cannot create the database

This is likely an office SQL permission issue.

Ask the DBA to create:

```text
BankingPlatform
```

and grant your SQL login sufficient permissions to run the application's migrations.

If Elsa will use its own SQL database, also request:

```text
BankingPlatformElsa
```

After the DBA creates `BankingPlatform`, rerun:

```powershell
dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

---

# 9. Run the main backend

Open **Terminal 1**.

```powershell
cd C:\Projects\banking-platform-fullstack
```

Set development mode:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
```

Because the SQL connection is stored in User Secrets, you do not need to type the password again.

Run:

```powershell
dotnet run `
  --project .\src\BankingPlatform.Api\BankingPlatform.Api.csproj `
  --urls http://localhost:5080
```

You want:

```text
Now listening on: http://localhost:5080
```

**Leave Terminal 1 running.**

---

# 10. Configure Elsa SQL connection

Open **Terminal 2**.

```powershell
cd C:\Projects\banking-platform-fullstack
```

Initialize WorkflowHost secrets:

```powershell
dotnet user-secrets init --project .\src\BankingPlatform.WorkflowHost
```

Set the Elsa database connection:

```powershell
dotnet user-secrets set `
  "ConnectionStrings:BankingDatabase" `
  "Server=YOUR_SQL_SERVER;Database=BankingPlatformElsa;User Id=YOUR_SQL_USERNAME;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" `
  --project .\src\BankingPlatform.WorkflowHost
```

Check:

```powershell
dotnet user-secrets list --project .\src\BankingPlatform.WorkflowHost
```

---

# 11. Run Elsa WorkflowHost

Still in Terminal 2:

```powershell
dotnet run `
  --project .\src\BankingPlatform.WorkflowHost\BankingPlatform.WorkflowHost.csproj `
  --urls http://localhost:5090
```

You want:

```text
Now listening on: http://localhost:5090
```

Test:

```text
http://localhost:5090/health
```

**Leave Terminal 2 running.**

> The current complaint workflow execution is still handled by `SqlWorkflowRuntime`. Elsa is the separate workflow-engine foundation for the next stage of the project.

---

# 12. Install frontend dependencies

Open **Terminal 3**:

```powershell
cd C:\Projects\banking-platform-fullstack\frontend
```

After a fresh clone:

```powershell
npm install
```

`node_modules` is intentionally not stored in GitHub. `npm install` recreates it from `package.json` / `package-lock.json`.

---

# 13. Run React

Still in Terminal 3:

```powershell
npm run dev
```

You should see something similar to:

```text
Local: http://localhost:5173/
```

Open:

```text
http://localhost:5173
```

---

# 14. What should now be running?

```text
React Frontend
http://localhost:5173
        │
        ▼
BankingPlatform.Api
http://localhost:5080
        │
        ▼
SQL Server
BankingPlatform


Elsa WorkflowHost
http://localhost:5090
        │
        ▼
SQL Server
BankingPlatformElsa
```

---

# 15. Normal daily startup after first setup

After everything has been installed/configured once, you do **not** need to run migrations, `npm install`, or User Secrets every morning.

## Terminal 1 — API

```powershell
cd C:\Projects\banking-platform-fullstack

$env:ASPNETCORE_ENVIRONMENT="Development"

dotnet run `
  --project .\src\BankingPlatform.Api\BankingPlatform.Api.csproj `
  --urls http://localhost:5080
```

## Terminal 2 — Elsa

```powershell
cd C:\Projects\banking-platform-fullstack

dotnet run `
  --project .\src\BankingPlatform.WorkflowHost\BankingPlatform.WorkflowHost.csproj `
  --urls http://localhost:5090
```

## Terminal 3 — React

```powershell
cd C:\Projects\banking-platform-fullstack\frontend

npm run dev
```

Then open:

```text
http://localhost:5173
```

---

# 16. Troubleshooting — problems encountered during initial setup

## A. EF says: "Build failed. Use dotnet build to see the errors."

Run:

```powershell
dotnet build
```

Or API only:

```powershell
dotnet build .\src\BankingPlatform.Api\BankingPlatform.Api.csproj
```

Infrastructure only:

```powershell
dotnet build .\src\BankingPlatform.Infrastructure\BankingPlatform.Infrastructure.csproj
```

Elsa only:

```powershell
dotnet build .\src\BankingPlatform.WorkflowHost\BankingPlatform.WorkflowHost.csproj
```

Filter errors:

```powershell
dotnet build 2>&1 | Select-String -Pattern "error CS|error NETSDK|error MSB|error NU"
```

---

## B. EF says BankingPlatform.Api does not reference Microsoft.EntityFrameworkCore.Design

Do **not** immediately start changing packages if the repository contains today's fixed `.csproj` files.

First run:

```powershell
git status
dotnet restore
dotnet build
```

Then:

```powershell
dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

The fixed package references should already come from GitHub.

---

## C. SQL Server: error 40 / server not found

Check the exact SQL Server instance in SSMS:

```sql
SELECT @@SERVERNAME;
```

Confirm:

- server name
- instance name
- SQL username
- password
- database permissions

Then update User Secrets:

```powershell
dotnet user-secrets set `
  "ConnectionStrings:BankingDatabase" `
  "Server=EXACT_SERVER_NAME;Database=BankingPlatform;User Id=USERNAME;Password=PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" `
  --project .\src\BankingPlatform.Api
```

Retry:

```powershell
dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

---

## D. EF reports SQL Server multiple cascade paths

The fix for this was already made in the project model/migration configuration.

Do **not** recreate the old migration.

After cloning today's final code, use:

```powershell
dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

---

## E. Elsa reports an EF Core package downgrade

Today's corrected package versions should be committed in:

```text
src/BankingPlatform.WorkflowHost/BankingPlatform.WorkflowHost.csproj
```

First:

```powershell
dotnet restore
dotnet build .\src\BankingPlatform.WorkflowHost\BankingPlatform.WorkflowHost.csproj
```

Do not downgrade packages back to the earlier versions.

---

## F. Check the database tables

In SSMS:

```sql
USE BankingPlatform;

SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

---

## G. Check development users

```sql
USE BankingPlatform;

SELECT *
FROM dbo.Users;
```

---

## H. Check Officer users/memberships

```sql
USE BankingPlatform;

SELECT
    u.Id,
    u.EmployeeCode,
    u.DisplayName,
    u.IsActive,
    dm.DepartmentId,
    dm.RoleCode
FROM dbo.DepartmentMemberships dm
INNER JOIN dbo.Users u
    ON u.Id = dm.UserId
WHERE dm.RoleCode = 'Officer';
```

You should have Officer users available for the appropriate department.

---

# 17. After pulling future changes

```powershell
cd C:\Projects\banking-platform-fullstack

git pull
dotnet restore
dotnet build
```

If new EF migrations were added:

```powershell
dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

If frontend packages changed:

```powershell
cd frontend
npm install
```

Then start the three applications normally.

---

# 18. If you change the EF model in the future

Example: a new `ComplaintAttachments` entity.

Create a NEW migration:

```powershell
dotnet ef migrations add AddComplaintAttachments `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api `
  --output-dir Persistence\Migrations
```

Apply it:

```powershell
dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

Then commit the migration files:

```powershell
git add .
git commit -m "Add complaint attachments"
git push
```

Do not repeatedly recreate `InitialBusinessSchema`.

---

# 19. Quick copy/paste checklist

Fresh clone:

```powershell
cd C:\Projects
git clone YOUR_GITHUB_REPO_URL
cd banking-platform-fullstack

dotnet --list-sdks
node --version
npm --version

dotnet restore
dotnet build

dotnet ef --version
```

Configure API SQL credentials:

```powershell
dotnet user-secrets init --project .\src\BankingPlatform.Api

dotnet user-secrets set `
  "ConnectionStrings:BankingDatabase" `
  "Server=YOUR_SQL_SERVER;Database=BankingPlatform;User Id=YOUR_SQL_USERNAME;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" `
  --project .\src\BankingPlatform.Api
```

Apply database:

```powershell
dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

Run API:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"

dotnet run `
  --project .\src\BankingPlatform.Api\BankingPlatform.Api.csproj `
  --urls http://localhost:5080
```

New terminal — React:

```powershell
cd C:\Projects\banking-platform-fullstack\frontend
npm install
npm run dev
```

Open:

```text
http://localhost:5173
```

New terminal — Elsa, if needed:

```powershell
cd C:\Projects\banking-platform-fullstack

dotnet user-secrets init --project .\src\BankingPlatform.WorkflowHost

dotnet user-secrets set `
  "ConnectionStrings:BankingDatabase" `
  "Server=YOUR_SQL_SERVER;Database=BankingPlatformElsa;User Id=YOUR_SQL_USERNAME;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" `
  --project .\src\BankingPlatform.WorkflowHost

dotnet run `
  --project .\src\BankingPlatform.WorkflowHost\BankingPlatform.WorkflowHost.csproj `
  --urls http://localhost:5090
```

---

# 20. Important security rules

Never push:

- SQL passwords
- SQL usernames if considered sensitive internally
- SMTP passwords
- API keys
- JWT signing secrets
- certificates/private keys
- bank credentials
- `.env` files containing secrets

The project does **not currently require a `.env` file** for the setup described here.

Use:

- `.NET User Secrets` for development credentials
- environment variables where appropriate
- the bank's approved secrets manager for deployed environments

The development `X-User-Id` authentication/user switcher is for local development only and must be replaced with the bank's real authentication/authorization solution before production.
