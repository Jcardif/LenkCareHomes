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
} from 'antd';
import type { MenuProps } from 'antd';
import {
  PlusOutlined,
  ReloadOutlined,
  FilterOutlined,
  WarningOutlined,
  ExclamationCircleOutlined,
  AlertOutlined,
  MoreOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  EyeOutlined,
} from '@ant-design/icons';

const { useBreakpoint } = Grid;
import { useRouter } from 'next/navigation';
import dayjs from 'dayjs';
import type { ColumnsType } from 'antd/es/table';

import { incidentsApi, homesApi, ApiError } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import type {
  IncidentSummary,
  IncidentType,
  IncidentStatus,
  HomeSummary,
} from '@/types';

const { Text } = Typography;
const { RangePicker } = DatePicker;

// Helper to render severity badge
function getSeverityBadge(severity: number) {
  const colors: Record<number, string> = {
    1: 'green',
    2: 'lime',
    3: 'gold',
    4: 'orange',
    5: 'red',
  };
  const labels: Record<number, string> = {
    1: 'Minor',
    2: 'Low',
    3: 'Mod',
    4: 'High',
    5: 'Severe',
  };
  return (
    <Tag color={colors[severity]} title={labels[severity]}>
      {severity}
    </Tag>
  );
}

interface IncidentListProps {
  clientId?: string;
  homeId?: string;
  showFilters?: boolean;
  showCreateButton?: boolean;
  pageSize?: number;
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

const INCIDENT_STATUSES: { label: string; value: IncidentStatus; color: string }[] = [
  { label: 'Draft', value: 'Draft', color: 'default' },
  { label: 'Submitted', value: 'Submitted', color: 'blue' },
  { label: 'Under Review', value: 'UnderReview', color: 'orange' },
  { label: 'Closed', value: 'Closed', color: 'green' },
];

function getTypeTag(type: IncidentType) {
  const t = INCIDENT_TYPES.find((x) => x.value === type);
  return <Tag color={t?.color}>{t?.label || type}</Tag>;
}

function getStatusTag(status: IncidentStatus) {
  const s = INCIDENT_STATUSES.find((x) => x.value === status);
  return <Tag color={s?.color}>{s?.label || status}</Tag>;
}

export default function IncidentList({
  clientId,
  homeId,
  showFilters = true,
  showCreateButton = true,
  pageSize = 10,
}: IncidentListProps) {
  const router = useRouter();
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Admin');
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;
  
  const [incidents, setIncidents] = useState<IncidentSummary[]>([]);
  const [homes, setHomes] = useState<HomeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [updatingStatus, setUpdatingStatus] = useState<string | null>(null);
  const [filtersExpanded, setFiltersExpanded] = useState(false);

  // Filters
  const [filterHomeId, setFilterHomeId] = useState<string | undefined>(homeId);
  const [filterStatus, setFilterStatus] = useState<IncidentStatus | undefined>();
  const [filterType, setFilterType] = useState<IncidentType | undefined>();
  const [filterDateRange, setFilterDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);

  const handleStatusChange = async (incidentId: string, newStatus: IncidentStatus) => {
    try {
      setUpdatingStatus(incidentId);
      const result = await incidentsApi.updateStatus(incidentId, newStatus);
      if (result.success) {
        void message.success(`Status changed to ${INCIDENT_STATUSES.find(s => s.value === newStatus)?.label}`);
        // Update local state
        setIncidents(prev => prev.map(i => 
          i.id === incidentId ? { ...i, status: newStatus } : i
        ));
      } else {
        void message.error(result.error || 'Failed to update status');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to update status';
      void message.error(msg);
    } finally {
      setUpdatingStatus(null);
    }
  };

  const fetchIncidents = useCallback(async () => {
    try {
      setLoading(true);
      const params = {
        homeId: filterHomeId,
        clientId,
        status: filterStatus,
        incidentType: filterType,
        startDate: filterDateRange?.[0]?.format('YYYY-MM-DD'),
        endDate: filterDateRange?.[1]?.format('YYYY-MM-DD'),
        pageNumber: currentPage,
        pageSize,
      };

      const result = await incidentsApi.getAll(params);
      setIncidents(result.items);
      setTotalCount(result.totalCount);
    } catch {
      // Silent fail - empty list is shown
      setIncidents([]);
    } finally {
      setLoading(false);
    }
  }, [clientId, filterHomeId, filterStatus, filterType, filterDateRange, currentPage, pageSize]);

  const fetchHomes = useCallback(async () => {
    try {
      const data = await homesApi.getAll(false);
      setHomes(data);
    } catch {
      // Silent fail
    }
  }, []);

  useEffect(() => {
    void fetchIncidents();
  }, [fetchIncidents]);

  useEffect(() => {
    if (showFilters && !homeId) {
      void fetchHomes();
    }
  }, [showFilters, homeId, fetchHomes]);

  const handleCreate = () => {
    if (clientId) {
      router.push(`/incidents/new?clientId=${clientId}`);
    } else {
      router.push('/incidents/new');
    }
  };

  const handleRowClick = (record: IncidentSummary) => {
    router.push(`/incidents/${record.id}`);
  };

  const columns: ColumnsType<IncidentSummary> = [
    {
      title: 'Incident',
      dataIndex: 'incidentNumber',
      key: 'incidentNumber',
      width: isMobile ? 100 : 140,
      render: (value, record) => (
        <Space size={4}>
          {record.severity >= 4 && <AlertOutlined style={{ color: '#ff4d4f' }} />}
          <Text strong style={{ whiteSpace: 'nowrap', fontSize: isSmallMobile ? 12 : 14 }}>{value}</Text>
        </Space>
      ),
    },
    {
      title: 'Date',
      dataIndex: 'occurredAt',
      key: 'occurredAt',
      width: isMobile ? 90 : 150,
      render: (value) => isSmallMobile 
        ? dayjs(value).format('M/D/YY')
        : dayjs(value).format('MMM D, YYYY h:mm A'),
      sorter: (a, b) => dayjs(a.occurredAt).unix() - dayjs(b.occurredAt).unix(),
      defaultSortOrder: 'descend',
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
          } as ColumnsType<IncidentSummary>[number],
        ]),
    ...(homeId || clientId
      ? []
      : [
          {
            title: 'Home',
            dataIndex: 'homeName',
            key: 'homeName',
            responsive: ['lg'] as const,
          } as ColumnsType<IncidentSummary>[number],
        ]),
    {
      title: 'Type',
      dataIndex: 'incidentType',
      key: 'incidentType',
      responsive: ['md'] as const,
      render: getTypeTag,
      filters: INCIDENT_TYPES.map((t) => ({ text: t.label, value: t.value })),
      onFilter: (value, record) => record.incidentType === value,
    },
    {
      title: 'Sev',
      dataIndex: 'severity',
      key: 'severity',
      width: 60,
      align: 'center',
      render: getSeverityBadge,
      sorter: (a, b) => a.severity - b.severity,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      width: isMobile ? 80 : 120,
      responsive: ['sm'] as const,
      render: getStatusTag,
      filters: INCIDENT_STATUSES.map((s) => ({ text: s.label, value: s.value })),
      onFilter: (value, record) => record.status === value,
    },
    {
      title: 'Reported By',
      dataIndex: 'reportedByName',
      key: 'reportedByName',
      responsive: ['xl'] as const,
    },
    ...(isAdmin ? [{
      title: '',
      key: 'actions',
      width: isMobile ? 50 : 100,
      render: (_: unknown, record: IncidentSummary) => {
        // Don't show actions for drafts or closed incidents
        if (record.status === 'Draft' || record.status === 'Closed') {
          return (
            <Button
              type="text"
              size="small"
              icon={<EyeOutlined />}
              onClick={(e) => {
                e.stopPropagation();
                router.push(`/incidents/${record.id}`);
              }}
              style={{ minWidth: 44, minHeight: 44 }}
            />
          );
        }

        const getNextStatuses = (currentStatus: IncidentStatus): MenuProps['items'] => {
          const items: MenuProps['items'] = [
            {
              key: 'view',
              icon: <EyeOutlined />,
              label: 'View Details',
              onClick: () => router.push(`/incidents/${record.id}`),
            },
            { type: 'divider' },
          ];

          if (currentStatus === 'Submitted') {
            items.push({
              key: 'UnderReview',
              icon: <ClockCircleOutlined />,
              label: 'Start Review',
              onClick: () => void handleStatusChange(record.id, 'UnderReview'),
            });
          }

          if (currentStatus === 'Submitted' || currentStatus === 'UnderReview') {
            items.push({
              key: 'Closed',
              icon: <CheckCircleOutlined />,
              label: 'Close Incident',
              onClick: () => void handleStatusChange(record.id, 'Closed'),
            });
          }

          return items;
        };

        return (
          <Dropdown
            menu={{ items: getNextStatuses(record.status) }}
            trigger={['click']}
            disabled={updatingStatus === record.id}
          >
            <Button
              type="text"
              size="small"
              icon={<MoreOutlined />}
              loading={updatingStatus === record.id}
              onClick={(e) => e.stopPropagation()}
              style={{ minWidth: 44, minHeight: 44 }}
            />
          </Dropdown>
        );
      },
    }] as ColumnsType<IncidentSummary> : []),
  ];

