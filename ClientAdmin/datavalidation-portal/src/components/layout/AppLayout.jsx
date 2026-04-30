import { useState } from 'react'
import { Outlet, NavLink, useParams, useLocation } from 'react-router-dom'

const NAV_ITEMS = [
  { to: '/dashboard',      icon: '📊', label: 'Dashboard' },
  { to: '/organizations',  icon: '🏢', label: 'Organizations' },
]

function OrgSubNav({ orgId }) {
  return (
    <div className="sidebar-sub">
      <NavLink to={`/organizations/${orgId}`} end
        className={({isActive}) => `sidebar-link ${isActive ? 'active' : ''}`}>
        <span className="nav-icon">🏠</span><span className="nav-label">Overview</span>
      </NavLink>
      <NavLink to={`/organizations/${orgId}/customers`}
        className={({isActive}) => `sidebar-link ${isActive ? 'active' : ''}`}>
        <span className="nav-icon">👥</span><span className="nav-label">Customers</span>
      </NavLink>
      <NavLink to={`/organizations/${orgId}/inputs`}
        className={({isActive}) => `sidebar-link ${isActive ? 'active' : ''}`}>
        <span className="nav-icon">🗂️</span><span className="nav-label">Inputs</span>
      </NavLink>
      <NavLink to={`/organizations/${orgId}/import`}
        className={({isActive}) => `sidebar-link ${isActive ? 'active' : ''}`}>
        <span className="nav-icon">📥</span><span className="nav-label">Import</span>
      </NavLink>
      <NavLink to={`/organizations/${orgId}/import-staging`}
        className={({isActive}) => `sidebar-link ${isActive ? 'active' : ''}`}>
        <span className="nav-icon">🔧</span><span className="nav-label">Staging</span>
      </NavLink>
    </div>
  )
}

function usePageTitle() {
  const location = useLocation()
  const { pathname } = location
  if (pathname.includes('/import-staging'))  return 'Import Staging'
  if (pathname.includes('/import'))          return 'Import'
  if (pathname.includes('/customers'))       return 'Customers'
  if (pathname.includes('/inputs'))          return 'Inputs'
  if (/\/organizations\/[^/]+$/.test(pathname)) return 'Organization Overview'
  if (pathname.startsWith('/organizations')) return 'Organizations'
  if (pathname.startsWith('/dashboard'))     return 'Dashboard'
  return 'Admin Portal'
}

export default function AppLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const params   = useParams()
  const orgId    = params.organizationId
  const title    = usePageTitle()

  return (
    <div className="app-shell">
      {/* Sidebar */}
      <aside className={`sidebar ${collapsed ? 'collapsed' : ''}`}>
        <a className="sidebar-brand" href="/dashboard">
          {collapsed
            ? <img src="/pci-logo.svg" alt="PCI" className="brand-logo-icon" />
            : <img src="/pci-logo.svg" alt="PCI — not the big company" className="brand-logo-full" />
          }
        </a>

        <nav className="sidebar-nav">
          {!collapsed && <div className="nav-section-label">Menu</div>}
          {NAV_ITEMS.map(item => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
            >
              <span className="nav-icon">{item.icon}</span>
              {!collapsed && <span className="nav-label">{item.label}</span>}
            </NavLink>
          ))}

          {orgId && !collapsed && (
            <>
              <div className="nav-section-label mt-2">Organisation</div>
              <OrgSubNav orgId={orgId} />
            </>
          )}
        </nav>

        <button
          className="btn btn-sm text-secondary w-100 border-0 py-2"
          style={{ background: 'rgba(255,255,255,.05)', borderRadius: 0, color: 'rgba(255,255,255,.5)', fontSize: '.75rem' }}
          onClick={() => setCollapsed(c => !c)}
        >
          {collapsed ? '▶' : '◀ Collapse'}
        </button>
      </aside>

      {/* Main */}
      <div className="main-area">
        <header className="topbar">
          <span className="page-title">{title}</span>
        </header>
        <main className="page-body">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
