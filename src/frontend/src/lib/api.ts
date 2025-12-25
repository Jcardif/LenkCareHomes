import type {
  LoginRequest,
  LoginResponse,
  MfaVerifyResponse,
  BackupCodeVerifyRequest,
  MfaSetupResponse,
  MfaSetupConfirmRequest,
  PasswordResetRequest,
  PasswordResetConfirmRequest,
  InvitationAcceptRequest,
  InvitationAcceptResponse,
  User,
  InviteUserRequest,
  InviteUserResponse,
  UpdateUserRequest,
  UpdateProfileRequest,
  CompleteSetupResponse,
  CurrentUserResponse,
  RegenerateBackupCodesResponse,
  UserRole,
  AuditLogResponse,
  AuditLogStats,
  Home,
  HomeSummary,
  CreateHomeRequest,
  UpdateHomeRequest,
  HomeOperationResponse,
  Bed,
  CreateBedRequest,
  UpdateBedRequest,
  BedOperationResponse,
  Client,
  ClientSummary,
  AdmitClientRequest,
  UpdateClientRequest,
  DischargeClientRequest,
  TransferClientRequest,
  ClientOperationResponse,
  Caregiver,
  CaregiverSummary,
  AssignHomesRequest,
  CaregiverOperationResponse,
  AdminDashboardStats,
  CaregiverDashboardStats,
  ADLLog,
  CreateADLLogRequest,
  ADLLogOperationResponse,
  VitalsLog,
  CreateVitalsLogRequest,
  VitalsLogOperationResponse,
  MedicationLog,
  CreateMedicationLogRequest,
  MedicationLogOperationResponse,
  ROMLog,
  CreateROMLogRequest,
  ROMLogOperationResponse,
  BehaviorNote,
  CreateBehaviorNoteRequest,
  BehaviorNoteOperationResponse,
  Activity,
  CreateActivityRequest,
  UpdateActivityRequest,
  ActivityOperationResponse,
  TimelineEntryType,
  TimelineResponse,
  Incident,
  CreateIncidentRequest,
  UpdateIncidentRequest,
  AddFollowUpRequest,
  IncidentOperationResponse,
  IncidentStatus,
  IncidentType,
  PagedIncidentResponse,
  IncidentPhoto,
  UploadIncidentPhotoRequest,
  IncidentPhotoUploadResponse,
  IncidentPhotoOperationResponse,
  IncidentPhotoViewResponse,
  Document,
  DocumentSummary,
  DocumentAccessHistory,
  UploadDocumentRequest,
  UpdateDocumentRequest,
  GrantAccessRequest,
  GenerateClientReportRequest,
  GenerateHomeReportRequest,
  DocumentOperationResponse,
  DocumentType,
  DocumentScope,
  PagedDocumentResponse,
  DocumentUploadResponse,
  DocumentViewResponse,
  Folder,
  FolderSummary,
  FolderTreeNode,
  BreadcrumbItem,
  CreateFolderRequest,
  UpdateFolderRequest,
  MoveFolderRequest,
  FolderOperationResponse,
  BrowseDocumentsQuery,
  BrowseDocumentsResponse,
  // Passkey types
  PasskeyDto,
  PasskeyRegistrationStartRequest,
  PasskeyRegistrationStartResponse,
  PasskeyRegistrationCompleteRequest,
  PasskeyRegistrationCompleteResponse,
  PasskeyAuthenticationStartRequest,
  PasskeyAuthenticationStartResponse,
  PasskeyAuthenticationCompleteRequest,
  PasskeyAuthenticationCompleteResponse,
  PasskeyListResponse,
  PasskeyRenameRequest,
  PasskeyRenameResponse,
  PasskeyDeleteResponse,
  MfaResetRequest,
  MfaResetResponse,
  // Appointment types
  Appointment,
  AppointmentSummary,
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
  CompleteAppointmentRequest,
  CancelAppointmentRequest,
  NoShowAppointmentRequest,
  RescheduleAppointmentRequest,
  AppointmentOperationResponse,
  AppointmentStatus,
  AppointmentType,
  PagedAppointmentResponse,
  UpcomingAppointment,
} from '@/types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

/**
 * Get a user-friendly error message from any error.
 * Technical details are logged to console for developers.
 * Users see simple, helpful messages.
 */
export function getUserFriendlyError(
  error: unknown,
  fallbackMessage = 'Something went wrong. Please try again.'
): string {
  // Log technical details for developers
  console.error('Error details:', error);
  
  // Handle ApiError with specific status codes
  if (error instanceof ApiError) {
    switch (error.status) {
      case 400:
        // Bad request - could be validation error, show server message if it's user-friendly
        if (error.message && !error.message.includes('Error') && !error.message.includes('Exception')) {
          return error.message;
        }
        return 'Please check your input and try again.';
      case 401:
        return 'Your session has expired. Please sign in again.';
      case 403:
        return 'You don\'t have permission to do this.';
      case 404:
        return 'The requested information could not be found.';
      case 409:
        return 'This action conflicts with existing data. Please refresh and try again.';
      case 429:
        return 'Too many requests. Please wait a moment and try again.';
      case 500:
      case 502:
      case 503:
        return 'We\'re having technical difficulties. Please try again in a few minutes.';
      default:
        return fallbackMessage;
    }
  }
  
  // Handle native Error objects
  if (error instanceof Error) {
    // Known user-actionable errors
    if (error.name === 'NotAllowedError') {
      return 'The operation was cancelled. Please try again.';
    }
    if (error.name === 'AbortError') {
      return 'The operation was cancelled.';
    }
    if (error.name === 'TypeError' && error.message.includes('fetch')) {
      return 'Unable to connect. Please check your internet connection.';
    }
    // Don't expose raw error messages to users
    return fallbackMessage;
  }
  
  return fallbackMessage;
}

/**
 * Base API client for making HTTP requests to the backend
 */
class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    console.log(`[API] ${options.method || 'GET'} ${url}`, options.body ? JSON.parse(options.body as string) : undefined);

    const response = await fetch(url, {
      ...options,
      headers,
      credentials: 'include', // Include cookies for authentication
    });

    console.log(`[API] Response: ${response.status} ${response.statusText}`);

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      console.error('[API] Error response:', errorData);
      console.error('[API] Validation errors:', errorData.errors);
      throw new ApiError(
        response.status,
        errorData.error || errorData.message || 'An error occurred'
      );
    }

    // Handle empty responses
    const text = await response.text();
    if (!text) {
      return {} as T;
    }

    return JSON.parse(text);
  }

  async get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' });
  }

  async post<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async put<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }
}

/**
 * Custom error class for API errors
 */
export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
    this.name = 'ApiError';
  }
}

const apiClient = new ApiClient(API_BASE_URL);

/**
 * Authentication API
 */
