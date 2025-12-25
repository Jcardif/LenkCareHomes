// ========== Home Types ==========

/** Home DTO */
export interface Home {
  id: string;
  name: string;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  phoneNumber?: string;
  capacity: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  totalBeds: number;
  availableBeds: number;
  occupiedBeds: number;
  activeClients: number;
}

/** Home summary DTO */
export interface HomeSummary {
  id: string;
  name: string;
  city: string;
  state: string;
  isActive: boolean;
  capacity: number;
  availableBeds: number;
  activeClients: number;
}

/** Create home request */
export interface CreateHomeRequest {
  name: string;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  phoneNumber?: string;
  capacity: number;
}

/** Update home request */
export interface UpdateHomeRequest {
  name?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  phoneNumber?: string;
  capacity?: number;
}

/** Home operation response */
export interface HomeOperationResponse {
  success: boolean;
  error?: string;
  home?: Home;
}

// ========== Bed Types ==========

/** Bed status */
export type BedStatus = 'Available' | 'Occupied';

/** Bed DTO */
export interface Bed {
  id: string;
  homeId: string;
  label: string;
  status: BedStatus;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  currentOccupantId?: string;
  currentOccupantName?: string;
}

/** Create bed request */
export interface CreateBedRequest {
  label: string;
}

/** Update bed request */
export interface UpdateBedRequest {
  label?: string;
  isActive?: boolean;
}

/** Bed operation response */
export interface BedOperationResponse {
  success: boolean;
  error?: string;
  bed?: Bed;
}

// ========== Client Types ==========

/** Client DTO */
export interface Client {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  dateOfBirth: string;
  gender: string;
  admissionDate: string;
  dischargeDate?: string;
  dischargeReason?: string;
  homeId: string;
  homeName: string;
  bedId?: string;
  bedLabel?: string;
  primaryPhysician?: string;
  primaryPhysicianPhone?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelationship?: string;
  allergies?: string;
  diagnoses?: string;
  medicationList?: string;
  photoUrl?: string;
  notes?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

/** Client summary DTO */
export interface ClientSummary {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  dateOfBirth: string;
  homeId: string;
  homeName: string;
  bedLabel?: string;
  allergies?: string;
  photoUrl?: string;
  isActive: boolean;
  admissionDate: string;
}

/** Admit client request */
export interface AdmitClientRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  ssn?: string;
  admissionDate: string;
  homeId: string;
  bedId: string;
  primaryPhysician?: string;
  primaryPhysicianPhone?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelationship?: string;
  allergies?: string;
  diagnoses?: string;
  medicationList?: string;
  notes?: string;
}

/** Update client request */
export interface UpdateClientRequest {
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string;
  gender?: string;
  primaryPhysician?: string;
  primaryPhysicianPhone?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelationship?: string;
  allergies?: string;
  diagnoses?: string;
  medicationList?: string;
  notes?: string;
  photoUrl?: string;
}

/** Discharge client request */
export interface DischargeClientRequest {
  dischargeDate: string;
  dischargeReason: string;
}

/** Transfer client request */
export interface TransferClientRequest {
  newBedId: string;
}

/** Client operation response */
export interface ClientOperationResponse {
  success: boolean;
  error?: string;
  client?: Client;
}

// ========== Caregiver Types ==========

/** Caregiver DTO */
export interface Caregiver {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  isMfaSetupComplete: boolean;
  invitationAccepted: boolean;
  createdAt: string;
  updatedAt?: string;
  homeAssignments: CaregiverHomeAssignment[];
}

/** Caregiver summary DTO */
export interface CaregiverSummary {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  invitationAccepted: boolean;
  assignedHomesCount: number;
}

/** Caregiver home assignment DTO */
export interface CaregiverHomeAssignment {
  id: string;
  homeId: string;
  homeName: string;
  assignedAt: string;
  isActive: boolean;
}

/** Assign homes request */
export interface AssignHomesRequest {
  homeIds: string[];
}

/** Caregiver operation response */
export interface CaregiverOperationResponse {
  success: boolean;
  error?: string;
  caregiver?: Caregiver;
}

// ========== Dashboard Types ==========

/** Admin dashboard stats */
export interface AdminDashboardStats {
  totalHomes: number;
  activeHomes: number;
  totalBeds: number;
  availableBeds: number;
  occupiedBeds: number;
  totalClients: number;
  activeClients: number;
  totalCaregivers: number;
  activeCaregivers: number;
  recentIncidentsCount: number;
  upcomingBirthdays: UpcomingBirthday[];
  upcomingAppointments: UpcomingAppointment[];
}

