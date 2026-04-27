import { createContext, useContext, useState, useCallback } from 'react'
import { Modal } from 'bootstrap'

// ---------------------------------------------------------------------------
// Spinner
// ---------------------------------------------------------------------------
export function Spinner({ size = 'md' }) {
  const sz = size === 'sm' ? 'spinner-border-sm' : size === 'lg' ? '' : ''
  return <div className={`spinner-border ${sz} text-primary`} role="status"><span className="visually-hidden">Loading…</span></div>
}

// ---------------------------------------------------------------------------
// StatusBadge
// ---------------------------------------------------------------------------
export function StatusBadge({ active }) {
  return active
    ? <span className="badge-active">Active</span>
    : <span className="badge-inactive">Inactive</span>
}

// ---------------------------------------------------------------------------
// FieldTypeBadge
// ---------------------------------------------------------------------------
const TYPE_CLASS = {
  text: 'badge-text', number: 'badge-number', date: 'badge-date',
  dropdown: 'badge-dropdown', multiselect: 'badge-multiselect', boolean: 'badge-boolean',
}
export function FieldTypeBadge({ type }) {
  const cls = TYPE_CLASS[type?.toLowerCase()] ?? ''
  return <span className={`badge-type ${cls}`}>{type}</span>
}

// ---------------------------------------------------------------------------
// ImportStatusBadge
// ---------------------------------------------------------------------------
export function ImportStatusBadge({ status }) {
  return <span className={`badge-status status-${status?.toLowerCase()}`}>{status}</span>
}

// ---------------------------------------------------------------------------
// EmptyState
// ---------------------------------------------------------------------------
export function EmptyState({ icon = '📭', title, description, action }) {
  return (
    <div className="empty-state">
      <div className="empty-icon">{icon}</div>
      <div className="empty-title">{title}</div>
      {description && <div className="empty-desc">{description}</div>}
      {action && <div className="mt-3">{action}</div>}
    </div>
  )
}

// ---------------------------------------------------------------------------
// PageHeader
// ---------------------------------------------------------------------------
export function PageHeader({ title, subtitle, actions, breadcrumbs }) {
  return (
    <>
      {breadcrumbs && (
        <nav className="breadcrumb-bar">
          {breadcrumbs.map((crumb, i) => (
            <span key={i}>
              {i > 0 && <span className="sep">›</span>}
              {crumb.href
                ? <a href={crumb.href}>{crumb.label}</a>
                : <span className="current">{crumb.label}</span>}
            </span>
          ))}
        </nav>
      )}
      <div className="page-header">
        <div className="page-header-title">
          <h1>{title}</h1>
          {subtitle && <p>{subtitle}</p>}
        </div>
        {actions && <div className="gap-actions">{actions}</div>}
      </div>
    </>
  )
}

// ---------------------------------------------------------------------------
// LoadingState
// ---------------------------------------------------------------------------
export function LoadingState({ message = 'Loading…' }) {
  return (
    <div className="d-flex flex-column align-items-center justify-content-center py-5 gap-3">
      <Spinner />
      <span className="text-muted-sm">{message}</span>
    </div>
  )
}

// ---------------------------------------------------------------------------
// ErrorAlert
// ---------------------------------------------------------------------------
export function ErrorAlert({ message }) {
  return (
    <div className="alert alert-danger d-flex align-items-center gap-2" role="alert">
      <span>⚠️</span>
      <span>{message ?? 'An unexpected error occurred.'}</span>
    </div>
  )
}

// ---------------------------------------------------------------------------
// ConfirmModal
// ---------------------------------------------------------------------------
export function ConfirmModal({ show, title, message, onConfirm, onCancel, danger = false, loading = false }) {
  if (!show) return null
  return (
    <div className="modal show d-block" tabIndex="-1" style={{ background: 'rgba(0,0,0,.45)' }}>
      <div className="modal-dialog modal-sm modal-dialog-centered">
        <div className="modal-content">
          <div className="modal-header border-0 pb-0">
            <h5 className="modal-title">{title}</h5>
          </div>
          <div className="modal-body pt-2 text-muted" style={{ fontSize: '.9rem' }}>{message}</div>
          <div className="modal-footer border-0 pt-0 gap-2">
            <button className="btn btn-sm btn-outline-secondary" onClick={onCancel} disabled={loading}>Cancel</button>
            <button
              className={`btn btn-sm ${danger ? 'btn-danger' : 'btn-primary'}`}
              onClick={onConfirm}
              disabled={loading}
            >
              {loading ? <Spinner size="sm" /> : 'Confirm'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Pagination
// ---------------------------------------------------------------------------
export function Pagination({ page, pageSize, total, onChange }) {
  const totalPages = Math.ceil(total / pageSize)
  if (totalPages <= 1) return null
  return (
    <nav className="d-flex align-items-center justify-content-between mt-3" aria-label="Pagination">
      <span className="text-muted-sm">
        {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, total)} of {total}
      </span>
      <ul className="pagination pagination-sm mb-0">
        <li className={`page-item ${page <= 1 ? 'disabled' : ''}`}>
          <button className="page-link" onClick={() => onChange(page - 1)}>‹</button>
        </li>
        {Array.from({ length: totalPages }, (_, i) => i + 1)
          .filter(p => Math.abs(p - page) <= 2)
          .map(p => (
            <li key={p} className={`page-item ${p === page ? 'active' : ''}`}>
              <button className="page-link" onClick={() => onChange(p)}>{p}</button>
            </li>
          ))}
        <li className={`page-item ${page >= totalPages ? 'disabled' : ''}`}>
          <button className="page-link" onClick={() => onChange(page + 1)}>›</button>
        </li>
      </ul>
    </nav>
  )
}

// ---------------------------------------------------------------------------
// Toast context
// ---------------------------------------------------------------------------
const ToastContext = createContext(null)

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([])

  const showToast = useCallback((message, variant = 'success') => {
    const id = Date.now()
    setToasts(prev => [...prev, { id, message, variant }])
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 4000)
  }, [])

  return (
    <ToastContext.Provider value={showToast}>
      {children}
      <div className="toast-container position-fixed bottom-0 end-0 p-3" style={{ zIndex: 1100 }}>
        {toasts.map(t => (
          <div key={t.id} className={`toast show align-items-center text-bg-${t.variant} border-0 mb-2`} role="alert">
            <div className="d-flex">
              <div className="toast-body">{t.message}</div>
              <button type="button" className="btn-close btn-close-white me-2 m-auto"
                onClick={() => setToasts(prev => prev.filter(x => x.id !== t.id))} />
            </div>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

export const useToast = () => useContext(ToastContext)
