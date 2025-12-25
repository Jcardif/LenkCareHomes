'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Typography,
  Card,
  Table,
  Tag,
  Space,
  Button,
  Select,
  DatePicker,
  Row,
  Col,
  Statistic,
  Alert,
  Tooltip,
  Input,
  Modal,
  Descriptions,
  message,
  Collapse,
  Segmented,
  Grid,
} from 'antd';

const { useBreakpoint } = Grid;
import {
  AuditOutlined,
  ReloadOutlined,
  FilterOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  StopOutlined,
  DownloadOutlined,
  InfoCircleOutlined,
  ClearOutlined,
  QuestionCircleOutlined,
  UnorderedListOutlined,
  CodeOutlined,
} from '@ant-design/icons';
import { ProtectedRoute, AuthenticatedLayout, ActivityFeedView } from '@/components';
import { useAuth } from '@/contexts/AuthContext';
import { auditApi, reportsApi, type AuditLogQueryParams } from '@/lib/api';
import type { AuditLogEntry, AuditLogStats } from '@/types';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';

dayjs.extend(relativeTime);

type ViewMode = 'activity' | 'technical';

const { Title, Paragraph, Text } = Typography;
const { RangePicker } = DatePicker;
const { Search } = Input;

// Action type display names and colors
const actionConfig: Record<string, { label: string; color: string }> = {
  LOGIN_SUCCESS: { label: 'Login Success', color: 'green' },
  LOGIN_FAILED: { label: 'Login Failed', color: 'red' },
  LOGOUT: { label: 'Logout', color: 'default' },
  MFA_SETUP: { label: 'MFA Setup', color: 'blue' },
  MFA_VERIFIED: { label: 'MFA Verified', color: 'green' },
  MFA_FAILED: { label: 'MFA Failed', color: 'red' },
  BACKUP_CODE_USED: { label: 'Backup Code Used', color: 'orange' },
  PASSWORD_RESET: { label: 'Password Reset', color: 'purple' },
  PASSWORD_RESET_REQUESTED: { label: 'Password Reset Request', color: 'purple' },
  USER_INVITED: { label: 'User Invited', color: 'cyan' },
  INVITATION_ACCEPTED: { label: 'Invitation Accepted', color: 'green' },
  ACCOUNT_SETUP_COMPLETED: { label: 'Account Setup Complete', color: 'green' },
  USER_CREATED: { label: 'User Created', color: 'cyan' },
  USER_UPDATED: { label: 'User Updated', color: 'blue' },
  USER_DEACTIVATED: { label: 'User Deactivated', color: 'orange' },
  USER_REACTIVATED: { label: 'User Reactivated', color: 'green' },
  USER_DELETED: { label: 'User Deleted', color: 'red' },
  ROLE_ASSIGNED: { label: 'Role Assigned', color: 'blue' },
  ROLE_REMOVED: { label: 'Role Removed', color: 'orange' },
  PHI_ACCESSED: { label: 'PHI Accessed', color: 'gold' },
  PHI_MODIFIED: { label: 'PHI Modified', color: 'volcano' },
  DOCUMENT_VIEWED: { label: 'Document Viewed', color: 'cyan' },
  DOCUMENT_UPLOADED: { label: 'Document Uploaded', color: 'blue' },
  DOCUMENT_DELETED: { label: 'Document Deleted', color: 'red' },
  DOCUMENT_ACCESS_GRANTED: { label: 'Document Access Granted', color: 'green' },
  DOCUMENT_ACCESS_REVOKED: { label: 'Document Access Revoked', color: 'orange' },
  API_READ: { label: 'API Read', color: 'default' },
  API_CREATE: { label: 'API Create', color: 'blue' },
  API_UPDATE: { label: 'API Update', color: 'orange' },
  API_DELETE: { label: 'API Delete', color: 'red' },
  API_REQUEST: { label: 'API Request', color: 'default' },
  USER_MANAGEMENT: { label: 'User Management', color: 'purple' },
  CLIENT_ADMITTED: { label: 'Client Admitted', color: 'green' },
  CLIENT_DISCHARGED: { label: 'Client Discharged', color: 'orange' },
  CLIENT_UPDATED: { label: 'Client Updated', color: 'blue' },
  CLIENT_VIEWED: { label: 'Client Viewed', color: 'cyan' },
  INCIDENT_CREATED: { label: 'Incident Created', color: 'red' },
  INCIDENT_UPDATED: { label: 'Incident Updated', color: 'orange' },
  INCIDENT_SUBMITTED: { label: 'Incident Submitted', color: 'blue' },
  ADL_LOGGED: { label: 'ADL Logged', color: 'green' },
  VITALS_LOGGED: { label: 'Vitals Logged', color: 'green' },
};