export const authApi = {
  /**
   * Login with email and password
   * Returns requiresPasskey/requiresPasskeySetup to indicate next steps
   */
  login: (request: LoginRequest): Promise<LoginResponse> =>
    apiClient.post<LoginResponse>('/auth/login', request),

  /**
   * Verify backup code (Sysadmin self-service only)
   */
  verifyBackupCode: (request: BackupCodeVerifyRequest): Promise<MfaVerifyResponse> =>
    apiClient.post<MfaVerifyResponse>('/auth/mfa/verify-backup', request),

  /**
   * Setup MFA for a user (returns info for passkey registration)
   */
  setupMfa: (userId: string, passkeySetupToken?: string): Promise<MfaSetupResponse> =>
    apiClient.post<MfaSetupResponse>(`/auth/mfa/setup/${userId}${passkeySetupToken ? `?passkeySetupToken=${encodeURIComponent(passkeySetupToken)}` : ''}`),

  /**
   * Confirm MFA setup (after passkey registration)
   */
  confirmMfaSetup: (request: MfaSetupConfirmRequest, passkeySetupToken?: string): Promise<{ message: string }> =>
    apiClient.post<{ message: string }>(`/auth/mfa/setup/confirm${passkeySetupToken ? `?passkeySetupToken=${encodeURIComponent(passkeySetupToken)}` : ''}`, request),

  /**
   * Logout
   */
  logout: (): Promise<{ message: string }> =>
    apiClient.post<{ message: string }>('/auth/logout'),

  /**
   * Request password reset
   */
  requestPasswordReset: (request: PasswordResetRequest): Promise<{ message: string }> =>
    apiClient.post<{ message: string }>('/auth/password/reset-request', request),

  /**
   * Reset password
   */
  resetPassword: (request: PasswordResetConfirmRequest): Promise<{ message: string }> =>
    apiClient.post<{ message: string }>('/auth/password/reset', request),

  /**
   * Accept invitation
   */
  acceptInvitation: (request: InvitationAcceptRequest): Promise<InvitationAcceptResponse> =>
    apiClient.post<InvitationAcceptResponse>('/auth/invitation/accept', request),

  /**
   * Update user profile during setup
   */
  updateProfile: (request: UpdateProfileRequest, passkeySetupToken?: string): Promise<User> =>
    apiClient.put<User>(`/auth/profile/${request.userId}${passkeySetupToken ? `?passkeySetupToken=${encodeURIComponent(passkeySetupToken)}` : ''}`, request),

  /**
   * Complete setup and login (after passkey setup and profile completion)
   */
  completeSetupAndLogin: (userId: string, passkeySetupToken?: string): Promise<CompleteSetupResponse> =>
    apiClient.post<CompleteSetupResponse>(`/auth/complete-setup/${userId}${passkeySetupToken ? `?passkeySetupToken=${encodeURIComponent(passkeySetupToken)}` : ''}`),

  /**
   * Get current authenticated user details
   */
  getCurrentUser: (): Promise<CurrentUserResponse> =>
    apiClient.get<CurrentUserResponse>('/auth/me'),

  /**
   * Regenerate backup codes (Sysadmin only)
   * This invalidates all previous backup codes
   */
  regenerateBackupCodes: (): Promise<RegenerateBackupCodesResponse> =>
    apiClient.post<RegenerateBackupCodesResponse>('/auth/backup-codes/regenerate'),
};

/**
 * Passkey (WebAuthn) API
 * Handles passkey registration, authentication, and management
 */
export const passkeyApi = {
  // ============================================================================
  // Registration
  // ============================================================================

  /**
   * Start passkey registration - get WebAuthn options
   */
  startRegistration: async (request?: PasskeyRegistrationStartRequest, passkeySetupToken?: string): Promise<PasskeyRegistrationStartResponse> => {
    const params = new URLSearchParams();
    if (request?.userId) params.append('userId', request.userId);
    if (passkeySetupToken) params.append('passkeySetupToken', passkeySetupToken);
    
    const queryString = params.toString() ? `?${params.toString()}` : '';
    console.log('[Passkey] Starting registration with params:', queryString, 'request:', request);
    
    try {
      const response = await apiClient.post<{ success: boolean; sessionId?: string; options?: string; error?: string }>(
        `/passkey/register/begin${queryString}`, 
        { deviceName: request?.deviceName }
      );
      
      console.log('[Passkey] Registration response:', response);
      
      // Parse the options JSON string from the backend
      const result = {
        success: response.success,
        sessionId: response.sessionId,
        options: response.options ? JSON.parse(response.options) : undefined,
        error: response.error,
      };
      
      console.log('[Passkey] Parsed options:', result);
      return result;
    } catch (err) {
      console.error('[Passkey] Registration error:', err);
      throw err;
    }
  },

  /**
   * Complete passkey registration - verify and store the credential
   */
  completeRegistration: (request: PasskeyRegistrationCompleteRequest, userId?: string, passkeySetupToken?: string): Promise<PasskeyRegistrationCompleteResponse> => {
    const params = new URLSearchParams();
    if (userId) params.append('userId', userId);
    if (passkeySetupToken) params.append('passkeySetupToken', passkeySetupToken);
    
    const queryString = params.toString() ? `?${params.toString()}` : '';
    return apiClient.post<PasskeyRegistrationCompleteResponse>(`/passkey/register/complete${queryString}`, request);
  },

  // ============================================================================
  // Authentication
  // ============================================================================

  /**
   * Start passkey authentication - get WebAuthn assertion options
   */
  startAuthentication: async (request: PasskeyAuthenticationStartRequest): Promise<PasskeyAuthenticationStartResponse> => {
    const response = await apiClient.post<{ success: boolean; sessionId?: string; options?: string; error?: string }>(
      '/passkey/authenticate/begin', 
      request
    );
    
    // Parse the options JSON string from the backend
    return {
      success: response.success,
      sessionId: response.sessionId,
      options: response.options ? JSON.parse(response.options) : undefined,
      error: response.error,
    };
  },

  /**
   * Complete passkey authentication - verify assertion and log in
   */
  completeAuthentication: (request: PasskeyAuthenticationCompleteRequest): Promise<PasskeyAuthenticationCompleteResponse> =>
    apiClient.post<PasskeyAuthenticationCompleteResponse>('/passkey/authenticate/complete', request),

  // ============================================================================
  // Management
  // ============================================================================

  /**
   * Get all passkeys for the current user
   */
  getMyPasskeys: (): Promise<PasskeyListResponse> =>
    apiClient.get<PasskeyListResponse>('/passkey'),

  /**
   * Get passkey by ID
   */
  getById: (id: string): Promise<PasskeyDto> =>
    apiClient.get<PasskeyDto>(`/passkey/${id}`),

  /**
   * Rename a passkey
   */
  rename: (id: string, request: PasskeyRenameRequest): Promise<PasskeyRenameResponse> =>
    apiClient.put<PasskeyRenameResponse>(`/passkey/${id}`, request),

  /**
   * Delete a passkey
   */
  delete: (id: string): Promise<PasskeyDeleteResponse> =>
    apiClient.delete<PasskeyDeleteResponse>(`/passkey/${id}`),
};

/**
 * Users API
 */
export const usersApi = {
  /**
   * Get all users
   */
  getAll: (): Promise<User[]> =>
    apiClient.get<User[]>('/users'),

  /**
   * Get user by ID
   */
  getById: (id: string): Promise<User> =>
    apiClient.get<User>(`/users/${id}`),

  /**
   * Invite a new user
   */
  invite: (request: InviteUserRequest): Promise<InviteUserResponse> =>
    apiClient.post<InviteUserResponse>('/users/invite', request),

  /**
   * Resend invitation email to a user who hasn't accepted yet
   */
  resendInvitation: (id: string): Promise<{ success: boolean; message?: string; error?: string }> =>
    apiClient.post<{ success: boolean; message?: string; error?: string }>(`/users/${id}/resend-invitation`),

  /**
   * Update a user
   */
  update: (id: string, request: UpdateUserRequest): Promise<User> =>
    apiClient.put<User>(`/users/${id}`, request),

  /**
   * Deactivate a user
   */
  deactivate: (id: string): Promise<{ message: string }> =>
    apiClient.post<{ message: string }>(`/users/${id}/deactivate`),

  /**
   * Reactivate a user
   */
  reactivate: (id: string): Promise<{ message: string }> =>
    apiClient.post<{ message: string }>(`/users/${id}/reactivate`),

  /**
   * Delete a user permanently
   */
  delete: (id: string): Promise<{ message: string }> =>
    apiClient.delete<{ message: string }>(`/users/${id}`),

  /**
   * Reset user MFA (Sysadmin only)
   * Removes all passkeys and backup codes, requires reason and verification method
   */
  resetMfa: (request: MfaResetRequest): Promise<MfaResetResponse> =>
    apiClient.post<MfaResetResponse>(`/users/${request.userId}/reset-mfa`, request),

  /**
   * Assign role to user
   */
  assignRole: (id: string, role: UserRole): Promise<{ message: string }> =>
    apiClient.post<{ message: string }>(`/users/${id}/roles/${role}`),

  /**
   * Remove role from user
   */
  removeRole: (id: string, role: UserRole): Promise<{ message: string }> =>
    apiClient.delete<{ message: string }>(`/users/${id}/roles/${role}`),

  /**
   * Get available roles
   */
  getRoles: (): Promise<UserRole[]> =>
    apiClient.get<UserRole[]>('/users/roles'),
};

