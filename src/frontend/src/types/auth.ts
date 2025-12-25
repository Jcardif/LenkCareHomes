/** User roles available in the system */
export type UserRole = 'Admin' | 'Caregiver' | 'Sysadmin';

/** Login request payload */
export interface LoginRequest {
  email: string;
  password: string;
}

/** Login response from the API */
export interface LoginResponse {
  success: boolean;
  /** True if user has passkeys and needs to authenticate with one */
  requiresPasskey: boolean;
  /** True if user needs to set up their first passkey */
  requiresPasskeySetup: boolean;
  /** User's email (needed for passkey authentication flow) */
  email?: string;
  userId?: string;
  token?: string;
  roles?: UserRole[];
  /** Temporary token for passkey setup flow */
  passkeySetupToken?: string;
  error?: string;
}

/** Backup code verification request (Sysadmin only) */
export interface BackupCodeVerifyRequest {
  userId: string;
  backupCode: string;
}

/** MFA verify response */
export interface MfaVerifyResponse {
  success: boolean;
  userId?: string;
  token?: string;
  roles?: UserRole[];
  remainingBackupCodes?: number;
  requiresPasskeySetup?: boolean;
  passkeySetupToken?: string;
  error?: string;
}

/** MFA setup response (passkey-based) */
export interface MfaSetupResponse {
  /** User ID for the setup session */
  userId: string;
  /** User's email address */
  email: string;
  /** Backup codes (only provided for Sysadmin users) */
  backupCodes?: string[];
  /** Whether backup codes were generated */
  hasBackupCodes: boolean;
  /** Whether the user has already completed their profile setup */
  hasProfileCompleted: boolean;
}

/** MFA setup confirmation request */
export interface MfaSetupConfirmRequest {
  /** User ID */
  userId: string;
  /** Confirmation that backup codes have been saved (for Sysadmin users) */
  backupCodesSaved?: boolean;
}

/** Password reset request */
export interface PasswordResetRequest {
  email: string;
}

/** Password reset confirmation request */
export interface PasswordResetConfirmRequest {
  token: string;
  newPassword: string;
}

/** Invitation acceptance request */
export interface InvitationAcceptRequest {
  invitationToken: string;
  password: string;
}

/** Invitation acceptance response */
export interface InvitationAcceptResponse {
  success: boolean;
  userId?: string;
  firstName?: string;
  lastName?: string;
  mfaSetup?: MfaSetupResponse;
  passkeySetupToken?: string;
  error?: string;
}

/** User DTO */
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  isMfaSetupComplete: boolean;
  invitationAccepted: boolean;
  roles: UserRole[];
  assignedHomeIds?: string[];
  createdAt: string;
  updatedAt?: string;
  /** Whether the user has completed the onboarding tour */
  tourCompleted?: boolean;
}

/** Invite user request */
export interface InviteUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  homeIds?: string[];
}

/** Invite user response */
export interface InviteUserResponse {
  success: boolean;
  userId?: string;
  error?: string;
}

/** Update user request */
export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
}

/** Update profile request (during setup) */
export interface UpdateProfileRequest {
  userId: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}

/** Complete setup and login response */
export interface CompleteSetupResponse {
  success: boolean;
  userId?: string;
  token?: string;
  roles?: UserRole[];
  user?: User;
  error?: string;
}

/** Current user response */
export interface CurrentUserResponse {
  success: boolean;
  user?: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    fullName: string;
    roles: UserRole[];
  };
  error?: string;
}

/** API error response */
export interface ApiError {
  error: string;
  message?: string;
}

/** Authentication state */
export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  roles: UserRole[];
  isLoading: boolean;
}

/** Audit log entry */
export interface AuditLogEntry {
  id: string;
  partitionKey: string;
  timestamp: string;
  userId?: string;
  userEmail?: string;
  action: string;
  resourceType?: string;
  resourceId?: string;
  outcome: string;
  ipAddress?: string;
  userAgent?: string;
  details?: string;
  httpMethod?: string;
  requestPath?: string;
  statusCode?: number;
  correlationId?: string;
}

/** Audit log response */
export interface AuditLogResponse {
  entries: AuditLogEntry[];
  continuationToken?: string;
}

/** Audit log statistics */
export interface AuditLogStats {
  actionCounts: Record<string, number>;
  since: string;
}

/** Response for regenerating backup codes */
export interface RegenerateBackupCodesResponse {
  success: boolean;
  backupCodes?: string[];
  error?: string;
}
