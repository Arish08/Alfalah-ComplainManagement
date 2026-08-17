import { useDevUser } from '../auth/DevUserContext.jsx'

const navItems = [
  { path: '/dashboard', label: 'Dashboard', icon: '▦' },
  { path: '/complaints', label: 'Complaints', icon: '◫' },
  { path: '/tasks', label: 'My Work Queue', icon: '✓' },
]

export default function Layout({ route, navigate, children }) {
  const { me, roles, devUsers, userId, switchUser, loading } = useDevUser()
  const items = roles.has('DeptAdmin')
    ? [...navItems, { path: '/workflows', label: 'Workflow Designer', icon: '⌘' }]
    : navItems

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">B</div>
          <div>
            <div className="brand-name">Bank Alfalah</div>
            <div className="brand-subtitle">Complaint Management</div>
          </div>
        </div>

        <nav className="sidebar-nav">
          {items.map((item) => (
            <button
              key={item.path}
              className={`nav-item ${route.startsWith(item.path) ? 'active' : ''}`}
              onClick={() => navigate(item.path)}
            >
              <span className="nav-icon">{item.icon}</span>
              <span>{item.label}</span>
            </button>
          ))}
        </nav>

      </aside>

      <section className="main-shell">
        <header className="topbar">
          <div>
            <div className="topbar-title">Complaint Management Unit</div>
           
          </div>
          <div className="user-switcher-wrap">
            <div className="user-meta">
              <strong>{loading ? 'Loading user…' : me?.displayName || 'Unknown user'}</strong>
              <span>{me?.memberships?.map((x) => x.roleCode).join(' · ') || 'No role'}</span>
            </div>
            <select
              className="user-switcher"
              value={userId}
              onChange={(e) => switchUser(e.target.value)}
              aria-label="Switch development user"
            >
              {devUsers.map((user) => (
                <option key={user.id} value={user.id}>{user.name}</option>
              ))}
            </select>
          </div>
        </header>

        <main className="content-area">{children}</main>
      </section>
    </div>
  )
}
