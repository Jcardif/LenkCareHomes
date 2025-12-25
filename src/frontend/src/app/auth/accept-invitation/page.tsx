'use client';

import React, { useState, useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Form, Input, Button, Card, Typography, Alert, Flex, Spin, Modal, message, Grid } from 'antd';

const { useBreakpoint } = Grid;
import { LockOutlined, UserOutlined, KeyOutlined, CheckCircleOutlined } from '@ant-design/icons';
import { useAuth } from '@/contexts/AuthContext';
import { passkeyApi, getUserFriendlyError } from '@/lib/api';
import { BackupCodesStep } from '@/components/auth/BackupCodesStep';
import {
  isWebAuthnSupported,
  prepareCredentialCreationOptions,
  serializeAttestationResponse,
  type AuthenticatorAttachment,
} from '@/types/passkey';
import type { MfaSetupResponse } from '@/types';

const { Title, Text, Paragraph } = Typography;

type SetupStep = 'password' | 'backup-codes' | 'register-passkey' | 'profile' | 'complete';

interface ProfileFormValues {
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}

function AcceptInvitationContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { acceptInvitation, confirmMfaSetup, updateProfile, completeSetupAndLogin } = useAuth();

  // Responsive breakpoints
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [step, setStep] = useState<SetupStep>('password');
  const [mfaSetup, setMfaSetup] = useState<MfaSetupResponse | null>(null);
  const [userId, setUserId] = useState<string | null>(null);
  const [passkeySetupToken, setPasskeySetupToken] = useState<string | undefined>(undefined);
  const [invitedFirstName, setInvitedFirstName] = useState<string>('');
  const [invitedLastName, setInvitedLastName] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [backupCodesCopied, setBackupCodesCopied] = useState(false);
  const [isSysadmin, setIsSysadmin] = useState(false);

  // Naming modal state
  const [namingModalOpen, setNamingModalOpen] = useState(false);
  const [pendingPasskeyName, setPendingPasskeyName] = useState('');
  const [pendingSessionId, setPendingSessionId] = useState<string | null>(null);
  const [pendingAttestationResponse, setPendingAttestationResponse] = useState<string | null>(null);
  const [namingLoading, setNamingLoading] = useState(false);
  
  // WebAuthn support state - initialize as null to avoid hydration mismatch
  const [webAuthnSupported, setWebAuthnSupported] = useState<boolean | null>(null);

  const invitationToken = searchParams.get('token');

  // Determine step labels based on role
  const stepLabels = isSysadmin 
    ? ['Password', 'Backup Codes', 'Passkey', 'Profile', 'Done']
    : ['Password', 'Passkey', 'Profile', 'Done'];
  
  const stepKeys: SetupStep[] = isSysadmin 
    ? ['password', 'backup-codes', 'register-passkey', 'profile', 'complete']
    : ['password', 'register-passkey', 'profile', 'complete'];
  
  const currentStepIndex = stepKeys.indexOf(step);

  // Check WebAuthn support only on client side after hydration
  useEffect(() => {
    const supported = isWebAuthnSupported();
    setWebAuthnSupported(supported);
    if (!supported) {
      setError('Your browser doesn\'t support passkeys. Please use a newer version of Chrome, Safari, Firefox, or Edge.');
    }
  }, []);

  const StepIndicator = () => (
    <Flex 
      justify="center" 
      align="center" 
      style={{ 
        marginBottom: isSmallMobile ? 24 : 40,
        flexWrap: 'nowrap',
        overflow: 'hidden',
      }}
    >
      {stepLabels.map((label, index) => {
        const isActive = index === currentStepIndex;
        const isComplete = index < currentStepIndex;
        
        return (
          <React.Fragment key={label}>
            <Flex align="center" gap={isSmallMobile ? 4 : 8} style={{ flexShrink: 0 }}>
              <div
                style={{
                  width: isSmallMobile ? 28 : 32,
                  height: isSmallMobile ? 28 : 32,
                  borderRadius: '50%',
                  background: isComplete || isActive ? '#5a7a6b' : '#e0e4e3',
                  color: isComplete || isActive ? '#fff' : '#6b7770',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: isSmallMobile ? 12 : 14,
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
                    fontSize: 13,
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
                  width: isSmallMobile ? 16 : 32, 
                  height: 2, 
                  background: index < currentStepIndex ? '#5a7a6b' : '#e0e4e3',
                  margin: isSmallMobile ? '0 4px' : '0 8px',
                  flexShrink: 0,
                }} 
              />
            )}
          </React.Fragment>
        );
      })}
    </Flex>
  );

  const handlePasswordSubmit = async (values: { password: string; confirmPassword: string }) => {
    if (!invitationToken) {
      setError('Invalid invitation link');
      return;
    }

    if (values.password !== values.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await acceptInvitation({
        invitationToken,
        password: values.password,
      });

      if (result.success && result.mfaSetup) {
        setMfaSetup(result.mfaSetup);
        setUserId(result.userId || null);
        setPasskeySetupToken(result.passkeySetupToken);
        setInvitedFirstName(result.firstName || '');
        setInvitedLastName(result.lastName || '');
        
        // Check if user is Sysadmin (has backup codes)
        const userIsSysadmin = result.mfaSetup.hasBackupCodes;
        setIsSysadmin(userIsSysadmin);
        
        // Navigate to appropriate next step
        if (userIsSysadmin) {
          setStep('backup-codes');
        } else {
          setStep('register-passkey');
        }
      } else {
        setError('Unable to accept invitation. Please try again or contact your administrator.');
      }
    } catch (err) {
      setError(getUserFriendlyError(err, 'Unable to accept invitation. Please try again.'));
    } finally {
      setLoading(false);
    }
  };

  const handleBackupCodesContinue = () => {
    // Just move to passkey step - MFA confirmation happens after passkey is registered
    setStep('register-passkey');
  };

  const handleCopyBackupCodes = () => {
    if (mfaSetup?.backupCodes) {
      navigator.clipboard.writeText(mfaSetup.backupCodes.join('\n'));
      setBackupCodesCopied(true);
    }
  };

  const handleRegisterPasskey = async () => {
    if (!userId) {
      setError('Session expired. Please start over.');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      // Detect device type for naming (used as initial request only)
      const ua = navigator.userAgent.toLowerCase();
      let deviceType = 'Desktop';
      if (/(tablet|ipad|playbook|silk)|(android(?!.*mobi))/i.test(ua)) {
        deviceType = 'Tablet';
      } else if (/mobile|android|iphone|ipod|blackberry|iemobile|opera mini/i.test(ua)) {
        deviceType = 'Mobile';
      }
      const initialDeviceName = `${deviceType} - ${new Date().toLocaleDateString()}`;

      // Step 1: Start passkey registration
      const startResponse = await passkeyApi.startRegistration({
        userId,
        deviceName: initialDeviceName,
      }, passkeySetupToken);
      
      if (!startResponse.success || !startResponse.options || !startResponse.sessionId) {
        throw new Error(startResponse.error || 'Failed to start passkey registration');
      }

      // Step 2: Convert options for browser API
      const creationOptions = prepareCredentialCreationOptions(
        startResponse.options as unknown as Record<string, unknown>
      );

      // Step 3: Create credential with authenticator
      const credential = await navigator.credentials.create({
        publicKey: creationOptions,
      }) as PublicKeyCredential | null;

      if (!credential) {
        throw new Error('Passkey creation was cancelled');
      }

      // Step 4: Serialize response and prepare for naming modal
      const attestationResponse = serializeAttestationResponse(credential);
      const attestationResponseJson = JSON.stringify(attestationResponse);
      
      // Generate suggested device name based on authenticator type
      const suggestedName = getDeviceName(credential.authenticatorAttachment as AuthenticatorAttachment | null);

      // Store pending registration data and show naming modal
      setPendingSessionId(startResponse.sessionId);
      setPendingAttestationResponse(attestationResponseJson);
      setPendingPasskeyName(suggestedName);
      setNamingModalOpen(true);

    } catch (err) {
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
      const completeResponse = await passkeyApi.completeRegistration({
        sessionId: pendingSessionId,
        attestationResponse: pendingAttestationResponse,
        deviceName,
      }, userId, passkeySetupToken);

      if (!completeResponse.success) {
        throw new Error(completeResponse.error || 'Failed to complete passkey registration');
      }

      // Success - passkey registered!
      setNamingModalOpen(false);
      setPendingSessionId(null);
      setPendingAttestationResponse(null);
      setPendingPasskeyName('');
      message.success('Passkey registered successfully!');

      // Confirm MFA setup (for Sysadmins, pass backupCodesSaved: true since they already copied codes)
      const mfaConfirmed = await confirmMfaSetup(userId, isSysadmin && backupCodesCopied, passkeySetupToken);
      if (!mfaConfirmed) {
        setError('Unable to complete MFA setup. Please try again.');
        return;
      }

      // Move to profile step
      setStep('profile');
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

  // Get a user-friendly device name based on authenticator type and platform
  const getDeviceName = (authenticatorAttachment: AuthenticatorAttachment | null): string => {
    const ua = navigator.userAgent;
    
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

  const handleProfileSubmit = async (values: ProfileFormValues) => {
    if (!userId) return;

    setLoading(true);
    setError(null);

    try {
      const success = await updateProfile({
        userId,
        firstName: values.firstName,
        lastName: values.lastName,
        phoneNumber: values.phoneNumber,
      }, passkeySetupToken);
      
      if (success) {
        setStep('complete');
        // Auto-login after a brief moment to show the success screen
        setTimeout(async () => {
          const loginSuccess = await completeSetupAndLogin(userId, passkeySetupToken);
          if (loginSuccess) {
            router.push('/dashboard');
          }
        }, 2000);
      } else {
        setError('Unable to save profile. Please try again.');
      }
    } catch {
      setError('Unable to save profile. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const renderPasswordStep = () => (
    <Form onFinish={handlePasswordSubmit} layout="vertical" size="large" requiredMark={false}>
      <div style={{ textAlign: 'center', marginBottom: 28 }}>
        <Title level={3} style={{ color: '#2d3732', marginBottom: 12 }}>Create Your Password</Title>
        <Paragraph style={{ color: '#6b7770', margin: 0, fontSize: 16 }}>
          Password must be at least 8 characters with uppercase, lowercase, numbers, and special characters.
        </Paragraph>
      </div>

      <Form.Item
        name="password"
        label={<Text style={{ color: '#2d3732', fontSize: 15 }}>Password</Text>}
        rules={[
          { required: true, message: 'Please enter a password' },
          { min: 8, message: 'Password must be at least 8 characters' },
          {
            pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
            message: 'Password must include uppercase, lowercase, number, and special character',
          },
        ]}
      >
        <Input.Password 
          prefix={<LockOutlined style={{ color: '#9ca5a0' }} />} 
          placeholder="Enter password" 
          style={{ borderRadius: 10, height: 52, fontSize: 16 }}
        />
      </Form.Item>

      <Form.Item
        name="confirmPassword"
        label={<Text style={{ color: '#2d3732', fontSize: 15 }}>Confirm Password</Text>}
        dependencies={['password']}
        rules={[
          { required: true, message: 'Please confirm your password' },
          ({ getFieldValue }) => ({
            validator(_, value) {
              if (!value || getFieldValue('password') === value) {
                return Promise.resolve();
              }
              return Promise.reject(new Error('Passwords do not match'));
            },
          }),
        ]}
      >
        <Input.Password 
          prefix={<LockOutlined style={{ color: '#9ca5a0' }} />} 
          placeholder="Confirm password" 
          style={{ borderRadius: 10, height: 52, fontSize: 16 }}
        />
      </Form.Item>

      <Form.Item style={{ marginBottom: 0 }}>
        <Button type="primary" htmlType="submit" block loading={loading || webAuthnSupported === null} disabled={webAuthnSupported === false} style={{ borderRadius: 10, height: 52, fontSize: 16 }}>
          Continue to Security Setup
        </Button>
      </Form.Item>
    </Form>
  );

  const renderBackupCodesStep = () => (
    <BackupCodesStep
      backupCodes={mfaSetup?.backupCodes || []}
      copied={backupCodesCopied}
      onCopy={handleCopyBackupCodes}
      onContinue={handleBackupCodesContinue}
      loading={loading}
      isSmallMobile={isSmallMobile}
      onBack={() => setStep('password')}
    />
  );

  const renderPasskeyStep = () => (
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
          <KeyOutlined style={{ fontSize: 36, color: '#5a7a6b' }} />
        </div>
        <Title level={3} style={{ color: '#2d3732', marginBottom: 12 }}>Register Your Passkey</Title>
        <Paragraph style={{ color: '#6b7770', margin: 0, fontSize: 16 }}>
          Use your device&apos;s fingerprint, face recognition, or PIN to create a secure passkey for signing in.
        </Paragraph>
      </div>

      {webAuthnSupported === false ? (
        <Alert
          message="Passkeys Not Supported"
          description="Your browser doesn't support passkeys. Please use a modern browser like Chrome, Safari, Firefox, or Edge."
          type="error"
          showIcon
          style={{ marginBottom: 24, borderRadius: 10 }}
        />
      ) : (
        <>
          <div style={{ 
            background: 'rgba(90, 122, 107, 0.05)',
            border: '1px solid rgba(90, 122, 107, 0.2)',
            borderRadius: 10, 
            padding: 20,
            marginBottom: 24,
            textAlign: 'center',
          }}>
            <Text style={{ color: '#5a7a6b', fontSize: 15 }}>
              When you click the button below, your device will prompt you to use your fingerprint, face, or PIN.
            </Text>
          </div>

          <Flex vertical gap={14}>
            <Button 
              type="primary" 
              icon={<KeyOutlined />}
              onClick={handleRegisterPasskey} 
              block 
              loading={loading}
              style={{ borderRadius: 10, height: 56, fontSize: 16 }}
            >
              {loading ? 'Registering Passkey...' : 'Register Passkey'}
            </Button>
            <Button 
              type="text" 
              onClick={() => setStep(isSysadmin ? 'backup-codes' : 'password')} 
              block 
              style={{ color: '#6b7770', fontSize: 15 }}
            >
              ← Back
            </Button>
          </Flex>
        </>
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
        initialValues={{
          firstName: invitedFirstName,
          lastName: invitedLastName,
        }}
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
        Your account is ready with passkey authentication enabled.
      </Paragraph>
      <Flex align="center" justify="center" gap={12}>
        <Spin size="small" />
        <Text style={{ color: '#5a7a6b', fontSize: 15 }}>Signing you in...</Text>
      </Flex>
    </div>
  );

  if (!invitationToken) {
    return (
      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: '#f8f9fa',
          padding: 24,
        }}
      >
        <Card style={{ 
          width: '100%', 
          maxWidth: 400,
          borderRadius: 12,
          boxShadow: '0 4px 20px rgba(45, 55, 50, 0.08)',
          border: '1px solid #ebeeed',
        }}>
          <Alert
            type="error"
            message="Invalid Invitation"
            description="This invitation link is invalid or has expired. Please contact your administrator for a new invitation."
          />
          <Button
            type="primary"
            block
            style={{ marginTop: 24, borderRadius: 8 }}
            onClick={() => router.push('/auth/login')}
          >
            Go to Login
          </Button>
        </Card>
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
        maxWidth: 800, 
        boxShadow: '0 4px 24px rgba(45, 55, 50, 0.1)',
        borderRadius: 16,
        border: '1px solid #ebeeed',
        padding: isSmallMobile ? '12px 16px' : '16px 24px',
      }}>
        <div style={{ textAlign: 'center', marginBottom: 12 }}>
          <Title level={isSmallMobile ? 3 : 2} style={{ color: '#2d3732', marginBottom: 8 }}>Welcome to LenkCare Homes</Title>
          <Text style={{ color: '#6b7770', fontSize: isSmallMobile ? 14 : 16 }}>Complete your account setup</Text>
        </div>

        {/* Step Indicator - hide on complete */}
        {step !== 'complete' && <StepIndicator />}

        {error && (
          <Alert
            message={error}
            type="error"
            showIcon
            closable
            onClose={() => setError(null)}
            style={{ marginBottom: 24, borderRadius: 8 }}
          />
        )}

        {step === 'password' && renderPasswordStep()}
        {step === 'backup-codes' && renderBackupCodesStep()}
        {step === 'register-passkey' && renderPasskeyStep()}
        {step === 'profile' && renderProfileStep()}
        {step === 'complete' && renderCompleteStep()}

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

export default function AcceptInvitationPage() {
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
      <AcceptInvitationContent />
    </Suspense>
  );
}
