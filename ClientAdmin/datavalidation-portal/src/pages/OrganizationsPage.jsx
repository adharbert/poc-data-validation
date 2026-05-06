import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import {
  useOrganizations, useCreateOrganization,
  useUpdateOrganization, useSetOrganizationStatus,
  useReprovisionOrganization, useMigrateIsolatedDatabases,
} from '@/hooks/useApi.js'
import {
  PageHeader, LoadingState, ErrorAlert,
  StatusBadge, ConfirmModal, EmptyState, useToast,
} from '@/components/common/index.jsx'
import { fmtDate, fmtPhone, formatPhoneInput } from '@/utils/dates.js'

function ProvisioningBadge({ status, required }) {
  if (!required)
    return <span className="badge bg-secondary-subtle text-secondary-emphasis">Shared</span>

  const map = {
    ready:        ['bg-success-subtle text-success-emphasis', 'Isolated · Ready'],
    provisioning: ['bg-primary-subtle text-primary-emphasis', 'Isolated · Provisioning…'],
    pending:      ['bg-warning-subtle text-warning-emphasis', 'Isolated · Pending'],
    failed:       ['bg-danger-subtle text-danger-emphasis',   'Isolated · Failed'],
  }
  const [cls, label] = map[status] ?? ['bg-secondary-subtle text-secondary-emphasis', 'Isolated']
  return <span className={`badge ${cls}`}>{label}</span>
}

function MigrateResultsPanel({ results, onClose }) {
  const succeeded = results.filter(r => r.success)
  const failed    = results.filter(r => !r.success)
  return (
    <div className={`alert ${failed.length ? 'alert-warning' : 'alert-success'} mb-3`}>
      <div className="d-flex justify-content-between align-items-start">
        <div>
          <strong>Migration complete</strong> — {succeeded.length} succeeded
          {failed.length > 0 && `, ${failed.length} failed`}
          {failed.length > 0 && (
            <ul className="mb-0 mt-2" style={{ fontSize: '.85rem' }}>
              {failed.map(r => (
                <li key={r.organizationId}>
                  <strong>{r.organizationName}</strong>: {r.error}
                </li>
              ))}
            </ul>
          )}
        </div>
        <button type="button" className="btn-close" onClick={onClose} />
      </div>
    </div>
  )
}

const DB_STATUS_UI = {
  pending:      { locked: false, cls: 'text-muted',   msg: 'Provisioning is queued and will start shortly.' },
  provisioning: { locked: true,  cls: 'text-primary',  msg: 'Database is being provisioned — please wait.' },
  ready:        { locked: true,  cls: 'text-success',  msg: 'Database is active. Routing is enabled for this organization.' },
  failed:       { locked: false, cls: 'text-danger',   msg: 'Last provisioning attempt failed. Save to retry.' },
}

function IsolatedDatabaseField({ register, org }) {
  const status   = org?.databaseProvisioningStatus ?? null
  const ui       = DB_STATUS_UI[status] ?? null
  const locked   = ui?.locked ?? false

  return (
    <div className="form-check">
      <input className="form-check-input" type="checkbox" id="requiresIsolatedDatabase"
        {...register('requiresIsolatedDatabase')} disabled={locked} />
      <label className="form-check-label fw-semibold" htmlFor="requiresIsolatedDatabase">
        Requires Isolated Database
      </label>
      {!ui && (
        <div className="form-text">Provisions a dedicated SQL database for this organization.</div>
      )}
      {ui && (
        <div className={`form-text ${ui.cls}`}>{ui.msg}</div>
      )}
    </div>
  )
}

