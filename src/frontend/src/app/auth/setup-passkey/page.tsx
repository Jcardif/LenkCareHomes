'use client';

import React, { useState, useEffect, Suspense, useCallback } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Form, Input, Button, Card, Typography, Flex, Alert, message, Spin, Modal, Grid } from 'antd';

const { useBreakpoint } = Grid;
import { KeyOutlined, CheckCircleOutlined, UserOutlined, SafetyOutlined } from '@ant-design/icons';
import { authApi, passkeyApi, ApiError, getUserFriendlyError } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { BackupCodesStep } from '@/components/auth/BackupCodesStep';
import {
  isWebAuthnSupported,
  prepareCredentialCreationOptions,
  serializeAttestationResponse,
  type AuthenticatorAttachment,
} from '@/types/passkey';
import type { MfaSetupResponse } from '@/types';

const { Title, Text, Paragraph } = Typography;

type SetupStep = 'backup-codes' | 'register-passkey' | 'profile' | 'complete';

interface ProfileFormValues {
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}

function SetupPasskeyContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { completeSetupAndLogin } = useAuth();

  // Responsive breakpoints
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [step, setStep] = useState<SetupStep | null>(null);
  const [mfaSetup, setMfaSetup] = useState<MfaSetupResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [backupCodesCopied, setBackupCodesCopied] = useState(false);
  const [passkeyRegistered, setPasskeyRegistered] = useState(false);
  const [registeredDeviceName, setRegisteredDeviceName] = useState<string | null>(null);
  
  // Naming modal state
  const [namingModalOpen, setNamingModalOpen] = useState(false);
  const [pendingPasskeyName, setPendingPasskeyName] = useState('');
  const [pendingSessionId, setPendingSessionId] = useState<string | null>(null);
  const [pendingAttestationResponse, setPendingAttestationResponse] = useState<string | null>(null);
  const [namingLoading, setNamingLoading] = useState(false);

  const userId = searchParams.get('userId');
  const passkeySetupToken = searchParams.get('passkeySetupToken');
  const isSysadmin = searchParams.get('isSysadmin') === 'true';
  const isReset = searchParams.get('reset') === 'true'; // Passkey reset via backup code
  const [hasProfileCompleted, setHasProfileCompleted] = useState(false);

  // Determine steps based on role, profile status, and reset mode
  const getStepLabels = useCallback(() => {
    // For passkey reset, skip backup codes step
    if (isReset) {
      return hasProfileCompleted 
        ? ['Passkey', 'Done'] 
        : ['Passkey', 'Profile', 'Done'];
    }
    if (isSysadmin) {
      return hasProfileCompleted 
        ? ['Backup', 'Passkey', 'Done'] 
        : ['Backup', 'Passkey', 'Profile', 'Done'];
    }
    return hasProfileCompleted 
      ? ['Passkey', 'Done'] 
      : ['Passkey', 'Profile', 'Done'];
  }, [isSysadmin, hasProfileCompleted, isReset]);

  const getStepKeys = useCallback((): SetupStep[] => {
    // For passkey reset, skip backup codes step
    if (isReset) {
      return hasProfileCompleted 
        ? ['register-passkey', 'complete'] 
        : ['register-passkey', 'profile', 'complete'];
    }
    if (isSysadmin) {
      return hasProfileCompleted 
        ? ['backup-codes', 'register-passkey', 'complete'] 
        : ['backup-codes', 'register-passkey', 'profile', 'complete'];
    }
    return hasProfileCompleted 
      ? ['register-passkey', 'complete'] 
      : ['register-passkey', 'profile', 'complete'];
  }, [isSysadmin, hasProfileCompleted, isReset]);

  const stepLabels = getStepLabels();
  const stepKeys = getStepKeys();

  // Check WebAuthn support
  useEffect(() => {
    if (!isWebAuthnSupported()) {
      setError('Your browser doesn\'t support passkeys. Please use a newer version of Chrome, Safari, Firefox, or Edge.');
    }
  }, []);

  // Initialize MFA setup
  useEffect(() => {
    if (!userId) {
      router.push('/auth/login');
      return;
    }

    if (!passkeySetupToken) {
      setError('Invalid setup link. Please try logging in again.');
      return;
    }

    const fetchMfaSetup = async () => {
      setLoading(true);
      console.log('[SetupPasskey] Fetching MFA setup for userId:', userId);
      try {
        const setup = await authApi.setupMfa(userId, passkeySetupToken);
        console.log('[SetupPasskey] MFA setup response:', setup);
        setMfaSetup(setup);
        setHasProfileCompleted(setup.hasProfileCompleted);
        
        // Determine whether to show backup codes step:
        // - For passkey reset (isReset=true): skip, user already has their remaining codes
        // - If no new backup codes returned (backupCodes is null/empty): skip, nothing to show
        // - Only show backup codes step when NEW codes are generated and need to be saved
        const hasNewBackupCodes = setup.backupCodes && setup.backupCodes.length > 0;
        
        if (isReset || !hasNewBackupCodes) {
          console.log('[SetupPasskey] Skipping to passkey registration (reset or no new backup codes to show)');
          setStep('register-passkey');
        } else {
          console.log('[SetupPasskey] Showing backup codes step (new codes generated)');
          setStep('backup-codes');
        }
      } catch (err) {
        console.error('[SetupPasskey] MFA setup error:', err);
        if (err instanceof ApiError && err.status === 401) {
          message.error('Session expired. Please sign in again.');
          router.push('/auth/login');
          return;
        }
        if (err instanceof ApiError) {
          setError(getUserFriendlyError(err, 'Unable to start security setup. Please try again.'));
        } else {
          setError('Unable to start security setup. Please try again.');
        }
      } finally {
        setLoading(false);
      }
    };

    fetchMfaSetup();
  }, [userId, passkeySetupToken, router, isReset]);

  const handleCopyBackupCodes = () => {
    if (!mfaSetup?.backupCodes) return;
    
    const codesText = mfaSetup.backupCodes.join('\n');
    navigator.clipboard.writeText(codesText);
    setBackupCodesCopied(true);
    message.success('Backup codes copied to clipboard');
  };

  const handleRegisterPasskey = async () => {
    if (!userId) return;

    setLoading(true);
    setError(null);

    console.log('[SetupPasskey] Starting passkey registration for userId:', userId);

    try {
      // Step 1: Start registration - get WebAuthn options from server
      console.log('[SetupPasskey] Step 1: Calling startRegistration API...');
      const startResponse = await passkeyApi.startRegistration({
        userId,
        deviceName: 'New Device', // Placeholder - actual name determined after credential creation
      }, passkeySetupToken || undefined);

      console.log('[SetupPasskey] Step 1 response:', startResponse);

      if (!startResponse.success || !startResponse.options || !startResponse.sessionId) {
        throw new Error(startResponse.error || 'Failed to start passkey registration');
      }

      // Step 2: Convert options for browser API
      console.log('[SetupPasskey] Step 2: Converting options for browser API...');
      const credentialOptions = prepareCredentialCreationOptions(
        startResponse.options as unknown as Record<string, unknown>
      );

      console.log('[SetupPasskey] Step 2 credential options:', credentialOptions);

      // Step 3: Create credential with browser's WebAuthn API
      console.log('[SetupPasskey] Step 3: Creating credential with browser WebAuthn API...');
      console.log('[SetupPasskey] Step 3: rp:', credentialOptions.rp);
      console.log('[SetupPasskey] Step 3: user:', credentialOptions.user);
      console.log('[SetupPasskey] Step 3: challenge type:', typeof credentialOptions.challenge, credentialOptions.challenge instanceof ArrayBuffer);
      console.log('[SetupPasskey] Step 3: pubKeyCredParams:', credentialOptions.pubKeyCredParams);
      console.log('[SetupPasskey] Step 3: authenticatorSelection:', credentialOptions.authenticatorSelection);
      
      // Add timeout detection
      const timeoutPromise = new Promise<null>((_, reject) => {
        setTimeout(() => {
          console.log('[SetupPasskey] Step 3: WebAuthn prompt timed out after 30 seconds');
          reject(new Error('Passkey prompt timed out. Please check if Windows Hello is set up and try again.'));
        }, 30000);
      });
      
      console.log('[SetupPasskey] Step 3: Calling navigator.credentials.create()...');
      console.log('[SetupPasskey] Step 3: Full publicKey options:', JSON.stringify({
        rp: credentialOptions.rp,
        user: {
          ...credentialOptions.user,
          id: '[ArrayBuffer]',
        },
        challenge: '[ArrayBuffer]',
        pubKeyCredParams: credentialOptions.pubKeyCredParams,
        timeout: credentialOptions.timeout,
        excludeCredentials: credentialOptions.excludeCredentials,
        authenticatorSelection: credentialOptions.authenticatorSelection,
        attestation: credentialOptions.attestation,
      }, null, 2));
      
      let credential: PublicKeyCredential | null = null;
      try {
        credential = await Promise.race([
          navigator.credentials.create({
            publicKey: credentialOptions,
          }),
          timeoutPromise,
        ]) as PublicKeyCredential | null;
      } catch (webauthnError) {
        console.error('[SetupPasskey] Step 3: WebAuthn error:', webauthnError);
        console.error('[SetupPasskey] Step 3: Error name:', (webauthnError as Error).name);
        console.error('[SetupPasskey] Step 3: Error message:', (webauthnError as Error).message);
        throw webauthnError;
      }

      console.log('[SetupPasskey] Step 3 credential result:', credential);

      if (!credential) {
        throw new Error('Passkey registration was cancelled');
      }

      // Step 4: Serialize and store for naming modal
      console.log('[SetupPasskey] Step 4: Serializing attestation response...');
      const attestationResponse = serializeAttestationResponse(credential);
      const attestationResponseJson = JSON.stringify(attestationResponse);
      const suggestedName = getDeviceName(credential.authenticatorAttachment as AuthenticatorAttachment | null);
      
      console.log('[SetupPasskey] Step 4: Showing naming modal with suggested name:', suggestedName);
      
      // Store pending data and show naming modal
      setPendingSessionId(startResponse.sessionId);
      setPendingAttestationResponse(attestationResponseJson);
      setPendingPasskeyName(suggestedName);
      setNamingModalOpen(true);

    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        message.error('Session expired. Please sign in again.');
        router.push('/auth/login');
        return;
      }
      if (err instanceof Error && err.name === 'NotAllowedError') {
        setError('Passkey setup was cancelled. Please try again.');
      } else {
        setError(getUserFriendlyError(err, 'Unable to set up passkey. Please try again.'));
      }
    } finally {
      setLoading(false);
    }
  };

  // Complete passkey registration with name
  const handleNamingConfirm = async () => {
    if (!pendingSessionId || !pendingAttestationResponse || !userId) return;

    const deviceName = pendingPasskeyName.trim() || 'Passkey';
    setNamingLoading(true);

    try {
      console.log('[SetupPasskey] Completing registration with device name:', deviceName);
      const completeResponse = await passkeyApi.completeRegistration({
        sessionId: pendingSessionId,
        attestationResponse: pendingAttestationResponse,
        deviceName,
      }, userId, passkeySetupToken || undefined);

      console.log('[SetupPasskey] Complete response:', completeResponse);

      if (!completeResponse.success) {
        throw new Error(completeResponse.error || 'Failed to complete passkey registration');
      }

      // Success!
      console.log('[SetupPasskey] Registration successful!');
      setNamingModalOpen(false);
      setPendingSessionId(null);
      setPendingAttestationResponse(null);
      setPendingPasskeyName('');
      setPasskeyRegistered(true);
      setRegisteredDeviceName(completeResponse.deviceName || deviceName);
      message.success('Passkey registered successfully!');

      // If profile is already complete, confirm MFA and go directly to completion
      if (hasProfileCompleted) {
        console.log('[SetupPasskey] Profile already complete, confirming MFA and skipping to complete');
        // If user already has backup codes (from reset, previous setup, or this session), mark as saved
        // For Sysadmin fresh setup without existing codes, use the backupCodesCopied state
        const userHasBackupCodes = isReset || mfaSetup?.hasBackupCodes;
        const backupCodesConfirmed = userHasBackupCodes ? true : (isSysadmin ? backupCodesCopied : undefined);
        await authApi.confirmMfaSetup({
          userId,
          backupCodesSaved: backupCodesConfirmed,
        }, passkeySetupToken || undefined);
        
        setTimeout(() => {
          setStep('complete');
          // Auto-login after showing success screen
          setTimeout(async () => {
            try {
              const loginSuccess = await completeSetupAndLogin(userId, passkeySetupToken || undefined);
              if (loginSuccess) {
                router.push('/dashboard');
              }
            } catch {
              router.push('/auth/login');
            }
          }, 2000);
        }, 1000);
      } else {
        // Move to profile step after brief delay
        setTimeout(() => setStep('profile'), 1000);
      }
    } catch (err) {
      setError(getUserFriendlyError(err, 'Unable to complete passkey registration. Please try again.'));
      setNamingModalOpen(false);
    } finally {
      setNamingLoading(false);
    }
  };

  // Handle naming modal cancel - still complete with suggested name
  const handleNamingCancel = async () => {
    await handleNamingConfirm();
  };

  const handleProfileSubmit = async (values: ProfileFormValues) => {
    if (!userId) return;

    setLoading(true);
    setError(null);

    try {
      await authApi.updateProfile({
        userId,
        firstName: values.firstName,
        lastName: values.lastName,
        phoneNumber: values.phoneNumber,
      }, passkeySetupToken || undefined);

      // Confirm MFA setup (backup codes saved if Sysadmin)
      // If user already has backup codes from previous setup, mark as saved
      const userHasBackupCodes = mfaSetup?.hasBackupCodes;
      await authApi.confirmMfaSetup({
        userId,
        backupCodesSaved: userHasBackupCodes ? true : (isSysadmin ? backupCodesCopied : undefined),
      }, passkeySetupToken || undefined);
      
      setStep('complete');
      
      // Auto-login after a brief moment to show the success screen
      setTimeout(async () => {
        try {
          const loginSuccess = await completeSetupAndLogin(userId, passkeySetupToken || undefined);
          if (loginSuccess) {
            router.push('/dashboard');
          }
        } catch {
          router.push('/auth/login');
        }
      }, 2000);
    } catch (err) {
      setError(getUserFriendlyError(err, 'Unable to complete setup. Please try again.'));
    } finally {
      setLoading(false);
    }
  };

  // Get a user-friendly device name based on authenticator type and platform
  const getDeviceName = (authenticatorAttachment: AuthenticatorAttachment | null): string => {
    const ua = (navigator as Navigator & { userAgent: string }).userAgent;
    
    // If it's a cross-platform authenticator (security key, password manager, phone)
    if (authenticatorAttachment === 'cross-platform') {
      return 'Password Manager / Security Key';
    }
    
    // Platform authenticator - use device-specific names
    if (ua.includes('iPhone')) return 'iPhone (Face ID / Touch ID)';
    if (ua.includes('iPad')) return 'iPad (Touch ID / Face ID)';
    if (ua.includes('Mac')) return 'Mac (Touch ID)';
    if (ua.includes('Windows')) return 'Windows (Windows Hello)';
    if (ua.includes('Android')) return 'Android Device';
    if (ua.includes('Linux')) return 'Linux PC';
    return 'Security Key';
  };

  const currentStepIndex = step ? stepKeys.indexOf(step) : -1;

  const StepIndicator = () => (
    <Flex justify="center" align="center" style={{ marginBottom: isSmallMobile ? 24 : 40 }} wrap="wrap" gap={isSmallMobile ? 8 : 0}>
      {stepLabels.map((label, index) => {
        const isActive = index === currentStepIndex;
        const isComplete = index < currentStepIndex;
        
        return (
          <React.Fragment key={label}>
            <Flex align="center" gap={isSmallMobile ? 6 : 10}>
              <div
                style={{
                  width: isSmallMobile ? 32 : 40,
                  height: isSmallMobile ? 32 : 40,
                  borderRadius: '50%',
                  background: isComplete || isActive ? '#5a7a6b' : '#e0e4e3',
                  color: isComplete || isActive ? '#fff' : '#6b7770',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: isSmallMobile ? 14 : 16,
                  fontWeight: 500,
                  flexShrink: 0,
                }}
              >
                {isComplete ? '✓' : index + 1}
              </div>
              {!isSmallMobile && (
                <Text 
                  style={{ 
                    color: isActive ? '#2d3732' : '#9ca5a0',
                    fontWeight: isActive ? 600 : 400,
                    fontSize: 15,
                    whiteSpace: 'nowrap',
                  }}
                >
                  {label}
                </Text>
              )}
            </Flex>
            {index < stepLabels.length - 1 && (
              <div 
                style={{ 
                  width: isSmallMobile ? 24 : 48, 
                  height: 2, 
                  background: index < currentStepIndex ? '#5a7a6b' : '#e0e4e3',
                  margin: isSmallMobile ? '0 4px' : '0 12px',
                  flexShrink: 0,
                }} 
              />
            )}
          </React.Fragment>
        );
      })}
    </Flex>
  );

  const renderBackupCodesStep = () => (
    <BackupCodesStep
      backupCodes={mfaSetup?.backupCodes || []}
      copied={backupCodesCopied}
      onCopy={handleCopyBackupCodes}
      onContinue={() => setStep('register-passkey')}
      continueText="Continue to Passkey Setup"
      isSmallMobile={isSmallMobile}
    />
  );

  const renderRegisterPasskeyStep = () => (
    <div>
      <div style={{ textAlign: 'center', marginBottom: 28 }}>
        <Title level={3} style={{ color: '#2d3732', marginBottom: 12 }}>Register Your Passkey</Title>
        <Paragraph style={{ color: '#6b7770', margin: 0, fontSize: 16 }}>
          Passkeys are a secure, passwordless way to sign in. 
          Use your fingerprint, face, or device PIN to authenticate.
        </Paragraph>
      </div>
      
      {!passkeyRegistered ? (
        <>
          <div style={{ 
            background: '#f0f9f4',
            border: '1px solid #86efac',
            borderRadius: 10, 
            padding: 24,
            marginBottom: 24,
            textAlign: 'center',
          }}>
            <KeyOutlined style={{ fontSize: 48, color: '#5a7a6b', marginBottom: 16 }} />
            <div>
              <Text style={{ color: '#166534', fontSize: 15, display: 'block', marginBottom: 8 }}>
                <strong>Why passkeys?</strong>
              </Text>
              <Text style={{ color: '#15803d', fontSize: 14 }}>
                Instead of typing codes from an app, you simply use your fingerprint, face, or device PIN to sign in.
              </Text>
            </div>
          </div>

          <Flex vertical gap={14}>
            <Button
              type="primary"
              icon={<KeyOutlined />}
              onClick={handleRegisterPasskey}
              block
              size="large"
              loading={loading}
              style={{ borderRadius: 10, height: 56, fontSize: 16 }}
            >
              Register Passkey
            </Button>
            
            {isSysadmin && !isReset && (
              <Button 
                type="text" 
                onClick={() => setStep('backup-codes')} 
                block
                style={{ color: '#6b7770', fontSize: 15 }}
              >
                ← Back to Backup Codes
              </Button>
            )}
          </Flex>
        </>
      ) : (
        <div style={{ textAlign: 'center', padding: '20px 0' }}>
          <div style={{ 
            width: 72, 
            height: 72, 
            borderRadius: 18, 
            background: 'rgba(90, 122, 107, 0.1)', 
            display: 'flex', 
            alignItems: 'center', 
            justifyContent: 'center',
            margin: '0 auto 20px'
          }}>
            <CheckCircleOutlined style={{ fontSize: 36, color: '#5a7a6b' }} />
          </div>
          <Title level={4} style={{ color: '#2d3732', marginBottom: 8 }}>Passkey Registered!</Title>
          <Paragraph style={{ color: '#6b7770', marginBottom: 0, fontSize: 15 }}>
            Device: {registeredDeviceName}
          </Paragraph>
        </div>
      )}
    </div>
  );

  const renderProfileStep = () => (
    <div>
      <div style={{ textAlign: 'center', marginBottom: 28 }}>
        <div style={{ 
          width: 72, 
          height: 72, 
          borderRadius: 18, 
          background: 'rgba(90, 122, 107, 0.1)', 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          margin: '0 auto 20px'
        }}>
          <UserOutlined style={{ fontSize: 36, color: '#5a7a6b' }} />
        </div>
        <Title level={2} style={{ color: '#2d3732', marginBottom: 12 }}>Complete Your Profile</Title>
        <Paragraph style={{ color: '#6b7770', margin: 0, fontSize: 16 }}>
          Tell us a bit about yourself to personalize your experience.
        </Paragraph>
      </div>

      <Form 
        onFinish={handleProfileSubmit} 
        layout="vertical"
        requiredMark={false}
        size="large"
      >
        <Form.Item
          name="firstName"
          label={<Text style={{ color: '#2d3732', fontSize: 15 }}>First Name</Text>}
          rules={[{ required: true, message: 'Please enter your first name' }]}
        >
          <Input 
            placeholder="John" 
            style={{ borderRadius: 10, height: 52, fontSize: 16 }}
          />
        </Form.Item>

        <Form.Item
          name="lastName"
          label={<Text style={{ color: '#2d3732', fontSize: 15 }}>Last Name</Text>}
          rules={[{ required: true, message: 'Please enter your last name' }]}
        >
          <Input 
            placeholder="Doe" 
            style={{ borderRadius: 10, height: 52, fontSize: 16 }}
          />
        </Form.Item>

        <Form.Item
          name="phoneNumber"
          label={<Text style={{ color: '#2d3732', fontSize: 15 }}>Phone Number (Optional)</Text>}
        >
          <Input 
            placeholder="+1 (555) 123-4567" 
            style={{ borderRadius: 10, height: 52, fontSize: 16 }}
          />
        </Form.Item>

        <Flex vertical gap={14} style={{ marginTop: 12 }}>
          <Button 
            type="primary" 
            htmlType="submit" 
            block 
            loading={loading}
            style={{ borderRadius: 10, height: 52, fontSize: 16 }}
          >
            Complete Setup
          </Button>
        </Flex>
      </Form>
    </div>
  );

  const renderCompleteStep = () => (
    <div style={{ textAlign: 'center', padding: '20px 0' }}>
      <div style={{ 
        width: 88, 
        height: 88, 
        borderRadius: 22, 
        background: 'rgba(90, 122, 107, 0.1)', 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        margin: '0 auto 28px'
      }}>
        <CheckCircleOutlined style={{ fontSize: 48, color: '#5a7a6b' }} />
      </div>
      <Title level={2} style={{ color: '#2d3732', marginBottom: 12 }}>You&apos;re All Set!</Title>
      <Paragraph style={{ color: '#6b7770', marginBottom: 24, fontSize: 16 }}>
        Your account is now protected with passkey authentication and your profile is complete.
      </Paragraph>
      <Flex align="center" justify="center" gap={12}>
        <Spin size="small" />
        <Text style={{ color: '#5a7a6b', fontSize: 15 }}>Signing you in...</Text>
      </Flex>
    </div>
  );

  if (loading && !mfaSetup) {
    return (
      <div style={{ 
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: '#f8f9fa',
      }}>
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: '#f8f9fa',
        padding: isSmallMobile ? 16 : 24,
      }}
    >
      <Card style={{ 
        width: '100%', 
        maxWidth: 640, 
        boxShadow: '0 4px 24px rgba(45, 55, 50, 0.1)',
        borderRadius: 16,
        border: '1px solid #ebeeed',
        padding: isSmallMobile ? '12px 16px' : '16px 24px',
      }}>
        {/* Header - show on passkey registration step */}
        {step === 'register-passkey' && !passkeyRegistered && (
          <div style={{ textAlign: 'center', marginBottom: 12 }}>
            <div style={{ 
              width: 72, 
              height: 72, 
              borderRadius: 18, 
              background: 'rgba(90, 122, 107, 0.1)', 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center',
              margin: '0 auto 20px'
            }}>
              <SafetyOutlined style={{ fontSize: 36, color: '#5a7a6b' }} />
            </div>
            <Title level={2} style={{ color: '#2d3732', marginBottom: 8 }}>
              Secure Your Account
            </Title>
            <Paragraph style={{ color: '#6b7770', marginBottom: 0, fontSize: 16 }}>
              Set up passwordless authentication with a passkey
            </Paragraph>
          </div>
        )}

        {/* Step Indicator - hide on complete */}
        {step && step !== 'complete' && <StepIndicator />}

        {/* Error Alert */}
        {error && (
          <Alert
            title={error}
            type="error"
            showIcon
            closable
            onClose={() => setError(null)}
            style={{ marginBottom: 24, borderRadius: 8 }}
          />
        )}

        {/* Step Content */}
        {step === 'backup-codes' && renderBackupCodesStep()}
        {step === 'register-passkey' && renderRegisterPasskeyStep()}
        {step === 'profile' && renderProfileStep()}
        {step === 'complete' && renderCompleteStep()}
        {!step && !loading && !error && (
          <div style={{ textAlign: 'center', padding: 20 }}>
            <Spin />
            <Text style={{ display: 'block', marginTop: 10 }}>Initializing setup...</Text>
          </div>
        )}

        {/* Naming Modal */}
        <Modal
          title="Name Your Passkey"
          open={namingModalOpen}
          onOk={handleNamingConfirm}
          onCancel={handleNamingCancel}
          okText="Save"
          cancelText="Use Suggested Name"
          confirmLoading={namingLoading}
          closable={false}
          maskClosable={false}
          width={isMobile ? '100%' : 420}
          style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
        >
          <Paragraph style={{ marginBottom: 16 }}>
            Give your passkey a memorable name to identify it later.
          </Paragraph>
          <Input
            value={pendingPasskeyName}
            onChange={(e) => setPendingPasskeyName(e.target.value)}
            placeholder="e.g., MacBook Touch ID, Windows Hello"
            maxLength={50}
            onPressEnter={handleNamingConfirm}
          />
        </Modal>
      </Card>
    </div>
  );
}

export default function SetupPasskeyPage() {
  return (
    <Suspense fallback={
      <div style={{ 
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: '#f8f9fa',
      }}>
        <Spin size="large" />
      </div>
    }>
      <SetupPasskeyContent />
    </Suspense>
  );
}
