import { useState } from 'react'

const fieldTypes = [
  ['text', 'Text'],
  ['textarea', 'Text area'],
  ['number', 'Number'],
  ['date', 'Date'],
  ['dropdown', 'Dropdown'],
  ['radio', 'Radio buttons'],
  ['checkbox', 'Checkbox'],
]

function makeFieldKey(label) {
  return label
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
}

function createEmptyField(displayOrder) {
  return {
    clientId: crypto.randomUUID(),
    fieldKey: '',
    label: '',
    fieldType: 'text',
    placeholder: '',
    isRequired: false,
    displayOrder,
    options: [],
  }
}

export default function WorkflowFieldBuilder({
  fields = [],
  onChange,
  disabled = false,
}) {
  const [isModalOpen, setIsModalOpen] =
    useState(false)

  const [editingIndex, setEditingIndex] =
    useState(null)

  const [draftField, setDraftField] =
    useState(null)

  // --------------------------------------------------
  // OPEN ADD MODAL
  // --------------------------------------------------

  const openAddModal = () => {
    setEditingIndex(null)

    setDraftField(
      createEmptyField(fields.length)
    )

    setIsModalOpen(true)
  }

  // --------------------------------------------------
  // OPEN EDIT MODAL
  // --------------------------------------------------

  const openEditModal = (field, index) => {
    setEditingIndex(index)

    setDraftField({
      ...field,
      options: [...(field.options || [])],
    })

    setIsModalOpen(true)
  }

  // --------------------------------------------------
  // CLOSE MODAL
  // --------------------------------------------------

  const closeModal = () => {
    setIsModalOpen(false)
    setEditingIndex(null)
    setDraftField(null)
  }

  // --------------------------------------------------
  // UPDATE MODAL FIELD
  // --------------------------------------------------

  const updateDraftField = (patch) => {
    setDraftField((current) => ({
      ...current,
      ...patch,
    }))
  }

  // --------------------------------------------------
  // SAVE ADD / EDIT
  // --------------------------------------------------

  const saveField = () => {
    if (!draftField) return

    const label = draftField.label.trim()

    if (!label) {
      window.alert('Field label is required.')
      return
    }

    const fieldKey =
      draftField.fieldKey.trim() ||
      makeFieldKey(label)

    if (!fieldKey) {
      window.alert('Field key is required.')
      return
    }

    // Prevent duplicate field keys inside the same step
    const duplicateKey = fields.some(
      (field, index) =>
        index !== editingIndex &&
        (field.fieldKey || '')
          .trim()
          .toLowerCase() ===
          fieldKey.toLowerCase()
    )

    if (duplicateKey) {
      window.alert(
        `A field with key "${fieldKey}" already exists in this step.`
      )
      return
    }

    if (
      (draftField.fieldType === 'dropdown' ||
        draftField.fieldType === 'radio') &&
      (!draftField.options ||
        draftField.options.length === 0)
    ) {
      window.alert(
        'Please add at least one option.'
      )
      return
    }

    const fieldToSave = {
      ...draftField,
      label,
      fieldKey,
      placeholder:
        draftField.placeholder?.trim() || '',
      options: draftField.options || [],
    }

    // EDIT EXISTING FIELD
    if (editingIndex !== null) {
      onChange(
        fields.map((field, index) =>
          index === editingIndex
            ? {
                ...fieldToSave,
                displayOrder:
                  field.displayOrder ?? index,
              }
            : field
        )
      )
    }

    // ADD NEW FIELD
    else {
      onChange([
        ...fields,
        {
          ...fieldToSave,
          displayOrder: fields.length,
        },
      ])
    }

    closeModal()
  }

  // --------------------------------------------------
  // REMOVE FIELD
  // --------------------------------------------------

  const removeField = (index) => {
    onChange(
      fields
        .filter(
          (_, fieldIndex) =>
            fieldIndex !== index
        )
        .map((field, fieldIndex) => ({
          ...field,
          displayOrder: fieldIndex,
        }))
    )
  }

  return (
    <div className="workflow-field-builder">
      <div className="designer-section-title">
        Step fields
      </div>

      <p className="muted-text">
        These fields will be editable by the
        person completing this step. Earlier
        step values will appear read-only.
      </p>

      {/* ---------------------------------------
          EXISTING FIELDS
      --------------------------------------- */}

      {fields.length === 0 && (
        <div className="workflow-fields-empty">
          No fields added to this step.
        </div>
      )}

      {fields.map((field, index) => (
        <div
          className="workflow-field-summary"
          key={
            field.clientId ||
            field.id ||
            index
          }
        >
          <div className="workflow-field-summary-info">
            <strong>
              {field.label || 'Untitled field'}
            </strong>

            <div className="workflow-field-meta">
              {field.fieldType || 'text'}

              {field.isRequired
                ? ' • Required'
                : ' • Optional'}
            </div>

            <div className="workflow-field-key">
              {field.fieldKey}
            </div>
          </div>

          {!disabled && (
            <div className="workflow-field-actions">
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() =>
                  openEditModal(field, index)
                }
              >
                Edit
              </button>

              <button
                type="button"
                className="btn btn-danger-soft"
                onClick={() =>
                  removeField(index)
                }
              >
                Remove
              </button>
            </div>
          )}
        </div>
      ))}

      {!disabled && (
        <button
          type="button"
          className="btn btn-secondary"
          onClick={openAddModal}
        >
          + Add field
        </button>
      )}

      {/* ---------------------------------------
          ADD / EDIT FIELD MODAL
      --------------------------------------- */}

      {isModalOpen && draftField && (
        <div
          className="workflow-field-modal-backdrop"
          onMouseDown={(event) => {
            if (
              event.target ===
              event.currentTarget
            ) {
              closeModal()
            }
          }}
        >
          <div
            className="workflow-field-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="workflow-field-modal-title"
          >
            <div className="workflow-field-modal-header">
              <div>
                <h3
                  id="workflow-field-modal-title"
                  className="workflow-field-modal-title"
                >
                  {editingIndex !== null
                    ? 'Edit field'
                    : 'Add field'}
                </h3>

                <p className="muted-text">
                  Configure the field for this
                  workflow step.
                </p>
              </div>

              <button
                type="button"
                className="workflow-field-modal-close"
                onClick={closeModal}
                aria-label="Close"
              >
                ×
              </button>
            </div>

            <div className="workflow-field-modal-body">
              {/* FIELD LABEL */}

              <label className="field compact">
                <span>Field label</span>

                <input
                  className="input"
                  autoFocus
                  value={
                    draftField.label || ''
                  }
                  onChange={(event) => {
                    const oldGeneratedKey =
                      makeFieldKey(
                        draftField.label || ''
                      )

                    const label =
                      event.target.value

                    const shouldUpdateKey =
                      !draftField.fieldKey ||
                      draftField.fieldKey ===
                        oldGeneratedKey

                    updateDraftField({
                      label,

                      ...(shouldUpdateKey
                        ? {
                            fieldKey:
                              makeFieldKey(
                                label
                              ),
                          }
                        : {}),
                    })
                  }}
                  placeholder="e.g. CNIC Number"
                />
              </label>

              {/* FIELD KEY */}

              <label className="field compact">
                <span>Field key</span>

                <input
                  className="input"
                  value={
                    draftField.fieldKey || ''
                  }
                  onChange={(event) =>
                    updateDraftField({
                      fieldKey:
                        event.target.value,
                    })
                  }
                  placeholder="e.g. cnic_number"
                />
              </label>

              {/* FIELD TYPE */}

              <label className="field compact">
                <span>Field type</span>

                <select
                  className="input"
                  value={
                    draftField.fieldType ||
                    'text'
                  }
                  onChange={(event) => {
                    const fieldType =
                      event.target.value

                    updateDraftField({
                      fieldType,

                      options:
                        fieldType ===
                          'dropdown' ||
                        fieldType === 'radio'
                          ? draftField.options ||
                            []
                          : [],
                    })
                  }}
                >
                  {fieldTypes.map(
                    ([value, label]) => (
                      <option
                        key={value}
                        value={value}
                      >
                        {label}
                      </option>
                    )
                  )}
                </select>
              </label>

              {/* PLACEHOLDER */}

              <label className="field compact">
                <span>Placeholder</span>

                <input
                  className="input"
                  value={
                    draftField.placeholder ||
                    ''
                  }
                  onChange={(event) =>
                    updateDraftField({
                      placeholder:
                        event.target.value,
                    })
                  }
                  placeholder="e.g. Enter CNIC number"
                />
              </label>

              {/* DROPDOWN / RADIO OPTIONS */}

              {(draftField.fieldType ===
                'dropdown' ||
                draftField.fieldType ===
                  'radio') && (
                <label className="field compact">
                  <span>
                    Options — one per line
                  </span>

                  <textarea
                    className="input textarea"
                    rows={5}
                    value={(
                      draftField.options || []
                    ).join('\n')}
                    onChange={(event) =>
                      updateDraftField({
                        options:
                          event.target.value
                            .split('\n')
                            .map((x) =>
                              x.trim()
                            )
                            .filter(Boolean),
                      })
                    }
                    placeholder={
                      'Option 1\nOption 2\nOption 3'
                    }
                  />
                </label>
              )}

              {/* REQUIRED */}

              <label className="checkbox-field">
                <input
                  type="checkbox"
                  checked={
                    !!draftField.isRequired
                  }
                  onChange={(event) =>
                    updateDraftField({
                      isRequired:
                        event.target.checked,
                    })
                  }
                />

                Required field
              </label>
            </div>

            <div className="workflow-field-modal-footer">
              <button
                type="button"
                className="btn btn-secondary"
                onClick={closeModal}
              >
                Cancel
              </button>

              <button
                type="button"
                className="btn btn-primary"
                onClick={saveField}
              >
                {editingIndex !== null
                  ? 'Save changes'
                  : 'Add field'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}