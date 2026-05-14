import { useState, useRef } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import {
  useOrganization, useContracts, useCreateContract, useUpdateContract,
  useSetContractStatus, useContractDocuments, useUploadContractDocument,
  useDeleteContractDocument,
} from '@/hooks/useApi.js'
import {
  PageHeader, LoadingState, ErrorAlert,
  StatusBadge, ConfirmModal, EmptyState, useToast,
} from '@/components/common/index.jsx'
import { fmtDate } from '@/utils/dates.js'
import { contractDocumentApi } from '@/api/services.js'

// ─── Helpers ─────────────────────────────────────────────────────────────────

function isExpired(endDate) {
  if (!endDate) return false
  return new Date(endDate) < new Date()
}

function fmtBytes(bytes) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

// ─── Contract Modal ───────────────────────────────────────────────────────────

function ContractModal({ orgId, contract, onClose }) {
  const toast  = useToast()
  const create = useCreateContract(orgId)
  const update = useUpdateContract(orgId)
  const isEdit = !!contract

  const { register, handleSubmit, formState: { errors } } = useForm({
    defaultValues: {
      contractName:   contract?.contractName   ?? '',
      contractNumber: contract?.contractNumber ?? '',
      startDate:      contract?.startDate      ?? '',
      endDate:        contract?.endDate        ?? '',
      notes:          contract?.notes          ?? '',
    },
  })

  async function onSubmit(values) {
    const payload = {
      contractName:   values.contractName,
      contractNumber: values.contractNumber || null,
      startDate:      values.startDate,
      endDate:        values.endDate || null,
      notes:          values.notes   || null,
    }
    try {
      if (isEdit) {
        await update.mutateAsync({ contractId: contract.contractId, data: payload })
        toast('Contract updated.')
      } else {
        await create.mutateAsync(payload)
        toast('Contract created.')
      }
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving contract.', 'danger')
    }
  }

  const saving = create.isPending || update.isPending

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">{isEdit ? 'Edit Contract' : 'New Contract'}</h5>
              <button type="button" className="btn-close" onClick={onClose} />
            </div>
            <div className="modal-body">
              <div className="row g-3">
                <div className="col-8">
                  <label className="form-label fw-semibold">Contract Name <span className="text-danger">*</span></label>
                  <input className={`form-control ${errors.contractName ? 'is-invalid' : ''}`}
                    {...register('contractName', { required: 'Required' })} />
                  {errors.contractName && <div className="invalid-feedback">{errors.contractName.message}</div>}
                </div>
                <div className="col-4">
                  <label className="form-label">Contract #</label>
                  <input className="form-control" {...register('contractNumber')} placeholder="e.g. CRM-1234" />
                </div>
                <div className="col-6">
                  <label className="form-label fw-semibold">Start Date <span className="text-danger">*</span></label>
                  <input type="date" className={`form-control ${errors.startDate ? 'is-invalid' : ''}`}
                    {...register('startDate', { required: 'Required' })} />
                  {errors.startDate && <div className="invalid-feedback">{errors.startDate.message}</div>}
                </div>
                <div className="col-6">
                  <label className="form-label">End Date</label>
                  <input type="date" className="form-control" {...register('endDate')} />
                </div>
                <div className="col-12">
                  <label className="form-label">Notes</label>
                  <textarea className="form-control" rows={3} {...register('notes')} />
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Contract'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

// ─── Document Panel ───────────────────────────────────────────────────────────

function DocumentPanel({ orgId, contract }) {
  const toast      = useToast()
  const fileRef    = useRef(null)
  const [delDocId, setDelDocId] = useState(null)

  const { data: docs = [], isLoading } = useContractDocuments(orgId, contract.contractId)
  const upload = useUploadContractDocument(orgId, contract.contractId)
  const remove = useDeleteContractDocument(orgId, contract.contractId)

  async function handleFileChange(e) {
    const file = e.target.files?.[0]
    if (!file) return
    const fd = new FormData()
    fd.append('file', file)
    fd.append('uploadedBy', 'Admin')
    try {
      await upload.mutateAsync(fd)
      toast('Document uploaded.')
    } catch (err) {
      toast(err.message ?? 'Upload failed.', 'danger')
    } finally {
      e.target.value = ''
    }
  }

  async function confirmDelete() {
    try {
      await remove.mutateAsync(delDocId)
      toast('Document deleted.')
    } catch (err) {
      toast(err.message ?? 'Delete failed.', 'danger')
    } finally {
      setDelDocId(null)
    }
  }

  return (
    <div className="px-4 py-3" style={{ background: '#f8fafc', borderTop: '1px solid #e5e7eb' }}>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <span className="fw-semibold" style={{ fontSize: '.85rem' }}>Documents</span>
        <button
          className="btn btn-sm btn-outline-primary"
          disabled={upload.isPending}
          onClick={() => fileRef.current?.click()}
        >
          {upload.isPending ? 'Uploading…' : '+ Upload Document'}
        </button>
        <input ref={fileRef} type="file"
          accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
          style={{ display: 'none' }}
          onChange={handleFileChange}
        />
      </div>

      {isLoading ? (
        <p className="text-muted-sm">Loading documents…</p>
      ) : !docs.length ? (
        <p className="text-muted-sm mb-0">No documents uploaded yet. Accepted formats: PDF, Word, JPEG, PNG.</p>
      ) : (
        <table className="data-table" style={{ fontSize: '.82rem' }}>
          <thead>
            <tr>
              <th>File Name</th>
              <th>Uploaded</th>
              <th>Size</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {docs.map(doc => (
              <tr key={doc.documentId}>
                <td>
                  <a
                    href={contractDocumentApi.downloadUrl(orgId, contract.contractId, doc.documentId)}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-decoration-none"
                    style={{ color: '#1a56db' }}
                  >
                    📄 {doc.originalFileName}
                  </a>
                </td>
                <td className="text-muted-sm">{fmtDate(doc.uploadedAt)}</td>
                <td className="text-muted-sm">{fmtBytes(doc.fileSizeBytes)}</td>
                <td>
                  <button className="btn btn-sm btn-outline-danger py-0"
                    onClick={() => setDelDocId(doc.documentId)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <ConfirmModal
        show={!!delDocId}
        title="Delete Document"
        message="This document will be permanently removed. This cannot be undone."
        danger
        onConfirm={confirmDelete}
        onCancel={() => setDelDocId(null)}
        loading={remove.isPending}
      />
    </div>
  )
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function ContractsPage() {
  const { organizationId } = useParams()
  const [showInactive, setShowInactive]   = useState(false)
  const [showCreate, setShowCreate]       = useState(false)
  const [editContract, setEditContract]   = useState(null)
  const [expandedId, setExpandedId]       = useState(null)
  const [confirmStatus, setConfirmStatus] = useState(null) // { contractId, targetActive }

  const { data: org }                    = useOrganization(organizationId)
  const { data: contracts = [], isLoading, isError } = useContracts(organizationId, showInactive)
  const setStatus = useSetContractStatus(organizationId)
  const toast     = useToast()

  function toggleExpand(contractId) {
    setExpandedId(prev => prev === contractId ? null : contractId)
  }

  async function handleStatusConfirm() {
    const { contractId, targetActive } = confirmStatus
    try {
      await setStatus.mutateAsync({ contractId, isActive: targetActive })
      toast(targetActive ? 'Contract activated.' : 'Contract deactivated.')
    } catch (err) {
      toast(err.message ?? 'Failed to update status.', 'danger')
    } finally {
      setConfirmStatus(null)
    }
  }

  if (isLoading) return <LoadingState message="Loading contracts…" />
  if (isError)   return <ErrorAlert message="Could not load contracts." />

  const activeCount = contracts.filter(c => c.isActive).length

  return (
    <div>
      <PageHeader
        breadcrumbs={[
          { label: 'Organizations', href: '/organizations' },
          { label: org?.organizationName ?? '…', href: `/organizations/${organizationId}` },
          { label: 'Contracts' },
        ]}
        title="Contracts"
        subtitle={`${activeCount} active contract${activeCount !== 1 ? 's' : ''}`}
        actions={
          <>
            <div className="form-check form-switch mb-0 mx-2">
              <input className="form-check-input" type="checkbox" id="showInactive"
                checked={showInactive} onChange={e => setShowInactive(e.target.checked)} />
              <label className="form-check-label text-muted-sm" htmlFor="showInactive">Show Inactive / Expired</label>
            </div>
            <button className="btn btn-primary btn-sm" onClick={() => setShowCreate(true)}>+ New Contract</button>
          </>
        }
      />

      <div className="admin-card p-0">
        {!contracts.length ? (
          <EmptyState
            icon="📄"
            title="No contracts on record"
            description="Create the first contract for this organization."
            action={<button className="btn btn-primary btn-sm" onClick={() => setShowCreate(true)}>+ New Contract</button>}
          />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Contract Name</th>
                  <th>Contract #</th>
                  <th>Start Date</th>
                  <th>End Date</th>
                  <th>Status</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {contracts.map(c => {
                  const expired = isExpired(c.endDate)
                  return [
                    <tr key={c.contractId}>
                      <td>
                        <span className="fw-semibold">{c.contractName}</span>
                        {expired && c.isActive && (
                          <span className="badge bg-warning-subtle text-warning-emphasis ms-2" style={{ fontSize: '.7rem' }}>Expired</span>
                        )}
                      </td>
                      <td className="text-muted-sm">{c.contractNumber ?? '—'}</td>
                      <td className="text-muted-sm">{fmtDate(c.startDate)}</td>
                      <td className="text-muted-sm" style={{ color: expired ? '#b91c1c' : undefined }}>
                        {c.endDate ? fmtDate(c.endDate) : 'Open-ended'}
                      </td>
                      <td><StatusBadge active={c.isActive} /></td>
                      <td>
                        <div className="gap-actions justify-content-end">
                          <button
                            className="btn btn-sm btn-outline-secondary"
                            onClick={() => toggleExpand(c.contractId)}
                          >
                            {expandedId === c.contractId ? 'Hide Docs' : 'Documents'}
                          </button>
                          <button className="btn btn-sm btn-outline-secondary"
                            onClick={() => setEditContract(c)}>
                            Edit
                          </button>
                          <button
                            className={`btn btn-sm ${c.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`}
                            onClick={() => setConfirmStatus({ contractId: c.contractId, targetActive: !c.isActive })}
                          >
                            {c.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                        </div>
                      </td>
                    </tr>,
                    expandedId === c.contractId && (
                      <tr key={`${c.contractId}-docs`}>
                        <td colSpan={6} style={{ padding: 0 }}>
                          <DocumentPanel orgId={organizationId} contract={c} />
                        </td>
                      </tr>
                    ),
                  ]
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {(showCreate || editContract) && (
        <ContractModal
          orgId={organizationId}
          contract={editContract}
          onClose={() => { setShowCreate(false); setEditContract(null) }}
        />
      )}

      <ConfirmModal
        show={!!confirmStatus}
        title={confirmStatus?.targetActive ? 'Activate Contract' : 'Deactivate Contract'}
        message={
          confirmStatus?.targetActive
            ? 'This contract will become active. If another contract is already active, that one must be deactivated first.'
            : 'This contract will be deactivated. It will remain on record for history.'
        }
        danger={!confirmStatus?.targetActive}
        onConfirm={handleStatusConfirm}
        onCancel={() => setConfirmStatus(null)}
        loading={setStatus.isPending}
      />
    </div>
  )
}
