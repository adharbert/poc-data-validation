import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { orgApi, fieldApi, fieldOptionApi, customerApi, contractApi, contractDocumentApi, projectApi, dashboardApi, importApi, stagingApi, sectionApi, libraryApi, ingestionApi, aliasApi } from '@/api/services.js'

// ---------------------------------------------------------------------------
// Query key registry — always use these, never raw string arrays
// ---------------------------------------------------------------------------
export const QK = {
  organizations:     (inactive = false, search = null) => ['organizations', inactive, search ?? ''],
  organization:      (id)                          => ['organizations', id],
  fields:            (orgId, inactive = false)     => ['fields', orgId, inactive],
  fieldOptions:      (fieldId)                     => ['fieldOptions', fieldId],
  customers:         (orgId, page = 1)             => ['customers', orgId, page],
  customer:          (orgId, customerId)           => ['customer', orgId, customerId],
  customerEmails:    (orgId, customerId)           => ['customerEmails', orgId, customerId],
  customerPhones:    (orgId, customerId)           => ['customerPhones', orgId, customerId],
  customerAddresses: (customerId)                  => ['customerAddresses', customerId],
  customerValues:    (customerId)                  => ['customerValues', customerId],
  contracts:         (orgId, inactive = false)     => ['contracts', orgId, inactive],
  contractDocs:      (orgId, contractId)           => ['contractDocs', orgId, contractId],
  projects:          (orgId, inactive = false)     => ['projects', orgId, inactive],
  dashboardStats:    ()                            => ['dashboard', 'stats'],
  expiringProjects:  ()                            => ['dashboard', 'expiring'],
  importBatches:     (orgId)                       => ['importBatches', orgId],
  importBatch:       (orgId, batchId)              => ['importBatch', orgId, batchId],
  savedMappings:     (orgId, fingerprint)          => ['savedMappings', orgId, fingerprint],
  valueMapping:      (orgId, batchId)              => ['valueMapping', orgId, batchId],
  fieldAliases:      (orgId)                       => ['fieldAliases', orgId],
  staging:           (orgId, status)               => ['staging', orgId, status],
  sections:          (orgId)                       => ['sections', orgId],
  section:           (orgId, sectionId)            => ['sections', orgId, sectionId],
  formPreview:       (orgId, customerId)           => ['formPreview', orgId, customerId],
  librarySections:   (inactive = false)            => ['library', 'sections', inactive],
  libraryFields:     (inactive = false)            => ['library', 'fields', inactive],
  ingestionJobs:     (orgId, page = 1)             => ['ingestion', 'jobs', orgId, page],
  ingestionJob:      (orgId, jobId)                => ['ingestion', 'job', orgId, jobId],
  ingestionStaging:  (orgId, jobId, status, page)  => ['ingestion', 'staging', orgId, jobId, status ?? 'all', page],
}

// ---------------------------------------------------------------------------
// Organizations
// ---------------------------------------------------------------------------
export const useOrganizations = (includeInactive = false, search = null) =>
  useQuery({ queryKey: QK.organizations(includeInactive, search), queryFn: () => orgApi.getAll(includeInactive, search) })

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

export const useReprovisionOrganization = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id) => orgApi.reprovision(id),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['organizations'] }),
  })
}

export const useMigrateIsolatedDatabases = () =>
  useMutation({ mutationFn: () => orgApi.migrateIsolated() })

// ---------------------------------------------------------------------------
// Fields
// ---------------------------------------------------------------------------
export const useFields = (organizationId, includeInactive = false) =>
  useQuery({
    queryKey: QK.fields(organizationId, includeInactive),
    queryFn:  () => fieldApi.getAll(organizationId, includeInactive),
    enabled:  !!organizationId,
  })

export const useFieldOptions = (organizationId, fieldId) =>
  useQuery({
    queryKey: QK.fieldOptions(fieldId),
    queryFn:  () => fieldOptionApi.getAll(organizationId, fieldId),
    enabled:  !!organizationId && !!fieldId,
  })

export const useCreateField = (organizationId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => fieldApi.create(organizationId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.fields(organizationId) }),
  })
}

export const useUpdateField = (organizationId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ fieldId, data }) => fieldApi.update(organizationId, fieldId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.fields(organizationId) }),
  })
}

export const useSetFieldStatus = (organizationId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ fieldId, isActive }) => fieldApi.setStatus(organizationId, fieldId, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.fields(organizationId) }),
  })
}

