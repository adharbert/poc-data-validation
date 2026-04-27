import { Link } from 'react-router-dom'
import { useDashboardStats, useExpiringProjects } from '@/hooks/useApi.js'
import { LoadingState, ErrorAlert } from '@/components/common/index.jsx'

function StatCard({ icon, label, value, bg = '#eff6ff', iconBg = '#dbeafe', iconColor = '#1e40af' }) {
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
      <div className="d-flex justify-content-between mb-1" style={{ fontSize: '.75rem', color: '#6b7280' }}>
        <span>{verified}/{total} verified</span>
        <span>{pct}%</span>
      </div>
      <div className="progress" style={{ height: '6px' }}>
        <div
          className="progress-bar bg-success"
          style={{ width: `${pct}%` }}
          role="progressbar"
          aria-valuenow={pct}
          aria-valuemin={0}
          aria-valuemax={100}
        />
      </div>
    </div>
  )
}

export default function DashboardPage() {
  const { data: stats, isLoading: statsLoading, isError: statsError } = useDashboardStats()
  const { data: expiring, isLoading: expiringLoading }               = useExpiringProjects()

  if (statsLoading) return <LoadingState message="Loading dashboard…" />
  if (statsError)   return <ErrorAlert message="Could not load dashboard stats. Is the API running?" />

  return (
    <div>
      <div className="page-header">
        <div className="page-header-title">
          <h1>Dashboard</h1>
          <p>Summary across all active organisations</p>
        </div>
      </div>

      {/* Top stat cards */}
      <div className="row g-3 mb-4">
        <div className="col-6 col-md-3">
          <StatCard icon="🏢" label="Active Organisations" value={stats?.totalActiveOrganisations}
            iconBg="#dbeafe" iconColor="#1e40af" />
        </div>
        <div className="col-6 col-md-3">
          <StatCard icon="👥" label="Total Customers" value={stats?.totalCustomers?.toLocaleString()}
            iconBg="#d1fae5" iconColor="#065f46" />
        </div>
        <div className="col-6 col-md-3">
          <StatCard icon="✅" label="Verified Customers" value={stats?.totalVerifiedCustomers?.toLocaleString()}
            iconBg="#ede9fe" iconColor="#5b21b6" />
        </div>
        <div className="col-6 col-md-3">
          <StatCard icon="⚠️" label="Expiring Projects" value={stats?.projectsApproachingEndDate}
            iconBg="#fef3c7" iconColor="#92400e" />
        </div>
      </div>

      <div className="row g-3">
        {/* Organisation summaries */}
        <div className="col-12 col-lg-7">
          <div className="admin-card">
            <h2 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '1rem' }}>Organisation Summaries</h2>
            {!stats?.organisationSummaries?.length ? (
              <p className="text-muted-sm">No organisations found.</p>
            ) : (
              <div className="table-wrap">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Organisation</th>
                      <th>Customers</th>
                      <th>Verification</th>
                      <th>Projects</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stats.organisationSummaries.map(org => (
                      <tr key={org.organisationId}>
                        <td>
                          <Link
                            to={`/organizations/${org.organisationId}/customers`}
                            className="fw-semibold text-decoration-none"
                            style={{ color: '#1a56db' }}
                          >
                            {org.organisationName}
                          </Link>
                        </td>
                        <td>{org.totalCustomers?.toLocaleString()}</td>
                        <td style={{ minWidth: '140px' }}>
                          <VerificationBar verified={org.verifiedCustomers} total={org.totalCustomers} />
                        </td>
                        <td>{org.activeProjects}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
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
                  <div className="text-muted-sm">{p.organisationName}</div>
                </div>
                <div className="text-end flex-shrink-0 ms-3">
                  <div style={{ fontSize: '.8rem', color: p.daysRemaining <= 7 ? '#b91c1c' : '#92400e', fontWeight: 600 }}>
                    {p.daysRemaining}d left
                  </div>
                  <div className="text-muted-sm">{p.marketingEndDate}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
