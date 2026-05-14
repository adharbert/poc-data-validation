import { useState, useRef } from 'react'
import { useForm } from 'react-hook-form'
import { parseOptionsCsv } from '@/utils/csv.js'
import {
  useLibrarySections, useCreateLibrarySection, useUpdateLibrarySection, useSetLibrarySectionStatus,
  useAssignLibraryFields, useLibraryFields, useCreateLibraryField, useUpdateLibraryField,
  useSetLibraryFieldStatus, useBulkUpsertLibraryOptions,
} from '@/hooks/useApi.js'
import {
  PageHeader, LoadingState, ErrorAlert, EmptyState, useToast,
} from '@/components/common/index.jsx'

const FIELD_TYPES = ['text', 'number', 'date', 'datetime', 'checkbox', 'dropdown', 'multiselect', 'phone']

// ---------------------------------------------------------------------------
// Field Options modal (reused for library fields)
// ---------------------------------------------------------------------------
function LibraryOptionsModal({ field, onClose }) {
  const toast    = useToast()
  const save     = useBulkUpsertLibraryOptions()
  const fileRef  = useRef()
  const [items, setItems] = useState(
    (field.options ?? []).map(o => ({ optionKey: o.optionKey, optionLabel: o.optionLabel, displayOrder: o.displayOrder }))
  )

  function addItem() {
    setItems([...items, { optionKey: '', optionLabel: '', displayOrder: items.length + 1 }])
  }
  function updateItem(i, key, val) {
    setItems(items.map((o, idx) => idx === i ? { ...o, [key]: val } : o))
  }
  function removeItem(i) {
    setItems(items.filter((_, idx) => idx !== i))
  }

  function handleFileUpload(e) {
    const file = e.target.files[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = evt => {
      const parsed = parseOptionsCsv(evt.target.result)
      setItems(parsed)
      toast(`Loaded ${parsed.length} options from file.`)
    }
    reader.readAsText(file)
    e.target.value = ''
  }

  async function handleSave() {
    try {
      await save.mutateAsync({ fieldId: field.id, options: items })
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
            <h5 className="modal-title">Options — {field.fieldLabel}</h5>
            <button type="button" className="btn-close" onClick={onClose} />
          </div>
          <div className="modal-body">
            <div style={{ maxHeight: 320, overflowY: 'auto' }}>
              {items.map((opt, i) => (
                <div key={i} className="d-flex gap-2 mb-2 align-items-center">
                  <input className="form-control form-control-sm" placeholder="Key (e.g. AL)"
                    value={opt.optionKey}
                    onChange={e => updateItem(i, 'optionKey', e.target.value)} />
                  <input className="form-control form-control-sm" placeholder="Label (e.g. Alabama)"
                    value={opt.optionLabel}
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
          </div>
          <div className="modal-footer">
            <button className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={save.isPending}>
              {save.isPending ? 'Saving…' : `Save Options (${items.length})`}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Library Field modal (create / edit)
// ---------------------------------------------------------------------------
function LibraryFieldModal({ field, onClose }) {
  const toast   = useToast()
  const create  = useCreateLibraryField()
  const update  = useUpdateLibraryField()
  const isEdit  = !!field

  const { register, handleSubmit, watch, formState: { errors } } = useForm({
    defaultValues: {
      fieldKey:       field?.fieldKey       ?? '',
      fieldLabel:     field?.fieldLabel     ?? '',
      fieldType:      field?.fieldType      ?? 'text',
      helpText:       field?.helpText       ?? '',
      isRequired:     field?.isRequired     ?? false,
      displayOrder:   field?.displayOrder   ?? 0,
      displayFormat:  field?.displayFormat  ?? '',
      minValue:       field?.minValue       ?? '',
      maxValue:       field?.maxValue       ?? '',
      minLength:      field?.minLength      ?? '',
      maxLength:      field?.maxLength      ?? '',
    },
  })

  const fieldType = watch('fieldType')
  const saving    = create.isPending || update.isPending

  async function onSubmit(values) {
    const payload = {
      ...values,
      displayOrder: values.displayOrder === '' ? 0 : Number(values.displayOrder),
      minValue:     values.minValue     === '' ? null : Number(values.minValue),
      maxValue:     values.maxValue     === '' ? null : Number(values.maxValue),
      minLength:    values.minLength    === '' ? null : Number(values.minLength),
      maxLength:    values.maxLength    === '' ? null : Number(values.maxLength),
      displayFormat: fieldType === 'phone' ? values.displayFormat || null : null,
    }
    try {
      if (isEdit) {
        await update.mutateAsync({ id: field.id, data: { ...payload, isActive: field.isActive } })
        toast('Field updated.')
      } else {
        await create.mutateAsync(payload)
        toast('Field created.')
      }
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving field.', 'danger')
    }
  }

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-lg modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">{isEdit ? 'Edit Library Field' : 'New Library Field'}</h5>
              <button type="button" className="btn-close" onClick={onClose} />
            </div>
            <div className="modal-body">
              <div className="row g-3">
                <div className="col-6">
                  <label className="form-label fw-semibold">Field Key <span className="text-danger">*</span></label>
                  <input className={`form-control ${errors.fieldKey ? 'is-invalid' : ''}`}
                    {...register('fieldKey', { required: 'Required' })} disabled={isEdit} />
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
                  <label className="form-label fw-semibold">Type</label>
                  <select className="form-select" {...register('fieldType')}>
                    {FIELD_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                  </select>
                </div>
                <div className="col-4">
                  <label className="form-label">Display Order</label>
                  <input type="number" className="form-control" {...register('displayOrder')} />
                </div>
                <div className="col-4 d-flex align-items-end pb-1">
                  <div className="form-check">
                    <input className="form-check-input" type="checkbox" id="libIsRequired" {...register('isRequired')} />
                    <label className="form-check-label fw-semibold" htmlFor="libIsRequired">Required</label>
                  </div>
                </div>
                <div className="col-12">
                  <label className="form-label">Help Text</label>
                  <input className="form-control" {...register('helpText')} />
                </div>
                {fieldType === 'phone' && (
                  <div className="col-6">
                    <label className="form-label">Display Format</label>
                    <select className="form-select" {...register('displayFormat')}>
                      <option value="(XXX) XXX-XXXX">(XXX) XXX-XXXX</option>
                      <option value="XXX-XXX-XXXX">XXX-XXX-XXXX</option>
                      <option value="XXX.XXX.XXXX">XXX.XXX.XXXX</option>
                    </select>
                  </div>
                )}
                {(fieldType === 'number') && (
                  <>
                    <div className="col-3">
                      <label className="form-label">Min Value</label>
                      <input type="number" className="form-control" {...register('minValue')} />
                    </div>
                    <div className="col-3">
                      <label className="form-label">Max Value</label>
                      <input type="number" className="form-control" {...register('maxValue')} />
                    </div>
                  </>
                )}
                {(fieldType === 'text') && (
                  <>
                    <div className="col-3">
                      <label className="form-label">Min Length</label>
                      <input type="number" className="form-control" {...register('minLength')} />
                    </div>
                    <div className="col-3">
                      <label className="form-label">Max Length</label>
                      <input type="number" className="form-control" {...register('maxLength')} />
                    </div>
                  </>
                )}
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Library Section modal
// ---------------------------------------------------------------------------
function LibrarySectionModal({ section, allFields, onClose }) {
  const toast    = useToast()
  const create   = useCreateLibrarySection()
  const update   = useUpdateLibrarySection()
  const assign   = useAssignLibraryFields()
  const isEdit   = !!section

  const currentFieldIds = (section?.fields ?? []).map(f => f.id)
  const [selectedIds, setSelectedIds] = useState(currentFieldIds)

  const { register, handleSubmit, formState: { errors } } = useForm({
    defaultValues: {
      sectionName:  section?.sectionName  ?? '',
      description:  section?.description  ?? '',
      displayOrder: section?.displayOrder ?? 0,
    },
  })

  function toggleField(id) {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id])
  }

  async function onSubmit(values) {
    try {
      let sectionId
      if (isEdit) {
        await update.mutateAsync({ id: section.id, data: { ...values, displayOrder: Number(values.displayOrder), isActive: section.isActive } })
        sectionId = section.id
        toast('Section updated.')
      } else {
        const created = await create.mutateAsync({ ...values, displayOrder: Number(values.displayOrder) })
        sectionId = created.id
        toast('Section created.')
      }
      await assign.mutateAsync({
        sectionId,
        fields: selectedIds.map((id, i) => ({ libraryFieldId: id, displayOrder: i + 1 })),
      })
      onClose()
    } catch (err) {
      toast(err.message ?? 'Error saving section.', 'danger')
    }
  }

  const saving = create.isPending || update.isPending || assign.isPending

  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-lg modal-dialog-centered">
        <div className="modal-content">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="modal-header">
              <h5 className="modal-title">{isEdit ? 'Edit Library Section' : 'New Library Section'}</h5>
              <button type="button" className="btn-close" onClick={onClose} />
            </div>
            <div className="modal-body">
              <div className="row g-3 mb-3">
                <div className="col-8">
                  <label className="form-label fw-semibold">Section Name <span className="text-danger">*</span></label>
                  <input className={`form-control ${errors.sectionName ? 'is-invalid' : ''}`}
                    {...register('sectionName', { required: 'Required' })} />
                  {errors.sectionName && <div className="invalid-feedback">{errors.sectionName.message}</div>}
                </div>
                <div className="col-4">
                  <label className="form-label">Display Order</label>
                  <input type="number" className="form-control" {...register('displayOrder')} />
                </div>
                <div className="col-12">
                  <label className="form-label">Description</label>
                  <input className="form-control" {...register('description')} />
                </div>
              </div>

              <label className="form-label fw-semibold">Fields in this section</label>
              <div style={{ maxHeight: 280, overflowY: 'auto', border: '1px solid #dee2e6', borderRadius: 6, padding: '0.5rem' }}>
                {(allFields ?? []).filter(f => f.isActive).map(f => (
                  <div key={f.id} className="form-check">
                    <input className="form-check-input" type="checkbox"
                      id={`lf-${f.id}`}
                      checked={selectedIds.includes(f.id)}
                      onChange={() => toggleField(f.id)} />
                    <label className="form-check-label" htmlFor={`lf-${f.id}`}>
                      <span className="fw-semibold">{f.fieldLabel}</span>
                      <span className="text-muted ms-2" style={{ fontSize: '.8rem' }}>({f.fieldType})</span>
                    </label>
                  </div>
                ))}
                {!(allFields ?? []).length && <div className="text-muted" style={{ fontSize: '.85rem' }}>No library fields yet.</div>}
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------
export default function LibraryPage() {
  const toast = useToast()
  const [tab, setTab] = useState('sections')
  const [showSectionModal, setShowSectionModal] = useState(false)
  const [editSection, setEditSection]           = useState(null)
  const [showFieldModal, setShowFieldModal]     = useState(false)
  const [editField, setEditField]               = useState(null)
  const [optionsField, setOptionsField]         = useState(null)
  const [showInactive, setShowInactive]         = useState(false)

  const { data: sections = [], isLoading: sectionsLoading, isError: sectionsError } = useLibrarySections(showInactive)
  const { data: fields   = [], isLoading: fieldsLoading,   isError: fieldsError   } = useLibraryFields(showInactive)

  const setSectionStatus = useSetLibrarySectionStatus()
  const setFieldStatus   = useSetLibraryFieldStatus()

  async function handleSectionStatus(section) {
    try {
      await setSectionStatus.mutateAsync({ id: section.id, isActive: !section.isActive })
      toast(section.isActive ? 'Section deactivated.' : 'Section activated.')
    } catch (err) {
      toast(err.message ?? 'Error updating status.', 'danger')
    }
  }

  async function handleFieldStatus(field) {
    try {
      await setFieldStatus.mutateAsync({ id: field.id, isActive: !field.isActive })
      toast(field.isActive ? 'Field deactivated.' : 'Field activated.')
    } catch (err) {
      toast(err.message ?? 'Error updating status.', 'danger')
    }
  }

  if (sectionsLoading || fieldsLoading) return <LoadingState message="Loading library…" />
  if (sectionsError || fieldsError) return <ErrorAlert message="Could not load field library." />

  return (
    <div>
      <PageHeader
        title="Field Library"
        subtitle="Reusable sections and fields for all organisations"
        actions={
          <>
            <div className="form-check form-switch mb-0 me-2">
              <input className="form-check-input" type="checkbox" id="showInactiveLib"
                checked={showInactive} onChange={e => setShowInactive(e.target.checked)} />
              <label className="form-check-label text-muted-sm" htmlFor="showInactiveLib">Show inactive</label>
            </div>
            {tab === 'sections'
              ? <button className="btn btn-primary btn-sm" onClick={() => setShowSectionModal(true)}>+ New Section</button>
              : <button className="btn btn-primary btn-sm" onClick={() => setShowFieldModal(true)}>+ New Field</button>
            }
          </>
        }
      />

      {/* Tab toggle */}
      <ul className="nav nav-tabs mb-3">
        <li className="nav-item">
          <button className={`nav-link ${tab === 'sections' ? 'active' : ''}`} onClick={() => setTab('sections')}>
            Sections ({sections.length})
          </button>
        </li>
        <li className="nav-item">
          <button className={`nav-link ${tab === 'fields' ? 'active' : ''}`} onClick={() => setTab('fields')}>
            Fields ({fields.length})
          </button>
        </li>
      </ul>

      {/* Sections tab */}
      {tab === 'sections' && (
        <div className="admin-card p-0">
          {!sections.length ? (
            <EmptyState icon="📋" title="No library sections"
              description="Create a section to group common fields."
              action={<button className="btn btn-primary btn-sm" onClick={() => setShowSectionModal(true)}>New Section</button>} />
          ) : (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Section</th>
                    <th>Description</th>
                    <th>Fields</th>
                    <th>Order</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {sections.map(s => (
                    <tr key={s.id}>
                      <td className="fw-semibold">{s.sectionName}</td>
                      <td className="text-muted-sm">{s.description ?? '—'}</td>
                      <td>{(s.fields ?? []).length}</td>
                      <td>{s.displayOrder}</td>
                      <td>
                        <span className={`badge ${s.isActive ? 'bg-success-subtle text-success-emphasis' : 'bg-secondary-subtle text-secondary-emphasis'}`}>
                          {s.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td>
                        <div className="gap-actions justify-content-end">
                          <button className="btn btn-sm btn-outline-secondary" onClick={() => { setEditSection(s); setShowSectionModal(true) }}>Edit</button>
                          <button className="btn btn-sm btn-outline-secondary" onClick={() => handleSectionStatus(s)}>
                            {s.isActive ? 'Deactivate' : 'Activate'}
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
      )}

      {/* Fields tab */}
      {tab === 'fields' && (
        <div className="admin-card p-0">
          {!fields.length ? (
            <EmptyState icon="🗂️" title="No library fields"
              description="Create reusable fields to add to your sections."
              action={<button className="btn btn-primary btn-sm" onClick={() => setShowFieldModal(true)}>New Field</button>} />
          ) : (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Key</th>
                    <th>Label</th>
                    <th>Type</th>
                    <th>Required</th>
                    <th>Options</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {fields.map(f => (
                    <tr key={f.id}>
                      <td><code className="text-muted-sm">{f.fieldKey}</code></td>
                      <td className="fw-semibold">{f.fieldLabel}</td>
                      <td><span className="badge bg-secondary-subtle text-secondary-emphasis">{f.fieldType}</span></td>
                      <td>{f.isRequired ? 'Yes' : '—'}</td>
                      <td>
                        {['dropdown', 'multiselect'].includes(f.fieldType)
                          ? <button className="btn btn-sm btn-link p-0" onClick={() => setOptionsField(f)}>
                              {(f.options ?? []).length} options
                            </button>
                          : '—'
                        }
                      </td>
                      <td>
                        <span className={`badge ${f.isActive ? 'bg-success-subtle text-success-emphasis' : 'bg-secondary-subtle text-secondary-emphasis'}`}>
                          {f.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td>
                        <div className="gap-actions justify-content-end">
                          <button className="btn btn-sm btn-outline-secondary" onClick={() => { setEditField(f); setShowFieldModal(true) }}>Edit</button>
                          <button className="btn btn-sm btn-outline-secondary" onClick={() => handleFieldStatus(f)}>
                            {f.isActive ? 'Deactivate' : 'Activate'}
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
      )}

      {/* Modals */}
      {(showSectionModal) && (
        <LibrarySectionModal
          section={editSection}
          allFields={fields}
          onClose={() => { setShowSectionModal(false); setEditSection(null) }}
        />
      )}

      {(showFieldModal) && (
        <LibraryFieldModal
          field={editField}
          onClose={() => { setShowFieldModal(false); setEditField(null) }}
        />
      )}

      {optionsField && (
        <LibraryOptionsModal
          field={optionsField}
          onClose={() => setOptionsField(null)}
        />
      )}
    </div>
  )
}
