import { useEffect, useState } from 'react'
import { api } from '../api/client.js'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'
import { PriorityBadge, StatusBadge } from '../components/Badge.jsx'
import { formatDate } from '../constants.js'

export default function ComplaintDetailPage({ id, navigate }) {
  const [complaint, setComplaint] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api(`/complaints/${id}`).then(setComplaint).catch((err) => setError(err.message))
  }, [id])

  if (!complaint) return <><PageHeader title="Complaint" /><ErrorBanner message={error} /><div className="loading-block">Loading complaint…</div></>

  return (
    <>
      <PageHeader
        eyebrow={complaint.complaintNumber}
        title={complaint.subject}
        description={complaint.category}
        actions={<button className="btn btn-secondary" onClick={() => navigate('/complaints')}>← Back to register</button>}
      />
      <ErrorBanner message={error} />
      <div className="complaint-summary panel">
        <div className="summary-item"><span>Status</span><StatusBadge status={complaint.status} /></div>
        <div className="summary-item"><span>Priority</span><PriorityBadge priority={complaint.priority} /></div>
        <div className="summary-item"><span>Created</span><strong>{formatDate(complaint.createdAtUtc)}</strong></div>
        <div className="summary-item"><span>Customer reference</span><strong>{complaint.customerReference || '—'}</strong></div>
      </div>
      <div className="two-column-grid detail-grid">
        <section className="panel">
          <div className="panel-header"><div><h2>Complaint details</h2><p>Information captured when the complaint was logged.</p></div></div>
          <div className="description-box">{complaint.description}</div>
        </section>
        <section className="panel">
          <div className="panel-header"><div><h2>Workflow timeline</h2><p>Audit history from creation through resolution.</p></div></div>
          <div className="timeline">
            {complaint.timeline.map((event, index) => (
              <div className="timeline-item" key={`${event.createdAtUtc}-${index}`}>
                <div className="timeline-dot" />
                <div className="timeline-content">
                  <div className="timeline-top"><strong>{event.message}</strong><span>{formatDate(event.createdAtUtc)}</span></div>
                  <div className="timeline-meta">{event.eventType}{event.actorName ? ` · ${event.actorName}` : ''}</div>
                </div>
              </div>
            ))}
          </div>
        </section>
      </div>
    </>
  )
}