/**
 * Audit Logs API
 */
export interface AuditLogQueryParams {
  userId?: string;
  action?: string;
  resourceType?: string;
  resourceId?: string;
  outcome?: string;
  searchText?: string;
  fromDate?: string;
  toDate?: string;
  pageSize?: number;
  continuationToken?: string;
}

export const auditApi = {
  /**
   * Get audit logs with optional filters
   */
  getLogs: (params?: AuditLogQueryParams): Promise<AuditLogResponse> => {
    const searchParams = new URLSearchParams();
    if (params?.userId) searchParams.append('userId', params.userId);
    if (params?.action) searchParams.append('action', params.action);
    if (params?.resourceType) searchParams.append('resourceType', params.resourceType);
    if (params?.resourceId) searchParams.append('resourceId', params.resourceId);
    if (params?.outcome) searchParams.append('outcome', params.outcome);
    if (params?.searchText) searchParams.append('searchText', params.searchText);
    if (params?.fromDate) searchParams.append('fromDate', params.fromDate);
    if (params?.toDate) searchParams.append('toDate', params.toDate);
    if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    if (params?.continuationToken) searchParams.append('continuationToken', params.continuationToken);
    
    const query = searchParams.toString();
    return apiClient.get<AuditLogResponse>(`/audit${query ? `?${query}` : ''}`);
  },

  /**
   * Get audit log statistics
   */
  getStats: (): Promise<AuditLogStats> =>
    apiClient.get<AuditLogStats>('/audit/stats'),

  /**
   * Get available action types for filtering
   */
  getActions: (): Promise<string[]> =>
    apiClient.get<string[]>('/audit/actions'),

  /**
   * Export audit logs to CSV
   */
  exportCsv: async (params?: Omit<AuditLogQueryParams, 'pageSize' | 'continuationToken'> & { maxRecords?: number }): Promise<Blob> => {
    const searchParams = new URLSearchParams();
    if (params?.userId) searchParams.append('userId', params.userId);
    if (params?.action) searchParams.append('action', params.action);
    if (params?.resourceType) searchParams.append('resourceType', params.resourceType);
    if (params?.resourceId) searchParams.append('resourceId', params.resourceId);
    if (params?.outcome) searchParams.append('outcome', params.outcome);
    if (params?.searchText) searchParams.append('searchText', params.searchText);
    if (params?.fromDate) searchParams.append('fromDate', params.fromDate);
    if (params?.toDate) searchParams.append('toDate', params.toDate);
    if (params?.maxRecords) searchParams.append('maxRecords', params.maxRecords.toString());
    
    const query = searchParams.toString();
    const url = `${API_BASE_URL}/audit/export${query ? `?${query}` : ''}`;
    
    const response = await fetch(url, {
      method: 'GET',
      credentials: 'include',
    });

    if (!response.ok) {
      throw new ApiError(response.status, 'Failed to export audit logs');
    }

    return response.blob();
  },
};

/**
 * Homes API
 */
export const homesApi = {
  /**
   * Get all homes
   */
  getAll: (includeInactive = false): Promise<HomeSummary[]> =>
    apiClient.get<HomeSummary[]>(`/homes?includeInactive=${includeInactive}`),

  /**
   * Get home by ID
   */
  getById: (id: string): Promise<Home> =>
    apiClient.get<Home>(`/homes/${id}`),

  /**
   * Create a new home
   */
  create: (request: CreateHomeRequest): Promise<HomeOperationResponse> =>
    apiClient.post<HomeOperationResponse>('/homes', request),

  /**
   * Update a home
   */
  update: (id: string, request: UpdateHomeRequest): Promise<HomeOperationResponse> =>
    apiClient.put<HomeOperationResponse>(`/homes/${id}`, request),

  /**
   * Deactivate a home
   */
  deactivate: (id: string): Promise<HomeOperationResponse> =>
    apiClient.post<HomeOperationResponse>(`/homes/${id}/deactivate`),

  /**
   * Reactivate a home
   */
  reactivate: (id: string): Promise<HomeOperationResponse> =>
    apiClient.post<HomeOperationResponse>(`/homes/${id}/reactivate`),

  /**
   * Get beds for a home
   */
  getBeds: (homeId: string, includeInactive = false): Promise<Bed[]> =>
    apiClient.get<Bed[]>(`/homes/${homeId}/beds?includeInactive=${includeInactive}`),

  /**
   * Get available beds for a home
   */
  getAvailableBeds: (homeId: string): Promise<Bed[]> =>
    apiClient.get<Bed[]>(`/homes/${homeId}/beds/available`),

  /**
   * Create a bed in a home
   */
  createBed: (homeId: string, request: CreateBedRequest): Promise<BedOperationResponse> =>
    apiClient.post<BedOperationResponse>(`/homes/${homeId}/beds`, request),
};

/**
 * Beds API
 */
export const bedsApi = {
  /**
   * Get bed by ID
   */
  getById: (id: string): Promise<Bed> =>
    apiClient.get<Bed>(`/beds/${id}`),

  /**
   * Update a bed
   */
  update: (id: string, request: UpdateBedRequest): Promise<BedOperationResponse> =>
    apiClient.put<BedOperationResponse>(`/beds/${id}`, request),
};

/**
 * Clients API
 */
export interface ClientQueryParams {
  homeId?: string;
  isActive?: boolean;
}

export const clientsApi = {
  /**
   * Get all clients with optional filters
   */
  getAll: (params?: ClientQueryParams): Promise<ClientSummary[]> => {
    const searchParams = new URLSearchParams();
    if (params?.homeId) searchParams.append('homeId', params.homeId);
    if (params?.isActive !== undefined) searchParams.append('isActive', params.isActive.toString());
    const query = searchParams.toString();
    return apiClient.get<ClientSummary[]>(`/clients${query ? `?${query}` : ''}`);
  },

  /**
   * Get client by ID
   */
  getById: (id: string): Promise<Client> =>
    apiClient.get<Client>(`/clients/${id}`),

  /**
   * Admit a new client
   */
  admit: (request: AdmitClientRequest): Promise<ClientOperationResponse> =>
    apiClient.post<ClientOperationResponse>('/clients', request),

  /**
   * Update a client
   */
  update: (id: string, request: UpdateClientRequest): Promise<ClientOperationResponse> =>
    apiClient.put<ClientOperationResponse>(`/clients/${id}`, request),

  /**
   * Discharge a client
   */
  discharge: (id: string, request: DischargeClientRequest): Promise<ClientOperationResponse> =>
    apiClient.post<ClientOperationResponse>(`/clients/${id}/discharge`, request),

  /**
   * Transfer a client to a different bed
   */
  transfer: (id: string, request: TransferClientRequest): Promise<ClientOperationResponse> =>
    apiClient.post<ClientOperationResponse>(`/clients/${id}/transfer`, request),
};

