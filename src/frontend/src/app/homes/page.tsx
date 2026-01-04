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
  InputNumber, 
  message, 
  Spin, 
  Alert,
  Flex,
  Switch,
  Popconfirm,
  Tooltip,
  Select,
  Grid,
  Row,
  Col,
  Dropdown,
} from 'antd';
import { 
  HomeOutlined, 
  PlusOutlined, 
  EditOutlined, 
  ReloadOutlined,
  StopOutlined,
  CheckCircleOutlined,
  MoreOutlined,
} from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import { ProtectedRoute, AuthenticatedLayout, AddressAutocomplete } from '@/components';
import { homesApi, ApiError } from '@/lib/api';
import { getStateSelectOptions } from '@/lib/constants/usStates';
import type { AddressSuggestion } from '@/lib/azureMaps';
import type { HomeSummary, CreateHomeRequest, UpdateHomeRequest } from '@/types';
import type { ColumnsType } from 'antd/es/table';
import type { MenuProps } from 'antd';

const { Title, Paragraph } = Typography;
const { useBreakpoint } = Grid;

interface HomeFormData {
  name: string;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  phoneNumber?: string;
  capacity: number;
}

function HomesContent() {
  const router = useRouter();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;
  
  const [homes, setHomes] = useState<HomeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [includeInactive, setIncludeInactive] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingHome, setEditingHome] = useState<HomeSummary | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [form] = Form.useForm<HomeFormData>();
  
  // Get state options for dropdown
  const stateOptions = getStateSelectOptions();

  // Handle address selection from autocomplete
  const handleAddressSelect = useCallback((suggestion: AddressSuggestion) => {
    form.setFieldsValue({
      address: suggestion.streetAddress || suggestion.address,
      city: suggestion.city,
      state: suggestion.state,
      zipCode: suggestion.zipCode,
    });
  }, [form]);

  const fetchHomes = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await homesApi.getAll(includeInactive);
      setHomes(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load homes';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [includeInactive]);

  useEffect(() => {
    void fetchHomes();
  }, [fetchHomes]);

  const handleCreate = () => {
    setEditingHome(null);
    form.resetFields();
    setModalOpen(true);
  };

  const handleEdit = async (home: HomeSummary) => {
    try {
      const fullHome = await homesApi.getById(home.id);
      setEditingHome(home);
      form.setFieldsValue({
        name: fullHome.name,
        address: fullHome.address,
        city: fullHome.city,
        state: fullHome.state,
        zipCode: fullHome.zipCode,
        phoneNumber: fullHome.phoneNumber,
        capacity: fullHome.capacity,
      });
      setModalOpen(true);
    } catch {
      void message.error('Failed to load home details');
    }
  };

  const handleSubmit = async (values: HomeFormData) => {
    try {
      setSubmitting(true);
      if (editingHome) {
        const request: UpdateHomeRequest = values;
        const result = await homesApi.update(editingHome.id, request);
        if (result.success) {
          void message.success('Home updated successfully');
        } else {
          void message.error(result.error || 'Failed to update home');
          return;
        }
      } else {
        const request: CreateHomeRequest = values;
        const result = await homesApi.create(request);
        if (result.success) {
          void message.success('Home created successfully');
        } else {
          void message.error(result.error || 'Failed to create home');
          return;
        }
      }
      setModalOpen(false);
      form.resetFields();
      setEditingHome(null);
      void fetchHomes();
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Operation failed';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeactivate = async (home: HomeSummary) => {
    try {
      const result = await homesApi.deactivate(home.id);
      if (result.success) {
        void message.success('Home deactivated successfully');
        void fetchHomes();
      } else {
        void message.error(result.error || 'Failed to deactivate home');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to deactivate home';
      void message.error(msg);
    }
  };

  const handleReactivate = async (home: HomeSummary) => {
    try {
      const result = await homesApi.reactivate(home.id);
      if (result.success) {
        void message.success('Home reactivated successfully');
        void fetchHomes();
      } else {
        void message.error(result.error || 'Failed to reactivate home');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to reactivate home';
      void message.error(msg);
    }
  };

  // Action menu for mobile dropdown
  const getActionMenuItems = (record: HomeSummary): MenuProps['items'] => [
    {
      key: 'edit',
      label: 'Edit',
      icon: <EditOutlined />,
      onClick: () => void handleEdit(record),
    },
    {
      type: 'divider',
    },
    record.isActive ? {
      key: 'deactivate',
      label: 'Deactivate',
      icon: <StopOutlined />,
      danger: true,
      onClick: () => void handleDeactivate(record),
    } : {
      key: 'reactivate',
      label: 'Reactivate',
      icon: <CheckCircleOutlined />,
      onClick: () => void handleReactivate(record),
    },
  ];

  const columns: ColumnsType<HomeSummary> = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
      sorter: (a, b) => a.name.localeCompare(b.name),
      ellipsis: true,
    },
    {
      title: 'Location',
      key: 'location',
      responsive: ['sm'],
      render: (_, record) => `${record.city}, ${record.state}`,
    },
    {
      title: 'Capacity',
      dataIndex: 'capacity',
      key: 'capacity',
      responsive: ['md'],
      sorter: (a, b) => a.capacity - b.capacity,
    },
    {
      title: 'Available Beds',
      dataIndex: 'availableBeds',
      key: 'availableBeds',
      render: (availableBeds, record) => (
        <Tag color={availableBeds > 0 ? 'green' : 'default'}>
          {availableBeds} / {record.capacity}
        </Tag>
      ),
    },
    {
      title: 'Clients',
      dataIndex: 'activeClients',
      key: 'activeClients',
      responsive: ['lg'],
    },
    {
      title: 'Status',
      key: 'status',
      responsive: ['lg'],
      render: (_, record) => (
        <Tag color={record.isActive ? 'green' : 'default'}>
          {record.isActive ? 'Active' : 'Inactive'}
        </Tag>
      ),
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
          <Space onClick={(e) => e.stopPropagation()}>
            <Tooltip title="Edit">
              <Button
                type="text"
                icon={<EditOutlined />}
                onClick={() => void handleEdit(record)}
              />
            </Tooltip>
            {record.isActive ? (
              <Popconfirm
                title="Deactivate home"
                description="Are you sure you want to deactivate this home?"
                onConfirm={() => void handleDeactivate(record)}
                okText="Yes"
                cancelText="No"
              >
                <Tooltip title="Deactivate">
                  <Button type="text" danger icon={<StopOutlined />} />
                </Tooltip>
              </Popconfirm>
            ) : (
              <Tooltip title="Reactivate">
                <Button
                  type="text"
                  icon={<CheckCircleOutlined />}
                  onClick={() => void handleReactivate(record)}
                />
              </Tooltip>
            )}
          </Space>
        )
      ),
    },
  ];

  if (loading) {
    return (
      <Flex justify="center" align="center" style={{ minHeight: 300 }}>
        <Spin size="large" tip="Loading homes..." />
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
          <Button size="small" onClick={() => void fetchHomes()}>
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
          <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>Homes</Title>
          {!isSmallMobile && (
            <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
              Manage adult family homes and their beds
            </Paragraph>
          )}
        </div>
        <div className="page-header-actions">
          {!isMobile && (
            <Button icon={<ReloadOutlined />} onClick={() => void fetchHomes()}>
              Refresh
            </Button>
          )}
          <Button 
            type="primary" 
            icon={<PlusOutlined />} 
            onClick={handleCreate}
            style={isSmallMobile ? { width: '100%' } : undefined}
          >
            {isSmallMobile ? 'Add' : 'Add Home'}
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
          {isMobile && (
            <Col xs={24} sm={12}>
              <Button 
                icon={<ReloadOutlined />} 
                onClick={() => void fetchHomes()}
                style={{ width: '100%' }}
              >
                Refresh
              </Button>
            </Col>
          )}
        </Row>

        {homes.length === 0 ? (
          <Empty
            image={<HomeOutlined style={{ fontSize: 64, color: '#5a7a6b' }} />}
            imageStyle={{ height: 80 }}
            description={
              <div>
                <Title level={5} style={{ color: '#2d3732', marginBottom: 8 }}>No homes yet</Title>
                <Paragraph style={{ color: '#6b7770' }}>
                  Add your first adult family home to get started with managing residents and care activities.
                </Paragraph>
              </div>
            }
          >
            <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
              Add Your First Home
            </Button>
          </Empty>
        ) : (
          <Table
            columns={columns}
            dataSource={homes}
            rowKey="id"
            scroll={{ x: 'max-content' }}
            pagination={{ 
              pageSize: 10,
              showSizeChanger: !isMobile,
              showTotal: isMobile ? undefined : (total, range) => `${range[0]}-${range[1]} of ${total} homes`,
              size: isMobile ? 'small' : 'default',
            }}
            onRow={(record) => ({
              onClick: () => router.push(`/homes/${record.id}`),
              style: { cursor: 'pointer' },
            })}
          />
        )}
      </Card>

      <Modal
        title={editingHome ? 'Edit Home' : 'Add New Home'}
        open={modalOpen}
        onCancel={() => {
          setModalOpen(false);
          form.resetFields();
          setEditingHome(null);
        }}
        footer={null}
        destroyOnHidden
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={(values) => void handleSubmit(values)}
          disabled={submitting}
        >
          <Form.Item
            name="name"
            label="Home Name"
            rules={[{ required: true, message: 'Please enter the home name' }]}
          >
            <Input placeholder="e.g., Sunshine Family Home" />
          </Form.Item>

          <Form.Item
            name="address"
            label="Address"
            rules={[{ required: true, message: 'Please enter the address' }]}
            tooltip="Start typing to see address suggestions"
          >
            <AddressAutocomplete 
              placeholder="Start typing an address..."
              onAddressSelect={handleAddressSelect}
            />
          </Form.Item>

          <Row gutter={12}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="city"
                label="City"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Input placeholder="Seattle" />
              </Form.Item>
            </Col>
            <Col xs={12} sm={6}>
              <Form.Item
                name="state"
                label="State"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Select
                  placeholder="Select"
                  options={stateOptions}
                  showSearch
                  filterOption={(input, option) =>
                    (option?.label ?? '').toLowerCase().includes(input.toLowerCase())
                  }
                />
              </Form.Item>
            </Col>
            <Col xs={12} sm={6}>
              <Form.Item
                name="zipCode"
                label="Zip Code"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Input placeholder="98101" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            name="phoneNumber"
            label="Phone Number"
          >
            <Input placeholder="(555) 123-4567" />
          </Form.Item>

          <Form.Item
            name="capacity"
            label="Capacity (Number of Beds)"
            initialValue={6}
            rules={[
              { required: true, message: 'Please enter the capacity' },
              { type: 'number', min: 1, message: 'Capacity must be at least 1' },
            ]}
          >
            <InputNumber 
              min={1} 
              precision={0}
              keyboard={true}
              controls={true}
              style={{ width: '100%' }} 
            />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Row gutter={8} justify="end">
              <Col xs={isMobile ? 12 : undefined}>
                <Button 
                  onClick={() => setModalOpen(false)}
                  style={isMobile ? { width: '100%' } : undefined}
                >
                  Cancel
                </Button>
              </Col>
              <Col xs={isMobile ? 12 : undefined}>
                <Button 
                  type="primary" 
                  htmlType="submit" 
                  loading={submitting}
                  style={isMobile ? { width: '100%' } : undefined}
                >
                  {editingHome ? 'Save Changes' : 'Create Home'}
                </Button>
              </Col>
            </Row>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

export default function HomesPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin']}>
      <AuthenticatedLayout>
        <HomesContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
