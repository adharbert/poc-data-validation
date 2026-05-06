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
// Fields  (existing controller — organizationId as query param)
// ---------------------------------------------------------------------------
export const fieldApi = {
  getAll:       (organizationId, includeInactive = false) => api.get('/fields', { params: { organizationId, includeInactive } }).then(r => r.data),
  getById:      (fieldId)                                  => api.get(`/fields/${fieldId}`).then(r => r.data),
  create:       (data)                                     => api.post('/fields', data).then(r => r.data),
  update:       (fieldId, data)                            => api.put(`/fields/${fieldId}`, data).then(r => r.data),
  setStatus:    (fieldId, isActive)                        => api.patch(`/fields/${fieldId}/status`, { isActive }).then(r => r.data),
  reorder:      (data)                                     => api.post('/fields/reorder', data).then(r => r.data),
}

// ---------------------------------------------------------------------------
// Field Options
// ---------------------------------------------------------------------------
export const fieldOptionApi = {
  getAll:       (fieldId)       => api.get(`/fields/${fieldId}/options`).then(r => r.data),
  save:         (fieldId, data) => api.put(`/fields/${fieldId}/options/bulk`, { options: data }).then(r => r.data),
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
  preview:         (orgId, batchId)              => api.post(`/organisations/${orgId}/imports/${batchId}/preview`).then(r => r.data),
  execute:         (orgId, batchId)              => api.post(`/organisations/${orgId}/imports/${batchId}/execute`).then(r => r.data),
  getErrors:       (orgId, batchId)              => api.get(`/organisations/${orgId}/imports/${batchId}/errors`).then(r => r.data),
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

  importToOrg:        (organizationId, sectionIds) => api.post('/library/import-to-org', { organizationId, sectionIds }).then(r => r.data),
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
