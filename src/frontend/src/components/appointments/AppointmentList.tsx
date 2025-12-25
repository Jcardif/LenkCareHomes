'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Table,
  Card,
  Button,
  Space,
  Tag,
  Typography,
  Select,
  DatePicker,
  Flex,
  Empty,
  Dropdown,
  message,
  Grid,
  Row,
  Col,
  Modal,
  Input,
} from 'antd';
import type { MenuProps } from 'antd';
import {
  PlusOutlined,
  ReloadOutlined,
  FilterOutlined,
  CalendarOutlined,
  MoreOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  EyeOutlined,
  EditOutlined,
  DeleteOutlined,
  MedicineBoxOutlined,
  ClockCircleOutlined,
  SwapOutlined,
} from '@ant-design/icons';

const { useBreakpoint } = Grid;
import { useRouter } from 'next/navigation';
import dayjs from 'dayjs';
import type { ColumnsType } from 'antd/es/table';

import { appointmentsApi, homesApi, ApiError } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import type {
  AppointmentSummary,
  AppointmentType,
  AppointmentStatus,
  HomeSummary,
} from '@/types';

const { Text } = Typography;
const { RangePicker } = DatePicker;
const { TextArea } = Input;

/** Appointment type definitions with display labels and colors */
export const APPOINTMENT_TYPES: { label: string; value: AppointmentType; color: string }[] = [
  { label: 'General Practice', value: 'GeneralPractice', color: 'blue' },
  { label: 'Dental', value: 'Dental', color: 'cyan' },
  { label: 'Ophthalmology', value: 'Ophthalmology', color: 'geekblue' },
  { label: 'Podiatry', value: 'Podiatry', color: 'purple' },
  { label: 'Physical Therapy', value: 'PhysicalTherapy', color: 'green' },
  { label: 'Occupational Therapy', value: 'OccupationalTherapy', color: 'lime' },
  { label: 'Speech Therapy', value: 'SpeechTherapy', color: 'gold' },
  { label: 'Psychiatry', value: 'Psychiatry', color: 'orange' },
  { label: 'Dermatology', value: 'Dermatology', color: 'volcano' },
  { label: 'Cardiology', value: 'Cardiology', color: 'red' },
  { label: 'Neurology', value: 'Neurology', color: 'magenta' },
  { label: 'Lab Work', value: 'LabWork', color: 'default' },
  { label: 'Imaging', value: 'Imaging', color: 'default' },
  { label: 'Audiology', value: 'Audiology', color: 'cyan' },
  { label: 'Social Worker', value: 'SocialWorker', color: 'blue' },
  { label: 'Family Visit', value: 'FamilyVisit', color: 'green' },
  { label: 'Other', value: 'Other', color: 'default' },
];

/** Appointment status definitions with display labels and colors */
export const APPOINTMENT_STATUSES: { label: string; value: AppointmentStatus; color: string }[] = [
  { label: 'Scheduled', value: 'Scheduled', color: 'blue' },
  { label: 'Completed', value: 'Completed', color: 'green' },
  { label: 'Cancelled', value: 'Cancelled', color: 'default' },
  { label: 'No Show', value: 'NoShow', color: 'red' },
  { label: 'Rescheduled', value: 'Rescheduled', color: 'orange' },
];

/** Get display label for appointment type */
export function getAppointmentTypeLabel(type: AppointmentType): string {
  return APPOINTMENT_TYPES.find((x) => x.value === type)?.label || type;
}

/** Render appointment type tag */
export function getTypeTag(type: AppointmentType) {
  const t = APPOINTMENT_TYPES.find((x) => x.value === type);
  return <Tag color={t?.color}>{t?.label || type}</Tag>;
}

/** Render appointment status tag */
export function getStatusTag(status: AppointmentStatus) {
  const s = APPOINTMENT_STATUSES.find((x) => x.value === status);
  return <Tag color={s?.color}>{s?.label || status}</Tag>;
}

interface AppointmentListProps {
  clientId?: string;
  homeId?: string;
  showFilters?: boolean;
  showCreateButton?: boolean;
  pageSize?: number;
}

