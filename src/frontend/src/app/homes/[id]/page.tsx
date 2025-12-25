'use client';

import React, { useEffect, useState, useCallback, use } from 'react';
import {
  Typography,
  Card,
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
  Descriptions,
  Breadcrumb,
  Popconfirm,
  Tooltip,
  Switch,
  Grid,
  Row,
  Col,
  Dropdown,
} from 'antd';
import type { MenuProps } from 'antd';
import {
  HomeOutlined,
  PlusOutlined,
  EditOutlined,
  ArrowLeftOutlined,
  ReloadOutlined,
  StopOutlined,
  CheckCircleOutlined,
  UserOutlined,
  MoreOutlined,
} from '@ant-design/icons';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { homesApi, bedsApi, ApiError } from '@/lib/api';
import type { Home, Bed, CreateBedRequest, UpdateBedRequest } from '@/types';
import type { ColumnsType } from 'antd/es/table';

const { Title, Paragraph, Text } = Typography;
const { useBreakpoint } = Grid;

interface HomeDetailPageProps {
  params: Promise<{ id: string }>;
}

interface BedFormData {
  label: string;
}

function HomeDetailContent({ params }: HomeDetailPageProps) {
  const router = useRouter();
  const resolvedParams = use(params);
  const homeId = resolvedParams.id;

  // Responsive breakpoints
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [home, setHome] = useState<Home | null>(null);
  const [beds, setBeds] = useState<Bed[]>([]);
  const [loading, setLoading] = useState(true);
  const [bedsLoading, setBedsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [includeInactiveBeds, setIncludeInactiveBeds] = useState(false);

  const [bedModalOpen, setBedModalOpen] = useState(false);
  const [editingBed, setEditingBed] = useState<Bed | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [bedForm] = Form.useForm<BedFormData>();

  const [editHomeModalOpen, setEditHomeModalOpen] = useState(false);
  const [homeForm] = Form.useForm();

  const fetchHome = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await homesApi.getById(homeId);
      setHome(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load home';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [homeId]);

  const fetchBeds = useCallback(async () => {
    try {
      setBedsLoading(true);
      const data = await homesApi.getBeds(homeId, includeInactiveBeds);
      setBeds(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load beds';
      void message.error(msg);
    } finally {
      setBedsLoading(false);
    }
  }, [homeId, includeInactiveBeds]);

  useEffect(() => {
    void fetchHome();
  }, [fetchHome]);

  useEffect(() => {
    void fetchBeds();
  }, [fetchBeds]);

  const handleCreateBed = () => {
    setEditingBed(null);
    bedForm.resetFields();
    setBedModalOpen(true);
  };

  const handleEditBed = (bed: Bed) => {
    setEditingBed(bed);
    bedForm.setFieldsValue({ label: bed.label });
    setBedModalOpen(true);
  };

  const handleSubmitBed = async (values: BedFormData) => {
    try {
      setSubmitting(true);
      if (editingBed) {
        const request: UpdateBedRequest = { label: values.label };
        const result = await bedsApi.update(editingBed.id, request);
        if (result.success) {
          void message.success('Bed updated successfully');
        } else {
          void message.error(result.error || 'Failed to update bed');
          return;
        }
      } else {
        const request: CreateBedRequest = { label: values.label };
        const result = await homesApi.createBed(homeId, request);
        if (result.success) {
          void message.success('Bed created successfully');
        } else {
          void message.error(result.error || 'Failed to create bed');
          return;
        }
      }
      setBedModalOpen(false);
      bedForm.resetFields();
      setEditingBed(null);
      void fetchBeds();
      void fetchHome();
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Operation failed';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeactivateBed = async (bed: Bed) => {
    try {
      const result = await bedsApi.update(bed.id, { isActive: false });
      if (result.success) {
        void message.success('Bed deactivated successfully');
        void fetchBeds();
        void fetchHome();
      } else {
        void message.error(result.error || 'Failed to deactivate bed');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to deactivate bed';
      void message.error(msg);
    }
  };

  const handleReactivateBed = async (bed: Bed) => {
    try {
      const result = await bedsApi.update(bed.id, { isActive: true });
      if (result.success) {
        void message.success('Bed reactivated successfully');
        void fetchBeds();
        void fetchHome();
      } else {
        void message.error(result.error || 'Failed to reactivate bed');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to reactivate bed';
      void message.error(msg);
    }
  };

  const handleEditHome = () => {
    if (!home) return;
    homeForm.setFieldsValue({
      name: home.name,
      address: home.address,
      city: home.city,
      state: home.state,
      zipCode: home.zipCode,
      phoneNumber: home.phoneNumber,
      capacity: home.capacity,
    });
    setEditHomeModalOpen(true);
  };

  const handleUpdateHome = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);
      const result = await homesApi.update(homeId, values);
      if (result.success) {
        void message.success('Home updated successfully');
        setEditHomeModalOpen(false);
        void fetchHome();
      } else {
        void message.error(result.error || 'Failed to update home');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to update home';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  // Mobile action menu items for beds
  const getBedActionMenuItems = (record: Bed): MenuProps['items'] => {
    const items: MenuProps['items'] = [
      {
        key: 'edit',
        label: 'Edit',
        icon: <EditOutlined />,
        onClick: () => handleEditBed(record),
      },
    ];
    
    if (record.isActive) {
      items.push({
        key: 'deactivate',
        label: 'Deactivate',
        icon: <StopOutlined />,
        danger: true,
        disabled: record.status === 'Occupied',
      });
    } else {
      items.push({
        key: 'reactivate',
        label: 'Reactivate',
        icon: <CheckCircleOutlined />,
        onClick: () => void handleReactivateBed(record),
      });
    }
    
    return items;
  };

  const bedColumns: ColumnsType<Bed> = [
    {
      title: 'Label',
      dataIndex: 'label',
      key: 'label',
      sorter: (a, b) => a.label.localeCompare(b.label),
    },
    {
      title: 'Status',
      key: 'status',
      render: (_, record) => (
        <Space size={4} wrap>
          <Tag color={record.status === 'Available' ? 'green' : 'blue'}>
            {record.status}
          </Tag>
          {!record.isActive && <Tag>Inactive</Tag>}
        </Space>
      ),
    },
    {
      title: 'Occupant',
      key: 'occupant',
      responsive: ['md'] as const,
      render: (_, record) =>
        record.currentOccupantName ? (
          <Link href={`/clients/${record.currentOccupantId}`}>
            <Space>
              <UserOutlined />
              {record.currentOccupantName}
            </Space>
          </Link>
        ) : (
          <Text type="secondary">—</Text>
        ),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: isMobile ? 60 : 120,
      render: (_, record) =>
        isMobile ? (
          <Dropdown 
            menu={{ 
              items: getBedActionMenuItems(record),
              onClick: ({ key }) => {
                if (key === 'deactivate' && record.status !== 'Occupied') {
                  void handleDeactivateBed(record);
                }
              }
            }} 
            trigger={['click']}
          >
            <Button 
              type="text" 
              icon={<MoreOutlined />}
              style={{ minWidth: 44, minHeight: 44 }}
            />
          </Dropdown>
        ) : (
          <Space>
            <Tooltip title="Edit">
              <Button
                type="text"
                icon={<EditOutlined />}
                onClick={() => handleEditBed(record)}
              />
            </Tooltip>
            {record.isActive ? (
              <Popconfirm
                title="Deactivate bed"
                description={
                  record.status === 'Occupied'
                    ? 'This bed is currently occupied. Please discharge or transfer the client first.'
                    : 'Are you sure you want to deactivate this bed?'
                }
                onConfirm={() => void handleDeactivateBed(record)}
                okText="Yes"
                cancelText="No"
                okButtonProps={{ disabled: record.status === 'Occupied' }}
              >
                <Tooltip title={record.status === 'Occupied' ? 'Cannot deactivate occupied bed' : 'Deactivate'}>
                  <Button
                    type="text"
                    danger
                    icon={<StopOutlined />}
                    disabled={record.status === 'Occupied'}
                  />
                </Tooltip>
              </Popconfirm>
            ) : (
              <Tooltip title="Reactivate">
                <Button
                  type="text"
                  icon={<CheckCircleOutlined />}
                  onClick={() => void handleReactivateBed(record)}
                />
              </Tooltip>
            )}
          </Space>
        ),
    },
  ];

  if (loading) {
    return (
      <Flex justify="center" align="center" style={{ minHeight: 300 }}>
        <Spin size="large" tip="Loading home details..." />
      </Flex>
    );
  }

  if (error || !home) {
    return (
      <Alert
        message="Error"
        description={error || 'Home not found'}
        type="error"
        showIcon
        action={
          <Space>
            <Button size="small" onClick={() => void fetchHome()}>
              Retry
            </Button>
            <Button size="small" onClick={() => router.push('/homes')}>
              Back to Homes
            </Button>
          </Space>
        }
      />
    );
  }

  return (
    <div>
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <Link href="/homes"><HomeOutlined /> {!isSmallMobile && 'Homes'}</Link> },
          { title: isSmallMobile ? home.name.substring(0, 15) + (home.name.length > 15 ? '...' : '') : home.name },
        ]}
      />

      <Flex 
        justify="space-between" 
        align={isMobile ? 'stretch' : 'center'} 
        style={{ marginBottom: isMobile ? 16 : 24 }}
        vertical={isMobile}
        gap={isMobile ? 12 : 0}
      >
        <div style={{ minWidth: 0 }}>
          <Flex align="center" gap={8} wrap="wrap">
            <Button
              type="text"
              icon={<ArrowLeftOutlined />}
              onClick={() => router.push('/homes')}
              style={{ minWidth: 44, minHeight: 44 }}
            />
            <Title level={isMobile ? 4 : 2} style={{ margin: 0, color: '#2d3732' }} ellipsis>
              {home.name}
            </Title>
            <Tag color={home.isActive ? 'green' : 'default'}>
              {home.isActive ? 'Active' : 'Inactive'}
            </Tag>
          </Flex>
          {!isSmallMobile && (
            <Paragraph style={{ color: '#6b7770', marginBottom: 0, marginLeft: 40 }}>
              {home.address}, {home.city}, {home.state} {home.zipCode}
            </Paragraph>
          )}
        </div>
        {isMobile ? (
          <Flex gap={8}>
            <Button 
              icon={<ReloadOutlined />} 
              onClick={() => { void fetchHome(); void fetchBeds(); }}
              style={{ flex: 1, minHeight: 44 }}
            >
              Refresh
            </Button>
            <Button 
              icon={<EditOutlined />} 
              onClick={handleEditHome}
              style={{ flex: 1, minHeight: 44 }}
            >
              Edit
            </Button>
          </Flex>
        ) : (
          <Space>
            <Button icon={<ReloadOutlined />} onClick={() => { void fetchHome(); void fetchBeds(); }}>
              Refresh
            </Button>
            <Button icon={<EditOutlined />} onClick={handleEditHome}>
              Edit Home
            </Button>
          </Space>
        )}
      </Flex>

      {/* Home Stats */}
      <Card style={{ marginBottom: isMobile ? 16 : 24 }} size={isMobile ? 'small' : 'default'}>
        <Descriptions 
          title={!isMobile ? 'Home Information' : undefined} 
          column={{ xs: 2, sm: 2, md: 3 }}
          size={isMobile ? 'small' : 'default'}
        >
          {!isSmallMobile && (
            <Descriptions.Item label="Phone">{home.phoneNumber || '—'}</Descriptions.Item>
          )}
          <Descriptions.Item label="Capacity">{home.capacity}</Descriptions.Item>
          <Descriptions.Item label={isSmallMobile ? 'Clients' : 'Active Clients'}>{home.activeClients}</Descriptions.Item>
          <Descriptions.Item label={isSmallMobile ? 'Total' : 'Total Beds'}>{home.totalBeds}</Descriptions.Item>
          <Descriptions.Item label={isSmallMobile ? 'Avail.' : 'Available'}>
            <Tag color={home.availableBeds > 0 ? 'green' : 'default'}>
              {home.availableBeds}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Occupied">{home.occupiedBeds}</Descriptions.Item>
        </Descriptions>
        {isSmallMobile && home.address && (
          <Paragraph style={{ color: '#6b7770', marginBottom: 0, marginTop: 8, fontSize: 12 }}>
            {home.address}, {home.city}, {home.state} {home.zipCode}
          </Paragraph>
        )}
      </Card>

      {/* Beds Section */}
      <Card
        title="Beds"
        size={isMobile ? 'small' : 'default'}
        extra={
          isMobile ? (
            <Button 
              type="primary" 
              icon={<PlusOutlined />} 
              onClick={handleCreateBed}
              size="small"
            >
              Add
            </Button>
          ) : (
            <Space>
              <Space>
                <Switch
                  checked={includeInactiveBeds}
                  onChange={setIncludeInactiveBeds}
                  size="small"
                />
                <Text type="secondary">Show inactive</Text>
              </Space>
              <Button type="primary" icon={<PlusOutlined />} onClick={handleCreateBed}>
                Add Bed
              </Button>
            </Space>
          )
        }
      >
        {isMobile && (
          <Flex align="center" gap={8} style={{ marginBottom: 12 }}>
            <Switch
              checked={includeInactiveBeds}
              onChange={setIncludeInactiveBeds}
              size="small"
            />
            <Text type="secondary" style={{ fontSize: 12 }}>Show inactive beds</Text>
          </Flex>
        )}
        <Table
          columns={bedColumns}
          dataSource={beds}
          rowKey="id"
          loading={bedsLoading}
          pagination={false}
          size={isMobile ? 'small' : 'middle'}
          scroll={{ x: 'max-content' }}
          locale={{
            emptyText: (
              <Flex vertical align="center" style={{ padding: isMobile ? 24 : 32 }}>
                <HomeOutlined style={{ fontSize: isMobile ? 36 : 48, color: '#d9d9d9', marginBottom: 16 }} />
                <Text type="secondary">No beds in this home yet.</Text>
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  style={{ marginTop: 16, minHeight: 44 }}
                  onClick={handleCreateBed}
                >
                  Add First Bed
                </Button>
              </Flex>
            ),
          }}
        />
      </Card>

      {/* Bed Modal */}
      <Modal
        title={editingBed ? 'Edit Bed' : 'Add New Bed'}
        open={bedModalOpen}
        onCancel={() => {
          setBedModalOpen(false);
          bedForm.resetFields();
          setEditingBed(null);
        }}
        footer={null}
        destroyOnHidden
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Form
          form={bedForm}
          layout="vertical"
          onFinish={(values) => void handleSubmitBed(values)}
          disabled={submitting}
        >
          <Form.Item
            name="label"
            label="Bed Label"
            rules={[{ required: true, message: 'Please enter a bed label' }]}
          >
            <Input placeholder="e.g., Room 1A, Bed 1, etc." />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex justify="end" gap={8} vertical={isSmallMobile}>
              <Button 
                onClick={() => setBedModalOpen(false)}
                style={isSmallMobile ? { order: 2, minHeight: 44 } : undefined}
              >
                Cancel
              </Button>
              <Button 
                type="primary" 
                htmlType="submit" 
                loading={submitting}
                style={isSmallMobile ? { order: 1, minHeight: 44 } : undefined}
              >
                {editingBed ? 'Save Changes' : 'Add Bed'}
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>

      {/* Edit Home Modal */}
      <Modal
        title="Edit Home"
        open={editHomeModalOpen}
        onCancel={() => {
          setEditHomeModalOpen(false);
          homeForm.resetFields();
        }}
        footer={null}
        destroyOnHidden
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Form
          form={homeForm}
          layout="vertical"
          onFinish={(values) => void handleUpdateHome(values)}
          disabled={submitting}
        >
          <Form.Item
            name="name"
            label="Home Name"
            rules={[{ required: true, message: 'Please enter the home name' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="address"
            label="Address"
            rules={[{ required: true, message: 'Please enter the address' }]}
          >
            <Input />
          </Form.Item>

          <Row gutter={[16, 0]}>
            <Col xs={24} sm={10}>
              <Form.Item
                name="city"
                label="City"
                rules={[{ required: true }]}
              >
                <Input />
              </Form.Item>
            </Col>
            <Col xs={12} sm={7}>
              <Form.Item
                name="state"
                label="State"
                rules={[{ required: true }]}
              >
                <Input maxLength={2} />
              </Form.Item>
            </Col>
            <Col xs={12} sm={7}>
              <Form.Item
                name="zipCode"
                label="Zip Code"
                rules={[{ required: true }]}
              >
                <Input />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item name="phoneNumber" label="Phone Number">
            <Input />
          </Form.Item>

          <Form.Item
            name="capacity"
            label="Capacity"
            rules={[{ required: true }]}
            extra="Note: Reducing capacity will not remove existing beds."
          >
            <Input type="number" min={1} />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex justify="end" gap={8} vertical={isSmallMobile}>
              <Button 
                onClick={() => setEditHomeModalOpen(false)}
                style={isSmallMobile ? { order: 2, minHeight: 44 } : undefined}
              >
                Cancel
              </Button>
              <Button 
                type="primary" 
                htmlType="submit" 
                loading={submitting}
                style={isSmallMobile ? { order: 1, minHeight: 44 } : undefined}
              >
                Save Changes
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

export default function HomeDetailPage(props: HomeDetailPageProps) {
  return (
    <ProtectedRoute requiredRoles={['Admin']}>
      <AuthenticatedLayout>
        <HomeDetailContent {...props} />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