/** Upcoming birthday DTO */
export interface UpcomingBirthday {
  clientId: string;
  clientName: string;
  dateOfBirth: string;
  age: number;
  daysUntilBirthday: number;
  homeName: string;
}

/** Caregiver dashboard stats */
export interface CaregiverDashboardStats {
  assignedHomesCount: number;
  activeClientsCount: number;
  assignedHomes: CaregiverHomeInfo[];
  clients: CaregiverClientInfo[];
  upcomingAppointments: UpcomingAppointment[];
}

/** Caregiver home info */
export interface CaregiverHomeInfo {
  id: string;
  name: string;
  address: string;
  city: string;
  state: string;
  activeClientsCount: number;
}

/** Caregiver client info */
export interface CaregiverClientInfo {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  homeId: string;
  homeName: string;
  bedLabel?: string;
  allergies?: string;
  photoUrl?: string;
  dateOfBirth: string;
}

// ========== ADL Log Types ==========

/** ADL assessment levels based on Katz Index */
export type ADLLevel = 'Independent' | 'PartialAssist' | 'Dependent' | 'NotApplicable';

/** ADL Log DTO */
export interface ADLLog {
  id: string;
  clientId: string;
  clientName: string;
  caregiverId: string;
  caregiverName: string;
  timestamp: string;
  bathing?: ADLLevel;
  dressing?: ADLLevel;
  toileting?: ADLLevel;
  transferring?: ADLLevel;
  continence?: ADLLevel;
  feeding?: ADLLevel;
  notes?: string;
  katzScore: number;
  createdAt: string;
}

/** Create ADL Log request */
export interface CreateADLLogRequest {
  timestamp: string;
  bathing?: ADLLevel;
  dressing?: ADLLevel;
  toileting?: ADLLevel;
  transferring?: ADLLevel;
  continence?: ADLLevel;
  feeding?: ADLLevel;
  notes?: string;
}

/** ADL Log operation response */
export interface ADLLogOperationResponse {
  success: boolean;
  error?: string;
  adlLog?: ADLLog;
}

// ========== Vitals Log Types ==========

/** Temperature unit */
export type TemperatureUnit = 'Fahrenheit' | 'Celsius';

/** Vitals Log DTO */
export interface VitalsLog {
  id: string;
  clientId: string;
  clientName: string;
  caregiverId: string;
  caregiverName: string;
  timestamp: string;
  systolicBP?: number;
  diastolicBP?: number;
  bloodPressure?: string;
  pulse?: number;
  temperature?: number;
  temperatureUnit: TemperatureUnit;
  oxygenSaturation?: number;
  notes?: string;
  createdAt: string;
}

/** Create Vitals Log request */
export interface CreateVitalsLogRequest {
  timestamp: string;
  systolicBP?: number;
  diastolicBP?: number;
  pulse?: number;
  temperature?: number;
  temperatureUnit?: TemperatureUnit;
  oxygenSaturation?: number;
  notes?: string;
}

/** Vitals Log operation response */
export interface VitalsLogOperationResponse {
  success: boolean;
  error?: string;
  vitalsLog?: VitalsLog;
}

// ========== Medication Log Types ==========

/** Medication route */
export type MedicationRoute =
  | 'Oral'
  | 'Sublingual'
  | 'Topical'
  | 'Inhalation'
  | 'Injection'
  | 'Transdermal'
  | 'Rectal'
  | 'Ophthalmic'
  | 'Otic'
  | 'Nasal'
  | 'Other';

/** Medication status */
export type MedicationStatus =
  | 'Administered'
  | 'Refused'
  | 'NotAvailable'
  | 'Held'
  | 'GivenEarly'
  | 'GivenLate';

/** Medication Log DTO */
export interface MedicationLog {
  id: string;
  clientId: string;
  caregiverId: string;
  caregiverName: string;
  timestamp: string;
  medicationName: string;
  dosage: string;
  route: MedicationRoute;
  status: MedicationStatus;
  scheduledTime?: string;
  prescribedBy?: string;
  pharmacy?: string;
  rxNumber?: string;
  notes?: string;
  createdAt: string;
}

