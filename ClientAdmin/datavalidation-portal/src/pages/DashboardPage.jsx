import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useDashboardStats, useExpiringProjects } from '@/hooks/useApi.js'
import { LoadingState, ErrorAlert } from '@/components/common/index.jsx'
import { fmtDate } from '@/utils/dates.js'

function StatCard({ icon, label, value, iconBg, iconColor }) {
  return (
    <div className="stat-card">
      <div className="stat-icon" style={{ background: iconBg }}>
        <span style={{ color: iconColor }}>{icon}</span>
      </div>
      <div>
        <div className="stat-label">{label}</div>
        <div className="stat-value">{value ?? '—'}</div>
      </div>
    </div>
  )
}

function VerificationBar({ verified, total }) {
  const pct = total > 0 ? Math.round((verified / total) * 100) : 0
  return (
    <div>
      <div className="d-flex justify-content-between mb-1" style={{ fontSize: '.72rem', color: '#6b7280' }}>
        <span>{verified?.toLocaleString()}/{total?.toLocaleString()} verified</span>
        <span className="fw-semibold">{pct}%</span>
      </div>
      <div className="progress" style={{ height: '5px' }}>
        <div className="progress-bar bg-success" style={{ width: `${pct}%` }} role="progressbar"
          aria-valuenow={pct} aria-valuemin={0} aria-valuemax={100} />
      </div>
    </div>
  )
}

function urgencyColor(days) {
  if (days <= 7)  return '#b91c1c'
  if (days <= 30) return '#d97706'
  return '#059669'
}

