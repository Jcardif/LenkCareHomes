'use client';

import React, { useState, useEffect, useCallback, use } from 'react';
import {
  Card,
  Typography,
  Descriptions,
  Button,
  Space,
  Tag,
  Spin,
  message,
  Modal,
  Input,
  Breadcrumb,
  Grid,
  Divider,
  Row,
  Col,
  DatePicker,
} from 'antd';
import {
  HomeOutlined,
  CalendarOutlined,
  EditOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  DeleteOutlined,
  ArrowLeftOutlined,
  EnvironmentOutlined,
  PhoneOutlined,
  UserOutlined,
  ClockCircleOutlined,
  CarOutlined,
  FileTextOutlined,
  SwapOutlined,
} from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import dayjs from 'dayjs';

import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { appointmentsApi, ApiError } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { getTypeTag, getStatusTag } from '@/components/appointments/AppointmentList';
import type { Appointment } from '@/types';

const { Title, Text } = Typography;
const { TextArea } = Input;
const { useBreakpoint } = Grid;

interface PageProps {
  params: Promise<{ id: string }>;
}

function AppointmentDetailContent({ params }: PageProps) {
  const { id } = use(params);
  const router = useRouter();
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Admin');
  const screens = useBreakpoint();
  const isMobile = !screens.md;

  const [appointment, setAppointment] = useState<Appointment | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);

  // Modal states
  const [completeModalOpen, setCompleteModalOpen] = useState(false);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [noShowModalOpen, setNoShowModalOpen] = useState(false);
  const [rescheduleModalOpen, setRescheduleModalOpen] = useState(false);
  const [deleteModalOpen, setDeleteModalOpen] = useState(false);
  const [outcomeNotes, setOutcomeNotes] = useState('');
  const [cancellationReason, setCancellationReason] = useState('');
  const [noShowNotes, setNoShowNotes] = useState('');
  const [rescheduleDate, setRescheduleDate] = useState<dayjs.Dayjs | null>(null);
  const [rescheduleNotes, setRescheduleNotes] = useState('');

  const fetchAppointment = useCallback(async () => {
    try {
      setLoading(true);
      const data = await appointmentsApi.getById(id);
      setAppointment(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load appointment';
      void message.error(msg);
      router.push('/appointments');
    } finally {
      setLoading(false);
    }
  }, [id, router]);

  useEffect(() => {
    void fetchAppointment();
  }, [fetchAppointment]);

  const handleComplete = async () => {
    try {
      setActionLoading(true);
      const result = await appointmentsApi.complete(id, { outcomeNotes });
      if (result.success) {
        void message.success('Appointment marked as completed');
        setCompleteModalOpen(false);
        setOutcomeNotes('');
        void fetchAppointment();
      } else {
        void message.error(result.error || 'Failed to complete appointment');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to complete appointment';
      void message.error(msg);
    } finally {
      setActionLoading(false);
    }
  };

  const handleCancel = async () => {
    try {
      setActionLoading(true);
      const result = await appointmentsApi.cancel(id, { cancellationReason });
      if (result.success) {
        void message.success('Appointment cancelled');
        setCancelModalOpen(false);
        setCancellationReason('');
        void fetchAppointment();
      } else {
        void message.error(result.error || 'Failed to cancel appointment');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to cancel appointment';
      void message.error(msg);
    } finally {
      setActionLoading(false);
    }
  };

  const handleNoShow = async () => {
    try {
      setActionLoading(true);
      const result = await appointmentsApi.noShow(id, { notes: noShowNotes });
      if (result.success) {
        void message.success('Appointment marked as no-show');
        setNoShowModalOpen(false);
        setNoShowNotes('');
        void fetchAppointment();
      } else {
        void message.error(result.error || 'Failed to mark as no-show');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to mark as no-show';
      void message.error(msg);
    } finally {
      setActionLoading(false);
    }
  };

  const handleReschedule = async () => {
    if (!rescheduleDate) return;
    try {
      setActionLoading(true);
      const result = await appointmentsApi.reschedule(id, {
        newScheduledAt: rescheduleDate.toISOString(),
        notes: rescheduleNotes,
      });
      if (result.success) {
        void message.success('Appointment rescheduled');
        setRescheduleModalOpen(false);
        setRescheduleDate(null);
        setRescheduleNotes('');
        void fetchAppointment();
      } else {
        void message.error(result.error || 'Failed to reschedule appointment');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to reschedule appointment';
      void message.error(msg);
    } finally {
      setActionLoading(false);
    }
  };

  const handleDelete = async () => {
    try {
      setActionLoading(true);
      const result = await appointmentsApi.delete(id);
      if (result.success) {
        void message.success('Appointment deleted');
        router.push('/appointments');
      } else {
        void message.error(result.error || 'Failed to delete appointment');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to delete appointment';
      void message.error(msg);
    } finally {
      setActionLoading(false);
      setDeleteModalOpen(false);
    }
  };

  if (loading) {
    return (
      <Card>
        <div style={{ textAlign: 'center', padding: 48 }}>
          <Spin size="large" />
          <div style={{ marginTop: 16 }}>Loading appointment...</div>
        </div>
      </Card>
    );
  }

  if (!appointment) {
    return null;
  }

  const isScheduled = appointment.status === 'Scheduled';
  const isOverdue = isScheduled && dayjs(appointment.scheduledAt).isBefore(dayjs());

  return (
    <div>
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <Link href="/dashboard"><HomeOutlined /></Link> },
          { title: <Link href="/appointments">Appointments</Link> },
          { title: appointment.title },
        ]}
      />

      <Card>
        {/* Header */}
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
            marginBottom: 24,
            flexWrap: 'wrap',
            gap: 16,
          }}
        >
          <div>
            <Space align="center" style={{ marginBottom: 8 }}>
              <Button
                type="text"
                icon={<ArrowLeftOutlined />}
                onClick={() => router.push('/appointments')}
              />
              <Title level={isMobile ? 4 : 3} style={{ margin: 0 }}>
                {appointment.title}
              </Title>
            </Space>
            <Space size="small" wrap>
              {getTypeTag(appointment.appointmentType)}
              {getStatusTag(appointment.status)}
              {isOverdue && <Tag color="red">Overdue</Tag>}
            </Space>
          </div>

          {isScheduled && (
            <Space wrap>
              <Button
                icon={<EditOutlined />}
                onClick={() => router.push(`/appointments/${id}/edit`)}
              >
                Edit
              </Button>
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                onClick={() => setCompleteModalOpen(true)}
              >
                Complete
              </Button>
              <Button
                icon={<ClockCircleOutlined />}
                onClick={() => setNoShowModalOpen(true)}
              >
                No-Show
              </Button>
              <Button
                icon={<SwapOutlined />}
                onClick={() => setRescheduleModalOpen(true)}
              >
                Reschedule
              </Button>
              <Button
                danger
                icon={<CloseCircleOutlined />}
                onClick={() => setCancelModalOpen(true)}
              >
                Cancel
              </Button>
              {isAdmin && (
                <Button
                  danger
                  type="text"
                  icon={<DeleteOutlined />}
                  onClick={() => setDeleteModalOpen(true)}
                >
                  Delete
                </Button>
              )}
            </Space>
          )}
        </div>

        <Divider />

        {/* Appointment Details */}
        <Row gutter={[24, 24]}>
          <Col xs={24} lg={12}>
            <Card type="inner" title="Appointment Details" size="small">
              <Descriptions column={1} size="small">
                <Descriptions.Item label={<><CalendarOutlined /> Date & Time</>}>
                  <Text strong>
                    {dayjs(appointment.scheduledAt).format('dddd, MMMM D, YYYY [at] h:mm A')}
                  </Text>
                </Descriptions.Item>
                <Descriptions.Item label={<><ClockCircleOutlined /> Duration</>}>
                  {appointment.durationMinutes} minutes
                </Descriptions.Item>
                <Descriptions.Item label={<><UserOutlined /> Client</>}>
                  <Link href={`/clients/${appointment.clientId}`}>
                    {appointment.clientName}
                  </Link>
                </Descriptions.Item>
                <Descriptions.Item label="Home">
                  {appointment.homeName}
                </Descriptions.Item>
              </Descriptions>
            </Card>
          </Col>

          <Col xs={24} lg={12}>
            <Card type="inner" title="Provider Information" size="small">
              <Descriptions column={1} size="small">
                {appointment.location && (
                  <Descriptions.Item label={<><EnvironmentOutlined /> Location</>}>
                    {appointment.location}
                  </Descriptions.Item>
                )}
                {appointment.providerName && (
                  <Descriptions.Item label={<><UserOutlined /> Provider</>}>
                    {appointment.providerName}
                  </Descriptions.Item>
                )}
                {appointment.providerPhone && (
                  <Descriptions.Item label={<><PhoneOutlined /> Phone</>}>
                    <a href={`tel:${appointment.providerPhone}`}>{appointment.providerPhone}</a>
                  </Descriptions.Item>
                )}
                {!appointment.location && !appointment.providerName && !appointment.providerPhone && (
                  <Descriptions.Item label="">
                    <Text type="secondary">No provider information specified</Text>
                  </Descriptions.Item>
                )}
              </Descriptions>
            </Card>
          </Col>

          {appointment.transportationNotes && (
            <Col xs={24}>
              <Card type="inner" title={<><CarOutlined /> Transportation Notes</>} size="small">
                <Text>{appointment.transportationNotes}</Text>
              </Card>
            </Col>
          )}

          {appointment.notes && (
            <Col xs={24}>
              <Card type="inner" title={<><FileTextOutlined /> Notes</>} size="small">
                <Text>{appointment.notes}</Text>
              </Card>
            </Col>
          )}

          {appointment.status === 'Completed' && (
            <Col xs={24}>
              <Card
                type="inner"
                title={<><CheckCircleOutlined /> Outcome</>}
                size="small"
                style={{ borderColor: '#52c41a' }}
              >
                <Descriptions column={1} size="small">
                  <Descriptions.Item label="Completed At">
                    {appointment.completedAt
                      ? dayjs(appointment.completedAt).format('MMMM D, YYYY [at] h:mm A')
                      : 'N/A'}
                  </Descriptions.Item>
                  <Descriptions.Item label="Completed By">
                    {appointment.completedByName || 'N/A'}
                  </Descriptions.Item>
                  {appointment.outcomeNotes && (
                    <Descriptions.Item label="Outcome Notes">
                      {appointment.outcomeNotes}
                    </Descriptions.Item>
                  )}
                </Descriptions>
              </Card>
            </Col>
          )}
        </Row>

        <Divider />

        {/* Audit Information */}
        <Descriptions size="small" column={isMobile ? 1 : 2}>
          <Descriptions.Item label="Created By">{appointment.createdByName}</Descriptions.Item>
          <Descriptions.Item label="Created At">
            {dayjs(appointment.createdAt).format('MMM D, YYYY h:mm A')}
          </Descriptions.Item>
          {appointment.updatedAt && (
            <Descriptions.Item label="Last Updated">
              {dayjs(appointment.updatedAt).format('MMM D, YYYY h:mm A')}
            </Descriptions.Item>
          )}
        </Descriptions>
      </Card>

      {/* Complete Appointment Modal */}
      <Modal
        title="Complete Appointment"
        open={completeModalOpen}
        onOk={() => void handleComplete()}
        onCancel={() => {
          setCompleteModalOpen(false);
          setOutcomeNotes('');
        }}
        confirmLoading={actionLoading}
        okText="Mark Completed"
      >
        <div style={{ marginTop: 16 }}>
          <Text>Add any outcome notes for this appointment (optional):</Text>
          <TextArea
            rows={4}
            value={outcomeNotes}
            onChange={(e) => setOutcomeNotes(e.target.value)}
            placeholder="Enter outcome notes, findings, or follow-up actions..."
            style={{ marginTop: 8 }}
          />
        </div>
      </Modal>

      {/* Cancel Appointment Modal */}
      <Modal
        title="Cancel Appointment"
        open={cancelModalOpen}
        onOk={() => void handleCancel()}
        onCancel={() => {
          setCancelModalOpen(false);
          setCancellationReason('');
        }}
        confirmLoading={actionLoading}
        okText="Cancel Appointment"
        okButtonProps={{ danger: true }}
      >
        <div style={{ marginTop: 16 }}>
          <Text>Please provide a reason for cancellation (optional):</Text>
          <TextArea
            rows={3}
            value={cancellationReason}
            onChange={(e) => setCancellationReason(e.target.value)}
            placeholder="Enter reason for cancellation..."
            style={{ marginTop: 8 }}
          />
        </div>
      </Modal>

      {/* No-Show Appointment Modal */}
      <Modal
        title="Mark as No-Show"
        open={noShowModalOpen}
        onOk={() => void handleNoShow()}
        onCancel={() => {
          setNoShowModalOpen(false);
          setNoShowNotes('');
        }}
        confirmLoading={actionLoading}
        okText="Mark No-Show"
        okButtonProps={{ danger: true }}
      >
        <div style={{ marginTop: 16 }}>
          <Text>Add any notes about the no-show (optional):</Text>
          <TextArea
            rows={3}
            value={noShowNotes}
            onChange={(e) => setNoShowNotes(e.target.value)}
            placeholder="Enter notes about why client did not show..."
            style={{ marginTop: 8 }}
          />
        </div>
      </Modal>

      {/* Reschedule Appointment Modal */}
      <Modal
        title="Reschedule Appointment"
        open={rescheduleModalOpen}
        onOk={() => void handleReschedule()}
        onCancel={() => {
          setRescheduleModalOpen(false);
          setRescheduleDate(null);
          setRescheduleNotes('');
        }}
        confirmLoading={actionLoading}
        okText="Reschedule"
        okButtonProps={{ disabled: !rescheduleDate }}
      >
        <div style={{ marginTop: 16 }}>
          <Text strong>New Date & Time:</Text>
          <DatePicker
            showTime
            value={rescheduleDate}
            onChange={setRescheduleDate}
            style={{ width: '100%', marginTop: 8, marginBottom: 16 }}
            placeholder="Select new date and time"
            format="MMMM D, YYYY h:mm A"
          />
          <Text>Add any notes about the reschedule (optional):</Text>
          <TextArea
            rows={3}
            value={rescheduleNotes}
            onChange={(e) => setRescheduleNotes(e.target.value)}
            placeholder="Enter reason for rescheduling..."
            style={{ marginTop: 8 }}
          />
        </div>
      </Modal>

      {/* Delete Confirmation Modal */}
      <Modal
        title="Delete Appointment"
        open={deleteModalOpen}
        onOk={() => void handleDelete()}
        onCancel={() => setDeleteModalOpen(false)}
        confirmLoading={actionLoading}
        okText="Delete"
        okButtonProps={{ danger: true }}
      >
        <Text>
          Are you sure you want to delete this appointment? This action cannot be undone.
        </Text>
      </Modal>
    </div>
  );
}

export default function AppointmentDetailPage(props: PageProps) {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <AppointmentDetailContent {...props} />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