  return (
    <Card
      className="responsive-card"
      title={
        <Space>
          <ExclamationCircleOutlined />
          <span>{isSmallMobile ? 'Incidents' : 'Incidents'}</span>
          {totalCount > 0 && (
            <Tag>{totalCount}</Tag>
          )}
        </Space>
      }
      extra={
        <Space size={isMobile ? 'small' : 'middle'}>
          {!isMobile && (
            <Button icon={<ReloadOutlined />} onClick={() => void fetchIncidents()}>
              Refresh
            </Button>
          )}
          {showCreateButton && (
            <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
              {isSmallMobile ? 'Report' : 'Report Incident'}
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
                    onClick={() => void fetchIncidents()}
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
                      options={INCIDENT_STATUSES.map((s) => ({ label: s.label, value: s.value }))}
                    />
                  </Col>
                  <Col xs={12}>
                    <Select
                      placeholder="Type"
                      allowClear
                      style={{ width: '100%' }}
                      value={filterType}
                      onChange={setFilterType}
                      options={INCIDENT_TYPES.map((t) => ({ label: t.label, value: t.value }))}
                    />
                  </Col>
                  <Col xs={24}>
                    <RangePicker
                      value={filterDateRange}
                      onChange={(dates) => setFilterDateRange(dates as [dayjs.Dayjs, dayjs.Dayjs] | null)}
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
                  options={INCIDENT_STATUSES.map((s) => ({ label: s.label, value: s.value }))}
                />
                <Select
                  placeholder="Type"
                  allowClear
                  style={{ width: 140 }}
                  value={filterType}
                  onChange={setFilterType}
                  options={INCIDENT_TYPES.map((t) => ({ label: t.label, value: t.value }))}
                />
                <RangePicker
                  value={filterDateRange}
                  onChange={(dates) => setFilterDateRange(dates as [dayjs.Dayjs, dayjs.Dayjs] | null)}
                  placeholder={['Start Date', 'End Date']}
                />
              </Space>
            </Flex>
          )}
        </div>
      )}

      <Table
        columns={columns}
        dataSource={incidents}
        rowKey="id"
        loading={loading}
        scroll={{ x: 'max-content' }}
        pagination={{
          current: currentPage,
          pageSize,
          total: totalCount,
          onChange: setCurrentPage,
          showSizeChanger: false,
          showTotal: isMobile ? undefined : (total) => `${total} incidents`,
          size: isMobile ? 'small' : 'default',
        }}
        onRow={(record) => ({
          onClick: () => handleRowClick(record),
          style: { cursor: 'pointer' },
        })}
        locale={{
          emptyText: (
            <Empty
              image={<WarningOutlined style={{ fontSize: 48, color: '#d9d9d9' }} />}
              description="No incidents found"
            />
          ),
        }}
      />
    </Card>
  );
}