export const useSaveFieldOptions = (organizationId, fieldId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => fieldOptionApi.save(organizationId, fieldId, data),
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

export const useCustomer = (orgId, customerId) =>
  useQuery({
    queryKey: QK.customer(orgId, customerId),
    queryFn:  () => customerApi.getById(orgId, customerId),
    enabled:  !!orgId && !!customerId,
  })

export const useCustomerEmails = (orgId, customerId) =>
  useQuery({
    queryKey: QK.customerEmails(orgId, customerId),
    queryFn:  () => customerApi.getEmails(orgId, customerId),
    enabled:  !!orgId && !!customerId,
  })

export const useCustomerPhones = (orgId, customerId) =>
  useQuery({
    queryKey: QK.customerPhones(orgId, customerId),
    queryFn:  () => customerApi.getPhones(orgId, customerId),
    enabled:  !!orgId && !!customerId,
  })

export const useCustomerAddresses = (customerId) =>
  useQuery({
    queryKey: QK.customerAddresses(customerId),
    queryFn:  () => customerApi.getAddresses(customerId),
    enabled:  !!customerId,
  })

export const useCustomerFieldValues = (customerId) =>
  useQuery({
    queryKey: QK.customerValues(customerId),
    queryFn:  () => customerApi.getFieldValues(customerId),
    enabled:  !!customerId,
  })

// ---------------------------------------------------------------------------
// Contracts
// ---------------------------------------------------------------------------
export const useContracts = (orgId, includeInactive = false) =>
  useQuery({
    queryKey: QK.contracts(orgId, includeInactive),
    queryFn:  () => contractApi.getAll(orgId, includeInactive),
    enabled:  !!orgId,
  })

export const useCreateContract = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => contractApi.create(orgId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['contracts', orgId] }),
  })
}

export const useUpdateContract = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ contractId, data }) => contractApi.update(orgId, contractId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['contracts', orgId] }),
  })
}

export const useSetContractStatus = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ contractId, isActive }) => contractApi.setStatus(orgId, contractId, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['contracts', orgId] }),
  })
}

// ---------------------------------------------------------------------------
// Contract Documents
// ---------------------------------------------------------------------------
export const useContractDocuments = (orgId, contractId) =>
  useQuery({
    queryKey: QK.contractDocs(orgId, contractId),
    queryFn:  () => contractDocumentApi.getAll(orgId, contractId),
    enabled:  !!orgId && !!contractId,
  })

export const useUploadContractDocument = (orgId, contractId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (formData) => contractDocumentApi.upload(orgId, contractId, formData),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.contractDocs(orgId, contractId) }),
  })
}

export const useDeleteContractDocument = (orgId, contractId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (docId) => contractDocumentApi.delete(orgId, contractId, docId),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.contractDocs(orgId, contractId) }),
  })
}

// ---------------------------------------------------------------------------
// Marketing Projects
// ---------------------------------------------------------------------------
export const useProjects = (orgId, includeInactive = false) =>
  useQuery({
    queryKey: QK.projects(orgId, includeInactive),
    queryFn:  () => projectApi.getAll(orgId, includeInactive),
    enabled:  !!orgId,
  })

export const useCreateProject = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => projectApi.create(orgId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['projects', orgId] }),
  })
}

export const useUpdateProject = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ projectId, data }) => projectApi.update(orgId, projectId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['projects', orgId] }),
  })
}

export const useSetProjectStatus = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ projectId, isActive }) => projectApi.setStatus(orgId, projectId, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['projects', orgId] }),
  })
}

// ---------------------------------------------------------------------------
// Field Sections
// ---------------------------------------------------------------------------
export const useSections = (orgId) =>
  useQuery({ queryKey: QK.sections(orgId), queryFn: () => sectionApi.getAll(orgId), enabled: !!orgId })

export const useCreateSection = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => sectionApi.create(orgId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.sections(orgId) }),
  })
}

export const useUpdateSection = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ sectionId, data }) => sectionApi.update(orgId, sectionId, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.sections(orgId) }),
  })
}

export const useSetSectionStatus = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ sectionId, isActive }) => sectionApi.setStatus(orgId, sectionId, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.sections(orgId) }),
  })
}

export const useReorderSections = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (sections) => sectionApi.reorder(orgId, sections),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.sections(orgId) }),
  })
}

export const useAssignFieldsToSection = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ sectionId, fields }) => sectionApi.assignFields(orgId, sectionId, fields),
    onSuccess:  () => {
      qc.invalidateQueries({ queryKey: QK.sections(orgId) })
      qc.invalidateQueries({ queryKey: ['fields', orgId] })
    },
  })
}

export const useFormPreview = (orgId, customerId) =>
  useQuery({
    queryKey: QK.formPreview(orgId, customerId),
    queryFn:  () => sectionApi.formPreview(orgId, customerId),
    enabled:  !!orgId && !!customerId,
  })

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
    queryKey:       QK.importBatch(orgId, batchId),
    queryFn:        () => importApi.getBatch(orgId, batchId),
    enabled:        !!orgId && !!batchId,
    refetchInterval: (query) => {
      const status = query.state.data?.status
      return status === 'importing' ? 30_000 : false  // 30 s fallback; SignalR is the primary signal
    },
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

export const useResumeImport = (orgId) =>
  useMutation({ mutationFn: (batchId) => importApi.resume(orgId, batchId) })

export const useResetImport = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ batchId, targetStatus = 'pending' }) => importApi.reset(orgId, batchId, targetStatus),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.importBatches(orgId) }),
  })
}

