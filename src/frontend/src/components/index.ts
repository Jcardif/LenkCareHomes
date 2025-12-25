// Layout Components
export { default as AuthenticatedLayout } from './layout/AuthenticatedLayout';
export { default as ResponsivePageHeader } from './layout/ResponsivePageHeader';
export type { ResponsivePageHeaderProps, PageAction } from './layout/ResponsivePageHeader';

// Auth Components
export { default as ProtectedRoute } from './auth/ProtectedRoute';
export { BackupCodesStep } from './auth/BackupCodesStep';

// Address Components
export { AddressAutocomplete } from './address/AddressAutocomplete';
export type { AddressAutocompleteProps } from './address/AddressAutocomplete';

// Care Log Components
export {
  ADLLogTab,
  VitalsLogTab,
  MedicationLogTab,
  ROMLogTab,
  BehaviorNotesTab,
  ActivitiesTab,
  TimelineTab,
  QuickLogModal,
} from './care-log';

// Appointment Components
export { default as AppointmentList } from './appointments/AppointmentList';
export {
  APPOINTMENT_TYPES,
  APPOINTMENT_STATUSES,
  getAppointmentTypeLabel,
  getTypeTag,
  getStatusTag,
} from './appointments/AppointmentList';
export { default as AppointmentForm } from './appointments/AppointmentForm';
export { AppointmentsTab } from './appointments/AppointmentsTab';

// Incident Components
export { IncidentForm, IncidentList, IncidentDetail } from './incidents';

// Document Components
export { DocumentUpload, DocumentList } from './documents';

// Audit Components
export { ActivityFeedView } from './audit';