/**
 * Caregivers API
 */
export const caregiversApi = {
  /**
   * Get all caregivers
   */
  getAll: (includeInactive = false): Promise<CaregiverSummary[]> =>
    apiClient.get<CaregiverSummary[]>(`/caregivers?includeInactive=${includeInactive}`),

  /**
   * Get caregivers assigned to a specific home
   */
  getByHome: (homeId: string, includeInactive = false): Promise<CaregiverSummary[]> =>
    apiClient.get<CaregiverSummary[]>(`/caregivers/by-home/${homeId}?includeInactive=${includeInactive}`),

  /**
   * Get caregiver by ID
   */
  getById: (id: string): Promise<Caregiver> =>
    apiClient.get<Caregiver>(`/caregivers/${id}`),

  /**
   * Assign homes to a caregiver
   */
  assignHomes: (id: string, request: AssignHomesRequest): Promise<CaregiverOperationResponse> =>
    apiClient.post<CaregiverOperationResponse>(`/caregivers/${id}/assign-homes`, request),

  /**
   * Remove home assignment from a caregiver
   */
  removeHomeAssignment: (caregiverId: string, homeId: string): Promise<CaregiverOperationResponse> =>
    apiClient.delete<CaregiverOperationResponse>(`/caregivers/${caregiverId}/homes/${homeId}`),
};

/**
 * Dashboard API
 */
export const dashboardApi = {
  /**
   * Get admin dashboard stats
   */
  getAdminStats: (): Promise<AdminDashboardStats> =>
    apiClient.get<AdminDashboardStats>('/dashboard/admin'),

  /**
   * Get caregiver dashboard stats
   */
  getCaregiverStats: (): Promise<CaregiverDashboardStats> =>
    apiClient.get<CaregiverDashboardStats>('/dashboard/caregiver'),
};

/**
 * Appointments API
 */
export interface AppointmentQueryParams {
  clientId?: string;
  homeId?: string;
  status?: AppointmentStatus;
  appointmentType?: AppointmentType;
  startDate?: string;
  endDate?: string;
  pageNumber?: number;
  pageSize?: number;
  sortDescending?: boolean;
}

export const appointmentsApi = {
  /**
   * Get all appointments with optional filters
   */
  getAll: (params?: AppointmentQueryParams): Promise<PagedAppointmentResponse> => {
    const searchParams = new URLSearchParams();
    if (params?.clientId) searchParams.append('clientId', params.clientId);
    if (params?.homeId) searchParams.append('homeId', params.homeId);
    if (params?.status) searchParams.append('status', params.status);
    if (params?.appointmentType) searchParams.append('appointmentType', params.appointmentType);
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    if (params?.pageNumber) searchParams.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    // Default to true (descending) if not specified - shows most recent first
    searchParams.append('sortDescending', (params?.sortDescending !== false).toString());
    const query = searchParams.toString();
    return apiClient.get<PagedAppointmentResponse>(`/appointments${query ? `?${query}` : ''}`);
  },

  /**
   * Get appointment by ID
   */
  getById: (id: string): Promise<Appointment> =>
    apiClient.get<Appointment>(`/appointments/${id}`),

  /**
   * Get upcoming appointments for dashboard
   */
  getUpcoming: (days = 7, limit = 10): Promise<UpcomingAppointment[]> =>
    apiClient.get<UpcomingAppointment[]>(`/appointments/upcoming?days=${days}&limit=${limit}`),

  /**
   * Get appointments for a specific client
   */
  getByClient: (clientId: string, includeCompleted = true): Promise<AppointmentSummary[]> =>
    apiClient.get<AppointmentSummary[]>(`/appointments/by-client/${clientId}?includeCompleted=${includeCompleted}`),

  /**
   * Create a new appointment
   */
  create: (request: CreateAppointmentRequest): Promise<AppointmentOperationResponse> =>
    apiClient.post<AppointmentOperationResponse>('/appointments', request),

  /**
   * Update an appointment
   */
  update: (id: string, request: UpdateAppointmentRequest): Promise<AppointmentOperationResponse> =>
    apiClient.put<AppointmentOperationResponse>(`/appointments/${id}`, request),

  /**
   * Complete an appointment
   */
  complete: (id: string, request?: CompleteAppointmentRequest): Promise<AppointmentOperationResponse> =>
    apiClient.post<AppointmentOperationResponse>(`/appointments/${id}/complete`, request || {}),

  /**
   * Cancel an appointment
   */
  cancel: (id: string, request?: CancelAppointmentRequest): Promise<AppointmentOperationResponse> =>
    apiClient.post<AppointmentOperationResponse>(`/appointments/${id}/cancel`, request || {}),

  /**
   * Mark an appointment as no-show
   */
  noShow: (id: string, request?: NoShowAppointmentRequest): Promise<AppointmentOperationResponse> =>
    apiClient.post<AppointmentOperationResponse>(`/appointments/${id}/no-show`, request || {}),

  /**
   * Reschedule an appointment
   */
  reschedule: (id: string, request: RescheduleAppointmentRequest): Promise<AppointmentOperationResponse> =>
    apiClient.post<AppointmentOperationResponse>(`/appointments/${id}/reschedule`, request),

  /**
   * Delete an appointment (Admin only, scheduled appointments only)
   */
  delete: (id: string): Promise<AppointmentOperationResponse> =>
    apiClient.delete<AppointmentOperationResponse>(`/appointments/${id}`),
};

/**
 * ADL Logs API
 */
export interface ADLLogQueryParams {
  startDate?: string;
  endDate?: string;
}

export const adlLogsApi = {
  /**
   * Get all ADL logs for a client
   */
  getAll: (clientId: string, params?: ADLLogQueryParams): Promise<ADLLog[]> => {
    const searchParams = new URLSearchParams();
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    const query = searchParams.toString();
    return apiClient.get<ADLLog[]>(`/clients/${clientId}/adls${query ? `?${query}` : ''}`);
  },

  /**
   * Get ADL log by ID
   */
  getById: (clientId: string, id: string): Promise<ADLLog> =>
    apiClient.get<ADLLog>(`/clients/${clientId}/adls/${id}`),

  /**
   * Create a new ADL log
   */
  create: (clientId: string, request: CreateADLLogRequest): Promise<ADLLogOperationResponse> =>
    apiClient.post<ADLLogOperationResponse>(`/clients/${clientId}/adls`, request),
};

/**
 * Vitals Logs API
 */
export interface VitalsLogQueryParams {
  startDate?: string;
  endDate?: string;
}

export const vitalsLogsApi = {
  /**
   * Get all vitals logs for a client
   */
  getAll: (clientId: string, params?: VitalsLogQueryParams): Promise<VitalsLog[]> => {
    const searchParams = new URLSearchParams();
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    const query = searchParams.toString();
    return apiClient.get<VitalsLog[]>(`/clients/${clientId}/vitals${query ? `?${query}` : ''}`);
  },

  /**
   * Get vitals log by ID
   */
  getById: (clientId: string, id: string): Promise<VitalsLog> =>
    apiClient.get<VitalsLog>(`/clients/${clientId}/vitals/${id}`),

  /**
   * Create a new vitals log
   */
  create: (clientId: string, request: CreateVitalsLogRequest): Promise<VitalsLogOperationResponse> =>
    apiClient.post<VitalsLogOperationResponse>(`/clients/${clientId}/vitals`, request),
};

