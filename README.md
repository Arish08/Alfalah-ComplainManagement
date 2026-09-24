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





Workflow Task Attachments — Temporary Implementation Without Database Changes

Purpose

This README documents how to add attachments beside the existing
workflow task comment field without adding an attachment table to the
database yet.

The existing WorkflowTasks.CompletionComment, dynamic fields, and
workflow completion logic remain unchanged.

Current Design

For this temporary version:

• No new database table.
• No EF Core migration.
• No change to the database diagram.
• WorkflowTasks.CompletionComment continues to store the comment.
• Multiple attachments can be selected.
• Files are stored outside SQL Server.
• Files are organized by WorkflowTaskId.
• The existing JSON /complete endpoint stays unchanged.
• A separate multipart endpoint uploads files.
• Attachment metadata can be moved into a proper database table later.

Files will be stored like this:

BankingPlatform.Api/
└── uploads/
    └── workflow-tasks/
        └── {WorkflowTaskId}/
            ├── generated-file-name.pdf
            └── generated-file-name.png

1. Frontend — Attachment State

File:

frontend/src/pages/MyTasksPage.jsx

Under the existing comment state:

const [comment, setComment] = useState('')

add:

const [attachments, setAttachments] = useState([])

Whenever a task is opened/reset and the code runs:

setComment('')

also add:

setAttachments([])

2. Frontend — Attachment Picker

Place this directly under the existing Comment field and before the
modal action buttons:

<div className="field">
  <span>Attachments</span>

  <input
    type="file"
    multiple
    onChange={(event) => {
      const files = Array.from(event.target.files || [])
      setAttachments(files)
    }}
  />

  {attachments.length > 0 && (
    <div className="attachment-list">
      {attachments.map((file, index) => (
        <div
          className="attachment-item"
          key={`${file.name}-${index}`}
        >
          <span>📎 {file.name}</span>

          <button
            type="button"
            className="icon-button"
            onClick={() => {
              setAttachments((current) =>
                current.filter((_, i) => i !== index)
              )
            }}
          >
            ×
          </button>
        </div>
      ))}
    </div>
  )}
</div>

The modal will conceptually look like:

Comment
┌─────────────────────────────────────────────┐
│ Add an optional action note...              │
└─────────────────────────────────────────────┘

Attachments
[ Choose Files ]

📎 customer-document.pdf        ×
📎 evidence.png                 ×

                              [ Forward ]

3. Backend — Upload Endpoint

File:

src/BankingPlatform.Api/Controllers/WorkflowTasksController.cs

Add this endpoint inside WorkflowTasksController:

[HttpPost("{taskId:guid}/attachments")]
[RequestSizeLimit(25_000_000)]
public async Task<IActionResult> UploadAttachments(
    Guid taskId,
    [FromForm] List<IFormFile> files,
    CancellationToken cancellationToken)
{
    if (files.Count == 0)
        return BadRequest("No files were selected.");

    var allowedExtensions = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png",
        ".docx",
        ".xlsx"
    };

    var uploadFolder = Path.Combine(
        Directory.GetCurrentDirectory(),
        "uploads",
        "workflow-tasks",
        taskId.ToString());

    Directory.CreateDirectory(uploadFolder);

    foreach (var file in files)
    {
        if (file.Length == 0)
            continue;

        var extension = Path.GetExtension(file.FileName);

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(
                $"File type '{extension}' is not allowed.");
        }

        var storedFileName =
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var filePath = Path.Combine(
            uploadFolder,
            storedFileName);

        await using var stream =
            new FileStream(filePath, FileMode.CreateNew);

        await file.CopyToAsync(
            stream,
            cancellationToken);
    }

    return Ok();
}

If required, add:

using Microsoft.AspNetCore.Http;

The backend generates a unique physical filename instead of trusting the
browser-provided filename. This reduces filename collisions and avoids
using a user-supplied path/name directly on disk.

4. Frontend — Upload Before Completing

Keep the existing JSON completion payload and /complete endpoint
unchanged.

Immediately before the existing completion API call, add:

