'use client';

import React, { useState, useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Form, Input, Button, Typography, Divider, Alert, Spin, Grid } from 'antd';

const { useBreakpoint } = Grid;
import { KeyOutlined, SafetyCertificateOutlined } from '@ant-design/icons';
import { useAuth } from '@/contexts/AuthContext';
import { passkeyApi, getUserFriendlyError, API_BASE_URL } from '@/lib/api';
import {
  isWebAuthnSupported,
  prepareCredentialRequestOptions,
  serializeAssertionResponse,
} from '@/types/passkey';

const { Title, Text, Link } = Typography;

type LoginStep = 'credentials' | 'passkey' | 'lost-passkey' | 'backup-recovery';

interface CredentialsFormValues {
  email: string;
  password: string;
}

interface BackupFormValues {
  backupCode: string;
}

function LoginContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { isAuthenticated, isLoading: authLoading, refreshUser } = useAuth();
  
  // Responsive breakpoints
  const screens = useBreakpoint();
  const isSmallMobile = !screens.sm;
  
  const [step, setStep] = useState<LoginStep>('credentials');
  const [email, setEmail] = useState<string>('');
  const [userId, setUserId] = useState<string | null>(null);
  const [isSysadmin, setIsSysadmin] = useState<boolean>(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [passkeySupported, setPasskeySupported] = useState(true);

  const redirectUrl = searchParams.get('redirect') || '/dashboard';

  // Check WebAuthn support on mount
  useEffect(() => {
    setPasskeySupported(isWebAuthnSupported());
  }, []);

  // Redirect if already authenticated
  useEffect(() => {
    if (!authLoading && isAuthenticated) {
      router.replace(redirectUrl);
    }
  }, [isAuthenticated, authLoading, router, redirectUrl]);

  const handleCredentialsSubmit = async (values: CredentialsFormValues) => {
    setLoading(true);
    setError(null);
    setEmail(values.email);

    try {
      const result = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(values),
        credentials: 'include',
      });
      
      const response = await result.json();

      if (!response.success) {
        throw new Error(response.error || 'Login failed');
      }

      if (response.requiresPasskeySetup) {
        // User needs to set up their first passkey
        const passkeySetupTokenParam = response.passkeySetupToken ? `&passkeySetupToken=${encodeURIComponent(response.passkeySetupToken)}` : '';
        const isSysadminParam = response.isSysadmin ? '&isSysadmin=true' : '';
        router.replace(`/auth/setup-passkey?userId=${response.userId}${passkeySetupTokenParam}${isSysadminParam}`);
        return;
      }

      if (response.requiresPasskey) {
        // User has passkeys - need to authenticate with one
        setUserId(response.userId);
        setIsSysadmin(response.isSysadmin || false);
        setStep('passkey');
        
        // Auto-trigger passkey authentication
        if (passkeySupported) {
          setTimeout(() => handlePasskeyAuthentication(values.email), 100);
        }
        return;
      }

      // Direct login (shouldn't happen with passkey requirement, but handle it)
      if (response.token) {
        await refreshUser();
        router.replace(redirectUrl);
      }
    } catch (err) {
      setError(getUserFriendlyError(err, 'Unable to sign in. Please check your email and password.'));
    } finally {
      setLoading(false);
    }
  };

  const handlePasskeyAuthentication = async (userEmail?: string) => {
    const emailToUse = userEmail || email;
    if (!emailToUse) {
      setError('Email is required for passkey authentication');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      // Step 1: Start authentication - get WebAuthn options
      const startResponse = await passkeyApi.startAuthentication({ email: emailToUse });
      
      if (!startResponse.success || !startResponse.options || !startResponse.sessionId) {
        throw new Error(startResponse.error || 'Failed to start passkey authentication');
      }

      // Step 2: Convert options for browser API
      const requestOptions = prepareCredentialRequestOptions(
        startResponse.options as unknown as Record<string, unknown>
      );

      // Step 3: Get credential from authenticator
      const credential = await navigator.credentials.get({
        publicKey: requestOptions,
      }) as PublicKeyCredential | null;

      if (!credential) {
        throw new Error('Passkey authentication was cancelled');
      }

      // Step 4: Serialize and send to server
      const assertionResponse = serializeAssertionResponse(credential);
      const assertionResponseJson = JSON.stringify(assertionResponse);
      
      const completeResponse = await passkeyApi.completeAuthentication({
        sessionId: startResponse.sessionId,
        assertionResponse: assertionResponseJson,
      });

      if (!completeResponse.success) {
        throw new Error(completeResponse.error || 'Passkey authentication failed');
      }

      // Success! Refresh user and redirect
      await refreshUser();
      router.replace(redirectUrl);

    } catch (err) {
      if (err instanceof Error && err.name === 'NotAllowedError') {
        setError('Sign-in was cancelled. Please try again.');
      } else {
        setError(getUserFriendlyError(err, 'Unable to sign in. Please try again.'));
      }
    } finally {
      setLoading(false);
    }
  };

  const handleBackupSubmit = async (values: BackupFormValues) => {
    if (!userId) {
      setError('Session expired. Please start over.');
      setStep('credentials');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`${API_BASE_URL}/auth/mfa/verify-backup`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId, backupCode: values.backupCode }),
        credentials: 'include',
      });

      const result = await response.json();

      if (result.success) {
        if (result.requiresPasskeySetup && result.passkeySetupToken) {
          // Redirect to passkey setup page with reset flag to skip backup codes regeneration
          router.replace(`/auth/setup-passkey?userId=${result.userId}&passkeySetupToken=${encodeURIComponent(result.passkeySetupToken)}&isSysadmin=true&reset=true`);
        } else {
          await refreshUser();
          router.replace(redirectUrl);
        }
      } else {
        setError('Invalid backup code. Please try again.');
      }
    } catch (err) {
      setError(getUserFriendlyError(err, 'Unable to verify backup code. Please try again.'));
    } finally {
      setLoading(false);
    }
  };

  // Show loading while checking auth status
  if (authLoading || isAuthenticated) {
    return (
      <div style={{ 
        minHeight: '100vh', 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        background: '#f8f9fa'
      }}>
        <Spin size="large" />
      </div>
    );
  }

  const renderCredentialsForm = () => (
    <Form<CredentialsFormValues>
      name="login"
      onFinish={handleCredentialsSubmit}
      layout="vertical"
      size="large"
      requiredMark={false}
    >
      <Form.Item
        name="email"
        label="Email"
        rules={[
          { required: true, message: 'Please enter your email' },
          { type: 'email', message: 'Please enter a valid email' },
        ]}
      >
        <Input
          placeholder="name@example.com"
          autoComplete="email"
        />
      </Form.Item>

      <Form.Item
        name="password"
        label="Password"
        rules={[{ required: true, message: 'Please enter your password' }]}
      >
        <Input.Password
          placeholder="Enter your password"
          autoComplete="current-password"
        />
      </Form.Item>

      <Form.Item style={{ marginBottom: 16, marginTop: 32 }}>
        <Button type="primary" htmlType="submit" block loading={loading}>
          Sign In
        </Button>
      </Form.Item>

      <div style={{ textAlign: 'center' }}>
        <Link href="/auth/forgot-password">Forgot password?</Link>
      </div>
    </Form>
  );

  const renderPasskeyForm = () => (
    <div>
      <div style={{ textAlign: 'center', marginBottom: 32 }}>
        <div style={{ 
          width: 64, 
          height: 64, 
          borderRadius: 16, 
          background: 'rgba(90, 122, 107, 0.1)', 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          margin: '0 auto 16px'
        }}>
          <KeyOutlined style={{ fontSize: 32, color: '#5a7a6b' }} />
        </div>
        <Title level={4} style={{ marginBottom: 8, fontWeight: 600 }}>Passkey Authentication</Title>
        <Text type="secondary">
          Use your fingerprint, face, or device PIN to sign in
        </Text>
      </div>

      {!passkeySupported ? (
        <Alert
          message="Passkeys Not Supported"
          description="Your browser doesn't support passkeys. Please use a modern browser like Chrome, Safari, Firefox, or Edge."
          type="warning"
          showIcon
          style={{ marginBottom: 24 }}
        />
      ) : (
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <Button
            type="primary"
            icon={<KeyOutlined />}
            size="large"
            onClick={() => handlePasskeyAuthentication()}
            loading={loading}
            block
            style={{ height: 52, fontSize: 16 }}
          >
            {loading ? 'Authenticating...' : 'Use Passkey'}
          </Button>
        </div>
      )}

      <div style={{ textAlign: 'center', marginBottom: 16 }}>
        <Text type="secondary" style={{ fontSize: 13 }}>
          Signing in as <strong>{email}</strong>
        </Text>
      </div>

      <div style={{ textAlign: 'center', marginTop: 24 }}>
        <Button 
          type="link" 
          onClick={() => setStep('lost-passkey')} 
          style={{ color: '#6b7770', fontSize: 13 }}
        >
          Lost your passkey?
        </Button>
      </div>

      <Divider style={{ margin: '24px 0' }} />

      <Button 
        type="text" 
        block 
        onClick={() => { setStep('credentials'); setEmail(''); setUserId(null); setIsSysadmin(false); }}
        style={{ color: '#6b7770' }}
      >
        ← Back to login
      </Button>
    </div>
  );

  const renderLostPasskeyForm = () => (
    <div>
      <div style={{ textAlign: 'center', marginBottom: 32 }}>
        <div style={{ 
          width: 64, 
          height: 64, 
          borderRadius: 16, 
          background: isSysadmin ? 'rgba(201, 162, 39, 0.1)' : 'rgba(90, 122, 107, 0.1)', 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          margin: '0 auto 16px'
        }}>
          <SafetyCertificateOutlined style={{ fontSize: 32, color: isSysadmin ? '#c9a227' : '#5a7a6b' }} />
        </div>
        <Title level={4} style={{ marginBottom: 8, fontWeight: 600 }}>Lost Your Passkey?</Title>
        <Text type="secondary">
          {isSysadmin 
            ? 'As a Sysadmin, you can use a backup code to reset your passkey'
            : 'Contact your system administrator to reset your authentication'}
        </Text>
      </div>

      {isSysadmin && (
        <>
          <Alert
            message="Backup Code Recovery"
            description="Use one of your backup codes to verify your identity and set up a new passkey. Each backup code can only be used once."
            type="info"
            showIcon
            style={{ marginBottom: 24 }}
          />
          <Button 
            type="primary" 
            block 
            onClick={() => {
              setLoading(false);
              setError(null);
              setStep('backup-recovery');
            }}
            style={{ height: 48 }}
          >
            Use Backup Code to Reset Passkey
          </Button>
        </>
      )}

      <Divider style={{ margin: '24px 0' }} />

      <Button 
        type="text" 
        block 
        onClick={() => setStep('passkey')}
        style={{ color: '#6b7770' }}
      >
        ← Back to passkey sign-in
      </Button>
    </div>
  );

  const renderBackupRecoveryForm = () => (
    <Form<BackupFormValues>
      name="backup-recovery"
      onFinish={handleBackupSubmit}
      layout="vertical"
      size="large"
      requiredMark={false}
    >
      <div style={{ textAlign: 'center', marginBottom: 32 }}>
        <div style={{ 
          width: 64, 
          height: 64, 
          borderRadius: 16, 
          background: 'rgba(201, 162, 39, 0.1)', 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          margin: '0 auto 16px'
        }}>
          <SafetyCertificateOutlined style={{ fontSize: 32, color: '#c9a227' }} />
        </div>
        <Title level={4} style={{ marginBottom: 8, fontWeight: 600 }}>Enter Backup Code</Title>
        <Text type="secondary">
          Enter one of your backup codes to reset your passkey
        </Text>
      </div>

      <Alert
        message="This will reset your passkey"
        description="After verifying your backup code, you will be prompted to set up a new passkey. Your old passkeys will be disabled."
        type="warning"
        showIcon
        style={{ marginBottom: 24 }}
      />

      <Form.Item
        name="backupCode"
        label="Backup Code"
        rules={[{ required: true, message: 'Please enter a backup code' }]}
      >
        <Input
          placeholder="XXXX-XXXX"
          style={{ textAlign: 'center', letterSpacing: 4, fontSize: 18, fontWeight: 500 }}
        />
      </Form.Item>

      <Form.Item style={{ marginTop: 24 }}>
        <Button type="primary" htmlType="submit" block loading={loading}>
          Verify & Reset Passkey
        </Button>
      </Form.Item>

      <Divider style={{ margin: '24px 0' }} />

      <Button 
        type="text" 
        block 
        onClick={() => setStep('lost-passkey')}
        style={{ color: '#6b7770' }}
      >
        ← Back
      </Button>
    </Form>
  );

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
      <div style={{ width: '100%', maxWidth: 400 }}>
        {/* Logo/Brand */}
        {step === 'credentials' && (
          <div style={{ textAlign: 'center', marginBottom: isSmallMobile ? 32 : 40 }}>
            <div style={{ 
              width: isSmallMobile ? 48 : 56, 
              height: isSmallMobile ? 48 : 56, 
              borderRadius: 12,
              background: '#5a7a6b',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 20px',
              boxShadow: '0 4px 16px rgba(90, 122, 107, 0.25)'
            }}>
              <span style={{ color: 'white', fontSize: isSmallMobile ? 18 : 20, fontWeight: 600, letterSpacing: '-0.5px' }}>LC</span>
            </div>
            <Title level={isSmallMobile ? 3 : 2} style={{ marginBottom: 4, fontWeight: 600, color: '#2d3732' }}>
              Welcome back
            </Title>
            <Text style={{ color: '#6b7770', fontSize: isSmallMobile ? 14 : 15 }}>
              Sign in to LenkCare Homes
            </Text>
          </div>
        )}

        {/* Card */}
        <div
          style={{
            background: '#ffffff',
            borderRadius: 12,
            padding: isSmallMobile ? 24 : 32,
            boxShadow: '0 4px 20px rgba(45, 55, 50, 0.08)',
            border: '1px solid #ebeeed',
          }}
        >
          {error && (
            <Alert
              message={error}
              type="error"
              showIcon
              closable
              onClose={() => setError(null)}
              style={{ marginBottom: 24, borderRadius: 10 }}
            />
          )}

          {step === 'credentials' && renderCredentialsForm()}
          {step === 'passkey' && renderPasskeyForm()}
          {step === 'lost-passkey' && renderLostPasskeyForm()}
          {step === 'backup-recovery' && renderBackupRecoveryForm()}
        </div>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={
      <div style={{ 
        minHeight: '100vh', 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        background: '#f8f9fa'
      }}>
        <Spin size="large" />
      </div>
    }>
      <LoginContent />
    </Suspense>
  );
}