/**
 * Medication Logs API
 */
export interface MedicationLogQueryParams {
  startDate?: string;
  endDate?: string;
}

export const medicationLogsApi = {
  /**
   * Get all medication logs for a client
   */
  getAll: (clientId: string, params?: MedicationLogQueryParams): Promise<MedicationLog[]> => {
    const searchParams = new URLSearchParams();
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    const query = searchParams.toString();
    return apiClient.get<MedicationLog[]>(`/clients/${clientId}/medications${query ? `?${query}` : ''}`);
  },

  /**
   * Get medication log by ID
   */
  getById: (clientId: string, id: string): Promise<MedicationLog> =>
    apiClient.get<MedicationLog>(`/clients/${clientId}/medications/${id}`),

  /**
   * Create a new medication log
   */
  create: (clientId: string, request: CreateMedicationLogRequest): Promise<MedicationLogOperationResponse> =>
    apiClient.post<MedicationLogOperationResponse>(`/clients/${clientId}/medications`, request),
};

/**
 * ROM Logs API
 */
export interface ROMLogQueryParams {
  startDate?: string;
  endDate?: string;
}

export const romLogsApi = {
  /**
   * Get all ROM logs for a client
   */
  getAll: (clientId: string, params?: ROMLogQueryParams): Promise<ROMLog[]> => {
    const searchParams = new URLSearchParams();
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    const query = searchParams.toString();
    return apiClient.get<ROMLog[]>(`/clients/${clientId}/rom${query ? `?${query}` : ''}`);
  },

  /**
   * Get ROM log by ID
   */
  getById: (clientId: string, id: string): Promise<ROMLog> =>
    apiClient.get<ROMLog>(`/clients/${clientId}/rom/${id}`),

  /**
   * Create a new ROM log
   */
  create: (clientId: string, request: CreateROMLogRequest): Promise<ROMLogOperationResponse> =>
    apiClient.post<ROMLogOperationResponse>(`/clients/${clientId}/rom`, request),
};

/**
 * Behavior Notes API
 */
export interface BehaviorNoteQueryParams {
  startDate?: string;
  endDate?: string;
}

export const behaviorNotesApi = {
  /**
   * Get all behavior notes for a client
   */
  getAll: (clientId: string, params?: BehaviorNoteQueryParams): Promise<BehaviorNote[]> => {
    const searchParams = new URLSearchParams();
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    const query = searchParams.toString();
    return apiClient.get<BehaviorNote[]>(`/clients/${clientId}/behavior-notes${query ? `?${query}` : ''}`);
  },

  /**
   * Get behavior note by ID
   */
  getById: (clientId: string, id: string): Promise<BehaviorNote> =>
    apiClient.get<BehaviorNote>(`/clients/${clientId}/behavior-notes/${id}`),

  /**
   * Create a new behavior note
   */
  create: (clientId: string, request: CreateBehaviorNoteRequest): Promise<BehaviorNoteOperationResponse> =>
    apiClient.post<BehaviorNoteOperationResponse>(`/clients/${clientId}/behavior-notes`, request),
};

/**
 * Activities API
 */
export interface ActivityQueryParams {
  homeId?: string;
  startDate?: string;
  endDate?: string;
}

export const activitiesApi = {
  /**
   * Get activities for a client
   */
  getByClient: (clientId: string, params?: ActivityQueryParams): Promise<Activity[]> => {
    const searchParams = new URLSearchParams();
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    const query = searchParams.toString();
    return apiClient.get<Activity[]>(`/activities/by-client/${clientId}${query ? `?${query}` : ''}`);
  },

  /**
   * Get activities for a home
   */
  getByHome: (homeId: string, params?: ActivityQueryParams): Promise<Activity[]> => {
    const searchParams = new URLSearchParams();
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    const query = searchParams.toString();
    return apiClient.get<Activity[]>(`/activities/by-home/${homeId}${query ? `?${query}` : ''}`);
  },

  /**
   * Get activity by ID
   */
  getById: (id: string): Promise<Activity> =>
    apiClient.get<Activity>(`/activities/${id}`),

  /**
   * Create a new activity
   */
  create: (request: CreateActivityRequest): Promise<ActivityOperationResponse> =>
    apiClient.post<ActivityOperationResponse>('/activities', request),

  /**
   * Update an activity
   */
  update: (id: string, request: UpdateActivityRequest): Promise<ActivityOperationResponse> =>
    apiClient.put<ActivityOperationResponse>(`/activities/${id}`, request),

  /**
   * Delete an activity
   */
  delete: (id: string): Promise<ActivityOperationResponse> =>
    apiClient.delete<ActivityOperationResponse>(`/activities/${id}`),
};

/**
 * Timeline API
 */
export interface TimelineQueryParams {
  startDate?: string;
  endDate?: string;
  entryTypes?: TimelineEntryType[];
  pageNumber?: number;
  pageSize?: number;
}

export const timelineApi = {
  /**
   * Get timeline for a client
   */
  getClientTimeline: (clientId: string, params?: TimelineQueryParams): Promise<TimelineResponse> => {
    const searchParams = new URLSearchParams();
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    if (params?.entryTypes && params.entryTypes.length > 0) {
      params.entryTypes.forEach(type => searchParams.append('entryTypes', type));
    }
    if (params?.pageNumber) searchParams.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    const query = searchParams.toString();
    return apiClient.get<TimelineResponse>(`/clients/${clientId}/timeline${query ? `?${query}` : ''}`);
  },
};

/**
 * Incidents API
 */
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

