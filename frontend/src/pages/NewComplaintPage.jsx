import { useEffect, useState } from 'react'
import { api } from '../api/client.js'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'

export default function NewComplaintPage({ navigate }) {
  const [categories, setCategories] = useState([])
  const [form, setForm] = useState({ categoryId: '', subject: '', description: '', customerReference: '', priority: 1 })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    api('/reference/categories')
      .then((items) => {
        setCategories(items)
        if (items.length) setForm((current) => ({ ...current, categoryId: items[0].id }))
      })
      .catch((err) => setError(err.message))
  }, [])

  const submit = async (e) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      const created = await api('/complaints', { method: 'POST', body: { ...form, priority: Number(form.priority) } })
      navigate(`/complaints/${created.id}`)
    } catch (err) {
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader eyebrow="NEW COMPLAINT" title="Log a complaint" description="The selected category determines which published workflow starts automatically." />
      <ErrorBanner message={error} />
      <form className="panel form-panel" onSubmit={submit}>
        <div className="form-grid two">
          <label className="field"><span>Related to</span><select className="input" value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })} required>{categories.map((x) => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
          <label className="field"><span>Priority</span><select className="input" value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value })}><option value="0">Low</option><option value="1">Normal</option><option value="2">High</option><option value="3">Critical</option></select></label>
        </div>
        <label className="field"><span>Customer / transaction reference</span><input className="input" value={form.customerReference} onChange={(e) => setForm({ ...form, customerReference: e.target.value })} placeholder="Optional customer ID, account reference or transaction ID" /></label>
        <label className="field"><span>Subject</span><input className="input" value={form.subject} onChange={(e) => setForm({ ...form, subject: e.target.value })} placeholder="Short description of the complaint" required maxLength={300} /></label>
        <label className="field"><span>Description</span><textarea className="input textarea" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="Add the complaint details, customer impact and relevant context…" required rows={8} /></label>
        <div className="form-actions"><button type="button" className="btn btn-secondary" onClick={() => navigate('/complaints')}>Cancel</button><button className="btn btn-primary" disabled={saving || !form.categoryId}>{saving ? 'Logging…' : 'Log complaint'}</button></div>
      </form>
    </>
  )
}
