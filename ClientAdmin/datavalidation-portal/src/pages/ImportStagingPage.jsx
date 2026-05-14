import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import {
  useOrganization,
  useStagingColumns, useResolveStaging, useDeleteStaging,
} from '@/hooks/useApi.js'
import {
  PageHeader, LoadingState, ErrorAlert, ConfirmModal, EmptyState, useToast,
} from '@/components/common/index.jsx'
import { fmtDate } from '@/utils/dates.js'

const STATUS_OPTS = [
  { value: '',          label: 'All'       },
  { value: 'unmatched', label: 'Unmatched' },
  { value: 'resolved',  label: 'Resolved'  },
  { value: 'skipped',   label: 'Skipped'   },
]

const STATUS_BADGE = {
  unmatched: 'bg-warning text-dark',
  resolved:  'bg-success text-white',
  skipped:   'bg-secondary text-white',
}

function ResolveModal({ orgId, staging, onClose }) {
  const toast   = useToast()
  const resolve = useResolveStaging(orgId)

  const { register, handleSubmit, watch, formState: { errors } } = useForm({
    defaultValues: {
      status:            'resolved',
      mappingType:       staging.mappingType       ?? 'customer_field',
      customerFieldName: staging.customerFieldName ?? '',
      notes:             staging.notes             ?? '',
    },
  })

  const status      = watch('status')
  const mappingType = watch('mappingType')

  // Note: ImportColumnStaging still uses legacy mappingType/customerFieldName column names.
  // The resolve request preserves those names so the staging table continues to work
  // until a future migration aligns it with the new schema.

  async function onSubmit(values) {
    try {
      await resolve.mutateAsync({ stagingId: staging.stagingId, data: values })
      toast(`Column "${staging.csvHeader}" ${values.status}.`)
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error resolving column.', 'danger')
    }
  }

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">Resolve Column</h5>
              <button type="button" className="btn-close" onClick={onClose} />
            </div>
            <div className="modal-body">
              <p className="text-muted-sm mb-3">
                CSV Header: <strong>{staging.csvHeader}</strong> — seen {staging.seenCount}× across uploads
              </p>

              <div className="mb-3">
                <label className="form-label fw-semibold">Action</label>
                <div className="d-flex gap-3">
                  {['resolved', 'skipped'].map(s => (
                    <div key={s} className="form-check">
                      <input className="form-check-input" type="radio" id={`status-${s}`}
                        value={s} {...register('status')} />
                      <label className="form-check-label" htmlFor={`status-${s}`}>
                        {s === 'resolved' ? '✅ Map to field' : '⏭️ Skip (ignore)'}
                      </label>
                    </div>
                  ))}
                </div>
              </div>

              {status === 'resolved' && (
                <>
                  <div className="mb-3">
                    <label className="form-label fw-semibold">Map to</label>
                    <select className="form-select" {...register('mappingType')}>
                      <option value="customer_field">Customer field</option>
                      <option value="customer_address">Address field</option>
                      <option value="field_definition">Key / Value field</option>
                    </select>
                  </div>

                  {mappingType === 'customer_field' && (
                    <div className="mb-3">
                      <label className="form-label fw-semibold">Customer Field <span className="text-danger">*</span></label>
                      <select className={`form-select ${errors.customerFieldName ? 'is-invalid' : ''}`}
                        {...register('customerFieldName', { required: status === 'resolved' ? 'Required' : false })}>
                        <option value="">Select…</option>
                        <option value="FirstName">First Name</option>
                        <option value="LastName">Last Name</option>
                        <option value="MiddleName">Middle Name</option>
                        <option value="MaidenName">Maiden Name</option>
                        <option value="DateOfBirth">Date of Birth</option>
                        <option value="Email">Email</option>
                        <option value="Phone">Phone</option>
                        <option value="OriginalId">Client ID (OriginalId)</option>
                      </select>
                      {errors.customerFieldName && <div className="invalid-feedback">{errors.customerFieldName.message}</div>}
                    </div>
                  )}

                  {mappingType === 'customer_address' && (
                    <div className="mb-3">
                      <label className="form-label fw-semibold">Address Field <span className="text-danger">*</span></label>
                      <select className={`form-select ${errors.customerFieldName ? 'is-invalid' : ''}`}
                        {...register('customerFieldName', { required: status === 'resolved' ? 'Required' : false })}>
                        <option value="">Select…</option>
                        <option value="AddressLine1">Address Line 1</option>
                        <option value="AddressLine2">Address Line 2</option>
                        <option value="City">City</option>
                        <option value="State">State</option>
                        <option value="PostalCode">Postal Code</option>
                        <option value="Country">Country</option>
                        <option value="AddressType">Address Type</option>
                      </select>
                      {errors.customerFieldName && <div className="invalid-feedback">{errors.customerFieldName.message}</div>}
                    </div>
                  )}
                </>
              )}

              <div className="mb-0">
                <label className="form-label">Notes</label>
                <input className="form-control" {...register('notes')} placeholder="Optional notes" />
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={resolve.isPending}>
                {resolve.isPending ? 'Saving…' : 'Save'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

export default function ImportStagingPage() {
  const { organizationId } = useParams()
  const { data: org } = useOrganization(organizationId)
  const [statusFilter, setStatusFilter] = useState('')
  const [resolveItem, setResolveItem]   = useState(null)
  const [deleteId, setDeleteId]         = useState(null)

  const { data: staging, isLoading, isError } = useStagingColumns(organizationId, statusFilter)
  const deleteMutation = useDeleteStaging(organizationId)
  const toast          = useToast()

  async function confirmDelete() {
    try {
      await deleteMutation.mutateAsync(deleteId)
      toast('Staging record deleted.')
    } catch (err) {
      toast(err.message ?? 'Error deleting.', 'danger')
    } finally {
      setDeleteId(null)
    }
  }

  if (isLoading) return <LoadingState message="Loading staging columns…" />
  if (isError)   return <ErrorAlert message="Could not load import staging." />

  const unmatched = staging?.filter(s => s.status === 'unmatched').length ?? 0

  return (
    <div>
      <PageHeader
        breadcrumbs={[
          { label: 'Organisations', href: '/organizations' },
          { label: org?.organizationName ?? '…', href: `/organizations/${organizationId}` },
          { label: 'Import Staging' },
        ]}
        title="Import Column Staging"
        subtitle="Unmatched CSV/Excel headers from previous uploads — resolve or skip them"
        actions={
          <Link to={`/organizations/${organizationId}/import`} className="btn btn-sm btn-outline-primary">
            📥 Go to Import
          </Link>
        }
      />

      {unmatched > 0 && (
        <div className="alert alert-warning d-flex gap-2 align-items-center mb-3" style={{ fontSize: '.875rem' }}>
          <span>⚠️</span>
          <span><strong>{unmatched} unmatched column{unmatched !== 1 ? 's' : ''}</strong> waiting to be resolved. Mapping them will apply automatically on future uploads.</span>
        </div>
      )}

      {/* Filter tabs */}
      <div className="d-flex gap-2 mb-3">
        {STATUS_OPTS.map(opt => (
          <button
            key={opt.value}
            className={`btn btn-sm ${statusFilter === opt.value ? 'btn-primary' : 'btn-outline-secondary'}`}
            onClick={() => setStatusFilter(opt.value)}
          >
            {opt.label}
          </button>
        ))}
      </div>

      <div className="admin-card p-0">
        {!staging?.length ? (
          <EmptyState icon="🔧" title="No staging records"
            description={statusFilter ? `No ${statusFilter} columns found.` : 'All columns have been resolved.'}
          />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>CSV Header</th>
                  <th>Status</th>
                  <th>Mapped To</th>
                  <th>Seen</th>
                  <th>First Seen</th>
                  <th>Last Seen</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {staging.map(s => (
                  <tr key={s.stagingId}>
                    <td className="fw-semibold font-monospace" style={{ fontSize: '.85rem' }}>{s.csvHeader}</td>
                    <td>
                      <span className={`badge ${STATUS_BADGE[s.status] ?? 'bg-secondary'}`} style={{ fontSize: '.75rem' }}>
                        {s.status}
                      </span>
                    </td>
                    <td className="text-muted-sm">
                      {s.status === 'resolved'
                        ? (s.customerFieldName ?? s.fieldLabel ?? '—')
                        : s.status === 'skipped' ? <em>Skipped</em> : '—'}
                    </td>
                    <td className="text-muted-sm">{s.seenCount}×</td>
                    <td className="text-muted-sm">{fmtDate(s.firstSeenAt)}</td>
                    <td className="text-muted-sm">{fmtDate(s.lastSeenAt)}</td>
                    <td>
                      <div className="gap-actions justify-content-end">
                        <button className="btn btn-sm btn-outline-primary" onClick={() => setResolveItem(s)}>
                          {s.status === 'unmatched' ? 'Resolve' : 'Edit'}
                        </button>
                        <button className="btn btn-sm btn-outline-danger" onClick={() => setDeleteId(s.stagingId)}>
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {resolveItem && (
        <ResolveModal orgId={organizationId} staging={resolveItem} onClose={() => setResolveItem(null)} />
      )}

      <ConfirmModal
        show={!!deleteId}
        title="Delete Staging Record"
        message="This header will reappear as unmatched the next time it appears in an upload."
        danger
        onConfirm={confirmDelete}
        onCancel={() => setDeleteId(null)}
        loading={deleteMutation.isPending}
      />
    </div>
  )
}