export default function DashboardPage() {
  const [orgSearch, setOrgSearch] = useState('')

  const { data: stats,    isLoading: statsLoading, isError } = useDashboardStats()
  const { data: expiring, isLoading: expiringLoading }       = useExpiringProjects()

  if (statsLoading) return <LoadingState message="Loading dashboard…" />
  if (isError)      return <ErrorAlert message="Could not load dashboard stats. Is the API running?" />

  const totalCustomers   = stats?.organisationSummaries?.reduce((s, o) => s + (o.totalCustomers    ?? 0), 0) ?? 0
  const totalVerified    = stats?.organisationSummaries?.reduce((s, o) => s + (o.verifiedCustomers ?? 0), 0) ?? 0
  const overallPct       = totalCustomers > 0 ? Math.round((totalVerified / totalCustomers) * 100) : 0

  const q = orgSearch.trim().toLowerCase()
  const visibleOrgs = q
    ? (stats?.organisationSummaries ?? []).filter(o =>
        o.organisationName?.toLowerCase().includes(q)
      )
    : (stats?.organisationSummaries ?? [])

  return (
    <div>
      <div className="page-header">
        <div className="page-header-title">
          <h1>Dashboard</h1>
          <p>Global summary across all organisations</p>
        </div>
      </div>

      {/* Top stat cards — global metrics only */}
      <div className="row g-3 mb-4">
        <div className="col-6 col-md-3">
          <StatCard icon="🏢" label="Active Organisations" value={stats?.totalActiveOrganizations}
            iconBg="#dbeafe" iconColor="#1e40af" />
        </div>
        <div className="col-6 col-md-3">
          <StatCard icon="⚠️" label="Expiring Projects" value={expiring?.length ?? '—'}
            iconBg="#fef3c7" iconColor="#92400e" />
        </div>
        <div className="col-6 col-md-3">
          <StatCard icon="👥" label="Total Customers" value={totalCustomers?.toLocaleString()}
            iconBg="#f0fdf4" iconColor="#166534" />
        </div>
        <div className="col-6 col-md-3">
          <StatCard icon="✅" label="Overall Verified" value={`${overallPct}%`}
            iconBg="#ede9fe" iconColor="#5b21b6" />
        </div>
      </div>

      <div className="row g-3">
        {/* Organisation comparison */}
        <div className="col-12 col-lg-7">
          <div className="admin-card">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h2 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: 0 }}>
                Organization Overview
              </h2>
              <input
                type="search"
                className="form-control form-control-sm"
                style={{ width: 200 }}
                placeholder="Search…"
                value={orgSearch}
                onChange={e => setOrgSearch(e.target.value)}
              />
            </div>
            {!stats?.organisationSummaries?.length ? (
              <p className="text-muted-sm">No organisations found.</p>
            ) : !visibleOrgs.length ? (
              <p className="text-muted-sm">No organisations match "{orgSearch}".</p>
            ) : (
              <div className="table-wrap">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Organization</th>
                      <th>Customers</th>
                      <th style={{ minWidth: 160 }}>Verification</th>
                      <th>Projects</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visibleOrgs.map(org => (
                      <tr key={org.organisationId}>
                        <td>
                          <Link
                            to={`/organizations/${org.organisationId}`}
                            className="fw-semibold text-decoration-none"
                            style={{ color: '#1a56db' }}
                          >
                            {org.organisationName}
                          </Link>
                        </td>
                        <td>{org.totalCustomers?.toLocaleString()}</td>
                        <td>
                          <VerificationBar verified={org.verifiedCustomers} total={org.totalCustomers} />
                        </td>
                        <td>{org.activeProjects}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {/* Bar chart: customers per org */}
            {visibleOrgs.length > 1 && (() => {
              const maxC = Math.max(...visibleOrgs.map(o => o.totalCustomers ?? 0))
              return maxC > 0 ? (
                <div className="mt-4">
                  <div style={{ fontSize: '.8rem', fontWeight: 600, color: '#374151', marginBottom: '.5rem' }}>
                    Customer Distribution
                  </div>
                  {visibleOrgs.map(org => {
                    const pct    = maxC > 0 ? Math.round(((org.totalCustomers ?? 0) / maxC) * 100) : 0
                    const verPct = org.totalCustomers > 0 ? Math.round(((org.verifiedCustomers ?? 0) / org.totalCustomers) * 100) : 0
                    return (
                      <div key={org.organisationId} className="mb-2">
                        <div className="d-flex justify-content-between mb-1" style={{ fontSize: '.75rem' }}>
                          <span className="text-truncate" style={{ maxWidth: '50%' }}>{org.organisationName}</span>
                          <span className="text-muted-sm">{org.totalCustomers?.toLocaleString()} · {verPct}% verified</span>
                        </div>
                        <div className="position-relative" style={{ height: 8, background: '#e5e7eb', borderRadius: 4 }}>
                          <div style={{ width: `${pct}%`, height: '100%', background: '#e5e7eb', borderRadius: 4, position: 'absolute' }} />
                          <div style={{ width: `${pct * verPct / 100}%`, height: '100%', background: '#059669', borderRadius: 4, position: 'absolute' }} />
                          <div style={{ width: `${pct}%`, height: '100%', background: 'rgba(59,130,246,.25)', borderRadius: 4, position: 'absolute' }} />
                        </div>
                      </div>
                    )
                  })}
                  <div className="d-flex gap-3 mt-2">
                    <div className="d-flex align-items-center gap-1">
                      <div style={{ width: 10, height: 10, borderRadius: 2, background: 'rgba(59,130,246,.25)' }} />
                      <span style={{ fontSize: '.72rem', color: '#6b7280' }}>Total customers</span>
                    </div>
                    <div className="d-flex align-items-center gap-1">
                      <div style={{ width: 10, height: 10, borderRadius: 2, background: '#059669' }} />
                      <span style={{ fontSize: '.72rem', color: '#6b7280' }}>Verified</span>
                    </div>
                  </div>
                </div>
              ) : null
            })()}
          </div>
        </div>

        {/* Expiring projects */}
        <div className="col-12 col-lg-5">
          <div className="admin-card">
            <h2 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '1rem' }}>
              ⚠️ Projects Approaching End Date
            </h2>
            {expiringLoading && <LoadingState message="Loading…" />}
            {!expiringLoading && !expiring?.length && (
              <p className="text-muted-sm">No projects expiring soon.</p>
            )}
            {expiring?.map(p => (
              <div key={p.projectId} className="d-flex justify-content-between align-items-start py-2 border-bottom">
                <div>
                  <div className="fw-semibold" style={{ fontSize: '.875rem' }}>{p.projectName}</div>
                  <Link
                    to={`/organizations/${p.organisationId}`}
                    className="text-decoration-none"
                    style={{ fontSize: '.78rem', color: '#1a56db' }}
                  >
                    {p.organisationName}
                  </Link>
                </div>
                <div className="text-end flex-shrink-0 ms-3">
                  <div style={{ fontSize: '.8rem', color: urgencyColor(p.daysUntilExpiry), fontWeight: 600 }}>
                    {p.daysUntilExpiry}d left
                  </div>
                  <div className="text-muted-sm">{fmtDate(p.marketingEndDate)}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
