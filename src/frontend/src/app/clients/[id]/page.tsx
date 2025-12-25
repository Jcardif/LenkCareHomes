'use client';

import React, { useEffect, useState, useCallback, use } from 'react';
import {
  Typography,
  Card,
  Button,
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
  Descriptions,
  Breadcrumb,
  Avatar,
  Divider,
  Tabs,
  Grid,
  Row,
  Col,
  Dropdown,
} from 'antd';
import type { MenuProps } from 'antd';
import {
  UserOutlined,
  EditOutlined,
  ArrowLeftOutlined,
  ReloadOutlined,
  SwapOutlined,
  StopOutlined,
  HomeOutlined,
  MedicineBoxOutlined,
  ContactsOutlined,
  FileTextOutlined,
  PlusOutlined,
  HeartOutlined,
  SmileOutlined,
  TeamOutlined,
  HistoryOutlined,
  CheckSquareOutlined,
  AimOutlined,
  WarningOutlined,
  FolderOutlined,
  MoreOutlined,
  CalendarOutlined,
} from '@ant-design/icons';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import dayjs from 'dayjs';
import {
  ProtectedRoute,
  AuthenticatedLayout,
  ADLLogTab,
  VitalsLogTab,
  MedicationLogTab,
  ROMLogTab,
  BehaviorNotesTab,
  ActivitiesTab,
  TimelineTab,
  QuickLogModal,
  IncidentList,
  DocumentList,
  DocumentUpload,
  AppointmentsTab,
} from '@/components';
import { useAuth } from '@/contexts/AuthContext';
import { clientsApi, homesApi, ApiError } from '@/lib/api';
import type { Client, UpdateClientRequest, Bed, HomeSummary } from '@/types';

const { Title, Paragraph, Text } = Typography;
const { TextArea } = Input;
const { useBreakpoint } = Grid;

interface ClientDetailPageProps {
  params: Promise<{ id: string }>;
}

