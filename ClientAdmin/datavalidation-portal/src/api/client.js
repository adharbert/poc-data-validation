// VITE_API_BASE_URL is set per environment in .env.* files.
// Leave empty to use the Vite dev proxy (local) or the Azure SWA linked-backend proxy (cloud).
// Set to a full URL (e.g. https://api.myapp.com) when the API is on a separate domain.
const API_ORIGIN = import.meta.env.VITE_API_BASE_URL ?? ''
const BASE = `${API_ORIGIN}/api`

function buildUrl(path, params) {
  // new URL() ignores the second arg when the first is already absolute
  const url = new URL(BASE + path, window.location.origin)
  if (params) {
    Object.entries(params).forEach(([k, v]) => {
      if (v !== undefined && v !== null) url.searchParams.set(k, v)
    })
  }
  return url.toString()
}

async function request(method, path, { params, body, isFormData } = {}) {
  const url    = buildUrl(path, params)
  const headers = isFormData ? {} : { 'Content-Type': 'application/json' }

  const res = await fetch(url, {
    method,
    headers,
    body: isFormData ? body : body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (!res.ok) {
    let err = { code: 'HTTP_ERROR', message: `HTTP ${res.status}` }
    try { err = await res.json() } catch { /* non-JSON error body */ }
    const error = new Error(err.message || `HTTP ${res.status}`)
    error.code   = err.code
    error.status = res.status
    throw error
  }

  if (res.status === 204) return { data: null }

  const data = await res.json()
  return { data }
}

const api = {
  get:    (path, options)             => request('GET',    path, options),
  post:   (path, body, options)       => request('POST',   path, { ...options, body }),
  put:    (path, body, options)       => request('PUT',    path, { ...options, body }),
  patch:  (path, body, options)       => request('PATCH',  path, { ...options, body }),
  delete: (path, options)             => request('DELETE', path, options),
  upload: (path, formData, options)   => request('POST',   path, { ...options, body: formData, isFormData: true }),
}

export default api
