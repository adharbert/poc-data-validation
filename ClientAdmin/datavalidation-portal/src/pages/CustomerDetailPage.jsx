import { useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import {
  useOrganization, useCustomer,
  useCustomerEmails, useCustomerPhones,
  useCustomerAddresses, useCustomerFieldValues,
  useUpdateCustomer, useSetCustomerStatus,
} from '@/hooks/useApi.js'
import {
  PageHeader, LoadingState, ErrorAlert, StatusBadge, ConfirmModal, useToast,
} from '@/components/common/index.jsx'
import { useForm } from 'react-hook-form'
import { fmtPhone, formatPhoneInput } from '@/utils/dates.js'

function Section({ title, children }) {
  return (
    <div className="admin-card mb-3">
      <div className="px-4 pt-3 pb-2 border-bottom">
        <h6 className="mb-0 fw-semibold text-muted">{title}</h6>
      </div>
      <div className="px-4 py-3">{children}</div>
    </div>
  )
}

function KVRow({ label, value }) {
  if (value == null || value === '') return null
  return (
    <div className="row py-1">
      <div className="col-4 text-muted-sm">{label}</div>
      <div className="col-8 fw-semibold">{value}</div>
    </div>
  )
}

function EditCustomerModal({ orgId, customer, onClose }) {
  const toast  = useToast()
  const update = useUpdateCustomer(orgId)

  const { register, handleSubmit, setValue, formState: { errors } } = useForm({
    defaultValues: {
      firstName:  customer.firstName  ?? '',
      lastName:   customer.lastName   ?? '',
      middleName: customer.middleName ?? '',
      email:      customer.email      ?? '',
      phone:      fmtPhone(customer.phone) || '',
      originalId: customer.originalId ?? '',
      isActive:   customer.isActive   ?? true,
    },
  })

  async function onSubmit(values) {
    try {
      await update.mutateAsync({ customerId: customer.customerId, data: values })
      toast('Customer updated.')
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving customer.', 'danger')
    }
  }

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">Edit Customer</h5>
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
                  <input className="form-control" {...register('phone')}
                    onChange={e => { e.target.value = formatPhoneInput(e.target.value); setValue('phone', e.target.value) }} />
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
              <button type="submit" className="btn btn-primary" disabled={update.isPending}>
                {update.isPending ? 'Saving…' : 'Save Changes'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

export default function CustomerDetailPage() {
  const { organizationId, customerId } = useParams()
  const navigate = useNavigate()
  const toast    = useToast()

  const [showEdit, setShowEdit]         = useState(false)
  const [showConfirm, setShowConfirm]   = useState(false)
  const [targetStatus, setTargetStatus] = useState(null)

  const { data: org }       = useOrganization(organizationId)
  const { data: customer, isLoading: loadingCustomer, isError: errCustomer } = useCustomer(organizationId, customerId)
  const { data: emails  = [] }    = useCustomerEmails(organizationId, customerId)
  const { data: phones  = [] }    = useCustomerPhones(organizationId, customerId)
  const { data: addresses = [] }  = useCustomerAddresses(customerId)
  const { data: fieldValues = [] } = useCustomerFieldValues(customerId)
  const setStatus = useSetCustomerStatus(organizationId)

  async function confirmStatusChange() {
    try {
      await setStatus.mutateAsync({ customerId, isActive: targetStatus })
      toast(targetStatus ? 'Customer activated.' : 'Customer deactivated.')
    } catch (err) {
      toast(err.message ?? 'Failed to update status.', 'danger')
    } finally {
      setShowConfirm(false)
    }
  }

  if (loadingCustomer) return <LoadingState message="Loading customer…" />
  if (errCustomer || !customer) return <ErrorAlert message="Could not load customer." />

  const fullName = [customer.firstName, customer.middleName, customer.lastName].filter(Boolean).join(' ')

  return (
    <div>
      <PageHeader
        breadcrumbs={[
          { label: 'Organizations', href: '/organizations' },
          { label: org?.organizationName ?? '…', href: `/organizations/${organizationId}` },
          { label: 'Customers', href: `/organizations/${organizationId}/customers` },
          { label: fullName },
        ]}
        title={fullName}
        subtitle={<><code className="text-muted-sm me-2">{customer.customerCode}</code><StatusBadge active={customer.isActive} /></>}
        actions={
          <div className="gap-actions">
            <button className="btn btn-sm btn-outline-secondary" onClick={() => setShowEdit(true)}>Edit</button>
            <button
              className={`btn btn-sm ${customer.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`}
              onClick={() => { setTargetStatus(!customer.isActive); setShowConfirm(true) }}
            >
              {customer.isActive ? 'Deactivate' : 'Activate'}
            </button>
          </div>
        }
      />

      {/* Core customer data */}
      <Section title="Customer Information">
        <div className="row">
          <div className="col-md-6">
            <KVRow label="First Name"   value={customer.firstName} />
            <KVRow label="Middle Name"  value={customer.middleName} />
            <KVRow label="Last Name"    value={customer.lastName} />
            <KVRow label="Date of Birth" value={customer.dateOfBirth} />
          </div>
          <div className="col-md-6">
            <KVRow label="Email"        value={customer.email} />
            <KVRow label="Phone"        value={fmtPhone(customer.phone)} />
            <KVRow label="Client ID"    value={customer.originalId} />
            <KVRow label="Customer Code" value={customer.customerCode} />
          </div>
        </div>
      </Section>

      {/* Emails */}
      <Section title={`Email Addresses (${emails.length})`}>
        {emails.length === 0 ? (
          <p className="text-muted-sm mb-0">No email addresses on file.</p>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr><th>Address</th><th>Type</th><th>Primary</th><th>Active</th></tr>
              </thead>
              <tbody>
                {emails.map(e => (
                  <tr key={e.emailId}>
                    <td>{e.emailAddress}</td>
                    <td className="text-muted-sm text-capitalize">{e.emailType}</td>
                    <td>{e.isPrimary ? <span className="badge bg-primary">Primary</span> : '—'}</td>
                    <td><StatusBadge active={e.isActive} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Section>

      {/* Phones */}
      <Section title={`Phone Numbers (${phones.length})`}>
        {phones.length === 0 ? (
          <p className="text-muted-sm mb-0">No phone numbers on file.</p>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr><th>Number</th><th>Type</th><th>Primary</th><th>Active</th></tr>
              </thead>
              <tbody>
                {phones.map(p => (
                  <tr key={p.phoneId}>
                    <td>{fmtPhone(p.phoneNumber)}</td>
                    <td className="text-muted-sm text-capitalize">{p.phoneType}</td>
                    <td>{p.isPrimary ? <span className="badge bg-primary">Primary</span> : '—'}</td>
                    <td><StatusBadge active={p.isActive} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Section>

      {/* Addresses */}
      <Section title={`Addresses (${addresses.length})`}>
        {addresses.length === 0 ? (
          <p className="text-muted-sm mb-0">No addresses on file.</p>
        ) : (
          <div className="row g-3">
            {addresses.map(a => (
              <div key={a.addressId} className="col-md-6">
                <div className="border rounded p-3">
                  <div className="d-flex justify-content-between mb-1">
                    <span className="badge bg-secondary text-capitalize">{a.addressType}</span>
                    <div>
                      {a.isCurrent && <span className="badge bg-success me-1">Current</span>}
                      {a.customerConfirmed && <span className="badge bg-info">Confirmed</span>}
                    </div>
                  </div>
                  <div className="fw-semibold">{a.addressLine1}</div>
                  {a.addressLine2 && <div>{a.addressLine2}</div>}
                  <div>{a.city}, {a.state} {a.postalCode}</div>
                  {a.country && a.country !== 'US' && <div className="text-muted-sm">{a.country}</div>}
                </div>
              </div>
            ))}
          </div>
        )}
      </Section>

      {/* Field Values */}
      <Section title={`Field Values (${fieldValues.length})`}>
        {fieldValues.length === 0 ? (
          <p className="text-muted-sm mb-0">No field values recorded.</p>
        ) : (
          <div className="row g-2">
            {fieldValues.map(fv => (
              <div key={fv.fieldValueId} className="col-md-6 col-lg-4">
                <div className="border rounded p-2">
                  <div className="text-muted-sm small mb-1">{fv.fieldLabel}</div>
                  <div className="fw-semibold">
                    {fv.displayValue ?? fv.valueText ?? fv.valueNumber?.toString() ?? fv.valueDate ?? '—'}
                  </div>
                  {fv.confirmedAt && (
                    <div className="text-success small mt-1">
                      Confirmed {new Date(fv.confirmedAt).toLocaleDateString()}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </Section>

      {showEdit && (
        <EditCustomerModal
          orgId={organizationId}
          customer={customer}
          onClose={() => setShowEdit(false)}
        />
      )}

      <ConfirmModal
        show={showConfirm}
        title={targetStatus ? 'Activate Customer' : 'Deactivate Customer'}
        message={targetStatus ? 'Customer will be set to active.' : 'Customer will be deactivated.'}
        danger={!targetStatus}
        onConfirm={confirmStatusChange}
        onCancel={() => setShowConfirm(false)}
        loading={setStatus.isPending}
      />
    </div>
  )
}
