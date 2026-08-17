# Banking Platform - Complaint Management Full Stack

This repository now contains the working base for a dynamic complaint/workflow platform:

- **Frontend:** React + JavaScript + Vite + React Flow (`@xyflow/react`)
- **Backend:** ASP.NET Core / .NET 10 Web API
- **Database:** SQL Server + Entity Framework Core 10 migrations
- **Workflow runtime:** SQL-backed dynamic workflow runtime behind `IWorkflowRuntime`
- **Elsa:** separate Elsa host included so the runtime can be adapted to Elsa without changing React or complaint controllers
- **Theme:** red/white CRM-style UI

The central rule is that workflows are **data**, not hard-coded C# classes. Department admins build nodes and transitions in the visual designer. Complaints bind to a published workflow version and keep that version for their lifetime.

## Folder structure

```text
banking-platform-fullstack/
├─ frontend/                         React/Vite UI
│  ├─ src/
│  │  ├─ api/                        API client + dev identity header
│  │  ├─ auth/                       Local development user switching
│  │  ├─ components/                 CRM layout/shared UI
│  │  ├─ pages/                      Dashboard, complaints, queue, designer
│  │  ├─ workflow/                   React Flow custom node
│  │  └─ styles/                     Red/white CRM theme
│  ├─ package.json
│  └─ vite.config.js                 /api -> http://localhost:5080
├─ src/
│  ├─ BankingPlatform.Domain/
│  ├─ BankingPlatform.Application/
│  ├─ BankingPlatform.Infrastructure/
│  ├─ BankingPlatform.Api/
│  └─ BankingPlatform.WorkflowHost/
├─ database/                         Reference SQL scripts
├─ scripts/                          Windows helper scripts
└─ setup.ps1
```

## Features implemented

### Complaint Management
- Log complaint with category, subject, description, customer reference and priority.
- ATM, Alfa Mall and Transaction categories are seeded.
- Creating a complaint automatically starts the currently published workflow for its category.
- Complaint detail page shows a full audit timeline.
- Complaint register supports search and filtering.

### Role dashboards / work queue
- Unit Head, Team Lead and Officer each see tasks in their own queue.
- Tasks may be role-queue tasks or assigned to a specific user.
- Team Lead can assign the next Officer when the target workflow node uses `assignmentMode = specific`.
- SLA deadline and overdue state are shown in the frontend.
- Team Lead / Unit Head can reassign an open task.

### Dynamic workflow designer
- Drag-and-drop node canvas using React Flow.
- Human task, Start and End nodes.
- Role, SLA hours, escalation role and assignment mode are editable per human task.
- Connect nodes with transitions.
- Transition label and outcome key are editable.
- Draft / Published / Archived version model.
- Published versions are immutable; create a new version to change them.
- Backend validates graph rules before saving/publishing.

### Seeded local users
- Unit Head + DeptAdmin: `11111111-1111-1111-1111-111111111111`
- Team Lead: `22222222-2222-2222-2222-222222222222`
- Officer One: `33333333-3333-3333-3333-333333333333`
- Officer Two: `44444444-4444-4444-4444-444444444444`

The top-right dropdown switches these users in development. The React API client sends `X-User-Id`. Replace this with bank SSO / Entra ID before production.

# Windows setup from zero

## 1. Install prerequisites

Install:
- .NET 10 SDK
- Node.js 22 LTS recommended
- SQL Server Developer/Express/LocalDB
- SSMS is optional but useful

Verify:

```powershell
dotnet --version
node --version
npm --version
```

## 2. Extract and open the project

```powershell
cd C:\Projects\banking-platform-fullstack
```

Optional helper setup:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\setup.ps1
```

Or manually:

```powershell
dotnet new sln -n BankingPlatform
dotnet sln BankingPlatform.sln add .\src\BankingPlatform.Domain\BankingPlatform.Domain.csproj
dotnet sln BankingPlatform.sln add .\src\BankingPlatform.Application\BankingPlatform.Application.csproj
dotnet sln BankingPlatform.sln add .\src\BankingPlatform.Infrastructure\BankingPlatform.Infrastructure.csproj
dotnet sln BankingPlatform.sln add .\src\BankingPlatform.Api\BankingPlatform.Api.csproj
dotnet sln BankingPlatform.sln add .\src\BankingPlatform.WorkflowHost\BankingPlatform.WorkflowHost.csproj
dotnet restore .\BankingPlatform.sln

