import { useState, useEffect, useRef } from 'react'
import { fmtNumber } from '@/utils/dates.js'
import { parseOptionsCsv } from '@/utils/csv.js'
import { useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import {
  DndContext, closestCenter, KeyboardSensor, PointerSensor,
  useSensor, useSensors,
} from '@dnd-kit/core'
import {
  SortableContext, sortableKeyboardCoordinates, verticalListSortingStrategy,
  useSortable, arrayMove,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import {
  useOrganization,
  useSections, useCreateSection, useUpdateSection, useSetSectionStatus,
  useReorderSections, useAssignFieldsToSection,
  useFields, useFieldOptions, useCreateField, useUpdateField,
  useSetFieldStatus, useSaveFieldOptions,
  useFormPreview, useCustomers,
  useLibrarySections, useImportFromLibrary,
} from '@/hooks/useApi.js'
import {
  PageHeader, LoadingState, ErrorAlert, FieldTypeBadge,
  StatusBadge, ConfirmModal, EmptyState, useToast,
} from '@/components/common/index.jsx'

const FIELD_TYPES = ['text', 'number', 'date', 'dropdown', 'multiselect', 'boolean', 'phone']

const PHONE_FORMATS = ['(XXX) XXX-XXXX', 'XXX-XXX-XXXX', 'XXX.XXX.XXXX']

function formatPhone(digits, format) {
  const d = (digits ?? '').replace(/\D/g, '')
  if (d.length !== 10) return digits ?? ''
  if (format === 'XXX-XXX-XXXX')   return `${d.slice(0,3)}-${d.slice(3,6)}-${d.slice(6)}`
  if (format === 'XXX.XXX.XXXX')   return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6)}`
  return `(${d.slice(0,3)}) ${d.slice(3,6)}-${d.slice(6)}`  // default: (XXX) XXX-XXXX
}

// ─── Drag handle ────────────────────────────────────────────────────────────
function DragHandle({ listeners, attributes }) {
  return (
    <span
      className="drag-handle"
      title="Drag to reorder"
      {...listeners}
      {...attributes}
    >⠿</span>
  )
}

// ─── Sortable section row ────────────────────────────────────────────────────
function SortableSectionRow({ section, children, onEdit, onToggleStatus }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({ id: section.sectionId })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  }

  return (
    <div ref={setNodeRef} style={style} className={`section-card ${!section.isActive ? 'section-inactive' : ''}`}>
      <div className="section-card-header">
        <DragHandle listeners={listeners} attributes={attributes} />
        <span className="section-card-name">{section.sectionName}</span>
        {!section.isActive && <span className="badge bg-secondary ms-2" style={{ fontSize: '.7rem' }}>Inactive</span>}
        <div className="ms-auto d-flex gap-2">
          <button className="btn btn-xs btn-outline-secondary" onClick={() => onEdit(section)}>Edit</button>
          <button
            className={`btn btn-xs ${section.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`}
            onClick={() => onToggleStatus(section)}
          >
            {section.isActive ? 'Deactivate' : 'Activate'}
          </button>
        </div>
      </div>
      {children}
    </div>
  )
}

// ─── Sortable field row ──────────────────────────────────────────────────────
function SortableFieldRow({ field, onEdit, onOptions, onToggleStatus }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({ id: field.fieldDefinitionId })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  }

  return (
    <div ref={setNodeRef} style={style} className="field-row">
      <DragHandle listeners={listeners} attributes={attributes} />
      <code className="field-row-key text-muted-sm">{field.fieldKey}</code>
      <span className="field-row-label fw-semibold">{field.fieldLabel}</span>
      <FieldTypeBadge type={field.fieldType} />
      {field.isRequired && <span className="badge bg-danger-subtle text-danger-emphasis" style={{ fontSize: '.7rem' }}>Required</span>}
      <StatusBadge active={field.isActive} />
      <div className="field-row-actions ms-auto d-flex gap-1">
        {(field.fieldType === 'dropdown' || field.fieldType === 'multiselect') && (
          <button className="btn btn-xs btn-outline-secondary" onClick={() => onOptions(field)}>Options</button>
        )}
        <button className="btn btn-xs btn-outline-secondary" onClick={() => onEdit(field)}>Edit</button>
        <button
          className={`btn btn-xs ${field.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`}
          onClick={() => onToggleStatus(field)}
        >
          {field.isActive ? 'Deactivate' : 'Activate'}
        </button>
      </div>
    </div>
  )
}

// ─── Field modal ─────────────────────────────────────────────────────────────
function FieldModal({ organizationId, field, sections, fields, onClose }) {
  const toast  = useToast()
  const create = useCreateField(organizationId)
  const update = useUpdateField(organizationId)
  const isEdit = !!field

  const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm({
    defaultValues: {
      organizationId,
      sectionId:      field?.sectionId      ?? '',
      fieldKey:       field?.fieldKey       ?? '',
      fieldLabel:     field?.fieldLabel     ?? '',
      fieldType:      field?.fieldType      ?? 'text',
      isRequired:     field?.isRequired     ?? false,
      helpText:       field?.helpText       ?? '',
      regexPattern:   field?.regexPattern   ?? '',
      minValue:       field?.minValue       ?? '',
      maxValue:       field?.maxValue       ?? '',
      displayOrder:   field?.displayOrder   ?? '',
      displayFormat:  field?.displayFormat  ?? PHONE_FORMATS[0],
      isActive:       field?.isActive       ?? true,
    },
  })

  const fieldType = watch('fieldType')
  const sectionId = watch('sectionId')
  const saving    = create.isPending || update.isPending

  useEffect(() => {
    if (isEdit) return
    const sectionFields = (fields ?? []).filter(f =>
      sectionId ? f.sectionId === sectionId : !f.sectionId
    )
    const nextOrder = sectionFields.length
      ? Math.max(...sectionFields.map(f => f.displayOrder ?? 0)) + 1
      : 1
    setValue('displayOrder', nextOrder)
  }, [sectionId])

  async function onSubmit(values) {
    const payload = {
      ...values,
      sectionId:     values.sectionId     === '' ? null : values.sectionId,
      minValue:      values.minValue      === '' ? null : Number(values.minValue),
      maxValue:      values.maxValue      === '' ? null : Number(values.maxValue),
      displayOrder:  values.displayOrder  === '' ? null : Number(values.displayOrder),
      displayFormat: values.fieldType === 'phone' ? values.displayFormat : null,
    }
    try {
      if (isEdit) {
        await update.mutateAsync({ fieldId: field.fieldDefinitionId, data: payload })
        toast('Input updated.')
      } else {
        await create.mutateAsync(payload)
        toast('Input created.')
      }
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving input.', 'danger')
    }
  }

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-lg modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">{isEdit ? 'Edit Input' : 'New Input'}</h5>
              <button type="button" className="btn-close" onClick={onClose} />
            </div>
            <div className="modal-body">
              <div className="row g-3">
                <div className="col-6">
                  <label className="form-label fw-semibold">Field Key <span className="text-danger">*</span></label>
                  <input
                    className={`form-control ${errors.fieldKey ? 'is-invalid' : ''}`}
                    {...register('fieldKey', { required: 'Required' })}
                    disabled={isEdit}
                  />
                  {isEdit && <div className="form-text text-muted">Key cannot be changed after creation.</div>}
                  {errors.fieldKey && <div className="invalid-feedback">{errors.fieldKey.message}</div>}
                </div>
                <div className="col-6">
                  <label className="form-label fw-semibold">Label <span className="text-danger">*</span></label>
                  <input className={`form-control ${errors.fieldLabel ? 'is-invalid' : ''}`}
                    {...register('fieldLabel', { required: 'Required' })} />
                  {errors.fieldLabel && <div className="invalid-feedback">{errors.fieldLabel.message}</div>}
                </div>
                <div className="col-4">
                  <label className="form-label fw-semibold">Type <span className="text-danger">*</span></label>
                  <select className="form-select" {...register('fieldType')}>
                    {FIELD_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                  </select>
                </div>
                <div className="col-4">
                  <label className="form-label">Section</label>
                  <select className="form-select" {...register('sectionId')}>
                    <option value="">— Unassigned —</option>
                    {(sections ?? []).filter(s => s.isActive).map(s => (
                      <option key={s.sectionId} value={s.sectionId}>{s.sectionName}</option>
                    ))}
                  </select>
                </div>
                <div className="col-4">
                  <label className="form-label">Display Order</label>
                  <input type="number" className="form-control" {...register('displayOrder')} />
                </div>
                <div className="col-12 d-flex align-items-center gap-3">
                  <div className="form-check">
                    <input className="form-check-input" type="checkbox" id="isRequired" {...register('isRequired')} />
                    <label className="form-check-label fw-semibold" htmlFor="isRequired">Required</label>
                  </div>
                </div>
                <div className="col-12">
                  <label className="form-label">Help Text</label>
                  <input className="form-control" {...register('helpText')} />
                </div>
                {fieldType === 'text' && (
                  <div className="col-12">
                    <label className="form-label">Validation Regex</label>
                    <input className="form-control font-monospace" {...register('regexPattern')} placeholder="e.g. ^\d{5}$" />
                  </div>
                )}
                {fieldType === 'number' && (
                  <>
                    <div className="col-6">
                      <label className="form-label">Min Value</label>
                      <input type="number" className="form-control" {...register('minValue')} />
                    </div>
                    <div className="col-6">
                      <label className="form-label">Max Value</label>
                      <input type="number" className="form-control" {...register('maxValue')} />
                    </div>
                  </>
                )}
                {fieldType === 'phone' && (
                  <div className="col-12">
                    <label className="form-label fw-semibold">Display Format</label>
                    <select className="form-select" {...register('displayFormat')}>
                      {PHONE_FORMATS.map(f => <option key={f} value={f}>{f}</option>)}
                    </select>
                    <div className="form-text">Digits are always stored without formatting. This controls how the number is displayed.</div>
                  </div>
                )}
                {(fieldType === 'dropdown' || fieldType === 'multiselect') && (
                  <div className="col-12">
                    <div className="alert alert-info py-2 mb-0" style={{ fontSize: '.85rem' }}>
                      After creating, use the <strong>Options</strong> button to add dropdown values.
                    </div>
                  </div>
                )}
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Input'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

// ─── Field options modal ─────────────────────────────────────────────────────
function FieldOptionsModal({ fieldId, fieldLabel, onClose }) {
  const toast    = useToast()
  const { data: options, isLoading } = useFieldOptions(fieldId)
  const save     = useSaveFieldOptions(fieldId)
  const fileRef  = useRef()
  const [items, setItems] = useState(null)
  const opts = items ?? (options ?? [])

  function addItem() {
    setItems([...opts, { optionKey: '', optionLabel: '', displayOrder: opts.length + 1, isActive: true }])
  }
  function updateItem(i, key, val) {
    setItems(opts.map((o, idx) => idx === i ? { ...o, [key]: val } : o))
  }
  function removeItem(i) {
    setItems(opts.filter((_, idx) => idx !== i))
  }

  function handleFileUpload(e) {
    const file = e.target.files[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = evt => {
      const parsed = parseOptionsCsv(evt.target.result)
      setItems(parsed.map(r => ({ ...r, isActive: true })))
      toast(`Loaded ${parsed.length} options from file.`)
    }
    reader.readAsText(file)
    e.target.value = ''
  }

  async function handleSave() {
    try {
      await save.mutateAsync(opts)
      toast('Options saved.')
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving options.', 'danger')
    }
  }

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Options — {fieldLabel}</h5>
            <button type="button" className="btn-close" onClick={onClose} />
          </div>
          <div className="modal-body">
            {isLoading ? <LoadingState /> : (
              <>
                <div style={{ maxHeight: 320, overflowY: 'auto' }}>
                  {opts.map((opt, i) => (
                    <div key={i} className="d-flex gap-2 mb-2 align-items-center">
                      <input className="form-control form-control-sm" placeholder="Key" value={opt.optionKey || ''}
                        onChange={e => updateItem(i, 'optionKey', e.target.value)} />
                      <input className="form-control form-control-sm" placeholder="Label" value={opt.optionLabel || ''}
                        onChange={e => updateItem(i, 'optionLabel', e.target.value)} />
                      <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => removeItem(i)}>✕</button>
                    </div>
                  ))}
                </div>
                <div className="d-flex gap-2 mt-2 align-items-center">
                  <button type="button" className="btn btn-sm btn-outline-secondary" onClick={addItem}>+ Add Option</button>
                  <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => fileRef.current?.click()}>
                    Upload CSV
                  </button>
                  <input ref={fileRef} type="file" className="d-none" accept=".csv,.txt,.tsv" onChange={handleFileUpload} />
                  <span className="text-muted ms-1" style={{ fontSize: '.75rem' }}>
                    CSV: key, label (replaces list)
                  </span>
                </div>
              </>
            )}
          </div>
          <div className="modal-footer">
            <button className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={save.isPending}>
              {save.isPending ? 'Saving…' : `Save Options (${opts.length})`}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

// ─── Section modal ───────────────────────────────────────────────────────────
function SectionModal({ organizationId, section, availableFields, onClose }) {
  const toast    = useToast()
  const create   = useCreateSection(organizationId)
  const update   = useUpdateSection(organizationId)
  const assign   = useAssignFieldsToSection(organizationId)
  const isEdit   = !!section

  // Pre-check fields already assigned to this section when editing
  const [selectedFieldIds, setSelectedFieldIds] = useState(
    () => availableFields?.filter(f => f.sectionId === section?.sectionId).map(f => f.fieldDefinitionId) ?? []
  )

  const { register, handleSubmit, formState: { errors } } = useForm({
    defaultValues: {
      sectionName:  section?.sectionName  ?? '',
      displayOrder: section?.displayOrder ?? 0,
      isActive:     section?.isActive     ?? true,
    },
  })

  const saving = create.isPending || update.isPending || assign.isPending

  function toggleField(fieldId) {
    setSelectedFieldIds(prev =>
      prev.includes(fieldId) ? prev.filter(id => id !== fieldId) : [...prev, fieldId]
    )
  }

  async function onSubmit(values) {
    try {
      let sectionId
      if (isEdit) {
        await update.mutateAsync({ sectionId: section.sectionId, data: { ...values, isActive: section.isActive } })
        sectionId = section.sectionId
      } else {
        const dto = await create.mutateAsync({ sectionName: values.sectionName, displayOrder: Number(values.displayOrder) })
        sectionId = dto.sectionId
      }

      // Always call assign so removals (unchecked fields) are processed by the service
      const fields = selectedFieldIds.map((id, i) => ({ fieldDefinitionId: id, displayOrder: i + 1 }))
      await assign.mutateAsync({ sectionId, fields })

      toast(isEdit ? 'Section updated.' : 'Section created.')
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving section.', 'danger')
    }
  }

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">{isEdit ? 'Edit Section' : 'New Section'}</h5>
              <button type="button" className="btn-close" onClick={onClose} />
            </div>
            <div className="modal-body">
              <div className="row g-3">
                <div className="col-8">
                  <label className="form-label fw-semibold">Section Name <span className="text-danger">*</span></label>
                  <input
                    className={`form-control ${errors.sectionName ? 'is-invalid' : ''}`}
                    {...register('sectionName', { required: 'Required' })}
                  />
                  {errors.sectionName && <div className="invalid-feedback">{errors.sectionName.message}</div>}
                </div>
                <div className="col-4">
                  <label className="form-label">Display Order</label>
                  <input type="number" className="form-control" {...register('displayOrder')} />
                </div>

                {availableFields?.length > 0 && (
                  <div className="col-12">
                    <label className="form-label fw-semibold">
                      {isEdit ? 'Inputs in this section' : 'Add unassigned inputs to this section'}
                    </label>
                    <div className="section-field-pick-list">
                      {availableFields.map(f => (
                        <label key={f.fieldDefinitionId} className="section-field-pick-item">
                          <input
                            type="checkbox"
                            className="form-check-input me-2"
                            checked={selectedFieldIds.includes(f.fieldDefinitionId)}
                            onChange={() => toggleField(f.fieldDefinitionId)}
                          />
                          <span className="fw-semibold">{f.fieldLabel}</span>
                          <code className="ms-2 text-muted-sm">{f.fieldKey}</code>
                          <FieldTypeBadge type={f.fieldType} />
                        </label>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Section'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

// ─── Form preview panel ───────────────────────────────────────────────────────
function PreviewPanel({ organizationId }) {
  const [open, setOpen]         = useState(false)
  const [customerId, setCustomerId] = useState('')

  const { data: customersPage } = useCustomers(organizationId, 1, 200)
  const customers = customersPage?.items ?? []

  const { data: preview, isLoading: previewLoading } = useFormPreview(
    open ? organizationId : null,
    open && customerId ? customerId : null
  )

  function renderFieldInput(field) {
    const val = field.currentValue ?? ''
    const baseProps = { className: 'form-control form-control-sm', disabled: true, readOnly: true }

    if (field.fieldType === 'checkbox') {
      const checked = val === '1' || val === 'true' || val === 'True'
      return <input type="text" {...baseProps} value={val === '' ? '' : checked ? 'Yes' : 'No'} />
    }
    if (field.fieldType === 'date')
      return <input type="date" {...baseProps} value={val} />
    if (field.fieldType === 'number')
      return <input type="number" {...baseProps} value={fmtNumber(val)} />
    if (field.fieldType === 'phone')
      return <input type="text" {...baseProps} value={formatPhone(val, field.displayFormat)} />
    if (field.fieldType === 'dropdown') {
      const selected = field.options?.find(o => o.optionKey === val)
      const displayVal = selected ? (selected.optionLabel || selected.optionKey) : val
      return <input type="text" {...baseProps} value={displayVal} />
    }
    return <input type="text" {...baseProps} value={val} />
  }

  function renderSection(sectionName, fields, key) {
    return (
      <div key={key} className="preview-section mb-3">
        {sectionName && <div className="preview-section-title">{sectionName}</div>}
        <div className="row g-2">
          {fields.map(f => (
            <div key={f.fieldDefinitionId} className="col-12 col-md-6">
              <label className="form-label form-label-sm fw-semibold mb-1">
                {f.fieldLabel}
                {f.isRequired && <span className="text-danger ms-1">*</span>}
              </label>
              {renderFieldInput(f)}
              {f.helpText && <div className="form-text" style={{ fontSize: '.75rem' }}>{f.helpText}</div>}
            </div>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="admin-card mt-3">
      <button
        className="btn btn-link p-0 text-decoration-none fw-semibold d-flex align-items-center gap-2"
        onClick={() => setOpen(o => !o)}
        style={{ fontSize: '.9375rem' }}
      >
        <span>{open ? '▼' : '▶'}</span>
        <span>Form Preview</span>
      </button>

      {open && (
        <div className="mt-3">
          <div className="row g-2 mb-3 align-items-end">
            <div className="col-auto">
              <label className="form-label fw-semibold mb-1">Select Customer</label>
              <select
                className="form-select form-select-sm"
                style={{ minWidth: 260 }}
                value={customerId}
                onChange={e => setCustomerId(e.target.value)}
              >
                <option value="">— choose a customer —</option>
                {customers.map(c => (
                  <option key={c.customerId} value={c.customerId}>
                    {c.firstName} {c.lastName} {c.customerCode ? `(${c.customerCode})` : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {!customerId && (
            <p className="text-muted-sm">Select a customer above to preview the form with their current values.</p>
          )}

          {customerId && previewLoading && <LoadingState message="Loading preview…" />}

          {customerId && preview && (
            <div className="preview-form-wrap">
              <div className="mb-2" style={{ fontSize: '.85rem', color: '#6b7280' }}>
                Previewing: <strong>{preview.customerName}</strong> — read-only, showing saved values
              </div>
              {preview.sections?.map(s => renderSection(s.sectionName, s.fields, s.sectionId))}
              {preview.unassignedFields?.length > 0 && renderSection(null, preview.unassignedFields, 'unassigned')}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

// ─── Import from library modal ───────────────────────────────────────────────
function ImportFromLibraryModal({ organizationId, onClose }) {
  const toast = useToast()
  const { data: libSections = [], isLoading } = useLibrarySections(false)
  const importMutation = useImportFromLibrary(organizationId)
  const [selectedIds, setSelectedIds] = useState([])

  function toggle(id) {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id])
  }

  async function handleImport() {
    if (selectedIds.length === 0) return
    try {
      const result = await importMutation.mutateAsync(selectedIds)
      toast(`Imported ${result.sectionsCreated} section(s) and ${result.fieldsCreated} field(s).`)
      onClose()
    } catch (err) {
      toast(err.message ?? 'Import failed.', 'danger')
    }
  }

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-lg modal-dialog-centered">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Import from Field Library</h5>
            <button type="button" className="btn-close" onClick={onClose} />
          </div>
          <div className="modal-body">
            {isLoading ? <LoadingState /> : libSections.length === 0 ? (
              <p className="text-muted">No library sections available.</p>
            ) : (
              <>
                <p className="text-muted mb-3" style={{ fontSize: '.875rem' }}>
                  Select sections to copy into this organisation. Fields and options will be duplicated — changes to the library will not affect what was already imported.
                </p>
                <div className="section-field-pick-list">
                  {libSections.map(s => (
                    <label key={s.id} className="section-field-pick-item" style={{ flexDirection: 'column', alignItems: 'flex-start', gap: 4 }}>
                      <div className="d-flex align-items-center w-100 gap-2">
                        <input
                          type="checkbox"
                          className="form-check-input"
                          checked={selectedIds.includes(s.id)}
                          onChange={() => toggle(s.id)}
                        />
                        <span className="fw-semibold">{s.sectionName}</span>
                        <span className="badge bg-secondary-subtle text-secondary-emphasis ms-auto" style={{ fontSize: '.7rem' }}>
                          {s.fields?.length ?? 0} field{(s.fields?.length ?? 0) !== 1 ? 's' : ''}
                        </span>
                      </div>
                      {s.description && (
                        <div className="text-muted ms-4" style={{ fontSize: '.8rem' }}>{s.description}</div>
                      )}
                      {s.fields?.length > 0 && (
                        <div className="ms-4 d-flex flex-wrap gap-1 mt-1">
                          {s.fields.map(f => (
                            <span key={f.id} className="badge bg-light text-dark border" style={{ fontSize: '.7rem', fontWeight: 400 }}>
                              {f.fieldLabel}
                            </span>
                          ))}
                        </div>
                      )}
                    </label>
                  ))}
                </div>
              </>
            )}
          </div>
          <div className="modal-footer">
            <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
            <button
              type="button"
              className="btn btn-primary"
              disabled={selectedIds.length === 0 || importMutation.isPending}
              onClick={handleImport}
            >
              {importMutation.isPending ? 'Importing…' : `Import ${selectedIds.length > 0 ? `(${selectedIds.length})` : ''}`}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────
export default function InputsPage() {
  const { organizationId } = useParams()
  const toast = useToast()

  const [showInactive,      setShowInactive]      = useState(false)
  const [showFieldModal,    setShowFieldModal]    = useState(false)
  const [editField,         setEditField]         = useState(null)
  const [optionsField,      setOptionsField]      = useState(null)
  const [showSectionModal,  setShowSectionModal]  = useState(false)
  const [editSection,       setEditSection]       = useState(null)
  const [showLibraryImport, setShowLibraryImport] = useState(false)
  const [confirmTarget,     setConfirmTarget]     = useState(null) // { type, item, newStatus }

  const { data: org } = useOrganization(organizationId)
  const { data: sections = [], isLoading: sectionsLoading } = useSections(organizationId)
  const { data: fields   = [], isLoading: fieldsLoading, isError } = useFields(organizationId, showInactive)

  const setFieldStatus   = useSetFieldStatus(organizationId)
  const setSectionStatus = useSetSectionStatus(organizationId)
  const reorderSections  = useReorderSections(organizationId)
  const reorderFields    = useAssignFieldsToSection(organizationId) // re-used for field reorder within section

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  )

  // Memoised grouping
  const fieldsBySectionId = {}
  const unassignedFields  = []
  for (const f of fields) {
    if (f.sectionId) {
      if (!fieldsBySectionId[f.sectionId]) fieldsBySectionId[f.sectionId] = []
      fieldsBySectionId[f.sectionId].push(f)
    } else {
      unassignedFields.push(f)
    }
  }

  const activeSections = sections
    .filter(s => showInactive || s.isActive)
    .sort((a, b) => a.displayOrder - b.displayOrder)

  // Section drag end
  function handleSectionDragEnd(event) {
    const { active, over } = event
    if (!over || active.id === over.id) return
    const oldIdx = activeSections.findIndex(s => s.sectionId === active.id)
    const newIdx = activeSections.findIndex(s => s.sectionId === over.id)
    const reordered = arrayMove(activeSections, oldIdx, newIdx)
    reorderSections.mutate(
      reordered.map((s, i) => ({ sectionId: s.sectionId, displayOrder: i + 1 }))
    )
  }

  // Field drag end within a section
  function handleFieldDragEnd(sectionId, event) {
    const { active, over } = event
    if (!over || active.id === over.id) return
    const sectionFields = fieldsBySectionId[sectionId] ?? []
    const oldIdx = sectionFields.findIndex(f => f.fieldDefinitionId === active.id)
    const newIdx = sectionFields.findIndex(f => f.fieldDefinitionId === over.id)
    const reordered = arrayMove(sectionFields, oldIdx, newIdx)
    reorderFields.mutate({
      sectionId,
      fields: reordered.map((f, i) => ({ fieldDefinitionId: f.fieldDefinitionId, displayOrder: i + 1 })),
    })
  }

  async function confirmStatusChange() {
    const { type, item, newStatus } = confirmTarget
    try {
      if (type === 'field') {
        await setFieldStatus.mutateAsync({ fieldId: item.fieldDefinitionId, isActive: newStatus })
        toast(newStatus ? 'Input activated.' : 'Input deactivated.')
      } else {
        await setSectionStatus.mutateAsync({ sectionId: item.sectionId, isActive: newStatus })
        toast(newStatus ? 'Section activated.' : 'Section deactivated.')
      }
    } catch (err) {
      toast(err.message ?? 'Failed to update status.', 'danger')
    } finally {
      setConfirmTarget(null)
    }
  }

  if (sectionsLoading || fieldsLoading) return <LoadingState message="Loading inputs…" />
  if (isError) return <ErrorAlert message="Could not load inputs." />

  return (
    <div>
      <PageHeader
        breadcrumbs={[
          { label: 'Organizations', href: '/organizations' },
          { label: org?.organizationName ?? '…', href: `/organizations/${organizationId}` },
          { label: 'Inputs' },
        ]}
        title="Inputs"
        subtitle="Manage sections and fields for this organisation's data collection form"
        actions={
          <>
            <div className="form-check form-switch mb-0 me-2">
              <input className="form-check-input" type="checkbox" id="showInactive"
                checked={showInactive} onChange={e => setShowInactive(e.target.checked)} />
              <label className="form-check-label text-muted-sm" htmlFor="showInactive">Show inactive</label>
            </div>
            <button className="btn btn-outline-secondary btn-sm" onClick={() => setShowLibraryImport(true)}>Import from Library</button>
            <button className="btn btn-outline-primary btn-sm" onClick={() => setShowSectionModal(true)}>+ New Section</button>
            <button className="btn btn-primary btn-sm" onClick={() => setShowFieldModal(true)}>+ New Input</button>
          </>
        }
      />

      {/* Sections with fields */}
      {activeSections.length === 0 && unassignedFields.length === 0 ? (
        <EmptyState icon="🗂️" title="No inputs defined"
          description="Create sections to group your inputs, then add fields."
          action={
            <div className="d-flex gap-2 justify-content-center">
              <button className="btn btn-outline-primary btn-sm" onClick={() => setShowSectionModal(true)}>+ New Section</button>
              <button className="btn btn-primary btn-sm" onClick={() => setShowFieldModal(true)}>+ New Input</button>
            </div>
          }
        />
      ) : (
        <>
          <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleSectionDragEnd}>
            <SortableContext items={activeSections.map(s => s.sectionId)} strategy={verticalListSortingStrategy}>
              {activeSections.map(section => {
                const sectionFields = (fieldsBySectionId[section.sectionId] ?? [])
                  .filter(f => showInactive || f.isActive)
                return (
                  <SortableSectionRow
                    key={section.sectionId}
                    section={section}
                    onEdit={s => { setEditSection(s); setShowSectionModal(true) }}
                    onToggleStatus={s => setConfirmTarget({ type: 'section', item: s, newStatus: !s.isActive })}
                  >
                    <DndContext
                      sensors={sensors}
                      collisionDetection={closestCenter}
                      onDragEnd={e => handleFieldDragEnd(section.sectionId, e)}
                    >
                      <SortableContext
                        items={sectionFields.map(f => f.fieldDefinitionId)}
                        strategy={verticalListSortingStrategy}
                      >
                        {sectionFields.length === 0 ? (
                          <div className="section-empty-hint">No inputs in this section yet — assign via New Input or edit an existing input.</div>
                        ) : (
                          sectionFields.map(f => (
                            <SortableFieldRow key={f.fieldDefinitionId} field={f}
                              onEdit={f => setEditField(f)}
                              onOptions={f => setOptionsField(f)}
                              onToggleStatus={f => setConfirmTarget({ type: 'field', item: f, newStatus: !f.isActive })}
                            />
                          ))
                        )}
                      </SortableContext>
                    </DndContext>
                  </SortableSectionRow>
                )
              })}
            </SortableContext>
          </DndContext>

          {/* Unassigned fields */}
          {unassignedFields.filter(f => showInactive || f.isActive).length > 0 && (
            <div className="section-card section-unassigned mt-3">
              <div className="section-card-header">
                <span className="section-card-name text-muted">Unassigned</span>
              </div>
              {unassignedFields.filter(f => showInactive || f.isActive).map(f => (
                <SortableFieldRow key={f.fieldDefinitionId} field={f}
                  onEdit={f => setEditField(f)}
                  onOptions={f => setOptionsField(f)}
                  onToggleStatus={f => setConfirmTarget({ type: 'field', item: f, newStatus: !f.isActive })}
                />
              ))}
            </div>
          )}
        </>
      )}

      {/* Preview panel */}
      <PreviewPanel organizationId={organizationId} />

      {/* Modals */}
      {(showFieldModal || editField) && (
        <FieldModal
          organizationId={organizationId}
          field={editField}
          sections={sections}
          fields={fields}
          onClose={() => { setShowFieldModal(false); setEditField(null) }}
        />
      )}

      {showSectionModal && (
        <SectionModal
          organizationId={organizationId}
          section={editSection}
          availableFields={
            editSection
              // Edit: show fields already in this section + unassigned ones
              ? fields.filter(f => !f.sectionId || f.sectionId === editSection.sectionId)
              // Create: only unassigned fields
              : unassignedFields
          }
          onClose={() => { setShowSectionModal(false); setEditSection(null) }}
        />
      )}

      {optionsField && (
        <FieldOptionsModal
          fieldId={optionsField.fieldDefinitionId}
          fieldLabel={optionsField.fieldLabel}
          onClose={() => setOptionsField(null)}
        />
      )}

      {showLibraryImport && (
        <ImportFromLibraryModal
          organizationId={organizationId}
          onClose={() => setShowLibraryImport(false)}
        />
      )}

      <ConfirmModal
        show={!!confirmTarget}
        title={confirmTarget?.newStatus ? `Activate ${confirmTarget?.type}` : `Deactivate ${confirmTarget?.type}`}
        message={
          confirmTarget?.newStatus
            ? 'This item will become visible in the form.'
            : 'This item will be hidden but data is retained for reporting.'
        }
        danger={!confirmTarget?.newStatus}
        onConfirm={confirmStatusChange}
        onCancel={() => setConfirmTarget(null)}
        loading={setFieldStatus.isPending || setSectionStatus.isPending}
      />
    </div>
  )
}
