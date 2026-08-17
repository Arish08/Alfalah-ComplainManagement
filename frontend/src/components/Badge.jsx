import { complaintPriority, complaintStatus, workflowStatus } from '../constants.js'

export function StatusBadge({ status }) {
  const label = complaintStatus[status] ?? String(status)
  const css = status === 2 ? 'success' : status === 4 ? 'muted' : status === 0 ? 'neutral' : 'info'
  return <span className={`badge badge-${css}`}>{label}</span>
}

export function PriorityBadge({ priority }) {
  const label = complaintPriority[priority] ?? String(priority)
  const css = priority >= 3 ? 'danger' : priority === 2 ? 'warning' : 'neutral'
  return <span className={`badge badge-${css}`}>{label}</span>
}

export function WorkflowStatusBadge({ status }) {
  const label = workflowStatus[status] ?? String(status)
  const css = status === 1 ? 'success' : status === 0 ? 'warning' : 'muted'
  return <span className={`badge badge-${css}`}>{label}</span>
}
