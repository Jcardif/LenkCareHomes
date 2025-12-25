'use client';

import React, { useState, useCallback, useEffect } from 'react';
import {
  Card,
  Button,
  Table,
  Modal,
  Form,
  InputNumber,
  Input,
  DatePicker,
  TimePicker,
  message,
  Space,
  Typography,
  Descriptions,
  Empty,
  Flex,
  Tag,
} from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import type { ColumnsType } from 'antd/es/table';

dayjs.extend(utc);
import { romLogsApi, ApiError } from '@/lib/api';
import type { ROMLog, CreateROMLogRequest } from '@/types';

const { Text, Paragraph } = Typography;
const { TextArea } = Input;

interface ROMLogTabProps {
  clientId: string;
  clientName: string;
}

export default function ROMLogTab({ clientId, clientName }: ROMLogTabProps) {
  const [logs, setLogs] = useState<ROMLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [viewModalOpen, setViewModalOpen] = useState(false);
  const [selectedLog, setSelectedLog] = useState<ROMLog | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [form] = Form.useForm();

  const fetchLogs = useCallback(async () => {
    try {
      setLoading(true);
      const data = await romLogsApi.getAll(clientId);
      setLogs(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load ROM logs';
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
    form.setFieldValue('timestamp', dayjs());
    form.setFieldValue('time', dayjs());
    setCreateModalOpen(true);
  };

  const handleSubmit = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);
      const timestamp = dayjs(values.timestamp as dayjs.Dayjs)
        .hour((values.time as dayjs.Dayjs).hour())
        .minute((values.time as dayjs.Dayjs).minute())
        .toISOString();

      const request: CreateROMLogRequest = {
        timestamp,
        activityDescription: values.activityDescription as string,
        duration: values.duration as number | undefined,
        repetitions: values.repetitions as number | undefined,
        notes: values.notes as string | undefined,
      };

      const result = await romLogsApi.create(clientId, request);
      if (result.success) {
        void message.success('ROM exercise logged successfully');
        setCreateModalOpen(false);
        void fetchLogs();
      } else {
        void message.error(result.error || 'Failed to create ROM log');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to create ROM log';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleView = (log: ROMLog) => {
    setSelectedLog(log);
    setViewModalOpen(true);
  };

  const columns: ColumnsType<ROMLog> = [
    {
      title: 'Date & Time',
      dataIndex: 'timestamp',
      key: 'timestamp',
      render: (value: string) => dayjs.utc(value).local().format('MMM D, YYYY h:mm A'),
      sorter: (a, b) => dayjs.utc(a.timestamp).unix() - dayjs.utc(b.timestamp).unix(),
      defaultSortOrder: 'descend',
    },
    {
      title: 'Activity',
      dataIndex: 'activityDescription',
      key: 'activityDescription',
      ellipsis: true,
    },
    {
      title: 'Duration',
      dataIndex: 'duration',
      key: 'duration',
      render: (value?: number) => value ? <Tag>{value} min</Tag> : <Text type="secondary">—</Text>,
      responsive: ['md'],
    },
    {
      title: 'Repetitions',
      dataIndex: 'repetitions',
      key: 'repetitions',
      render: (value?: number) => value ? <Tag>{value} reps</Tag> : <Text type="secondary">—</Text>,
      responsive: ['md'],
    },
    {
      title: 'Caregiver',
      dataIndex: 'caregiverName',
      key: 'caregiverName',
      responsive: ['lg'],
    },
  ];

  return (
    <>
      <Card
        title="Range of Motion Exercises"
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Log Exercise
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
            emptyText: <Empty description="No ROM exercises logged" />,
          }}
        />
      </Card>

      {/* Create Modal */}
      <Modal
        title={`Log ROM Exercise - ${clientName}`}
        open={createModalOpen}
        onCancel={() => setCreateModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={600}
      >
        <Paragraph type="secondary" style={{ marginBottom: 24 }}>
          Document range of motion exercises performed with the client.
        </Paragraph>
        <Form
          form={form}
          layout="vertical"
          onFinish={(values) => void handleSubmit(values)}
          disabled={submitting}
        >
          <Flex gap={16}>
            <Form.Item
              name="timestamp"
              label="Date"
              rules={[{ required: true, message: 'Required' }]}
              style={{ flex: 1 }}
            >
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item
              name="time"
              label="Time"
              rules={[{ required: true, message: 'Required' }]}
              style={{ flex: 1 }}
            >
              <TimePicker format="h:mm A" style={{ width: '100%' }} use12Hours />
            </Form.Item>
          </Flex>

          <Form.Item
            name="activityDescription"
            label="Activity Description"
            rules={[{ required: true, message: 'Please describe the exercise' }]}
          >
            <TextArea
              rows={3}
              placeholder="e.g., Passive range of motion exercises for upper extremities, including shoulder flexion, elbow extension..."
            />
          </Form.Item>

          <Flex gap={16}>
            <Form.Item
              name="duration"
              label="Duration (minutes)"
              style={{ flex: 1 }}
              rules={[
                { type: 'number', min: 1, max: 240, message: 'Must be between 1-240' },
              ]}
            >
              <InputNumber style={{ width: '100%' }} placeholder="e.g., 15" />
            </Form.Item>
            <Form.Item
              name="repetitions"
              label="Repetitions"
              style={{ flex: 1 }}
              rules={[
                { type: 'number', min: 1, max: 100, message: 'Must be between 1-100' },
              ]}
            >
              <InputNumber style={{ width: '100%' }} placeholder="e.g., 10" />
            </Form.Item>
          </Flex>

          <Form.Item name="notes" label="Notes">
            <TextArea rows={2} placeholder="Client response, tolerance, any issues..." />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex justify="end" gap={8}>
              <Button onClick={() => setCreateModalOpen(false)}>Cancel</Button>
              <Button type="primary" htmlType="submit" loading={submitting}>
                Save Exercise
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>

      {/* View Modal */}
      <Modal
        title="ROM Exercise Details"
        open={viewModalOpen}
        onCancel={() => setViewModalOpen(false)}
        footer={<Button onClick={() => setViewModalOpen(false)}>Close</Button>}
        width={600}
      >
        {selectedLog && (
          <Space direction="vertical" size={16} style={{ width: '100%' }}>
            <Descriptions column={2} bordered size="small">
              <Descriptions.Item label="Date & Time" span={2}>
                {dayjs.utc(selectedLog.timestamp).local().format('MMMM D, YYYY h:mm A')}
              </Descriptions.Item>
              <Descriptions.Item label="Caregiver" span={2}>
                {selectedLog.caregiverName}
              </Descriptions.Item>
              <Descriptions.Item label="Duration">
                {selectedLog.duration ? `${selectedLog.duration} minutes` : '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Repetitions">
                {selectedLog.repetitions || '—'}
              </Descriptions.Item>
            </Descriptions>

            <Card title="Activity Description" size="small">
              <Paragraph>{selectedLog.activityDescription}</Paragraph>
            </Card>

            {selectedLog.notes && (
              <Card title="Notes" size="small">
                <Paragraph>{selectedLog.notes}</Paragraph>
              </Card>
            )}
          </Space>
        )}
      </Modal>
    </>
  );
}
