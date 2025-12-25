'use client';

import React from 'react';
import { Typography, Card, Button, Switch, Divider, Flex, Grid } from 'antd';
import { ArrowLeftOutlined, BellOutlined, MailOutlined, AlertOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';

const { useBreakpoint } = Grid;

const { Title, Paragraph, Text } = Typography;

function NotificationsContent() {
  const router = useRouter();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const notificationItems = [
    {
      key: 'email',
      icon: <MailOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />,
      title: 'Email Notifications',
      description: 'Receive important updates via email',
      enabled: true,
    },
    {
      key: 'incidents',
      icon: <AlertOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />,
      title: 'Incident Alerts',
      description: 'Get notified about new incidents immediately',
      enabled: true,
    },
    {
      key: 'reminders',
      icon: <BellOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />,
      title: 'Care Reminders',
      description: 'Receive reminders for scheduled care activities',
      enabled: false,
    },
  ];

  return (
    <div>
      <Flex style={{ marginBottom: 24 }}>
        <Button 
          type="text" 
          icon={<ArrowLeftOutlined />} 
          onClick={() => router.push('/settings')}
          style={{ color: '#6b7770', minWidth: 44, minHeight: 44 }}
        >
          {isSmallMobile ? 'Back' : 'Back to Settings'}
        </Button>
      </Flex>

      <div style={{ marginBottom: 24 }}>
        <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>Notifications</Title>
        {!isSmallMobile && (
          <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
            Configure how you receive alerts and updates
          </Paragraph>
        )}
      </div>

      <Card size={isMobile ? 'small' : 'default'}>
        <Flex vertical gap={0}>
          {notificationItems.map((item, index) => (
            <Flex
              key={item.key}
              align="center"
              justify="space-between"
              style={{ 
                padding: '16px 0',
                borderBottom: index < notificationItems.length - 1 ? '1px solid #f0f0f0' : 'none',
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
              <Switch defaultChecked={item.enabled} disabled />
            </Flex>
          ))}
        </Flex>

        <Divider />

        <Paragraph type="secondary">
          Notification preferences will be fully configurable in a future update.
        </Paragraph>
      </Card>
    </div>
  );
}

export default function NotificationsPage() {
  return (
    <ProtectedRoute>
      <AuthenticatedLayout>
        <NotificationsContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
