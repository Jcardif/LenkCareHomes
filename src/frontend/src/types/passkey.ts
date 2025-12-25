/**
 * Passkey (WebAuthn) types for frontend authentication.
 * These types mirror the backend DTOs in PasskeyModels.cs.
 */

/** Information about a registered passkey */
export interface PasskeyDto {
  /** Unique identifier for the passkey */
  id: string;
  /** User-friendly device name */
  deviceName: string;
  /** When the passkey was registered */
  createdAt: string;
  /** Last time the passkey was used for authentication */
  lastUsedAt: string | null;
  /** Whether the passkey is currently active */
  isActive: boolean;
}

// ============================================================================
// Registration Types
// ============================================================================

/** Request to start passkey registration */
export interface PasskeyRegistrationStartRequest {
  /** User ID (required during initial setup when not authenticated) */
  userId?: string;
  /** Optional custom device name (will be auto-generated if not provided) */
  deviceName?: string;
}

/** Response with WebAuthn options for registration */
export interface PasskeyRegistrationStartResponse {
  /** Whether the request was successful */
  success: boolean;
  /** Session ID to correlate start and complete calls */
  sessionId?: string;
  /** WebAuthn credential creation options as JSON (to pass to navigator.credentials.create) */
  options?: PublicKeyCredentialCreationOptions;
  /** Error message if unsuccessful */
  error?: string;
}

/** Request to complete passkey registration */
export interface PasskeyRegistrationCompleteRequest {
  /** Session ID from the start response */
  sessionId: string;
  /** The attestation response from the authenticator (JSON serialized string) */
  attestationResponse: string;
  /** User-friendly device name for this passkey */
  deviceName: string;
}

/** Response after completing passkey registration */
export interface PasskeyRegistrationCompleteResponse {
  /** Whether registration was successful */
  success: boolean;
  /** ID of the newly registered passkey */
  passkeyId?: string;
  /** Device name of the passkey */
  deviceName?: string;
  /** Error message if unsuccessful */
  error?: string;
}

// ============================================================================
// Authentication Types
// ============================================================================

/** Request to start passkey authentication */
export interface PasskeyAuthenticationStartRequest {
  /** Email address of the user trying to authenticate */
  email: string;
}

/** Response with WebAuthn options for authentication */
export interface PasskeyAuthenticationStartResponse {
  /** Whether the request was successful */
  success: boolean;
  /** Session ID to correlate start and complete calls */
  sessionId?: string;
  /** WebAuthn assertion options as JSON (to pass to navigator.credentials.get) */
  options?: PublicKeyCredentialRequestOptions;
  /** Error message if unsuccessful */
  error?: string;
}

/** Request to complete passkey authentication */
export interface PasskeyAuthenticationCompleteRequest {
  /** Session ID from the start response */
  sessionId: string;
  /** The assertion response from the authenticator (JSON serialized string) */
  assertionResponse: string;
}

/** Response after completing passkey authentication */
export interface PasskeyAuthenticationCompleteResponse {
  /** Whether authentication was successful */
  success: boolean;
  /** User ID if successful */
  userId?: string;
  /** Authentication token if successful */
  token?: string;
  /** User roles if successful */
  roles?: string[];
  /** Device name of the passkey used */
  deviceName?: string;
  /** Error message if unsuccessful */
  error?: string;
}

// ============================================================================
// Management Types
// ============================================================================

/** Response containing list of user's passkeys */
export interface PasskeyListResponse {
  /** List of user's registered passkeys */
  passkeys: PasskeyDto[];
}

/** Request to rename a passkey */
export interface PasskeyRenameRequest {
  /** New device name for the passkey */
  deviceName: string;
}

/** Response after renaming a passkey */
export interface PasskeyRenameResponse {
  /** Whether the rename was successful */
  success: boolean;
  /** Error message if unsuccessful */
  error?: string;
}

/** Response after deleting a passkey */
export interface PasskeyDeleteResponse {
  /** Whether the deletion was successful */
  success: boolean;
  /** Error message if unsuccessful */
  error?: string;
}

// ============================================================================
// MFA Reset Types (Sysadmin only)
// ============================================================================

/** Request to reset a user's MFA (passkeys and backup codes) */
export interface MfaResetRequest {
  /** ID of the user whose MFA is being reset */
  userId: string;
  /** Documented reason for the MFA reset (HIPAA audit requirement) */
  reason: string;
  /** Method used to verify the user's identity before reset */
  verificationMethod: string;
  /** Additional notes about the reset request */
  notes?: string;
}

/** Response after MFA reset operation */
export interface MfaResetResponse {
  /** Whether the reset was successful */
  success: boolean;
  /** Number of passkeys that were removed */
  passkeysRemoved?: number;
  /** Error message if unsuccessful */
  error?: string;
  /** Success message */
  message?: string;
}

// ============================================================================
// WebAuthn Browser API Types (subset needed for our implementation)
// ============================================================================

/**
 * Attestation response from the authenticator during registration.
 * This is the serializable version for sending to the backend.
 */
export interface AuthenticatorAttestationResponse {
  /** The credential ID as base64url */
  id: string;
  /** Raw credential ID as base64url */
  rawId: string;
  /** Type is always 'public-key' */
  type: 'public-key';
  /** Response data from the authenticator */
  response: {
    /** Client data JSON as base64url */
    clientDataJSON: string;
    /** Attestation object as base64url */
    attestationObject: string;
    /** Transports supported by the authenticator */
    transports?: AuthenticatorTransport[];
  };
  /** Authenticator attachment type */
  authenticatorAttachment?: AuthenticatorAttachment;
}