export const incidentsApi = {
  /**
   * Get all incidents with optional filters
   */
  getAll: (params?: IncidentQueryParams): Promise<PagedIncidentResponse> => {
    const searchParams = new URLSearchParams();
    if (params?.homeId) searchParams.append('homeId', params.homeId);
    if (params?.clientId) searchParams.append('clientId', params.clientId);
    if (params?.status) searchParams.append('status', params.status);
    if (params?.incidentType) searchParams.append('incidentType', params.incidentType);
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    if (params?.pageNumber) searchParams.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    const query = searchParams.toString();
    return apiClient.get<PagedIncidentResponse>(`/incidents${query ? `?${query}` : ''}`);
  },

  /**
   * Get incident by ID
   */
  getById: (id: string): Promise<Incident> =>
    apiClient.get<Incident>(`/incidents/${id}`),

  /**
   * Create a new incident
   */
  create: (request: CreateIncidentRequest): Promise<IncidentOperationResponse> =>
    apiClient.post<IncidentOperationResponse>('/incidents', request),

  /**
   * Update an incident
   */
  update: (id: string, request: UpdateIncidentRequest): Promise<IncidentOperationResponse> =>
    apiClient.put<IncidentOperationResponse>(`/incidents/${id}`, request),

  /**
   * Submit a draft incident
   */
  submit: (id: string): Promise<IncidentOperationResponse> =>
    apiClient.post<IncidentOperationResponse>(`/incidents/${id}/submit`),

  /**
   * Delete a draft incident (only author can delete)
   */
  delete: (id: string): Promise<IncidentOperationResponse> =>
    apiClient.delete<IncidentOperationResponse>(`/incidents/${id}`),

  /**
   * Add a follow-up note to an incident
   */
  addFollowUp: (id: string, request: AddFollowUpRequest): Promise<IncidentOperationResponse> =>
    apiClient.post<IncidentOperationResponse>(`/incidents/${id}/follow-up`, request),

  /**
   * Update incident status (Admin only)
   */
  updateStatus: (id: string, status: IncidentStatus, closureNotes?: string): Promise<IncidentOperationResponse> =>
    apiClient.put<IncidentOperationResponse>(`/incidents/${id}/status`, { newStatus: status, closureNotes }),

  /**
   * Close an incident (Admin only)
   */
  close: (id: string, closureNotes?: string): Promise<IncidentOperationResponse> =>
    apiClient.put<IncidentOperationResponse>(`/incidents/${id}/status`, { newStatus: 'Closed', closureNotes }),

  /**
   * Get incidents for a specific client
   */
  getByClient: (clientId: string, params?: Omit<IncidentQueryParams, 'clientId'>): Promise<PagedIncidentResponse> => {
    const searchParams = new URLSearchParams();
    if (params?.homeId) searchParams.append('homeId', params.homeId);
    if (params?.status) searchParams.append('status', params.status);
    if (params?.incidentType) searchParams.append('incidentType', params.incidentType);
    if (params?.startDate) searchParams.append('startDate', params.startDate);
    if (params?.endDate) searchParams.append('endDate', params.endDate);
    if (params?.pageNumber) searchParams.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    const query = searchParams.toString();
    return apiClient.get<PagedIncidentResponse>(`/clients/${clientId}/incidents${query ? `?${query}` : ''}`);
  },

  // ============================================================================
  // Photo Management
  // ============================================================================

  /**
   * Get all photos for an incident
   */
  getPhotos: (incidentId: string): Promise<IncidentPhoto[]> =>
    apiClient.get<IncidentPhoto[]>(`/incidents/${incidentId}/photos`),

  /**
   * Initiate a photo upload - returns SAS URL for direct blob upload
   */
  initiatePhotoUpload: (incidentId: string, request: UploadIncidentPhotoRequest): Promise<IncidentPhotoUploadResponse> =>
    apiClient.post<IncidentPhotoUploadResponse>(`/incidents/${incidentId}/photos`, request),

  /**
   * Upload file directly to Azure Blob Storage using SAS URL
   */
  uploadPhotoToBlob: async (uploadUrl: string, file: File): Promise<void> => {
    const response = await fetch(uploadUrl, {
      method: 'PUT',
      headers: {
        'x-ms-blob-type': 'BlockBlob',
        'Content-Type': file.type || 'application/octet-stream',
      },
      body: file,
    });

    if (!response.ok) {
      throw new ApiError(response.status, 'Failed to upload photo to storage');
    }
  },

  /**
   * Confirm a photo upload completed
   */
  confirmPhotoUpload: (incidentId: string, photoId: string): Promise<IncidentPhotoOperationResponse> =>
    apiClient.post<IncidentPhotoOperationResponse>(`/incidents/${incidentId}/photos/${photoId}/confirm`),

  /**
   * Full photo upload flow: initiate -> upload to blob -> confirm
   * If blob upload fails, attempts to clean up the orphan database record
   */
  uploadPhoto: async (
    incidentId: string,
    file: File,
    caption?: string
  ): Promise<IncidentPhotoOperationResponse> => {
    // Step 1: Initiate upload and get SAS URL
    const initResponse = await incidentsApi.initiatePhotoUpload(incidentId, {
      fileName: file.name,
      contentType: file.type || 'application/octet-stream',
      fileSizeBytes: file.size,
      caption,
    });

    if (!initResponse.success || !initResponse.uploadUrl || !initResponse.photoId) {
      return {
        success: false,
        error: initResponse.error || 'Failed to initiate photo upload',
      };
    }

    // Step 2: Upload file directly to Azure Blob Storage
    try {
      await incidentsApi.uploadPhotoToBlob(initResponse.uploadUrl, file);
    } catch (error) {
      // Clean up the orphan database record if blob upload fails
      try {
        await incidentsApi.deletePhoto(incidentId, initResponse.photoId);
      } catch {
        // Silently ignore cleanup errors - the record will be orphaned but won't affect user
        console.warn('Failed to clean up orphan photo record:', initResponse.photoId);
      }
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Failed to upload photo',
      };
    }

    // Step 3: Confirm upload completed
    return incidentsApi.confirmPhotoUpload(incidentId, initResponse.photoId);
  },

  /**
   * Get SAS URL for viewing/downloading a photo
   */
  getPhotoViewUrl: (incidentId: string, photoId: string): Promise<IncidentPhotoViewResponse> =>
    apiClient.get<IncidentPhotoViewResponse>(`/incidents/${incidentId}/photos/${photoId}/view`),

  /**
   * Delete a photo from an incident
   */
  deletePhoto: (incidentId: string, photoId: string): Promise<IncidentPhotoOperationResponse> =>
    apiClient.delete<IncidentPhotoOperationResponse>(`/incidents/${incidentId}/photos/${photoId}`),

  /**
   * Export an incident report as PDF
   */
  exportPdf: async (incidentId: string): Promise<Blob> => {
    const url = `${API_BASE_URL}/incidents/${incidentId}/export/pdf`;
    const response = await fetch(url, {
      method: 'GET',
      credentials: 'include',
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new ApiError(
        response.status,
        errorData.error || 'Failed to export incident report'
      );
    }

    return response.blob();
  },
};

/**
 * Documents API
 */
export interface DocumentQueryParams {
  clientId?: string;
  documentType?: DocumentType;
  uploadedById?: string;
  pageNumber?: number;
  pageSize?: number;
}

