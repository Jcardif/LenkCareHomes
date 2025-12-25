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
  Select,
  DatePicker,
  message,
  Spin,
  Alert,
  Flex,
  Switch,
  Avatar,
  Row,
  Col,
  Grid,
  Dropdown,
} from 'antd';
import type { MenuProps } from 'antd';
import {
  UserOutlined,
  PlusOutlined,
  ReloadOutlined,
  FilterOutlined,
  MinusCircleOutlined,
  MoreOutlined,
  EyeOutlined,
} from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import dayjs from 'dayjs';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { useAuth } from '@/contexts/AuthContext';
import { clientsApi, homesApi, ApiError } from '@/lib/api';
import type { ClientSummary, AdmitClientRequest, HomeSummary, Bed } from '@/types';
import type { ColumnsType } from 'antd/es/table';

const { Title, Paragraph, Text } = Typography;
const { TextArea } = Input;
const { useBreakpoint } = Grid;

interface EmergencyContact {
  name?: string;
  phone?: string;
  relationship?: string;
}

interface AdmitClientFormData {
  firstName: string;
  lastName: string;
  dateOfBirth: dayjs.Dayjs;
  gender: string;
  admissionDate: dayjs.Dayjs;
  homeId: string;
  bedId: string;
  primaryPhysician?: string;
  primaryPhysicianPhone?: string;
  emergencyContacts?: EmergencyContact[];
  allergies?: string;
  diagnoses?: string;
  medicationList?: string;
  notes?: string;
}

