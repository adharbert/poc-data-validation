import { useRef, useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useIngestionJobs, useUploadIngestionFile, useIngestionJob } from '@/hooks/useApi.js'

// Status → badge colour
const STATUS_CLASS = {
  Pending:        'bg-secondary',
  Processing:     'bg-info text-dark',
  AwaitingReview: 'bg-warning text-dark',
  AwaitingETL:    'bg-warning text-dark',
  Committing:     'bg-info text-dark',
  Complete:       'bg-success',
  Failed:         'bg-danger',
}

const TIER_CLASS = {
  Auto:   'bg-success',
  Review: 'bg-warning text-dark',
  ETL:    'bg-danger',
}

const IN_PROGRESS_STATUSES = new Set(['Pending', 'Processing', 'Committing'])

function StatusBadge({ status }) {
  return (
    <span className={`badge ${STATUS_CLASS[status] ?? 'bg-secondary'}`}>
      {status}
    </span>
  )
}

function TierBadge({ tier }) {
  if (!tier) return null
  return (
    <span className={`badge ms-1 ${TIER_CLASS[tier] ?? 'bg-secondary'}`}>
      {tier}
    </span>
  )
}

// Single row — polls while the job is still in-progress
function JobRow({ orgId, job }) {
  const inProgress = IN_PROGRESS_STATUSES.has(job.status)
  const { data: live } = useIngestionJob(orgId, job.id, {
    refetchInterval: inProgress ? 3000 : false,
  })
  const j = live ?? job

  const canReview = j.status === 'AwaitingReview' || j.status === 'AwaitingETL'

  return (
    <tr>
      <td className="text-truncate" style={{ maxWidth: 240 }} title={j.fileName}>
        {j.fileName}
      </td>
      <td>
        <StatusBadge status={j.status} />
        <TierBadge   tier={j.tier} />
      </td>
      <td className="text-end">{j.totalRows ?? '—'}</td>
      <td className="text-end">
        {j.passedRows != null ? (
          <span>
            <span className="text-success">{j.passedRows}✓</span>
            {j.flaggedRows > 0 && <span className="text-warning ms-1">{j.flaggedRows}⚠</span>}
            {j.failedRows  > 0 && <span className="text-danger  ms-1">{j.failedRows}✗</span>}
          </span>
        ) : '—'}
      </td>
      <td className="text-muted small">
        {new Date(j.uploadedAt).toLocaleString()}
      </td>
      <td>
        {canReview && (
          <Link
            to={`/organizations/${orgId}/ingestion/${j.id}/review`}
            className="btn btn-sm btn-outline-warning"
          >
            Review
          </Link>
        )}
        {j.status === 'Complete' && (
          <Link
            to={`/organizations/${orgId}/ingestion/${j.id}/review`}
            className="btn btn-sm btn-outline-secondary"
          >
            View
          </Link>
        )}
        {j.status === 'Failed' && j.errorMessage && (
          <span className="text-danger small" title={j.errorMessage}>Error ⓘ</span>
        )}
        {inProgress && (
          <span className="spinner-border spinner-border-sm text-info ms-2" role="status" />
        )}
      </td>
    </tr>
  )
}