export const documentsApi = {
  /**
   * Get all documents for a client
   */
  getByClient: async (clientId: string, params?: Omit<DocumentQueryParams, 'clientId'>): Promise<PagedDocumentResponse> => {
    const searchParams = new URLSearchParams();
    if (params?.documentType) searchParams.append('documentType', params.documentType);
    if (params?.uploadedById) searchParams.append('uploadedById', params.uploadedById);
    if (params?.pageNumber) searchParams.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    const query = searchParams.toString();
    
    // Backend returns array directly, wrap in paged response format
    const documents = await apiClient.get<DocumentSummary[]>(`/clients/${clientId}/documents${query ? `?${query}` : ''}`);
    const pageNumber = params?.pageNumber || 1;
    const pageSize = params?.pageSize || documents.length;
    const totalPages = 1;
    return {
      items: documents,
      totalCount: documents.length,
      pageNumber,
      pageSize,
      totalPages,
      hasNextPage: pageNumber < totalPages,
      hasPreviousPage: pageNumber > 1,
    };
  },

  /**
   * Get document by ID
   */
  getById: (id: string): Promise<Document> =>
    apiClient.get<Document>(`/documents/${id}`),

  /**
   * Initiate document upload - returns SAS URL for direct blob upload
   */
  initiateUpload: (clientId: string, request: UploadDocumentRequest): Promise<DocumentUploadResponse> =>
    apiClient.post<DocumentUploadResponse>(`/clients/${clientId}/documents`, request),

  /**
   * Upload file directly to Azure Blob Storage using SAS URL
   */
  uploadToBlob: async (uploadUrl: string, file: File): Promise<void> => {
    const response = await fetch(uploadUrl, {
      method: 'PUT',
      headers: {
        'x-ms-blob-type': 'BlockBlob',
        'Content-Type': file.type || 'application/octet-stream',
      },
      body: file,
    });

    if (!response.ok) {
      throw new ApiError(response.status, 'Failed to upload file to storage');
    }
  },

  /**
   * Confirm document upload completed
   */
  confirmUpload: (documentId: string): Promise<DocumentOperationResponse> =>
    apiClient.post<DocumentOperationResponse>(`/documents/${documentId}/confirm`),

  /**
   * Full upload flow: initiate -> upload to blob -> confirm
   */
  upload: async (
    clientId: string,
    file: File,
    documentType: DocumentType,
    description?: string
  ): Promise<DocumentOperationResponse> => {
    // Step 1: Initiate upload and get SAS URL
    console.log('Step 1: Initiating upload...');
    const initResponse = await documentsApi.initiateUpload(clientId, {
      fileName: file.name,
      contentType: file.type || 'application/octet-stream',
      fileSizeBytes: file.size,
      documentType,
      description,
    });
    console.log('Step 1 response:', initResponse);

    if (!initResponse.success || !initResponse.uploadUrl || !initResponse.documentId) {
      return {
        success: false,
        error: initResponse.error || 'Failed to initiate upload',
      };
    }

    // Step 2: Upload file directly to Azure Blob Storage
    console.log('Step 2: Uploading to blob storage...');
    try {
      await documentsApi.uploadToBlob(initResponse.uploadUrl, file);
      console.log('Step 2: Upload to blob completed');
    } catch (error) {
      console.error('Step 2 failed:', error);
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Failed to upload file',
      };
    }

    // Step 3: Confirm upload completed
    console.log('Step 3: Confirming upload with documentId:', initResponse.documentId);
    const confirmResponse = await documentsApi.confirmUpload(initResponse.documentId);
    console.log('Step 3 response:', confirmResponse);
    return confirmResponse;
  },

  /**
   * Update document metadata
   */
  update: (id: string, request: UpdateDocumentRequest): Promise<DocumentOperationResponse> =>
    apiClient.put<DocumentOperationResponse>(`/documents/${id}`, request),

  /**
   * Delete a document (soft delete)
   */
  delete: (id: string): Promise<DocumentOperationResponse> =>
    apiClient.delete<DocumentOperationResponse>(`/documents/${id}`),

  /**
   * Get SAS URL for viewing/downloading a document
   * Returns a time-limited URL to Azure Blob Storage
   */
  getSasUrl: (id: string): Promise<DocumentViewResponse> =>
    apiClient.get<DocumentViewResponse>(`/documents/${id}/sas`),

  /**
   * Grant document access to caregivers
   */
  grantAccess: (id: string, request: GrantAccessRequest): Promise<DocumentOperationResponse> =>
    apiClient.post<DocumentOperationResponse>(`/documents/${id}/permissions`, request),

  /**
   * Revoke document access from a caregiver
   */
  revokeAccess: (documentId: string, caregiverId: string): Promise<DocumentOperationResponse> =>
    apiClient.delete<DocumentOperationResponse>(`/documents/${documentId}/permissions/${caregiverId}`),

  /**
   * Get document access history (grants and revocations)
   */
  getAccessHistory: (documentId: string): Promise<DocumentAccessHistory[]> =>
    apiClient.get<DocumentAccessHistory[]>(`/documents/${documentId}/history`),

  /**
   * Initiate general document upload (for any scope)
   */
  initiateGeneralUpload: (request: UploadDocumentRequest): Promise<DocumentUploadResponse> =>
    apiClient.post<DocumentUploadResponse>('/documents/upload', request),

  /**
   * Full upload flow for general documents (non-client specific)
   */
  uploadGeneral: async (
    file: File,
    documentType: DocumentType,
    scope: DocumentScope,
    options?: {
      description?: string;
      folderId?: string;
      clientId?: string;
      homeId?: string;
    }
  ): Promise<DocumentOperationResponse> => {
    // Step 1: Initiate upload and get SAS URL
    const initResponse = await documentsApi.initiateGeneralUpload({
      fileName: file.name,
      contentType: file.type || 'application/octet-stream',
      fileSizeBytes: file.size,
      documentType,
      scope,
      description: options?.description,
      folderId: options?.folderId,
      clientId: options?.clientId,
      homeId: options?.homeId,
    });

    if (!initResponse.success || !initResponse.uploadUrl || !initResponse.documentId) {
      return {
        success: false,
        error: initResponse.error || 'Failed to initiate upload',
      };
    }

    // Step 2: Upload file directly to Azure Blob Storage
    try {
      await documentsApi.uploadToBlob(initResponse.uploadUrl, file);
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Failed to upload file',
      };
    }

    // Step 3: Confirm upload completed
    return documentsApi.confirmUpload(initResponse.documentId);
  },
};

/**
 * Folders API - Document folder management
 */
export const foldersApi = {
  /**
   * Get folder tree for a specific scope
   */
  getTree: (scope: DocumentScope, clientId?: string, homeId?: string): Promise<FolderTreeNode[]> => {
    const searchParams = new URLSearchParams();
    searchParams.append('scope', scope);
    if (clientId) searchParams.append('clientId', clientId);
    if (homeId) searchParams.append('homeId', homeId);
    return apiClient.get<FolderTreeNode[]>(`/folders/tree?${searchParams.toString()}`);
  },

  /**
   * Get root folders for a specific scope
   */
  getRoots: (scope: DocumentScope, clientId?: string, homeId?: string): Promise<FolderSummary[]> => {
    const searchParams = new URLSearchParams();
    searchParams.append('scope', scope);
    if (clientId) searchParams.append('clientId', clientId);
    if (homeId) searchParams.append('homeId', homeId);
    return apiClient.get<FolderSummary[]>(`/folders/roots?${searchParams.toString()}`);
  },

  /**
   * Browse documents in a folder or at root level
   */
  browse: (params: BrowseDocumentsQuery): Promise<BrowseDocumentsResponse> => {
    const searchParams = new URLSearchParams();
    if (params.scope) searchParams.append('scope', params.scope);
    if (params.folderId) searchParams.append('folderId', params.folderId);
    if (params.clientId) searchParams.append('clientId', params.clientId);
    if (params.homeId) searchParams.append('homeId', params.homeId);
    if (params.pageNumber) searchParams.append('pageNumber', params.pageNumber.toString());
    if (params.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    const query = searchParams.toString();
    return apiClient.get<BrowseDocumentsResponse>(`/folders/browse${query ? `?${query}` : ''}`);
  },

  /**
   * Get folder by ID
   */
  getById: (id: string): Promise<Folder> =>
    apiClient.get<Folder>(`/folders/${id}`),

  /**
   * Get breadcrumbs for a folder
   */
  getBreadcrumbs: (folderId: string): Promise<BreadcrumbItem[]> =>
    apiClient.get<BreadcrumbItem[]>(`/folders/${folderId}/breadcrumbs`),

  /**
   * Create a new folder
   */
  create: (request: CreateFolderRequest): Promise<FolderOperationResponse> =>
    apiClient.post<FolderOperationResponse>('/folders', request),

  /**
   * Update a folder
   */
  update: (id: string, request: UpdateFolderRequest): Promise<FolderOperationResponse> =>
    apiClient.put<FolderOperationResponse>(`/folders/${id}`, request),

  /**
   * Move a folder to a new parent
   */
  move: (id: string, request: MoveFolderRequest): Promise<FolderOperationResponse> =>
    apiClient.put<FolderOperationResponse>(`/folders/${id}/move`, request),

  /**
   * Delete a folder
   */
  delete: (id: string): Promise<FolderOperationResponse> =>
    apiClient.delete<FolderOperationResponse>(`/folders/${id}`),
};

/**
 * Reports API
 */
export const reportsApi = {
  /**
   * Generate client summary report PDF
   * Returns the PDF as a blob for download
   */
  generateClientReport: async (request: GenerateClientReportRequest): Promise<Blob> => {
    const url = `${API_BASE_URL}/reports/client`;
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new ApiError(
        response.status,
        errorData.error || 'Failed to generate client report'
      );
    }

    return response.blob();
  },

  /**
   * Generate home summary report PDF
   * Returns the PDF as a blob for download
   */
  generateHomeReport: async (request: GenerateHomeReportRequest): Promise<Blob> => {
    const url = `${API_BASE_URL}/reports/home`;
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new ApiError(
        response.status,
        errorData.error || 'Failed to generate home report'
      );
    }

    return response.blob();
  },

  /**
   * Helper function to trigger download of a blob
   */
  downloadBlob: (blob: Blob, fileName: string): void => {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  },
};

