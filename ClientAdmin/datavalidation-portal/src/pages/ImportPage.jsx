import { useState, useRef, useCallback } from 'react'
import { useParams, Link } from 'react-router-dom'
import {
  useUploadImport, useSaveMappings, usePreviewImport,
  useExecuteImport, useImportBatch, useImportBatches,
} from '@/hooks/useApi.js'
import { importApi } from '@/api/services.js'
import {
  PageHeader, LoadingState, ErrorAlert, ImportStatusBadge, EmptyState, useToast,
} from '@/components/common/index.jsx'

const STEPS = ['Upload', 'Map Columns', 'Preview', 'Execute', 'Done']

const CUSTOMER_FIELDS = [
  { value: 'skip',        label: '— Skip this column —' },
  { value: 'FirstName',   label: 'First Name',   group: 'Customer Field' },
  { value: 'LastName',    label: 'Last Name',    group: 'Customer Field' },
  { value: 'MiddleName',  label: 'Middle Name',  group: 'Customer Field' },
  { value: 'Email',       label: 'Email',        group: 'Customer Field' },
  { value: 'Phone',       label: 'Phone',        group: 'Customer Field' },
  { value: 'OriginalId',  label: 'Client ID (OriginalId)', group: 'Customer Field' },
]

// ---------------------------------------------------------------------------
// Step indicator
// ---------------------------------------------------------------------------
function WizardSteps({ current }) {
  return (
    <div className="wizard-steps">
      {STEPS.map((s, i) => {
        const state = i < current ? 'done' : i === current ? 'active' : ''
        return (
          <div key={s} className={`wizard-step ${state}`}>
            <span className="step-num">{i < current ? '✓' : i + 1}</span>
            {s}
          </div>
        )
      })}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Step 1 — Upload
// ---------------------------------------------------------------------------
function StepUpload({ orgId, onUploaded }) {
  const toast      = useToast()
  const upload     = useUploadImport(orgId)
  const fileRef    = useRef()
  const [dragOver, setDragOver]           = useState(false)
  const [dupStrategy, setDupStrategy]     = useState('skip')
  const [uploadedBy, setUploadedBy]       = useState('Admin')

  async function handleFile(file) {
    if (!file) return
    const fd = new FormData()
    fd.append('file', file)
    fd.append('uploadedBy', uploadedBy)
    fd.append('duplicateStrategy', dupStrategy)
    try {
      const result = await upload.mutateAsync(fd)
      onUploaded(result)
    } catch (err) {
      toast(err.message ?? 'Upload failed.', 'danger')
    }
  }

  function onDrop(e) {
    e.preventDefault()
    setDragOver(false)
    handleFile(e.dataTransfer.files[0])
  }

  return (
    <div>
      <h2 className="h5 mb-3">Upload File</h2>

      {/* Drop zone */}
      <div
        className={`border-2 border-dashed rounded-3 text-center p-5 mb-4 ${dragOver ? 'border-primary bg-primary bg-opacity-10' : 'border-secondary'}`}
        style={{ cursor: 'pointer', borderStyle: 'dashed', borderWidth: '2px' }}
        onDragOver={e => { e.preventDefault(); setDragOver(true) }}
        onDragLeave={() => setDragOver(false)}
        onDrop={onDrop}
        onClick={() => fileRef.current?.click()}
      >
        <div style={{ fontSize: '2.5rem', marginBottom: '.5rem' }}>📁</div>
        <div className="fw-semibold mb-1">Drop CSV or Excel file here</div>
        <div className="text-muted-sm">or click to browse — max 50 MB</div>
        <input ref={fileRef} type="file" className="d-none"
          accept=".csv,.xlsx,.xls"
          onChange={e => handleFile(e.target.files[0])} />
      </div>

      <div className="row g-3">
        <div className="col-md-6">
          <label className="form-label fw-semibold">Uploaded By</label>
          <input className="form-control" value={uploadedBy} onChange={e => setUploadedBy(e.target.value)} />
        </div>
        <div className="col-md-6">
          <label className="form-label fw-semibold">Duplicate Strategy</label>
          <select className="form-select" value={dupStrategy} onChange={e => setDupStrategy(e.target.value)}>
            <option value="skip">Skip duplicates</option>
            <option value="update">Update existing</option>
            <option value="error">Flag as error</option>
          </select>
          <div className="form-text">How to handle customers already in the system (matched by email or client ID).</div>
        </div>
      </div>

      {upload.isPending && (
        <div className="d-flex align-items-center gap-2 mt-3">
          <div className="spinner-border spinner-border-sm text-primary" />
          <span className="text-muted-sm">Uploading and parsing headers…</span>
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Step 2 — Map Columns
// ---------------------------------------------------------------------------
function StepMapping({ orgId, batch, autoMatches, onSaved }) {
  const toast    = useToast()
  const save     = useSaveMappings(orgId)
  const [mappings, setMappings] = useState(() =>
    autoMatches.map(m => ({
      csvHeader:         m.csvHeader,
      csvColumnIndex:    m.columnIndex ?? 0,
      mappingType:       m.mappingType ?? 'customer_field',
      customerFieldName: m.customerFieldName ?? 'skip',
      fieldDefinitionId: m.fieldDefinitionId ?? null,
      isAutoMatched:     m.isAutoMatched ?? false,
      displayOrder:      m.displayOrder ?? 0,
    }))
  )

  function setMapping(index, key, value) {
    setMappings(prev => prev.map((m, i) => i === index ? { ...m, [key]: value } : m))
  }

  async function handleSave() {
    const payload = {
      mappings: mappings.map(m => ({
        ...m,
        customerFieldName: m.customerFieldName === 'skip' ? null : m.customerFieldName,
      })),
    }
    try {
      await save.mutateAsync({ batchId: batch.batchId, data: payload })
      toast('Mappings saved.')
      onSaved()
    } catch (err) {
      toast(err.message ?? 'Error saving mappings.', 'danger')
    }
  }

  return (
    <div>
      <h2 className="h5 mb-1">Map Columns</h2>
      <p className="text-muted-sm mb-3">
        File: <strong>{batch.fileName}</strong> — {mappings.length} columns detected.
        Auto-matched columns are pre-filled.
      </p>

      <div className="admin-card mb-3 p-0">
        <div style={{ padding: '0 1.25rem' }}>
          {/* Header row */}
          <div className="mapping-row" style={{ fontWeight: 600, fontSize: '.75rem', textTransform: 'uppercase', color: '#6b7280' }}>
            <span>CSV Header</span>
            <span className="mapping-arrow"></span>
            <span>Map To</span>
          </div>

          {mappings.map((m, i) => (
            <div key={m.csvHeader} className="mapping-row">
              <div>
                <span className="font-monospace" style={{ fontSize: '.875rem' }}>{m.csvHeader}</span>
                {m.isAutoMatched && (
                  <span className="ms-2 badge bg-success bg-opacity-75" style={{ fontSize: '.65rem' }}>auto</span>
                )}
              </div>
              <span className="mapping-arrow">→</span>
              <select
                className="form-select form-select-sm"
                value={m.customerFieldName ?? 'skip'}
                onChange={e => setMapping(i, 'customerFieldName', e.target.value)}
              >
                {CUSTOMER_FIELDS.map(f => (
                  <option key={f.value} value={f.value}>{f.label}</option>
                ))}
              </select>
            </div>
          ))}
        </div>
      </div>

      <button className="btn btn-primary" onClick={handleSave} disabled={save.isPending}>
        {save.isPending ? 'Saving…' : 'Save Mappings & Preview →'}
      </button>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Step 3 — Preview
// ---------------------------------------------------------------------------
function StepPreview({ orgId, batchId, onConfirmed }) {
  const toast   = useToast()
  const preview = usePreviewImport(orgId)
  const [previewData, setPreviewData] = useState(null)

  async function runPreview() {
    try {
      const data = await preview.mutateAsync(batchId)
      setPreviewData(data)
    } catch (err) {
      toast(err.message ?? 'Preview failed.', 'danger')
    }
  }

  const rowClass = status =>
    status === 'error'   ? 'preview-row-error'
    : status === 'warning' ? 'preview-row-warning'
    : 'preview-row-ok'

  return (
    <div>
      <h2 className="h5 mb-3">Preview Import</h2>

      {!previewData ? (
        <div>
          <p className="text-muted-sm mb-3">
            Preview validates the first 10 rows and shows any issues before you commit to the full import.
          </p>
          <button className="btn btn-primary" onClick={runPreview} disabled={preview.isPending}>
            {preview.isPending ? <><span className="spinner-border spinner-border-sm me-2" />Running preview…</> : 'Run Preview'}
          </button>
        </div>
      ) : (
        <>
          {/* Summary */}
          <div className="d-flex gap-3 mb-3">
            {[
              { label: 'OK',      count: previewData.okCount,      color: 'success' },
              { label: 'Warning', count: previewData.warningCount, color: 'warning' },
              { label: 'Error',   count: previewData.errorCount,   color: 'danger'  },
            ].map(s => (
              <div key={s.label} className={`badge bg-${s.color} bg-opacity-15 text-${s.color} border border-${s.color} px-3 py-2`}
                style={{ fontSize: '.8rem', fontWeight: 600, borderRadius: '6px' }}>
                {s.count} {s.label}
              </div>
            ))}
          </div>

          <div className="table-wrap admin-card p-0 mb-3">
            <table className="data-table" style={{ fontSize: '.8rem' }}>
              <thead>
                <tr>
                  <th>Row</th>
                  <th>Status</th>
                  {previewData.rows?.[0]?.values && Object.keys(previewData.rows[0].values).map(k => <th key={k}>{k}</th>)}
                  <th>Issues</th>
                </tr>
              </thead>
              <tbody>
                {previewData.rows?.map(row => (
                  <tr key={row.rowNumber} className={rowClass(row.status)}>
                    <td>{row.rowNumber}</td>
                    <td>
                      <span className={`badge ${row.status === 'error' ? 'bg-danger' : row.status === 'warning' ? 'bg-warning text-dark' : 'bg-success'}`}>
                        {row.status}
                      </span>
                    </td>
                    {row.values && Object.values(row.values).map((v, i) => (
                      <td key={i} className="text-muted-sm">{v ?? '—'}</td>
                    ))}
                    <td className="text-muted-sm">{row.errors?.join('; ') ?? ''}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {previewData.errorCount > 0 && (
            <div className="alert alert-warning" style={{ fontSize: '.875rem' }}>
              ⚠️ Some rows have errors and will be skipped during import (or flagged, depending on your duplicate strategy).
            </div>
          )}

          <div className="d-flex gap-2">
            <button className="btn btn-outline-secondary" onClick={runPreview}>Re-run Preview</button>
            <button className="btn btn-primary" onClick={onConfirmed}>
              Looks good — Execute Import →
            </button>
          </div>
        </>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Step 4 — Execute
// ---------------------------------------------------------------------------
function StepExecute({ orgId, batchId, onDone }) {
  const toast    = useToast()
  const execute  = useExecuteImport(orgId)
  const [started, setStarted] = useState(false)

  // Poll batch status while importing
  const { data: batch } = useImportBatch(orgId, batchId)

  async function runExecute() {
    try {
      await execute.mutateAsync(batchId)
      setStarted(true)
      toast('Import started. Polling for completion…')
    } catch (err) {
      toast(err.message ?? 'Failed to start import.', 'danger')
    }
  }

  if (started && batch?.status === 'completed') {
    return (
      <div className="text-center py-4">
        <div style={{ fontSize: '3rem' }}>✅</div>
        <h3 className="h5 mt-2">Import Complete!</h3>
        <p className="text-muted-sm">
          {batch.importedRows} imported, {batch.skippedRows} skipped, {batch.errorRows} errors
        </p>
        <button className="btn btn-primary" onClick={onDone}>View Results →</button>
      </div>
    )
  }

  if (started && batch?.status === 'failed') {
    return (
      <div className="text-center py-4">
        <div style={{ fontSize: '3rem' }}>❌</div>
        <h3 className="h5 mt-2">Import Failed</h3>
        <p className="text-muted-sm">{batch.notes}</p>
        <button className="btn btn-outline-danger" onClick={() => setStarted(false)}>Retry</button>
      </div>
    )
  }

  if (started) {
    return (
      <div className="text-center py-4">
        <div className="spinner-border text-primary mb-3" style={{ width: '3rem', height: '3rem' }} />
        <h3 className="h5">Importing…</h3>
        <p className="text-muted-sm">This may take a moment for large files. Status: <strong>{batch?.status ?? 'importing'}</strong></p>
        <p className="text-muted-sm">The page will update automatically.</p>
      </div>
    )
  }

  return (
    <div>
      <h2 className="h5 mb-3">Execute Import</h2>
      <p className="text-muted-sm mb-4">
        The import will run in the background. You can monitor progress here or navigate away
        and check the import history later.
      </p>
      <div className="alert alert-info" style={{ fontSize: '.875rem' }}>
        ℹ️ The process runs server-side. Large files may take several minutes.
      </div>
      <button className="btn btn-primary btn-lg" onClick={runExecute} disabled={execute.isPending}>
        {execute.isPending ? 'Starting…' : '🚀 Start Import'}
      </button>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Step 5 — Results
// ---------------------------------------------------------------------------
function StepResults({ orgId, batchId }) {
  const { data: batch } = useImportBatch(orgId, batchId)
  const [errors, setErrors] = useState(null)
  const [loadingErrors, setLoadingErrors] = useState(false)

  async function loadErrors() {
    setLoadingErrors(true)
    try {
      const data = await importApi.getErrors(orgId, batchId)
      setErrors(data)
    } finally {
      setLoadingErrors(false)
    }
  }

  if (!batch) return <LoadingState />

  return (
    <div>
      <div className="text-center mb-4">
        <div style={{ fontSize: '3rem' }}>✅</div>
        <h2 className="h4 mt-2">Import Complete</h2>
        <p className="text-muted-sm">{batch.fileName}</p>
      </div>

      <div className="row g-3 mb-4">
        {[
          { label: 'Total Rows',  value: batch.totalRows,    color: '#dbeafe', text: '#1e40af' },
          { label: 'Imported',    value: batch.importedRows, color: '#d1fae5', text: '#065f46' },
          { label: 'Skipped',     value: batch.skippedRows,  color: '#fef3c7', text: '#92400e' },
          { label: 'Errors',      value: batch.errorRows,    color: '#fee2e2', text: '#991b1b' },
        ].map(s => (
          <div key={s.label} className="col-6 col-md-3">
            <div className="p-3 rounded-3 text-center" style={{ background: s.color }}>
              <div style={{ fontSize: '1.75rem', fontWeight: 700, color: s.text }}>{s.value ?? 0}</div>
              <div style={{ fontSize: '.8rem', color: s.text, fontWeight: 500 }}>{s.label}</div>
            </div>
          </div>
        ))}
      </div>

      {(batch.errorRows ?? 0) > 0 && (
        <div className="mb-3">
          <button className="btn btn-sm btn-outline-danger" onClick={loadErrors} disabled={loadingErrors}>
            {loadingErrors ? 'Loading…' : 'View Error Rows'}
          </button>
          {errors && (
            <div className="mt-2 admin-card p-0">
              <div className="table-wrap">
                <table className="data-table" style={{ fontSize: '.8rem' }}>
                  <thead><tr><th>Row</th><th>Error Type</th><th>Message</th><th>Raw Data</th></tr></thead>
                  <tbody>
                    {errors.map(e => (
                      <tr key={e.errorId}>
                        <td>{e.rowNumber}</td>
                        <td><span className="badge bg-danger">{e.errorType}</span></td>
                        <td className="text-muted-sm">{e.errorMessage}</td>
                        <td style={{ fontFamily: 'monospace', fontSize: '.75rem', maxWidth: '200px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{e.rawData}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}

      <div className="d-flex gap-2">
        <Link to={`/organizations/${orgId}/customers`} className="btn btn-primary">View Customers</Link>
        <Link to={`/organizations/${orgId}/import`}    className="btn btn-outline-secondary">New Import</Link>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Import history list
// ---------------------------------------------------------------------------
function ImportHistory({ orgId }) {
  const { data: paged, isLoading } = useImportBatches(orgId)
  const batches = paged?.items ?? paged ?? []

  if (isLoading) return <LoadingState message="Loading history…" />

  return (
    <div className="admin-card p-0 mt-4">
      <div className="px-4 pt-3 pb-2 border-bottom" style={{ fontWeight: 600, fontSize: '.9rem' }}>Import History</div>
      {!batches.length ? (
        <EmptyState icon="📋" title="No imports yet" description="Complete your first import above." />
      ) : (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr><th>File</th><th>Status</th><th>Rows</th><th>Imported</th><th>Errors</th><th>Uploaded</th></tr>
            </thead>
            <tbody>
              {batches.slice(0, 10).map(b => (
                <tr key={b.batchId}>
                  <td className="fw-semibold text-muted-sm" style={{ maxWidth: 180, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{b.fileName}</td>
                  <td><ImportStatusBadge status={b.status} /></td>
                  <td className="text-muted-sm">{b.totalRows ?? '—'}</td>
                  <td className="text-muted-sm">{b.importedRows ?? '—'}</td>
                  <td className="text-muted-sm">{b.errorRows ?? '—'}</td>
                  <td className="text-muted-sm">{b.uploadedAt ? new Date(b.uploadedAt).toLocaleDateString() : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Main ImportPage
// ---------------------------------------------------------------------------
export default function ImportPage() {
  const { organizationId } = useParams()
  const [step, setStep]         = useState(0)
  const [uploadResult, setUploadResult] = useState(null)

  function handleUploaded(result) {
    setUploadResult(result)
    setStep(1)
  }

  function handleMappingsSaved() {
    setStep(2)
  }

  function handlePreviewConfirmed() {
    setStep(3)
  }

  function handleExecuteDone() {
    setStep(4)
  }

  function reset() {
    setStep(0)
    setUploadResult(null)
  }

  return (
    <div>
      <PageHeader
        breadcrumbs={[
          { label: 'Organisations', href: '/organizations' },
          { label: 'Import' },
        ]}
        title="Import Customers"
        subtitle="Upload a CSV or Excel file to import or update customer records"
        actions={
          <Link to={`/organizations/${organizationId}/import-staging`} className="btn btn-sm btn-outline-secondary">
            🔧 View Staging
          </Link>
        }
      />

      <div className="admin-card mb-4">
        <WizardSteps current={step} />

        {step === 0 && (
          <StepUpload orgId={organizationId} onUploaded={handleUploaded} />
        )}
        {step === 1 && uploadResult && (
          <StepMapping
            orgId={organizationId}
            batch={uploadResult}
            autoMatches={uploadResult.columnMatches ?? []}
            onSaved={handleMappingsSaved}
          />
        )}
        {step === 2 && uploadResult && (
          <StepPreview
            orgId={organizationId}
            batchId={uploadResult.batchId}
            onConfirmed={handlePreviewConfirmed}
          />
        )}
        {step === 3 && uploadResult && (
          <StepExecute
            orgId={organizationId}
            batchId={uploadResult.batchId}
            onDone={handleExecuteDone}
          />
        )}
        {step === 4 && uploadResult && (
          <StepResults orgId={organizationId} batchId={uploadResult.batchId} />
        )}

        {step > 0 && step < 4 && (
          <div className="mt-3 pt-3 border-top">
            <button className="btn btn-sm btn-link text-muted p-0" onClick={reset}>← Start over with a new file</button>
          </div>
        )}
      </div>

      {step === 0 && <ImportHistory orgId={organizationId} />}
    </div>
  )
}
