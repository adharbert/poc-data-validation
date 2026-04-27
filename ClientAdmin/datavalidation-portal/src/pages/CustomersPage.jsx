import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import {
  useCustomers, useCreateCustomer, useUpdateCustomer, useSetCustomerStatus,
} from '@/hooks/useApi.js'
import {
  PageHeader, LoadingState, ErrorAlert,
  StatusBadge, ConfirmModal, EmptyState, Pagination, useToast,
} from '@/components/common/index.jsx'

function CustomerModal({ orgId, customer, onClose }) {
  const toast  = useToast()
  const create = useCreateCustomer(orgId)
  const update = useUpdateCustomer(orgId)
  const isEdit = !!customer

  const { register, handleSubmit, formState: { errors } } = useForm({
    defaultValues: {
      firstName:   customer?.firstName   ?? '',
      lastName:    customer?.lastName    ?? '',
      middleName:  customer?.middleName  ?? '',
      email:       customer?.email       ?? '',
      phone:       customer?.phone       ?? '',
      originalId:  customer?.originalId  ?? '',
      isActive:    customer?.isActive    ?? true,
    },
  })

  async function onSubmit(values) {
    try {
      if (isEdit) {
        await update.mutateAsync({ customerId: customer.customerId, data: values })
        toast('Customer updated.')
      } else {
        await create.mutateAsync(values)
        toast('Customer created.')
      }
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving customer.', 'danger')
    }
  }

  const saving = create.isPending || update.isPending

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">{isEdit ? 'Edit Customer' : 'New Customer'}</h5>
              <button type="button" className="btn-close" onClick={onClose} />
            </div>
            <div className="modal-body">
              <div className="row g-3">
                <div className="col-5">
                  <label className="form-label fw-semibold">First Name <span className="text-danger">*</span></label>
                  <input className={`form-control ${errors.firstName ? 'is-invalid' : ''}`}
                    {...register('firstName', { required: 'Required' })} />
                  {errors.firstName && <div className="invalid-feedback">{errors.firstName.message}</div>}
                </div>
                <div className="col-4">
                  <label className="form-label fw-semibold">Last Name <span className="text-danger">*</span></label>
                  <input className={`form-control ${errors.lastName ? 'is-invalid' : ''}`}
                    {...register('lastName', { required: 'Required' })} />
                  {errors.lastName && <div className="invalid-feedback">{errors.lastName.message}</div>}
                </div>
                <div className="col-3">
                  <label className="form-label">Middle</label>
                  <input className="form-control" {...register('middleName')} />
                </div>
                <div className="col-6">
                  <label className="form-label fw-semibold">Email <span className="text-danger">*</span></label>
                  <input type="email" className={`form-control ${errors.email ? 'is-invalid' : ''}`}
                    {...register('email', { required: 'Required' })} />
                  {errors.email && <div className="invalid-feedback">{errors.email.message}</div>}
                </div>
                <div className="col-6">
                  <label className="form-label">Phone</label>
                  <input className="form-control" {...register('phone')} />
                </div>
                <div className="col-12">
                  <label className="form-label">Client ID (Original ID)</label>
                  <input className="form-control" {...register('originalId')}
                    placeholder="Client's own identifier (member #, account #, etc.)" />
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Customer'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

export default function CustomersPage() {
  const { organizationId } = useParams()
  const [page, setPage]            = useState(1)
  const [showInactive, setShowInactive] = useState(false)
  const [showCreate, setShowCreate] = useState(false)
  const [editCustomer, setEditCustomer] = useState(null)
  const [confirmId, setConfirmId]   = useState(null)
  const [targetStatus, setTargetStatus] = useState(null)

  const { data: paged, isLoading, isError } = useCustomers(organizationId, page, 50, showInactive)
  const setStatus = useSetCustomerStatus(organizationId)
  const toast     = useToast()

  const customers = paged?.items ?? []
  const total     = paged?.total ?? 0

  async function confirmStatus() {
    try {
      await setStatus.mutateAsync({ customerId: confirmId, isActive: targetStatus })
      toast(targetStatus ? 'Customer activated.' : 'Customer deactivated.')
    } catch (err) {
      toast(err.message ?? 'Failed to update status.', 'danger')
    } finally {
      setConfirmId(null)
    }
  }

  if (isLoading) return <LoadingState message="Loading customers…" />
  if (isError)   return <ErrorAlert message="Could not load customers." />

  return (
    <div>
      <PageHeader
        breadcrumbs={[
          { label: 'Organisations', href: '/organizations' },
          { label: 'Customers' },
        ]}
        title="Customers"
        subtitle={`${total.toLocaleString()} customer${total !== 1 ? 's' : ''}`}
        actions={
          <>
            <Link to={`/organizations/${organizationId}/import`} className="btn btn-sm btn-outline-primary">📥 Import</Link>
            <div className="form-check form-switch mb-0 mx-2">
              <input className="form-check-input" type="checkbox" id="showInactive"
                checked={showInactive} onChange={e => setShowInactive(e.target.checked)} />
              <label className="form-check-label text-muted-sm" htmlFor="showInactive">Inactive</label>
            </div>
            <button className="btn btn-primary btn-sm" onClick={() => setShowCreate(true)}>+ New</button>
          </>
        }
      />

      <div className="admin-card p-0">
        {!customers.length ? (
          <EmptyState icon="👥" title="No customers yet"
            description="Upload a CSV/Excel file or create customers manually."
            action={
              <div className="gap-actions justify-content-center">
                <Link to={`/organizations/${organizationId}/import`} className="btn btn-primary btn-sm">📥 Import File</Link>
                <button className="btn btn-outline-primary btn-sm" onClick={() => setShowCreate(true)}>+ New Customer</button>
              </div>
            }
          />
        ) : (
          <>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Customer Code</th>
                    <th>Client ID</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {customers.map(c => (
                    <tr key={c.customerId}>
                      <td className="fw-semibold">{c.firstName} {c.middleName ? c.middleName + ' ' : ''}{c.lastName}</td>
                      <td className="text-muted-sm">{c.email ?? '—'}</td>
                      <td><code className="text-muted-sm">{c.customerCode}</code></td>
                      <td className="text-muted-sm">{c.originalId ?? '—'}</td>
                      <td><StatusBadge active={c.isActive} /></td>
                      <td>
                        <div className="gap-actions justify-content-end">
                          <button className="btn btn-sm btn-outline-secondary" onClick={() => setEditCustomer(c)}>Edit</button>
                          <button
                            className={`btn btn-sm ${c.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`}
                            onClick={() => { setConfirmId(c.customerId); setTargetStatus(!c.isActive) }}
                          >
                            {c.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="px-4 pb-3">
              <Pagination page={page} pageSize={50} total={total} onChange={setPage} />
            </div>
          </>
        )}
      </div>

      {(showCreate || editCustomer) && (
        <CustomerModal
          orgId={organizationId}
          customer={editCustomer}
          onClose={() => { setShowCreate(false); setEditCustomer(null) }}
        />
      )}

      <ConfirmModal
        show={!!confirmId}
        title={targetStatus ? 'Activate Customer' : 'Deactivate Customer'}
        message={targetStatus ? 'Customer will be set to active.' : 'Customer will be deactivated.'}
        danger={!targetStatus}
        onConfirm={confirmStatus}
        onCancel={() => setConfirmId(null)}
        loading={setStatus.isPending}
      />
    </div>
  )
}
