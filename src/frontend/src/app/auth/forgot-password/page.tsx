'use client';

import React, { useState, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Form, Input, Button, Card, Typography, Alert, Result, Grid } from 'antd';
import { MailOutlined, LockOutlined, CheckCircleOutlined } from '@ant-design/icons';
import { authApi, getUserFriendlyError } from '@/lib/api';

const { Title, Paragraph } = Typography;
const { useBreakpoint } = Grid;

function ForgotPasswordContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [step, setStep] = useState<'request' | 'reset' | 'success'>('request');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [emailSent, setEmailSent] = useState(false);
  const screens = useBreakpoint();
  const isSmallMobile = !screens.sm;

  const token = searchParams.get('token');

  // If we have a token in URL, show reset form
  React.useEffect(() => {
    if (token) {
      setStep('reset');
    }
  }, [token]);

  const handleRequestReset = async (values: { email: string }) => {
    setLoading(true);
    setError(null);

    try {
      await authApi.requestPasswordReset({ email: values.email });
      setEmailSent(true);
    } catch {
      // Don't show error - always show success to prevent user enumeration
      setEmailSent(true);
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (values: { password: string; confirmPassword: string }) => {
    if (!token) {
      setError('Invalid reset link');
      return;
    }

    if (values.password !== values.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await authApi.resetPassword({
        token,
        newPassword: values.password,
      });
      setStep('success');
    } catch (err) {
      setError(getUserFriendlyError(err, 'Unable to reset password. The link may have expired. Please request a new one.'));
    } finally {
      setLoading(false);
    }
  };

  const renderRequestForm = () => {
    if (emailSent) {
      return (
        <Result
          icon={<MailOutlined style={{ color: '#5a7a6b' }} />}
          title="Check Your Email"
          subTitle="If an account exists with that email, we've sent password reset instructions."
          extra={
            <Button type="primary" onClick={() => router.push('/auth/login')}>
              Back to Login
            </Button>
          }
        />
      );
    }

    return (
      <Form onFinish={handleRequestReset} layout="vertical" size="large" requiredMark={false}>
        <Paragraph>
          Enter your email address and we&apos;ll send you instructions to reset your password.
        </Paragraph>

        <Form.Item
          name="email"
          label="Email"
          rules={[
            { required: true, message: 'Please enter your email' },
            { type: 'email', message: 'Please enter a valid email' },
          ]}
        >
          <Input prefix={<MailOutlined />} placeholder="Enter your email" />
        </Form.Item>

        <Form.Item>
          <Button type="primary" htmlType="submit" block loading={loading}>
            Send Reset Link
          </Button>
        </Form.Item>

        <div style={{ textAlign: 'center' }}>
          <Button type="link" onClick={() => router.push('/auth/login')}>
            Back to Login
          </Button>
        </div>
      </Form>
    );
  };

  const renderResetForm = () => (
    <Form onFinish={handleResetPassword} layout="vertical" size="large" requiredMark={false}>
      <Paragraph>
        Create a new password. It must be at least 8 characters and include
        uppercase, lowercase, numbers, and special characters.
      </Paragraph>

      <Form.Item
        name="password"
        label="New Password"
        rules={[
          { required: true, message: 'Please enter a password' },
          { min: 8, message: 'Password must be at least 8 characters' },
          {
            pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
            message: 'Password must include uppercase, lowercase, number, and special character',
          },
        ]}
      >
        <Input.Password prefix={<LockOutlined />} placeholder="Enter new password" />
      </Form.Item>

      <Form.Item
        name="confirmPassword"
        label="Confirm Password"
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
        <Input.Password prefix={<LockOutlined />} placeholder="Confirm new password" />
      </Form.Item>

      <Form.Item>
        <Button type="primary" htmlType="submit" block loading={loading}>
          Reset Password
        </Button>
      </Form.Item>
    </Form>
  );

  const renderSuccess = () => (
    <Result
      icon={<CheckCircleOutlined style={{ color: '#5a7a6b' }} />}
      title="Password Reset Successful"
      subTitle="Your password has been reset. You can now sign in with your new password."
      extra={
        <Button type="primary" onClick={() => router.push('/auth/login')}>
          Go to Login
        </Button>
      }
    />
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
      <Card style={{ 
        width: '100%', 
        maxWidth: 400, 
        boxShadow: '0 4px 20px rgba(45, 55, 50, 0.08)',
        borderRadius: 12,
        border: '1px solid #ebeeed',
      }}>
        {step !== 'success' && (
          <div style={{ textAlign: 'center', marginBottom: 24 }}>
            <Title level={isSmallMobile ? 4 : 3} style={{ color: '#2d3732' }}>
              {step === 'request' ? 'Forgot Password' : 'Reset Password'}
            </Title>
          </div>
        )}

        {error && (
          <Alert
            title={error}
            type="error"
            showIcon
            closable
            onClose={() => setError(null)}
            style={{ marginBottom: 24 }}
          />
        )}

        {step === 'request' && renderRequestForm()}
        {step === 'reset' && renderResetForm()}
        {step === 'success' && renderSuccess()}
      </Card>
    </div>
  );
}

export default function ForgotPasswordPage() {
  return (
    <Suspense fallback={<div style={{ textAlign: 'center', padding: 48 }}>Loading...</div>}>
      <ForgotPasswordContent />
    </Suspense>
  );
}
