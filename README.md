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





































Suggested structure:

src/
├── components/
│   └── eforms/
│       ├── EFormBuilder.jsx
│       ├── DynamicEForm.jsx
│       ├── DynamicField.jsx
│       ├── EFormPreview.jsx
│       └── eform.css
└── pages/
    └── WorkflowStepFormDesigner.jsx


============================================================
FILE: src/components/eforms/EFormBuilder.jsx
============================================================

import React from "react";
import "./eform.css";

const FIELD_TYPES = [
  { value: "text", label: "Text" },
  { value: "textarea", label: "Text Area" },
  { value: "number", label: "Number" },
  { value: "date", label: "Date" },
  { value: "datetime", label: "Date & Time" },
  { value: "dropdown", label: "Dropdown" },
  { value: "radio", label: "Radio Buttons" },
  { value: "checkbox", label: "Checkbox" },
];

export default function EFormBuilder({ form, setForm }) {
  const addField = () => {
    const newField = {
      id: crypto.randomUUID(),
      label: "",
      fieldKey: "",
      type: "text",
      placeholder: "",
      required: false,
      readOnly: false,
      options: [],
    };

    setForm({
      ...form,
      fields: [...form.fields, newField],
    });
  };

  const updateField = (id, property, value) => {
    setForm({
      ...form,
      fields: form.fields.map((field) =>
        field.id === id ? { ...field, [property]: value } : field
      ),
    });
  };

  const removeField = (id) => {
    setForm({
      ...form,
      fields: form.fields.filter((field) => field.id !== id),
    });
  };

  const addOption = (fieldId) => {
    setForm({
      ...form,
      fields: form.fields.map((field) =>
        field.id === fieldId
          ? { ...field, options: [...field.options, ""] }
          : field
      ),
    });
  };

  const updateOption = (fieldId, optionIndex, value) => {
    setForm({
      ...form,
      fields: form.fields.map((field) => {
        if (field.id !== fieldId) return field;

        const options = [...field.options];
        options[optionIndex] = value;

        return { ...field, options };
      }),
    });
  };

  const removeOption = (fieldId, optionIndex) => {
    setForm({
      ...form,
      fields: form.fields.map((field) => {
        if (field.id !== fieldId) return field;

        return {
          ...field,
          options: field.options.filter((_, index) => index !== optionIndex),
        };
      }),
    });
  };

  return (
    <div className="eform-builder">
      <div className="builder-heading">
        <div>
          <h2>E-Form Designer</h2>
          <p>
            Configure the fields that will appear during this workflow step.
          </p>
        </div>

        <button className="add-field-btn" type="button" onClick={addField}>
          + Add Field
        </button>
      </div>

      <div className="form-information">
        <label>Form Name</label>

        <input
          value={form.name}
          placeholder="Example: Officer Investigation"
          onChange={(e) =>
            setForm({
              ...form,
              name: e.target.value,
            })
          }
        />

        <label>Description</label>

        <textarea
          value={form.description}
          placeholder="Instructions for the user..."
          onChange={(e) =>
            setForm({
              ...form,
              description: e.target.value,
            })
          }
        />
      </div>

      {form.fields.length === 0 && (
        <div className="empty-fields">No fields have been added yet.</div>
      )}

      {form.fields.map((field, index) => (
        <div className="field-card" key={field.id}>
          <div className="field-card-header">
            <strong>Field {index + 1}</strong>

            <button
              className="remove-btn"
              type="button"
              onClick={() => removeField(field.id)}
            >
              Remove
            </button>
          </div>

          <div className="field-grid">
            <div>
              <label>Field Label</label>

              <input
                value={field.label}
                placeholder="Investigation Comments"
                onChange={(e) =>
                  updateField(field.id, "label", e.target.value)
                }
              />
            </div>

            <div>
              <label>Field Type</label>

              <select
                value={field.type}
                onChange={(e) =>
                  updateField(field.id, "type", e.target.value)
                }
              >
                {FIELD_TYPES.map((type) => (
                  <option key={type.value} value={type.value}>
                    {type.label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label>Placeholder</label>

            <input
              value={field.placeholder}
              placeholder="Enter placeholder..."
              onChange={(e) =>
                updateField(field.id, "placeholder", e.target.value)
              }
            />
          </div>

          <div className="field-settings">
            <label>
              <input
                type="checkbox"
                checked={field.required}
                onChange={(e) =>
                  updateField(field.id, "required", e.target.checked)
                }
              />
              Required
            </label>

            <label>
              <input
                type="checkbox"
                checked={field.readOnly}
                onChange={(e) =>
                  updateField(field.id, "readOnly", e.target.checked)
                }
              />
              Read Only
            </label>
          </div>

          {(field.type === "dropdown" || field.type === "radio") && (
            <div className="options-section">
              <label>Options</label>

              {field.options.map((option, optionIndex) => (
                <div className="option-row" key={optionIndex}>
                  <input
                    value={option}
                    placeholder={`Option ${optionIndex + 1}`}
                    onChange={(e) =>
                      updateOption(field.id, optionIndex, e.target.value)
                    }
                  />

                  <button
                    type="button"
                    onClick={() => removeOption(field.id, optionIndex)}
                  >
                    ×
                  </button>
                </div>
              ))}

              <button
                type="button"
                className="add-option-btn"
                onClick={() => addOption(field.id)}
              >
                + Add Option
              </button>
            </div>
          )}
        </div>
      ))}
    </div>
  );
}


============================================================
FILE: src/components/eforms/DynamicField.jsx
============================================================

import React from "react";

export default function DynamicField({ field, value, onChange }) {
  const change = (newValue) => {
    onChange(field.id, newValue);
  };

  const commonProps = {
    disabled: field.readOnly,
    required: field.required,
  };

  const renderField = () => {
    switch (field.type) {
      case "textarea":
        return (
          <textarea
            value={value || ""}
            placeholder={field.placeholder}
            onChange={(e) => change(e.target.value)}
            {...commonProps}
          />
        );

      case "number":
        return (
          <input
            type="number"
            value={value || ""}
            placeholder={field.placeholder}
            onChange={(e) => change(e.target.value)}
            {...commonProps}
          />
        );

      case "date":
        return (
          <input
            type="date"
            value={value || ""}
            onChange={(e) => change(e.target.value)}
            {...commonProps}
          />
        );

      case "datetime":
        return (
          <input
            type="datetime-local"
            value={value || ""}
            onChange={(e) => change(e.target.value)}
            {...commonProps}
          />
        );

      case "dropdown":
        return (
          <select
            value={value || ""}
            onChange={(e) => change(e.target.value)}
            {...commonProps}
          >
            <option value="">Select...</option>

            {field.options.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        );

      case "radio":
        return (
          <div className="radio-options">
            {field.options.map((option) => (
              <label key={option}>
                <input
                  type="radio"
                  name={field.id}
                  value={option}
                  checked={value === option}
                  disabled={field.readOnly}
                  onChange={() => change(option)}
                />
                {option}
              </label>
            ))}
          </div>
        );

      case "checkbox":
        return (
          <label className="checkbox-field">
            <input
              type="checkbox"
              checked={Boolean(value)}
              disabled={field.readOnly}
              onChange={(e) => change(e.target.checked)}
            />
            Yes
          </label>
        );

      default:
        return (
          <input
            type="text"
            value={value || ""}
            placeholder={field.placeholder}
            onChange={(e) => change(e.target.value)}
            {...commonProps}
          />
        );
    }
  };

  return (
    <div className="dynamic-field">
      <label className="field-label">
        {field.label}
        {field.required && <span className="required">*</span>}
      </label>

      {renderField()}
    </div>
  );
}


============================================================
FILE: src/components/eforms/DynamicEForm.jsx
============================================================

import React, { useState } from "react";
import DynamicField from "./DynamicField";
import "./eform.css";

export default function DynamicEForm({
  form,
  complaint,
  onSubmit,
  onSaveDraft,
}) {
  const [values, setValues] = useState({});
  const [errors, setErrors] = useState({});

  const handleChange = (fieldId, value) => {
    setValues((previous) => ({
      ...previous,
      [fieldId]: value,
    }));

    setErrors((previous) => ({
      ...previous,
      [fieldId]: null,
    }));
  };

  const validate = () => {
    const newErrors = {};

    form.fields.forEach((field) => {
      if (!field.required) return;

      const value = values[field.id];

      if (value === undefined || value === null || value === "") {
        newErrors[field.id] = `${field.label} is required.`;
      }
    });

    setErrors(newErrors);

    return Object.keys(newErrors).length === 0;
  };

  const submit = (event) => {
    event.preventDefault();

    if (!validate()) return;

    const submission = {
      complaintId: complaint.id,
      workflowStepId: complaint.currentWorkflowStepId,
      formId: form.id,

      responses: form.fields.map((field) => ({
        fieldId: field.id,
        fieldKey: field.fieldKey,
        label: field.label,
        value: values[field.id] ?? null,
      })),
    };

    console.log("E-Form Submission:", submission);

    onSubmit?.(submission);
  };

  const saveDraft = () => {
    const draft = {
      complaintId: complaint.id,
      formId: form.id,
      responses: values,
    };

    console.log("Saving draft:", draft);

    onSaveDraft?.(draft);
  };

  return (
    <form className="dynamic-eform" onSubmit={submit}>
      <div className="complaint-summary">
        <div>
          <span>Complaint</span>
          <strong>{complaint.referenceNumber}</strong>
        </div>

        <div>
          <span>Status</span>
          <strong>{complaint.status}</strong>
        </div>

        <div>
          <span>Current Step</span>
          <strong>{complaint.currentStepName}</strong>
        </div>
      </div>

      <div className="eform-title">
        <h2>{form.name}</h2>

        {form.description && <p>{form.description}</p>}
      </div>

      {form.fields.map((field) => (
        <div key={field.id}>
          <DynamicField
            field={field}
            value={values[field.id]}
            onChange={handleChange}
          />

          {errors[field.id] && (
            <div className="validation-error">
              {errors[field.id]}
            </div>
          )}
        </div>
      ))}

      <div className="form-actions">
        <button
          type="button"
          className="draft-button"
          onClick={saveDraft}
        >
          Save Draft
        </button>

        <button type="submit" className="submit-button">
          Submit & Continue
        </button>
      </div>
    </form>
  );
}


============================================================
FILE: src/components/eforms/EFormPreview.jsx
============================================================

import React, { useState } from "react";
import DynamicField from "./DynamicField";

export default function EFormPreview({ form }) {
  const [values, setValues] = useState({});

  const handleChange = (fieldId, value) => {
    setValues((previous) => ({
      ...previous,
      [fieldId]: value,
    }));
  };

  return (
    <div className="form-preview">
      <div className="preview-label">FORM PREVIEW</div>

      <h2>{form.name || "Untitled E-Form"}</h2>

      {form.description && <p>{form.description}</p>}

      {form.fields.length === 0 ? (
        <div className="empty-preview">
          Add fields to preview the form.
        </div>
      ) : (
        form.fields.map((field) => (
          <DynamicField
            key={field.id}
            field={field}
            value={values[field.id]}
            onChange={handleChange}
          />
        ))
      )}
    </div>
  );
}


============================================================
FILE: src/pages/WorkflowStepFormDesigner.jsx
============================================================

import React, { useState } from "react";
import EFormBuilder from "../components/eforms/EFormBuilder";
import EFormPreview from "../components/eforms/EFormPreview";
import "../components/eforms/eform.css";

export default function WorkflowStepFormDesigner() {
  const [form, setForm] = useState({
    id: null,
    name: "Officer Investigation",
    description: "Complete the investigation before continuing.",
    fields: [],
  });

  const saveForm = () => {
    console.log("FORM TO SAVE:", form);

    // Later connect this to your ASP.NET Core API:
    // await api.post("/eforms", form);
  };

  return (
    <div className="designer-layout">
      <div className="designer-left">
        <EFormBuilder form={form} setForm={setForm} />
      </div>

      <div className="designer-right">
        <EFormPreview form={form} />
      </div>

      <div className="designer-footer">
        <button
          type="button"
          className="save-form-btn"
          onClick={saveForm}
        >
          Save E-Form
        </button>
      </div>
    </div>
  );
}


============================================================
FILE: src/components/eforms/eform.css
============================================================

.eform-builder,
.dynamic-eform,
.form-preview {
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  padding: 24px;
}

.builder-heading {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.builder-heading h2,
.eform-title h2,
.form-preview h2 {
  margin: 0 0 6px;
}

.builder-heading p,
.eform-title p {
  margin: 0;
  color: #6b7280;
}

.form-information {
  display: grid;
  gap: 8px;
  margin-bottom: 24px;
}

.field-card {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 18px;
  margin-bottom: 16px;
}

.field-card-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}

.field-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.field-grid > div {
  display: flex;
  flex-direction: column;
}

input,
textarea,
select {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  padding: 10px 12px;
  font-size: 14px;
}

textarea {
  resize: vertical;
}

.field-settings {
  display: flex;
  gap: 24px;
  margin-top: 14px;
}

.field-settings input {
  width: auto;
  margin-right: 6px;
}

.dynamic-field {
  margin-bottom: 20px;
}

.field-label {
  display: block;
  font-weight: 600;
  margin-bottom: 7px;
}

.required {
  margin-left: 4px;
  color: #b91c1c;
}

.radio-options {
  display: flex;
  gap: 20px;
}

.radio-options input {
  width: auto;
  margin-right: 5px;
}

.checkbox-field input {
  width: auto;
  margin-right: 7px;
}

.complaint-summary {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  padding: 16px;
  border-radius: 8px;
  background: #f9fafb;
  margin-bottom: 24px;
}

.complaint-summary span {
  display: block;
  font-size: 12px;
  color: #6b7280;
  margin-bottom: 4px;
}

.eform-title {
  margin-bottom: 24px;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 20px;
  border-top: 1px solid #e5e7eb;
}

button {
  cursor: pointer;
}

.add-field-btn,
.save-form-btn,
.submit-button {
  border: none;
  border-radius: 6px;
  padding: 10px 18px;
  font-weight: 600;
}

.draft-button {
  border: 1px solid #d1d5db;
  background: white;
  border-radius: 6px;
  padding: 10px 18px;
}

.remove-btn {
  border: none;
  background: transparent;
}

.validation-error {
  color: #b91c1c;
  font-size: 12px;
  margin-top: -14px;
  margin-bottom: 16px;
}

.options-section {
  margin-top: 16px;
}

.option-row {
  display: flex;
  gap: 8px;
  margin-top: 8px;
}

.designer-layout {
  display: grid;
  grid-template-columns: minmax(500px, 1fr) minmax(400px, 0.8fr);
  gap: 24px;
}

.designer-footer {
  grid-column: 1 / -1;
  display: flex;
  justify-content: flex-end;
}

.preview-label {
  font-size: 12px;
  font-weight: 700;
  color: #6b7280;
  margin-bottom: 12px;
}

.empty-fields,
.empty-preview {
  padding: 24px;
  text-align: center;
  color: #6b7280;
  border: 1px dashed #d1d5db;
  border-radius: 8px;
}

@media (max-width: 1000px) {
  .designer-layout {
    grid-template-columns: 1fr;
  }

  .designer-footer {
    grid-column: 1;
  }

  .field-grid,
  .complaint-summary {
    grid-template-columns: 1fr;
  }
}


============================================================
EXAMPLE MOCK DATA FOR TESTING DynamicEForm
============================================================

const mockComplaint = {
  id: 1024,
  referenceNumber: "CMP-1024",
  status: "In Progress",
  currentWorkflowStepId: 30,
  currentStepName: "Officer Investigation",
};

const mockForm = {
  id: 10,
  name: "Officer Investigation",
  description: "Complete the investigation before continuing.",

  fields: [
    {
      id: "field-1",
      fieldKey: "investigationComments",
      label: "Investigation Comments",
      type: "textarea",
      placeholder: "Enter investigation details...",
      required: true,
      readOnly: false,
      options: [],
    },
    {
      id: "field-2",
      fieldKey: "transactionDate",
      label: "Transaction Date",
      type: "date",
      placeholder: "",
      required: true,
      readOnly: false,
      options: [],
    },
    {
      id: "field-3",
      fieldKey: "customerContacted",
      label: "Customer Contacted?",
      type: "dropdown",
      placeholder: "",
      required: true,
      readOnly: false,
      options: ["Yes", "No"],
    },
    {
      id: "field-4",
      fieldKey: "investigationResult",
      label: "Investigation Result",
      type: "radio",
      placeholder: "",
      required: true,
      readOnly: false,
      options: [
        "Valid Complaint",
        "Invalid Complaint",
        "Need More Information",
      ],
    },
  ],
};


============================================================
HOW TO USE THE RUNTIME FORM
============================================================

import DynamicEForm from "./components/eforms/DynamicEForm";

<DynamicEForm
  form={mockForm}
  complaint={mockComplaint}
  onSubmit={(submission) => {
    console.log("FINAL SUBMISSION", submission);
  }}
  onSaveDraft={(draft) => {
    console.log("DRAFT", draft);
  }}
/>


============================================================
CURRENT FRONTEND FLOW
============================================================

UNIT HEAD:

Workflow Designer
    ↓
Select workflow node
    ↓
Open E-Form Designer
    ↓
Add fields
    ↓
Choose field type
    ↓
Set Required / Read Only
    ↓
Configure dropdown/radio options
    ↓
Live Preview
    ↓
Save E-Form


OFFICER / TEAM LEAD / UNIT HEAD:

Open Complaint
    ↓
Current workflow step determines E-Form
    ↓
DynamicEForm renders configured fields
    ↓
User fills fields
    ↓
Save Draft
       OR
Submit & Continue
    ↓
Frontend creates submission object
    ↓
Later connect submission to ASP.NET Core API


IMPORTANT:
This file contains FRONTEND ONLY. The current Save/Submit actions use
console.log so the UI can be developed and tested before the backend
and database are connected.