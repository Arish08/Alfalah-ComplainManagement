import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client.js'
import { useDevUser } from '../auth/DevUserContext.jsx'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'
import EmptyState from '../components/EmptyState.jsx'
import { formatDate, relativeSla } from '../constants.js'
import WorkflowTaskFields from '../components/WorkflowTaskFields.jsx'

export default function MyTasksPage({ navigate }) {
  const { userId, roles } = useDevUser()

  const [tasks, setTasks] = useState([])
  const [selected, setSelected] = useState(null)

  const [actions, setActions] = useState([])
  const [selectedActionIndex, setSelectedActionIndex] = useState(0)

  const [assignees, setAssignees] = useState([])
  const [assigneeId, setAssigneeId] = useState('')

  const [comment, setComment] = useState('')
  const [error, setError] = useState('')

  const [saving, setSaving] = useState(false)
  const [loading, setLoading] = useState(true)

  const [reassigning, setReassigning] = useState(false)
  const [reassignUsers, setReassignUsers] = useState([])
  const [reassignUserId, setReassignUserId] = useState('')

  const [taskForm, setTaskForm] = useState(null)

const [fieldValues, setFieldValues] = useState({})

  // =========================================================
  // LOAD MY WORK QUEUE
  // =========================================================

  const load = async () => {
    setLoading(true)

    try {
      const data = await api('/workflow-tasks/my-bucket')

      console.log('MY TASKS:', data)

      setTasks(Array.isArray(data) ? data : [])
      setError('')
    } catch (err) {
      console.error('FAILED TO LOAD TASKS:', err)
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [userId])

  // =========================================================
  // CURRENT SELECTED WORKFLOW ACTION
  // =========================================================

  const selectedAction = actions[selectedActionIndex] || null

  // =========================================================
  // LOAD POSSIBLE ASSIGNEES FOR NEXT WORKFLOW STEP
  //
  // Example:
  // Team Lead -> Officer
  //
  // Once the actions API returns targetRoleCode = Officer,
  // this effect calls:
  //
  // /reference/users?departmentId=...&roleCode=Officer
  // =========================================================

  useEffect(() => {
    if (
      !selected ||
      !selectedAction ||
      !selectedAction.targetRoleCode ||
      selectedAction.targetNodeType !== 1
    ) {
      setAssignees([])
      setAssigneeId('')
      return
    }

    let cancelled = false

    const loadAssignees = async () => {
      try {
        setAssignees([])
        setAssigneeId('')

        console.log('LOADING ASSIGNEES:', {
          taskId: selected.id,
          departmentId: selected.departmentId,
          roleCode: selectedAction.targetRoleCode,
          requiresAssignee: selectedAction.requiresAssignee,
          targetNodeType: selectedAction.targetNodeType,
        })

        const users = await api(
          `/reference/users?departmentId=${selected.departmentId}&roleCode=${encodeURIComponent(
            selectedAction.targetRoleCode
          )}`
        )

        console.log('ASSIGNEES RETURNED:', users)

        if (cancelled) return

        const userList = Array.isArray(users) ? users : []

        setAssignees(userList)

        // Automatically select first user when the workflow
        // specifically requires an individual assignment.
        if (
          selectedAction.requiresAssignee &&
          userList.length > 0
        ) {
          setAssigneeId(userList[0].id)
        } else {
          setAssigneeId('')
        }
      } catch (err) {
        if (cancelled) return

        console.error('FAILED TO LOAD ASSIGNEES:', err)

        setAssignees([])
        setAssigneeId('')
        setError(err.message)
      }
    }

    loadAssignees()

    return () => {
      cancelled = true
    }
  }, [
    selected?.id,
    selected?.departmentId,
    selectedActionIndex,
    selectedAction?.targetNodeId,
    selectedAction?.targetRoleCode,
    selectedAction?.targetNodeType,
    selectedAction?.requiresAssignee,
  ])

  // =========================================================
  // OPEN TASK
  // =========================================================

  const openTask = async (task) => {
    setSelected(task)

    setActions([])
    setSelectedActionIndex(0)

    setAssignees([])
    setAssigneeId('')

    setComment('')
    setTaskForm(null)
setFieldValues({})

    setReassigning(false)
    setReassignUsers([])
    setReassignUserId('')

    setError('')

    try {
      console.log('OPENING TASK:', task)

     const [
  availableActions,
  form,
] = await Promise.all([
  api(
    `/workflow-tasks/${task.id}/actions`
  ),

  api(
    `/workflow-tasks/${task.id}/form`
  ),
])

setActions(
  Array.isArray(availableActions)
    ? availableActions
    : []
)

setTaskForm(form)
    } catch (err) {
      console.error('FAILED TO LOAD TASK ACTIONS:', err)
      setError(err.message)
    }
  }

  // =========================================================
  // COMPLETE / ADVANCE TASK
  // =========================================================

  const complete = async () => {
    if (!selected || !selectedAction) return

    if (
      selectedAction.requiresAssignee &&
      !assigneeId
    ) {
      setError(
        `Select a ${selectedAction.targetRoleCode} before continuing.`
      )

      return
    }

    setSaving(true)
    setError('')

    try {
const payload = {
  outcomeKey:
    selectedAction.outcomeKey,

  nextAssigneeUserId:
    assigneeId || null,

  comment:
    comment.trim() || null,

  fieldAnswers:
    taskForm?.currentFields?.map(
      (field) => ({
        fieldId: field.id,
        value:
          fieldValues[field.id] ??
          null,
      })
    ) || [],
}

      console.log('COMPLETING TASK:', {
        taskId: selected.id,
        action: selectedAction,
        payload,
      })

      await api(
        `/workflow-tasks/${selected.id}/complete`,
        {
          method: 'POST',
          body: payload,
        }
      )

      setSelected(null)

      setActions([])
      setAssignees([])
      setAssigneeId('')
      setComment('')
      setTaskForm(null)
setFieldValues({})

      await load()
    } catch (err) {
      console.error('FAILED TO COMPLETE TASK:', err)
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  // =========================================================
  // START REASSIGNMENT
  //
  // This moves the CURRENT step to another user without
  // advancing the workflow.
  // =========================================================

  const startReassign = async () => {
    if (
      !selected ||
      !selected.assignedRoleCode
    ) {
      return
    }

    setReassigning(true)
    setReassignUsers([])
    setReassignUserId('')
    setError('')

    try {
      console.log('LOADING REASSIGNMENT USERS:', {
        departmentId: selected.departmentId,
        roleCode: selected.assignedRoleCode,
      })

      const users = await api(
        `/reference/users?departmentId=${selected.departmentId}&roleCode=${encodeURIComponent(
          selected.assignedRoleCode
        )}`
      )

      console.log(
        'REASSIGNMENT USERS RETURNED:',
        users
      )

      const userList = Array.isArray(users)
        ? users
        : []

      setReassignUsers(userList)

      if (userList.length > 0) {
        setReassignUserId(userList[0].id)
      }
    } catch (err) {
      console.error(
        'FAILED TO LOAD REASSIGNMENT USERS:',
        err
      )

      setError(err.message)
    }
  }

  // =========================================================
  // REASSIGN CURRENT TASK
  // =========================================================

  const reassign = async () => {
    if (
      !selected ||
      !reassignUserId
    ) {
      return
    }

    setSaving(true)
    setError('')

    try {
      console.log('REASSIGNING TASK:', {
        taskId: selected.id,
        userId: reassignUserId,
      })

      await api(
        `/workflow-tasks/${selected.id}/reassign`,
        {
          method: 'POST',
          body: {
            userId: reassignUserId,
            comment: comment.trim() || null,
          },
        }
      )

      setSelected(null)
      setReassigning(false)
      setReassignUsers([])
      setReassignUserId('')
      setComment('')

      await load()
    } catch (err) {
      console.error(
        'FAILED TO REASSIGN TASK:',
        err
      )

      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  // =========================================================
  // OVERDUE COUNT
  // =========================================================

  const overdue = useMemo(() => {
    const now = new Date()

    return tasks.filter(
      (task) =>
        task.dueAtUtc &&
        new Date(task.dueAtUtc) < now
    ).length
  }, [tasks])

  // =========================================================
  // PAGE
  // =========================================================

  return (
    <>
      <PageHeader
        eyebrow="WORK QUEUE"
        title="My assigned work"
        description={`${tasks.length} open task${
          tasks.length === 1 ? '' : 's'
        } · ${overdue} overdue`}
      />

      <ErrorBanner message={error} />

      {/* =====================================================
          TASK LIST
      ====================================================== */}

      {loading ? (
        <div className="loading-block">
          Loading your queue…
        </div>
      ) : tasks.length === 0 ? (
        <section className="panel">
          <EmptyState
            title="Your queue is clear"
            description="New workflow tasks assigned to you or your role will appear here."
          />
        </section>
      ) : (
        <div className="task-grid">
          {tasks.map((task) => {
            const sla = relativeSla(task.dueAtUtc)

            return (
              <article
                className={`task-card ${
                  sla.state === 'danger'
                    ? 'task-overdue'
                    : ''
                }`}
                key={task.id}
              >
                <div className="task-card-top">
                  <span className="step-pill">
                    {task.nodeName}
                  </span>

                  <span
                    className={`sla-chip sla-${sla.state}`}
                  >
                    {sla.text}
                  </span>
                </div>

                <button
                  className="task-title-link"
                  onClick={() =>
                    navigate(
                      `/complaints/${task.complaintId}`
                    )
                  }
                >
                  {task.complaintNumber}
                </button>

                <h3>
                  {task.complaintSubject}
                </h3>

                <div className="task-details">
                  <div>
                    <span>Queue</span>

                    <strong>
                      {task.assignedRoleCode || '—'}
                    </strong>
                  </div>

                  <div>
                    <span>Assigned to</span>

                    <strong>
                      {task.assignedToUserName ||
                        'Role queue'}
                    </strong>
                  </div>

                  <div>
                    <span>Opened</span>

                    <strong>
                      {formatDate(
                        task.openedAtUtc
                      )}
                    </strong>
                  </div>

                  <div>
                    <span>Due</span>

                    <strong>
                      {formatDate(
                        task.dueAtUtc
                      )}
                    </strong>
                  </div>
                </div>

                <button
                  className="btn btn-primary full-width"
                  onClick={() =>
                    openTask(task)
                  }
                >
                  Open task
                </button>
              </article>
            )
          })}
        </div>
      )}

      {/* =====================================================
          TASK MODAL
      ====================================================== */}

      {selected && (
        <div
          className="modal-backdrop"
          onMouseDown={(event) => {
            if (
              event.target ===
              event.currentTarget
            ) {
              setSelected(null)
            }
          }}
        >
          <div className="modal">
            {/* =========================
                MODAL HEADER
            ========================== */}

            <div className="modal-header">
              <div>
                <div className="eyebrow">
                  {selected.complaintNumber}
                </div>

                <h2>
                  {selected.nodeName}
                </h2>

                <p>
                  {selected.complaintSubject}
                </p>
              </div>

              <button
                className="icon-button"
                onClick={() =>
                  setSelected(null)
                }
              >
                ×
              </button>
            </div>

            <ErrorBanner message={error} />

            {!reassigning ? (
              <>
                {/* =========================
                    ACTION SELECTION
                ========================== */}

                <div className="field">
                  <span>
                    Choose action
                  </span>

                  <div className="action-choice-list">
                    {actions.map(
                      (action, index) => (
                        <button
                          key={`${action.targetNodeId}-${index}`}
                          type="button"
                          className={`action-choice ${
                            selectedActionIndex ===
                            index
                              ? 'selected'
                              : ''
                          }`}
                          onClick={() => {
                            setSelectedActionIndex(
                              index
                            )

                            setAssignees([])
                            setAssigneeId('')
                            setError('')
                          }}
                        >
                          <strong>
                            {action.label}
                          </strong>

                          <span>
                            Next:{' '}
                            {
                              action.targetNodeName
                            }

                            {action.targetRoleCode
                              ? ` · ${action.targetRoleCode}`
                              : ''}
                          </span>
                        </button>
                      )
                    )}

                    {actions.length === 0 && (
                      <div className="inline-empty">
                        No available transition
                        was returned for this
                        step.
                      </div>
                    )}
                  </div>
                </div>

                {/* =========================
                    NEXT USER ASSIGNMENT
                ========================== */}

                {selectedAction?.targetNodeType ===
                  1 &&
                  selectedAction.targetRoleCode && (
                    <label className="field">
                      <span>
                        {selectedAction.requiresAssignee
                          ? `Assign specific ${selectedAction.targetRoleCode}`
                          : `Optional direct assignment (${selectedAction.targetRoleCode})`}
                      </span>

                      <select
                        className="input"
                        value={assigneeId}
                        onChange={(event) =>
                          setAssigneeId(
                            event.target.value
                          )
                        }
                        required={
                          selectedAction.requiresAssignee
                        }
                      >
                        {/* No users found */}

                        {assignees.length ===
                          0 && (
                          <option value="">
                            No{' '}
                            {
                              selectedAction.targetRoleCode
                            }{' '}
                            users available
                          </option>
                        )}

                        {/* Allow role queue when
                            direct assignment is optional */}

                        {!selectedAction.requiresAssignee &&
                          assignees.length >
                            0 && (
                            <option value="">
                              Send to role queue
                            </option>
                          )}

                        {/* Actual users */}

                        {assignees.map(
                          (user) => (
                            <option
                              key={user.id}
                              value={user.id}
                            >
                              {user.displayName}
                              {' · '}
                              {
                                user.employeeCode
                              }
                            </option>
                          )
                        )}
                      </select>

                      {/* Debug / useful status */}

                      {selectedAction.requiresAssignee &&
                        assignees.length ===
                          0 && (
                          <small
                            style={{
                              color:
                                '#b42318',
                              marginTop:
                                '6px',
                            }}
                          >
                            No active{' '}
                            {
                              selectedAction.targetRoleCode
                            }{' '}
                            users were returned
                            for this department.
                          </small>
                        )}
                    </label>
                  )}
                  <WorkflowTaskFields
  form={taskForm}
  values={fieldValues}
  onChange={setFieldValues}
/>

                {/* =========================
                    COMMENT
                ========================== */}

                <label className="field">
                  <span>
                    Comment
                  </span>

                  <textarea
                    className="input textarea"
                    rows={4}
                    value={comment}
                    onChange={(event) =>
                      setComment(
                        event.target.value
                      )
                    }
                    placeholder="Add an optional action note…"
                  />
                </label>

                {/* =========================
                    ACTION BUTTONS
                ========================== */}

                <div className="modal-actions">
                  {(roles.has('TeamLead') ||
                    roles.has(
                      'UnitHead'
                    )) &&
                    selected.assignedRoleCode && (
                      <button
                        className="btn btn-secondary"
                        onClick={
                          startReassign
                        }
                      >
                        Reassign current step
                      </button>
                    )}

                  <div className="modal-actions-right">
                    <button
                      className="btn btn-secondary"
                      onClick={() =>
                        setSelected(null)
                      }
                    >
                      Cancel
                    </button>

                    <button
                      className="btn btn-primary"
                      disabled={
                        saving ||
                        !selectedAction ||
                        (selectedAction.requiresAssignee &&
                          !assigneeId)
                      }
                      onClick={complete}
                    >
                      {saving
                        ? 'Processing…'
                        : selectedAction?.label ||
                          'Complete'}
                    </button>
                  </div>
                </div>
              </>
            ) : (
              <>
                {/* =================================================
                    REASSIGNMENT VIEW
                ================================================== */}

                <div className="notice-box">
                  Reassign this{' '}
                  <strong>
                    {
                      selected.assignedRoleCode
                    }
                  </strong>{' '}
                  task without advancing
                  the workflow.
                </div>

                <label className="field">
                  <span>
                    Assign to
                  </span>

                  <select
                    className="input"
                    value={
                      reassignUserId
                    }
                    onChange={(event) =>
                      setReassignUserId(
                        event.target.value
                      )
                    }
                  >
                    {reassignUsers.length ===
                      0 && (
                      <option value="">
                        No users available
                      </option>
                    )}

                    {reassignUsers.map(
                      (user) => (
                        <option
                          key={user.id}
                          value={user.id}
                        >
                          {user.displayName}
                          {' · '}
                          {
                            user.employeeCode
                          }
                        </option>
                      )
                    )}
                  </select>
                </label>

                <label className="field">
                  <span>
                    Comment
                  </span>

                  <textarea
                    className="input textarea"
                    rows={3}
                    value={comment}
                    onChange={(event) =>
                      setComment(
                        event.target.value
                      )
                    }
                  />
                </label>

                <div className="modal-actions">
                  <button
                    className="btn btn-secondary"
                    onClick={() =>
                      setReassigning(
                        false
                      )
                    }
                  >
                    ← Back
                  </button>

                  <button
                    className="btn btn-primary"
                    onClick={reassign}
                    disabled={
                      saving ||
                      !reassignUserId
                    }
                  >
                    {saving
                      ? 'Reassigning…'
                      : 'Reassign task'}
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </>
  )
}