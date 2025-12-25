'use client';

import React, { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import { message } from 'antd';
import { authApi, ApiError } from '@/lib/api';
import type {
  AuthState,
  User,
  UserRole,
  LoginRequest,
  BackupCodeVerifyRequest,
  MfaSetupResponse,
  InvitationAcceptRequest,
  UpdateProfileRequest,
} from '@/types';

interface AuthContextType extends AuthState {
  login: (request: LoginRequest) => Promise<{ requiresPasskey: boolean; requiresPasskeySetup: boolean; userId?: string; email?: string; passkeySetupToken?: string }>;
  verifyBackupCode: (request: BackupCodeVerifyRequest) => Promise<{ success: boolean; remainingCodes?: number }>;
  setupMfa: (userId: string, passkeySetupToken?: string) => Promise<MfaSetupResponse>;
  confirmMfaSetup: (userId: string, backupCodesSaved: boolean, passkeySetupToken?: string) => Promise<boolean>;
  updateProfile: (request: UpdateProfileRequest, passkeySetupToken?: string) => Promise<boolean>;
  completeSetupAndLogin: (userId: string, passkeySetupToken?: string) => Promise<boolean>;
  logout: () => Promise<void>;
  acceptInvitation: (request: InvitationAcceptRequest) => Promise<{ success: boolean; userId?: string; firstName?: string; lastName?: string; mfaSetup?: MfaSetupResponse; passkeySetupToken?: string }>;
  hasRole: (role: UserRole) => boolean;
  hasAnyRole: (roles: UserRole[]) => boolean;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const AUTH_STORAGE_KEY = 'lenkcare_auth';

interface StoredAuth {
  user: User | null;
  roles: UserRole[];
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    isAuthenticated: false,
    user: null,
    roles: [],
    isLoading: true,
  });

  // Load auth state from storage on mount and refresh user details
  useEffect(() => {
    const loadAuth = async () => {
      const stored = localStorage.getItem(AUTH_STORAGE_KEY);
      if (stored) {
        try {
          const parsed: StoredAuth = JSON.parse(stored);
          
          // If we have stored auth, try to refresh user details from backend
          if (parsed.user) {
            setState({
              isAuthenticated: true,
              user: parsed.user,
              roles: parsed.roles || [],
              isLoading: true,
            });
            
            // Try to get fresh user details from the server
            try {
              const response = await authApi.getCurrentUser();
              if (response.success && response.user) {
                const freshUser: User = {
                  id: response.user.id,
                  email: response.user.email,
                  firstName: response.user.firstName,
                  lastName: response.user.lastName,
                  fullName: response.user.fullName,
                  isActive: true,
                  isMfaSetupComplete: true,
                  invitationAccepted: true,
                  roles: response.user.roles,
                  createdAt: parsed.user.createdAt || new Date().toISOString(),
                };
                localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify({ user: freshUser, roles: freshUser.roles }));
                setState({
                  isAuthenticated: true,
                  user: freshUser,
                  roles: freshUser.roles,
                  isLoading: false,
                });
                return;
              }
            } catch {
              // If refresh fails (401, network error), clear auth state
              localStorage.removeItem(AUTH_STORAGE_KEY);
              setState({
                isAuthenticated: false,
                user: null,
                roles: [],
                isLoading: false,
              });
              return;
            }
          }
          
          setState({
            isAuthenticated: !!parsed.user,
            user: parsed.user,
            roles: parsed.roles || [],
            isLoading: false,
          });
        } catch {
          localStorage.removeItem(AUTH_STORAGE_KEY);
          setState(prev => ({ ...prev, isLoading: false }));
        }
      } else {
        setState(prev => ({ ...prev, isLoading: false }));
      }
    };
    
    loadAuth();
  }, []);

  const saveAuth = useCallback((user: User | null, roles: UserRole[]) => {
    if (user) {
      localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify({ user, roles }));
    } else {
      localStorage.removeItem(AUTH_STORAGE_KEY);
    }
    setState({
      isAuthenticated: !!user,
      user,
      roles,
      isLoading: false,
    });
  }, []);

  // Fetch current user details from the backend
  const fetchCurrentUser = useCallback(async (): Promise<User | null> => {
    try {
      const response = await authApi.getCurrentUser();
      if (response.success && response.user) {
        return {
          id: response.user.id,
          email: response.user.email,
          firstName: response.user.firstName,
          lastName: response.user.lastName,
          fullName: response.user.fullName,
          isActive: true,
          isMfaSetupComplete: true,
          invitationAccepted: true,
          roles: response.user.roles,
          createdAt: new Date().toISOString(),
        };
      }
      return null;
    } catch {
      return null;
    }
  }, []);

  const refreshUser = useCallback(async () => {
    const user = await fetchCurrentUser();
    if (user) {
      saveAuth(user, user.roles);
    }
  }, [fetchCurrentUser, saveAuth]);

  const login = useCallback(async (request: LoginRequest) => {
    try {
      const response = await authApi.login(request);
      
      if (!response.success) {
        throw new Error(response.error || 'Login failed');
      }

      if (response.requiresPasskeySetup) {
        return {
          requiresPasskey: false,
          requiresPasskeySetup: true,
          userId: response.userId,
          email: request.email,
          passkeySetupToken: response.passkeySetupToken,
        };
      }

      if (response.requiresPasskey) {
        return {
          requiresPasskey: true,
          requiresPasskeySetup: false,
          userId: response.userId,
          email: request.email,
        };
      }

      // Direct login (shouldn't happen with passkey requirement)
      const user: User = {
        id: response.userId!,
        email: request.email,
        firstName: '',
        lastName: '',
        fullName: '',
        isActive: true,
        isMfaSetupComplete: true,
        invitationAccepted: true,
        roles: response.roles || [],
        createdAt: new Date().toISOString(),
      };

      saveAuth(user, response.roles || []);
      return { requiresPasskey: false, requiresPasskeySetup: false };
    } catch (error) {
      const errorMessage = error instanceof ApiError ? error.message : 'Login failed';
      message.error(errorMessage);
      throw error;
    }
  }, [saveAuth]);

  const verifyBackupCode = useCallback(async (request: BackupCodeVerifyRequest) => {
    try {
      const response = await authApi.verifyBackupCode(request);
      
      if (!response.success) {
        throw new Error(response.error || 'Backup code verification failed');
      }

      // Fetch full user details from the backend
      const user = await fetchCurrentUser();
      if (user) {
        saveAuth(user, user.roles);
      } else {
        // Fallback if fetching user details fails
        const fallbackUser: User = {
          id: response.userId!,
          email: '',
          firstName: '',
          lastName: '',
          fullName: '',
          isActive: true,
          isMfaSetupComplete: true,
          invitationAccepted: true,
          roles: response.roles || [],
          createdAt: new Date().toISOString(),
        };
        saveAuth(fallbackUser, response.roles || []);
      }
      
      if (response.remainingBackupCodes !== undefined && response.remainingBackupCodes <= 3) {
        message.warning(`You have ${response.remainingBackupCodes} backup codes remaining. Consider regenerating your backup codes.`);
      }

      return { success: true, remainingCodes: response.remainingBackupCodes };
    } catch (error) {
      const errorMessage = error instanceof ApiError ? error.message : 'Backup code verification failed';
      message.error(errorMessage);
      return { success: false };
    }
  }, [fetchCurrentUser, saveAuth]);

  const setupMfa = useCallback(async (userId: string, passkeySetupToken?: string) => {
    const response = await authApi.setupMfa(userId, passkeySetupToken);
    return response;
  }, []);

  const confirmMfaSetup = useCallback(async (userId: string, backupCodesSaved: boolean, passkeySetupToken?: string) => {
    try {
      await authApi.confirmMfaSetup({ userId, backupCodesSaved }, passkeySetupToken);
      return true;
    } catch (error) {
      const errorMessage = error instanceof ApiError ? error.message : 'MFA setup confirmation failed';
      message.error(errorMessage);
      return false;
    }
  }, []);

  const updateProfile = useCallback(async (request: UpdateProfileRequest, passkeySetupToken?: string) => {
    try {
      await authApi.updateProfile(request, passkeySetupToken);
      return true;
    } catch (error) {
      const errorMessage = error instanceof ApiError ? error.message : 'Failed to update profile';
      message.error(errorMessage);
      return false;
    }
  }, []);

  const completeSetupAndLogin = useCallback(async (userId: string, passkeySetupToken?: string) => {
    try {
      const response = await authApi.completeSetupAndLogin(userId, passkeySetupToken);
      
      if (!response.success) {
        throw new Error(response.error || 'Failed to complete setup');
      }

      // Try to fetch full user details, or use response data
      const fetchedUser = await fetchCurrentUser();
      const user: User = fetchedUser || response.user || {
        id: response.userId || userId,
        email: '',
        firstName: '',
        lastName: '',
        fullName: '',
        isActive: true,
        isMfaSetupComplete: true,
        invitationAccepted: true,
        roles: response.roles || [],
        createdAt: new Date().toISOString(),
      };

      saveAuth(user, user.roles);
      message.success('Setup complete! Welcome to LenkCare Homes.');
      return true;
    } catch (error) {
      const errorMessage = error instanceof ApiError ? error.message : 'Failed to complete setup';
      message.error(errorMessage);
      return false;
    }
  }, [fetchCurrentUser, saveAuth]);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignore logout errors
    }
    saveAuth(null, []);
    message.success('Logged out successfully');
  }, [saveAuth]);

  const acceptInvitation = useCallback(async (request: InvitationAcceptRequest) => {
    try {
      const response = await authApi.acceptInvitation(request);
      
      if (!response.success) {
        throw new Error(response.error || 'Failed to accept invitation');
      }

      return {
        success: true,
        userId: response.userId,
        firstName: response.firstName,
        lastName: response.lastName,
        mfaSetup: response.mfaSetup,
        passkeySetupToken: response.passkeySetupToken,
      };
    } catch (error) {
      const errorMessage = error instanceof ApiError ? error.message : 'Failed to accept invitation';
      message.error(errorMessage);
      return { success: false };
    }
  }, []);

  const hasRole = useCallback((role: UserRole) => {
    return state.roles.includes(role);
  }, [state.roles]);

  const hasAnyRole = useCallback((roles: UserRole[]) => {
    return roles.some(role => state.roles.includes(role));
  }, [state.roles]);

  return (
    <AuthContext.Provider
      value={{
        ...state,
        login,
        verifyBackupCode,
        setupMfa,
        confirmMfaSetup,
        updateProfile,
        completeSetupAndLogin,
        logout,
        acceptInvitation,
        hasRole,
        hasAnyRole,
        refreshUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
