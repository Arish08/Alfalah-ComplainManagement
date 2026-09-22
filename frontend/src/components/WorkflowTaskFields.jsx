function displayValue(value) {
  if (
    value === null ||
    value === undefined ||
    value === ''
  ) {
    return '—'
  }

  if (Array.isArray(value)) {
    return value.join(', ')
  }

  if (typeof value === 'boolean') {
    return value ? 'Yes' : 'No'
  }

  if (typeof value === 'object') {
    return JSON.stringify(value)
  }

  return String(value)
}

export default function WorkflowTaskFields({
  form,
  values,
  onChange,
}) {
  if (!form) return null

  const setValue = (
    fieldId,
    value
  ) => {
    onChange({
      ...values,
      [fieldId]: value,
    })
  }

  return (
    <div className="workflow-task-form">

      {form.previousSteps?.map(
        (step) => (
          <section
            className="previous-step-fields"
            key={step.workflowTaskId}
          >
            <div className="designer-section-title">
              {step.nodeName}
            </div>

            <small>
              Completed by{' '}
              {step.submittedBy || '—'}
            </small>

            {step.fields.map(
              (field) => (
                <label
                  className="field"
                  key={`${step.workflowTaskId}-${field.fieldKey}`}
                >
                  <span>
                    {field.label}
                  </span>

                  <input
                    className="input"
                    value={displayValue(
                      field.value
                    )}
                    readOnly
                  />
                </label>
              )
            )}
          </section>
        )
      )}

      {form.currentFields?.length >
        0 && (
        <section className="current-step-fields">
          <div className="designer-section-title">
            {form.nodeName}
          </div>

          {form.currentFields.map(
            (field) => {
              const value =
                values[field.id] ?? ''

              if (
                field.fieldType ===
                'textarea'
              ) {
                return (
                  <label
                    className="field"
                    key={field.id}
                  >
                    <span>
                      {field.label}
                      {field.isRequired
                        ? ' *'
                        : ''}
                    </span>

                    <textarea
                      className="input textarea"
                      value={value}
                      placeholder={
                        field.placeholder ||
                        ''
                      }
                      onChange={(event) =>
                        setValue(
                          field.id,
                          event.target
                            .value
                        )
                      }
                    />
                  </label>
                )
              }

              if (
                field.fieldType ===
                  'dropdown' ||
                field.fieldType ===
                  'radio'
              ) {
                return (
                  <label
                    className="field"
                    key={field.id}
                  >
                    <span>
                      {field.label}
                      {field.isRequired
                        ? ' *'
                        : ''}
                    </span>

                    <select
                      className="input"
                      value={value}
                      onChange={(event) =>
                        setValue(
                          field.id,
                          event.target
                            .value
                        )
                      }
                    >
                      <option value="">
                        Select...
                      </option>

                      {field.options.map(
                        (option) => (
                          <option
                            key={option}
                            value={option}
                          >
                            {option}
                          </option>
                        )
                      )}
                    </select>
                  </label>
                )
              }

              if (
                field.fieldType ===
                'checkbox'
              ) {
                return (
                  <label
                    className="checkbox-field"
                    key={field.id}
                  >
                    <input
                      type="checkbox"
                      checked={
                        !!value
                      }
                      onChange={(event) =>
                        setValue(
                          field.id,
                          event.target
                            .checked
                        )
                      }
                    />

                    {field.label}

                    {field.isRequired
                      ? ' *'
                      : ''}
                  </label>
                )
              }

              return (
                <label
                  className="field"
                  key={field.id}
                >
                  <span>
                    {field.label}

                    {field.isRequired
                      ? ' *'
                      : ''}
                  </span>

                  <input
                    className="input"
                    type={
                      field.fieldType ===
                      'number'
                        ? 'number'
                        : field.fieldType ===
                            'date'
                          ? 'date'
                          : 'text'
                    }
                    value={value}
                    placeholder={
                      field.placeholder ||
                      ''
                    }
                    onChange={(event) =>
                      setValue(
                        field.id,
                        event.target.value
                      )
                    }
                  />
                </label>
              )
            }
          )}
        </section>
      )}
    </div>
  )
}