import { useState, useRef, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import {
  useOrganization, useFields, useSections,
  useUploadImport, useSaveMappings, usePreviewImport,
  useExecuteImport, useImportBatch, useImportBatches,
  useResumeImport, useCancelImport, useDeleteImport,
  useResetImport, useValueMapping, useSaveAliases,
} from '@/hooks/useApi.js'
import { useImportHub } from '@/hooks/useImportHub.js'
import { importApi, fieldApi } from '@/api/services.js'
import {
  PageHeader, LoadingState, ImportStatusBadge, EmptyState, useToast,
} from '@/components/common/index.jsx'
import { fmtDate } from '@/utils/dates.js'

const STEPS = ['Upload', 'Map Columns', 'Value Mapping', 'Preview', 'Execute', 'Done']

const DEST_TABLES = [
  { value: 'skip',             label: '— Skip —' },
  { value: 'customer',         label: 'Customer' },
  { value: 'customer_address', label: 'Address' },
  { value: 'customer_email',   label: 'Email' },
  { value: 'customer_phone',   label: 'Phone' },
  { value: 'field_value',      label: 'Key / Value Field' },
]

const CUSTOMER_FIELDS = [
  { value: 'FirstName',    label: 'First Name' },
  { value: 'LastName',     label: 'Last Name' },
  { value: 'MiddleName',   label: 'Middle Name' },
  { value: 'MaidenName',   label: 'Maiden Name' },
  { value: 'DateOfBirth',  label: 'Date of Birth' },
  { value: 'Email',        label: 'Email' },
  { value: 'Phone',        label: 'Phone' },
  { value: 'OriginalId',   label: 'Client ID (OriginalId)' },
  { value: 'CustomerCode', label: 'Customer Code' },
]

const ADDRESS_FIELDS = [
  { value: 'AddressLine1', label: 'Address Line 1' },
  { value: 'AddressLine2', label: 'Address Line 2' },
  { value: 'City',         label: 'City' },
  { value: 'State',        label: 'State' },
  { value: 'PostalCode',   label: 'Postal Code' },
  { value: 'Country',      label: 'Country' },
  { value: 'AddressType',  label: 'Address Type' },
  { value: 'Latitude',     label: 'Latitude' },
  { value: 'Longitude',    label: 'Longitude' },
]

const EMAIL_FIELDS = [
  { value: 'EmailAddress', label: 'Email Address' },
  { value: 'EmailType',    label: 'Email Type (personal / work / other)' },
  { value: 'IsPrimary',    label: 'Is Primary (1/0)' },
]

const PHONE_FIELDS = [
  { value: 'PhoneNumber', label: 'Phone Number' },
  { value: 'PhoneType',   label: 'Phone Type (mobile / home / work / fax / other)' },
  { value: 'IsPrimary',   label: 'Is Primary (1/0)' },
]

const FIELD_TYPES = [
  { value: 'text',        label: 'Text' },
  { value: 'number',      label: 'Number' },
  { value: 'date',        label: 'Date' },
  { value: 'checkbox',    label: 'Yes / No' },
  { value: 'dropdown',    label: 'Dropdown' },
  { value: 'multiselect', label: 'Multi-select' },
  { value: 'phone',       label: 'Phone' },
]

const DEFAULT_SPLIT_FULL_NAME_OUTPUTS = [
  { outputToken: 'FirstName',   destinationTable: 'customer', destinationField: 'FirstName',  sortOrder: 0 },
  { outputToken: 'MiddleName',  destinationTable: 'customer', destinationField: 'MiddleName', sortOrder: 1 },
  { outputToken: 'LastName',    destinationTable: 'customer', destinationField: 'LastName',   sortOrder: 2 },
  { outputToken: 'Suffix',      destinationTable: 'skip',     destinationField: null,         sortOrder: 3 },
  { outputToken: 'Credentials', destinationTable: 'skip',     destinationField: null,         sortOrder: 4 },
]

const DEFAULT_SPLIT_FULL_ADDRESS_OUTPUTS = [
  { outputToken: 'AddressLine1', destinationTable: 'customer_address', destinationField: 'AddressLine1', sortOrder: 0 },
  { outputToken: 'AddressLine2', destinationTable: 'customer_address', destinationField: 'AddressLine2', sortOrder: 1 },
  { outputToken: 'City',         destinationTable: 'customer_address', destinationField: 'City',         sortOrder: 2 },
  { outputToken: 'State',        destinationTable: 'customer_address', destinationField: 'State',        sortOrder: 3 },
  { outputToken: 'PostalCode',   destinationTable: 'customer_address', destinationField: 'PostalCode',   sortOrder: 4 },
  { outputToken: 'Country',      destinationTable: 'customer_address', destinationField: 'Country',      sortOrder: 5 },
]

function fieldsForTable(table) {
  if (table === 'customer')         return CUSTOMER_FIELDS
  if (table === 'customer_address') return ADDRESS_FIELDS
  if (table === 'customer_email')   return EMAIL_FIELDS
  if (table === 'customer_phone')   return PHONE_FIELDS
  return []
}

function slugify(label) {
  return label.toLowerCase().replace(/\s+/g, '_').replace(/[^a-z0-9_]/g, '')
}

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

// Inline "Create new field" mini-form shown when user picks the create option
function CreateFieldInline({ orgId, onCreated, onCancel }) {
  const toast = useToast()
  const { data: sections } = useSections(orgId)
  const [label,     setLabel]     = useState('')
  const [type,      setType]      = useState('text')
  const [sectionId, setSectionId] = useState('')
  const [saving,    setSaving]    = useState(false)

  async function handleCreate() {
    if (!label.trim()) return
    setSaving(true)
    try {
      const created = await fieldApi.create(orgId, {
        organizationId:  orgId,
        sectionId:       sectionId ? Number(sectionId) : null,
        fieldKey:        slugify(label.trim()),
        fieldLabel:      label.trim(),
        fieldType:       type,
        placeholderText: null,
        helpText:        null,
      })
      onCreated(created)
    } catch (err) {
      toast(err.message ?? 'Could not create field.', 'danger')
    } finally {
      setSaving(false)
    }
  }

  const derivedKey = slugify(label.trim())

  return (
    <div className="mt-1 ms-1 p-2 border rounded-2 bg-light" style={{ fontSize: '.8rem' }}>
      <div className="d-flex align-items-center gap-2 flex-wrap">
        <input
          className="form-control form-control-sm"
          style={{ width: 180 }}
          placeholder="Field label"
          value={label}
          onChange={e => setLabel(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleCreate()}
          autoFocus
        />
        <select className="form-select form-select-sm" style={{ width: 130 }} value={type} onChange={e => setType(e.target.value)}>
          {FIELD_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
        </select>
        <select className="form-select form-select-sm" style={{ width: 150 }} value={sectionId} onChange={e => setSectionId(e.target.value)}>
          <option value="">No section</option>
          {(sections ?? []).map(s => <option key={s.sectionId} value={s.sectionId}>{s.sectionName}</option>)}
        </select>
        <button className="btn btn-sm btn-primary" onClick={handleCreate} disabled={saving || !label.trim()}>
          {saving ? '…' : 'Create'}
        </button>
        <button className="btn btn-sm btn-link text-muted p-0" onClick={onCancel}>Cancel</button>
      </div>
      {derivedKey && (
        <div className="mt-1 text-muted" style={{ fontSize: '.75rem' }}>
          Field name: <code>{derivedKey}</code>
        </div>
      )}
    </div>
  )
}

// A compact secondary destination row shown below the primary mapping
function ExtraMappingRow({ extra, orgId, fieldDefs, onChange, onRemove, onFieldCreated }) {
  const fields = fieldsForTable(extra.destinationTable)
  const [creating, setCreating] = useState(false)

  function handleCreated(newField) {
    setCreating(false)
    onFieldCreated?.(newField)
    onChange({ fieldDefinitionId: newField.fieldDefinitionId })
  }

  return (
    <div className="ms-4 mt-1">
      <div className="d-flex align-items-center gap-2 flex-wrap">
        <span className="text-muted" style={{ fontSize: '.75rem', minWidth: 60 }}>also →</span>

        <select
          className="form-select form-select-sm"
          style={{ width: 'auto', minWidth: 140 }}
          value={extra.destinationTable}
          onChange={e => onChange({ destinationTable: e.target.value, destinationField: null, fieldDefinitionId: null })}
        >
          {DEST_TABLES.filter(t => t.value !== 'skip').map(t => (
            <option key={t.value} value={t.value}>{t.label}</option>
          ))}
        </select>

        {(extra.destinationTable === 'customer' || extra.destinationTable === 'customer_address') && (
          <select
            className="form-select form-select-sm"
            style={{ width: 'auto', minWidth: 150 }}
            value={extra.destinationField ?? ''}
            onChange={e => onChange({ destinationField: e.target.value || null })}
          >
            <option value="">— Select field —</option>
            {fields.map(f => <option key={f.value} value={f.value}>{f.label}</option>)}
          </select>
        )}

        {extra.destinationTable === 'field_value' && (
          <select
            className="form-select form-select-sm"
            style={{ width: 'auto', minWidth: 180 }}
            value={creating ? '__create__' : (extra.fieldDefinitionId ?? '')}
            onChange={e => {
              if (e.target.value === '__create__') { setCreating(true) }
              else { setCreating(false); onChange({ fieldDefinitionId: e.target.value || null }) }
            }}
          >
            <option value="">— Select field —</option>
            {fieldDefs.map(fd => <option key={fd.fieldDefinitionId} value={fd.fieldDefinitionId}>{fd.fieldLabel}</option>)}
            <option value="__create__">+ Create new field…</option>
          </select>
        )}

        <button type="button" className="btn btn-sm btn-outline-danger ms-1" style={{ padding: '1px 6px', fontSize: '.75rem' }} onClick={onRemove}>✕</button>
      </div>

      {creating && extra.destinationTable === 'field_value' && (
        <CreateFieldInline orgId={orgId} onCreated={handleCreated} onCancel={() => setCreating(false)} />
      )}
    </div>
  )
}

function MappingRow({ m, index, orgId, fieldDefs, onChange, onAddExtra, onChangeExtra, onRemoveExtra, onFieldCreated }) {
  const [showOutputs, setShowOutputs] = useState(false)
  const [creating, setCreating]       = useState(false)

  function handleTableChange(table) {
    const update = { destinationTable: table, destinationField: null, fieldDefinitionId: null, transformType: 'direct', outputs: [] }
    onChange(index, update)
  }

  function handleFieldChange(field) {
    onChange(index, { destinationField: field })
  }

  function handleTransformChange(transform) {
    const outputs =
      transform === 'split_full_name'    ? DEFAULT_SPLIT_FULL_NAME_OUTPUTS    :
      transform === 'split_full_address' ? DEFAULT_SPLIT_FULL_ADDRESS_OUTPUTS :
      []
    onChange(index, { transformType: transform, destinationField: null, outputs })
  }

  function handleFieldDefChange(id) {
    if (id === '__create__') { setCreating(true); return }
    setCreating(false)
    onChange(index, { fieldDefinitionId: id || null })
  }

  function handleOutputFieldChange(tokenIndex, field) {
    const outputs = m.outputs.map((o, ti) =>
      ti === tokenIndex ? { ...o, destinationField: field || null, destinationTable: field ? o.destinationTable : 'skip' } : o
    )
    onChange(index, { outputs })
  }

  function handleCreated(newField) {
    setCreating(false)
    onFieldCreated?.(newField)
    onChange(index, { fieldDefinitionId: newField.fieldDefinitionId })
  }

  const isSplit      = m.transformType === 'split_full_name' || m.transformType === 'split_full_address'
  const isNameSplit  = m.transformType === 'split_full_name'
  const fields       = fieldsForTable(m.destinationTable)

  return (
    <div className="border-bottom py-2">
      <div className="d-flex align-items-center gap-2 flex-wrap">
        {/* CSV header */}
        <div style={{ minWidth: 160, flex: '0 0 auto' }}>
          <span className="font-monospace" style={{ fontSize: '.85rem' }}>{m.csvHeader}</span>
          {m.isAutoMatched && (
            <span className="ms-2 badge bg-success bg-opacity-75" style={{ fontSize: '.6rem' }}>auto</span>
          )}
        </div>

        <span className="text-muted" style={{ fontSize: '.8rem' }}>→</span>

        {/* Destination table */}
        <select
          className="form-select form-select-sm"
          style={{ width: 'auto', minWidth: 140 }}
          value={m.destinationTable}
          onChange={e => handleTableChange(e.target.value)}
        >
          {DEST_TABLES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
        </select>

        {/* Destination field — hidden for field_value and skip */}
        {m.destinationTable === 'customer' || m.destinationTable === 'customer_address' ? (
          !isSplit ? (
            <select
              className="form-select form-select-sm"
              style={{ width: 'auto', minWidth: 150 }}
              value={m.destinationField ?? ''}
              onChange={e => handleFieldChange(e.target.value || null)}
            >
              <option value="">— Select field —</option>
              {fields.map(f => <option key={f.value} value={f.value}>{f.label}</option>)}
            </select>
          ) : (
            <span className="badge bg-primary bg-opacity-15 text-primary border border-primary px-2 py-1" style={{ fontSize: '.75rem' }}>
              {isNameSplit ? 'Split → First / Middle / Last' : 'Split → Street / City / State / ZIP'}
            </span>
          )
        ) : null}

        {/* FieldDefinition picker for field_value — with create option */}
        {m.destinationTable === 'field_value' && (
          <select
            className="form-select form-select-sm"
            style={{ width: 'auto', minWidth: 180 }}
            value={creating ? '__create__' : (m.fieldDefinitionId ?? '')}
            onChange={e => handleFieldDefChange(e.target.value)}
          >
            <option value="">— Select field —</option>
            {fieldDefs.map(fd => <option key={fd.fieldDefinitionId} value={fd.fieldDefinitionId}>{fd.fieldLabel}</option>)}
            <option value="__create__">+ Create new field…</option>
          </select>
        )}

        {/* Transform toggle — customer and customer_address */}
        {(m.destinationTable === 'customer' || m.destinationTable === 'customer_address') && (
          <select
            className="form-select form-select-sm"
            style={{ width: 'auto', minWidth: 150 }}
            value={m.transformType}
            onChange={e => handleTransformChange(e.target.value)}
          >
            <option value="direct">Direct (1:1)</option>
            {m.destinationTable === 'customer'         && <option value="split_full_name">Split Full Name</option>}
            {m.destinationTable === 'customer_address' && <option value="split_full_address">Split Full Address</option>}
          </select>
        )}

        {/* Expand outputs for split transforms */}
        {isSplit && (
          <button
            type="button"
            className="btn btn-sm btn-link p-0 text-muted"
            style={{ fontSize: '.75rem' }}
            onClick={() => setShowOutputs(v => !v)}
          >
            {showOutputs ? '▲ Hide' : '▼ Outputs'}
          </button>
        )}
      </div>

      {/* Inline create-field form */}
      {creating && m.destinationTable === 'field_value' && (
        <CreateFieldInline orgId={orgId} onCreated={handleCreated} onCancel={() => setCreating(false)} />
      )}

      {/* Split output token assignments */}
      {isSplit && showOutputs && (
        <div className="ms-4 mt-2 ps-3 border-start" style={{ borderColor: '#e5e7eb' }}>
          {m.outputs.map((o, ti) => (
            <div key={o.outputToken} className="d-flex align-items-center gap-2 mb-1" style={{ fontSize: '.8rem' }}>
              <span className="font-monospace text-muted" style={{ minWidth: 100 }}>{o.outputToken}</span>
              <span className="text-muted">→</span>
              <select
                className="form-select form-select-sm"
                style={{ width: 'auto', minWidth: 150 }}
                value={o.destinationField ?? ''}
                onChange={e => handleOutputFieldChange(ti, e.target.value)}
              >
                <option value="">— Skip —</option>
                {(isNameSplit ? CUSTOMER_FIELDS : ADDRESS_FIELDS).map(f => (
                  <option key={f.value} value={f.value}>{f.label}</option>
                ))}
              </select>
            </div>
          ))}
        </div>
      )}

      {/* Additional destination rows */}
      {(m.extraMappings ?? []).map((extra, ei) => (
        <ExtraMappingRow
          key={ei}
          extra={extra}
          orgId={orgId}
          fieldDefs={fieldDefs}
          onChange={patch => onChangeExtra(index, ei, patch)}
          onRemove={() => onRemoveExtra(index, ei)}
          onFieldCreated={onFieldCreated}
        />
      ))}

      {/* Add extra destination button */}
      <div className="ms-4 mt-1">
        <button
          type="button"
          className="btn btn-link p-0 text-muted"
          style={{ fontSize: '.75rem' }}
          onClick={() => onAddExtra(index)}
        >
          + Also map to…
        </button>
      </div>
    </div>
  )
}

function StepMapping({ orgId, batch, autoMatches, fieldDefs: initialFieldDefs, onSaved }) {
  const toast    = useToast()
  const save     = useSaveMappings(orgId)

  // Allow newly-created fields to appear in pickers within this session
  const [fieldDefs, setFieldDefs] = useState(initialFieldDefs)

  // Group auto-matches: if two entries share the same csvHeader they represent
  // a primary mapping + one or more extra destinations (loaded from a resumed batch).
  const [mappings, setMappings] = useState(() => {
    const grouped = {}
    autoMatches.forEach((m, i) => {
      const key = `${m.csvHeader}__${m.columnIndex ?? i}`
      if (!grouped[key]) {
        grouped[key] = {
          csvHeader:        m.csvHeader,
          csvColumnIndex:   m.columnIndex ?? i,
          destinationTable: m.destinationTable ?? 'skip',
          destinationField: m.destinationField ?? null,
          fieldDefinitionId:m.fieldDefinitionId ?? null,
          transformType:    m.transformType ?? 'direct',
          isAutoMatched:    m.isAutoMatched ?? false,
          saveForReuse:     true,
          displayOrder:     i,
          outputs:          m.outputs ?? [],
          extraMappings:    [],
        }
      } else if (m.matchStatus === 'extra') {
        grouped[key].extraMappings.push({
          destinationTable:  m.destinationTable ?? 'field_value',
          destinationField:  m.destinationField ?? null,
          fieldDefinitionId: m.fieldDefinitionId ?? null,
        })
      }
    })
    return Object.values(grouped)
  })

  // When a new field is created inline, add it to the local fieldDefs list
  // so it's immediately selectable in other rows without reloading the page.
  function handleFieldCreated(newField) {
    setFieldDefs(prev => [...prev, newField])
  }

  function handleChange(index, patch) {
    setMappings(prev => prev.map((m, i) => i === index ? { ...m, ...patch } : m))
  }

  function handleAddExtra(index) {
    setMappings(prev => prev.map((m, i) => i !== index ? m : {
      ...m,
      extraMappings: [...(m.extraMappings ?? []), { destinationTable: 'field_value', destinationField: null, fieldDefinitionId: null }],
    }))
  }

  function handleChangeExtra(index, extraIndex, patch) {
    setMappings(prev => prev.map((m, i) => i !== index ? m : {
      ...m,
      extraMappings: m.extraMappings.map((e, ei) => {
        if (ei !== extraIndex) return e
        const merged = { ...e, ...patch }
        // If a new field was created from an ExtraMappingRow, add it to local list
        return merged
      }),
    }))
  }

  function handleRemoveExtra(index, extraIndex) {
    setMappings(prev => prev.map((m, i) => i !== index ? m : {
      ...m,
      extraMappings: m.extraMappings.filter((_, ei) => ei !== extraIndex),
    }))
  }

  async function handleSave() {
    // Flatten primary + extra mappings into one list for the API
    const flatMappings = mappings.flatMap(m => {
      const { extraMappings, ...primary } = m
      const extras = (extraMappings ?? []).map(e => ({
        csvHeader:         m.csvHeader,
        csvColumnIndex:    m.csvColumnIndex,
        destinationTable:  e.destinationTable,
        destinationField:  e.destinationField ?? null,
        fieldDefinitionId: e.fieldDefinitionId ?? null,
        transformType:     'direct',
        isAutoMatched:     false,
        saveForReuse:      m.saveForReuse,
        displayOrder:      m.displayOrder,
        outputs:           [],
      }))
      return [primary, ...extras]
    })

    try {
      await save.mutateAsync({ batchId: batch.batchId, data: { mappings: flatMappings } })
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
        Auto-matched columns are pre-filled. Use <em>Also map to…</em> to send the same column to multiple destinations.
      </p>

      <div className="admin-card mb-3" style={{ padding: '0 1.25rem' }}>
        <div className="d-flex gap-2 py-2 border-bottom" style={{ fontWeight: 600, fontSize: '.7rem', textTransform: 'uppercase', color: '#6b7280' }}>
          <span style={{ minWidth: 160 }}>CSV Header</span>
          <span style={{ width: 8 }}></span>
          <span>Destination</span>
        </div>
        {mappings.map((m, i) => (
          <MappingRow
            key={m.csvHeader}
            m={m}
            index={i}
            orgId={orgId}
            fieldDefs={fieldDefs}
            onChange={handleChange}
            onAddExtra={handleAddExtra}
            onChangeExtra={handleChangeExtra}
            onRemoveExtra={handleRemoveExtra}
            onFieldCreated={handleFieldCreated}
          />
        ))}
      </div>

      <button className="btn btn-primary" onClick={handleSave} disabled={save.isPending}>
        {save.isPending ? 'Saving…' : 'Save Mappings & Preview →'}
      </button>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Step 3 — Value Mapping
// ---------------------------------------------------------------------------
function StepValueMapping({ orgId, batchId, onSaved, onSkipped }) {
  const toast      = useToast()
  const saveAliases = useSaveAliases(orgId)
  const { data: vm, isLoading } = useValueMapping(orgId, batchId)

  // localMappings: { [fieldDefinitionId + '|' + aliasValue]: canonicalValue }
  const [localMappings, setLocalMappings] = useState({})

  if (isLoading) return <div className="d-flex align-items-center gap-2"><div className="spinner-border spinner-border-sm" /> Loading value mapping…</div>

  // If there are no dropdown/multiselect columns at all, or no unresolved values, skip this step
  if (!vm || vm.columns.length === 0) {
    return (
      <div>
        <h2 className="h5 mb-2">Value Mapping</h2>
        <p className="text-muted-sm mb-3">No dropdown or multiselect columns found — nothing to map.</p>
        <button className="btn btn-primary" onClick={onSkipped}>Continue to Preview →</button>
      </div>
    )
  }

  if (!vm.hasUnresolved && vm.columns.every(c => c.unresolvedValues.length === 0)) {
    return (
      <div>
        <h2 className="h5 mb-2">Value Mapping</h2>
        <p className="text-muted-sm mb-3">All values in this file already match known options or saved aliases.</p>
        <button className="btn btn-primary" onClick={onSkipped}>Continue to Preview →</button>
      </div>
    )
  }

  function setMapping(fieldDefinitionId, aliasValue, canonicalValue) {
    const key = `${fieldDefinitionId}|${aliasValue}`
    setLocalMappings(prev => ({ ...prev, [key]: canonicalValue }))
  }

  async function handleSave() {
    const aliases = []
    for (const col of vm.columns) {
      for (const rawValue of col.unresolvedValues) {
        const key = `${col.fieldDefinitionId}|${rawValue}`
        const canonical = localMappings[key]
        if (canonical) {
          aliases.push({ fieldDefinitionId: col.fieldDefinitionId, aliasValue: rawValue, canonicalValue: canonical })
        }
      }
    }
    try {
      if (aliases.length > 0) {
        await saveAliases.mutateAsync(aliases)
        toast(`${aliases.length} alias${aliases.length !== 1 ? 'es' : ''} saved.`)
      }
      onSaved()
    } catch (err) {
      toast(err.message ?? 'Failed to save aliases.', 'danger')
    }
  }

  const totalUnresolved = vm.columns.reduce((n, c) => n + c.unresolvedValues.length, 0)
  const totalMapped = Object.values(localMappings).filter(Boolean).length

  return (
    <div>
      <h2 className="h5 mb-1">Map Values</h2>
      <p className="text-muted-sm mb-3">
        The file contains {totalUnresolved} value{totalUnresolved !== 1 ? 's' : ''} that don&apos;t match known options.
        Map them to canonical values below. These mappings are saved for all future imports from this organisation.
      </p>

      {vm.columns.filter(c => c.unresolvedValues.length > 0).map(col => (
        <div key={col.fieldDefinitionId} className="admin-card mb-3">
          <div className="fw-semibold mb-1">{col.fieldLabel} <span className="text-muted fw-normal" style={{ fontSize: '.8rem' }}>({col.fieldType})</span></div>
          <div className="text-muted-sm mb-2" style={{ fontSize: '.78rem' }}>
            CSV column: <code>{col.csvHeader}</code> &nbsp;·&nbsp;
            Known options: {col.knownOptions.join(', ') || '—'}
          </div>

          <table className="table table-sm mb-0" style={{ fontSize: '.82rem' }}>
            <thead>
              <tr>
                <th style={{ width: '40%' }}>Value in file</th>
                <th>Map to canonical option</th>
              </tr>
            </thead>
            <tbody>
              {col.unresolvedValues.map(raw => {
                const key = `${col.fieldDefinitionId}|${raw}`
                const selected = localMappings[key] ?? ''
                return (
                  <tr key={raw}>
                    <td><code>{raw}</code></td>
                    <td>
                      <select
                        className={`form-select form-select-sm ${selected ? '' : 'border-warning'}`}
                        style={{ maxWidth: 260 }}
                        value={selected}
                        onChange={e => setMapping(col.fieldDefinitionId, raw, e.target.value)}
                      >
                        <option value="">— Leave unmapped —</option>
                        {col.knownOptions.map(opt => (
                          <option key={opt} value={opt}>{opt}</option>
                        ))}
                      </select>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>

          {col.existingAliases.length > 0 && (
            <div className="mt-2 text-muted-sm" style={{ fontSize: '.75rem' }}>
              Already mapped: {col.existingAliases.map(a => `"${a.aliasValue}" → "${a.canonicalValue}"`).join(', ')}
            </div>
          )}
        </div>
      ))}

      <div className="d-flex gap-2 align-items-center">
        <button
          className="btn btn-primary"
          onClick={handleSave}
          disabled={saveAliases.isPending}
        >
          {saveAliases.isPending ? 'Saving…' : `Save ${totalMapped > 0 ? totalMapped + ' mapping' + (totalMapped !== 1 ? 's' : '') + ' &' : ''} Continue →`}
        </button>
        <button className="btn btn-link text-muted p-0" style={{ fontSize: '.85rem' }} onClick={onSkipped}>
          Skip — import without mapping
        </button>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Step 4 — Preview (was Step 3)
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
                  {previewData.headers?.map((h, i) => <th key={i}>{h}</th>)}
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
                    {row.values?.map((v, i) => (
                      <td key={i} className="text-muted-sm">{v ?? '—'}</td>
                    ))}
                    <td className="text-muted-sm">{row.message ?? ''}</td>
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

  // SignalR push — updates the React Query cache when the import finishes.
  // Falls back to 30 s polling in case the WebSocket connection is lost.
  useImportHub(orgId, batchId)
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
function RawDataCell({ rawData, headers }) {
  const [expanded, setExpanded] = useState(false)
  let values = null
  try { values = JSON.parse(rawData) } catch { /* ignore */ }

  if (!values || !Array.isArray(values)) {
    return <span className="font-monospace" style={{ fontSize: '.72rem' }}>{rawData}</span>
  }

  if (!expanded) {
    return (
      <button className="btn btn-link p-0 text-muted" style={{ fontSize: '.72rem' }} onClick={() => setExpanded(true)}>
        View {values.length} values
      </button>
    )
  }

  return (
    <div style={{ fontSize: '.72rem' }}>
      <button className="btn btn-link p-0 text-muted mb-1" style={{ fontSize: '.72rem' }} onClick={() => setExpanded(false)}>
        Hide ▲
      </button>
      <table style={{ borderCollapse: 'collapse', width: '100%' }}>
        <tbody>
          {values.map((v, i) => (
            <tr key={i} style={{ borderBottom: '1px solid #e5e7eb' }}>
              <td style={{ padding: '1px 6px 1px 0', color: '#6b7280', whiteSpace: 'nowrap', fontWeight: 500 }}>
                {headers?.[i] ?? `[${i}]`}
              </td>
              <td className="font-monospace" style={{ padding: '1px 0', wordBreak: 'break-all' }}>
                {v ?? <em className="text-muted">—</em>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function StepResults({ orgId, batchId, onFixMappings, onFixAliases, onRetry }) {
  const { data: batch } = useImportBatch(orgId, batchId)
  const [allEntries, setAllEntries] = useState(null)
  const [loadingEntries, setLoadingEntries] = useState(false)

  useEffect(() => {
    if (!batchId || !orgId) return
    setLoadingEntries(true)
    importApi.getErrors(orgId, batchId)
      .then(data => setAllEntries(data))
      .catch(() => setAllEntries([]))
      .finally(() => setLoadingEntries(false))
  }, [orgId, batchId])

  if (!batch) return <LoadingState />

  const headers   = batch.fileHeaders ?? []
  const warnings  = (allEntries ?? []).filter(e => e.errorType === 'warning')
  const errors    = (allEntries ?? []).filter(e => e.errorType !== 'warning')
  const hasIssues = (allEntries ?? []).length > 0

  return (
    <div>
      <div className="text-center mb-4">
        <div style={{ fontSize: '3rem' }}>{(batch.errorRows ?? 0) > 0 ? '⚠️' : '✅'}</div>
        <h2 className="h4 mt-2">Import Complete</h2>
        <p className="text-muted-sm">{batch.fileName}</p>
      </div>

      {/* Stats */}
      <div className="row g-3 mb-4">
        {[
          { label: 'Total Rows', value: batch.totalRows,    color: '#dbeafe', text: '#1e40af' },
          { label: 'Imported',   value: batch.importedRows, color: '#d1fae5', text: '#065f46' },
          { label: 'Skipped',    value: batch.skippedRows,  color: '#fef3c7', text: '#92400e' },
          { label: 'Errors',     value: batch.errorRows,    color: '#fee2e2', text: '#991b1b' },
        ].map(s => (
          <div key={s.label} className="col-6 col-md-3">
            <div className="p-3 rounded-3 text-center" style={{ background: s.color }}>
              <div style={{ fontSize: '1.75rem', fontWeight: 700, color: s.text }}>{s.value ?? 0}</div>
              <div style={{ fontSize: '.8rem', color: s.text, fontWeight: 500 }}>{s.label}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Remediation actions */}
      {hasIssues && (
        <div className="admin-card mb-4" style={{ borderLeft: '4px solid #f59e0b' }}>
          <div className="fw-semibold mb-2" style={{ fontSize: '.9rem' }}>Fix &amp; Retry Options</div>
          <div className="d-flex gap-2 flex-wrap">
            <button className="btn btn-sm btn-outline-primary" onClick={onFixMappings}>
              🗂 Fix Column Mappings
            </button>
            <button className="btn btn-sm btn-outline-secondary" onClick={onFixAliases}>
              🔤 Fix Value Aliases
            </button>
            <button className="btn btn-sm btn-outline-warning" onClick={onRetry}>
              🔁 Retry Failed Rows
            </button>
          </div>
          <div className="text-muted mt-2" style={{ fontSize: '.75rem' }}>
            <strong>Fix Column Mappings</strong> — Go back to step 1 and remap columns, then re-run.
            &nbsp;·&nbsp;
            <strong>Fix Value Aliases</strong> — Add aliases for unrecognised values, then re-run.
            &nbsp;·&nbsp;
            <strong>Retry</strong> — Re-execute the import without any changes.
          </div>
        </div>
      )}

      {/* Warnings — rows imported but with incomplete data */}
      {loadingEntries && (
        <div className="d-flex align-items-center gap-2 mb-3 text-muted" style={{ fontSize: '.85rem' }}>
          <div className="spinner-border spinner-border-sm" /> Loading error details…
        </div>
      )}

      {warnings.length > 0 && (
        <div className="mb-4">
          <div className="fw-semibold mb-2" style={{ fontSize: '.85rem', color: '#92400e' }}>
            ⚠️ {warnings.length} row{warnings.length !== 1 ? 's' : ''} imported with incomplete data
          </div>
          <p className="text-muted-sm mb-2" style={{ fontSize: '.8rem' }}>
            These customers were imported, but some data (e.g., address) was skipped due to missing required fields.
          </p>
          <div className="admin-card p-0">
            <div className="table-wrap">
              <table className="data-table" style={{ fontSize: '.8rem' }}>
                <thead><tr><th style={{ width: 60 }}>Row</th><th>Issue</th><th>Raw Data</th></tr></thead>
                <tbody>
                  {warnings.map(w => (
                    <tr key={w.errorId}>
                      <td>{w.rowNumber}</td>
                      <td className="text-muted-sm">{w.errorMessage}</td>
                      <td><RawDataCell rawData={w.rawData} headers={headers} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Errors — rows that failed to import */}
      {errors.length > 0 && (
        <div className="mb-4">
          <div className="fw-semibold mb-2" style={{ fontSize: '.85rem', color: '#991b1b' }}>
            ❌ {errors.length} row{errors.length !== 1 ? 's' : ''} failed to import
          </div>
          <div className="admin-card p-0">
            <div className="table-wrap">
              <table className="data-table" style={{ fontSize: '.8rem' }}>
                <thead><tr><th style={{ width: 60 }}>Row</th><th style={{ width: 100 }}>Error Type</th><th>Message</th><th>Raw Data</th></tr></thead>
                <tbody>
                  {errors.map(e => (
                    <tr key={e.errorId}>
                      <td>{e.rowNumber}</td>
                      <td>
                        <span className={`badge ${e.errorType === 'duplicate' ? 'bg-warning text-dark' : 'bg-danger'}`}>
                          {e.errorType}
                        </span>
                      </td>
                      <td className="text-muted-sm">{e.errorMessage}</td>
                      <td><RawDataCell rawData={e.rawData} headers={headers} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {!loadingEntries && allEntries !== null && allEntries.length === 0 && (batch.errorRows ?? 0) === 0 && (
        <div className="alert alert-success mb-4" style={{ fontSize: '.875rem' }}>
          All rows imported successfully with no issues.
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
function ImportHistory({ orgId, onResume, onReview }) {
  const toast = useToast()
  const { data: paged, isLoading } = useImportBatches(orgId)
  const cancel  = useCancelImport(orgId)
  const destroy = useDeleteImport(orgId)
  const resume  = useResumeImport(orgId)
  const batches = paged?.items ?? paged ?? []

  const [confirm, setConfirm] = useState(null) // { action: 'cancel'|'delete', batchId }

  async function handleResume(b) {
    try {
      const result = await resume.mutateAsync(b.batchId)
      onResume(result)
    } catch (err) {
      toast(err.message ?? 'Could not resume import.', 'danger')
    }
  }

  async function handleConfirmed() {
    const { action, batchId } = confirm
    setConfirm(null)
    try {
      if (action === 'cancel') await cancel.mutateAsync(batchId)
      else                     await destroy.mutateAsync(batchId)
      toast(action === 'cancel' ? 'Import cancelled.' : 'Import deleted.')
    } catch (err) {
      toast(err.message ?? `Could not ${action} import.`, 'danger')
    }
  }

  if (isLoading) return <LoadingState message="Loading history…" />

  const canResume  = s => s === 'pending' || s === 'preview' || s === 'failed' || s === 'importing'
  const canCancel  = s => s === 'pending' || s === 'preview' || s === 'failed' || s === 'importing'
  const canReview  = b => b.status === 'completed'
  const canDelete  = () => true

  return (
    <>
      {confirm && (
        <div className="modal d-block" style={{ background: 'rgba(0,0,0,.4)' }}>
          <div className="modal-dialog modal-sm modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-body text-center py-4">
                <p className="mb-3 fw-semibold">
                  {confirm.action === 'delete'
                    ? 'Permanently delete this import batch?'
                    : 'Cancel this import batch?'}
                </p>
                <div className="d-flex gap-2 justify-content-center">
                  <button className={`btn btn-sm btn-${confirm.action === 'delete' ? 'danger' : 'warning'}`} onClick={handleConfirmed}>
                    Yes, {confirm.action}
                  </button>
                  <button className="btn btn-sm btn-outline-secondary" onClick={() => setConfirm(null)}>No, keep it</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      <div className="admin-card p-0 mt-4">
        <div className="px-4 pt-3 pb-2 border-bottom" style={{ fontWeight: 600, fontSize: '.9rem' }}>Import History</div>
        {!batches.length ? (
          <EmptyState icon="📋" title="No imports yet" description="Complete your first import above." />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr><th>File</th><th>Status</th><th>Rows</th><th>Imported</th><th>Errors</th><th>Uploaded</th><th></th></tr>
              </thead>
              <tbody>
                {batches.slice(0, 10).map(b => (
                  <tr key={b.batchId}>
                    <td className="fw-semibold text-muted-sm" style={{ maxWidth: 180, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{b.fileName}</td>
                    <td><ImportStatusBadge status={b.status} /></td>
                    <td className="text-muted-sm">{b.totalRows ?? '—'}</td>
                    <td className="text-muted-sm">{b.importedRows ?? '—'}</td>
                    <td className="text-muted-sm">{b.errorRows > 0 ? <span className="text-danger fw-semibold">{b.errorRows}</span> : (b.errorRows ?? '—')}</td>
                    <td className="text-muted-sm">{fmtDate(b.uploadedAt)}</td>
                    <td>
                      <div className="d-flex gap-1 justify-content-end">
                        {canReview(b) && (
                          <button
                            className="btn btn-xs btn-outline-info"
                            style={{ fontSize: '.7rem', padding: '2px 8px' }}
                            onClick={() => onReview(b)}
                          >
                            Review
                          </button>
                        )}
                        {canResume(b.status) && (
                          <button
                            className="btn btn-xs btn-outline-primary"
                            style={{ fontSize: '.7rem', padding: '2px 8px' }}
                            disabled={resume.isPending}
                            onClick={() => handleResume(b)}
                          >
                            Resume
                          </button>
                        )}
                        {canCancel(b.status) && (
                          <button
                            className="btn btn-xs btn-outline-warning"
                            style={{ fontSize: '.7rem', padding: '2px 8px' }}
                            onClick={() => setConfirm({ action: 'cancel', batchId: b.batchId })}
                          >
                            Cancel
                          </button>
                        )}
                        {canDelete(b.status) && (
                          <button
                            className="btn btn-xs btn-outline-danger"
                            style={{ fontSize: '.7rem', padding: '2px 8px' }}
                            onClick={() => setConfirm({ action: 'delete', batchId: b.batchId })}
                          >
                            Delete
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  )
}

// ---------------------------------------------------------------------------
// Main ImportPage
// ---------------------------------------------------------------------------
export default function ImportPage() {
  const { organizationId } = useParams()
  const toast              = useToast()
  const { data: org }      = useOrganization(organizationId)
  const { data: fieldDefsPage } = useFields(organizationId)
  const fieldDefs = fieldDefsPage?.items ?? fieldDefsPage ?? []
  const [step, setStep]               = useState(0)
  const [uploadResult, setUploadResult] = useState(null)
  const resetImport = useResetImport(organizationId)
  const resume      = useResumeImport(organizationId)

  function handleUploaded(result) {
    setUploadResult(result)
    setStep(1)
  }

  // Resume from import history: result has same shape as upload response
  function handleResume(result) {
    setUploadResult(result)
    setStep(1)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  // Review a completed batch from history — jump straight to results step
  function handleReview(batch) {
    setUploadResult({ batchId: batch.batchId, fileName: batch.fileName })
    setStep(5)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function handleMappingsSaved() { setStep(2) }
  function handleValueMappingDone() { setStep(3) }
  function handlePreviewConfirmed() { setStep(4) }
  function handleExecuteDone() { setStep(5) }

  function reset() {
    setStep(0)
    setUploadResult(null)
  }

  // Reset to pending then resume into step 1 (Map Columns)
  async function handleFixMappings() {
    if (!uploadResult) return
    try {
      await resetImport.mutateAsync({ batchId: uploadResult.batchId, targetStatus: 'pending' })
      const resumed = await resume.mutateAsync(uploadResult.batchId)
      setUploadResult(resumed)
      setStep(1)
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (err) {
      toast(err.message ?? 'Could not reset batch.', 'danger')
    }
  }

  // Reset to preview then navigate to step 2 (Value Aliases)
  async function handleFixAliases() {
    if (!uploadResult) return
    try {
      await resetImport.mutateAsync({ batchId: uploadResult.batchId, targetStatus: 'preview' })
      setStep(2)
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (err) {
      toast(err.message ?? 'Could not reset batch.', 'danger')
    }
  }

  // Reset to preview then navigate to step 4 (Execute) for retry
  async function handleRetry() {
    if (!uploadResult) return
    try {
      await resetImport.mutateAsync({ batchId: uploadResult.batchId, targetStatus: 'preview' })
      setStep(4)
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (err) {
      toast(err.message ?? 'Could not reset batch.', 'danger')
    }
  }

  return (
    <div>
      <PageHeader
        breadcrumbs={[
          { label: 'Organisations', href: '/organizations' },
          { label: org?.organizationName ?? '…', href: `/organizations/${organizationId}` },
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
            fieldDefs={fieldDefs}
            onSaved={handleMappingsSaved}
          />
        )}
        {step === 2 && uploadResult && (
          <StepValueMapping
            orgId={organizationId}
            batchId={uploadResult.batchId}
            onSaved={handleValueMappingDone}
            onSkipped={handleValueMappingDone}
          />
        )}
        {step === 3 && uploadResult && (
          <StepPreview
            orgId={organizationId}
            batchId={uploadResult.batchId}
            onConfirmed={handlePreviewConfirmed}
          />
        )}
        {step === 4 && uploadResult && (
          <StepExecute
            orgId={organizationId}
            batchId={uploadResult.batchId}
            onDone={handleExecuteDone}
          />
        )}
        {step === 5 && uploadResult && (
          <StepResults
            orgId={organizationId}
            batchId={uploadResult.batchId}
            onFixMappings={handleFixMappings}
            onFixAliases={handleFixAliases}
            onRetry={handleRetry}
          />
        )}

        {step > 0 && step < 5 && (
          <div className="mt-3 pt-3 border-top">
            <button className="btn btn-sm btn-link text-muted p-0" onClick={reset}>← Start over with a new file</button>
          </div>
        )}
        {step === 5 && (
          <div className="mt-3 pt-3 border-top">
            <button className="btn btn-sm btn-link text-muted p-0" onClick={reset}>← Start a new import</button>
          </div>
        )}
      </div>

      {step === 0 && <ImportHistory orgId={organizationId} onResume={handleResume} onReview={handleReview} />}
    </div>
  )
}