function OrgModal({ org, onClose }) {
  const toast   = useToast()
  const create  = useCreateOrganization()
  const update  = useUpdateOrganization()
  const isEdit  = !!org

  const { register, handleSubmit, setValue, formState: { errors } } = useForm({
    defaultValues: {
      organizationName: org?.organizationName ?? '',
      abbreviation:     org?.abbreviation     ?? '',
      marketingName:    org?.marketingName    ?? '',
      filingName:       org?.filingName       ?? '',
      website:          org?.website          ?? '',
      phone:            fmtPhone(org?.phone) || '',
      companyEmail:              org?.companyEmail              ?? '',
      isActive:                  org?.isActive                  ?? true,
      requiresIsolatedDatabase:  org?.requiresIsolatedDatabase  ?? false,
    },
  })

  async function onSubmit(values) {
    try {
      if (isEdit) {
        await update.mutateAsync({ id: org.organizationId, data: values })
        toast('Organization updated.')
      } else {
        await create.mutateAsync(values)
        toast('Organization created.')
      }
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving Organization.', 'danger')
    }
  }

  const saving = create.isPending || update.isPending

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">{isEdit ? 'Edit Organization' : 'New Organization'}</h5>
              <button type="button" className="btn-close" onClick={onClose} />
            </div>
            <div className="modal-body">
              <div className="row g-3">
                <div className="col-8">
                  <label className="form-label fw-semibold">Organization Name <span className="text-danger">*</span></label>
                  <input className={`form-control ${errors.organizationName ? 'is-invalid' : ''}`}
                    {...register('organizationName', { required: 'Required' })} />
                  {errors.organizationName && <div className="invalid-feedback">{errors.organizationName.message}</div>}
                </div>
                <div className="col-4">
                  <label className="form-label fw-semibold">Abbreviation <span className="text-danger">*</span></label>
                  <input className={`form-control ${errors.abbreviation ? 'is-invalid' : ''}`}
                    style={{ textTransform: 'uppercase' }}
                    {...register('abbreviation', {
                      required: 'Required',
                      maxLength: { value: 4, message: 'Max 4 characters' },
                      setValueAs: v => v?.toUpperCase(),
                    })} />
                  {errors.abbreviation && <div className="invalid-feedback">{errors.abbreviation.message}</div>}
                </div>
                <div className="col-6">
                  <label className="form-label">Marketing Name</label>
                  <input className="form-control" {...register('marketingName')} />
                </div>
                <div className="col-6">
                  <label className="form-label">Filing Name</label>
                  <input className="form-control" {...register('filingName')} />
                </div>
                <div className="col-6">
                  <label className="form-label">Phone</label>
                  <input className="form-control" {...register('phone')}
                    onChange={e => { e.target.value = formatPhoneInput(e.target.value); setValue('phone', e.target.value) }} />
                </div>
                <div className="col-6">
                  <label className="form-label">Company Email</label>
                  <input className="form-control" type="email" {...register('companyEmail')} />
                </div>
                <div className="col-12">
                  <label className="form-label">Website</label>
                  <input className="form-control" {...register('website')} />
                </div>
                {isEdit && (
                  <div className="col-12">
                    <div className="form-check form-switch">
                      <input className="form-check-input" type="checkbox" role="switch"
                        id="isActive" {...register('isActive')} />
                      <label className="form-check-label fw-semibold" htmlFor="isActive">Active</label>
                      <div className="form-text">Inactive organisations are hidden from public access.</div>
                    </div>
                  </div>
                )}
                <div className="col-12">
                  <IsolatedDatabaseField register={register} org={isEdit ? org : null} />
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

export default function OrganizationsPage() {
  const [showInactive, setShowInactive] = useState(false)
  const [editOrg,  setEditOrg]   = useState(null)
  const [showCreate, setShowCreate] = useState(false)
  const [confirmId, setConfirmId]  = useState(null)
  const [targetStatus, setTargetStatus] = useState(null)
  const [searchInput, setSearchInput]   = useState('')
  const [search, setSearch]             = useState(null)
  const [migrateResults, setMigrateResults] = useState(null)

  useEffect(() => {
    const t = setTimeout(() => setSearch(searchInput.trim() || null), 350)
    return () => clearTimeout(t)
  }, [searchInput])

  const { data: orgs, isLoading, isError } = useOrganizations(showInactive, search)
  const setStatus    = useSetOrganizationStatus()
  const reprovision  = useReprovisionOrganization()
  const migrate      = useMigrateIsolatedDatabases()
  const toast        = useToast()

  async function handleMigrateAll() {
    setMigrateResults(null)
    try {
      const results = await migrate.mutateAsync()
      setMigrateResults(results)
    } catch (err) {
      toast(err.message ?? 'Migration failed.', 'danger')
    }
  }

  async function handleReprovision(org) {
    try {
      await reprovision.mutateAsync(org.organizationId)
      toast(`Reprovisioning started for ${org.organizationName}.`)
    } catch (err) {
      toast(err.message ?? 'Failed to start reprovisioning.', 'danger')
    }
  }

  function promptStatus(org) {
    setConfirmId(org.organizationId)
    setTargetStatus(!org.isActive)
  }

  async function confirmStatus() {
    try {
      await setStatus.mutateAsync({ id: confirmId, status: targetStatus })
      toast(targetStatus ? 'Organisation activated.' : 'Organisation deactivated.')
    } catch (err) {
      toast(err.message ?? 'Failed to update status.', 'danger')
    } finally {
      setConfirmId(null)
    }
  }

  if (isLoading) return <LoadingState message="Loading organisations…" />
  if (isError)   return <ErrorAlert message="Could not load organisations." />

  return (
    <div>
      <PageHeader
        title="Organisations"
        subtitle={`${orgs?.length ?? 0} organisation${orgs?.length !== 1 ? 's' : ''}`}
        actions={
          <>
            <input
              type="search"
              className="form-control form-control-sm"
              style={{ width: 220 }}
              placeholder="Search organisations…"
              value={searchInput}
              onChange={e => setSearchInput(e.target.value)}
            />
            <div className="form-check form-switch mb-0 me-2">
              <input className="form-check-input" type="checkbox" id="showInactive"
                checked={showInactive} onChange={e => setShowInactive(e.target.checked)} />
              <label className="form-check-label text-muted-sm" htmlFor="showInactive">Show inactive</label>
            </div>
            <button className="btn btn-outline-secondary btn-sm" onClick={handleMigrateAll}
              disabled={migrate.isPending}>
              {migrate.isPending ? 'Migrating…' : '🗄️ Migrate All DBs'}
            </button>
            <button className="btn btn-primary btn-sm" onClick={() => setShowCreate(true)}>+ New Organisation</button>
          </>
        }
      />

      {migrateResults && (
        <MigrateResultsPanel results={migrateResults} onClose={() => setMigrateResults(null)} />
      )}

      <div className="admin-card p-0">
        {!orgs?.length ? (
          <EmptyState icon="🏢" title="No organisations yet"
            description="Create your first organisation to get started."
            action={<button className="btn btn-primary btn-sm" onClick={() => setShowCreate(true)}>Create Organisation</button>} />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Code</th>
                  <th>Marketing Name</th>
                  <th>Status</th>
                  <th>Database</th>
                  <th>Created</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {orgs.map(org => (
                  <tr key={org.organizationId}>
                    <td className="fw-semibold">
                      <Link to={`/organizations/${org.organizationId}`}
                        className="text-decoration-none" style={{ color: '#1a56db' }}>
                        {org.organizationName}
                      </Link>
                    </td>
                    <td><code className="text-muted-sm">{org.organizationCode}</code></td>
                    <td className="text-muted-sm">{org.marketingName ?? '—'}</td>
                    <td><StatusBadge active={org.isActive} /></td>
                    <td><ProvisioningBadge status={org.databaseProvisioningStatus} required={org.requiresIsolatedDatabase} /></td>
                    <td className="text-muted-sm">{fmtDate(org.createdDate)}</td>
                    <td>
                      <div className="gap-actions justify-content-end">
                        <div className="dropdown">
                          <button className="btn btn-sm btn-outline-secondary dropdown-toggle" data-bs-toggle="dropdown">
                            Actions
                          </button>
                          <ul className="dropdown-menu dropdown-menu-end">
                            <li><Link className="dropdown-item" to={`/organizations/${org.organizationId}`}>🏠 Overview</Link></li>
                            <li><Link className="dropdown-item" to={`/organizations/${org.organizationId}/customers`}>👥 Customers</Link></li>
                            <li><Link className="dropdown-item" to={`/organizations/${org.organizationId}/inputs`}>🗂️ Inputs</Link></li>
                            <li><Link className="dropdown-item" to={`/organizations/${org.organizationId}/import`}>📥 Import</Link></li>
                            <li><hr className="dropdown-divider" /></li>
                            <li><button className="dropdown-item" onClick={() => setEditOrg(org)}>✏️ Edit</button></li>
                            <li>
                              <button className="dropdown-item" onClick={() => promptStatus(org)}>
                                {org.isActive ? '🚫 Deactivate' : '✅ Activate'}
                              </button>
                            </li>
                            {org.requiresIsolatedDatabase && (
                              <li>
                                <button className="dropdown-item" onClick={() => handleReprovision(org)}>
                                  🔄 Reprovision Database
                                </button>
                              </li>
                            )}
                          </ul>
                        </div>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {(showCreate || editOrg) && (
        <OrgModal org={editOrg} onClose={() => { setShowCreate(false); setEditOrg(null) }} />
      )}

      <ConfirmModal
        show={!!confirmId}
        title={targetStatus ? 'Activate Organisation' : 'Deactivate Organisation'}
        message={targetStatus
          ? 'This will make the organisation visible and active.'
          : 'This will hide the organisation and disable access.'}
        danger={!targetStatus}
        onConfirm={confirmStatus}
        onCancel={() => setConfirmId(null)}
        loading={setStatus.isPending}
      />
    </div>
  )
}