if (attachments.length > 0) {
  const formData = new FormData()

  attachments.forEach((file) => {
    formData.append('files', file)
  })

  const response = await fetch(
    `/api/workflow-tasks/${selected.id}/attachments`,
    {
      method: 'POST',
      body: formData,
    }
  )

  if (!response.ok) {
    const message = await response.text()
    throw new Error(
      message || 'Failed to upload attachments.'
    )
  }
}

Then leave the existing completion request unchanged:

await api(
  `/workflow-tasks/${selected.id}/complete`,
  {
    method: 'POST',
    body: payload,
  }
)

Important

Do not manually set:

'Content-Type': 'multipart/form-data'

The browser automatically creates the correct multipart boundary when
using FormData.

5. Reset Attachments After Completion

Where successful task completion currently resets values:

setComment('')
setTaskForm(null)
setFieldValues({})

also add:

setAttachments([])

6. CSS

Add to the stylesheet used by the task modal:

.attachment-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 10px;
}

.attachment-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 12px;
  border: 1px solid #d8dee8;
  border-radius: 8px;
  background: #f8fafc;
}

.attachment-item span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

7. Runtime Flow

1. Team Lead/Officer opens a workflow task.
2. User enters a comment.
3. User selects one or more files.
4. Frontend uploads them to:

POST /api/workflow-tasks/{taskId}/attachments

5. Backend saves them under:

uploads/workflow-tasks/{taskId}/

6. If upload succeeds, the frontend calls the existing:

POST /api/workflow-tasks/{taskId}/complete

7. The comment continues to save in:

WorkflowTasks.CompletionComment

8. No attachment information is written to SQL Server in this temporary
version.

8. Example

For task:

19b47b0e-1111-2222-3333-444444444444

the filesystem can contain:

BankingPlatform.Api/
└── uploads/
    └── workflow-tasks/
        └── 19b47b0e-1111-2222-3333-444444444444/
            ├── 73ae521947de4b56a1ec0b09e20511ac.pdf
            └── f94b16489ac8493391347fd487640bc2.png

while SQL still stores:

WorkflowTasks
└── CompletionComment = "Customer documents attached."

9. Allowed File Types and Size

The sample implementation allows:

.pdf
.jpg
.jpeg
.png
.docx
.xlsx

The endpoint currently has:

[RequestSizeLimit(25_000_000)]

This sets the request limit to approximately 25 MB. Both the file types
and limit can be adjusted later.

10. Previous-Step Attachments — Next Enhancement

First verify this flow:

Choose Files
    ↓
Upload
    ↓
Store under correct WorkflowTaskId
    ↓
Complete Task

After that works, add GET/download endpoints so the next workflow
participant can see previous-step attachments.

Example:

TEAM LEAD — Completed

Comment:
Customer provided requested documentation.

Attachments:
📄 Customer_Request.pdf     [View]
📄 Email_Evidence.pdf       [View]


OFFICER — Current Task

Comment:
[________________________________]

Attachments:
[ + Add Attachment ]

                              [Submit]

Previous-step attachments should be read-only. A user should only
add/remove files belonging to their current task before submission.

This can still be done without a new database table by reading the
appropriate workflow-task folder.

11. Future Production Design

The filesystem-only approach is temporary. For production/auditing, add
attachment metadata later while keeping the actual files outside SQL
Server.

A future table could be:

WorkflowTaskAttachments
────────────────────────────────
Id
WorkflowTaskId
ComplaintId
UploadedByUserId
FileName
StoredFileName
ContentType
FileSizeBytes
StoragePath
CreatedAtUtc
UpdatedAtUtc

That would make it possible to reliably track who uploaded each file,
which complaint/task it belongs to, the original filename, upload time,
file size/type, and storage location.

This future table is not required for the current implementation.

Final Summary

Current design:

WorkflowTasks.CompletionComment
        │
        └── SQL Server

Attachments
        │
        └── uploads/workflow-tasks/{WorkflowTaskId}/

For now:

• Keep the database schema unchanged.
• Do not create an attachment entity.
• Do not add a DbSet.
• Do not create an EF migration.
• Do not modify the current ER diagram.
• Add the attachment picker to MyTasksPage.jsx.
• Add the separate upload endpoint to WorkflowTasksController.
• Store files by WorkflowTaskId.
• Keep /complete and dynamic-field logic unchanged.
• Add previous-step attachment viewing/downloading afterward.


