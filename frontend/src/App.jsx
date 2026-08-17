import { useEffect, useState } from 'react'
import Layout from './components/Layout.jsx'
import DashboardPage from './pages/DashboardPage.jsx'
import ComplaintsPage from './pages/ComplaintsPage.jsx'
import NewComplaintPage from './pages/NewComplaintPage.jsx'
import ComplaintDetailPage from './pages/ComplaintDetailPage.jsx'
import MyTasksPage from './pages/MyTasksPage.jsx'
import WorkflowsPage from './pages/WorkflowsPage.jsx'
import WorkflowDesignerPage from './pages/WorkflowDesignerPage.jsx'
import { useDevUser } from './auth/DevUserContext.jsx'
import ErrorBanner from './components/ErrorBanner.jsx'

function getRoute() {
  const hash = window.location.hash.replace(/^#/, '')
  return hash || '/dashboard'
}

export default function App() {
  const [route, setRoute] = useState(getRoute())
  const { error: authError } = useDevUser()

  useEffect(() => {
    if (!window.location.hash) window.location.hash = '#/dashboard'
    const handler = () => setRoute(getRoute())
    window.addEventListener('hashchange', handler)
    return () => window.removeEventListener('hashchange', handler)
  }, [])

  const navigate = (path) => {
    window.location.hash = `#${path}`
  }

  let page
  const complaintMatch = route.match(/^\/complaints\/([0-9a-f-]{36})$/i)
  const workflowDesignerMatch = route.match(/^\/workflows\/([0-9a-f-]{36})\/designer$/i)

  if (route === '/dashboard') page = <DashboardPage navigate={navigate} />
  else if (route === '/complaints') page = <ComplaintsPage navigate={navigate} />
  else if (route === '/complaints/new') page = <NewComplaintPage navigate={navigate} />
  else if (complaintMatch) page = <ComplaintDetailPage id={complaintMatch[1]} navigate={navigate} />
  else if (route === '/tasks') page = <MyTasksPage navigate={navigate} />
  else if (route === '/workflows') page = <WorkflowsPage navigate={navigate} />
  else if (route === '/workflows/new') page = <WorkflowDesignerPage isNew navigate={navigate} />
  else if (workflowDesignerMatch) page = <WorkflowDesignerPage workflowId={workflowDesignerMatch[1]} navigate={navigate} />
  else page = <DashboardPage navigate={navigate} />

  return (
    <Layout route={route} navigate={navigate}>
      <ErrorBanner message={authError} />
      {page}
    </Layout>
  )
}
