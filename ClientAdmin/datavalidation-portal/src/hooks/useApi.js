import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { orgApi, fieldApi, fieldOptionApi, customerApi, contractApi, projectApi, dashboardApi, importApi, stagingApi } from '@/api/services.js'

// ---------------------------------------------------------------------------
// Query key registry — always use these, never raw string arrays
// ---------------------------------------------------------------------------
export const QK = {
  organizations:     (inactive = false)            => ['organizations', inactive],
  organization:      (id)                          => ['organizations', id],
  fields:            (orgId, inactive = false)     => ['fields', orgId, inactive],
  fieldOptions:      (fieldId)                     => ['fieldOptions', fieldId],
  customers:         (orgId, page = 1)             => ['customers', orgId, page],
  contracts:         (orgId)                       => ['contracts', orgId],
  projects:          (orgId)                       => ['projects', orgId],
  dashboardStats:    ()                            => ['dashboard', 'stats'],
  expiringProjects:  ()                            => ['dashboard', 'expiring'],
  importBatches:     (orgId)                       => ['importBatches', orgId],
  importBatch:       (orgId, batchId)              => ['importBatch', orgId, batchId],
  savedMappings:     (orgId, fingerprint)          => ['savedMappings', orgId, fingerprint],
  staging:           (orgId, status)               => ['staging', orgId, status],
}

// ---------------------------------------------------------------------------
// Organizations
// ---------------------------------------------------------------------------
export const useOrganizations = (includeInactive = false) =>
  useQuery({ queryKey: QK.organizations(includeInactive), queryFn: () => orgApi.getAll(includeInactive) })

export const useOrganization = (id) =>
  useQuery({ queryKey: QK.organization(id), queryFn: () => orgApi.getById(id), enabled: !!id })

export const useCreateOrganization = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => orgApi.create(data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['organizations'] }),
  })
}

export const useUpdateOrganization = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }) => orgApi.update(id, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['organizations'] }),
  })
}

export const useSetOrganizationStatus = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }) => orgApi.setStatus(id, status),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['organizations'] }),
  })
}

// ---------------------------------------------------------------------------
// Fields
// ---------------------------------------------------------------------------
export const useFields = (organizationId, includeInactive = false) =>
  useQuery({
    queryKey: QK.fields(organizationId, includeInactive),
    queryFn:  () => fieldApi.getAll(organizationId, includeInactive),
    enabled:  !!organizationId,
  })

export const useFieldOptions = (fieldId) =>
  useQuery({
    queryKey: QK.fieldOptions(fieldId),
    queryFn:  () => fieldOptionApi.getAll(fieldId),
    enabled:  !!fieldId,
  })

export const useCreateField = (organizationId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => fieldApi.create(data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.fields(organizationId) }),
  })
}

export const useUpdateField = (organizationId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ fieldId, data }) => fieldApi.update(fieldId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.fields(organizationId) }),
  })
}

export const useSetFieldStatus = (organizationId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ fieldId, isActive }) => fieldApi.setStatus(fieldId, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.fields(organizationId) }),
  })
}

export const useSaveFieldOptions = (fieldId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => fieldOptionApi.save(fieldId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.fieldOptions(fieldId) }),
  })
}

// ---------------------------------------------------------------------------
// Customers
// ---------------------------------------------------------------------------
export const useCustomers = (orgId, page = 1, pageSize = 50, includeInactive = false) =>
  useQuery({
    queryKey: QK.customers(orgId, page),
    queryFn:  () => customerApi.getAll(orgId, { page, pageSize, includeInactive }),
    enabled:  !!orgId,
  })

export const useCreateCustomer = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => customerApi.create(orgId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['customers', orgId] }),
  })
}

export const useUpdateCustomer = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ customerId, data }) => customerApi.update(orgId, customerId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['customers', orgId] }),
  })
}

export const useSetCustomerStatus = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ customerId, isActive }) => customerApi.setStatus(orgId, customerId, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['customers', orgId] }),
  })
}

// ---------------------------------------------------------------------------
// Contracts
// ---------------------------------------------------------------------------
export const useContracts = (orgId, includeInactive = false) =>
  useQuery({
    queryKey: QK.contracts(orgId),
    queryFn:  () => contractApi.getAll(orgId, includeInactive),
    enabled:  !!orgId,
  })

export const useCreateContract = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => contractApi.create(orgId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.contracts(orgId) }),
  })
}

export const useSetContractStatus = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ contractId, isActive }) => contractApi.setStatus(orgId, contractId, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.contracts(orgId) }),
  })
}

// ---------------------------------------------------------------------------
// Marketing Projects
// ---------------------------------------------------------------------------
export const useProjects = (orgId, includeInactive = false) =>
  useQuery({
    queryKey: QK.projects(orgId),
    queryFn:  () => projectApi.getAll(orgId, includeInactive),
    enabled:  !!orgId,
  })

export const useCreateProject = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => projectApi.create(orgId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.projects(orgId) }),
  })
}

export const useUpdateProject = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ projectId, data }) => projectApi.update(orgId, projectId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.projects(orgId) }),
  })
}

export const useSetProjectStatus = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ projectId, isActive }) => projectApi.setStatus(orgId, projectId, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.projects(orgId) }),
  })
}

// ---------------------------------------------------------------------------
// Dashboard
// ---------------------------------------------------------------------------
export const useDashboardStats = () =>
  useQuery({ queryKey: QK.dashboardStats(), queryFn: dashboardApi.getStats })

export const useExpiringProjects = () =>
  useQuery({ queryKey: QK.expiringProjects(), queryFn: dashboardApi.getExpiringProjects })

// ---------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------
export const useImportBatches = (orgId, page = 1) =>
  useQuery({
    queryKey: QK.importBatches(orgId),
    queryFn:  () => importApi.getBatches(orgId, page),
    enabled:  !!orgId,
  })

export const useImportBatch = (orgId, batchId) =>
  useQuery({
    queryKey: QK.importBatch(orgId, batchId),
    queryFn:  () => importApi.getBatch(orgId, batchId),
    enabled:  !!orgId && !!batchId,
  })

export const useUploadImport = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (formData) => importApi.upload(orgId, formData),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.importBatches(orgId) }),
  })
}

export const useSaveMappings = (orgId) =>
  useMutation({ mutationFn: ({ batchId, data }) => importApi.saveMappings(orgId, batchId, data) })

export const usePreviewImport = (orgId) =>
  useMutation({ mutationFn: (batchId) => importApi.preview(orgId, batchId) })

export const useExecuteImport = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (batchId) => importApi.execute(orgId, batchId),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.importBatches(orgId) }),
  })
}

// ---------------------------------------------------------------------------
// Import Column Staging
// ---------------------------------------------------------------------------
export const useStagingColumns = (orgId, status) =>
  useQuery({
    queryKey: QK.staging(orgId, status),
    queryFn:  () => stagingApi.getAll(orgId, status),
    enabled:  !!orgId,
  })

export const useResolveStaging = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ stagingId, data }) => stagingApi.resolve(orgId, stagingId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['staging', orgId] }),
  })
}

export const useDeleteStaging = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (stagingId) => stagingApi.delete(orgId, stagingId),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['staging', orgId] }),
  })
}
