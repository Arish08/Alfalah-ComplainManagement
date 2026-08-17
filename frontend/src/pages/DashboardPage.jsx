import { useEffect, useState } from 'react'
import { api } from '../api/client.js'
import { useDevUser } from '../auth/DevUserContext.jsx'
import PageHeader from '../components/PageHeader.jsx'
import StatCard from '../components/StatCard.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'
import { PriorityBadge, StatusBadge } from '../components/Badge.jsx'
import { formatDate, relativeSla } from '../constants.js'

export default function DashboardPage({ navigate }) {
  const { userId, me } = useDevUser()
  const [complaints, setComplaints] = useState([])
  const [tasks, setTasks] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true
    setLoading(true)
    setError('')
    Promise.all([api('/complaints'), api('/workflow-tasks/my-bucket')])
      .then(([complaintData, taskData]) => {
        if (!active) return
        setComplaints(complaintData)
        setTasks(taskData)
      })
      .catch((err) => active && setError(err.message))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [userId])

  const openCount = complaints.filter((x) => x.status === 0 || x.status === 1).length
  const resolvedCount = complaints.filter((x) => x.status === 2).length
  const overdueCount = tasks.filter((x) => x.dueAtUtc && new Date(x.dueAtUtc) < new Date()).length

  return (
    <>
      <PageHeader
        eyebrow="OPERATIONS"
        title={`Welcome, ${me?.displayName || 'User'}`}
      
        actions={<button className="btn btn-primary" onClick={() => navigate('/complaints/new')}>+ Log complaint</button>}
      />
      <ErrorBanner message={error} />

      <div className="stats-grid">
        <StatCard label="Open complaints" value={loading ? '—' : openCount} helper="Across the complaint unit" />
        <StatCard label="My work queue" value={loading ? '—' : tasks.length} helper="Tasks you can action now" tone="red" />
        <StatCard label="Overdue SLA" value={loading ? '—' : overdueCount} helper="Needs immediate attention" tone={overdueCount ? 'danger' : 'default'} />
        <StatCard label="Resolved" value={loading ? '—' : resolvedCount} helper="Completed complaints" />
      </div>

      <div className="two-column-grid dashboard-grid">
        <section className="panel">
          <div className="panel-header">
            <div>
              <h2>My priority queue</h2>
              <p>Ordered by SLA deadline.</p>
            </div>
            <button className="btn btn-secondary btn-small" onClick={() => navigate('/tasks')}>View queue</button>
          </div>
          <div className="stack-list">
            {tasks.slice(0, 5).map((task) => {
              const sla = relativeSla(task.dueAtUtc)
              return (
                <button key={task.id} className="queue-row" onClick={() => navigate('/tasks')}>
                  <div className="queue-main">
                    <strong>{task.complaintNumber}</strong>
                    <span>{task.complaintSubject}</span>
                  </div>
                  <div className="queue-meta">
                    <span>{task.nodeName}</span>
                    <span className={`sla-text sla-${sla.state}`}>{sla.text}</span>
                  </div>
                </button>
              )
            })}
            {!loading && tasks.length === 0 && <div className="inline-empty">Your work queue is clear.</div>}
          </div>
        </section>

        <section className="panel">
          <div className="panel-header">
            <div>
              <h2>Recent complaints</h2>
              <p>Newest complaints logged in the platform.</p>
            </div>
            <button className="btn btn-secondary btn-small" onClick={() => navigate('/complaints')}>View all</button>
          </div>
          <div className="stack-list">
            {complaints.slice(0, 5).map((complaint) => (
              <button key={complaint.id} className="queue-row" onClick={() => navigate(`/complaints/${complaint.id}`)}>
                <div className="queue-main">
                  <strong>{complaint.complaintNumber}</strong>
                  <span>{complaint.subject}</span>
                </div>
                <div className="queue-badges">
                  <PriorityBadge priority={complaint.priority} />
                  <StatusBadge status={complaint.status} />
                </div>
                <span className="row-date">{formatDate(complaint.createdAtUtc)}</span>
              </button>
            ))}
            {!loading && complaints.length === 0 && <div className="inline-empty">No complaints have been logged yet.</div>}
          </div>
        </section>
      </div>
    </>
  )
}