export const useCancelImport = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (batchId) => importApi.cancel(orgId, batchId),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.importBatches(orgId) }),
  })
}

export const useDeleteImport = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (batchId) => importApi.deleteBatch(orgId, batchId),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.importBatches(orgId) }),
  })
}

export const useValueMapping = (orgId, batchId) =>
  useQuery({
    queryKey: QK.valueMapping(orgId, batchId),
    queryFn:  () => importApi.getValueMapping(orgId, batchId),
    enabled:  !!orgId && !!batchId,
  })

// ---------------------------------------------------------------------------
// Field Option Aliases
// ---------------------------------------------------------------------------
export const useFieldAliases = (orgId) =>
  useQuery({
    queryKey: QK.fieldAliases(orgId),
    queryFn:  () => aliasApi.getAll(orgId),
    enabled:  !!orgId,
  })

export const useSaveAliases = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (aliases) => aliasApi.bulkSave(orgId, aliases),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.fieldAliases(orgId) }),
  })
}

export const useDeleteAlias = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (aliasId) => aliasApi.delete(orgId, aliasId),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QK.fieldAliases(orgId) }),
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

// ---------------------------------------------------------------------------
// Field Library
// ---------------------------------------------------------------------------
export const useLibrarySections = (includeInactive = false) =>
  useQuery({ queryKey: QK.librarySections(includeInactive), queryFn: () => libraryApi.getSections(includeInactive) })

export const useCreateLibrarySection = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => libraryApi.createSection(data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['library', 'sections'] }),
  })
}

export const useUpdateLibrarySection = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }) => libraryApi.updateSection(id, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['library', 'sections'] }),
  })
}

export const useSetLibrarySectionStatus = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, isActive }) => libraryApi.setSectionStatus(id, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['library', 'sections'] }),
  })
}

export const useAssignLibraryFields = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ sectionId, fields }) => libraryApi.assignFields(sectionId, fields),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['library'] }),
  })
}

export const useLibraryFields = (includeInactive = false) =>
  useQuery({ queryKey: QK.libraryFields(includeInactive), queryFn: () => libraryApi.getFields(includeInactive) })

export const useCreateLibraryField = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data) => libraryApi.createField(data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['library', 'fields'] }),
  })
}

export const useUpdateLibraryField = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }) => libraryApi.updateField(id, data),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['library', 'fields'] }),
  })
}

export const useSetLibraryFieldStatus = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, isActive }) => libraryApi.setFieldStatus(id, isActive),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['library', 'fields'] }),
  })
}

export const useBulkUpsertLibraryOptions = () => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ fieldId, options }) => libraryApi.bulkUpsertOptions(fieldId, options),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['library'] }),
  })
}

export const useImportFromLibrary = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (sectionIds) => libraryApi.importToOrg(orgId, sectionIds),
    onSuccess:  () => {
      qc.invalidateQueries({ queryKey: ['sections', orgId] })
      qc.invalidateQueries({ queryKey: ['fields', orgId] })
    },
  })
}

// ---------------------------------------------------------------------------
// Ingestion Pipeline
// ---------------------------------------------------------------------------

export const useIngestionJobs = (orgId, page = 1) =>
  useQuery({
    queryKey: QK.ingestionJobs(orgId, page),
    queryFn:  () => ingestionApi.getJobs(orgId, page),
    enabled:  !!orgId,
  })

export const useIngestionJob = (orgId, jobId, { refetchInterval } = {}) =>
  useQuery({
    queryKey:       QK.ingestionJob(orgId, jobId),
    queryFn:        () => ingestionApi.getJob(orgId, jobId),
    enabled:        !!orgId && !!jobId,
    refetchInterval,
  })

export const useIngestionStagingRows = (orgId, jobId, { status, page = 1, pageSize = 50 } = {}) =>
  useQuery({
    queryKey: QK.ingestionStaging(orgId, jobId, status, page),
    queryFn:  () => ingestionApi.getStagingRows(orgId, jobId, { status, page, pageSize }),
    enabled:  !!orgId && !!jobId,
  })

export const useUploadIngestionFile = (orgId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (formData) => ingestionApi.upload(orgId, formData),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['ingestion', 'jobs', orgId] }),
  })
}

export const useReviewStagingRow = (orgId, jobId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ rowId, action, reviewedBy, reason }) =>
      ingestionApi.reviewRow(orgId, jobId, rowId, { action, reviewedBy, reason }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['ingestion', 'staging', orgId, jobId] })
      qc.invalidateQueries({ queryKey: QK.ingestionJob(orgId, jobId) })
    },
  })
}

export const useCommitIngestionJob = (orgId, jobId) => {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (committedBy = 'Admin') => ingestionApi.commit(orgId, jobId, { committedBy }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['ingestion', 'jobs', orgId] })
      qc.invalidateQueries({ queryKey: QK.ingestionJob(orgId, jobId) })
      qc.invalidateQueries({ queryKey: ['ingestion', 'staging', orgId, jobId] })
    },
  })
}
