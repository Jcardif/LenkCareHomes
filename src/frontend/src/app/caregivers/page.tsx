'use client';

import React, { useEffect, useState, useCallback } from 'react';
import {
  Typography,
  Card,
  Empty,
  Button,
  Table,
  Tag,
  Space,
  Modal,
  Form,
  Input,
  message,
  Spin,
  Alert,
  Flex,
  Switch,
  Tooltip,
  Avatar,
  Select,
  Descriptions,
  List,
  Popconfirm,
  Grid,
  Row,
  Col,
  Dropdown,
} from 'antd';
import type { MenuProps } from 'antd';
import {
  TeamOutlined,
  PlusOutlined,
  EyeOutlined,
  ReloadOutlined,
  HomeOutlined,
  UserOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  DeleteOutlined,
  MailOutlined,
  MoreOutlined,
} from '@ant-design/icons';

const { useBreakpoint } = Grid;
import Link from 'next/link';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { caregiversApi, homesApi, usersApi, ApiError } from '@/lib/api';
import type { CaregiverSummary, Caregiver, HomeSummary, CaregiverHomeAssignment, InviteUserRequest } from '@/types';
import type { ColumnsType } from 'antd/es/table';

const { Title, Paragraph, Text } = Typography;

interface InviteCaregiverFormData {
  email: string;
  firstName: string;
  lastName: string;
  homeIds: string[];
}

