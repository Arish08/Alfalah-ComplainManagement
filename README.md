# Complaint Management System --- Office PC (.NET 8)

This is the command reference for setting up and running the Complaint
Management System on the office PC.

The office PC uses **.NET 8**. Keep the project on .NET 8 if the project
files contain:

``` xml
<TargetFramework>net8.0</TargetFramework>
```

## 1. First-time checks

Open PowerShell in the repository root and check the installed versions:

``` powershell
dotnet --version
node --version
npm --version
```

The .NET command should show an installed .NET 8 SDK, for example
`8.0.xxx`.

## 2. Entity Framework CLI

Use the EF Core 8 CLI with the .NET 8 project:

``` powershell
dotnet tool update --global dotnet-ef --version 8.*
```

Check it:

``` powershell
dotnet ef --version
```

## 3. Restore backend packages

From the repository root:

``` powershell
dotnet restore src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

This restores the NuGet packages already declared by the project.

## 4. Build the backend

``` powershell
dotnet build src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

Fix any build error before starting the application.

## 5. Apply existing database migrations

``` powershell
dotnet ef database update --project src/BankingPlatform.Infrastructure --startup-project src/BankingPlatform.Api --context AppDbContext
```

Do **not** create a new migration just to run the project.

Only use `dotnet ef migrations add ...` when the EF database model has
actually changed and a new migration needs to be created.

## 6. Email notification package

The workflow email service uses MailKit.

If MailKit has already been added to the Infrastructure project and
committed to source control, you do **not** need to add it again.
`dotnet restore` will restore it.

If it has not been added yet, run this once:

``` powershell
dotnet add src/BankingPlatform.Infrastructure package MailKit
```

Then:

``` powershell
dotnet restore src/BankingPlatform.Api/BankingPlatform.Api.csproj
dotnet build src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

## 7. Configure email credentials on the office PC

Do not put the real SMTP password in source control.

The non-secret configuration can remain in `appsettings.json`:

``` json
"Email": {
  "Host": "YOUR_SMTP_HOST",
  "Port": 587,
  "Username": "complaints@yourcompany.com",
  "Password": "",
  "FromEmail": "complaints@yourcompany.com",
  "FromName": "Complaint Management",
  "EnableSsl": true
}
```

Replace the example host, username, and sender address with the real
office email configuration.

Initialize .NET User Secrets on the office PC if the API project has not
already been initialized for them:

``` powershell
dotnet user-secrets init --project src/BankingPlatform.Api
```

Set the real SMTP password locally:

``` powershell
dotnet user-secrets set "Email:Password" "YOUR_REAL_SMTP_PASSWORD" --project src/BankingPlatform.Api
```

Check the configured secrets:

``` powershell
dotnet user-secrets list --project src/BankingPlatform.Api
```

The password stays on the development PC instead of being committed to
the repository.

## 8. Frontend first-time setup

Open a separate PowerShell terminal:

``` powershell
cd frontend
npm install
```

This installs the packages from the frontend package configuration.

## 9. Daily startup

After the machine is configured, these are the commands normally needed
each day.

### Terminal 1 --- API

From the repository root:

``` powershell
dotnet run --project src/BankingPlatform.Api
```

Keep this terminal open.

### Terminal 2 --- Workflow Host

If the current solution uses the separate Workflow Host:

``` powershell
dotnet run --project src/BankingPlatform.WorkflowHost
```

Keep this terminal open.

If the current branch does not use the separate Workflow Host, skip this
step.

### Terminal 3 --- Frontend

``` powershell
cd frontend
npm run dev
```

Keep the terminal open and use the local URL displayed by Vite.

## 10. After pulling new changes at the office

After getting the latest code, run from the repository root:

``` powershell
dotnet restore src/BankingPlatform.Api/BankingPlatform.Api.csproj
dotnet build src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

Apply any new committed migrations:

``` powershell
dotnet ef database update --project src/BankingPlatform.Infrastructure --startup-project src/BankingPlatform.Api --context AppDbContext
```

Update frontend packages:

``` powershell
cd frontend
npm install
```

Then start the application normally.

### API

``` powershell
dotnet run --project src/BankingPlatform.Api
```

### Workflow Host, if used

``` powershell
dotnet run --project src/BankingPlatform.WorkflowHost
```

### Frontend

``` powershell
cd frontend
npm run dev
```

## 11. SQL Server

The development SQL Server instance previously used for this project is:

``` text
ARISH\SQLEXPRESS
```

When it appears inside JSON, the backslash must be escaped:

``` text
Server=ARISH\\SQLEXPRESS;...
```

If the office PC uses a different SQL Server instance, use that
machine's correct connection string instead.

## 12. Useful troubleshooting commands

Check the complete .NET installation:

``` powershell
dotnet --info
```

Clean and rebuild:

``` powershell
dotnet clean
dotnet restore src/BankingPlatform.Api/BankingPlatform.Api.csproj
dotnet build src/BankingPlatform.Api/BankingPlatform.Api.csproj
```

Check EF:

``` powershell
dotnet ef --version
```

List migrations:

``` powershell
dotnet ef migrations list --project src/BankingPlatform.Infrastructure --startup-project src/BankingPlatform.Api --context AppDbContext
```

Check Node/npm:

``` powershell
node --version
npm --version
```

Restore frontend packages:

``` powershell
cd frontend
npm install
```

## Quick daily reference

Normally, once everything is configured, use three terminals:

**Terminal 1**

``` powershell
dotnet run --project src/BankingPlatform.Api
```

**Terminal 2 --- only if Workflow Host is required**

``` powershell
dotnet run --project src/BankingPlatform.WorkflowHost
```

**Terminal 3**

``` powershell
cd frontend
npm run dev
```

## Important

-   This setup is for **.NET 8**.
-   Keep `dotnet-ef` aligned with the EF Core major version used by the
    project.
-   Do not commit SMTP passwords or other secrets.
-   Do not create a new migration every time the application starts.
-   Use `dotnet ef database update` to apply migrations that already
    exist.
-   `npm install` is normally required after cloning or when frontend
    dependencies change.
-   The workflow's email toggle is stored with the workflow step
    configuration.
-   The assigned officer's recipient email should be fetched from the
    database rather than hardcoded.
