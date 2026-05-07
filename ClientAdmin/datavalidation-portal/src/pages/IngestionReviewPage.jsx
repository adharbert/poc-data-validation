import { useState, useMemo } from 'react'
import { useParams, Link } from 'react-router-dom'
import {
  useIngestionJob,
  useIngestionStagingRows,
  useReviewStagingRow,
  useCommitIngestionJob,
} from '@/hooks/useApi.js'

// ----------------------------------------------------------------
// Helpers
// ----------------------------------------------------------------

const STATUS_CLASS = {
  Pending:   'bg-secondary',
  Pass:      'bg-success',
  Flagged:   'bg-warning text-dark',
  Rejected:  'bg-danger',
  Committed: 'bg-light text-muted border',
}

const CONFIDENCE_COLOR = (score) => {
  if (score == null) return 'text-muted'
  if (score >= 0.92) return 'text-success'
  if (score >= 0.75) return 'text-warning'
  return 'text-danger'
}

function parseRowJson(json) {
  try { return JSON.parse(json) } catch { return {} }
}

function CustomerSummary({ data }) {
  const c = data?.customer ?? {}
  const a = data?.address  ?? {}
  const name = [c.firstName, c.lastName].filter(Boolean).join(' ') || '—'
  const addr = [a.addressLine1, a.city, a.state].filter(Boolean).join(', ')
  return (
    <div style={{ fontSize: '.875rem' }}>
      <div className="fw-semibold">{name}</div>
      {c.email  && <div className="text-muted">{c.email}</div>}
      {c.phone  && <div className="text-muted">{c.phone}</div>}
      {addr     && <div className="text-muted">{addr}</div>}
      {data?.fieldValues?.length > 0 && (
        <div className="text-muted small mt-1">{data.fieldValues.length} custom field{data.fieldValues.length !== 1 ? 's' : ''}</div>
      )}
    </div>
  )
}

function FlagReasons({ json }) {
  if (!json) return null
  let reasons = []
  try { reasons = JSON.parse(json) } catch { return null }
  if (!reasons.length) return null
  return (
    <ul className="mb-0 ps-3" style={{ fontSize: '.8rem' }}>
      {reasons.map((r, i) => <li key={i} className="text-warning">{r}</li>)}
    </ul>
  )
}

function StagingRow({ row, onApprove, onReject, busy }) {
  const data       = useMemo(() => parseRowJson(row.rowJson), [row.rowJson])
  const reviewable = row.status === 'Pending' || row.status === 'Flagged'

  return (
    <tr>
      <td className="text-muted text-end pe-3" style={{ width: 50 }}>{row.rowNumber}</td>
      <td><CustomerSummary data={data} /></td>
      <td>
        <FlagReasons json={row.flagReasons} />
      </td>
      <td className="text-end" style={{ width: 80 }}>
        {row.confidenceScore != null && (
          <span className={`fw-semibold ${CONFIDENCE_COLOR(row.confidenceScore)}`}>
            {Math.round(row.confidenceScore * 100)}%
          </span>
        )}
      </td>
      <td style={{ width: 90 }}>
        <span className={`badge ${STATUS_CLASS[row.status] ?? 'bg-secondary'}`}>
          {row.status}
        </span>
      </td>
      <td style={{ width: 110 }}>
        {reviewable && (
          <div className="d-flex gap-1">
            <button
              className="btn btn-sm btn-outline-success py-0"
              disabled={busy}
              onClick={() => onApprove(row.id)}
              title="Approve"
            >✓</button>
            <button
              className="btn btn-sm btn-outline-danger py-0"
              disabled={busy}
              onClick={() => onReject(row.id)}
              title="Reject"
            >✗</button>
          </div>
        )}
        {row.status === 'Committed' && <span className="text-muted small">Done</span>}
      </td>
    </tr>
  )
}

// ----------------------------------------------------------------
// Main page
// ----------------------------------------------------------------

const STATUS_FILTERS = ['All', 'Flagged', 'Pass', 'Pending', 'Rejected', 'Committed']

