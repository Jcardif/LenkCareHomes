'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import { Button, Typography } from 'antd';
import { HomeOutlined } from '@ant-design/icons';

const { Title, Paragraph } = Typography;

export default function NotFound() {
  const router = useRouter();

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
      <div style={{ textAlign: 'center', maxWidth: 400 }}>
        <div style={{ 
          width: 80, 
          height: 80, 
          borderRadius: 20, 
          background: 'rgba(90, 122, 107, 0.1)', 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          margin: '0 auto 24px'
        }}>
          <span style={{ fontSize: 40, color: '#5a7a6b', fontWeight: 300 }}>404</span>
        </div>
        
        <Title level={2} style={{ color: '#2d3732', marginBottom: 8 }}>
          Page Not Found
        </Title>
        
        <Paragraph style={{ color: '#6b7770', marginBottom: 32 }}>
          Sorry, the page you&apos;re looking for doesn&apos;t exist or has been moved.
        </Paragraph>

        <Button 
          type="primary" 
          size="large"
          icon={<HomeOutlined />}
          onClick={() => router.push('/dashboard')}
        >
          Go to Dashboard
        </Button>
      </div>
    </div>
  );
}
