'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import { Result, Button, Grid, Space } from 'antd';

const { useBreakpoint } = Grid;

export default function UnauthorizedPage() {
  const router = useRouter();
  const screens = useBreakpoint();
  const isSmallMobile = !screens.sm;

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
      <Result
        status="403"
        title="Access Denied"
        subTitle="Sorry, you don't have permission to access this page."
        extra={
          <Space direction={isSmallMobile ? 'vertical' : 'horizontal'} style={{ width: isSmallMobile ? '100%' : 'auto' }}>
            <Button 
              key="dashboard" 
              type="primary" 
              onClick={() => router.push('/dashboard')}
              style={{ minWidth: 44, minHeight: 44, width: isSmallMobile ? '100%' : 'auto' }}
            >
              Go to Dashboard
            </Button>
            <Button 
              key="logout" 
              onClick={() => router.push('/auth/login')}
              style={{ minWidth: 44, minHeight: 44, width: isSmallMobile ? '100%' : 'auto' }}
            >
              Sign Out
            </Button>
          </Space>
        }
      />
    </div>
  );
}