export default function IngestionPage() {
  const { organizationId } = useParams()
  const fileRef   = useRef()
  const [dragging, setDragging] = useState(false)
  const [feedback, setFeedback] = useState(null)   // { type: 'success'|'error', message }
  const [page, setPage]         = useState(1)

  const { data: jobsPage, isLoading } = useIngestionJobs(organizationId, page)
  const upload = useUploadIngestionFile(organizationId)

  // Auto-clear feedback after 5 s
  useEffect(() => {
    if (!feedback) return
    const t = setTimeout(() => setFeedback(null), 5000)
    return () => clearTimeout(t)
  }, [feedback])

  async function handleFile(file) {
    if (!file) return
    const ext = file.name.split('.').pop().toLowerCase()
    if (!['csv', 'xlsx', 'xls'].includes(ext)) {
      setFeedback({ type: 'error', message: `Unsupported file type: .${ext}. Use CSV or Excel.` })
      return
    }

    const fd = new FormData()
    fd.append('file', file)
    fd.append('uploadedBy', 'Admin')

    try {
      await upload.mutateAsync(fd)
      setFeedback({ type: 'success', message: `${file.name} submitted. Processing will begin shortly.` })
    } catch (err) {
      setFeedback({ type: 'error', message: err.message ?? 'Upload failed.' })
    }
  }

  function onInputChange(e) {
    handleFile(e.target.files?.[0])
    e.target.value = ''
  }

  function onDrop(e) {
    e.preventDefault()
    setDragging(false)
    handleFile(e.dataTransfer.files?.[0])
  }

  const jobs   = jobsPage?.items ?? []
  const total  = jobsPage?.totalCount ?? 0
  const pages  = Math.ceil(total / (jobsPage?.pageSize ?? 20)) || 1

  return (
    <div>
      {/* Upload area */}
      <div className="card mb-4">
        <div className="card-header fw-semibold">Submit File for Ingestion</div>
        <div className="card-body">
          <div
            onClick={() => fileRef.current?.click()}
            onDragOver={e => { e.preventDefault(); setDragging(true) }}
            onDragLeave={() => setDragging(false)}
            onDrop={onDrop}
            style={{
              border: `2px dashed ${dragging ? '#0d6efd' : '#ced4da'}`,
              borderRadius: 8,
              padding: '2.5rem',
              textAlign: 'center',
              cursor: upload.isPending ? 'wait' : 'pointer',
              background: dragging ? 'rgba(13,110,253,.05)' : undefined,
              transition: 'border-color .2s, background .2s',
            }}
          >
            {upload.isPending
              ? <><span className="spinner-border spinner-border-sm me-2" />Uploading…</>
              : <><span style={{ fontSize: '2rem' }}>📤</span><br />Drop a CSV or Excel file here, or <strong>click to browse</strong></>
            }
          </div>
          <input
            ref={fileRef}
            type="file"
            accept=".csv,.xlsx,.xls"
            style={{ display: 'none' }}
            onChange={onInputChange}
          />

          {feedback && (
            <div className={`alert alert-${feedback.type === 'error' ? 'danger' : 'success'} mt-3 mb-0 py-2`}>
              {feedback.message}
            </div>
          )}

          <p className="text-muted small mt-3 mb-0">
            The file is processed automatically. High-confidence matches go straight through;
            lower-confidence jobs are queued for review before data is committed.
          </p>
        </div>
      </div>

      {/* Job history */}
      <div className="card">
        <div className="card-header d-flex justify-content-between align-items-center">
          <span className="fw-semibold">Ingestion History</span>
          <span className="text-muted small">{total} total</span>
        </div>
        <div className="card-body p-0">
          {isLoading ? (
            <div className="p-4 text-center text-muted">Loading…</div>
          ) : jobs.length === 0 ? (
            <div className="p-4 text-center text-muted">No ingestion jobs yet. Upload a file above to get started.</div>
          ) : (
            <table className="table table-hover mb-0">
              <thead className="table-light">
                <tr>
                  <th>File</th>
                  <th>Status</th>
                  <th className="text-end">Rows</th>
                  <th className="text-end">Results</th>
                  <th>Uploaded</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {jobs.map(job => (
                  <JobRow key={job.id} orgId={organizationId} job={job} />
                ))}
              </tbody>
            </table>
          )}
        </div>
        {pages > 1 && (
          <div className="card-footer d-flex gap-2 align-items-center">
            <button className="btn btn-sm btn-outline-secondary" disabled={page <= 1}  onClick={() => setPage(p => p - 1)}>Previous</button>
            <span className="text-muted small">Page {page} of {pages}</span>
            <button className="btn btn-sm btn-outline-secondary" disabled={page >= pages} onClick={() => setPage(p => p + 1)}>Next</button>
          </div>
        )}
      </div>
    </div>
  )
}
