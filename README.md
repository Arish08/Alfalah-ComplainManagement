Complaint Management System — Dynamic E-Form Frontend

A React frontend for a dynamic E-Form system integrated with a Complaint Management workflow.

Instead of keeping the complaint action/comment modal static, the Unit Head can configure E-Form fields for each workflow step. When a complaint reaches that step, the assigned Officer, Team Lead, or Unit Head receives the appropriate dynamic form.

Architecture

Workflow Step
    ↓
E-Form Definition
    ↓
Dynamic Fields
    ↓
Officer / Team Lead / Unit Head
    ↓
Save Draft or Submit & Continue
    ↓
Backend / Database (future integration)
    ↓
Next Workflow Step

Features

• Dynamic E-Form builder
• Live form preview
• Workflow-step-based forms
• Required and read-only fields
• Text, textarea, number, date, date/time fields
• Dropdowns, radio buttons, and checkboxes
• Configurable dropdown/radio options
• Client-side validation
• Save Draft
• Submit & Continue
• Mock complaint/form data for testing
• Ready for ASP.NET Core API integration

Project Structure

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

1. EFormBuilder.jsx

Used by the Unit Head to configure the form belonging to a workflow step.

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

    setForm({ ...form, fields: [...form.fields, newField] });
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
      fields: form.fields.map((field) =>
        field.id === fieldId
          ? {
              ...field,
              options: field.options.filter((_, i) => i !== optionIndex),
            }
          : field
      ),
    });
  };

  return (
    <div className="eform-builder">
      <div className="builder-heading">
        <div>
          <h2>E-Form Designer</h2>
          <p>Configure fields for this workflow step.</p>
        </div>

        <button type="button" className="add-field-btn" onClick={addField}>
          + Add Field
        </button>
      </div>

      <div className="form-information">
        <label>Form Name</label>
        <input
          value={form.name}
          placeholder="Officer Investigation"
          onChange={(e) => setForm({ ...form, name: e.target.value })}
        />

        <label>Description</label>
        <textarea
          value={form.description}
          placeholder="Instructions for the user..."
          onChange={(e) => setForm({ ...form, description: e.target.value })}
        />
      </div>

      {form.fields.map((field, index) => (
        <div className="field-card" key={field.id}>
          <div className="field-card-header">
            <strong>Field {index + 1}</strong>
            <button type="button" onClick={() => removeField(field.id)}>
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

          <label>Placeholder</label>
          <input
            value={field.placeholder}
            onChange={(e) =>
              updateField(field.id, "placeholder", e.target.value)
            }
          />

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

              <button type="button" onClick={() => addOption(field.id)}>
                + Add Option
              </button>
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

2. DynamicField.jsx

Reusable renderer that determines the correct React control from the field definition.

import React from "react";

export default function DynamicField({ field, value, onChange }) {
  const change = (newValue) => onChange(field.id, newValue);

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
          <label>
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

3. DynamicEForm.jsx

Runtime form displayed to the Officer, Team Lead, or Unit Head.

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
    setValues((previous) => ({ ...previous, [fieldId]: value }));
    setErrors((previous) => ({ ...previous, [fieldId]: null }));
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
            <div className="validation-error">{errors[field.id]}</div>
          )}
        </div>
      ))}

      <div className="form-actions">
        <button type="button" onClick={saveDraft}>
          Save Draft
        </button>

        <button type="submit">
          Submit & Continue
        </button>
      </div>
    </form>
  );
}

4. EFormPreview.jsx

import React, { useState } from "react";
import DynamicField from "./DynamicField";

export default function EFormPreview({ form }) {
  const [values, setValues] = useState({});

  return (
    <div className="form-preview">
      <div className="preview-label">FORM PREVIEW</div>
      <h2>{form.name || "Untitled E-Form"}</h2>
      {form.description && <p>{form.description}</p>}

      {form.fields.map((field) => (
        <DynamicField
          key={field.id}
          field={field}
          value={values[field.id]}
          onChange={(fieldId, value) =>
            setValues((previous) => ({
              ...previous,
              [fieldId]: value,
            }))
          }
        />
      ))}
    </div>
  );
}

5. WorkflowStepFormDesigner.jsx

Combines the E-Form Builder and live preview.

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

    // Future:
    // await api.post("/eforms", form);
  };

  return (
    <div className="designer-layout">
      <div>
        <EFormBuilder form={form} setForm={setForm} />
      </div>

      <div>
        <EFormPreview form={form} />
      </div>

      <div className="designer-footer">
        <button type="button" onClick={saveForm}>
          Save E-Form
        </button>
      </div>
    </div>
  );
}

6. Basic Styling

.eform-builder,
.dynamic-eform,
.form-preview {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  padding: 24px;
}

.builder-heading,
.field-card-header,
.form-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.field-card {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 18px;
  margin: 16px 0;
}

.field-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

input,
textarea,
select {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  padding: 10px 12px;
  margin: 6px 0 12px;
}

.dynamic-field {
  margin-bottom: 20px;
}

.field-label {
  display: block;
  font-weight: 600;
  margin-bottom: 7px;
}

.required,
.validation-error {
  color: #b91c1c;
}

.required {
  margin-left: 4px;
}

.complaint-summary {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  background: #f9fafb;
  padding: 16px;
  border-radius: 8px;
  margin-bottom: 24px;
}

.complaint-summary span {
  display: block;
  font-size: 12px;
  color: #6b7280;
}

.form-actions {
  justify-content: flex-end;
  gap: 12px;
  border-top: 1px solid #e5e7eb;
  padding-top: 20px;
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

@media (max-width: 1000px) {
  .designer-layout,
  .field-grid,
  .complaint-summary {
    grid-template-columns: 1fr;
  }
}

Mock Data

Use this while the backend is not connected.

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
      fieldKey: "customerContacted",
      label: "Customer Contacted?",
      type: "dropdown",
      required: true,
      readOnly: false,
      options: ["Yes", "No"],
    },
    {
      id: "field-3",
      fieldKey: "investigationResult",
      label: "Investigation Result",
      type: "radio",
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

Runtime Example

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

Backend Integration

The current version is intentionally frontend-only.

When the ASP.NET Core backend is ready, replace console.log() with API requests.

await fetch("/api/eforms", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
  },
  body: JSON.stringify(form),
});

The intended full architecture is:

React Workflow Designer
        ↓
E-Form Builder
        ↓
ASP.NET Core API
        ↓
SQL Server

Complaint Runtime
        ↓
Current Workflow Step
        ↓
Load E-Form Definition
        ↓
Dynamic React Form
        ↓
Submit Responses
        ↓
ASP.NET Core API
        ↓
Database
        ↓
Advance Workflow

Future Enhancements

• Drag-and-drop field ordering
• Conditional fields
• File uploads
• Role-based field visibility
• Field-level edit permissions
• Configurable validation
• Form versioning
• Workflow branching based on E-Form responses
• Submission history
• Full audit trail
• Reporting/export support
• Dynamic backend integration

────────