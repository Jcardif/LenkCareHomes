'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Table,
  Button,
  Typography,
  message,
  Modal,
  Input,
  Empty,
  Dropdown,
  Grid,
  DatePicker,
} from 'antd';
import type { MenuProps, TableProps } from 'antd';
import {
  PlusOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  EyeOutlined,
  EditOutlined,
  MoreOutlined,
  ClockCircleOutlined,
  SwapOutlined,
} from '@ant-design/icons';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import dayjs from 'dayjs';

import { appointmentsApi, ApiError } from '@/lib/api';
import { getTypeTag, getStatusTag } from '@/components/appointments/AppointmentList';
import type { AppointmentSummary, AppointmentType, AppointmentStatus } from '@/types';

const { Text } = Typography;
const { TextArea } = Input;
const { useBreakpoint } = Grid;

interface AppointmentsTabProps {
  clientId: string;
  clientName: string;
  isActive?: boolean;
}

export function AppointmentsTab({ clientId, clientName, isActive = true }: AppointmentsTabProps) {
  const router = useRouter();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [appointments, setAppointments] = useState<AppointmentSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [includeCompleted] = useState(true);

  // Modal states
  const [completeModalOpen, setCompleteModalOpen] = useState(false);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [noShowModalOpen, setNoShowModalOpen] = useState(false);
  const [rescheduleModalOpen, setRescheduleModalOpen] = useState(false);
  const [selectedAppointment, setSelectedAppointment] = useState<AppointmentSummary | null>(null);
  const [outcomeNotes, setOutcomeNotes] = useState('');
  const [cancellationReason, setCancellationReason] = useState('');
  const [noShowNotes, setNoShowNotes] = useState('');
  const [rescheduleDate, setRescheduleDate] = useState<dayjs.Dayjs | null>(null);
  const [rescheduleNotes, setRescheduleNotes] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  const fetchAppointments = useCallback(async () => {
    try {
      setLoading(true);
      const data = await appointmentsApi.getByClient(clientId, includeCompleted);
      setAppointments(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load appointments';
      void message.error(msg);
    } finally {
      setLoading(false);
    }
  }, [clientId, includeCompleted]);

  useEffect(() => {
    void fetchAppointments();
  }, [fetchAppointments]);

  const handleComplete = async () => {
    if (!selectedAppointment) return;
    try {
      setActionLoading(true);
      const result = await appointmentsApi.complete(selectedAppointment.id, { outcomeNotes });
      if (result.success) {
        void message.success('Appointment marked as completed');
        setCompleteModalOpen(false);
        setOutcomeNotes('');
        setSelectedAppointment(null);
        void fetchAppointments();
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
    if (!selectedAppointment) return;
    try {
      setActionLoading(true);
      const result = await appointmentsApi.cancel(selectedAppointment.id, { cancellationReason });
      if (result.success) {
        void message.success('Appointment cancelled');
        setCancelModalOpen(false);
        setCancellationReason('');
        setSelectedAppointment(null);
        void fetchAppointments();
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
    if (!selectedAppointment) return;
    try {
      setActionLoading(true);
      const result = await appointmentsApi.noShow(selectedAppointment.id, { notes: noShowNotes });
      if (result.success) {
        void message.success('Appointment marked as no-show');
        setNoShowModalOpen(false);
        setNoShowNotes('');
        setSelectedAppointment(null);
        void fetchAppointments();
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
    if (!selectedAppointment || !rescheduleDate) return;
    try {
      setActionLoading(true);
      const result = await appointmentsApi.reschedule(selectedAppointment.id, {
        newScheduledAt: rescheduleDate.toISOString(),
        notes: rescheduleNotes,
      });
      if (result.success) {
        void message.success('Appointment rescheduled');
        setRescheduleModalOpen(false);
        setRescheduleDate(null);
        setRescheduleNotes('');
        setSelectedAppointment(null);
        void fetchAppointments();
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

  const getActionMenuItems = (record: AppointmentSummary): MenuProps['items'] => {
    const items: MenuProps['items'] = [
      {
        key: 'view',
        label: 'View Details',
        icon: <EyeOutlined />,
        onClick: () => router.push(`/appointments/${record.id}`),
      },
    ];

    if (record.status === 'Scheduled') {
      items.push(
        {
          key: 'edit',
          label: 'Edit',
          icon: <EditOutlined />,
          onClick: () => router.push(`/appointments/${record.id}/edit`),
        },
        { type: 'divider' },
        {
          key: 'complete',
          label: 'Mark Complete',
          icon: <CheckCircleOutlined />,
          onClick: () => {
            setSelectedAppointment(record);
            setCompleteModalOpen(true);
          },
        },
        {
          key: 'noshow',
          label: 'Mark No-Show',
          icon: <ClockCircleOutlined />,
          onClick: () => {
            setSelectedAppointment(record);
            setNoShowModalOpen(true);
          },
        },
        {
          key: 'reschedule',
          label: 'Reschedule',
          icon: <SwapOutlined />,
          onClick: () => {
            setSelectedAppointment(record);
            setRescheduleModalOpen(true);
          },
        },
        {
          key: 'cancel',
          label: 'Cancel',
          icon: <CloseCircleOutlined />,
          danger: true,
          onClick: () => {
            setSelectedAppointment(record);
            setCancelModalOpen(true);
          },
        }
      );
    }

    return items;
  };

  const columns: TableProps<AppointmentSummary>['columns'] = [
    {
      title: 'Date & Time',
      dataIndex: 'scheduledAt',
      key: 'scheduledAt',
      width: isMobile ? 150 : 200,
      render: (value: string) => (
        <div>
          <div style={{ fontWeight: 500 }}>
            {dayjs(value).format(isSmallMobile ? 'MMM D, YYYY' : 'MMM D, YYYY')}
          </div>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {dayjs(value).format('h:mm A')}
          </Text>
        </div>
      ),
    },
    {
      title: 'Title',
      dataIndex: 'title',
      key: 'title',
      ellipsis: true,
      render: (value: string, record: AppointmentSummary) => (
        <Link href={`/appointments/${record.id}`}>{value}</Link>
      ),
    },
    {
      title: 'Type',
      dataIndex: 'appointmentType',
      key: 'appointmentType',
      width: 140,
      responsive: ['md'],
      render: (value: AppointmentType) => getTypeTag(value),
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (value: AppointmentStatus) => getStatusTag(value),
    },
    {
      title: 'Location',
      dataIndex: 'location',
      key: 'location',
      ellipsis: true,
      responsive: ['lg'],
      render: (value: string | null) => value || <Text type="secondary">—</Text>,
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 80,
      align: 'center',
      render: (_: unknown, record: AppointmentSummary) => (
        <Dropdown menu={{ items: getActionMenuItems(record) }} trigger={['click']}>
          <Button type="text" icon={<MoreOutlined />} />
        </Dropdown>
      ),
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'flex-end' }}>
        {isActive && (
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => router.push(`/appointments/new?clientId=${clientId}`)}
          >
            {isMobile ? 'Add' : 'Add Appointment'}
          </Button>
        )}
      </div>

      <Table
        columns={columns}
        dataSource={appointments}
        rowKey="id"
        loading={loading}
        pagination={{
          showSizeChanger: !isMobile,
          showTotal: (total) => `${total} appointment${total !== 1 ? 's' : ''}`,
          size: isMobile ? 'small' : 'default',
        }}
        scroll={{ x: isMobile ? 500 : undefined }}
        size={isMobile ? 'small' : 'middle'}
        locale={{
          emptyText: (
            <Empty
              description={`No appointments for ${clientName}`}
              image={Empty.PRESENTED_IMAGE_SIMPLE}
            >
              {isActive && (
                <Button
                  type="primary"
                  icon={<PlusOutlined />}
                  onClick={() => router.push(`/appointments/new?clientId=${clientId}`)}
                >
                  Schedule Appointment
                </Button>
              )}
            </Empty>
          ),
        }}
      />

      {/* Complete Appointment Modal */}
      <Modal
        title="Complete Appointment"
        open={completeModalOpen}
        onOk={() => void handleComplete()}
        onCancel={() => {
          setCompleteModalOpen(false);
          setOutcomeNotes('');
          setSelectedAppointment(null);
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
          setSelectedAppointment(null);
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
          setSelectedAppointment(null);
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
          setSelectedAppointment(null);
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
    </div>
  );
}