cd .\frontend
npm install
cd ..
```

## 3. Configure SQL Server

Edit:

```text
src\BankingPlatform.Api\appsettings.json
```

Default SQL Server instance example:

```json
"BankingDatabase": "Server=localhost;Database=BankingPlatform;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

SQL Express example:

```json
"BankingDatabase": "Server=.\\SQLEXPRESS;Database=BankingPlatform;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

LocalDB example:

```json
"BankingDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=BankingPlatform;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

## 4. Install the EF Core CLI

Run once on your machine:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.*
```

If it already exists:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.*
```

Verify:

```powershell
dotnet ef --version
```

## 5. Create the FIRST migration

The ZIP intentionally contains the model but not generated migration files, because the generated migration should match the SDK/EF tooling on the development machine.

From repository root, run **once**:

```powershell
dotnet ef migrations add InitialBusinessSchema `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api `
  --output-dir Persistence\Migrations
```

This generates:

```text
src\BankingPlatform.Infrastructure\Persistence\Migrations\
```

You can verify it:

```powershell
dotnet ef migrations list `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

## 6. Create/update the database

```powershell
dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

The database will be created if it does not exist. When the API starts in Development it also calls `Database.MigrateAsync()` and then seeds departments, categories, users and demo workflows.

Do not separately run `database/001_business_schema.sql` if you are using EF migrations against the same database; that file is a reference/manual schema alternative.

## 7. Run the backend API

Terminal 1, repository root:

```powershell
dotnet run --project .\src\BankingPlatform.Api --urls http://localhost:5080
```

The frontend is configured to proxy `/api` calls to this URL.

## 8. Run the React frontend

Terminal 2:

```powershell
cd .\frontend
npm run dev
```

Open:

```text
http://localhost:5173
```

## 9. Optional Elsa host

The current complaint UI/API runs through the durable SQL workflow runtime behind `IWorkflowRuntime`. The Elsa host is already separated so it can replace that runtime adapter later without changing the frontend contract.

To start the Elsa host separately:

```powershell
dotnet run --project .\src\BankingPlatform.WorkflowHost
```

## Future database changes

Whenever you change an entity/model:

```powershell
dotnet ef migrations add DescribeYourChange `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api `
  --output-dir Persistence\Migrations

dotnet ef database update `
  --project .\src\BankingPlatform.Infrastructure `
  --startup-project .\src\BankingPlatform.Api
```

Examples:

```powershell
dotnet ef migrations add AddComplaintAttachments --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api --output-dir Persistence\Migrations
dotnet ef database update --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api
```

## Test the complete demo flow

1. Start API and frontend.
2. In the top-right development user dropdown choose **Unit Head / Dept Admin**.
3. Open **Workflow Designer** and inspect ATM / Alfa Mall / Transaction workflows.
4. Go to **Complaints -> Log complaint** and choose ATM Issue.
5. The ATM complaint enters the Unit Head queue.
6. Open **My Work Queue** as Unit Head and complete `Forward to team lead`.
7. Switch to **Team Lead**.
8. Open the task and choose `Assign officer`; the UI requires Officer One or Officer Two because the target node is configured for specific-user assignment.
9. Switch to that Officer.
10. The Officer task shows its SLA countdown (ATM uses 48 hours).
11. Choose `Resolve complaint`.
12. Open the complaint to see the complete workflow timeline.

## API endpoints used by React

```text
GET    /api/me
GET    /api/reference/departments
GET    /api/reference/categories
GET    /api/reference/users
GET    /api/complaints
POST   /api/complaints
GET    /api/complaints/{id}
GET    /api/workflow-tasks/my-bucket
GET    /api/workflow-tasks/{taskId}/actions
POST   /api/workflow-tasks/{taskId}/complete
POST   /api/workflow-tasks/{taskId}/reassign
GET    /api/workflow-definitions
GET    /api/workflow-definitions/{id}
POST   /api/workflow-definitions
PUT    /api/workflow-definitions/{id}
POST   /api/workflow-definitions/{id}/publish
POST   /api/workflow-definitions/{id}/new-version
```
#   A l f a l a h - C o m p l a i n M a n a g e m e n t  
 