/** Create Medication Log request */
export interface CreateMedicationLogRequest {
  timestamp?: string;
  medicationName: string;
  dosage: string;
  route?: MedicationRoute;
  status?: MedicationStatus;
  scheduledTime?: string;
  prescribedBy?: string;
  pharmacy?: string;
  rxNumber?: string;
  notes?: string;
}

/** Medication Log operation response */
export interface MedicationLogOperationResponse {
  success: boolean;
  error?: string;
  medicationLog?: MedicationLog;
}

// ========== ROM Log Types ==========

/** ROM (Range of Motion) Log DTO */
export interface ROMLog {
  id: string;
  clientId: string;
  clientName: string;
  caregiverId: string;
  caregiverName: string;
  timestamp: string;
  activityDescription: string;
  duration?: number;
  repetitions?: number;
  notes?: string;
  createdAt: string;
}

/** Create ROM Log request */
export interface CreateROMLogRequest {
  timestamp: string;
  activityDescription: string;
  duration?: number;
  repetitions?: number;
  notes?: string;
}

/** ROM Log operation response */
export interface ROMLogOperationResponse {
  success: boolean;
  error?: string;
  romLog?: ROMLog;
}

// ========== Behavior Note Types ==========

/** Behavior note category */
export type BehaviorCategory = 'Behavior' | 'Mood' | 'General';

/** Note severity level */
export type NoteSeverity = 'Low' | 'Medium' | 'High';

/** Behavior Note DTO */
export interface BehaviorNote {
  id: string;
  clientId: string;
  clientName: string;
  caregiverId: string;
  caregiverName: string;
  timestamp: string;
  category: BehaviorCategory;
  noteText: string;
  severity: NoteSeverity;
  createdAt: string;
}

/** Create Behavior Note request */
export interface CreateBehaviorNoteRequest {
  timestamp: string;
  category: BehaviorCategory;
  noteText: string;
  severity?: NoteSeverity;
}

/** Behavior Note operation response */
export interface BehaviorNoteOperationResponse {
  success: boolean;
  error?: string;
  behaviorNote?: BehaviorNote;
}

// ========== Activity Types ==========

/** Activity category */
export type ActivityCategory = 'Recreational' | 'Social' | 'Exercise' | 'Other';

/** Activity participant DTO */
export interface ActivityParticipant {
  clientId: string;
  clientName: string;
}

/** Activity DTO */
export interface Activity {
  id: string;
  homeId: string;
  homeName: string;
  activityName: string;
  description?: string;
  date: string;
  startTime?: string;
  endTime?: string;
  duration?: number;
  category: ActivityCategory;
  isGroupActivity: boolean;
  participants: ActivityParticipant[];
  createdById: string;
  createdByName: string;
  createdAt: string;
  updatedAt?: string;
}

/** Create Activity request */
export interface CreateActivityRequest {
  homeId?: string;
  activityName: string;
  description?: string;
  date: string;
  startTime?: string;
  endTime?: string;
  category: ActivityCategory;
  isGroupActivity?: boolean;
  clientIds: string[];
}

/** Update Activity request */
export interface UpdateActivityRequest {
  activityName?: string;
  description?: string;
  date?: string;
  startTime?: string;
  endTime?: string;
  category?: ActivityCategory;
  isGroupActivity?: boolean;
  clientIds?: string[];
}

/** Activity operation response */
export interface ActivityOperationResponse {
  success: boolean;
  error?: string;
  activity?: Activity;
}

// ========== Timeline Types ==========

/** Timeline entry types */
export const TimelineEntryTypes = {
  ADL: 'ADL',
  Vitals: 'Vitals',
  ROM: 'ROM',
  Behavior: 'Behavior',
  Activity: 'Activity',
} as const;

export type TimelineEntryType = typeof TimelineEntryTypes[keyof typeof TimelineEntryTypes];

/** Timeline entry DTO */
export interface TimelineEntry {
  id: string;
  entryType: TimelineEntryType;
  timestamp: string;
  summary: string;
  caregiverId: string;
  caregiverName: string;
  details: Record<string, unknown>;
}

/** Timeline query parameters */
export interface TimelineQueryParams {
  startDate?: string;
  endDate?: string;
  entryTypes?: TimelineEntryType[];
  pageNumber?: number;
  pageSize?: number;
}