export default function IngestionReviewPage() {
  const { organizationId, jobId } = useParams()
  const [statusFilter, setStatusFilter] = useState('All')
  const [page, setPage] = useState(1)
  const [rejectTarget, setRejectTarget] = useState(null)  // rowId awaiting reason input
  const [rejectReason, setRejectReason] = useState('')

  const inProgressRefetch = (status) =>
    ['Pending', 'Processing', 'Committing'].includes(status) ? 3000 : false

  const { data: job, isLoading: jobLoading } = useIngestionJob(organizationId, jobId, {
    refetchInterval: (q) => inProgressRefetch(q?.data?.status),
  })

  const apiStatus = statusFilter === 'All' ? undefined : statusFilter

  const { data: stagingPage, isLoading: rowsLoading } = useIngestionStagingRows(
    organizationId, jobId, { status: apiStatus, page, pageSize: 50 }
  )

  const reviewRow = useReviewStagingRow(organizationId, jobId)
  const commit    = useCommitIngestionJob(organizationId, jobId)

  const rows  = stagingPage?.items ?? []
  const total = stagingPage?.totalCount ?? 0
  const pages = Math.ceil(total / (stagingPage?.pageSize ?? 50)) || 1

  const canCommit = job && (job.status === 'AwaitingReview' || job.status === 'AwaitingETL') && !commit.isPending

  async function handleApprove(rowId) {
    await reviewRow.mutateAsync({ rowId, action: 'approve', reviewedBy: 'Admin' })
  }

  function startReject(rowId) {
    setRejectTarget(rowId)
    setRejectReason('')
  }

  async function confirmReject() {
    if (!rejectTarget) return
    await reviewRow.mutateAsync({ rowId: rejectTarget, action: 'reject', reviewedBy: 'Admin', reason: rejectReason || undefined })
    setRejectTarget(null)
    setRejectReason('')
  }

  async function handleApproveAll() {
    const reviewable = rows.filter(r => r.status === 'Pending' || r.status === 'Flagged')
    for (const r of reviewable) {
      await reviewRow.mutateAsync({ rowId: r.id, action: 'approve', reviewedBy: 'Admin' })
    }
  }

  async function handleCommit() {
    await commit.mutateAsync('Admin')
  }

  if (jobLoading) return <div className="text-muted p-4">Loading…</div>
  if (!job) return <div className="alert alert-danger">Job not found.</div>

  return (
    <div>
      {/* Header */}
      <div className="d-flex align-items-start justify-content-between mb-3 flex-wrap gap-2">
        <div>
          <Link to={`/organizations/${organizationId}/ingestion`} className="text-muted small me-2">← Back</Link>
          <h5 className="mb-1 d-inline">{job.fileName}</h5>
          <div className="mt-1">
            <span className={`badge me-1 ${job.status === 'Complete' ? 'bg-success' : job.status === 'Failed' ? 'bg-danger' : job.status.startsWith('Awaiting') ? 'bg-warning text-dark' : 'bg-secondary'}`}>
              {job.status}
            </span>
            {job.tier && (
              <span className={`badge me-2 ${job.tier === 'Auto' ? 'bg-success' : job.tier === 'Review' ? 'bg-warning text-dark' : 'bg-danger'}`}>
                {job.tier}
              </span>
            )}
            {job.totalRows != null && (
              <span className="text-muted small">
                {job.totalRows} rows
                {job.passedRows != null && <> · <span className="text-success">{job.passedRows} passed</span></>}
                {job.flaggedRows > 0 && <> · <span className="text-warning">{job.flaggedRows} flagged</span></>}
                {job.failedRows  > 0 && <> · <span className="text-danger">{job.failedRows} failed</span></>}
              </span>
            )}
          </div>
        </div>

        <div className="d-flex gap-2">
          {canCommit && (
            <>
              <button
                className="btn btn-sm btn-outline-success"
                onClick={handleApproveAll}
                disabled={reviewRow.isPending}
              >
                Approve All Visible
              </button>
              <button
                className="btn btn-sm btn-success"
                onClick={handleCommit}
                disabled={commit.isPending}
              >
                {commit.isPending
                  ? <><span className="spinner-border spinner-border-sm me-1" />Committing…</>
                  : 'Commit to Database'}
              </button>
            </>
          )}
          {job.status === 'Complete' && (
            <span className="badge bg-success align-self-center">All rows committed</span>
          )}
        </div>
      </div>

      {job.errorMessage && (
        <div className="alert alert-danger py-2 small mb-3">{job.errorMessage}</div>
      )}

      {commit.isSuccess && (
        <div className="alert alert-success py-2 mb-3">Commit complete. Rows have been written to customer tables.</div>
      )}

      {/* Filter tabs */}
      <div className="mb-3">
        <ul className="nav nav-tabs">
          {STATUS_FILTERS.map(f => (
            <li key={f} className="nav-item">
              <button
                className={`nav-link py-1 px-3 ${statusFilter === f ? 'active' : ''}`}
                onClick={() => { setStatusFilter(f); setPage(1) }}
              >
                {f}
              </button>
            </li>
          ))}
        </ul>
      </div>

      {/* Reject reason modal */}
      {rejectTarget && (
        <div className="modal d-block" style={{ background: 'rgba(0,0,0,.4)' }} tabIndex="-1">
          <div className="modal-dialog modal-sm">
            <div className="modal-content">
              <div className="modal-header py-2">
                <h6 className="modal-title mb-0">Reject Row</h6>
                <button className="btn-close btn-sm" onClick={() => setRejectTarget(null)} />
              </div>
              <div className="modal-body py-2">
                <label className="form-label small">Reason (optional)</label>
                <input
                  className="form-control form-control-sm"
                  value={rejectReason}
                  onChange={e => setRejectReason(e.target.value)}
                  placeholder="e.g. Duplicate, bad data…"
                  autoFocus
                  onKeyDown={e => e.key === 'Enter' && confirmReject()}
                />
              </div>
              <div className="modal-footer py-2 gap-2">
                <button className="btn btn-sm btn-secondary" onClick={() => setRejectTarget(null)}>Cancel</button>
                <button className="btn btn-sm btn-danger" onClick={confirmReject} disabled={reviewRow.isPending}>
                  Reject
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Staging rows table */}
      <div className="card">
        <div className="card-header d-flex justify-content-between align-items-center py-2">
          <span className="small fw-semibold">{total} row{total !== 1 ? 's' : ''}{statusFilter !== 'All' ? ` · ${statusFilter}` : ''}</span>
          {rowsLoading && <span className="spinner-border spinner-border-sm text-secondary" />}
        </div>
        <div className="card-body p-0">
          {rows.length === 0 && !rowsLoading ? (
            <div className="p-4 text-center text-muted">No rows match the current filter.</div>
          ) : (
            <table className="table table-hover mb-0 align-middle">
              <thead className="table-light">
                <tr>
                  <th className="text-end pe-3" style={{ width: 50 }}>#</th>
                  <th>Customer Data</th>
                  <th>Flags</th>
                  <th className="text-end" style={{ width: 80 }}>Confidence</th>
                  <th style={{ width: 90 }}>Status</th>
                  <th style={{ width: 110 }}></th>
                </tr>
              </thead>
              <tbody>
                {rows.map(row => (
                  <StagingRow
                    key={row.id}
                    row={row}
                    onApprove={handleApprove}
                    onReject={startReject}
                    busy={reviewRow.isPending}
                  />
                ))}
              </tbody>
            </table>
          )}
        </div>
        {pages > 1 && (
          <div className="card-footer d-flex gap-2 align-items-center py-2">
            <button className="btn btn-sm btn-outline-secondary" disabled={page <= 1}    onClick={() => setPage(p => p - 1)}>Previous</button>
            <span className="text-muted small">Page {page} of {pages}</span>
            <button className="btn btn-sm btn-outline-secondary" disabled={page >= pages} onClick={() => setPage(p => p + 1)}>Next</button>
          </div>
        )}
      </div>
    </div>
  )
}