/**
 * Assertion response from the authenticator during authentication.
 * This is the serializable version for sending to the backend.
 */
export interface AuthenticatorAssertionResponse {
  /** The credential ID as base64url */
  id: string;
  /** Raw credential ID as base64url */
  rawId: string;
  /** Type is always 'public-key' */
  type: 'public-key';
  /** Response data from the authenticator */
  response: {
    /** Authenticator data as base64url */
    authenticatorData: string;
    /** Client data JSON as base64url */
    clientDataJSON: string;
    /** Signature as base64url */
    signature: string;
    /** User handle as base64url (may be null) */
    userHandle: string | null;
  };
}

/** Authenticator transport types */
export type AuthenticatorTransport = 'usb' | 'nfc' | 'ble' | 'internal' | 'hybrid';

/** Authenticator attachment types */
export type AuthenticatorAttachment = 'platform' | 'cross-platform';

// ============================================================================
// Utility Types
// ============================================================================

/** Helper to check if WebAuthn is supported in the browser */
export function isWebAuthnSupported(): boolean {
  return (
    typeof window !== 'undefined' &&
    window.PublicKeyCredential !== undefined &&
    typeof window.PublicKeyCredential === 'function'
  );
}

/**
 * Convert ArrayBuffer to base64url string.
 * Used for serializing WebAuthn responses to send to the backend.
 */
export function bufferToBase64url(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  for (let i = 0; i < bytes.byteLength; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return btoa(binary)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
}

/**
 * Convert base64url string to ArrayBuffer.
 * Used for deserializing WebAuthn options from the backend.
 */
export function base64urlToBuffer(base64url: string): ArrayBuffer {
  const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
  const paddedBase64 = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
  const binary = atob(paddedBase64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes.buffer;
}

/**
 * Convert a PublicKeyCredential (registration) to a serializable format.
 */
export function serializeAttestationResponse(
  credential: PublicKeyCredential
): AuthenticatorAttestationResponse {
  const response = credential.response as globalThis.AuthenticatorAttestationResponse;
  
  return {
    id: credential.id,
    rawId: bufferToBase64url(credential.rawId),
    type: 'public-key',
    response: {
      clientDataJSON: bufferToBase64url(response.clientDataJSON),
      attestationObject: bufferToBase64url(response.attestationObject),
      transports: response.getTransports?.() as AuthenticatorTransport[] | undefined,
    },
    authenticatorAttachment: credential.authenticatorAttachment as AuthenticatorAttachment | undefined,
  };
}

/**
 * Convert a PublicKeyCredential (assertion) to a serializable format.
 */
export function serializeAssertionResponse(
  credential: PublicKeyCredential
): AuthenticatorAssertionResponse {
  const response = credential.response as globalThis.AuthenticatorAssertionResponse;
  
  return {
    id: credential.id,
    rawId: bufferToBase64url(credential.rawId),
    type: 'public-key',
    response: {
      authenticatorData: bufferToBase64url(response.authenticatorData),
      clientDataJSON: bufferToBase64url(response.clientDataJSON),
      signature: bufferToBase64url(response.signature),
      userHandle: response.userHandle ? bufferToBase64url(response.userHandle) : null,
    },
  };
}

/**
 * Prepare PublicKeyCredentialCreationOptions for the browser API.
 * Converts base64url strings back to ArrayBuffers where needed.
 */
export function prepareCredentialCreationOptions(
  options: Record<string, unknown>
): PublicKeyCredentialCreationOptions {
  const prepared = { ...options } as unknown as PublicKeyCredentialCreationOptions;
  
  // Convert challenge from base64url to ArrayBuffer
  if (typeof options.challenge === 'string') {
    prepared.challenge = base64urlToBuffer(options.challenge);
  }
  
  // Convert user.id from base64url to ArrayBuffer
  if (options.user && typeof (options.user as Record<string, unknown>).id === 'string') {
    const user = options.user as Record<string, unknown>;
    prepared.user = {
      ...user,
      id: base64urlToBuffer(user.id as string),
    } as PublicKeyCredentialUserEntity;
  }
  
  // Convert excludeCredentials[].id from base64url to ArrayBuffer
  if (Array.isArray(options.excludeCredentials)) {
    prepared.excludeCredentials = (options.excludeCredentials as Record<string, unknown>[]).map(
      (cred) => ({
        ...cred,
        id: typeof cred.id === 'string' ? base64urlToBuffer(cred.id) : cred.id,
      })
    ) as PublicKeyCredentialDescriptor[];
  }
  
  return prepared;
}

/**
 * Prepare PublicKeyCredentialRequestOptions for the browser API.
 * Converts base64url strings back to ArrayBuffers where needed.
 */
export function prepareCredentialRequestOptions(
  options: Record<string, unknown>
): PublicKeyCredentialRequestOptions {
  const prepared = { ...options } as unknown as PublicKeyCredentialRequestOptions;
  
  // Convert challenge from base64url to ArrayBuffer
  if (typeof options.challenge === 'string') {
    prepared.challenge = base64urlToBuffer(options.challenge);
  }
  
  // Convert allowCredentials[].id from base64url to ArrayBuffer
  if (Array.isArray(options.allowCredentials)) {
    prepared.allowCredentials = (options.allowCredentials as Record<string, unknown>[]).map(
      (cred) => ({
        ...cred,
        id: typeof cred.id === 'string' ? base64urlToBuffer(cred.id) : cred.id,
      })
    ) as PublicKeyCredentialDescriptor[];
  }
  
  return prepared;
}