const outcomeConfig: Record<string, { icon: React.ReactNode; color: string }> = {
  Success: { icon: <CheckCircleOutlined />, color: 'green' },
  Failure: { icon: <CloseCircleOutlined />, color: 'red' },
  Denied: { icon: <StopOutlined />, color: 'orange' },
};

const resourceTypeOptions = [
  { value: 'clients', label: 'Clients' },
  { value: 'documents', label: 'Documents' },
  { value: 'incidents', label: 'Incidents' },
  { value: 'users', label: 'Users' },
  { value: 'homes', label: 'Homes' },
  { value: 'caregivers', label: 'Caregivers' },
  { value: 'auth', label: 'Authentication' },
];

const outcomeOptions = [
  { value: 'Success', label: 'Success' },
  { value: 'Failure', label: 'Failure' },
  { value: 'Denied', label: 'Denied' },
];

function AuditLogsContent() {
  const { hasRole } = useAuth();
  const isSysadmin = hasRole('Sysadmin');
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;
  
  // View mode - Admin always sees activity, Sysadmin defaults to technical but can toggle
  const [viewMode, setViewMode] = useState<ViewMode>(isSysadmin ? 'technical' : 'activity');
  
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [stats, setStats] = useState<AuditLogStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [statsLoading, setStatsLoading] = useState(true);
  const [exportLoading, setExportLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [continuationToken, setContinuationToken] = useState<string | undefined>();
  const [hasMore, setHasMore] = useState(false);
  const [selectedLog, setSelectedLog] = useState<AuditLogEntry | null>(null);
  const [detailModalOpen, setDetailModalOpen] = useState(false);
  
  // Filters
  const [actionFilter, setActionFilter] = useState<string | undefined>();
  const [resourceTypeFilter, setResourceTypeFilter] = useState<string | undefined>();
  const [outcomeFilter, setOutcomeFilter] = useState<string | undefined>();
  const [searchText, setSearchText] = useState<string>('');
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);
  const [filtersExpanded, setFiltersExpanded] = useState(false);

  // Build query params from filters
  const buildQueryParams = useCallback((token?: string): AuditLogQueryParams => {
    const params: AuditLogQueryParams = {
      pageSize: 50,
    };

    if (actionFilter) params.action = actionFilter;
    if (resourceTypeFilter) params.resourceType = resourceTypeFilter;
    if (outcomeFilter) params.outcome = outcomeFilter;
    if (searchText) params.searchText = searchText;
    if (dateRange) {
      params.fromDate = dateRange[0].toISOString();
      params.toDate = dateRange[1].toISOString();
    }
    if (token) params.continuationToken = token;

    return params;
  }, [actionFilter, resourceTypeFilter, outcomeFilter, searchText, dateRange]);

  const fetchLogs = useCallback(async (append = false, token?: string) => {
    try {
      setLoading(true);
      setError(null);
      
      const queryParams = buildQueryParams(append ? continuationToken : token);
      const response = await auditApi.getLogs(queryParams);
      
      if (append) {
        setLogs(prev => [...prev, ...response.entries]);
      } else {
        setLogs(response.entries);
      }
      
      setContinuationToken(response.continuationToken || undefined);
      setHasMore(!!response.continuationToken);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load audit logs';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [buildQueryParams, continuationToken]);

  const fetchStats = useCallback(async () => {
    try {
      setStatsLoading(true);
      const response = await auditApi.getStats();
      setStats(response);
    } catch (err) {
      console.error('Failed to load stats:', err);
    } finally {
      setStatsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchLogs();
    fetchStats();
    // Only run on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleRefresh = () => {
    fetchLogs();
    fetchStats();
  };

  const handleLoadMore = () => {
    if (continuationToken) {
      fetchLogs(true);
    }
  };

  const handleApplyFilters = () => {
    fetchLogs();
  };

  const handleClearFilters = () => {
    setActionFilter(undefined);
    setResourceTypeFilter(undefined);
    setOutcomeFilter(undefined);
    setSearchText('');
    setDateRange(null);
    // Fetch with no filters after state updates
    setTimeout(() => fetchLogs(), 0);
  };

  const handleExportCsv = async () => {
    try {
      setExportLoading(true);
      const params = buildQueryParams();
      // Remove pagination params for export
      const exportParams = {
        ...params,
        maxRecords: 10000,
      };
      delete (exportParams as { pageSize?: number }).pageSize;
      delete (exportParams as { continuationToken?: string }).continuationToken;

      const blob = await auditApi.exportCsv(exportParams);
      
      // Download the file
      const fileName = `audit_logs_${dayjs().format('YYYYMMDD_HHmmss')}.csv`;
      reportsApi.downloadBlob(blob, fileName);
      
      message.success('Audit logs exported successfully');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to export audit logs';
      message.error(errorMessage);
    } finally {
      setExportLoading(false);
    }
  };

  const handleRowClick = (record: AuditLogEntry) => {
    setSelectedLog(record);
    setDetailModalOpen(true);
  };

  const columns: ColumnsType<AuditLogEntry> = [
    {
      title: 'Time',
      dataIndex: 'timestamp',
      key: 'timestamp',
      width: isMobile ? 80 : 180,
      render: (timestamp: string) => (
        <Tooltip title={dayjs(timestamp).format('YYYY-MM-DD HH:mm:ss')}>
          <span>{isSmallMobile ? dayjs(timestamp).format('M/D h:mma') : dayjs(timestamp).fromNow()}</span>
        </Tooltip>
      ),
    },
    {
      title: 'Action',
      dataIndex: 'action',
      key: 'action',
      width: isMobile ? 100 : 180,
      render: (action: string) => {
        const config = actionConfig[action] || { label: action, color: 'default' };
        return <Tag color={config.color}>{isSmallMobile ? config.label.split(' ')[0] : config.label}</Tag>;
      },
    },
    {
      title: 'User',
      dataIndex: 'userEmail',
      key: 'userEmail',
      width: 200,
      responsive: ['sm'],
      ellipsis: true,
      render: (email: string | undefined) => email || <Text type="secondary">Anonymous</Text>,
    },
    {
      title: 'Outcome',
      dataIndex: 'outcome',
      key: 'outcome',
      width: isMobile ? 70 : 100,
      render: (outcome: string) => {
        const config = outcomeConfig[outcome] || { icon: null, color: 'default' };
        return (
          <Tag icon={isMobile ? undefined : config.icon} color={config.color}>
            {isSmallMobile ? outcome.charAt(0) : outcome}
          </Tag>
        );
      },
    },
    {
      title: 'Resource',
      key: 'resource',
      width: 150,
      responsive: ['md'],
      render: (_, record) => (
        record.resourceType ? (
          <span>
            <Text strong>{record.resourceType}</Text>
            {record.resourceId && <Text type="secondary"> #{record.resourceId.slice(0, 8)}</Text>}
          </span>
        ) : (
          <Text type="secondary">-</Text>
        )
      ),
    },
    {
      title: 'Path',
      dataIndex: 'requestPath',
      key: 'requestPath',
      responsive: ['lg'],
      ellipsis: true,
      render: (path: string | undefined, record) => (
        path ? (
          <Tooltip title={path}>
            <Text code>{record.httpMethod} {path}</Text>
          </Tooltip>
        ) : <Text type="secondary">-</Text>
      ),
    },
    {
      title: 'IP',
      dataIndex: 'ipAddress',
      key: 'ipAddress',
      width: 130,
      responsive: ['xl'],
      render: (ip: string | undefined) => ip || <Text type="secondary">-</Text>,
    },
    {
      title: 'Status',
      dataIndex: 'statusCode',
      key: 'statusCode',
      width: 80,
      responsive: ['lg'],
      render: (code: number | undefined) => (
        code ? (
          <Tag color={code >= 200 && code < 300 ? 'green' : code >= 400 ? 'red' : 'orange'}>
            {code}
          </Tag>
        ) : <Text type="secondary">-</Text>
      ),
    },
    {
      title: '',
      key: 'actions',
      width: 50,
      render: (_, record) => (
        <Tooltip title="View Details">
          <Button
            type="text"
            icon={<InfoCircleOutlined />}
            onClick={(e) => {
              e.stopPropagation();
              handleRowClick(record);
            }}
            aria-label="View audit log details"
            style={{ minWidth: 44, minHeight: 44 }}
          />
        </Tooltip>
      ),
    },
  ];

  const actionOptions = Object.entries(actionConfig).map(([value, config]) => ({
    value,
    label: config.label,
  }));

  const hasActiveFilters = actionFilter || resourceTypeFilter || outcomeFilter || searchText || dateRange;

  return (
    <div role="main" aria-label="Audit Logs">
      <div className="page-header-wrapper" style={{ marginBottom: 24 }}>
        <Space align="center">
          <AuditOutlined style={{ fontSize: isSmallMobile ? 24 : 28, color: '#5a7a6b' }} aria-hidden="true" />
          <div>
            <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>
              {isSysadmin ? (isSmallMobile ? 'Audit' : 'Audit Logs') : 'Activity Log'}
            </Title>
            {!isSmallMobile && (
              <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
                {isSysadmin 
                  ? 'System activity, PHI access records, and technical diagnostics'
                  : 'View who did what and when across the system'}
              </Paragraph>
            )}
          </div>
        </Space>
        
        {/* View mode toggle - only for Sysadmin */}
        {isSysadmin && (
          <Segmented
            value={viewMode}
            onChange={(value) => setViewMode(value as ViewMode)}
            size={isSmallMobile ? 'small' : 'middle'}
            options={[
              {
                label: (
                  <Space size={4}>
                    <UnorderedListOutlined />
                    {!isSmallMobile && <span>Activity</span>}
                  </Space>
                ),
                value: 'activity',
              },
              {
                label: (
                  <Space size={4}>
                    <CodeOutlined />
                    {!isSmallMobile && <span>Technical</span>}
                  </Space>
                ),
                value: 'technical',
              },
            ]}
            aria-label="Switch view mode"
          />
        )}
      </div>

      {/* Stats Cards */}
      {!statsLoading && stats && (
        <Row gutter={[16, 16]} style={{ marginBottom: 24 }} role="region" aria-label="Audit Statistics">
          <Col xs={24} sm={8}>
            <Card>
              <Statistic
                title="Total Events (24h)"
                value={Object.values(stats.actionCounts).reduce((a, b) => a + b, 0)}
                prefix={<AuditOutlined aria-hidden="true" />}
              />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card>
              <Statistic
                title="Login Events (24h)"
                value={(stats.actionCounts['LOGIN_SUCCESS'] || 0) + (stats.actionCounts['LOGIN_FAILED'] || 0)}
                valueStyle={{ color: '#5a7a6b' }}
              />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card>
              <Statistic
                title="Failed Logins (24h)"
                value={stats.actionCounts['LOGIN_FAILED'] || 0}
                valueStyle={{ color: stats.actionCounts['LOGIN_FAILED'] ? '#cf1322' : undefined }}
              />
            </Card>
          </Col>
        </Row>
      )}

      {/* Filters - Simplified for Activity view, Full for Technical view */}
      <Card 
        className="responsive-card"
        style={{ marginBottom: 16 }}
        role="search"
        aria-label="Activity filters"
      >
        {isMobile ? (
          <Space direction="vertical" style={{ width: '100%' }} size="middle">
            <Search
              placeholder={viewMode === 'activity' ? 'Search activity...' : 'Search logs...'}
              allowClear
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              onSearch={handleApplyFilters}
              aria-label="Search audit logs"
            />
            <Row gutter={[8, 8]}>
              <Col span={12}>
                <Select
                  placeholder="Type"
                  allowClear
                  style={{ width: '100%' }}
                  options={viewMode === 'activity' ? [
                    { value: 'auth', label: 'Sign-ins' },
                    { value: 'clients', label: 'Clients' },
                    { value: 'documents', label: 'Documents' },
                    { value: 'incidents', label: 'Incidents' },
                    { value: 'users', label: 'Users' },
                  ] : resourceTypeOptions}
                  value={resourceTypeFilter}
                  onChange={setResourceTypeFilter}
                />
              </Col>
              <Col span={12}>
                <Select
                  placeholder="Outcome"
                  allowClear
                  style={{ width: '100%' }}
                  options={outcomeOptions}
                  value={outcomeFilter}
                  onChange={setOutcomeFilter}
                />
              </Col>
            </Row>
            <RangePicker
              showTime={viewMode === 'technical'}
              value={dateRange}
              onChange={(dates) => setDateRange(dates as [dayjs.Dayjs, dayjs.Dayjs] | null)}
              style={{ width: '100%' }}
              placeholder={['Start', 'End']}
            />
            <Row gutter={8}>
              <Col span={8}>
                <Button 
                  type="primary" 
                  onClick={handleApplyFilters}
                  style={{ width: '100%' }}
                >
                  Search
                </Button>
              </Col>
              <Col span={8}>
                <Button 
                  icon={<ReloadOutlined />} 
                  onClick={handleRefresh}
                  style={{ width: '100%' }}
                >
                  Refresh
                </Button>
              </Col>
              <Col span={8}>
                <Button
                  icon={<DownloadOutlined />}
                  onClick={handleExportCsv}
                  loading={exportLoading}
                  style={{ width: '100%' }}
                >
                  Export
                </Button>
              </Col>
            </Row>
            {hasActiveFilters && (
              <Button
                icon={<ClearOutlined />}
                onClick={handleClearFilters}
                style={{ width: '100%' }}
              >
                Clear Filters
              </Button>
            )}
          </Space>
        ) : (
          <Space direction="vertical" style={{ width: '100%' }} size="middle">
            {/* Primary filter row */}
            <Space wrap size="middle">
              <FilterOutlined aria-hidden="true" />
              <Search
                placeholder={viewMode === 'activity' ? 'Search activity...' : 'Search logs...'}
                allowClear
                style={{ width: 250 }}
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
                onSearch={handleApplyFilters}
                aria-label="Search audit logs"
              />
              {viewMode === 'activity' ? (
                /* Simplified filters for Activity view */
                <Select
                  placeholder="Filter by type"
                  allowClear
                  style={{ width: 180 }}
                  options={[
                    { value: 'auth', label: '🔐 Sign-ins' },
                    { value: 'clients', label: '👥 Client Records' },
                    { value: 'documents', label: '📄 Documents' },
                    { value: 'incidents', label: '⚠️ Incidents' },
                    { value: 'users', label: '👤 User Management' },
                  ]}
                  value={resourceTypeFilter}
                  onChange={setResourceTypeFilter}
                  aria-label="Filter by activity type"
                />
              ) : (
                /* Full action filter for Technical view */
                <Select
                  placeholder="Filter by action"
                  allowClear
                  showSearch
                  style={{ width: 200 }}
                  options={actionOptions}
                  value={actionFilter}
                  onChange={setActionFilter}
                  filterOption={(input, option) =>
                    (option?.label as string)?.toLowerCase().includes(input.toLowerCase())
                  }
                  aria-label="Filter by action type"
                />
              )}
              <RangePicker
                showTime={viewMode === 'technical'}
                value={dateRange}
                onChange={(dates) => setDateRange(dates as [dayjs.Dayjs, dayjs.Dayjs] | null)}
                aria-label="Filter by date range"
              />
              <Button 
                type="primary" 
                onClick={handleApplyFilters}
                aria-label="Apply filters"
              >
                {viewMode === 'activity' ? 'Search' : 'Apply Filters'}
              </Button>
              <Button 
                icon={<ReloadOutlined aria-hidden="true" />} 
                onClick={handleRefresh}
                aria-label="Refresh"
              >
                Refresh
              </Button>
              {hasActiveFilters && (
                <Button
                  icon={<ClearOutlined aria-hidden="true" />}
                  onClick={handleClearFilters}
                  aria-label="Clear filters"
                >
                  Clear
                </Button>
              )}
              {/* Export button - always available */}
              <Button
                type="default"
                icon={<DownloadOutlined aria-hidden="true" />}
                onClick={handleExportCsv}
                loading={exportLoading}
                aria-label="Export activity to CSV"
              >
                Export
              </Button>
            </Space>

            {/* Advanced filters (collapsible) - Only show in Technical view */}
            {viewMode === 'technical' && (
              <Collapse 
                ghost 
                activeKey={filtersExpanded ? ['advanced'] : []}
                onChange={(keys) => setFiltersExpanded(keys.includes('advanced'))}
                items={[
                  {
                    key: 'advanced',
                    label: (
                      <Space>
                        <span>Advanced Filters</span>
                        {hasActiveFilters && (
                          <Tag color="blue" aria-label="Filters active">
                            Filters Active
                          </Tag>
                        )}
                      </Space>
                    ),
                    children: (
                      <Space wrap size="middle" style={{ paddingTop: 8 }}>
                        <Select
                          placeholder="Resource Type"
                          allowClear
                          style={{ width: 150 }}
                          options={resourceTypeOptions}
                          value={resourceTypeFilter}
                          onChange={setResourceTypeFilter}
                          aria-label="Filter by resource type"
                        />
                        <Select
                          placeholder="Outcome"
                          allowClear
                          style={{ width: 120 }}
                          options={outcomeOptions}
                          value={outcomeFilter}
                          onChange={setOutcomeFilter}
                          aria-label="Filter by outcome"
                        />
                      </Space>
                    ),
                  },
                ]}
              />
            )}
          </Space>
        )}
      </Card>

      {/* Help tip */}
      <Alert
        message={
          <Space>
            <QuestionCircleOutlined aria-hidden="true" />
            <span>
              {viewMode === 'activity' 
                ? 'This log shows all system activity. Failed logins and unusual activity are highlighted.'
                : 'Audit logs are retained for 6+ years per HIPAA requirements. Use filters to find specific events.'}
              <Button type="link" href="/help#audit-logs" style={{ padding: '0 4px' }}>
                Learn more
              </Button>
            </span>
          </Space>
        }
        type="info"
        showIcon={false}
        style={{ marginBottom: 16 }}
        closable
        role="note"
      />

      {/* Error Alert */}
      {error && viewMode === 'technical' && (
        <Alert
          message="Error Loading Audit Logs"
          description={error}
          type="error"
          showIcon
          style={{ marginBottom: 16 }}
          closable
          onClose={() => setError(null)}
          role="alert"
        />
      )}

      {/* Content - Activity Feed or Technical Table */}
      {viewMode === 'activity' ? (
        <ActivityFeedView
          logs={logs}
          loading={loading}
          error={error}
          hasMore={hasMore}
          onLoadMore={handleLoadMore}
        />
      ) : (
        <>
          {/* Logs Table - Technical View */}
          <Card className="responsive-card">
            <Table
              columns={columns}
              dataSource={logs}
              rowKey="id"
              loading={loading}
              pagination={false}
              scroll={{ x: 'max-content' }}
              size={isMobile ? 'small' : 'middle'}
              onRow={(record) => ({
                onClick: () => handleRowClick(record),
                style: { cursor: 'pointer' },
                role: 'button',
                tabIndex: 0,
                'aria-label': `Audit log entry: ${record.action} by ${record.userEmail || 'Anonymous'} at ${dayjs(record.timestamp).format('YYYY-MM-DD HH:mm')}`,
                onKeyPress: (e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    handleRowClick(record);
                  }
                },
              })}
              locale={{
                emptyText: (
                  <div style={{ padding: isMobile ? 20 : 40 }} role="status">
                    <AuditOutlined style={{ fontSize: isMobile ? 36 : 48, color: '#5a7a6b', marginBottom: 16 }} aria-hidden="true" />
                    <div>
                      <Title level={5} style={{ color: '#2d3732', marginBottom: 8 }}>No audit logs found</Title>
                      <Paragraph style={{ color: '#6b7770' }}>
                        {hasActiveFilters 
                          ? 'Try adjusting your filters to see more results.'
                          : 'System activity will appear here.'}
                      </Paragraph>
                    </div>
                  </div>
                ),
              }}
              aria-label="Audit logs table"
            />
            
            {hasMore && (
              <div style={{ textAlign: 'center', marginTop: 16 }}>
                <Button 
                  onClick={handleLoadMore} 
                  loading={loading}
                  aria-label="Load more audit logs"
                >
                  Load More
                </Button>
              </div>
            )}
          </Card>
        </>
      )}

      {/* Detail Modal */}
      <Modal
        title="Audit Log Details"
        open={detailModalOpen}
        onCancel={() => setDetailModalOpen(false)}
        footer={[
          <Button key="close" onClick={() => setDetailModalOpen(false)}>
            Close
          </Button>,
        ]}
        width={isMobile ? '100%' : 700}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
        aria-labelledby="audit-detail-title"
      >
        {selectedLog && (
          <Descriptions 
            bordered 
            column={1} 
            size="small"
            aria-label="Audit log entry details"
          >
            <Descriptions.Item label="Timestamp">
              {dayjs(selectedLog.timestamp).format('YYYY-MM-DD HH:mm:ss')}
            </Descriptions.Item>
            <Descriptions.Item label="Action">
              {(() => {
                const config = actionConfig[selectedLog.action] || { label: selectedLog.action, color: 'default' };
                return <Tag color={config.color}>{config.label}</Tag>;
              })()}
            </Descriptions.Item>
            <Descriptions.Item label="Outcome">
              {(() => {
                const config = outcomeConfig[selectedLog.outcome] || { icon: null, color: 'default' };
                return <Tag icon={config.icon} color={config.color}>{selectedLog.outcome}</Tag>;
              })()}
            </Descriptions.Item>
            <Descriptions.Item label="User Email">
              {selectedLog.userEmail || <Text type="secondary">Anonymous</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="User ID">
              {selectedLog.userId || <Text type="secondary">N/A</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="Resource Type">
              {selectedLog.resourceType || <Text type="secondary">N/A</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="Resource ID">
              {selectedLog.resourceId || <Text type="secondary">N/A</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="HTTP Method">
              {selectedLog.httpMethod || <Text type="secondary">N/A</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="Request Path">
              <Text code>{selectedLog.requestPath || 'N/A'}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Status Code">
              {selectedLog.statusCode ? (
                <Tag color={selectedLog.statusCode >= 200 && selectedLog.statusCode < 300 ? 'green' : 'red'}>
                  {selectedLog.statusCode}
                </Tag>
              ) : <Text type="secondary">N/A</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="IP Address">
              {selectedLog.ipAddress || <Text type="secondary">N/A</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="User Agent">
              <Text style={{ fontSize: 12, wordBreak: 'break-all' }}>
                {selectedLog.userAgent || <Text type="secondary">N/A</Text>}
              </Text>
            </Descriptions.Item>
            <Descriptions.Item label="Details">
              {selectedLog.details || <Text type="secondary">N/A</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="Correlation ID">
              <Text code style={{ fontSize: 11 }}>
                {selectedLog.correlationId || 'N/A'}
              </Text>
            </Descriptions.Item>
            <Descriptions.Item label="Entry ID">
              <Text code style={{ fontSize: 11 }}>
                {selectedLog.id}
              </Text>
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>
    </div>
  );
}

export default function AuditLogsPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Sysadmin']}>
      <AuthenticatedLayout>
        <AuditLogsContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