/**
 * Synthetic Data API (Sysadmin only, dev environment only)
 */
export interface SyntheticDataAvailability {
  isAvailable: boolean;
  message: string;
}

export interface DataStatistics {
  homeCount: number;
  bedCount: number;
  userCount: number;
  clientCount: number;
  activeClientCount: number;
  adlLogCount: number;
  vitalsLogCount: number;
  medicationLogCount: number;
  romLogCount: number;
  behaviorNoteCount: number;
  activityCount: number;
  incidentCount: number;
  documentCount: number;
  appointmentCount: number;
}

export interface LoadSyntheticDataResult {
  success: boolean;
  error?: string;
  homesLoaded: number;
  bedsLoaded: number;
  usersLoaded: number;
  clientsLoaded: number;
  careLogsLoaded: number;
  activitiesLoaded: number;
  incidentsLoaded: number;
  documentsLoaded: number;
  appointmentsLoaded: number;
  duration: string;
}

export interface ClearDataResult {
  success: boolean;
  error?: string;
  recordsDeleted: number;
  details?: string;
}

/**
 * Progress update during synthetic data loading (SSE event).
 */
export interface LoadProgressUpdate {
  phase: string;
  message: string;
  currentStep: number;
  totalSteps: number;
  percentComplete: number;
  itemsProcessed: number;
  isComplete: boolean;
  isError: boolean;
  errorMessage?: string;
}

export const syntheticDataApi = {
  /**
   * Check if synthetic data operations are available
   */
  checkAvailability: (): Promise<SyntheticDataAvailability> =>
    apiClient.get<SyntheticDataAvailability>('/syntheticdata/available'),

  /**
   * Get current database statistics
   */
  getStatistics: (): Promise<DataStatistics> =>
    apiClient.get<DataStatistics>('/syntheticdata/statistics'),

  /**
   * Load synthetic data into the database
   */
  loadData: (): Promise<LoadSyntheticDataResult> =>
    apiClient.post<LoadSyntheticDataResult>('/syntheticdata/load'),

  /**
   * Load synthetic data with real-time progress updates via Server-Sent Events.
   * @param onProgress Callback invoked for each progress update.
   * @param onComplete Callback invoked when loading completes successfully.
   * @param onError Callback invoked if an error occurs.
   * @returns A function to abort the operation.
   */
  loadDataWithProgress: (
    onProgress: (progress: LoadProgressUpdate) => void,
    onComplete: (result: LoadSyntheticDataResult) => void,
    onError: (error: string) => void
  ): (() => void) => {
    const controller = new AbortController();

    const startStream = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/syntheticdata/load-stream`, {
          method: 'GET',
          credentials: 'include',
          signal: controller.signal,
        });

        if (!response.ok) {
          throw new Error(`HTTP error: ${response.status}`);
        }

        const reader = response.body?.getReader();
        if (!reader) {
          throw new Error('No response body');
        }

        const decoder = new TextDecoder();
        let buffer = '';

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });

          // Process complete SSE messages (event + data separated by \n\n)
          const messages = buffer.split('\n\n');
          buffer = messages.pop() || ''; // Keep incomplete message in buffer

          for (const message of messages) {
            if (!message.trim()) continue;

            const lines = message.split('\n');
            let eventType = '';
            let eventData = '';

            for (const line of lines) {
              if (line.startsWith('event: ')) {
                eventType = line.slice(7).trim();
              } else if (line.startsWith('data: ')) {
                eventData = line.slice(6);
              }
            }

            if (eventType && eventData) {
              try {
                const parsed = JSON.parse(eventData);
                
                if (eventType === 'progress') {
                  onProgress(parsed as LoadProgressUpdate);
                } else if (eventType === 'complete') {
                  onComplete(parsed as LoadSyntheticDataResult);
                } else if (eventType === 'error') {
                  onError(parsed.error || 'Unknown error');
                }
              } catch {
                console.warn('Failed to parse SSE data:', eventData);
              }
            }
          }
        }
      } catch (error) {
        if (error instanceof Error && error.name === 'AbortError') {
          return; // User cancelled
        }
        onError(error instanceof Error ? error.message : 'Connection failed');
      }
    };

    startStream();

    return () => controller.abort();
  },

  /**
   * Clear all non-system data from the database
   */
  clearData: (): Promise<ClearDataResult> =>
    apiClient.post<ClearDataResult>('/syntheticdata/clear'),

  /**
   * Clear all data with real-time progress updates via Server-Sent Events.
   * @param onProgress Callback invoked for each progress update.
   * @param onComplete Callback invoked when clearing completes successfully.
   * @param onError Callback invoked if an error occurs.
   * @returns A function to abort the operation.
   */
  clearDataWithProgress: (
    onProgress: (progress: LoadProgressUpdate) => void,
    onComplete: (result: ClearDataResult) => void,
    onError: (error: string) => void
  ): (() => void) => {
    const controller = new AbortController();

    const startStream = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/syntheticdata/clear-stream`, {
          method: 'GET',
          credentials: 'include',
          signal: controller.signal,
        });

        if (!response.ok) {
          throw new Error(`HTTP error: ${response.status}`);
        }

        const reader = response.body?.getReader();
        if (!reader) {
          throw new Error('No response body');
        }

        const decoder = new TextDecoder();
        let buffer = '';

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });

          // Process complete SSE messages
          const messages = buffer.split('\n\n');
          buffer = messages.pop() || '';

          for (const message of messages) {
            if (!message.trim()) continue;

            const lines = message.split('\n');
            let eventType = '';
            let eventData = '';

            for (const line of lines) {
              if (line.startsWith('event: ')) {
                eventType = line.slice(7).trim();
              } else if (line.startsWith('data: ')) {
                eventData = line.slice(6);
              }
            }

            if (eventType && eventData) {
              try {
                const parsed = JSON.parse(eventData);
                
                if (eventType === 'progress') {
                  onProgress(parsed as LoadProgressUpdate);
                } else if (eventType === 'complete') {
                  onComplete(parsed as ClearDataResult);
                } else if (eventType === 'error') {
                  onError(parsed.error || 'Unknown error');
                }
              } catch {
                console.warn('Failed to parse SSE data:', eventData);
              }
            }
          }
        }
      } catch (error) {
        if (error instanceof Error && error.name === 'AbortError') {
          return;
        }
        onError(error instanceof Error ? error.message : 'Connection failed');
      }
    };

    startStream();

    return () => controller.abort();
  },
};

/**
 * Tour API - User tour completion status
 */
export interface TourStatusResponse {
  tourCompleted: boolean;
  completedAt?: string;
}

export interface TourOperationResponse {
  success: boolean;
  error?: string;
}

export const tourApi = {
  /**
   * Get user's tour completion status
   */
  getTourStatus: (): Promise<TourStatusResponse> =>
    apiClient.get<TourStatusResponse>('/users/me/tour-status'),

  /**
   * Mark the tour as completed
   */
  completeTour: (): Promise<TourOperationResponse> =>
    apiClient.post<TourOperationResponse>('/users/me/tour-status/complete'),

  /**
   * Reset the tour status (to allow replaying)
   */
  resetTour: (): Promise<TourOperationResponse> =>
    apiClient.post<TourOperationResponse>('/users/me/tour-status/reset'),
};

export default apiClient;
