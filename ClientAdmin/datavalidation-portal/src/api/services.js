import api from './client.js'

// ---------------------------------------------------------------------------
// Organizations  (American spelling — existing controller)
// ---------------------------------------------------------------------------
export const orgApi = {
  getAll:       (includeInactive = false, search = null) => api.get('/organizations', { params: { includeInactive, ...(search ? { search } : {}) } }).then(r => r.data),
  getById:      (id)                                => api.get(`/organizations/${id}`).then(r => r.data),
  create:       (data)                              => api.post('/organizations', data).then(r => r.data),
  update:       (id, data)                          => api.put(`/organizations/${id}`, data).then(r => r.data),
  setStatus:    (id, status)                        => api.put(`/organizations/${id}/status/${status}`).then(r => r.data),
  reprovision:      (id)   => api.post(`/organizations/${id}/reprovision`).then(r => r.data),
  migrateIsolated:  ()     => api.post('/organizations/migrate-isolated').then(r => r.data),
}

// ---------------------------------------------------------------------------
// Fields
// organizationId must be passed to all mutations so TenantResolutionMiddleware
// can route the request to the correct isolated database (query-string fallback).
// ---------------------------------------------------------------------------
export const fieldApi = {
  getAll:    (organizationId, includeInactive = false) =>
    api.get('/fields', { params: { organizationId, includeInactive } }).then(r => r.data),
  getById:   (fieldId) => api.get(`/fields/${fieldId}`).then(r => r.data),

  // Uses the org-scoped route so middleware resolves tenant from the route segment.
  create:    (organizationId, data) =>
    api.post(`/organisations/${organizationId}/fields`, data).then(r => r.data),

  // Pass organizationId as query param — middleware picks it up via query string fallback.
  update:    (organizationId, fieldId, data) =>
    api.put(`/fields/${fieldId}?organizationId=${organizationId}`, data).then(r => r.data),
  setStatus: (organizationId, fieldId, isActive) =>
    api.patch(`/fields/${fieldId}/status/${isActive}?organizationId=${organizationId}`).then(r => r.data),
  reorder:   (organizationId, data) =>
    api.post(`/fields/reorder?organizationId=${organizationId}`, data).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Field Options
// ---------------------------------------------------------------------------
export const fieldOptionApi = {
  // organizationId forwarded so middleware routes to the isolated DB on both read and write.
  getAll: (organizationId, fieldId) => api.get(`/fields/${fieldId}/options?organizationId=${organizationId}`).then(r => r.data),
  save:   (organizationId, fieldId, data) =>
    api.put(`/fields/${fieldId}/options/bulk?organizationId=${organizationId}`, { options: data }).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Customers  (British spelling — new controller)
// ---------------------------------------------------------------------------
export const customerApi = {
  getAll:       (orgId, { includeInactive = false, page = 1, pageSize = 50 } = {}) =>
    api.get(`/organisations/${orgId}/customers`, { params: { includeInactive, page, pageSize } }).then(r => r.data),
  getById:      (orgId, customerId)          => api.get(`/organisations/${orgId}/customers/${customerId}`).then(r => r.data),
  create:       (orgId, data)                => api.post(`/organisations/${orgId}/customers`, data).then(r => r.data),
  update:       (orgId, customerId, data)    => api.put(`/organisations/${orgId}/customers/${customerId}`, data).then(r => r.data),
  setStatus:    (orgId, customerId, isActive) => api.patch(`/organisations/${orgId}/customers/${customerId}/status`, { isActive }).then(r => r.data),
  getEmails:    (orgId, customerId)          => api.get(`/organisations/${orgId}/customers/${customerId}/emails`).then(r => r.data),
  getPhones:    (orgId, customerId)          => api.get(`/organisations/${orgId}/customers/${customerId}/phones`).then(r => r.data),
  getAddresses: (customerId)                 => api.get(`/customers/${customerId}/addresses`).then(r => r.data),
  getFieldValues: (customerId)               => api.get(`/customers/${customerId}/values`).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Contracts
// ---------------------------------------------------------------------------
export const contractApi = {
  getAll:       (orgId, includeInactive = false)        => api.get(`/organisations/${orgId}/contracts`, { params: { includeInactive } }).then(r => r.data),
  getById:      (orgId, contractId)                     => api.get(`/organisations/${orgId}/contracts/${contractId}`).then(r => r.data),
  create:       (orgId, data)                           => api.post(`/organisations/${orgId}/contracts`, data).then(r => r.data),
  update:       (orgId, contractId, data)               => api.put(`/organisations/${orgId}/contracts/${contractId}`, data).then(r => r.data),
  setStatus:    (orgId, contractId, isActive, modifiedBy = 'Admin') =>
    api.patch(`/organisations/${orgId}/contracts/${contractId}/status`, { isActive, modifiedBy }).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Contract Documents
// ---------------------------------------------------------------------------
export const contractDocumentApi = {
  getAll:      (orgId, contractId)            => api.get(`/organisations/${orgId}/contracts/${contractId}/documents`).then(r => r.data),
  upload:      (orgId, contractId, formData)  => api.upload(`/organisations/${orgId}/contracts/${contractId}/documents`, formData).then(r => r.data),
  delete:      (orgId, contractId, docId)     => api.delete(`/organisations/${orgId}/contracts/${contractId}/documents/${docId}`).then(r => r.data),
  // Returns a URL that streams the file inline in the browser (PDF opens directly, images render, etc.)
  downloadUrl: (orgId, contractId, docId)     => `${import.meta.env.VITE_API_BASE_URL ?? ''}/api/organisations/${orgId}/contracts/${contractId}/documents/${docId}`,
}

// ---------------------------------------------------------------------------
// Marketing Projects
// ---------------------------------------------------------------------------
export const projectApi = {
  getAll:       (orgId, includeInactive = false)        => api.get(`/organisations/${orgId}/projects`, { params: { includeInactive } }).then(r => r.data),
  getById:      (orgId, projectId)                      => api.get(`/organisations/${orgId}/projects/${projectId}`).then(r => r.data),
  create:       (orgId, data)                           => api.post(`/organisations/${orgId}/projects`, data).then(r => r.data),
  update:       (orgId, projectId, data)                => api.put(`/organisations/${orgId}/projects/${projectId}`, data).then(r => r.data),
  setStatus:    (orgId, projectId, isActive, modifiedBy = 'Admin') =>
    api.patch(`/organisations/${orgId}/projects/${projectId}/status`, { isActive, modifiedBy }).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Field Sections
// ---------------------------------------------------------------------------
export const sectionApi = {
  getAll:       (orgId)                       => api.get(`/organisations/${orgId}/sections`).then(r => r.data),
  getById:      (orgId, sectionId)            => api.get(`/organisations/${orgId}/sections/${sectionId}`).then(r => r.data),
  create:       (orgId, data)                 => api.post(`/organisations/${orgId}/sections`, data).then(r => r.data),
  update:       (orgId, sectionId, data)      => api.put(`/organisations/${orgId}/sections/${sectionId}`, data).then(r => r.data),
  setStatus:    (orgId, sectionId, isActive)  => api.patch(`/organisations/${orgId}/sections/${sectionId}/status`, { isActive }).then(r => r.data),
  reorder:      (orgId, sections)             => api.post(`/organisations/${orgId}/sections/reorder`, { sections }).then(r => r.data),
  assignFields: (orgId, sectionId, fields)    => api.put(`/organisations/${orgId}/sections/${sectionId}/fields`, { fields }).then(r => r.data),
  formPreview:  (orgId, customerId)           => api.get(`/organisations/${orgId}/customers/${customerId}/form-preview`).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Dashboard
// ---------------------------------------------------------------------------
export const dashboardApi = {
  getStats:            () => api.get('/dashboard/stats').then(r => r.data),
  getExpiringProjects: () => api.get('/dashboard/expiring-projects').then(r => r.data),
}

// ---------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------
export const importApi = {
  upload:          (orgId, formData)             => api.upload(`/organisations/${orgId}/imports`, formData).then(r => r.data),
  getBatches:      (orgId, page = 1, pageSize = 20) => api.get(`/organisations/${orgId}/imports`, { params: { page, pageSize } }).then(r => r.data),
  getBatch:        (orgId, batchId)              => api.get(`/organisations/${orgId}/imports/${batchId}`).then(r => r.data),
  getSavedMappings:(orgId, fingerprint)          => api.get(`/organisations/${orgId}/imports/saved-mappings`, { params: { fingerprint } }).then(r => r.data),
  saveMappings:    (orgId, batchId, data)        => api.post(`/organisations/${orgId}/imports/${batchId}/mappings`, data).then(r => r.data),
  getValueMapping: (orgId, batchId)              => api.get(`/organisations/${orgId}/imports/${batchId}/value-mapping`).then(r => r.data),
  preview:         (orgId, batchId)              => api.post(`/organisations/${orgId}/imports/${batchId}/preview`).then(r => r.data),
  execute:         (orgId, batchId)              => api.post(`/organisations/${orgId}/imports/${batchId}/execute`).then(r => r.data),
  getErrors:       (orgId, batchId)              => api.get(`/organisations/${orgId}/imports/${batchId}/errors`).then(r => r.data),
  resume:          (orgId, batchId)              => api.post(`/organisations/${orgId}/imports/${batchId}/resume`).then(r => r.data),
  cancel:          (orgId, batchId)              => api.post(`/organisations/${orgId}/imports/${batchId}/cancel`).then(r => r.data),
  reset:           (orgId, batchId, targetStatus = 'pending') =>
    api.post(`/organisations/${orgId}/imports/${batchId}/reset?targetStatus=${targetStatus}`).then(r => r.data),
  deleteBatch:     (orgId, batchId)              => api.delete(`/organisations/${orgId}/imports/${batchId}`).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Field Library
// ---------------------------------------------------------------------------
export const libraryApi = {
  getSections:        (includeInactive = false)   => api.get('/library/sections', { params: { includeInactive } }).then(r => r.data),
  createSection:      (data)                       => api.post('/library/sections', data).then(r => r.data),
  updateSection:      (id, data)                   => api.put(`/library/sections/${id}`, data).then(r => r.data),
  setSectionStatus:   (id, isActive)               => api.patch(`/library/sections/${id}/status`, { isActive }).then(r => r.data),
  assignFields:       (sectionId, fields)          => api.put(`/library/sections/${sectionId}/fields`, { fields }).then(r => r.data),

  getFields:          (includeInactive = false)    => api.get('/library/fields', { params: { includeInactive } }).then(r => r.data),
  createField:        (data)                       => api.post('/library/fields', data).then(r => r.data),
  updateField:        (id, data)                   => api.put(`/library/fields/${id}`, data).then(r => r.data),
  setFieldStatus:     (id, isActive)               => api.patch(`/library/fields/${id}/status`, { isActive }).then(r => r.data),
  bulkUpsertOptions:  (fieldId, data)              => api.put(`/library/fields/${fieldId}/options/bulk`, { options: data }).then(r => r.data),

  importToOrg: (organizationId, sectionIds) =>
    api.post(`/library/import-to-org?organizationId=${organizationId}`, { organizationId, sectionIds }).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Ingestion Pipeline
// ---------------------------------------------------------------------------
export const ingestionApi = {
  upload:          (orgId, formData)                    => api.upload(`/organisations/${orgId}/ingestion/upload`, formData).then(r => r.data),
  getJobs:         (orgId, page = 1, pageSize = 20)     => api.get(`/organisations/${orgId}/ingestion`, { params: { page, pageSize } }).then(r => r.data),
  getJob:          (orgId, jobId)                       => api.get(`/organisations/${orgId}/ingestion/${jobId}`).then(r => r.data),
  getStagingRows:  (orgId, jobId, { status, page = 1, pageSize = 50 } = {}) =>
    api.get(`/organisations/${orgId}/ingestion/${jobId}/staging`, { params: { status, page, pageSize } }).then(r => r.data),
  reviewRow:       (orgId, jobId, rowId, data)          => api.post(`/organisations/${orgId}/ingestion/${jobId}/staging/${rowId}/review`, data).then(r => r.data),
  commit:          (orgId, jobId, data)                 => api.post(`/organisations/${orgId}/ingestion/${jobId}/commit`, data).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Field Option Aliases
// ---------------------------------------------------------------------------
export const aliasApi = {
  getAll:   (orgId)           => api.get(`/organisations/${orgId}/field-option-aliases`).then(r => r.data),
  bulkSave: (orgId, aliases)  => api.post(`/organisations/${orgId}/field-option-aliases/bulk`, { aliases }).then(r => r.data),
  delete:   (orgId, aliasId)  => api.delete(`/organisations/${orgId}/field-option-aliases/${aliasId}`).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Import Column Staging
// ---------------------------------------------------------------------------
export const stagingApi = {
  getAll:       (orgId, status)          => api.get(`/organisations/${orgId}/import-staging`, { params: status ? { status } : undefined }).then(r => r.data),
  getById:      (orgId, stagingId)       => api.get(`/organisations/${orgId}/import-staging/${stagingId}`).then(r => r.data),
  resolve:      (orgId, stagingId, data) => api.put(`/organisations/${orgId}/import-staging/${stagingId}`, data).then(r => r.data),
  delete:       (orgId, stagingId)       => api.delete(`/organisations/${orgId}/import-staging/${stagingId}`).then(r => r.data),
}
