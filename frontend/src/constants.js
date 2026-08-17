export const complaintStatus = {
  0: 'Open',
  1: 'In Progress',
  2: 'Resolved',
  3: 'Closed',
  4: 'Cancelled',
}

export const complaintPriority = {
  0: 'Low',
  1: 'Normal',
  2: 'High',
  3: 'Critical',
}

export const workflowStatus = {
  0: 'Draft',
  1: 'Published',
  2: 'Archived',
}

export const workflowNodeType = {
  Start: 0,
  HumanTask: 1,
  End: 2,
}

export const roleOptions = [
  'UnitHead',
  'TeamLead',
  'Officer',
  'DeptAdmin',
]

export function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function relativeSla(dueAtUtc) {
  if (!dueAtUtc) return { text: 'No SLA', state: 'neutral' }
  const diff = new Date(dueAtUtc).getTime() - Date.now()
  const absoluteMinutes = Math.abs(Math.round(diff / 60000))
  const hours = Math.floor(absoluteMinutes / 60)
  const minutes = absoluteMinutes % 60
  const text = hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`
  return diff < 0
    ? { text: `${text} overdue`, state: 'danger' }
    : { text: `${text} remaining`, state: diff < 4 * 3600000 ? 'warning' : 'good' }
}
