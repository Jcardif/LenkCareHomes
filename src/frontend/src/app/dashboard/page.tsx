'use client';

import React, { useEffect, useState, useCallback } from 'react';
import { Typography, Card, Row, Col, Statistic, Flex, List, Spin, Alert, Badge, Tag, Button, Grid } from 'antd';
import { 
  HomeOutlined, 
  TeamOutlined, 
  UserOutlined, 
  SafetyCertificateOutlined,
  CheckCircleOutlined,
  InboxOutlined,
  GiftOutlined,
  ReloadOutlined,
  CalendarOutlined,
} from '@ant-design/icons';
import Link from 'next/link';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { useAuth } from '@/contexts/AuthContext';
import { dashboardApi, ApiError } from '@/lib/api';
import type { AdminDashboardStats, CaregiverDashboardStats } from '@/types';

const { useBreakpoint } = Grid;

const { Title, Paragraph, Text } = Typography;

function AdminDashboard() {
  const [stats, setStats] = useState<AdminDashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const fetchStats = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await dashboardApi.getAdminStats();
      setStats(data);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to load dashboard stats';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchStats();
  }, [fetchStats]);

  if (loading) {
    return (
      <Flex justify="center" align="center" style={{ minHeight: 300 }}>
        <Spin size="large" tip="Loading dashboard..." />
      </Flex>
    );
  }

  if (error) {
    return (
      <Alert
        message="Error"
        description={error}
        type="error"
        showIcon
        action={
          <Button size="small" onClick={() => void fetchStats()}>
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <>
      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <div />
        <Button 
          icon={<ReloadOutlined />} 
          onClick={() => void fetchStats()}
          size={isMobile ? 'middle' : 'middle'}
          style={{ minWidth: 44, minHeight: 44 }}
        >
          {isSmallMobile ? '' : 'Refresh'}
        </Button>
      </Flex>

      {/* Key Metrics */}
      <Row gutter={[16, 16]} data-tour="dashboard-stats">
        <Col xs={24} sm={12} lg={6}>
          <Link href="/homes">
            <Card size={isMobile ? 'small' : 'default'} hoverable>
              <Statistic
                title="Active Homes"
                value={stats?.activeHomes ?? 0}
                suffix={<Text type="secondary">/ {stats?.totalHomes ?? 0}</Text>}
                prefix={<HomeOutlined />}
              />
            </Card>
          </Link>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Link href="/homes">
            <Card size={isMobile ? 'small' : 'default'} hoverable>
              <Statistic
                title="Beds"
                value={stats?.occupiedBeds ?? 0}
                suffix={<Text type="secondary">/ {stats?.totalBeds ?? 0}</Text>}
                prefix={<InboxOutlined />}
              />
              <Text type="secondary">{stats?.availableBeds ?? 0} available</Text>
            </Card>
          </Link>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Link href="/clients">
            <Card size={isMobile ? 'small' : 'default'} hoverable>
              <Statistic
                title="Active Clients"
                value={stats?.activeClients ?? 0}
                suffix={<Text type="secondary">/ {stats?.totalClients ?? 0}</Text>}
                prefix={<UserOutlined />}
              />
            </Card>
          </Link>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Link href="/caregivers">
            <Card size={isMobile ? 'small' : 'default'} hoverable>
              <Statistic
                title="Active Caregivers"
                value={stats?.activeCaregivers ?? 0}
                suffix={<Text type="secondary">/ {stats?.totalCaregivers ?? 0}</Text>}
                prefix={<TeamOutlined />}
              />
            </Card>
          </Link>
        </Col>
      </Row>

      {/* Secondary Info */}
      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        <Col xs={24} lg={12}>
          <Card 
            title={
              <Flex align="center" gap={8}>
                <GiftOutlined />
                <span>Upcoming Birthdays</span>
              </Flex>
            }
          >
            {(stats?.upcomingBirthdays?.length ?? 0) === 0 ? (
              <Paragraph type="secondary">No upcoming birthdays in the next 30 days.</Paragraph>
            ) : (
              <List
                dataSource={stats?.upcomingBirthdays ?? []}
                renderItem={(item) => (
                  <List.Item>
                    <List.Item.Meta
                      title={
                        <Link href={`/clients/${item.clientId}`}>
                          {item.clientName}
                        </Link>
                      }
                      description={`${item.homeName}`}
                    />
                    <Tag color={item.daysUntilBirthday <= 7 ? 'gold' : 'blue'}>
                      {item.daysUntilBirthday === 0 
                        ? 'Today!' 
                        : `In ${item.daysUntilBirthday} days`}
                    </Tag>
                  </List.Item>
                )}
              />
            )}
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card 
            title={
              <Flex align="center" justify="space-between">
                <Flex align="center" gap={8}>
                  <CalendarOutlined />
                  <span>Upcoming Appointments</span>
                </Flex>
                <Link href="/appointments">
                  <Button type="link" size="small">View All</Button>
                </Link>
              </Flex>
            }
          >
            {(stats?.upcomingAppointments?.length ?? 0) === 0 ? (
              <Paragraph type="secondary">No upcoming appointments in the next 7 days.</Paragraph>
            ) : (
              <List
                dataSource={stats?.upcomingAppointments ?? []}
                renderItem={(item) => {
                  const scheduledDate = new Date(item.scheduledAt);
                  const today = new Date();
                  today.setHours(0, 0, 0, 0);
                  const appointmentDay = new Date(scheduledDate);
                  appointmentDay.setHours(0, 0, 0, 0);
                  const daysUntil = Math.round((appointmentDay.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
                  
                  return (
                    <List.Item>
                      <List.Item.Meta
                        title={
                          <Link href={`/appointments/${item.id}`}>
                            {item.title}
                          </Link>
                        }
                        description={
                          <Flex vertical gap={2}>
                            <Text type="secondary">
                              <Link href={`/clients/${item.clientId}`}>{item.clientName}</Link>
                              {item.location && ` • ${item.location}`}
                            </Text>
                          </Flex>
                        }
                      />
                      <Tag color={daysUntil === 0 ? 'orange' : daysUntil <= 1 ? 'gold' : 'blue'}>
                        {daysUntil === 0 
                          ? scheduledDate.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
                          : daysUntil === 1 
                            ? 'Tomorrow' 
                            : `In ${daysUntil} days`}
                      </Tag>
                    </List.Item>
                  );
                }}
              />
            )}
          </Card>
        </Col>
      </Row>

      {/* Quick Actions */}
      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        <Col xs={24}>
          <Card title="Quick Actions" data-tour="quick-actions">
            <Flex gap={12} wrap="wrap">
              <Link href="/homes">
                <Button type="default" icon={<HomeOutlined />}>
                  Manage homes
                </Button>
              </Link>
              <Link href="/clients">
                <Button type="default" icon={<UserOutlined />}>
                  Manage clients
                </Button>
              </Link>
              <Link href="/caregivers">
                <Button type="default" icon={<TeamOutlined />}>
                  Manage caregivers
                </Button>
              </Link>
              <Link href="/appointments">
                <Button type="default" icon={<CalendarOutlined />}>
                  Manage appointments
                </Button>
              </Link>
              <Link href="/audit-logs">
                <Button type="default" icon={<SafetyCertificateOutlined />}>
                  View audit logs
                </Button>
              </Link>
            </Flex>
          </Card>
        </Col>
      </Row>
    </>
  );
}

function CaregiverDashboard() {
  const [stats, setStats] = useState<CaregiverDashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const fetchStats = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await dashboardApi.getCaregiverStats();
      setStats(data);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to load dashboard stats';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchStats();
  }, [fetchStats]);

  if (loading) {
    return (
      <Flex justify="center" align="center" style={{ minHeight: 300 }}>
        <Spin size="large" tip="Loading dashboard..." />
      </Flex>
    );
  }

  if (error) {
    return (
      <Alert
        message="Error"
        description={error}
        type="error"
        showIcon
        action={
          <Button size="small" onClick={() => void fetchStats()}>
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <>
      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <div />
        <Button 
          icon={<ReloadOutlined />} 
          onClick={() => void fetchStats()}
          style={{ minWidth: 44, minHeight: 44 }}
        >
          {isSmallMobile ? '' : 'Refresh'}
        </Button>
      </Flex>

      {/* Key Metrics */}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12}>
          <Link href="/homes">
            <Card size={isMobile ? 'small' : 'default'} hoverable>
              <Statistic
                title="Assigned Homes"
                value={stats?.assignedHomesCount ?? 0}
                prefix={<HomeOutlined />}
              />
            </Card>
          </Link>
        </Col>
        <Col xs={24} sm={12}>
          <Link href="/clients">
            <Card size={isMobile ? 'small' : 'default'} hoverable>
              <Statistic
                title="Active Clients"
                value={stats?.activeClientsCount ?? 0}
                prefix={<UserOutlined />}
              />
            </Card>
          </Link>
        </Col>
      </Row>

      {/* Assigned Homes and Clients */}
      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        <Col xs={24} lg={12}>
          <Card title="My Assigned Homes" data-tour="assigned-homes">
            {(stats?.assignedHomes?.length ?? 0) === 0 ? (
              <Paragraph type="secondary">
                You haven&apos;t been assigned to any homes yet. Please contact your administrator.
              </Paragraph>
            ) : (
              <List
                dataSource={stats?.assignedHomes ?? []}
                renderItem={(home) => (
                  <List.Item>
                    <List.Item.Meta
                      avatar={<HomeOutlined style={{ fontSize: 24, color: '#5a7a6b' }} />}
                      title={home.name}
                      description={`${home.city}, ${home.state}`}
                    />
                    <Badge 
                      count={home.activeClientsCount} 
                      style={{ backgroundColor: '#5a7a6b' }}
                      title={`${home.activeClientsCount} active clients`}
                    />
                  </List.Item>
                )}
              />
            )}
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card 
            title={
              <Flex align="center" justify="space-between">
                <Flex align="center" gap={8}>
                  <CalendarOutlined />
                  <span>Upcoming Appointments</span>
                </Flex>
                <Link href="/appointments">
                  <Button type="link" size="small">View All</Button>
                </Link>
              </Flex>
            }
          >
            {(stats?.upcomingAppointments?.length ?? 0) === 0 ? (
              <Paragraph type="secondary">No upcoming appointments in the next 7 days.</Paragraph>
            ) : (
              <List
                dataSource={stats?.upcomingAppointments ?? []}
                renderItem={(item) => {
                  const scheduledDate = new Date(item.scheduledAt);
                  const today = new Date();
                  today.setHours(0, 0, 0, 0);
                  const appointmentDay = new Date(scheduledDate);
                  appointmentDay.setHours(0, 0, 0, 0);
                  const daysUntil = Math.round((appointmentDay.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
                  
                  return (
                    <List.Item>
                      <List.Item.Meta
                        title={
                          <Link href={`/appointments/${item.id}`}>
                            {item.title}
                          </Link>
                        }
                        description={
                          <Flex vertical gap={2}>
                            <Text type="secondary">
                              <Link href={`/clients/${item.clientId}`}>{item.clientName}</Link>
                              {item.location && ` • ${item.location}`}
                            </Text>
                          </Flex>
                        }
                      />
                      <Tag color={daysUntil === 0 ? 'orange' : daysUntil <= 1 ? 'gold' : 'blue'}>
                        {daysUntil === 0 
                          ? scheduledDate.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
                          : daysUntil === 1 
                            ? 'Tomorrow' 
                            : `In ${daysUntil} days`}
                      </Tag>
                    </List.Item>
                  );
                }}
              />
            )}
          </Card>
        </Col>
      </Row>

      {/* My Clients */}
      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        <Col xs={24}>
          <Card title="My Clients" data-tour="my-clients">
            {(stats?.clients?.length ?? 0) === 0 ? (
              <Paragraph type="secondary">
                No clients in your assigned homes yet.
              </Paragraph>
            ) : (
              <List
                dataSource={stats?.clients ?? []}
                renderItem={(client) => (
                  <List.Item>
                    <List.Item.Meta
                      title={
                        <Link href={`/clients/${client.id}`}>
                          {client.fullName}
                        </Link>
                      }
                      description={
                        <Flex vertical gap={4}>
                          <Text type="secondary">{client.homeName} - {client.bedLabel ?? 'No bed'}</Text>
                          {client.allergies && (
                            <Tag color="red" style={{ width: 'fit-content' }}>
                              Allergies: {client.allergies}
                            </Tag>
                          )}
                        </Flex>
                      }
                    />
                  </List.Item>
                )}
              />
            )}
          </Card>
        </Col>
      </Row>
    </>
  );
}

function DashboardContent() {
  const { user, hasRole } = useAuth();

  return (
    <div data-tour="dashboard-welcome">
      <Title level={2}>Welcome, {user?.firstName}!</Title>
      <Paragraph type="secondary">
        LenkCare Homes Management System
      </Paragraph>

      {hasRole('Admin') ? (
        <AdminDashboard />
      ) : hasRole('Caregiver') ? (
        <CaregiverDashboard />
      ) : (
        <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
          <Col span={24}>
            <Card title="Security Status">
              <Flex align="center" gap={16}>
                <div style={{ 
                  width: 48, 
                  height: 48, 
                  borderRadius: 12, 
                  background: 'rgba(90, 122, 107, 0.1)', 
                  display: 'flex', 
                  alignItems: 'center', 
                  justifyContent: 'center'
                }}>
                  <CheckCircleOutlined style={{ fontSize: 24, color: '#5a7a6b' }} />
                </div>
                <div>
                  <Title level={5} style={{ margin: 0, color: '#2d3732' }}>Account Active</Title>
                  <Paragraph style={{ margin: 0, color: '#6b7770' }}>
                    Contact an administrator for role assignment.
                  </Paragraph>
                </div>
              </Flex>
            </Card>
          </Col>
        </Row>
      )}
    </div>
  );
}

export default function DashboardPage() {
  return (
    <ProtectedRoute>
      <AuthenticatedLayout>
        <DashboardContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
