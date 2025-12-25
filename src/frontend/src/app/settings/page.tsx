'use client';

import React from 'react';
import { Typography, Card, Flex, Grid } from 'antd';
import { UserOutlined, SafetyCertificateOutlined, BellOutlined, RightOutlined, TeamOutlined, CodeOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { useAuth } from '@/contexts/AuthContext';

const { useBreakpoint } = Grid;

const { Title, Paragraph, Text } = Typography;

function SettingsContent() {
  const router = useRouter();
  const { hasAnyRole, hasRole } = useAuth();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const canManageUsers = hasAnyRole(['Admin', 'Sysadmin']);
  const isSysadmin = hasRole('Sysadmin');
  
  // Developer tools are only available in development environment
  // Set NEXT_PUBLIC_SHOW_DEV_TOOLS=true in development
  const showDevTools = process.env.NEXT_PUBLIC_SHOW_DEV_TOOLS === 'true' && isSysadmin;

  const settingsItems = [
    {
      key: 'profile',
      icon: <UserOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />,
      title: 'Profile',
      description: 'Update your personal information and preferences',
      onClick: () => router.push('/settings/profile'),
      visible: true,
    },
    {
      key: 'security',
      icon: <SafetyCertificateOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />,
      title: 'Security',
      description: 'Manage your password and two-factor authentication',
      onClick: () => router.push('/settings/security'),
      visible: true,
    },
    {
      key: 'notifications',
      icon: <BellOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />,
      title: 'Notifications',
      description: 'Configure how you receive alerts and updates',
      onClick: () => router.push('/settings/notifications'),
      visible: true,
    },
    {
      key: 'users',
      icon: <TeamOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />,
      title: 'User Management',
      description: 'Manage users, roles, and permissions',
      onClick: () => router.push('/settings/users'),
      visible: canManageUsers,
    },
    {
      key: 'developer',
      icon: <CodeOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />,
      title: 'Developer Tools',
      description: 'Synthetic data loading and system diagnostics',
      onClick: () => router.push('/settings/developer'),
      visible: showDevTools,
    },
  ];

  const visibleItems = settingsItems.filter(item => item.visible);

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>Settings</Title>
        {!isSmallMobile && (
          <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
            Manage your account preferences and security settings
          </Paragraph>
        )}
      </div>

      <Card size={isMobile ? 'small' : 'default'}>
        <Flex vertical gap={0}>
          {visibleItems.map((item, index) => (
            <Flex
              key={item.key}
              align="center"
              justify="space-between"
              onClick={item.onClick}
              style={{ 
                cursor: 'pointer', 
                padding: '16px 0',
                borderBottom: index < visibleItems.length - 1 ? '1px solid #f0f0f0' : 'none',
              }}
            >
              <Flex align="center" gap={16}>
                <div style={{ 
                  width: 44, 
                  height: 44, 
                  borderRadius: 10, 
                  background: 'rgba(90, 122, 107, 0.1)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}>
                  {item.icon}
                </div>
                <div>
                  <Text strong style={{ color: '#2d3732', display: 'block' }}>{item.title}</Text>
                  <Text style={{ color: '#6b7770' }}>{item.description}</Text>
                </div>
              </Flex>
              <RightOutlined style={{ color: '#9ca5a0' }} />
            </Flex>
          ))}
        </Flex>
      </Card>
    </div>
  );
}

export default function SettingsPage() {
  return (
    <ProtectedRoute>
      <AuthenticatedLayout>
        <SettingsContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