function ClientDetailContent({ params }: ClientDetailPageProps) {
  const router = useRouter();
  const resolvedParams = use(params);
  const clientId = resolvedParams.id;
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Admin');

  // Responsive breakpoints
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [client, setClient] = useState<Client | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editModalOpen, setEditModalOpen] = useState(false);
  const [dischargeModalOpen, setDischargeModalOpen] = useState(false);
  const [transferModalOpen, setTransferModalOpen] = useState(false);
  const [quickLogModalOpen, setQuickLogModalOpen] = useState(false);
  const [quickLogDefaultTab, setQuickLogDefaultTab] = useState('adl');
  const [documentUploadOpen, setDocumentUploadOpen] = useState(false);
  const [documentsRefreshKey, setDocumentsRefreshKey] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [careLogRefreshKey, setCareLogRefreshKey] = useState(0);

  const [homes, setHomes] = useState<HomeSummary[]>([]);
  const [availableBeds, setAvailableBeds] = useState<Bed[]>([]);
  const [selectedHomeId, setSelectedHomeId] = useState<string | undefined>(undefined);

  const [editForm] = Form.useForm();
  const [dischargeForm] = Form.useForm();
  const [transferForm] = Form.useForm();

  const fetchClient = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await clientsApi.getById(clientId);
      setClient(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load client';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [clientId]);

  const fetchHomes = useCallback(async () => {
    try {
      const data = await homesApi.getAll(false);
      setHomes(data);
    } catch {
      // Silently fail
    }
  }, []);

  useEffect(() => {
    void fetchClient();
    void fetchHomes();
  }, [fetchClient, fetchHomes]);

  // Fetch available beds when home is selected for transfer
  useEffect(() => {
    if (selectedHomeId && transferModalOpen) {
      void (async () => {
        try {
          const beds = await homesApi.getAvailableBeds(selectedHomeId);
          // Add current bed if in the same home
          if (selectedHomeId === client?.homeId && client?.bedId) {
            const currentBed = await homesApi.getBeds(selectedHomeId, false);
            const curr = currentBed.find((b) => b.id === client.bedId);
            if (curr && !beds.find((b) => b.id === curr.id)) {
              beds.push(curr);
            }
          }
          setAvailableBeds(beds);
        } catch {
          setAvailableBeds([]);
        }
      })();
    } else {
      setAvailableBeds([]);
    }
  }, [selectedHomeId, transferModalOpen, client]);

  const calculateAge = (dateOfBirth: string): number => {
    return dayjs().diff(dayjs(dateOfBirth), 'year');
  };

  const handleEdit = () => {
    if (!client) return;
    editForm.setFieldsValue({
      firstName: client.firstName,
      lastName: client.lastName,
      dateOfBirth: dayjs(client.dateOfBirth),
      gender: client.gender,
      primaryPhysician: client.primaryPhysician,
      primaryPhysicianPhone: client.primaryPhysicianPhone,
      emergencyContactName: client.emergencyContactName,
      emergencyContactPhone: client.emergencyContactPhone,
      emergencyContactRelationship: client.emergencyContactRelationship,
      allergies: client.allergies,
      diagnoses: client.diagnoses,
      medicationList: client.medicationList,
      notes: client.notes,
    });
    setEditModalOpen(true);
  };

  const handleUpdateClient = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);
      const request: UpdateClientRequest = {
        firstName: values.firstName as string,
        lastName: values.lastName as string,
        dateOfBirth: (values.dateOfBirth as dayjs.Dayjs).format('YYYY-MM-DD'),
        gender: values.gender as string,
        primaryPhysician: values.primaryPhysician as string | undefined,
        primaryPhysicianPhone: values.primaryPhysicianPhone as string | undefined,
        emergencyContactName: values.emergencyContactName as string | undefined,
        emergencyContactPhone: values.emergencyContactPhone as string | undefined,
        emergencyContactRelationship: values.emergencyContactRelationship as string | undefined,
        allergies: values.allergies as string | undefined,
        diagnoses: values.diagnoses as string | undefined,
        medicationList: values.medicationList as string | undefined,
        notes: values.notes as string | undefined,
      };

      const result = await clientsApi.update(clientId, request);
      if (result.success) {
        void message.success('Client updated successfully');
        setEditModalOpen(false);
        void fetchClient();
      } else {
        void message.error(result.error || 'Failed to update client');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to update client';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDischarge = () => {
    dischargeForm.resetFields();
    dischargeForm.setFieldValue('dischargeDate', dayjs());
    setDischargeModalOpen(true);
  };

  const handleConfirmDischarge = async (values: { dischargeDate: dayjs.Dayjs; dischargeReason: string }) => {
    try {
      setSubmitting(true);
      const result = await clientsApi.discharge(clientId, {
        dischargeDate: values.dischargeDate.format('YYYY-MM-DD'),
        dischargeReason: values.dischargeReason,
      });
      if (result.success) {
        void message.success('Client discharged successfully');
        setDischargeModalOpen(false);
        void fetchClient();
      } else {
        void message.error(result.error || 'Failed to discharge client');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to discharge client';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleTransfer = () => {
    transferForm.resetFields();
    setSelectedHomeId(client?.homeId);
    transferForm.setFieldValue('homeId', client?.homeId);
    setTransferModalOpen(true);
  };

  const handleConfirmTransfer = async (values: { newBedId: string }) => {
    try {
      setSubmitting(true);
      const result = await clientsApi.transfer(clientId, {
        newBedId: values.newBedId,
      });
      if (result.success) {
        void message.success('Client transferred successfully');
        setTransferModalOpen(false);
        void fetchClient();
      } else {
        void message.error(result.error || 'Failed to transfer client');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to transfer client';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  // Mobile action menu items
  const getActionMenuItems = (): MenuProps['items'] => {
    const items: MenuProps['items'] = [
      {
        key: 'refresh',
        label: 'Refresh',
        icon: <ReloadOutlined />,
        onClick: () => void fetchClient(),
      },
    ];
    
    if (isAdmin && client?.isActive) {
      items.push(
        {
          key: 'edit',
          label: 'Edit',
          icon: <EditOutlined />,
          onClick: handleEdit,
        },
        {
          key: 'transfer',
          label: 'Transfer',
          icon: <SwapOutlined />,
          onClick: handleTransfer,
        },
        {
          type: 'divider',
        },
        {
          key: 'discharge',
          label: 'Discharge',
          icon: <StopOutlined />,
          danger: true,
          onClick: handleDischarge,
        }
      );
    }
    
    return items;
  };

  if (loading) {
    return (
      <Flex justify="center" align="center" style={{ minHeight: 300 }}>
        <Spin size="large" tip="Loading client details..." />
      </Flex>
    );
  }

  if (error || !client) {
    return (
      <Alert
        message="Error"
        description={error || 'Client not found'}
        type="error"
        showIcon
        action={
          <Space>
            <Button size="small" onClick={() => void fetchClient()}>
              Retry
            </Button>
            <Button size="small" onClick={() => router.push('/clients')}>
              Back to Clients
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
          { title: <Link href="/clients"><UserOutlined /> {!isSmallMobile && 'Clients'}</Link> },
          { title: isSmallMobile ? client.lastName : client.fullName },
        ]}
      />

      <Card style={{ marginBottom: isMobile ? 16 : 24 }}>
        <Flex 
          justify="space-between" 
          align={isMobile ? 'stretch' : 'flex-start'} 
          wrap="wrap" 
          gap={isMobile ? 12 : 16}
          vertical={isMobile}
        >
          <Flex align="center" gap={isMobile ? 12 : 16}>
            <Button
              type="text"
              icon={<ArrowLeftOutlined />}
              onClick={() => router.push('/clients')}
              style={{ minWidth: 44, minHeight: 44 }}
            />
            <Avatar
              size={isMobile ? 48 : 64}
              src={client.photoUrl}
              icon={<UserOutlined />}
              style={{ backgroundColor: '#5a7a6b', flexShrink: 0 }}
            />
            <div style={{ minWidth: 0, flex: 1 }}>
              <Flex align="center" gap={8} wrap="wrap">
                <Title level={isMobile ? 4 : 3} style={{ margin: 0, color: '#2d3732' }} ellipsis>
                  {client.fullName}
                </Title>
                <Tag color={client.isActive ? 'green' : 'default'}>
                  {client.isActive ? 'Active' : 'Discharged'}
                </Tag>
              </Flex>
              <Text type="secondary" style={{ fontSize: isSmallMobile ? 12 : 14 }}>
                Age: {calculateAge(client.dateOfBirth)} • DOB: {dayjs(client.dateOfBirth).format(isSmallMobile ? 'MMM D, YYYY' : 'MMMM D, YYYY')}
              </Text>
            </div>
          </Flex>

          {isMobile ? (
            <Flex gap={8} style={{ marginTop: 4 }}>
              <Button 
                icon={<ReloadOutlined />} 
                onClick={() => void fetchClient()}
                style={{ minHeight: 44, flex: 1 }}
              >
                Refresh
              </Button>
              {isAdmin && client.isActive && (
                <Dropdown menu={{ items: getActionMenuItems() }} trigger={['click']}>
                  <Button icon={<MoreOutlined />} style={{ minWidth: 44, minHeight: 44 }}>
                    Actions
                  </Button>
                </Dropdown>
              )}
            </Flex>
          ) : (
            <Space>
              <Button icon={<ReloadOutlined />} onClick={() => void fetchClient()}>
                Refresh
              </Button>
              {isAdmin && client.isActive && (
                <>
                  <Button icon={<EditOutlined />} onClick={handleEdit}>
                    Edit
                  </Button>
                  <Button icon={<SwapOutlined />} onClick={handleTransfer}>
                    Transfer
                  </Button>
                  <Button danger icon={<StopOutlined />} onClick={handleDischarge}>
                    Discharge
                  </Button>
                </>
              )}
            </Space>
          )}
        </Flex>
      </Card>

      <Tabs
        defaultActiveKey="overview"
        size={isMobile ? 'small' : 'middle'}
        tabBarStyle={{ marginBottom: isMobile ? 12 : 16 }}
        items={[
          {
            key: 'overview',
            label: (
              <span>
                <FileTextOutlined />
                {!isSmallMobile && ' Overview'}
              </span>
            ),
            children: (
              <Space direction="vertical" size={isMobile ? 12 : 16} style={{ width: '100%' }}>
                {/* Placement Info */}
                <Card
                  title={
                    <Space>
                      <HomeOutlined />
                      Placement
                    </Space>
                  }
                  size={isMobile ? 'small' : 'default'}
                >
                  <Descriptions column={{ xs: 1, sm: 2 }} size={isMobile ? 'small' : 'default'}>
                    <Descriptions.Item label="Home">
                      <Link href={`/homes/${client.homeId}`}>{client.homeName}</Link>
                    </Descriptions.Item>
                    <Descriptions.Item label="Bed">{client.bedLabel || '—'}</Descriptions.Item>
                    <Descriptions.Item label={isSmallMobile ? 'Admitted' : 'Admission Date'}>
                      {dayjs(client.admissionDate).format(isSmallMobile ? 'MMM D, YYYY' : 'MMMM D, YYYY')}
                    </Descriptions.Item>
                    {client.dischargeDate && (
                      <>
                        <Descriptions.Item label={isSmallMobile ? 'Discharged' : 'Discharge Date'}>
                          {dayjs(client.dischargeDate).format(isSmallMobile ? 'MMM D, YYYY' : 'MMMM D, YYYY')}
                        </Descriptions.Item>
                        <Descriptions.Item label="Reason">
                          {client.dischargeReason}
                        </Descriptions.Item>
                      </>
                    )}
                  </Descriptions>
                </Card>

                {/* Medical Info */}
                <Card
                  title={
                    <Space>
                      <MedicineBoxOutlined />
                      {isSmallMobile ? 'Medical' : 'Medical Information'}
                    </Space>
                  }
                  size={isMobile ? 'small' : 'default'}
                >
                  {client.allergies && (
                    <Alert
                      type="warning"
                      message="Allergies"
                      description={client.allergies}
                      style={{ marginBottom: 16 }}
                    />
                  )}
                  <Descriptions column={1} size={isMobile ? 'small' : 'default'}>
                    <Descriptions.Item label={isSmallMobile ? 'Physician' : 'Primary Physician'}>
                      {client.primaryPhysician || '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label={isSmallMobile ? 'Phone' : 'Physician Phone'}>
                      {client.primaryPhysicianPhone || '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label="Diagnoses">
                      {client.diagnoses || '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label={isSmallMobile ? 'Medications' : 'Current Medications'}>
                      {client.medicationList || '—'}
                    </Descriptions.Item>
                  </Descriptions>
                </Card>

                {/* Emergency Contact */}
                <Card
                  title={
                    <Space>
                      <ContactsOutlined />
                      {isSmallMobile ? 'Emergency' : 'Emergency Contact'}
                    </Space>
                  }
                  size={isMobile ? 'small' : 'default'}
                >
                  <Descriptions column={{ xs: 1, sm: 3 }} size={isMobile ? 'small' : 'default'}>
                    <Descriptions.Item label="Name">
                      {client.emergencyContactName || '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label="Phone">
                      {client.emergencyContactPhone || '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label={isSmallMobile ? 'Relation' : 'Relationship'}>
                      {client.emergencyContactRelationship || '—'}
                    </Descriptions.Item>
                  </Descriptions>
                </Card>

                {/* Notes */}
                {client.notes && (
                  <Card title="Notes" size={isMobile ? 'small' : 'default'}>
                    <Paragraph style={{ marginBottom: 0 }}>{client.notes}</Paragraph>
                  </Card>
                )}
              </Space>
            ),
          },
          {
            key: 'care-log',
            label: (
              <span>
                <FileTextOutlined />
                {!isSmallMobile && ' Care Log'}
              </span>
            ),
            children: (
              <Space direction="vertical" size={isMobile ? 12 : 16} style={{ width: '100%' }}>
                {/* Quick Log Button */}
                {client.isActive && (
                  <Card size="small">
                    <Flex 
                      justify="space-between" 
                      align="center" 
                      gap={12}
                      vertical={isSmallMobile}
                    >
                      <Text style={{ textAlign: isSmallMobile ? 'center' : 'left' }}>
                        {isSmallMobile ? `Log care for ${client.firstName}` : `Quickly log care activities for ${client.firstName}`}
                      </Text>
                      <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={() => {
                          setQuickLogDefaultTab('adl');
                          setQuickLogModalOpen(true);
                        }}
                        style={{ minHeight: 44, width: isSmallMobile ? '100%' : 'auto' }}
                      >
                        Quick Log
                      </Button>
                    </Flex>
                  </Card>
                )}

                {/* Care Log Sub-Tabs */}
                <Card size={isMobile ? 'small' : 'default'}>
                  <Tabs
                    defaultActiveKey="timeline"
                    size={isMobile ? 'small' : 'middle'}
                    tabBarGutter={isMobile ? 8 : 16}
                    items={[
                      {
                        key: 'timeline',
                        label: (
                          <span>
                            <HistoryOutlined />
                            {!isSmallMobile && ' Timeline'}
                          </span>
                        ),
                        children: (
                          <TimelineTab
                            key={`timeline-${careLogRefreshKey}`}
                            clientId={clientId}
                            clientName={client.fullName}
                          />
                        ),
                      },
                      {
                        key: 'adl',
                        label: (
                          <span>
                            <CheckSquareOutlined />
                            {!isSmallMobile && ' ADL'}
                          </span>
                        ),
                        children: (
                          <ADLLogTab
                            key={`adl-${careLogRefreshKey}`}
                            clientId={clientId}
                            clientName={client.fullName}
                          />
                        ),
                      },
                      {
                        key: 'vitals',
                        label: (
                          <span>
                            <HeartOutlined />
                            {!isSmallMobile && ' Vitals'}
                          </span>
                        ),
                        children: (
                          <VitalsLogTab
                            key={`vitals-${careLogRefreshKey}`}
                            clientId={clientId}
                            clientName={client.fullName}
                          />
                        ),
                      },
                      {
                        key: 'medications',
                        label: (
                          <span>
                            <MedicineBoxOutlined />
                            {!isSmallMobile && ' Meds'}
                          </span>
                        ),
                        children: (
                          <MedicationLogTab
                            key={`medications-${careLogRefreshKey}`}
                            clientId={clientId}
                            clientName={client.fullName}
                          />
                        ),
                      },
                      {
                        key: 'rom',
                        label: (
                          <span>
                            <AimOutlined />
                            {!isSmallMobile && ' ROM'}
                          </span>
                        ),
                        children: (
                          <ROMLogTab
                            key={`rom-${careLogRefreshKey}`}
                            clientId={clientId}
                            clientName={client.fullName}
                          />
                        ),
                      },
                      {
                        key: 'behavior',
                        label: (
                          <span>
                            <SmileOutlined />
                            {!isSmallMobile && ' Behavior'}
                          </span>
                        ),
                        children: (
                          <BehaviorNotesTab
                            key={`behavior-${careLogRefreshKey}`}
                            clientId={clientId}
                            clientName={client.fullName}
                          />
                        ),
                      },
                      {
                        key: 'activities',
                        label: (
                          <span>
                            <TeamOutlined />
                            {!isSmallMobile && ' Activities'}
                          </span>
                        ),
                        children: (
                          <ActivitiesTab
                            key={`activities-${careLogRefreshKey}`}
                            clientId={clientId}
                            clientName={client.fullName}
                            homeId={client.homeId}
                          />
                        ),
                      },
                    ]}
                  />
                </Card>
              </Space>
            ),
          },
          {
            key: 'incidents',
            label: (
              <span>
                <WarningOutlined />
                {!isSmallMobile && ' Incidents'}
              </span>
            ),
            children: (
              <IncidentList
                clientId={clientId}
                showFilters={false}
                showCreateButton={client.isActive}
              />
            ),
          },
          {
            key: 'appointments',
            label: (
              <span>
                <CalendarOutlined />
                {!isSmallMobile && ' Appts'}
              </span>
            ),
            children: (
              <AppointmentsTab
                clientId={clientId}
                clientName={client.fullName}
                isActive={client.isActive}
              />
            ),
          },
          {
            key: 'documents',
            label: (
              <span>
                <FolderOutlined />
                {!isSmallMobile && ' Docs'}
              </span>
            ),
            children: (
              <>
                <DocumentList
                  key={`documents-${documentsRefreshKey}`}
                  clientId={clientId}
                  homeId={client.homeId}
                  onUploadClick={() => setDocumentUploadOpen(true)}
                  onRefresh={() => setDocumentsRefreshKey((k) => k + 1)}
                />
                <DocumentUpload
                  clientId={clientId}
                  open={documentUploadOpen}
                  onCancel={() => setDocumentUploadOpen(false)}
                  onSuccess={() => {
                    setDocumentUploadOpen(false);
                    setDocumentsRefreshKey((k) => k + 1);
                  }}
                />
              </>
            ),
          },
        ]}
      />

      {/* Quick Log Modal */}
      <QuickLogModal
        open={quickLogModalOpen}
        onClose={() => setQuickLogModalOpen(false)}
        clientId={clientId}
        clientName={client.fullName}
        homeId={client.homeId}
        defaultTab={quickLogDefaultTab}
        onSuccess={() => setCareLogRefreshKey((k) => k + 1)}
      />

      {/* Edit Modal */}
      <Modal
        title="Edit Client Information"
        open={editModalOpen}
        onCancel={() => setEditModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={isMobile ? '100%' : 720}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Form
          form={editForm}
          layout="vertical"
          onFinish={(values) => void handleUpdateClient(values)}
          disabled={submitting}
        >
          <Title level={5}>Personal Information</Title>
          <Row gutter={[16, 0]}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="firstName"
                label="First Name"
                rules={[{ required: true }]}
              >
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="lastName"
                label="Last Name"
                rules={[{ required: true }]}
              >
                <Input />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={[16, 0]}>
            <Col xs={24} sm={12}>
              <Form.Item
                name="dateOfBirth"
                label="Date of Birth"
                rules={[{ required: true }]}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item
                name="gender"
                label="Gender"
                rules={[{ required: true }]}
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

          <Divider />
          <Title level={5}>Medical Information</Title>
          <Form.Item name="primaryPhysician" label="Primary Physician">
            <Input />
          </Form.Item>
          <Form.Item name="primaryPhysicianPhone" label="Physician Phone">
            <Input />
          </Form.Item>
          <Form.Item name="allergies" label="Allergies">
            <Input />
          </Form.Item>
          <Form.Item name="diagnoses" label="Diagnoses">
            <TextArea rows={2} />
          </Form.Item>
          <Form.Item name="medicationList" label="Current Medications">
            <TextArea rows={2} />
          </Form.Item>

          <Divider />
          <Title level={5}>Emergency Contact</Title>
          <Row gutter={[16, 0]}>
            <Col xs={24} sm={8}>
              <Form.Item name="emergencyContactName" label="Name">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item name="emergencyContactPhone" label="Phone">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item name="emergencyContactRelationship" label="Relationship">
                <Input />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item name="notes" label="Notes">
            <TextArea rows={3} />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex justify="end" gap={8} vertical={isSmallMobile}>
              <Button 
                onClick={() => setEditModalOpen(false)}
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

      {/* Discharge Modal */}
      <Modal
        title="Discharge Client"
        open={dischargeModalOpen}
        onCancel={() => setDischargeModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Alert
          type="warning"
          message="This will discharge the client and mark their bed as available."
          style={{ marginBottom: 24 }}
        />
        <Form
          form={dischargeForm}
          layout="vertical"
          onFinish={(values) => void handleConfirmDischarge(values)}
          disabled={submitting}
        >
          <Form.Item
            name="dischargeDate"
            label="Discharge Date"
            rules={[{ required: true, message: 'Required' }]}
          >
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item
            name="dischargeReason"
            label="Discharge Reason"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Select
              placeholder="Select reason"
              options={[
                { label: 'Family Decision', value: 'Family Decision' },
                { label: 'Medical Transfer', value: 'Medical Transfer' },
                { label: 'Deceased', value: 'Deceased' },
                { label: 'Financial', value: 'Financial' },
                { label: 'Other', value: 'Other' },
              ]}
            />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex justify="end" gap={8} vertical={isSmallMobile}>
              <Button 
                onClick={() => setDischargeModalOpen(false)}
                style={isSmallMobile ? { order: 2, minHeight: 44 } : undefined}
              >
                Cancel
              </Button>
              <Button 
                type="primary" 
                danger 
                htmlType="submit" 
                loading={submitting}
                style={isSmallMobile ? { order: 1, minHeight: 44 } : undefined}
              >
                Confirm Discharge
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>

      {/* Transfer Modal */}
      <Modal
        title="Transfer Client"
        open={transferModalOpen}
        onCancel={() => setTransferModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Alert
          type="info"
          message="Select a new bed for the client. The current bed will be marked as available."
          style={{ marginBottom: 24 }}
        />
        <Form
          form={transferForm}
          layout="vertical"
          onFinish={(values) => void handleConfirmTransfer(values)}
          disabled={submitting}
        >
          <Form.Item
            name="homeId"
            label="Home"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Select
              placeholder="Select home"
              value={selectedHomeId}
              onChange={setSelectedHomeId}
              options={homes.map((h) => ({
                label: h.name,
                value: h.id,
              }))}
            />
          </Form.Item>
          <Form.Item
            name="newBedId"
            label="New Bed"
            rules={[{ required: true, message: 'Required' }]}
          >
            <Select
              placeholder={selectedHomeId ? 'Select bed' : 'Select home first'}
              disabled={!selectedHomeId}
              options={availableBeds
                .filter((b) => b.id !== client?.bedId)
                .map((b) => ({
                  label: `${b.label} (${b.status})`,
                  value: b.id,
                  disabled: b.status === 'Occupied',
                }))}
            />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex justify="end" gap={8} vertical={isSmallMobile}>
              <Button 
                onClick={() => setTransferModalOpen(false)}
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
                Confirm Transfer
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

export default function ClientDetailPage(props: ClientDetailPageProps) {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <ClientDetailContent {...props} />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
