'use client';

import React, { useState, useCallback, useEffect } from 'react';
import {
  Card,
  Empty,
  Spin,
  Typography,
  Space,
  Tag,
  Timeline,
  Button,
  DatePicker,
  Select,
  Flex,
  message,
  Pagination,
  Descriptions,
  Modal,
} from 'antd';
import {
  HistoryOutlined,
  HeartOutlined,
  MedicineBoxOutlined,
  SmileOutlined,
  TeamOutlined,
  FilterOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import { timelineApi, ApiError } from '@/lib/api';

dayjs.extend(utc);
import type { TimelineEntry, TimelineEntryType, TimelineResponse } from '@/types';

const { Text, Paragraph, Title } = Typography;
const { RangePicker } = DatePicker;

interface TimelineTabProps {
  clientId: string;
  clientName: string;
}

const ENTRY_TYPE_CONFIG: Record<TimelineEntryType, { icon: React.ReactNode; color: string; label: string }> = {
  ADL: { icon: <MedicineBoxOutlined />, color: 'blue', label: 'ADL Assessment' },
  Vitals: { icon: <HeartOutlined />, color: 'red', label: 'Vital Signs' },
  ROM: { icon: <MedicineBoxOutlined />, color: 'green', label: 'ROM Exercise' },
  Behavior: { icon: <SmileOutlined />, color: 'orange', label: 'Behavior Note' },
  Activity: { icon: <TeamOutlined />, color: 'purple', label: 'Activity' },
};

function getTimelineIcon(entryType: TimelineEntryType) {
  return ENTRY_TYPE_CONFIG[entryType]?.icon || <HistoryOutlined />;
}

function getTimelineColor(entryType: TimelineEntryType) {
  return ENTRY_TYPE_CONFIG[entryType]?.color || 'gray';
}

export default function TimelineTab({ clientId }: TimelineTabProps) {
  const [response, setResponse] = useState<TimelineResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedEntry, setSelectedEntry] = useState<TimelineEntry | null>(null);
  const [viewModalOpen, setViewModalOpen] = useState(false);

  // Filter state
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs | null, dayjs.Dayjs | null] | null>(null);
  const [selectedTypes, setSelectedTypes] = useState<TimelineEntryType[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const fetchTimeline = useCallback(async () => {
    try {
      setLoading(true);
      const data = await timelineApi.getClientTimeline(clientId, {
        startDate: dateRange?.[0]?.format('YYYY-MM-DD'),
        endDate: dateRange?.[1]?.format('YYYY-MM-DD'),
        entryTypes: selectedTypes.length > 0 ? selectedTypes : undefined,
        pageNumber,
        pageSize,
      });
      setResponse(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load timeline';
      void message.error(msg);
    } finally {
      setLoading(false);
    }
  }, [clientId, dateRange, selectedTypes, pageNumber, pageSize]);

  useEffect(() => {
    void fetchTimeline();
  }, [fetchTimeline]);

  const handleDateRangeChange = (dates: [dayjs.Dayjs | null, dayjs.Dayjs | null] | null) => {
    setDateRange(dates);
    setPageNumber(1);
  };

  const handleTypesChange = (types: TimelineEntryType[]) => {
    setSelectedTypes(types);
    setPageNumber(1);
  };

  const handlePageChange = (page: number, size?: number) => {
    setPageNumber(page);
    if (size) setPageSize(size);
  };

  const handleClearFilters = () => {
    setDateRange(null);
    setSelectedTypes([]);
    setPageNumber(1);
  };

  const handleViewDetails = (entry: TimelineEntry) => {
    setSelectedEntry(entry);
    setViewModalOpen(true);
  };

  const groupEntriesByDate = (entries: TimelineEntry[]) => {
    const groups: Record<string, TimelineEntry[]> = {};
    entries.forEach((entry) => {
      const date = dayjs.utc(entry.timestamp).local().format('YYYY-MM-DD');
      if (!groups[date]) {
        groups[date] = [];
      }
      groups[date].push(entry);
    });
    return groups;
  };

  const renderEntryDetails = (entry: TimelineEntry) => {
    const details = entry.details as Record<string, unknown>;
    
    switch (entry.entryType) {
      case 'ADL':
        return (
          <Descriptions column={2} size="small" bordered>
            <Descriptions.Item label="Katz Score">{String(details.katzScore ?? '')}/6</Descriptions.Item>
            <Descriptions.Item label="Bathing">{String(details.bathing ?? '—')}</Descriptions.Item>
            <Descriptions.Item label="Dressing">{String(details.dressing ?? '—')}</Descriptions.Item>
            <Descriptions.Item label="Toileting">{String(details.toileting ?? '—')}</Descriptions.Item>
            <Descriptions.Item label="Transferring">{String(details.transferring ?? '—')}</Descriptions.Item>
            <Descriptions.Item label="Continence">{String(details.continence ?? '—')}</Descriptions.Item>
            <Descriptions.Item label="Feeding">{String(details.feeding ?? '—')}</Descriptions.Item>
            {details.notes != null && <Descriptions.Item label="Notes" span={2}>{String(details.notes)}</Descriptions.Item>}
          </Descriptions>
        );
      case 'Vitals':
        return (
          <Descriptions column={2} size="small" bordered>
            {details.bloodPressure != null && <Descriptions.Item label="Blood Pressure">{String(details.bloodPressure)} mmHg</Descriptions.Item>}
            {details.pulse != null && <Descriptions.Item label="Pulse">{String(details.pulse)} bpm</Descriptions.Item>}
            {details.oxygenSaturation != null && <Descriptions.Item label="O₂ Saturation">{String(details.oxygenSaturation)}%</Descriptions.Item>}
            {details.temperature != null && <Descriptions.Item label="Temperature">{String(details.temperature)}°{details.temperatureUnit === 'Celsius' ? 'C' : 'F'}</Descriptions.Item>}
            {details.notes != null && <Descriptions.Item label="Notes" span={2}>{String(details.notes)}</Descriptions.Item>}
          </Descriptions>
        );
      case 'ROM':
        return (
          <Descriptions column={2} size="small" bordered>
            <Descriptions.Item label="Activity" span={2}>{String(details.activityDescription ?? '—')}</Descriptions.Item>
            {details.duration != null && <Descriptions.Item label="Duration">{String(details.duration)} min</Descriptions.Item>}
            {details.repetitions != null && <Descriptions.Item label="Repetitions">{String(details.repetitions)}</Descriptions.Item>}
            {details.notes != null && <Descriptions.Item label="Notes" span={2}>{String(details.notes)}</Descriptions.Item>}
          </Descriptions>
        );
      case 'Behavior':
        return (
          <Descriptions column={2} size="small" bordered>
            <Descriptions.Item label="Category">{String(details.category ?? '—')}</Descriptions.Item>
            <Descriptions.Item label="Severity">{String(details.severity ?? '—')}</Descriptions.Item>
            <Descriptions.Item label="Note" span={2}>{String(details.noteText ?? '—')}</Descriptions.Item>
          </Descriptions>
        );
      case 'Activity':
        return (
          <Descriptions column={2} size="small" bordered>
            <Descriptions.Item label="Activity">{String(details.activityName ?? '—')}</Descriptions.Item>
            <Descriptions.Item label="Category">{String(details.category ?? '—')}</Descriptions.Item>
            <Descriptions.Item label="Type">{details.isGroupActivity ? 'Group' : 'Individual'}</Descriptions.Item>
            {details.duration != null && <Descriptions.Item label="Duration">{String(details.duration)} min</Descriptions.Item>}
            {details.participantCount != null && <Descriptions.Item label="Participants">{String(details.participantCount)}</Descriptions.Item>}
            {details.description != null && <Descriptions.Item label="Description" span={2}>{String(details.description)}</Descriptions.Item>}
          </Descriptions>
        );
      default:
        return <Paragraph>No additional details available.</Paragraph>;
    }
  };

  const entries = response?.entries || [];
  const groupedEntries = groupEntriesByDate(entries);
  const sortedDates = Object.keys(groupedEntries).sort((a, b) => dayjs(b).unix() - dayjs(a).unix());

  return (
    <>
      <Card
        title={
          <Space>
            <HistoryOutlined />
            Care Timeline
          </Space>
        }
        extra={
          <Button icon={<ReloadOutlined />} onClick={() => void fetchTimeline()} loading={loading}>
            Refresh
          </Button>
        }
      >
        {/* Filters */}
        <Card size="small" style={{ marginBottom: 16 }}>
          <Flex wrap="wrap" gap={16} align="center">
            <Space>
              <FilterOutlined />
              <Text strong>Filters:</Text>
            </Space>
            <RangePicker
              value={dateRange}
              onChange={handleDateRangeChange}
              placeholder={['Start Date', 'End Date']}
              style={{ width: 280 }}
            />
            <Select
              mode="multiple"
              placeholder="Filter by type"
              value={selectedTypes}
              onChange={handleTypesChange}
              style={{ minWidth: 200 }}
              options={Object.entries(ENTRY_TYPE_CONFIG).map(([key, config]) => ({
                label: (
                  <Space>
                    {config.icon}
                    {config.label}
                  </Space>
                ),
                value: key as TimelineEntryType,
              }))}
            />
            {(dateRange || selectedTypes.length > 0) && (
              <Button onClick={handleClearFilters}>Clear Filters</Button>
            )}
          </Flex>
        </Card>

        {/* Loading State */}
        {loading && (
          <Flex justify="center" align="center" style={{ minHeight: 200 }}>
            <Spin tip="Loading timeline..." />
          </Flex>
        )}

        {/* Empty State */}
        {!loading && entries.length === 0 && (
          <Empty
            description={
              selectedTypes.length > 0 || dateRange
                ? 'No entries match your filters'
                : 'No care logs recorded yet'
            }
          />
        )}

        {/* Timeline */}
        {!loading && entries.length > 0 && (
          <>
            {sortedDates.map((date) => (
              <div key={date} style={{ marginBottom: 24 }}>
                <Title level={5} style={{ marginBottom: 16, color: '#5a7a6b' }}>
                  {dayjs(date, 'YYYY-MM-DD').format('dddd, MMMM D, YYYY')}
                </Title>
                <Timeline
                  items={groupedEntries[date].map((entry) => ({
                    color: getTimelineColor(entry.entryType),
                    dot: getTimelineIcon(entry.entryType),
                    children: (
                      <Card
                        size="small"
                        style={{ marginBottom: 8, cursor: 'pointer' }}
                        hoverable
                        onClick={() => handleViewDetails(entry)}
                      >
                        <Space direction="vertical" size={4} style={{ width: '100%' }}>
                            <Space>
                              <Tag color={getTimelineColor(entry.entryType)}>
                                {ENTRY_TYPE_CONFIG[entry.entryType]?.label || entry.entryType}
                              </Tag>
                              <Text type="secondary">
                                {dayjs.utc(entry.timestamp).local().format('h:mm A')}
                              </Text>
                            </Space>
                            <Text>{entry.summary}</Text>
                            <Text type="secondary" style={{ fontSize: 12 }}>
                              by {entry.caregiverName}
                            </Text>
                          </Space>
                      </Card>
                    ),
                  }))}
                />
              </div>
            ))}

            {/* Pagination */}
            {response && response.totalPages > 1 && (
              <Flex justify="center" style={{ marginTop: 24 }}>
                <Pagination
                  current={pageNumber}
                  pageSize={pageSize}
                  total={response.totalCount}
                  onChange={handlePageChange}
                  showSizeChanger
                  showTotal={(total, range) => `${range[0]}-${range[1]} of ${total} entries`}
                />
              </Flex>
            )}
          </>
        )}
      </Card>

      {/* Details Modal */}
      <Modal
        title={
          selectedEntry && (
            <Space>
              {getTimelineIcon(selectedEntry.entryType)}
              {ENTRY_TYPE_CONFIG[selectedEntry.entryType]?.label}
            </Space>
          )
        }
        open={viewModalOpen}
        onCancel={() => setViewModalOpen(false)}
        footer={<Button onClick={() => setViewModalOpen(false)}>Close</Button>}
        width={600}
      >
        {selectedEntry && (
          <Space direction="vertical" size={16} style={{ width: '100%' }}>
            <Descriptions column={2} size="small">
              <Descriptions.Item label="Date & Time" span={2}>
                {dayjs.utc(selectedEntry.timestamp).local().format('MMMM D, YYYY h:mm A')}
              </Descriptions.Item>
              <Descriptions.Item label="Caregiver" span={2}>
                {selectedEntry.caregiverName}
              </Descriptions.Item>
            </Descriptions>
            
            <Card size="small" title="Details">
              {renderEntryDetails(selectedEntry)}
            </Card>
          </Space>
        )}
      </Modal>
    </>
  );
}
