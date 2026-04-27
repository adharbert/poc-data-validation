import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import {
  useFields, useFieldOptions, useCreateField, useUpdateField,
  useSetFieldStatus, useSaveFieldOptions,
} from '@/hooks/useApi.js'
import {
  PageHeader, LoadingState, ErrorAlert, FieldTypeBadge,
  StatusBadge, ConfirmModal, EmptyState, useToast,
} from '@/components/common/index.jsx'

const FIELD_TYPES = ['text', 'number', 'date', 'dropdown', 'multiselect', 'boolean']

function FieldModal({ organizationId, field, onClose }) {
  const toast  = useToast()
  const create = useCreateField(organizationId)
  const update = useUpdateField(organizationId)
  const isEdit = !!field

  const { register, handleSubmit, watch, formState: { errors } } = useForm({
    defaultValues: {
      organizationId,
      fieldKey:       field?.fieldKey       ?? '',
      fieldLabel:     field?.fieldLabel     ?? '',
      fieldType:      field?.fieldType      ?? 'text',
      isRequired:     field?.isRequired     ?? false,
      helpText:       field?.helpText       ?? '',
      validationRegex:field?.validationRegex ?? '',
      minValue:       field?.minValue       ?? '',
      maxValue:       field?.maxValue       ?? '',
      displayOrder:   field?.displayOrder   ?? '',
    },
  })

  const fieldType = watch('fieldType')
  const saving    = create.isPending || update.isPending

  async function onSubmit(values) {
    const payload = {
      ...values,
      minValue:     values.minValue     === '' ? null : Number(values.minValue),
      maxValue:     values.maxValue     === '' ? null : Number(values.maxValue),
      displayOrder: values.displayOrder === '' ? null : Number(values.displayOrder),
    }
    try {
      if (isEdit) {
        await update.mutateAsync({ fieldId: field.fieldId, data: payload })
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
              <h5 className="modal-title">{isEdit ? 'Edit Field' : 'New Field Definition'}</h5>
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
                  <label className="form-label">Display Order</label>
                  <input type="number" className="form-control" {...register('displayOrder')} />
                </div>
                <div className="col-4 d-flex align-items-end">
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
                    <input className="form-control font-monospace" {...register('validationRegex')}
                      placeholder="e.g. ^\d{5}$" />
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
                {(fieldType === 'dropdown' || fieldType === 'multiselect') && (
                  <div className="col-12">
                    <div className="alert alert-info py-2 mb-0" style={{ fontSize: '.85rem' }}>
                      After creating the field, use the <strong>Options</strong> button to add dropdown values.
                    </div>
                  </div>
                )}
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Field'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

function FieldOptionsModal({ fieldId, fieldLabel, onClose }) {
  const toast    = useToast()
  const { data: options, isLoading } = useFieldOptions(fieldId)
  const save     = useSaveFieldOptions(fieldId)
  const [items, setItems] = useState(null)

  const opts = items ?? (options ?? [])

  function addItem() {
    setItems([...opts, { optionValue: '', optionLabel: '', displayOrder: opts.length + 1, isActive: true }])
  }

  function updateItem(i, key, val) {
    setItems(opts.map((o, idx) => idx === i ? { ...o, [key]: val } : o))
  }

  function removeItem(i) {
    setItems(opts.filter((_, idx) => idx !== i))
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
                {opts.map((opt, i) => (
                  <div key={i} className="d-flex gap-2 mb-2 align-items-center">
                    <input className="form-control form-control-sm" placeholder="Value" value={opt.optionValue}
                      onChange={e => updateItem(i, 'optionValue', e.target.value)} />
                    <input className="form-control form-control-sm" placeholder="Label" value={opt.optionLabel || ''}
                      onChange={e => updateItem(i, 'optionLabel', e.target.value)} />
                    <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => removeItem(i)}>✕</button>
                  </div>
                ))}
                <button type="button" className="btn btn-sm btn-outline-secondary mt-1" onClick={addItem}>+ Add Option</button>
              </>
            )}
          </div>
          <div className="modal-footer">
            <button className="btn btn-outline-secondary" onClick={onClose}>Cancel</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={save.isPending}>
              {save.isPending ? 'Saving…' : 'Save Options'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

export default function FieldDefinitionsPage() {
  const { organizationId } = useParams()
  const [showInactive, setShowInactive] = useState(false)
  const [showCreate, setShowCreate]     = useState(false)
  const [editField, setEditField]       = useState(null)
  const [optionsField, setOptionsField] = useState(null)
  const [confirmId, setConfirmId]       = useState(null)
  const [targetStatus, setTargetStatus] = useState(null)

  const { data: fields, isLoading, isError } = useFields(organizationId, showInactive)
  const setStatus = useSetFieldStatus(organizationId)
  const toast     = useToast()

  async function confirmStatus() {
    try {
      await setStatus.mutateAsync({ fieldId: confirmId, isActive: targetStatus })
      toast(targetStatus ? 'Field activated.' : 'Field deactivated.')
    } catch (err) {
      toast(err.message ?? 'Failed to update status.', 'danger')
    } finally {
      setConfirmId(null)
    }
  }

  if (isLoading) return <LoadingState message="Loading field definitions…" />
  if (isError)   return <ErrorAlert message="Could not load field definitions." />

  return (
    <div>
      <PageHeader
        breadcrumbs={[
          { label: 'Organisations', href: '/organizations' },
          { label: 'Field Definitions' },
        ]}
        title="Field Definitions"
        subtitle="Configure the custom data fields for this organisation"
        actions={
          <>
            <div className="form-check form-switch mb-0 me-2">
              <input className="form-check-input" type="checkbox" id="showInactive"
                checked={showInactive} onChange={e => setShowInactive(e.target.checked)} />
              <label className="form-check-label text-muted-sm" htmlFor="showInactive">Show inactive</label>
            </div>
            <button className="btn btn-primary btn-sm" onClick={() => setShowCreate(true)}>+ New Field</button>
          </>
        }
      />

      <div className="admin-card p-0">
        {!fields?.length ? (
          <EmptyState icon="🗂️" title="No fields defined"
            description="Add field definitions to capture custom customer data."
            action={<button className="btn btn-primary btn-sm" onClick={() => setShowCreate(true)}>+ New Field</button>}
          />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Key</th>
                  <th>Label</th>
                  <th>Type</th>
                  <th>Required</th>
                  <th>Status</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {fields.map(f => (
                  <tr key={f.fieldId}>
                    <td className="text-muted-sm">{f.displayOrder ?? '—'}</td>
                    <td><code className="text-muted-sm">{f.fieldKey}</code></td>
                    <td className="fw-semibold">{f.fieldLabel}</td>
                    <td><FieldTypeBadge type={f.fieldType} /></td>
                    <td>{f.isRequired ? <span className="text-danger fw-semibold">Yes</span> : <span className="text-muted-sm">No</span>}</td>
                    <td><StatusBadge active={f.isActive} /></td>
                    <td>
                      <div className="gap-actions justify-content-end">
                        {(f.fieldType === 'dropdown' || f.fieldType === 'multiselect') && (
                          <button className="btn btn-sm btn-outline-secondary" onClick={() => setOptionsField(f)}>
                            Options
                          </button>
                        )}
                        <button className="btn btn-sm btn-outline-secondary" onClick={() => setEditField(f)}>Edit</button>
                        <button
                          className={`btn btn-sm ${f.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`}
                          onClick={() => { setConfirmId(f.fieldId); setTargetStatus(!f.isActive) }}
                        >
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

      {(showCreate || editField) && (
        <FieldModal
          organizationId={organizationId}
          field={editField}
          onClose={() => { setShowCreate(false); setEditField(null) }}
        />
      )}

      {optionsField && (
        <FieldOptionsModal
          fieldId={optionsField.fieldId}
          fieldLabel={optionsField.fieldLabel}
          onClose={() => setOptionsField(null)}
        />
      )}

      <ConfirmModal
        show={!!confirmId}
        title={targetStatus ? 'Activate Field' : 'Deactivate Field'}
        message={targetStatus ? 'This field will appear to customers.' : 'This field will be hidden but data is retained.'}
        danger={!targetStatus}
        onConfirm={confirmStatus}
        onCancel={() => setConfirmId(null)}
        loading={setStatus.isPending}
      />
    </div>
  )
}
