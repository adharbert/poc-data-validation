import { Routes, Route, Navigate } from 'react-router-dom'
import AppLayout from '@/components/layout/AppLayout.jsx'
import DashboardPage       from '@/pages/DashboardPage.jsx'
import OrganizationsPage   from '@/pages/OrganizationsPage.jsx'
import OrgDetailPage       from '@/pages/OrgDetailPage.jsx'
import InputsPage          from '@/pages/InputsPage.jsx'
import CustomersPage       from '@/pages/CustomersPage.jsx'
import ImportPage          from '@/pages/ImportPage.jsx'
import ImportStagingPage   from '@/pages/ImportStagingPage.jsx'
import LibraryPage         from '@/pages/LibraryPage.jsx'

export default function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard"                                          element={<DashboardPage />} />
        <Route path="organizations"                                      element={<OrganizationsPage />} />
        <Route path="organizations/:organizationId"                      element={<OrgDetailPage />} />
        <Route path="organizations/:organizationId/inputs"               element={<InputsPage />} />
        <Route path="organizations/:organizationId/customers"            element={<CustomersPage />} />
        <Route path="organizations/:organizationId/import"               element={<ImportPage />} />
        <Route path="organizations/:organizationId/import-staging"       element={<ImportStagingPage />} />
        <Route path="library"                                            element={<LibraryPage />} />
      </Route>
    </Routes>
  )
}