/** Timeline response with pagination */
export interface TimelineResponse {
  entries: TimelineEntry[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ========== Incident Types ==========

/** Incident type */
export type IncidentType =
  | 'Fall'
  | 'Medication'
  | 'Behavioral'
  | 'Medical'
  | 'Injury'
  | 'Elopement'
  | 'Other';

/** Incident status */
export type IncidentStatus = 'Draft' | 'Submitted' | 'UnderReview' | 'Closed';

/** Incident severity levels */
export type IncidentSeverity = 1 | 2 | 3 | 4 | 5;

/** Incident follow-up DTO */
export interface IncidentFollowUp {
  id: string;
  incidentId: string;
  createdById: string;
  createdByName: string;
  note: string;
  createdAt: string;
}

/** Incident summary DTO for list views */
export interface IncidentSummary {
  id: string;
  incidentNumber: string;
  clientId?: string;
  clientName: string;
  homeId: string;
  homeName: string;
  incidentType: IncidentType;
  severity: IncidentSeverity;
  status: IncidentStatus;
  occurredAt: string;
  reportedById: string;
  reportedByName: string;
  createdAt: string;
}

/** Incident DTO */
export interface Incident {
  id: string;
  incidentNumber: string;
  clientId?: string;
  clientName: string;
  homeId: string;
  homeName: string;
  incidentType: IncidentType;
  severity: IncidentSeverity;
  status: IncidentStatus;
  occurredAt: string;
  location: string;
  description: string;
  actionsTaken?: string;
  witnessNames?: string;
  notifiedParties?: string;
  adminNotifiedAt?: string;
  reportedById: string;
  reportedByName: string;
  closedById?: string;
  closedByName?: string;
  closedAt?: string;
  closureNotes?: string;
  createdAt: string;
  updatedAt?: string;
  followUps: IncidentFollowUp[];
  photos: IncidentPhoto[];
}

/** Create incident request */
export interface CreateIncidentRequest {
  clientId?: string;
  homeId: string;
  incidentType: IncidentType;
  severity: IncidentSeverity;
  occurredAt: string;
  location: string;
  description: string;
  actionsTaken?: string;
  witnessNames?: string;
  submitImmediately?: boolean;
}

/** Update incident request */
export interface UpdateIncidentRequest {
  incidentType?: IncidentType;
  severity?: IncidentSeverity;
  occurredAt?: string;
  location?: string;
  description?: string;
  actionsTaken?: string;
  witnessNames?: string;
  notifiedParties?: string;
}

/** Add follow-up request */
export interface AddFollowUpRequest {
  note: string;
}

/** Close incident request */
export interface CloseIncidentRequest {
  closureNotes?: string;
}

/** Incident operation response */
export interface IncidentOperationResponse {
  success: boolean;
  error?: string;
  incident?: Incident;
}

/** Incident query parameters */
export interface IncidentQueryParams {
  homeId?: string;
  clientId?: string;
  status?: IncidentStatus;
  incidentType?: IncidentType;
  startDate?: string;
  endDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

/** Paged incident response */
export interface PagedIncidentResponse {
  items: IncidentSummary[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ========== Incident Photo Types ==========

/** Incident photo DTO */
export interface IncidentPhoto {
  id: string;
  incidentId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  displayOrder: number;
  caption?: string;
  createdAt: string;
  createdByName: string;
}

/** Upload incident photo request */
export interface UploadIncidentPhotoRequest {
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  caption?: string;
}

/** Incident photo upload response */
export interface IncidentPhotoUploadResponse {
  success: boolean;
  error?: string;
  photoId?: string;
  uploadUrl?: string;
  expiresAt?: string;
}

/** Incident photo operation response */
export interface IncidentPhotoOperationResponse {
  success: boolean;
  error?: string;
  photo?: IncidentPhoto;
}

/** Incident photo view response */
export interface IncidentPhotoViewResponse {
  success: boolean;
  error?: string;
  url?: string;
  expiresAt?: string;
}

// ========== Document Types ==========

/** Document scope - determines where the document belongs */
export type DocumentScope = 'Client' | 'Home' | 'Business' | 'General';

/** Document type */
export type DocumentType =
  | 'CarePlan'
  | 'Medical'
  | 'Legal'
  | 'Financial'
  | 'Photo'
  | 'Assessment'
  | 'Other';

/** Document folder summary DTO */
export interface FolderSummary {
  id: string;
  name: string;
  scope: DocumentScope;
  parentFolderId?: string;
  clientId?: string;
  clientName?: string;
  homeId?: string;
  homeName?: string;
  isSystemFolder: boolean;
  documentCount: number;
  subfolderCount: number;
  createdAt: string;
}

/** Document folder DTO */
export interface Folder {
  id: string;
  name: string;
  scope: DocumentScope;
  parentFolderId?: string;
  clientId?: string;
  clientName?: string;
  homeId?: string;
  homeName?: string;
  isSystemFolder: boolean;
  createdById: string;
  createdByName: string;
  createdAt: string;
  updatedAt?: string;
  documentCount: number;
  subfolders: FolderSummary[];
}

/** Folder tree node DTO for hierarchical navigation */
export interface FolderTreeNode {
  id: string;
  name: string;
  scope: DocumentScope;
  parentFolderId?: string;
  isSystemFolder: boolean;
  children: FolderTreeNode[];
}

/** Breadcrumb item for folder navigation */
export interface BreadcrumbItem {
  id: string;
  name: string;
}

/** Create folder request */
export interface CreateFolderRequest {
  name: string;
  scope: DocumentScope;
  parentFolderId?: string;
  clientId?: string;
  homeId?: string;
}

/** Update folder request */
export interface UpdateFolderRequest {
  name?: string;
}

/** Move folder request */
export interface MoveFolderRequest {
  newParentFolderId?: string;
}

/** Folder operation response */
export interface FolderOperationResponse {
  success: boolean;
  error?: string;
  folder?: Folder;
}

/** Browse documents query parameters */
export interface BrowseDocumentsQuery {
  scope?: DocumentScope;
  folderId?: string;
  clientId?: string;
  homeId?: string;
  pageNumber?: number;
  pageSize?: number;
}

/** Browse documents response */
export interface BrowseDocumentsResponse {
  scope: DocumentScope;
  currentFolder?: FolderSummary;
  breadcrumbs: BreadcrumbItem[];
  folders: FolderSummary[];
  documents: DocumentSummary[];
  totalDocuments: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

/** Document access DTO */
export interface DocumentAccess {
  id: string;
  caregiverId: string;
  caregiverName: string;
  grantedById: string;
  grantedByName: string;
  grantedAt: string;
}

/** Document access history entry (for tracking grants and revocations) */
export interface DocumentAccessHistory {
  id: string;
  documentId: string;
  caregiverId: string;
  caregiverName: string;
  caregiverEmail: string;
  action: 'Granted' | 'Revoked';
  performedById: string;
  performedByName: string;
  performedAt: string;
}

/** Document summary DTO for list views */
export interface DocumentSummary {
  id: string;
  scope: DocumentScope;
  clientId?: string;
  clientName?: string;
  homeId?: string;
  homeName?: string;
  folderId?: string;
  folderName?: string;
  fileName: string;
  contentType: string;
  documentType: DocumentType;
  description?: string;
  uploadedById: string;
  uploadedByName: string;
  uploadedAt: string;
  fileSizeBytes: number;
  hasAccess: boolean;
  isActive: boolean;
}

/** Document DTO */
export interface Document {
  id: string;
  scope: DocumentScope;
  clientId?: string;
  clientName?: string;
  homeId?: string;
  homeName?: string;
  folderId?: string;
  folderName?: string;
  fileName: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  documentType: DocumentType;
  description?: string;
  blobPath: string;
  uploadedById: string;
  uploadedByName: string;
  uploadedAt: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  accessPermissions: DocumentAccess[];
}

/** Upload document request (metadata for initiating upload) */
export interface UploadDocumentRequest {
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  documentType: DocumentType;
  description?: string;
  scope?: DocumentScope;
  folderId?: string;
  clientId?: string;
  homeId?: string;
}

/** Document upload response with SAS URL */
export interface DocumentUploadResponse {
  success: boolean;
  error?: string;
  documentId?: string;
  uploadUrl?: string;
  expiresAt?: string;
}

/** Document view SAS URL response */
export interface DocumentViewResponse {
  success: boolean;
  error?: string;
  viewUrl?: string;
  fileName?: string;
  contentType?: string;
  expiresAt?: string;
}

/** Update document request */
export interface UpdateDocumentRequest {
  documentType?: DocumentType;
  description?: string;
}

/** Grant document access request */
export interface GrantAccessRequest {
  caregiverIds: string[];
}

/** Document operation response */
export interface DocumentOperationResponse {
  success: boolean;
  error?: string;
  document?: Document;
}

/** Document query parameters */
export interface DocumentQueryParams {
  clientId?: string;
  documentType?: DocumentType;
  uploadedById?: string;
  pageNumber?: number;
  pageSize?: number;
}

/** Paged document response */
export interface PagedDocumentResponse {
  items: DocumentSummary[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ========== Report Types ==========

/** Report type */
export type ReportType = 'Client' | 'Home';

/** Generate client report request */
export interface GenerateClientReportRequest {
  clientId: string;
  startDate: string;
  endDate: string;
}

/** Generate home report request */
export interface GenerateHomeReportRequest {
  homeId: string;
  startDate: string;
  endDate: string;
}

/** Report generation response */
export interface ReportGenerationResponse {
  success: boolean;
  error?: string;
  reportId?: string;
  fileName?: string;
}

// ========== Appointment Types ==========

/** Appointment type */
export type AppointmentType =
  | 'GeneralPractice'
  | 'Dental'
  | 'Ophthalmology'
  | 'Podiatry'
  | 'PhysicalTherapy'
  | 'OccupationalTherapy'
  | 'SpeechTherapy'
  | 'Psychiatry'
  | 'Dermatology'
  | 'Cardiology'
  | 'Neurology'
  | 'LabWork'
  | 'Imaging'
  | 'Audiology'
  | 'SocialWorker'
  | 'FamilyVisit'
  | 'Other';

/** Appointment status */
export type AppointmentStatus =
  | 'Scheduled'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow'
  | 'Rescheduled';

/** Appointment summary DTO for list views */
export interface AppointmentSummary {
  id: string;
  clientId: string;
  clientName: string;
  homeId: string;
  homeName: string;
  appointmentType: AppointmentType;
  status: AppointmentStatus;
  title: string;
  scheduledAt: string;
  durationMinutes: number;
  location?: string;
  providerName?: string;
}

/** Appointment DTO */
export interface Appointment {
  id: string;
  clientId: string;
  clientName: string;
  homeId: string;
  homeName: string;
  appointmentType: AppointmentType;
  status: AppointmentStatus;
  title: string;
  scheduledAt: string;
  durationMinutes: number;
  location?: string;
  providerName?: string;
  providerPhone?: string;
  notes?: string;
  transportationNotes?: string;
  reminderSent: boolean;
  createdById: string;
  createdByName: string;
  createdAt: string;
  updatedAt?: string;
  outcomeNotes?: string;
  completedById?: string;
  completedByName?: string;
  completedAt?: string;
}

/** Create appointment request */
export interface CreateAppointmentRequest {
  clientId: string;
  appointmentType: AppointmentType;
  title: string;
  scheduledAt: string;
  durationMinutes?: number;
  location?: string;
  providerName?: string;
  providerPhone?: string;
  notes?: string;
  transportationNotes?: string;
}

/** Update appointment request */
export interface UpdateAppointmentRequest {
  appointmentType?: AppointmentType;
  title?: string;
  scheduledAt?: string;
  durationMinutes?: number;
  location?: string;
  providerName?: string;
  providerPhone?: string;
  notes?: string;
  transportationNotes?: string;
}

/** Complete appointment request */
export interface CompleteAppointmentRequest {
  outcomeNotes?: string;
}

/** Cancel appointment request */
export interface CancelAppointmentRequest {
  cancellationReason?: string;
}

/** No-show appointment request */
export interface NoShowAppointmentRequest {
  notes?: string;
}

/** Reschedule appointment request */
export interface RescheduleAppointmentRequest {
  newScheduledAt: string;
  notes?: string;
}

/** Appointment operation response */
export interface AppointmentOperationResponse {
  success: boolean;
  error?: string;
  appointment?: Appointment;
}

/** Appointment query parameters */
export interface AppointmentQueryParams {
  clientId?: string;
  homeId?: string;
  status?: AppointmentStatus;
  appointmentType?: AppointmentType;
  startDate?: string;
  endDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

/** Paged appointment response */
export interface PagedAppointmentResponse {
  items: AppointmentSummary[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/** Upcoming appointment DTO for dashboard */
export interface UpcomingAppointment {
  id: string;
  clientId: string;
  clientName: string;
  homeId: string;
  homeName: string;
  appointmentType: AppointmentType;
  title: string;
  scheduledAt: string;
  durationMinutes: number;
  location?: string;
  providerName?: string;
  transportationNotes?: string;
}