function CaregiversContent() {
  const [caregivers, setCaregivers] = useState<CaregiverSummary[]>([]);
  const [homes, setHomes] = useState<HomeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [includeInactive, setIncludeInactive] = useState(false);

  const [inviteModalOpen, setInviteModalOpen] = useState(false);
  const [inviteForm] = Form.useForm<InviteCaregiverFormData>();
  const [inviting, setInviting] = useState(false);

  const [detailModalOpen, setDetailModalOpen] = useState(false);
  const [selectedCaregiver, setSelectedCaregiver] = useState<Caregiver | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const [assignModalOpen, setAssignModalOpen] = useState(false);
  const [selectedHomeIds, setSelectedHomeIds] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);

  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const fetchCaregivers = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await caregiversApi.getAll(includeInactive);
      setCaregivers(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load caregivers';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [includeInactive]);

  const fetchHomes = useCallback(async () => {
    try {
      const data = await homesApi.getAll(false);
      setHomes(data);
    } catch {
      // Silently fail
    }
  }, []);

  useEffect(() => {
    void fetchCaregivers();
    void fetchHomes();
  }, [fetchCaregivers, fetchHomes]);

  const handleInviteCaregiver = async (values: InviteCaregiverFormData) => {
    try {
      setInviting(true);
      const request: InviteUserRequest = {
        email: values.email,
        firstName: values.firstName,
        lastName: values.lastName,
        role: 'Caregiver',
        homeIds: values.homeIds,
      };
      const response = await usersApi.invite(request);
      if (response.success) {
        void message.success('Caregiver invitation sent successfully');
        setInviteModalOpen(false);
        inviteForm.resetFields();
        void fetchCaregivers();
      } else {
        void message.error(response.error || 'Failed to send invitation');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to send invitation';
      void message.error(msg);
    } finally {
      setInviting(false);
    }
  };

  const handleViewDetails = async (caregiver: CaregiverSummary) => {
    try {
      setDetailLoading(true);
      setDetailModalOpen(true);
      const data = await caregiversApi.getById(caregiver.id);
      setSelectedCaregiver(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load caregiver details';
      void message.error(msg);
      setDetailModalOpen(false);
    } finally {
      setDetailLoading(false);
    }
  };

  const handleOpenAssignModal = () => {
    if (!selectedCaregiver) return;
    setSelectedHomeIds(
      selectedCaregiver.homeAssignments
        .filter((a) => a.isActive)
        .map((a) => a.homeId)
    );
    setAssignModalOpen(true);
  };

  const handleAssignHomes = async () => {
    if (!selectedCaregiver) return;
    try {
      setSubmitting(true);
      const result = await caregiversApi.assignHomes(selectedCaregiver.id, {
        homeIds: selectedHomeIds,
      });
      if (result.success) {
        void message.success('Home assignments updated successfully');
        setAssignModalOpen(false);
        // Refresh the selected caregiver details
        const updated = await caregiversApi.getById(selectedCaregiver.id);
        setSelectedCaregiver(updated);
        void fetchCaregivers();
      } else {
        void message.error(result.error || 'Failed to update home assignments');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to update home assignments';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleRemoveAssignment = async (assignment: CaregiverHomeAssignment) => {
    if (!selectedCaregiver) return;
    try {
      const result = await caregiversApi.removeHomeAssignment(
        selectedCaregiver.id,
        assignment.homeId
      );
      if (result.success) {
        void message.success('Home assignment removed');
        // Refresh
        const updated = await caregiversApi.getById(selectedCaregiver.id);
        setSelectedCaregiver(updated);
        void fetchCaregivers();
      } else {
        void message.error(result.error || 'Failed to remove assignment');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to remove assignment';
      void message.error(msg);
    }
  };

  // Action menu for mobile dropdown
  const getActionMenuItems = (record: CaregiverSummary): MenuProps['items'] => [
    {
      key: 'manage',
      label: 'Manage',
      icon: <EyeOutlined />,
      onClick: () => void handleViewDetails(record),
    },
  ];

  const columns: ColumnsType<CaregiverSummary> = [
    {
      title: 'Caregiver',
      key: 'caregiver',
      render: (_, record) => (
        <Flex align="center" gap={12}>
          <Avatar icon={<UserOutlined />} style={{ backgroundColor: '#5a7a6b' }} />
          <div>
            <Text strong style={{ display: 'block' }}>{record.fullName}</Text>
            {!isSmallMobile && (
              <Text type="secondary" style={{ fontSize: 12 }}>
                {record.email}
              </Text>
            )}
          </div>
        </Flex>
      ),
      sorter: (a, b) => a.lastName.localeCompare(b.lastName),
    },
    {
      title: 'Status',
      key: 'status',
      responsive: ['sm'],
      render: (_, record) => (
        <Space direction="vertical" size={4}>
          <Tag color={record.isActive ? 'green' : 'default'}>
            {record.isActive ? 'Active' : 'Inactive'}
          </Tag>
          {!record.invitationAccepted && (
            <Tag color="orange">Pending</Tag>
          )}
        </Space>
      ),
    },
    {
      title: 'Homes',
      dataIndex: 'assignedHomesCount',
      key: 'assignedHomesCount',
      responsive: ['md'],
      render: (count) => (
        <Tag color={count > 0 ? 'blue' : 'default'}>
          {count} {count === 1 ? 'home' : 'homes'}
        </Tag>
      ),
      sorter: (a, b) => a.assignedHomesCount - b.assignedHomesCount,
    },
    {
      title: 'Actions',
      key: 'actions',
      width: isMobile ? 60 : 120,
      render: (_, record) => (
        isMobile ? (
          <Dropdown menu={{ items: getActionMenuItems(record) }} trigger={['click']}>
            <Button
              type="text"
              icon={<MoreOutlined />}
              onClick={(e) => e.stopPropagation()}
              style={{ minWidth: 44, minHeight: 44 }}
            />
          </Dropdown>
        ) : (
          <Space>
            <Tooltip title="View Details & Manage Assignments">
              <Button
                type="text"
                icon={<EyeOutlined />}
                onClick={() => void handleViewDetails(record)}
              >
                Manage
              </Button>
            </Tooltip>
          </Space>
        )
      ),
    },
  ];

  if (loading && caregivers.length === 0) {
    return (
      <Flex justify="center" align="center" style={{ minHeight: 300 }}>
        <Spin size="large" tip="Loading caregivers..." />
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
          <Button size="small" onClick={() => void fetchCaregivers()}>
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <div>
      <div className="page-header-wrapper" style={{ marginBottom: 24 }}>
        <div>
          <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>Caregivers</Title>
          {!isSmallMobile && (
            <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
              Manage caregivers and their home assignments
            </Paragraph>
          )}
        </div>
        <div className="page-header-actions">
          {!isMobile && (
            <Button icon={<ReloadOutlined />} onClick={() => void fetchCaregivers()}>
              Refresh
            </Button>
          )}
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setInviteModalOpen(true)}
            style={isSmallMobile ? { width: '100%' } : undefined}
          >
            {isSmallMobile ? 'Invite' : 'Invite Caregiver'}
          </Button>
        </div>
      </div>

      <Card className="responsive-card">
        <Row gutter={[16, 16]} align="middle" style={{ marginBottom: 16 }}>
          <Col xs={24} sm={12}>
            <Space>
              <Switch
                checked={includeInactive}
                onChange={setIncludeInactive}
              />
              <span>Show inactive</span>
            </Space>
          </Col>
          <Col xs={24} sm={12} style={{ textAlign: isMobile ? 'left' : 'right' }}>
            {isMobile ? (
              <Button 
                icon={<ReloadOutlined />} 
                onClick={() => void fetchCaregivers()}
                style={{ width: '100%' }}
              >
                Refresh
              </Button>
            ) : (
              <Text type="secondary">{caregivers.length} caregivers</Text>
            )}
          </Col>
        </Row>

        {caregivers.length === 0 ? (
          <Empty
            image={<TeamOutlined style={{ fontSize: 64, color: '#5a7a6b' }} />}
            imageStyle={{ height: 80 }}
            description={
              <div>
                <Title level={5} style={{ color: '#2d3732', marginBottom: 8 }}>No caregivers yet</Title>
                <Paragraph style={{ color: '#6b7770' }}>
                  Invite caregivers to help manage residents and log daily care activities.
                </Paragraph>
              </div>
            }
          >
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setInviteModalOpen(true)}
            >
              Invite Your First Caregiver
            </Button>
          </Empty>
        ) : (
          <Table
            columns={columns}
            dataSource={caregivers}
            rowKey="id"
            scroll={{ x: 'max-content' }}
            pagination={{ 
              pageSize: 10,
              showSizeChanger: !isMobile,
              showTotal: isMobile ? undefined : (total, range) => `${range[0]}-${range[1]} of ${total} caregivers`,
              size: isMobile ? 'small' : 'default',
            }}
            loading={loading}
          />
        )}
      </Card>

      {/* Invite Caregiver Modal */}
      <Modal
        title={
          <Flex align="center" gap={8}>
            <MailOutlined />
            <span>Invite New Caregiver</span>
          </Flex>
        }
        open={inviteModalOpen}
        onCancel={() => {
          setInviteModalOpen(false);
          inviteForm.resetFields();
        }}
        footer={null}
        destroyOnHidden
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Paragraph type="secondary" style={{ marginBottom: 24 }}>
          Send an invitation email to a new caregiver. They will receive a link to set up their account and MFA.
        </Paragraph>
        <Form
          form={inviteForm}
          layout="vertical"
          onFinish={(values) => void handleInviteCaregiver(values)}
          disabled={inviting}
        >
          <Form.Item
            name="homeIds"
            label="Assign to Homes"
            rules={[{ required: true, message: 'Please select at least one home' }]}
            extra="Caregivers can only access clients in their assigned homes"
          >
            <Select
              mode="multiple"
              placeholder="Select homes to assign"
              optionFilterProp="label"
              options={homes.map((h) => ({
                label: (
                  <Flex align="center" gap={8}>
                    <HomeOutlined />
                    <span>{h.name}</span>
                    {!isSmallMobile && (
                      <Text type="secondary" style={{ fontSize: 12 }}>
                        ({h.city}, {h.state})
                      </Text>
                    )}
                  </Flex>
                ),
                value: h.id,
              }))}
            />
          </Form.Item>

          <Form.Item
            name="email"
            label="Email Address"
            rules={[
              { required: true, message: 'Please enter email address' },
              { type: 'email', message: 'Please enter a valid email address' },
            ]}
          >
            <Input prefix={<MailOutlined />} placeholder="caregiver@example.com" />
          </Form.Item>

          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="firstName"
                label="First Name"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Input placeholder="Maria" />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="lastName"
                label="Last Name"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Input placeholder="Santos" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Row gutter={8} justify="end">
              <Col xs={isMobile ? 12 : undefined}>
                <Button 
                  onClick={() => {
                    setInviteModalOpen(false);
                    inviteForm.resetFields();
                  }}
                  style={isMobile ? { width: '100%' } : undefined}
                >
                  Cancel
                </Button>
              </Col>
              <Col xs={isMobile ? 12 : undefined}>
                <Button 
                  type="primary" 
                  htmlType="submit" 
                  loading={inviting}
                  style={isMobile ? { width: '100%' } : undefined}
                >
                  {isSmallMobile ? 'Send' : 'Send Invitation'}
                </Button>
              </Col>
            </Row>
          </Form.Item>
        </Form>
      </Modal>

      {/* Caregiver Detail Modal */}
      <Modal
        title="Caregiver Details"
        open={detailModalOpen}
        onCancel={() => {
          setDetailModalOpen(false);
          setSelectedCaregiver(null);
        }}
        footer={null}
        width={isMobile ? '100%' : 640}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        {detailLoading ? (
          <Flex justify="center" style={{ padding: 48 }}>
            <Spin size="large" />
          </Flex>
        ) : selectedCaregiver ? (
          <div>
            <Flex align="center" gap={16} style={{ marginBottom: 24 }} wrap="wrap">
              <Avatar
                size={isSmallMobile ? 48 : 64}
                icon={<UserOutlined />}
                style={{ backgroundColor: '#5a7a6b' }}
              />
              <div style={{ flex: 1, minWidth: 0 }}>
                <Title level={isSmallMobile ? 5 : 4} style={{ margin: 0 }} ellipsis>{selectedCaregiver.fullName}</Title>
                <Text type="secondary" style={{ wordBreak: 'break-word' }}>{selectedCaregiver.email}</Text>
              </div>
            </Flex>

            <Descriptions column={isSmallMobile ? 1 : 2} style={{ marginBottom: 24 }} size={isSmallMobile ? 'small' : 'default'}>
              <Descriptions.Item label="Status">
                <Tag color={selectedCaregiver.isActive ? 'green' : 'default'}>
                  {selectedCaregiver.isActive ? 'Active' : 'Inactive'}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Invitation">
                {selectedCaregiver.invitationAccepted ? (
                  <Tag color="green" icon={<CheckCircleOutlined />}>Accepted</Tag>
                ) : (
                  <Tag color="orange" icon={<CloseCircleOutlined />}>Pending</Tag>
                )}
              </Descriptions.Item>
              <Descriptions.Item label="MFA Status">
                {selectedCaregiver.isMfaSetupComplete ? (
                  <Tag color="green">Configured</Tag>
                ) : (
                  <Tag color="orange">Not Set</Tag>
                )}
              </Descriptions.Item>
            </Descriptions>

            <Card
              title={
                <Row align="middle" gutter={8}>
                  <Col flex="auto">
                    <Space>
                      <HomeOutlined />
                      <span>Home Assignments</span>
                    </Space>
                  </Col>
                  <Col>
                    <Button
                      type="primary"
                      size="small"
                      icon={<PlusOutlined />}
                      onClick={handleOpenAssignModal}
                    >
                      {isSmallMobile ? 'Assign' : 'Assign Homes'}
                    </Button>
                  </Col>
                </Row>
              }
              size="small"
            >
              {selectedCaregiver.homeAssignments.filter((a) => a.isActive).length === 0 ? (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description="No homes assigned"
                >
                  <Button type="primary" onClick={handleOpenAssignModal}>
                    Assign First Home
                  </Button>
                </Empty>
              ) : (
                <List
                  dataSource={selectedCaregiver.homeAssignments.filter((a) => a.isActive)}
                  renderItem={(assignment) => (
                    <List.Item
                      actions={isSmallMobile ? undefined : [
                        <Popconfirm
                          key="remove"
                          title="Remove assignment?"
                          description="The caregiver will no longer have access to this home's clients."
                          onConfirm={() => void handleRemoveAssignment(assignment)}
                        >
                          <Button type="text" danger icon={<DeleteOutlined />} size="small">
                            Remove
                          </Button>
                        </Popconfirm>,
                      ]}
                    >
                      <List.Item.Meta
                        avatar={<HomeOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />}
                        title={<Link href={`/homes/${assignment.homeId}`}>{assignment.homeName}</Link>}
                        description={`Assigned ${new Date(assignment.assignedAt).toLocaleDateString()}`}
                      />
                      {isSmallMobile && (
                        <Popconfirm
                          key="remove-mobile"
                          title="Remove assignment?"
                          description="The caregiver will no longer have access to this home's clients."
                          onConfirm={() => void handleRemoveAssignment(assignment)}
                        >
                          <Button type="text" danger icon={<DeleteOutlined />} size="small" />
                        </Popconfirm>
                      )}
                    </List.Item>
                  )}
                />
              )}
            </Card>
          </div>
        ) : null}
      </Modal>

      {/* Assign Homes Modal */}
      <Modal
        title="Assign Homes"
        open={assignModalOpen}
        onCancel={() => setAssignModalOpen(false)}
        onOk={() => void handleAssignHomes()}
        confirmLoading={submitting}
        okText="Save"
        destroyOnHidden
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Paragraph type="secondary" style={{ marginBottom: 16 }}>
          Select the homes this caregiver should have access to.
        </Paragraph>
        <Select
          mode="multiple"
          style={{ width: '100%' }}
          placeholder="Select homes"
          value={selectedHomeIds}
          onChange={(value: string[]) => setSelectedHomeIds(value)}
          optionFilterProp="label"
          options={homes.map((h) => ({
            label: h.name,
            value: h.id,
          }))}
        />
      </Modal>
    </div>
  );
}

export default function CaregiversPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin']}>
      <AuthenticatedLayout>
        <CaregiversContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
