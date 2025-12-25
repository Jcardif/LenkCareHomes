'use client';

import React, { useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Spin, Typography } from 'antd';

const { Text } = Typography;

/**
 * @deprecated This page has been replaced by /auth/setup-passkey.
 * This redirect page ensures any existing links still work.
 */
function SetupMfaRedirect() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const userId = searchParams.get('userId');

  useEffect(() => {
    // Redirect to new passkey setup page, preserving userId if present
    const redirectUrl = userId 
      ? `/auth/setup-passkey?userId=${userId}`
      : '/auth/setup-passkey';
    
    router.replace(redirectUrl);
  }, [router, userId]);

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        background: '#f8f9fa',
        gap: 16,
      }}
    >
      <Spin size="large" />
      <Text style={{ color: '#6b7770' }}>
        Redirecting to passkey setup...
      </Text>
    </div>
  );
}

export default function SetupMfaPage() {
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
      <SetupMfaRedirect />
    </Suspense>
  );
}