function ClientsContent() {
  const router = useRouter();
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Admin');
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [clients, setClients] = useState<ClientSummary[]>([]);
  const [homes, setHomes] = useState<HomeSummary[]>([]);
  const [availableBeds, setAvailableBeds] = useState<Bed[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showInactive, setShowInactive] = useState(false);
  const [filterHomeId, setFilterHomeId] = useState<string | undefined>(undefined);

  const [modalOpen, setModalOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [form] = Form.useForm<AdmitClientFormData>();

  const selectedHomeId = Form.useWatch('homeId', form);

  const fetchClients = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await clientsApi.getAll({
        homeId: filterHomeId,
        isActive: showInactive ? undefined : true,
      });
      setClients(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load clients';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [filterHomeId, showInactive]);

  const fetchHomes = useCallback(async () => {
    try {
      const data = await homesApi.getAll(false);
      setHomes(data);
    } catch {
      // Silently fail - homes are optional for filtering
    }
  }, []);

  useEffect(() => {
    void fetchClients();
    void fetchHomes();
  }, [fetchClients, fetchHomes]);

  // Fetch available beds when home is selected in the form
  useEffect(() => {
    if (selectedHomeId && modalOpen) {
      void (async () => {
        try {
          const beds = await homesApi.getAvailableBeds(selectedHomeId);
          setAvailableBeds(beds);
        } catch {
          setAvailableBeds([]);
        }
      })();
    } else {
      setAvailableBeds([]);
    }
  }, [selectedHomeId, modalOpen]);

  const handleAdmit = () => {
    form.resetFields();
    form.setFieldValue('admissionDate', dayjs());
    setModalOpen(true);
  };

  const handleSubmit = async (values: AdmitClientFormData) => {
    try {
      setSubmitting(true);
      // Use first emergency contact for the primary fields
      const primaryContact = values.emergencyContacts?.[0];
      const request: AdmitClientRequest = {
        firstName: values.firstName,
        lastName: values.lastName,
        dateOfBirth: values.dateOfBirth.format('YYYY-MM-DD'),
        gender: values.gender,
        admissionDate: values.admissionDate.format('YYYY-MM-DD'),
        homeId: values.homeId,
        bedId: values.bedId,
        primaryPhysician: values.primaryPhysician,
        primaryPhysicianPhone: values.primaryPhysicianPhone,
        emergencyContactName: primaryContact?.name,
        emergencyContactPhone: primaryContact?.phone,
        emergencyContactRelationship: primaryContact?.relationship,
        allergies: values.allergies,
        diagnoses: values.diagnoses,
        medicationList: values.medicationList,
        notes: values.notes,
      };

      const result = await clientsApi.admit(request);
      if (result.success) {
        void message.success('Client admitted successfully');
        setModalOpen(false);
        form.resetFields();
        void fetchClients();
        if (result.client) {
          router.push(`/clients/${result.client.id}`);
        }
      } else {
        void message.error(result.error || 'Failed to admit client');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to admit client';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const calculateAge = (dateOfBirth: string): number => {
    return dayjs().diff(dayjs(dateOfBirth), 'year');
  };

  // Action menu items for mobile dropdown
  const getActionMenuItems = (record: ClientSummary): MenuProps['items'] => [
    {
      key: 'view',
      label: 'View Details',
      icon: <EyeOutlined />,
      onClick: () => router.push(`/clients/${record.id}`),
    },
  ];

  const columns: ColumnsType<ClientSummary> = [
    {
      title: 'Client',
      key: 'client',
      fixed: 'left',
      render: (_, record) => (
        <Flex align="center" gap={12}>
          <Avatar
            src={record.photoUrl}
            icon={<UserOutlined />}
            style={{ backgroundColor: '#5a7a6b', flexShrink: 0 }}
          />
          <div style={{ minWidth: 0 }}>
            <Text strong style={{ display: 'block', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {record.fullName}
            </Text>
            <Text type="secondary" style={{ fontSize: 12 }}>
              Age: {calculateAge(record.dateOfBirth)}
            </Text>
          </div>
        </Flex>
      ),
      sorter: (a, b) => a.lastName.localeCompare(b.lastName),
    },
    {
      title: 'Home',
      dataIndex: 'homeName',
      key: 'homeName',
      sorter: (a, b) => a.homeName.localeCompare(b.homeName),
      ellipsis: true,
    },
    {
      title: 'Bed',
      dataIndex: 'bedLabel',
      key: 'bedLabel',
      responsive: ['md'],
      render: (bedLabel) => bedLabel || <Text type="secondary">—</Text>,
    },
    {
      title: 'Admitted',
      dataIndex: 'admissionDate',
      key: 'admissionDate',
      responsive: ['md'],
      render: (date) => dayjs(date).format('MMM D, YYYY'),
      sorter: (a, b) => dayjs(a.admissionDate).unix() - dayjs(b.admissionDate).unix(),
    },
    {
      title: 'Allergies',
      dataIndex: 'allergies',
      key: 'allergies',
      responsive: ['lg'],
      render: (allergies) =>
        allergies ? (
          <Tag color="red">{allergies}</Tag>
        ) : (
          <Text type="secondary">None</Text>
        ),
    },
    {
      title: 'Status',
      key: 'status',
      responsive: ['lg'],
      render: (_, record) => (
        <Tag color={record.isActive ? 'green' : 'default'}>
          {record.isActive ? 'Active' : 'Discharged'}
        </Tag>
      ),
    },
    // Mobile action column
    ...(isMobile ? [{
      title: '',
      key: 'actions',
      width: 48,
      render: (_: unknown, record: ClientSummary) => (
        <Dropdown menu={{ items: getActionMenuItems(record) }} trigger={['click']}>
          <Button
            type="text"
            icon={<MoreOutlined />}
            onClick={(e) => e.stopPropagation()}
            style={{ minWidth: 44, minHeight: 44 }}
          />
        </Dropdown>
      ),
    }] as ColumnsType<ClientSummary> : []),
  ];

  if (loading && clients.length === 0) {
    return (
      <Flex justify="center" align="center" style={{ minHeight: 300 }}>
        <Spin size="large" tip="Loading clients..." />
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
          <Button size="small" onClick={() => void fetchClients()}>
            Retry
          </Button>
        }
      />
    );
  }

  return (
    <div>
      <div 
        className="page-header-wrapper"
        style={{ 
          display: 'flex', 
          flexDirection: isMobile ? 'column' : 'row',
          justifyContent: 'space-between', 
          alignItems: isMobile ? 'stretch' : 'center', 
          marginBottom: 24,
          gap: 16,
        }}
      >
        <div>
          <Title level={2} style={{ margin: 0, color: '#2d3732', fontSize: isMobile ? 20 : 24 }}>Clients</Title>
          <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
            Manage residents across all homes
          </Paragraph>
        </div>
        <Space style={{ flexDirection: isSmallMobile ? 'column' : 'row', width: isSmallMobile ? '100%' : 'auto' }}>
          <Button 
            icon={<ReloadOutlined />} 
            onClick={() => void fetchClients()}
            style={{ width: isSmallMobile ? '100%' : 'auto', minHeight: 44 }}
          >
            Refresh
          </Button>
          {isAdmin && (
            <Button 
              type="primary" 
              icon={<PlusOutlined />} 
              onClick={handleAdmit}
              style={{ width: isSmallMobile ? '100%' : 'auto', minHeight: 44 }}
            >
              Admit Client
            </Button>
          )}
        </Space>
      </div>

      <Card>
        <Flex 
          justify="space-between" 
          align={isMobile ? 'stretch' : 'center'} 
          style={{ marginBottom: 16 }} 
          wrap="wrap" 
          gap={16}
          vertical={isMobile}
        >
          <Space wrap style={{ width: isMobile ? '100%' : 'auto' }}>
            <FilterOutlined />
            <Select
              placeholder="Filter by home"
              allowClear
              style={{ width: isMobile ? '100%' : 200, minWidth: isMobile ? undefined : 200 }}
              value={filterHomeId}
              onChange={setFilterHomeId}
              options={homes.map((h) => ({ label: h.name, value: h.id }))}
            />
            <Space>
              <Switch
                checked={showInactive}
                onChange={setShowInactive}
              />
              <Text type="secondary">Show discharged</Text>
            </Space>
          </Space>
          <Text type="secondary">{clients.length} clients</Text>
        </Flex>

        {clients.length === 0 ? (
          <Empty
            image={<UserOutlined style={{ fontSize: 64, color: '#5a7a6b' }} />}
            imageStyle={{ height: 80 }}
            description={
              <div>
                <Title level={5} style={{ color: '#2d3732', marginBottom: 8 }}>
                  {filterHomeId || showInactive ? 'No clients found' : 'No active clients'}
                </Title>
                <Paragraph style={{ color: '#6b7770' }}>
                  {isAdmin
                    ? 'Admit your first client to begin tracking care activities.'
                    : 'No clients in your assigned homes.'}
                </Paragraph>
              </div>
            }
          >
            {isAdmin && (
              <Button type="primary" icon={<PlusOutlined />} onClick={handleAdmit}>
                Admit First Client
              </Button>
            )}
          </Empty>
        ) : (
          <Table
            columns={columns}
            dataSource={clients}
            rowKey="id"
            pagination={{ pageSize: 10 }}
            loading={loading}
            scroll={{ x: 'max-content' }}
            onRow={(record) => ({
              onClick: () => router.push(`/clients/${record.id}`),
              style: { cursor: 'pointer' },
            })}
          />
        )}
      </Card>

      {/* Admit Client Modal */}
      <Modal
        title="Admit New Client"
        open={modalOpen}
        onCancel={() => {
          setModalOpen(false);
          form.resetFields();
        }}
        footer={null}
        destroyOnHidden
        width={isMobile ? '95vw' : 900}
        style={{ maxWidth: 900 }}
        styles={{ body: { maxHeight: '70vh', overflowY: 'auto' } }}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={(values) => void handleSubmit(values)}
          disabled={submitting}
        >
          <Title level={5}>Personal Information</Title>
          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="firstName"
                label="First Name"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="lastName"
                label="Last Name"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Input />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="dateOfBirth"
                label="Date of Birth"
                rules={[{ required: true, message: 'Required' }]}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="gender"
                label="Gender"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Select
                  options={[
                    { label: 'Male', value: 'Male' },
                    { label: 'Female', value: 'Female' },
                    { label: 'Other', value: 'Other' },
                  ]}
                />
              </Form.Item>
            </Col>
          </Row>

          <Title level={5} style={{ marginTop: 24 }}>Placement</Title>
          <Row gutter={16}>
            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                name="homeId"
                label="Home"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Select
                  placeholder="Select a home"
                  options={homes.map((h) => ({
                    label: `${h.name} (${h.availableBeds} beds available)`,
                    value: h.id,
                    disabled: h.availableBeds === 0,
                  }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} lg={8}>
              <Form.Item
                name="bedId"
                label="Bed"
                rules={[{ required: true, message: 'Required' }]}
              >
                <Select
                  placeholder={selectedHomeId ? 'Select a bed' : 'Select home first'}
                  disabled={!selectedHomeId}
                  options={availableBeds.map((b) => ({
                    label: b.label,
                    value: b.id,
                  }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={24} lg={8}>
              <Form.Item
                name="admissionDate"
                label="Admission Date"
                rules={[{ required: true, message: 'Required' }]}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Title level={5} style={{ marginTop: 24 }}>Medical Information</Title>
          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item name="primaryPhysician" label="Primary Physician">
                <Input placeholder="Dr. Smith" />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="primaryPhysicianPhone" label="Physician Phone">
                <Input placeholder="(555) 123-4567" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item name="allergies" label="Allergies">
                <Input placeholder="Penicillin, Latex, etc." />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="diagnoses" label="Diagnoses">
                <Input placeholder="List primary diagnoses" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="medicationList" label="Current Medications">
            <TextArea rows={2} placeholder="List current medications" />
          </Form.Item>

          <Title level={5} style={{ marginTop: 24 }}>Emergency Contacts</Title>
          <Form.List name="emergencyContacts" initialValue={[{}]}>
            {(fields, { add, remove }) => (
              <>
                {fields.map(({ key, name, ...restField }, index) => (
                  <Row key={key} gutter={16} align="top">
                    <Col xs={24} sm={8}>
                      <Form.Item
                        {...restField}
                        name={[name, 'name']}
                        label={index === 0 ? 'Name' : undefined}
                        style={{ marginBottom: 12 }}
                      >
                        <Input placeholder="Contact name" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} sm={8}>
                      <Form.Item
                        {...restField}
                        name={[name, 'phone']}
                        label={index === 0 ? 'Phone' : undefined}
                        style={{ marginBottom: 12 }}
                      >
                        <Input placeholder="(555) 123-4567" />
                      </Form.Item>
                    </Col>
                    <Col xs={20} sm={6}>
                      <Form.Item
                        {...restField}
                        name={[name, 'relationship']}
                        label={index === 0 ? 'Relationship' : undefined}
                        style={{ marginBottom: 12 }}
                      >
                        <Input placeholder="Son, Daughter, etc." />
                      </Form.Item>
                    </Col>
                    <Col xs={4} sm={2} style={{ display: 'flex', alignItems: 'flex-end', paddingBottom: 12 }}>
                      {fields.length > 1 && (
                        <Button
                          type="text"
                          danger
                          icon={<MinusCircleOutlined />}
                          onClick={() => remove(name)}
                          style={{ marginTop: index === 0 ? 30 : 0 }}
                        />
                      )}
                    </Col>
                  </Row>
                ))}
                <Button
                  type="dashed"
                  onClick={() => add()}
                  icon={<PlusOutlined />}
                  style={{ marginBottom: 16 }}
                >
                  Add Emergency Contact
                </Button>
              </>
            )}
          </Form.List>

          <Form.Item name="notes" label="Additional Notes">
            <TextArea rows={3} />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex 
              justify="end" 
              gap={8}
              vertical={isSmallMobile}
            >
              <Button 
                onClick={() => setModalOpen(false)}
                style={{ width: isSmallMobile ? '100%' : 'auto', minHeight: 44 }}
              >
                Cancel
              </Button>
              <Button 
                type="primary" 
                htmlType="submit" 
                loading={submitting}
                style={{ width: isSmallMobile ? '100%' : 'auto', minHeight: 44 }}
              >
                Admit Client
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

export default function ClientsPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <ClientsContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
