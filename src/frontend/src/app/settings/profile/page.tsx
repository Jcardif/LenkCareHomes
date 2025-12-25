'use client';

import React from 'react';
import { Typography, Card, Form, Input, Button, Flex, Divider, Grid } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { useAuth } from '@/contexts/AuthContext';

const { Title, Paragraph } = Typography;
const { useBreakpoint } = Grid;

function ProfileContent() {
  const router = useRouter();
  const { user } = useAuth();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  return (
    <div>
      <Flex style={{ marginBottom: 24 }}>
        <Button 
          type="text" 
          icon={<ArrowLeftOutlined />} 
          onClick={() => router.push('/settings')}
          style={{ color: '#6b7770', minWidth: 44, minHeight: 44 }}
        >
          {isSmallMobile ? '' : 'Back to Settings'}
        </Button>
      </Flex>

      <div style={{ marginBottom: 24 }}>
        <Title level={isMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>Profile</Title>
        <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
          Update your personal information
        </Paragraph>
      </div>

      <Card size={isMobile ? 'small' : 'default'}>
        <Form
          layout="vertical"
          initialValues={{
            firstName: user?.firstName || '',
            lastName: user?.lastName || '',
            email: user?.email || '',
          }}
        >
          <Form.Item label="First Name" name="firstName">
            <Input placeholder="Enter your first name" disabled />
          </Form.Item>

          <Form.Item label="Last Name" name="lastName">
            <Input placeholder="Enter your last name" disabled />
          </Form.Item>

          <Form.Item label="Email" name="email">
            <Input placeholder="Enter your email" disabled />
          </Form.Item>

          <Divider />

          <Paragraph type="secondary">
            Profile editing will be available in a future update. Contact your administrator to update your information.
          </Paragraph>
        </Form>
      </Card>
    </div>
  );
}

export default function ProfilePage() {
  return (
    <ProtectedRoute>
      <AuthenticatedLayout>
        <ProfileContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