export default function AppointmentList({
  clientId,
  homeId,
  showFilters = true,
  showCreateButton = true,
  pageSize = 10,
}: AppointmentListProps) {
  const router = useRouter();
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Admin');
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [appointments, setAppointments] = useState<AppointmentSummary[]>([]);
  const [homes, setHomes] = useState<HomeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [filtersExpanded, setFiltersExpanded] = useState(false);

  // Modal states
  const [completeModalOpen, setCompleteModalOpen] = useState(false);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [noShowModalOpen, setNoShowModalOpen] = useState(false);
  const [rescheduleModalOpen, setRescheduleModalOpen] = useState(false);
  const [selectedAppointmentId, setSelectedAppointmentId] = useState<string | null>(null);
  const [outcomeNotes, setOutcomeNotes] = useState('');
  const [cancellationReason, setCancellationReason] = useState('');
  const [noShowNotes, setNoShowNotes] = useState('');
  const [rescheduleDate, setRescheduleDate] = useState<dayjs.Dayjs | null>(null);
  const [rescheduleNotes, setRescheduleNotes] = useState('');

  // Filters
  const [filterHomeId, setFilterHomeId] = useState<string | undefined>(homeId);
  const [filterStatus, setFilterStatus] = useState<AppointmentStatus | undefined>();
  const [filterType, setFilterType] = useState<AppointmentType | undefined>();
  const [filterDateRange, setFilterDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);
  const [sortDescending, setSortDescending] = useState(true); // Default to most recent first

  const fetchAppointments = useCallback(async () => {
    try {
      setLoading(true);
      const params = {
        homeId: filterHomeId,
        clientId,
        status: filterStatus,
        appointmentType: filterType,
        startDate: filterDateRange?.[0]?.format('YYYY-MM-DD'),
        endDate: filterDateRange?.[1]?.format('YYYY-MM-DD'),
        pageNumber: currentPage,
        pageSize,
        sortDescending,
      };

      const result = await appointmentsApi.getAll(params);
      setAppointments(result.items);
      setTotalCount(result.totalCount);
    } catch {
      setAppointments([]);
    } finally {
      setLoading(false);
    }
  }, [clientId, filterHomeId, filterStatus, filterType, filterDateRange, currentPage, pageSize, sortDescending]);

  const fetchHomes = useCallback(async () => {
    try {
      const data = await homesApi.getAll(false);
      setHomes(data);
    } catch {
      // Silent fail
    }
  }, []);

  useEffect(() => {
    void fetchAppointments();
  }, [fetchAppointments]);

  useEffect(() => {
    if (showFilters && !homeId) {
      void fetchHomes();
    }
  }, [showFilters, homeId, fetchHomes]);

  const handleCreate = () => {
    if (clientId) {
      router.push(`/appointments/new?clientId=${clientId}`);
    } else {
      router.push('/appointments/new');
    }
  };

  const handleRowClick = (record: AppointmentSummary) => {
    router.push(`/appointments/${record.id}`);
  };

  const handleComplete = async () => {
    if (!selectedAppointmentId) return;
    try {
      setActionLoading(selectedAppointmentId);
      const result = await appointmentsApi.complete(selectedAppointmentId, { outcomeNotes });
      if (result.success) {
        void message.success('Appointment marked as completed');
        void fetchAppointments();
        setCompleteModalOpen(false);
        setOutcomeNotes('');
        setSelectedAppointmentId(null);
      } else {
        void message.error(result.error || 'Failed to complete appointment');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to complete appointment';
      void message.error(msg);
    } finally {
      setActionLoading(null);
    }
  };

  const handleCancel = async () => {
    if (!selectedAppointmentId) return;
    try {
      setActionLoading(selectedAppointmentId);
      const result = await appointmentsApi.cancel(selectedAppointmentId, { cancellationReason });
      if (result.success) {
        void message.success('Appointment cancelled');
        void fetchAppointments();
        setCancelModalOpen(false);
        setCancellationReason('');
        setSelectedAppointmentId(null);
      } else {
        void message.error(result.error || 'Failed to cancel appointment');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to cancel appointment';
      void message.error(msg);
    } finally {
      setActionLoading(null);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      setActionLoading(id);
      const result = await appointmentsApi.delete(id);
      if (result.success) {
        void message.success('Appointment deleted');
        void fetchAppointments();
      } else {
        void message.error(result.error || 'Failed to delete appointment');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to delete appointment';
      void message.error(msg);
    } finally {
      setActionLoading(null);
    }
  };

  const openCompleteModal = (id: string) => {
    setSelectedAppointmentId(id);
    setOutcomeNotes('');
    setCompleteModalOpen(true);
  };

  const openCancelModal = (id: string) => {
    setSelectedAppointmentId(id);
    setCancellationReason('');
    setCancelModalOpen(true);
  };

  const openNoShowModal = (id: string) => {
    setSelectedAppointmentId(id);
    setNoShowNotes('');
    setNoShowModalOpen(true);
  };

  const openRescheduleModal = (id: string) => {
    setSelectedAppointmentId(id);
    setRescheduleDate(null);
    setRescheduleNotes('');
    setRescheduleModalOpen(true);
  };

  const handleNoShow = async () => {
    if (!selectedAppointmentId) return;
    try {
      setActionLoading(selectedAppointmentId);
      const result = await appointmentsApi.noShow(selectedAppointmentId, { notes: noShowNotes });
      if (result.success) {
        void message.success('Appointment marked as no-show');
        void fetchAppointments();
        setNoShowModalOpen(false);
        setNoShowNotes('');
        setSelectedAppointmentId(null);
      } else {
        void message.error(result.error || 'Failed to mark as no-show');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to mark as no-show';
      void message.error(msg);
    } finally {
      setActionLoading(null);
    }
  };

  const handleReschedule = async () => {
    if (!selectedAppointmentId || !rescheduleDate) return;
    try {
      setActionLoading(selectedAppointmentId);
      const result = await appointmentsApi.reschedule(selectedAppointmentId, {
        newScheduledAt: rescheduleDate.toISOString(),
        notes: rescheduleNotes,
      });
      if (result.success) {
        void message.success('Appointment rescheduled');
        void fetchAppointments();
        setRescheduleModalOpen(false);
        setRescheduleDate(null);
        setRescheduleNotes('');
        setSelectedAppointmentId(null);
      } else {
        void message.error(result.error || 'Failed to reschedule appointment');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to reschedule appointment';
      void message.error(msg);
    } finally {
      setActionLoading(null);
    }
  };

  const columns: ColumnsType<AppointmentSummary> = [
    {
      title: 'Date & Time',
      dataIndex: 'scheduledAt',
      key: 'scheduledAt',
      width: isMobile ? 100 : 160,
      render: (value) =>
        isSmallMobile
          ? dayjs(value).format('M/D/YY h:mm A')
          : dayjs(value).format('MMM D, YYYY h:mm A'),
      sorter: true,
      sortOrder: sortDescending ? 'descend' : 'ascend',
      sortDirections: ['descend', 'ascend', 'descend'], // No null/cancel state - cycle between desc and asc
      showSorterTooltip: { title: sortDescending ? 'Click to sort oldest first' : 'Click to sort newest first' },
    },
    {
      title: 'Title',
      dataIndex: 'title',
      key: 'title',
      ellipsis: true,
      render: (value, record) => (
        <Space size={4}>
          <Text strong style={{ fontSize: isSmallMobile ? 12 : 14 }}>{value}</Text>
          {record.status === 'Scheduled' && dayjs(record.scheduledAt).isBefore(dayjs()) && (
            <Tag color="red" style={{ fontSize: 10 }}>Overdue</Tag>
          )}
        </Space>
      ),
    },
    ...(clientId
      ? []
      : [
          {
            title: 'Client',
            dataIndex: 'clientName',
            key: 'clientName',
            ellipsis: true,
            responsive: ['sm'] as const,
          } as ColumnsType<AppointmentSummary>[number],
        ]),
    ...(homeId || clientId
      ? []
      : [
          {
            title: 'Home',
            dataIndex: 'homeName',
            key: 'homeName',
            responsive: ['lg'] as const,
          } as ColumnsType<AppointmentSummary>[number],
        ]),
    {
      title: 'Type',
      dataIndex: 'appointmentType',
      key: 'appointmentType',
      responsive: ['md'] as const,
      render: getTypeTag,
      filters: APPOINTMENT_TYPES.map((t) => ({ text: t.label, value: t.value })),
      onFilter: (value, record) => record.appointmentType === value,
    },
    {
      title: 'Duration',
      dataIndex: 'durationMinutes',
      key: 'durationMinutes',
      width: 80,
      responsive: ['lg'] as const,
      render: (value) => `${value} min`,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      width: isMobile ? 90 : 110,
      render: getStatusTag,
      filters: APPOINTMENT_STATUSES.map((s) => ({ text: s.label, value: s.value })),
      onFilter: (value, record) => record.status === value,
    },
    {
      title: 'Location',
      dataIndex: 'location',
      key: 'location',
      ellipsis: true,
      responsive: ['xl'] as const,
    },
    {
      title: '',
      key: 'actions',
      width: isMobile ? 50 : 100,
      render: (_: unknown, record: AppointmentSummary) => {
        const items: MenuProps['items'] = [
          {
            key: 'view',
            icon: <EyeOutlined />,
            label: 'View Details',
            onClick: () => router.push(`/appointments/${record.id}`),
          },
        ];

        // Only show action buttons for scheduled appointments
        if (record.status === 'Scheduled') {
          items.push(
            { type: 'divider' },
            {
              key: 'edit',
              icon: <EditOutlined />,
              label: 'Edit',
              onClick: () => router.push(`/appointments/${record.id}/edit`),
            },
            {
              key: 'complete',
              icon: <CheckCircleOutlined />,
              label: 'Mark Completed',
              onClick: () => openCompleteModal(record.id),
            },
            {
              key: 'noshow',
              icon: <ClockCircleOutlined />,
              label: 'Mark No-Show',
              onClick: () => openNoShowModal(record.id),
            },
            {
              key: 'reschedule',
              icon: <SwapOutlined />,
              label: 'Reschedule',
              onClick: () => openRescheduleModal(record.id),
            },
            {
              key: 'cancel',
              icon: <CloseCircleOutlined />,
              label: 'Cancel',
              onClick: () => openCancelModal(record.id),
            }
          );

          if (isAdmin) {
            items.push(
              { type: 'divider' },
              {
                key: 'delete',
                icon: <DeleteOutlined />,
                label: 'Delete',
                danger: true,
                onClick: () => void handleDelete(record.id),
              }
            );
          }
        }

        return (
          <Dropdown
            menu={{ items }}
            trigger={['click']}
            disabled={actionLoading === record.id}
          >
            <Button
              type="text"
              size="small"
              icon={<MoreOutlined />}
              loading={actionLoading === record.id}
              onClick={(e) => e.stopPropagation()}
              style={{ minWidth: 44, minHeight: 44 }}
            />
          </Dropdown>
        );
      },
    },
  ];

  return (
    <>
      <Card
        className="responsive-card"
        title={
          <Space>
            <CalendarOutlined />
            <span>{isSmallMobile ? 'Appointments' : 'Appointments'}</span>
            {totalCount > 0 && <Tag>{totalCount}</Tag>}
          </Space>
        }
        extra={
          <Space size={isMobile ? 'small' : 'middle'}>
            {!isMobile && (
              <Button icon={<ReloadOutlined />} onClick={() => void fetchAppointments()}>
                Refresh
              </Button>
            )}
            {showCreateButton && (
              <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
                {isSmallMobile ? 'New' : 'New Appointment'}
              </Button>
            )}
          </Space>
        }
      >
        {showFilters && (
          <div style={{ marginBottom: 16 }}>
            {isMobile ? (
              <>
                <Row gutter={[8, 8]} style={{ marginBottom: 8 }}>
                  <Col span={12}>
                    <Button
                      icon={<FilterOutlined />}
                      onClick={() => setFiltersExpanded(!filtersExpanded)}
                      style={{ width: '100%' }}
                    >
                      Filters
                    </Button>
                  </Col>
                  <Col span={12}>
                    <Button
                      icon={<ReloadOutlined />}
                      onClick={() => void fetchAppointments()}
                      style={{ width: '100%' }}
                    >
                      Refresh
                    </Button>
                  </Col>
                </Row>
                {filtersExpanded && (
                  <Row gutter={[8, 8]}>
                    {!homeId && (
                      <Col xs={24}>
                        <Select
                          placeholder="Filter by home"
                          allowClear
                          style={{ width: '100%' }}
                          value={filterHomeId}
                          onChange={setFilterHomeId}
                          options={homes.map((h) => ({ label: h.name, value: h.id }))}
                        />
                      </Col>
                    )}
                    <Col xs={12}>
                      <Select
                        placeholder="Status"
                        allowClear
                        style={{ width: '100%' }}
                        value={filterStatus}
                        onChange={setFilterStatus}
                        options={APPOINTMENT_STATUSES.map((s) => ({ label: s.label, value: s.value }))}
                      />
                    </Col>
                    <Col xs={12}>
                      <Select
                        placeholder="Type"
                        allowClear
                        style={{ width: '100%' }}
                        value={filterType}
                        onChange={setFilterType}
                        options={APPOINTMENT_TYPES.map((t) => ({ label: t.label, value: t.value }))}
                      />
                    </Col>
                    <Col xs={24}>
                      <RangePicker
                        value={filterDateRange}
                        onChange={(dates) =>
                          setFilterDateRange(dates as [dayjs.Dayjs, dayjs.Dayjs] | null)
                        }
                        placeholder={['Start', 'End']}
                        style={{ width: '100%' }}
                      />
                    </Col>
                  </Row>
                )}
              </>
            ) : (
              <Flex gap={16} wrap="wrap">
                <Space>
                  <FilterOutlined />
                  {!homeId && (
                    <Select
                      placeholder="Filter by home"
                      allowClear
                      style={{ width: 180 }}
                      value={filterHomeId}
                      onChange={setFilterHomeId}
                      options={homes.map((h) => ({ label: h.name, value: h.id }))}
                    />
                  )}
                  <Select
                    placeholder="Status"
                    allowClear
                    style={{ width: 140 }}
                    value={filterStatus}
                    onChange={setFilterStatus}
                    options={APPOINTMENT_STATUSES.map((s) => ({ label: s.label, value: s.value }))}
                  />
                  <Select
                    placeholder="Type"
                    allowClear
                    style={{ width: 180 }}
                    value={filterType}
                    onChange={setFilterType}
                    options={APPOINTMENT_TYPES.map((t) => ({ label: t.label, value: t.value }))}
                  />
                  <RangePicker
                    value={filterDateRange}
                    onChange={(dates) =>
                      setFilterDateRange(dates as [dayjs.Dayjs, dayjs.Dayjs] | null)
                    }
                    placeholder={['Start Date', 'End Date']}
                  />
                </Space>
              </Flex>
            )}
          </div>
        )}

        <Table
          columns={columns}
          dataSource={appointments}
          rowKey="id"
          loading={loading}
          scroll={{ x: 'max-content' }}
          onChange={(_pagination, _filters, sorter) => {
            // Handle sort order change for scheduledAt column
            if (!Array.isArray(sorter) && sorter.columnKey === 'scheduledAt' && sorter.order) {
              const newSortDescending = sorter.order === 'descend';
              if (newSortDescending !== sortDescending) {
                setSortDescending(newSortDescending);
                setCurrentPage(1); // Reset to first page when sort changes
              }
            }
          }}
          pagination={{
            current: currentPage,
            pageSize,
            total: totalCount,
            onChange: setCurrentPage,
            showSizeChanger: false,
            showTotal: isMobile ? undefined : (total) => `${total} appointments`,
            size: isMobile ? 'small' : 'default',
          }}
          onRow={(record) => ({
            onClick: () => handleRowClick(record),
            style: { cursor: 'pointer' },
          })}
          locale={{
            emptyText: (
              <Empty
                image={<MedicineBoxOutlined style={{ fontSize: 48, color: '#d9d9d9' }} />}
                description="No appointments found"
              />
            ),
          }}
        />
      </Card>

      {/* Complete Appointment Modal */}
      <Modal
        title="Complete Appointment"
        open={completeModalOpen}
        onOk={() => void handleComplete()}
        onCancel={() => {
          setCompleteModalOpen(false);
          setSelectedAppointmentId(null);
          setOutcomeNotes('');
        }}
        confirmLoading={!!actionLoading}
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
          setSelectedAppointmentId(null);
          setCancellationReason('');
        }}
        confirmLoading={!!actionLoading}
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
          setSelectedAppointmentId(null);
          setNoShowNotes('');
        }}
        confirmLoading={!!actionLoading}
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
          setSelectedAppointmentId(null);
          setRescheduleDate(null);
          setRescheduleNotes('');
        }}
        confirmLoading={!!actionLoading}
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
    </>
  );
}
