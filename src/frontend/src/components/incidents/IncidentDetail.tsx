'use client';

import React, { useState } from 'react';
import {
  Card,
  Descriptions,
  Tag,
  Typography,
  Button,
  Space,
  Timeline,
  Form,
  Input,
  Modal,
  message,
  Badge,
  Divider,
  Alert,
  Flex,
  Popconfirm,
  Grid,
  Dropdown,
} from 'antd';
import {
  EditOutlined,
  SendOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  MessageOutlined,
  UserOutlined,
  WarningOutlined,
  DeleteOutlined,
  MoreOutlined,
  CameraOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import { useRouter } from 'next/navigation';

import { incidentsApi, ApiError } from '@/lib/api';
import type { Incident, IncidentType, IncidentStatus, IncidentFollowUp, IncidentPhoto } from '@/types';
import { useAuth } from '@/contexts/AuthContext';
import IncidentPhotoUpload from './IncidentPhotoUpload';
import type { MenuProps } from 'antd';

const { Text, Paragraph, Title } = Typography;
const { TextArea } = Input;
const { useBreakpoint } = Grid;

interface IncidentDetailProps {
  incident: Incident;
  onUpdate?: (incident: Incident) => void;
}

const INCIDENT_TYPES: { label: string; value: IncidentType; color: string }[] = [
  { label: 'Fall', value: 'Fall', color: 'red' },
  { label: 'Medication', value: 'Medication', color: 'orange' },
  { label: 'Behavioral', value: 'Behavioral', color: 'purple' },
  { label: 'Medical', value: 'Medical', color: 'blue' },
  { label: 'Injury', value: 'Injury', color: 'volcano' },
  { label: 'Elopement', value: 'Elopement', color: 'magenta' },
  { label: 'Other', value: 'Other', color: 'default' },
];

const INCIDENT_STATUSES: { label: string; value: IncidentStatus; color: string; icon: React.ReactNode }[] = [
  { label: 'Draft', value: 'Draft', color: 'default', icon: <EditOutlined /> },
  { label: 'Submitted', value: 'Submitted', color: 'blue', icon: <SendOutlined /> },
  { label: 'Under Review', value: 'UnderReview', color: 'orange', icon: <ClockCircleOutlined /> },
  { label: 'Closed', value: 'Closed', color: 'green', icon: <CheckCircleOutlined /> },
];

function getTypeTag(type: IncidentType) {
  const t = INCIDENT_TYPES.find((x) => x.value === type);
  return <Tag color={t?.color}>{t?.label || type}</Tag>;
}

function getStatusInfo(status: IncidentStatus) {
  return INCIDENT_STATUSES.find((s) => s.value === status);
}

function getSeverityColor(severity: number): string {
  const colors: Record<number, string> = {
    1: '#52c41a',
    2: '#95de64',
    3: '#faad14',
    4: '#ff7a45',
    5: '#ff4d4f',
  };
  return colors[severity] || '#d9d9d9';
}

function getSeverityLabel(severity: number): string {
  const labels: Record<number, string> = {
    1: 'Minor',
    2: 'Low',
    3: 'Moderate',
    4: 'High',
    5: 'Severe',
  };
  return labels[severity] || 'Unknown';
}

export default function IncidentDetail({ incident, onUpdate }: IncidentDetailProps) {
  const router = useRouter();
  const { hasRole, user } = useAuth();
  const isAdmin = hasRole('Admin');
  const isAuthor = user?.id === incident.reportedById;
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [followUpModalOpen, setFollowUpModalOpen] = useState(false);
  const [closeModalOpen, setCloseModalOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [changingStatus, setChangingStatus] = useState(false);
  const [photos, setPhotos] = useState<IncidentPhoto[]>(incident.photos || []);
  const [followUpForm] = Form.useForm();
  const [closeForm] = Form.useForm();

  const statusInfo = getStatusInfo(incident.status);
  const isDraft = incident.status === 'Draft';
  const isSubmitted = incident.status === 'Submitted';
  const isClosed = incident.status === 'Closed';

  // Can edit photos if:
  // - Draft status and is author, OR
  // - Not closed and is admin
  const canEditPhotos = (isDraft && isAuthor) || (!isClosed && isAdmin);

  const handlePhotosChange = (newPhotos: IncidentPhoto[]) => {
    setPhotos(newPhotos);
    // Update the incident object if we have an update callback
    if (onUpdate && incident) {
      onUpdate({ ...incident, photos: newPhotos });
    }
  };

  const handleStatusChange = async (newStatus: IncidentStatus) => {
    try {
      setChangingStatus(true);
      const result = await incidentsApi.updateStatus(incident.id, newStatus);
      if (result.success && result.incident) {
        const statusLabel = INCIDENT_STATUSES.find(s => s.value === newStatus)?.label;
        void message.success(`Status changed to ${statusLabel}`);
        onUpdate?.(result.incident);
      } else {
        void message.error(result.error || 'Failed to update status');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to update status';
      void message.error(msg);
    } finally {
      setChangingStatus(false);
    }
  };

  const handleSubmit = async () => {
    try {
      setSubmitting(true);
      const result = await incidentsApi.submit(incident.id);
      if (result.success && result.incident) {
        void message.success('Incident submitted successfully');
        onUpdate?.(result.incident);
      } else {
        void message.error(result.error || 'Failed to submit incident');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to submit incident';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async () => {
    try {
      setDeleting(true);
      const result = await incidentsApi.delete(incident.id);
      if (result.success) {
        void message.success('Incident deleted successfully');
        router.push('/incidents');
      } else {
        void message.error(result.error || 'Failed to delete incident');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to delete incident';
      void message.error(msg);
    } finally {
      setDeleting(false);
    }
  };

  const handleAddFollowUp = async (values: { note: string }) => {
    try {
      setSubmitting(true);
      const result = await incidentsApi.addFollowUp(incident.id, { note: values.note });
      if (result.success && result.incident) {
        void message.success('Follow-up added successfully');
        setFollowUpModalOpen(false);
        followUpForm.resetFields();
        onUpdate?.(result.incident);
      } else {
        void message.error(result.error || 'Failed to add follow-up');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to add follow-up';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleClose = async (values: { closureNotes?: string }) => {
    try {
      setSubmitting(true);
      const result = await incidentsApi.close(incident.id, values.closureNotes);
      if (result.success && result.incident) {
        void message.success('Incident closed successfully');
        setCloseModalOpen(false);
        closeForm.resetFields();
        onUpdate?.(result.incident);
      } else {
        void message.error(result.error || 'Failed to close incident');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to close incident';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      {/* Status Banner */}
      <Alert
        type={isClosed ? 'success' : incident.severity >= 4 ? 'error' : 'info'}
        message={
          <Flex justify="space-between" align="center" wrap="wrap" gap={8}>
            <Space>
              {statusInfo?.icon}
              <Text strong>Status: {statusInfo?.label}</Text>
              {incident.severity >= 4 && !isClosed && !isSmallMobile && (
                <Tag color="red" icon={<WarningOutlined />}>
                  High Severity
                </Tag>
              )}
            </Space>
            {(() => {
              // Render action buttons based on role and status
              if (isDraft && isAuthor) {
                if (isMobile) {
                  const menuItems: MenuProps['items'] = [
                    { key: 'edit', icon: <EditOutlined />, label: 'Edit', onClick: () => router.push(`/incidents/${incident.id}/edit`) },
                    { key: 'submit', icon: <SendOutlined />, label: 'Submit', onClick: () => void handleSubmit() },
                    { key: 'delete', icon: <DeleteOutlined />, label: 'Delete', danger: true, onClick: () => void handleDelete() },
                  ];
                  return (
                    <Dropdown menu={{ items: menuItems }} trigger={['click']}>
                      <Button icon={<MoreOutlined />} style={{ minWidth: 44, minHeight: 44 }}>Actions</Button>
                    </Dropdown>
                  );
                }
                return (
                  <Space wrap>
                    <Button icon={<EditOutlined />} onClick={() => router.push(`/incidents/${incident.id}/edit`)} style={{ minHeight: 44 }}>
                      Edit
                    </Button>
                    <Popconfirm
                      title="Submit this incident?"
                      description="Once submitted, the incident will be reviewed by administrators."
                      onConfirm={() => void handleSubmit()}
                      okText="Submit"
                      cancelText="Cancel"
                    >
                      <Button type="primary" icon={<SendOutlined />} loading={submitting} style={{ minHeight: 44 }}>
                        Submit
                      </Button>
                    </Popconfirm>
                    <Popconfirm
                      title="Delete this draft?"
                      description="This action cannot be undone."
                      onConfirm={() => void handleDelete()}
                      okText="Delete"
                      okButtonProps={{ danger: true }}
                      cancelText="Cancel"
                    >
                      <Button danger icon={<DeleteOutlined />} loading={deleting} style={{ minHeight: 44 }}>
                        Delete
                      </Button>
                    </Popconfirm>
                  </Space>
                );
              }
              if (isAdmin && !isDraft && !isClosed) {
                if (isMobile) {
                  const menuItems: MenuProps['items'] = [
                    { key: 'followup', icon: <MessageOutlined />, label: 'Add Follow-up', onClick: () => setFollowUpModalOpen(true) },
                    ...(isSubmitted ? [{ key: 'review', icon: <ClockCircleOutlined />, label: 'Start Review', onClick: () => void handleStatusChange('UnderReview') }] : []),
                    { key: 'close', icon: <CheckCircleOutlined />, label: 'Close Incident', danger: true, onClick: () => setCloseModalOpen(true) },
                  ];
                  return (
                    <Dropdown menu={{ items: menuItems }} trigger={['click']}>
                      <Button icon={<MoreOutlined />} style={{ minWidth: 44, minHeight: 44 }}>Actions</Button>
                    </Dropdown>
                  );
                }
                return (
                  <Space wrap>
                    <Button icon={<MessageOutlined />} onClick={() => setFollowUpModalOpen(true)} style={{ minHeight: 44 }}>
                      {isSmallMobile ? '' : 'Add Follow-up'}
                    </Button>
                    {isSubmitted && (
                      <Button
                        type="primary"
                        icon={<ClockCircleOutlined />}
                        loading={changingStatus}
                        onClick={() => void handleStatusChange('UnderReview')}
                        style={{ minHeight: 44 }}
                      >
                        Start Review
                      </Button>
                    )}
                    <Button
                      type="primary"
                      danger
                      icon={<CheckCircleOutlined />}
                      onClick={() => setCloseModalOpen(true)}
                      style={{ minHeight: 44 }}
                    >
                      {isSmallMobile ? 'Close' : 'Close Incident'}
                    </Button>
                  </Space>
                );
              }
              if (isClosed && incident.closedByName) {
                return <Text type="secondary">Closed by {incident.closedByName}</Text>;
              }
              return null;
            })()}
          </Flex>
        }
        showIcon={false}
        style={{ marginBottom: 24 }}
      />

      {/* Main Details */}
      <Card title="Incident Details" style={{ marginBottom: 24 }} size={isMobile ? 'small' : 'default'}>
        <Descriptions column={{ xs: 1, sm: 2, lg: 3 }} bordered size={isMobile ? 'small' : 'default'}>
          <Descriptions.Item label="Incident Number">
            <Text strong>{incident.incidentNumber}</Text>
          </Descriptions.Item>
          <Descriptions.Item label="Client">
            <Button type="link" onClick={() => router.push(`/clients/${incident.clientId}`)} style={{ padding: 0 }}>
              {incident.clientName}
            </Button>
          </Descriptions.Item>
          <Descriptions.Item label="Home">{incident.homeName}</Descriptions.Item>
          <Descriptions.Item label="Type">{getTypeTag(incident.incidentType)}</Descriptions.Item>
          <Descriptions.Item label="Severity">
            <Space>
              <Badge
                count={incident.severity}
                style={{ backgroundColor: getSeverityColor(incident.severity) }}
              />
              <Text>{getSeverityLabel(incident.severity)}</Text>
            </Space>
          </Descriptions.Item>
          <Descriptions.Item label="Status">
            <Tag color={statusInfo?.color} icon={statusInfo?.icon}>
              {statusInfo?.label}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Occurred At" span={2}>
            {dayjs(incident.occurredAt).format('MMMM D, YYYY h:mm A')}
          </Descriptions.Item>
          <Descriptions.Item label="Location">{incident.location}</Descriptions.Item>
          <Descriptions.Item label="Reported By">{incident.reportedByName}</Descriptions.Item>
          <Descriptions.Item label="Reported At">
            {dayjs(incident.createdAt).format('MMM D, YYYY h:mm A')}
          </Descriptions.Item>
          {incident.adminNotifiedAt && (
            <Descriptions.Item label="Admin Notified">
              {dayjs(incident.adminNotifiedAt).format('MMM D, YYYY h:mm A')}
            </Descriptions.Item>
          )}
        </Descriptions>
      </Card>

      {/* Description & Actions */}
      <Card title="Description & Actions Taken" style={{ marginBottom: 24 }} size={isMobile ? 'small' : 'default'}>
        <Title level={5}>What Happened</Title>
        <Paragraph style={{ whiteSpace: 'pre-wrap' }}>{incident.description}</Paragraph>

        {incident.actionsTaken && (
          <>
            <Divider />
            <Title level={5}>Actions Taken</Title>
            <Paragraph style={{ whiteSpace: 'pre-wrap' }}>{incident.actionsTaken}</Paragraph>
          </>
        )}

        {incident.witnessNames && (
          <>
            <Divider />
            <Title level={5}>Witnesses</Title>
            <Paragraph>{incident.witnessNames}</Paragraph>
          </>
        )}

        {incident.notifiedParties && (
          <>
            <Divider />
            <Title level={5}>Notified Parties</Title>
            <Paragraph>{incident.notifiedParties}</Paragraph>
          </>
        )}
      </Card>

      {/* Closure Information */}
      {isClosed && (
        <Card title="Closure Information" style={{ marginBottom: 24 }} size={isMobile ? 'small' : 'default'}>
          <Descriptions column={{ xs: 1, sm: 2 }} bordered size={isMobile ? 'small' : 'default'}>
            <Descriptions.Item label="Closed By">{incident.closedByName}</Descriptions.Item>
            <Descriptions.Item label="Closed At">
              {dayjs(incident.closedAt).format('MMM D, YYYY h:mm A')}
            </Descriptions.Item>
            {incident.closureNotes && (
              <Descriptions.Item label="Closure Notes" span={2}>
                <Paragraph style={{ marginBottom: 0, whiteSpace: 'pre-wrap' }}>
                  {incident.closureNotes}
                </Paragraph>
              </Descriptions.Item>
            )}
          </Descriptions>
        </Card>
      )}

      {/* Photos */}
      <Card
        title={
          <Space>
            <CameraOutlined />
            <span>Photos</span>
            <Tag>{photos.length}</Tag>
          </Space>
        }
        style={{ marginBottom: 24 }}
        size={isMobile ? 'small' : 'default'}
      >
        <IncidentPhotoUpload
          incidentId={incident.id}
          photos={photos}
          onPhotosChange={handlePhotosChange}
          canEdit={canEditPhotos}
        />
      </Card>

      {/* Follow-ups */}
      <Card
        title={
          <Space>
            <span>Follow-up Notes</span>
            <Tag>{incident.followUps.length}</Tag>
          </Space>
        }
        size={isMobile ? 'small' : 'default'}
      >
        {incident.followUps.length === 0 ? (
          <Text type="secondary">No follow-up notes yet.</Text>
        ) : (
          <Timeline
            items={incident.followUps.map((fu: IncidentFollowUp) => ({
              color: 'blue',
              children: (
                <div key={fu.id}>
                  <Flex justify="space-between" align="start">
                    <Space>
                      <UserOutlined />
                      <Text strong>{fu.createdByName}</Text>
                    </Space>
                    <Text type="secondary">
                      {dayjs(fu.createdAt).format('MMM D, YYYY h:mm A')}
                    </Text>
                  </Flex>
                  <Paragraph style={{ marginTop: 8, marginBottom: 0, whiteSpace: 'pre-wrap' }}>
                    {fu.note}
                  </Paragraph>
                </div>
              ),
            }))}
          />
        )}
      </Card>

      {/* Add Follow-up Modal */}
      <Modal
        title="Add Follow-up Note"
        open={followUpModalOpen}
        onCancel={() => setFollowUpModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Form
          form={followUpForm}
          layout="vertical"
          onFinish={(values) => void handleAddFollowUp(values)}
          disabled={submitting}
        >
          <Form.Item
            name="note"
            label="Note"
            rules={[
              { required: true, message: 'Please enter a note' },
              { min: 10, message: 'Note must be at least 10 characters' },
            ]}
          >
            <TextArea
              rows={4}
              placeholder="Enter follow-up information, actions taken, or observations..."
              showCount
              maxLength={2000}
            />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0 }}>
            <Flex justify="end" gap={8}>
              <Button onClick={() => setFollowUpModalOpen(false)}>Cancel</Button>
              <Button type="primary" htmlType="submit" loading={submitting}>
                Add Follow-up
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>

      {/* Close Incident Modal */}
      <Modal
        title="Close Incident"
        open={closeModalOpen}
        onCancel={() => setCloseModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Alert
          type="warning"
          message="Closing an incident is a permanent action"
          description="Once closed, no further edits or follow-ups can be added."
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Form
          form={closeForm}
          layout="vertical"
          onFinish={(values) => void handleClose(values)}
          disabled={submitting}
        >
          <Form.Item
            name="closureNotes"
            label="Closure Notes (Optional)"
          >
            <TextArea
              rows={4}
              placeholder="Summary of resolution, lessons learned, or preventive measures..."
              showCount
              maxLength={2000}
            />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0 }}>
            <Flex justify="end" gap={8}>
              <Button onClick={() => setCloseModalOpen(false)}>Cancel</Button>
              <Button type="primary" danger htmlType="submit" loading={submitting}>
                Close Incident
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
}
