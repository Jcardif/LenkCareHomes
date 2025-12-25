'use client';

import React, { useState, useCallback, useEffect } from 'react';
import {
  Card,
  Button,
  Table,
  Modal,
  Form,
  Select,
  Input,
  DatePicker,
  TimePicker,
  message,
  Space,
  Typography,
  Tag,
  Descriptions,
  Empty,
  Flex,
  Checkbox,
  Divider,
} from 'antd';
import { PlusOutlined, ClockCircleOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import type { ColumnsType } from 'antd/es/table';

dayjs.extend(utc);
import { adlLogsApi, ApiError } from '@/lib/api';
import type { ADLLog, ADLLevel, CreateADLLogRequest } from '@/types';

const { Text, Paragraph } = Typography;
const { TextArea } = Input;

interface ADLLogTabProps {
  clientId: string;
  clientName: string;
}

type ADLTaskKey = 'bathing' | 'dressing' | 'toileting' | 'transferring' | 'continence' | 'feeding';

interface SelectedTask {
  task: ADLTaskKey;
  level: ADLLevel;
}

const ADL_TASKS: { key: ADLTaskKey; label: string; icon: string }[] = [
  { key: 'bathing', label: 'Bathing', icon: '🛁' },
  { key: 'dressing', label: 'Dressing', icon: '👕' },
  { key: 'toileting', label: 'Toileting', icon: '🚽' },
  { key: 'transferring', label: 'Transferring', icon: '🚶' },
  { key: 'continence', label: 'Continence Care', icon: '🩹' },
  { key: 'feeding', label: 'Feeding', icon: '🍽️' },
];

const ASSISTANCE_LEVELS: { label: string; value: ADLLevel; color: string }[] = [
  { label: 'No Assistance', value: 'Independent', color: 'green' },
  { label: 'Some Assistance', value: 'PartialAssist', color: 'orange' },
  { label: 'Full Assistance', value: 'Dependent', color: 'red' },
];

function getLevelTag(level?: ADLLevel) {
  if (!level || level === 'NotApplicable') return null;
  const levelInfo = ASSISTANCE_LEVELS.find((l) => l.value === level);
  if (!levelInfo) return <Tag>{level}</Tag>;
  return <Tag color={levelInfo.color}>{levelInfo.label}</Tag>;
}

function getTasksWithLevels(log: ADLLog): { task: string; icon: string; level: ADLLevel }[] {
  const tasks: { task: string; icon: string; level: ADLLevel }[] = [];
  if (log.bathing && log.bathing !== 'NotApplicable') tasks.push({ task: 'Bathing', icon: '🛁', level: log.bathing });
  if (log.dressing && log.dressing !== 'NotApplicable') tasks.push({ task: 'Dressing', icon: '👕', level: log.dressing });
  if (log.toileting && log.toileting !== 'NotApplicable') tasks.push({ task: 'Toileting', icon: '🚽', level: log.toileting });
  if (log.transferring && log.transferring !== 'NotApplicable') tasks.push({ task: 'Transferring', icon: '🚶', level: log.transferring });
  if (log.continence && log.continence !== 'NotApplicable') tasks.push({ task: 'Continence', icon: '🩹', level: log.continence });
  if (log.feeding && log.feeding !== 'NotApplicable') tasks.push({ task: 'Feeding', icon: '🍽️', level: log.feeding });
  return tasks;
}

export default function ADLLogTab({ clientId, clientName }: ADLLogTabProps) {
  const [logs, setLogs] = useState<ADLLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [viewModalOpen, setViewModalOpen] = useState(false);
  const [selectedLog, setSelectedLog] = useState<ADLLog | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [useCurrentTime, setUseCurrentTime] = useState(true);
  const [selectedTasks, setSelectedTasks] = useState<SelectedTask[]>([]);
  const [form] = Form.useForm();

  const fetchLogs = useCallback(async () => {
    try {
      setLoading(true);
      const data = await adlLogsApi.getAll(clientId);
      setLogs(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load ADL logs';
      void message.error(msg);
    } finally {
      setLoading(false);
    }
  }, [clientId]);

  useEffect(() => {
    void fetchLogs();
  }, [fetchLogs]);

  const handleCreate = () => {
    form.resetFields();
    setUseCurrentTime(true);
    setSelectedTasks([]);
    form.setFieldValue('timestamp', dayjs());
    form.setFieldValue('time', dayjs());
    setCreateModalOpen(true);
  };

  const handleAddTask = (taskKey: ADLTaskKey) => {
    if (selectedTasks.some((t) => t.task === taskKey)) return;
    setSelectedTasks([...selectedTasks, { task: taskKey, level: 'PartialAssist' }]);
  };

  const handleRemoveTask = (taskKey: ADLTaskKey) => {
    setSelectedTasks(selectedTasks.filter((t) => t.task !== taskKey));
  };

  const handleUpdateTaskLevel = (taskKey: ADLTaskKey, level: ADLLevel) => {
    setSelectedTasks(selectedTasks.map((t) => (t.task === taskKey ? { ...t, level } : t)));
  };

  const handleSubmit = async (values: Record<string, unknown>) => {
    try {
      if (selectedTasks.length === 0) {
        void message.error('Please select at least one ADL task');
        return;
      }

      setSubmitting(true);
      
      const timestampDate = useCurrentTime ? dayjs() : (values.timestamp as dayjs.Dayjs);
      const timestampTime = useCurrentTime ? dayjs() : (values.time as dayjs.Dayjs);
      
      const timestamp = timestampDate
        .hour(timestampTime.hour())
        .minute(timestampTime.minute())
        .toISOString();

      // Build task levels from selected tasks
      const taskLevels: Partial<Record<ADLTaskKey, ADLLevel>> = {};
      for (const { task, level } of selectedTasks) {
        taskLevels[task] = level;
      }

      // Build request with all selected tasks
      const request: CreateADLLogRequest = {
        timestamp,
        notes: values.notes as string | undefined,
        bathing: taskLevels.bathing,
        dressing: taskLevels.dressing,
        toileting: taskLevels.toileting,
        transferring: taskLevels.transferring,
        continence: taskLevels.continence,
        feeding: taskLevels.feeding,
      };

      const result = await adlLogsApi.create(clientId, request);
      if (result.success) {
        void message.success('ADL logged successfully');
        setCreateModalOpen(false);
        void fetchLogs();
      } else {
        void message.error(result.error || 'Failed to log ADL');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to log ADL';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleView = (log: ADLLog) => {
    setSelectedLog(log);
    setViewModalOpen(true);
  };

  const availableTasks = ADL_TASKS.filter((t) => !selectedTasks.some((st) => st.task === t.key));

  const columns: ColumnsType<ADLLog> = [
    {
      title: 'Date & Time',
      dataIndex: 'timestamp',
      key: 'timestamp',
      render: (value: string) => dayjs.utc(value).local().format('MMM D, YYYY h:mm A'),
      sorter: (a, b) => dayjs.utc(a.timestamp).unix() - dayjs.utc(b.timestamp).unix(),
      defaultSortOrder: 'descend',
    },
    {
      title: 'Tasks',
      key: 'tasks',
      render: (_, record) => {
        const tasks = getTasksWithLevels(record);
        return (
          <Space wrap size={[4, 4]}>
            {tasks.map((t) => (
              <Tag key={t.task} icon={<span style={{ marginRight: 4 }}>{t.icon}</span>}>
                {t.task}
              </Tag>
            ))}
          </Space>
        );
      },
    },
    {
      title: 'Caregiver',
      dataIndex: 'caregiverName',
      key: 'caregiverName',
      responsive: ['md'],
    },
  ];

  return (
    <>
      <Card
        title="ADL Care Log"
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Log ADL
          </Button>
        }
      >
        <Table
          columns={columns}
          dataSource={logs}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 10 }}
          onRow={(record) => ({
            onClick: () => handleView(record),
            style: { cursor: 'pointer' },
          })}
          locale={{
            emptyText: <Empty description="No ADL care logged yet" />,
          }}
        />
      </Card>

      {/* Create Modal */}
      <Modal
        title={`Log ADL - ${clientName}`}
        open={createModalOpen}
        onCancel={() => setCreateModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={550}
      >
        <Paragraph type="secondary" style={{ marginBottom: 16 }}>
          Select the ADL tasks you helped with and the level of assistance for each.
        </Paragraph>
        
        <Form
          form={form}
          layout="vertical"
          onFinish={(values) => void handleSubmit(values)}
          disabled={submitting}
        >
          {/* Task Selection */}
          <Form.Item label="What did you help with?">
            <Select
              placeholder="Select ADL task to add"
              value={undefined}
              onChange={(value: ADLTaskKey) => handleAddTask(value)}
              options={availableTasks.map((t) => ({
                label: (
                  <Space>
                    <span>{t.icon}</span>
                    <Text>{t.label}</Text>
                  </Space>
                ),
                value: t.key,
              }))}
              disabled={availableTasks.length === 0}
            />
          </Form.Item>

          {/* Selected Tasks with Levels */}
          {selectedTasks.length > 0 && (
            <Card size="small" style={{ marginBottom: 16 }}>
              <Space direction="vertical" style={{ width: '100%' }} size={8}>
                {selectedTasks.map(({ task, level }) => {
                  const taskInfo = ADL_TASKS.find((t) => t.key === task)!;
                  return (
                    <Flex key={task} align="center" justify="space-between" gap={8}>
                      <Space>
                        <span>{taskInfo.icon}</span>
                        <Text strong>{taskInfo.label}</Text>
                      </Space>
                      <Space>
                        <Select
                          size="small"
                          value={level}
                          onChange={(v) => handleUpdateTaskLevel(task, v)}
                          style={{ width: 150 }}
                          options={ASSISTANCE_LEVELS.map((l) => ({
                            label: <Tag color={l.color}>{l.label}</Tag>,
                            value: l.value,
                          }))}
                        />
                        <Button
                          type="text"
                          size="small"
                          danger
                          icon={<DeleteOutlined />}
                          onClick={() => handleRemoveTask(task)}
                        />
                      </Space>
                    </Flex>
                  );
                })}
              </Space>
            </Card>
          )}

          {selectedTasks.length === 0 && (
            <Card size="small" style={{ marginBottom: 16, background: '#fafafa', textAlign: 'center' }}>
              <Text type="secondary">No tasks selected. Add tasks above.</Text>
            </Card>
          )}

          <Divider style={{ margin: '16px 0' }} />

          {/* Time Selection */}
          <Card size="small" style={{ marginBottom: 16, background: '#fafafa' }}>
            <Flex align="center" gap={8} style={{ marginBottom: useCurrentTime ? 0 : 12 }}>
              <Checkbox
                checked={useCurrentTime}
                onChange={(e) => setUseCurrentTime(e.target.checked)}
              >
                <Space>
                  <ClockCircleOutlined />
                  <Text>Use current time</Text>
                </Space>
              </Checkbox>
              {useCurrentTime && (
                <Text type="secondary">({dayjs().format('h:mm A')})</Text>
              )}
            </Flex>
            
            {!useCurrentTime && (
              <Flex gap={16}>
                <Form.Item
                  name="timestamp"
                  label="Date"
                  rules={[{ required: !useCurrentTime, message: 'Required' }]}
                  style={{ flex: 1, marginBottom: 0 }}
                >
                  <DatePicker style={{ width: '100%' }} />
                </Form.Item>
                <Form.Item
                  name="time"
                  label="Time"
                  rules={[{ required: !useCurrentTime, message: 'Required' }]}
                  style={{ flex: 1, marginBottom: 0 }}
                >
                  <TimePicker format="h:mm A" style={{ width: '100%' }} use12Hours />
                </Form.Item>
              </Flex>
            )}
          </Card>

          {/* Notes */}
          <Form.Item name="notes" label="Notes (optional)">
            <TextArea rows={2} placeholder="Any additional observations..." />
          </Form.Item>

          {/* Submit */}
          <Form.Item style={{ marginBottom: 0, marginTop: 16 }}>
            <Flex justify="end" gap={8}>
              <Button onClick={() => setCreateModalOpen(false)}>Cancel</Button>
              <Button 
                type="primary" 
                htmlType="submit" 
                loading={submitting}
                disabled={selectedTasks.length === 0}
              >
                Log ADL
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>

      {/* View Modal */}
      <Modal
        title="ADL Log Details"
        open={viewModalOpen}
        onCancel={() => setViewModalOpen(false)}
        footer={<Button onClick={() => setViewModalOpen(false)}>Close</Button>}
        width={500}
      >
        {selectedLog && (
          <Space direction="vertical" size={16} style={{ width: '100%' }}>
            <Descriptions column={1} bordered size="small">
              <Descriptions.Item label="Date & Time">
                {dayjs.utc(selectedLog.timestamp).local().format('MMMM D, YYYY h:mm A')}
              </Descriptions.Item>
              <Descriptions.Item label="Caregiver">
                {selectedLog.caregiverName}
              </Descriptions.Item>
            </Descriptions>

            <Card title="Tasks & Assistance" size="small">
              <Space direction="vertical" style={{ width: '100%' }} size={8}>
                {getTasksWithLevels(selectedLog).map((t) => (
                  <Flex key={t.task} justify="space-between" align="center">
                    <Space>
                      <span>{t.icon}</span>
                      <Text>{t.task}</Text>
                    </Space>
                    {getLevelTag(t.level)}
                  </Flex>
                ))}
              </Space>
            </Card>

            {selectedLog.notes && (
              <Card title="Notes" size="small">
                <Paragraph style={{ marginBottom: 0 }}>{selectedLog.notes}</Paragraph>
              </Card>
            )}
          </Space>
        )}
      </Modal>
    </>
  );
}
