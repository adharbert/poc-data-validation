import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import {
  useOrganization, useProjects, useContracts,
  useCreateProject, useUpdateProject, useSetProjectStatus,
} from '@/hooks/useApi.js'
import {
  PageHeader, LoadingState, ErrorAlert,
  StatusBadge, ConfirmModal, EmptyState, useToast,
} from '@/components/common/index.jsx'
import { fmtDate } from '@/utils/dates.js'

// ─── Constants ────────────────────────────────────────────────────────────────

const PROJECT_TYPES = [
  { value: 'public_university',   label: 'Public University' },
  { value: 'private_university',  label: 'Private University' },
  { value: 'public_high_school',  label: 'Public High School' },
  { value: 'private_high_school', label: 'Private High School' },
  { value: 'fraternities',        label: 'Fraternities' },
  { value: 'sororities',          label: 'Sororities' },
  { value: 'military',            label: 'Military' },
  { value: 'general',             label: 'General' },
  { value: 'story_cause',         label: 'Story Cause' },
]

function projectTypeLabel(value) {
  return PROJECT_TYPES.find(t => t.value === value)?.label ?? value ?? '—'
}

function isEnded(endDate) {
  if (!endDate) return false
  return new Date(endDate) < new Date()
}

function daysUntil(dateStr) {
  if (!dateStr) return null
  return Math.ceil((new Date(dateStr) - Date.now()) / 86_400_000)
}

// ─── Project Modal ────────────────────────────────────────────────────────────

