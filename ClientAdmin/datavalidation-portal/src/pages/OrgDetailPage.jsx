import { useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import {
  useOrganization, useContracts, useProjects,
  useDashboardStats, useCustomers,
} from '@/hooks/useApi.js'
import { PageHeader, LoadingState, ErrorAlert } from '@/components/common/index.jsx'
import { fmtDate, fmtPhone } from '@/utils/dates.js'

// ─── Helpers ─────────────────────────────────────────────────────────────────

function fmt(dateStr) {
  return fmtDate(dateStr)
}

function daysUntil(dateStr) {
  if (!dateStr) return null
  const diff = Math.ceil((new Date(dateStr) - Date.now()) / 86_400_000)
  return diff
}

function urgencyColor(days) {
  if (days == null) return '#6b7280'
  if (days <= 7)  return '#b91c1c'
  if (days <= 30) return '#d97706'
  return '#059669'
}

function projectProgress(start, end) {
  if (!start || !end) return 0
  const s = new Date(start).getTime()
  const e = new Date(end).getTime()
  const n = Date.now()
  if (n <= s) return 0
  if (n >= e) return 100
  return Math.round(((n - s) / (e - s)) * 100)
}

// ─── Stat card ────────────────────────────────────────────────────────────────
function StatCard({ icon, label, value, sub, iconBg, iconColor }) {
  return (
    <div className="org-stat-card">
      <div className="org-stat-icon" style={{ background: iconBg }}>
        <span style={{ color: iconColor, fontSize: '1.25rem' }}>{icon}</span>
      </div>
      <div className="org-stat-body">
        <div className="org-stat-label">{label}</div>
        <div className="org-stat-value">{value ?? '—'}</div>
        {sub && <div className="org-stat-sub">{sub}</div>}
      </div>
    </div>
  )
}

// ─── Progress bar ─────────────────────────────────────────────────────────────
function ProgressBar({ value, max, color = '#1a56db', height = 8 }) {
  const pct = max > 0 ? Math.min(100, Math.round((value / max) * 100)) : 0
  return (
    <div>
      <div className="d-flex justify-content-between mb-1" style={{ fontSize: '.75rem', color: '#6b7280' }}>
        <span>{value?.toLocaleString()} / {max?.toLocaleString()}</span>
        <span className="fw-semibold">{pct}%</span>
      </div>
      <div className="progress" style={{ height }}>
        <div className="progress-bar" style={{ width: `${pct}%`, background: color }} role="progressbar"
          aria-valuenow={pct} aria-valuemin={0} aria-valuemax={100} />
      </div>
    </div>
  )
}

// ─── Nav tile ─────────────────────────────────────────────────────────────────
function NavTile({ to, icon, label, desc }) {
  return (
    <Link to={to} className="org-nav-tile text-decoration-none">
      <span className="org-nav-tile-icon">{icon}</span>
      <span className="org-nav-tile-label">{label}</span>
      <span className="org-nav-tile-desc">{desc}</span>
    </Link>
  )
}

// ─── Contracts section ────────────────────────────────────────────────────────
function ContractsSection({ orgId }) {
  const { data: contracts = [], isLoading } = useContracts(orgId)
  const active = contracts.filter(c => c.isActive)

  if (isLoading) return <LoadingState message="Loading contracts…" />

  return (
    <div className="admin-card h-100">
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h2 className="org-section-title">Contracts</h2>
        <span className="badge bg-primary-subtle text-primary-emphasis">{active.length} active</span>
      </div>

      {!contracts.length ? (
        <p className="text-muted-sm">No contracts on record.</p>
      ) : (
        <div className="org-timeline-list">
          {contracts.slice(0, 6).map(c => {
            const days = daysUntil(c.endDate)
            return (
              <div key={c.contractId} className="org-timeline-item">
                <div className="d-flex justify-content-between align-items-start">
                  <div>
                    <div className="fw-semibold" style={{ fontSize: '.875rem' }}>{c.contractName}</div>
                    {c.contractNumber && <code className="text-muted-sm">{c.contractNumber}</code>}
                  </div>
                  {!c.isActive && <span className="badge bg-secondary-subtle text-secondary-emphasis ms-2">Inactive</span>}
                </div>
                <div className="d-flex gap-3 mt-1" style={{ fontSize: '.78rem', color: '#6b7280' }}>
                  <span>Start: <strong>{fmt(c.startDate)}</strong></span>
                  <span>End: <strong style={{ color: days != null ? urgencyColor(days) : undefined }}>
                    {c.endDate ? fmt(c.endDate) : 'Open-ended'}
                  </strong></span>
                  {days != null && days <= 60 && (
                    <span style={{ color: urgencyColor(days), fontWeight: 600 }}>
                      {days < 0 ? `${Math.abs(days)}d overdue` : `${days}d left`}
                    </span>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

// ─── Projects section ─────────────────────────────────────────────────────────
function ProjectsSection({ orgId }) {
  const { data: projects = [], isLoading } = useProjects(orgId)
  const active = projects.filter(p => p.isActive)

  if (isLoading) return <LoadingState message="Loading projects…" />

  return (
    <div className="admin-card h-100">
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h2 className="org-section-title">Marketing Projects</h2>
        <span className="badge bg-success-subtle text-success-emphasis">{active.length} active</span>
      </div>

      {!projects.length ? (
        <p className="text-muted-sm">No marketing projects on record.</p>
      ) : (
        <div className="org-timeline-list">
          {projects.slice(0, 6).map(p => {
            const pct  = projectProgress(p.marketingStartDate, p.marketingEndDate)
            const days = daysUntil(p.marketingEndDate)
            return (
              <div key={p.projectId} className="org-timeline-item">
                <div className="d-flex justify-content-between align-items-start">
                  <div className="fw-semibold" style={{ fontSize: '.875rem' }}>{p.projectName}</div>
                  {!p.isActive && <span className="badge bg-secondary-subtle text-secondary-emphasis ms-2">Inactive</span>}
                </div>
                <div className="d-flex gap-3 mt-1" style={{ fontSize: '.78rem', color: '#6b7280' }}>
                  <span>{fmt(p.marketingStartDate)} → {p.marketingEndDate ? fmt(p.marketingEndDate) : 'Ongoing'}</span>
                  {days != null && days <= 30 && (
                    <span style={{ color: urgencyColor(days), fontWeight: 600 }}>
                      {days < 0 ? `${Math.abs(days)}d overdue` : `${days}d left`}
                    </span>
                  )}
                </div>
                {p.marketingEndDate && (
                  <div className="progress mt-2" style={{ height: 4 }}>
                    <div className="progress-bar bg-success" style={{ width: `${pct}%` }} role="progressbar" />
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────
export default function OrgDetailPage() {
  const { organizationId } = useParams()
  const navigate = useNavigate()

  const { data: org,      isLoading: orgLoading,   isError } = useOrganization(organizationId)
  const { data: stats }                                       = useDashboardStats()
  const { data: customers }                                   = useCustomers(organizationId, 1, 1)

  const orgSummary = stats?.organisationSummaries?.find(
    s => s.organisationId?.toLowerCase() === organizationId?.toLowerCase()
  )

  const totalCustomers    = customers?.totalCount ?? orgSummary?.totalCustomers ?? 0
  const verifiedCustomers = orgSummary?.verifiedCustomers ?? 0
  const activeProjects    = orgSummary?.activeProjects ?? 0

  if (orgLoading) return <LoadingState message="Loading organisation…" />
  if (isError || !org) return <ErrorAlert message="Could not load organisation." />

  return (
    <div>
      <PageHeader
        breadcrumbs={[{ label: 'Organizations', href: '/organizations' }, { label: org.organizationName }]}
        title={org.organizationName}
        subtitle={
          <span className="d-flex flex-wrap align-items-center gap-3" style={{ fontSize: '.875rem', color: '#6b7280' }}>
            <code>{org.organizationCode}</code>
            {org.phone      && <span>📞 {fmtPhone(org.phone)}</span>}
            {org.companyEmail && <span>✉️ {org.companyEmail}</span>}
            {org.website    && <a href={org.website} target="_blank" rel="noopener noreferrer" className="text-decoration-none" style={{ color: '#1a56db' }}>🌐 Website</a>}
            {org.marketingName && <span>aka {org.marketingName}</span>}
          </span>
        }
        actions={
          <Link to={`/organizations/${organizationId}/customers`} className="btn btn-primary btn-sm">
            Open Full View
          </Link>
        }
      />

      {/* Stat cards */}
      <div className="row g-3 mb-4">
        <div className="col-6 col-md-3">
          <StatCard icon="👥" label="Total Customers" value={totalCustomers?.toLocaleString()}
            iconBg="#dbeafe" iconColor="#1e40af" />
        </div>
        <div className="col-6 col-md-3">
          <StatCard icon="✅" label="Verified"
            value={verifiedCustomers?.toLocaleString()}
            sub={totalCustomers > 0 ? `${Math.round((verifiedCustomers / totalCustomers) * 100)}% complete` : null}
            iconBg="#d1fae5" iconColor="#065f46" />
        </div>
        <div className="col-6 col-md-3">
          <StatCard icon="📁" label="Active Projects" value={activeProjects}
            iconBg="#fef3c7" iconColor="#92400e" />
        </div>
        <div className="col-6 col-md-3">
          <StatCard icon={org.isActive ? '✅' : '🚫'} label="Status"
            value={org.isActive ? 'Active' : 'Inactive'}
            iconBg={org.isActive ? '#d1fae5' : '#fee2e2'}
            iconColor={org.isActive ? '#065f46' : '#b91c1c'} />
        </div>
      </div>

      {/* Validation progress */}
      {totalCustomers > 0 && (
        <div className="admin-card mb-4">
          <div className="d-flex justify-content-between align-items-center mb-2">
            <h2 className="org-section-title mb-0">Validation Progress</h2>
            <span style={{ fontSize: '.8rem', color: '#6b7280' }}>
              {verifiedCustomers?.toLocaleString()} of {totalCustomers?.toLocaleString()} customers verified
            </span>
          </div>
          <ProgressBar value={verifiedCustomers} max={totalCustomers} color="#059669" height={14} />
          <div className="d-flex gap-4 mt-3">
            <div className="d-flex align-items-center gap-2">
              <div style={{ width: 12, height: 12, borderRadius: '50%', background: '#059669' }} />
              <span style={{ fontSize: '.8rem', color: '#6b7280' }}>Verified ({verifiedCustomers?.toLocaleString()})</span>
            </div>
            <div className="d-flex align-items-center gap-2">
              <div style={{ width: 12, height: 12, borderRadius: '50%', background: '#e5e7eb' }} />
              <span style={{ fontSize: '.8rem', color: '#6b7280' }}>Pending ({(totalCustomers - verifiedCustomers)?.toLocaleString()})</span>
            </div>
          </div>
        </div>
      )}

      {/* Contracts + Projects */}
      <div className="row g-3 mb-4">
        <div className="col-12 col-lg-6">
          <ContractsSection orgId={organizationId} />
        </div>
        <div className="col-12 col-lg-6">
          <ProjectsSection orgId={organizationId} />
        </div>
      </div>

      {/* Navigation tiles */}
      <div className="admin-card mb-0">
        <h2 className="org-section-title mb-3">Manage Organisation</h2>
        <div className="org-nav-tiles">
          <NavTile
            to={`/organizations/${organizationId}/customers`}
            icon="👥" label="Customers"
            desc="Search, view, and validate customer records"
          />
          <NavTile
            to={`/organizations/${organizationId}/inputs`}
            icon="🗂️" label="Inputs"
            desc="Configure sections and field definitions"
          />
          <NavTile
            to={`/organizations/${organizationId}/import`}
            icon="📥" label="Import"
            desc="Upload CSV or Excel files to bulk-import customers"
          />
          <NavTile
            to={`/organizations/${organizationId}/import-staging`}
            icon="🔧" label="Staging"
            desc="Review and resolve unmapped import columns"
          />
        </div>
      </div>
    </div>
  )
}