function ProjectModal({ orgId, project, contracts, onClose }) {
  const toast  = useToast()
  const create = useCreateProject(orgId)
  const update = useUpdateProject(orgId)
  const isEdit = !!project

  const { register, handleSubmit, formState: { errors } } = useForm({
    defaultValues: {
      projectName:        project?.projectName        ?? '',
      projectType:        project?.projectType        ?? '',
      marketingStartDate: project?.marketingStartDate ?? '',
      marketingEndDate:   project?.marketingEndDate   ?? '',
      contractId:         project?.contractId         ?? '',
      notes:              project?.notes              ?? '',
      isActive:           project?.isActive           ?? true,
    },
  })

  async function onSubmit(values) {
    const payload = {
      projectName:        values.projectName,
      projectType:        values.projectType || null,
      marketingStartDate: values.marketingStartDate,
      marketingEndDate:   values.marketingEndDate || null,
      contractId:         values.contractId || null,
      notes:              values.notes      || null,
      isActive:           values.isActive,
    }
    try {
      if (isEdit) {
        await update.mutateAsync({ projectId: project.projectId, data: payload })
        toast('Project updated.')
      } else {
        await create.mutateAsync(payload)
        toast('Project created.')
      }
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving project.', 'danger')
    }
  }

  const saving = create.isPending || update.isPending

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">{isEdit ? 'Edit Project' : 'New Marketing Project'}</h5>
              <button type="button" className="btn-close" onClick={onClose} />
            </div>
            <div className="modal-body">
              <div className="row g-3">
                <div className="col-8">
                  <label className="form-label fw-semibold">Project Name <span className="text-danger">*</span></label>
                  <input className={`form-control ${errors.projectName ? 'is-invalid' : ''}`}
                    {...register('projectName', { required: 'Required' })} />
                  {errors.projectName && <div className="invalid-feedback">{errors.projectName.message}</div>}
                </div>
                <div className="col-4">
                  <label className="form-label">Project Type</label>
                  <select className="form-select" {...register('projectType')}>
                    <option value="">— Select —</option>
                    {PROJECT_TYPES.map(t => (
                      <option key={t.value} value={t.value}>{t.label}</option>
                    ))}
                  </select>
                </div>
                <div className="col-6">
                  <label className="form-label fw-semibold">Start Date <span className="text-danger">*</span></label>
                  <input type="date" className={`form-control ${errors.marketingStartDate ? 'is-invalid' : ''}`}
                    {...register('marketingStartDate', { required: 'Required' })} />
                  {errors.marketingStartDate && <div className="invalid-feedback">{errors.marketingStartDate.message}</div>}
                </div>
                <div className="col-6">
                  <label className="form-label">End Date</label>
                  <input type="date" className="form-control" {...register('marketingEndDate')} />
                </div>
                <div className="col-12">
                  <label className="form-label">Link to Contract <span className="text-muted-sm">(optional)</span></label>
                  <select className="form-select" {...register('contractId')}>
                    <option value="">— None —</option>
                    {contracts.map(c => (
                      <option key={c.contractId} value={c.contractId}>{c.contractName}</option>
                    ))}
                  </select>
                </div>
                <div className="col-12">
                  <label className="form-label">Notes</label>
                  <textarea className="form-control" rows={2} {...register('notes')} />
                </div>
                {isEdit && (
                  <div className="col-12">
                    <div className="form-check form-switch">
                      <input className="form-check-input" type="checkbox" id="projectIsActive"
                        {...register('isActive')} />
                      <label className="form-check-label" htmlFor="projectIsActive">Active</label>
                    </div>
                  </div>
                )}
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Project'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function ProjectsPage() {
  const { organizationId } = useParams()
  const [showInactive, setShowInactive]   = useState(false)
  const [showCreate, setShowCreate]       = useState(false)
  const [editProject, setEditProject]     = useState(null)
  const [confirmStatus, setConfirmStatus] = useState(null) // { projectId, targetActive }

  const { data: org }                    = useOrganization(organizationId)
  const { data: projects = [], isLoading, isError } = useProjects(organizationId, showInactive)
  // Contracts list — used for the "link to contract" dropdown in the modal
  const { data: contracts = [] }         = useContracts(organizationId, true)
  const setStatus = useSetProjectStatus(organizationId)
  const toast     = useToast()

  async function handleStatusConfirm() {
    const { projectId, targetActive } = confirmStatus
    try {
      await setStatus.mutateAsync({ projectId, isActive: targetActive })
      toast(targetActive ? 'Project activated.' : 'Project deactivated.')
    } catch (err) {
      toast(err.message ?? 'Failed to update status.', 'danger')
    } finally {
      setConfirmStatus(null)
    }
  }

  if (isLoading) return <LoadingState message="Loading projects…" />
  if (isError)   return <ErrorAlert message="Could not load marketing projects." />

  const activeCount = projects.filter(p => p.isActive).length

  return (
    <div>
      <PageHeader
        breadcrumbs={[
          { label: 'Organizations', href: '/organizations' },
          { label: org?.organizationName ?? '…', href: `/organizations/${organizationId}` },
          { label: 'Marketing Projects' },
        ]}
        title="Marketing Projects"
        subtitle={`${activeCount} active project${activeCount !== 1 ? 's' : ''}`}
        actions={
          <>
            <div className="form-check form-switch mb-0 mx-2">
              <input className="form-check-input" type="checkbox" id="showInactive"
                checked={showInactive} onChange={e => setShowInactive(e.target.checked)} />
              <label className="form-check-label text-muted-sm" htmlFor="showInactive">Show Inactive / Ended</label>
            </div>
            <button className="btn btn-primary btn-sm" onClick={() => setShowCreate(true)}>+ New Project</button>
          </>
        }
      />

      <div className="admin-card p-0">
        {!projects.length ? (
          <EmptyState
            icon="📁"
            title="No marketing projects yet"
            description="Create the first marketing project for this organization."
            action={<button className="btn btn-primary btn-sm" onClick={() => setShowCreate(true)}>+ New Project</button>}
          />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Project Name</th>
                  <th>Type</th>
                  <th>Start Date</th>
                  <th>End Date</th>
                  <th>Status</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {projects.map(p => {
                  const ended = isEnded(p.marketingEndDate)
                  const days  = daysUntil(p.marketingEndDate)
                  return (
                    <tr key={p.projectId}>
                      <td>
                        <span className="fw-semibold">{p.projectName}</span>
                        {ended && p.isActive && (
                          <span className="badge bg-warning-subtle text-warning-emphasis ms-2" style={{ fontSize: '.7rem' }}>Ended</span>
                        )}
                        {!ended && days != null && days <= 30 && (
                          <span className="badge bg-danger-subtle text-danger-emphasis ms-2" style={{ fontSize: '.7rem' }}>
                            {days}d left
                          </span>
                        )}
                      </td>
                      <td className="text-muted-sm">{projectTypeLabel(p.projectType)}</td>
                      <td className="text-muted-sm">{fmtDate(p.marketingStartDate)}</td>
                      <td className="text-muted-sm" style={{ color: ended ? '#b91c1c' : undefined }}>
                        {p.marketingEndDate ? fmtDate(p.marketingEndDate) : 'Ongoing'}
                      </td>
                      <td><StatusBadge active={p.isActive} /></td>
                      <td>
                        <div className="gap-actions justify-content-end">
                          <button className="btn btn-sm btn-outline-secondary"
                            onClick={() => setEditProject(p)}>
                            Edit
                          </button>
                          <button
                            className={`btn btn-sm ${p.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`}
                            onClick={() => setConfirmStatus({ projectId: p.projectId, targetActive: !p.isActive })}
                          >
                            {p.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {(showCreate || editProject) && (
        <ProjectModal
          orgId={organizationId}
          project={editProject}
          contracts={contracts}
          onClose={() => { setShowCreate(false); setEditProject(null) }}
        />
      )}

      <ConfirmModal
        show={!!confirmStatus}
        title={confirmStatus?.targetActive ? 'Activate Project' : 'Deactivate Project'}
        message={
          confirmStatus?.targetActive
            ? 'This project will be set to active.'
            : 'This project will be deactivated. It will remain on record.'
        }
        danger={!confirmStatus?.targetActive}
        onConfirm={handleStatusConfirm}
        onCancel={() => setConfirmStatus(null)}
        loading={setStatus.isPending}
      />
    </div>
  )
}
