'use client';

import React, { useState, useCallback, useEffect } from 'react';
import {
  Card,
  Button,
  Table,
  Modal,
  Form,
  Input,
  Select,
  DatePicker,
  TimePicker,
  message,
  Space,
  Typography,
  Tag,
  Descriptions,
  Empty,
  Flex,
  Alert,
} from 'antd';
import { PlusOutlined, ExclamationCircleOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import type { ColumnsType } from 'antd/es/table';

dayjs.extend(utc);
import { behaviorNotesApi, ApiError } from '@/lib/api';
import type { BehaviorNote, BehaviorCategory, NoteSeverity, CreateBehaviorNoteRequest } from '@/types';

const { Text, Paragraph } = Typography;
const { TextArea } = Input;

interface BehaviorNotesTabProps {
  clientId: string;
  clientName: string;
}

const CATEGORIES: { label: string; value: BehaviorCategory; color: string }[] = [
  { label: 'Behavior', value: 'Behavior', color: 'blue' },
  { label: 'Mood', value: 'Mood', color: 'purple' },
  { label: 'General', value: 'General', color: 'default' },
];

const SEVERITIES: { label: string; value: NoteSeverity; color: string }[] = [
  { label: 'Low', value: 'Low', color: 'green' },
  { label: 'Medium', value: 'Medium', color: 'orange' },
  { label: 'High', value: 'High', color: 'red' },
];

function getCategoryTag(category: BehaviorCategory) {
  const cat = CATEGORIES.find((c) => c.value === category);
  return <Tag color={cat?.color}>{cat?.label || category}</Tag>;
}

function getSeverityTag(severity: NoteSeverity) {
  const sev = SEVERITIES.find((s) => s.value === severity);
  return <Tag color={sev?.color}>{sev?.label || severity}</Tag>;
}

export default function BehaviorNotesTab({ clientId, clientName }: BehaviorNotesTabProps) {
  const [notes, setNotes] = useState<BehaviorNote[]>([]);
  const [loading, setLoading] = useState(true);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [viewModalOpen, setViewModalOpen] = useState(false);
  const [selectedNote, setSelectedNote] = useState<BehaviorNote | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [form] = Form.useForm();

  const fetchNotes = useCallback(async () => {
    try {
      setLoading(true);
      const data = await behaviorNotesApi.getAll(clientId);
      setNotes(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load behavior notes';
      void message.error(msg);
    } finally {
      setLoading(false);
    }
  }, [clientId]);

  useEffect(() => {
    void fetchNotes();
  }, [fetchNotes]);

  const handleCreate = () => {
    form.resetFields();
    form.setFieldValue('timestamp', dayjs());
    form.setFieldValue('time', dayjs());
    form.setFieldValue('severity', 'Low');
    setCreateModalOpen(true);
  };

  const handleSubmit = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);
      const timestamp = dayjs(values.timestamp as dayjs.Dayjs)
        .hour((values.time as dayjs.Dayjs).hour())
        .minute((values.time as dayjs.Dayjs).minute())
        .toISOString();

      const request: CreateBehaviorNoteRequest = {
        timestamp,
        category: values.category as BehaviorCategory,
        noteText: values.noteText as string,
        severity: values.severity as NoteSeverity | undefined,
      };

      const result = await behaviorNotesApi.create(clientId, request);
      if (result.success) {
        void message.success('Behavior note created successfully');
        setCreateModalOpen(false);
        void fetchNotes();
      } else {
        void message.error(result.error || 'Failed to create behavior note');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to create behavior note';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleView = (note: BehaviorNote) => {
    setSelectedNote(note);
    setViewModalOpen(true);
  };

  // Check for high severity notes in recent entries
  const recentHighSeverityNotes = notes.filter(
    (n) => n.severity === 'High' && dayjs(n.timestamp).isAfter(dayjs().subtract(7, 'day'))
  );

  const columns: ColumnsType<BehaviorNote> = [
    {
      title: 'Date & Time',
      dataIndex: 'timestamp',
      key: 'timestamp',
      render: (value: string) => dayjs.utc(value).local().format('MMM D, YYYY h:mm A'),
      sorter: (a, b) => dayjs.utc(a.timestamp).unix() - dayjs.utc(b.timestamp).unix(),
      defaultSortOrder: 'descend',
    },
    {
      title: 'Category',
      dataIndex: 'category',
      key: 'category',
      render: getCategoryTag,
      filters: CATEGORIES.map((c) => ({ text: c.label, value: c.value })),
      onFilter: (value, record) => record.category === value,
    },
    {
      title: 'Severity',
      dataIndex: 'severity',
      key: 'severity',
      render: getSeverityTag,
      filters: SEVERITIES.map((s) => ({ text: s.label, value: s.value })),
      onFilter: (value, record) => record.severity === value,
    },
    {
      title: 'Note',
      dataIndex: 'noteText',
      key: 'noteText',
      ellipsis: true,
      render: (text: string) => (
        <Text style={{ maxWidth: 300 }} ellipsis={{ tooltip: text }}>
          {text}
        </Text>
      ),
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
      {recentHighSeverityNotes.length > 0 && (
        <Alert
          type="error"
          message={`${recentHighSeverityNotes.length} High Severity Note(s) This Week`}
          description="There are high severity behavior/mood notes recorded in the past 7 days. Please review and address any concerns."
          icon={<ExclamationCircleOutlined />}
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      <Card
        title="Behavior & Mood Notes"
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Add Note
          </Button>
        }
      >
        <Table
          columns={columns}
          dataSource={notes}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 10 }}
          onRow={(record) => ({
            onClick: () => handleView(record),
            style: { cursor: 'pointer' },
          })}
          locale={{
            emptyText: <Empty description="No behavior notes recorded" />,
          }}
        />
      </Card>

      {/* Create Modal */}
      <Modal
        title={`Add Behavior Note - ${clientName}`}
        open={createModalOpen}
        onCancel={() => setCreateModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={600}
      >
        <Paragraph type="secondary" style={{ marginBottom: 24 }}>
          Document observations about the client&apos;s behavior, mood, or other notable information.
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

          <Flex gap={16}>
            <Form.Item
              name="category"
              label="Category"
              rules={[{ required: true, message: 'Please select a category' }]}
              style={{ flex: 1 }}
            >
              <Select
                placeholder="Select category"
                options={CATEGORIES.map((c) => ({
                  label: <Tag color={c.color}>{c.label}</Tag>,
                  value: c.value,
                }))}
              />
            </Form.Item>
            <Form.Item
              name="severity"
              label="Severity"
              style={{ flex: 1 }}
            >
              <Select
                placeholder="Select severity"
                options={SEVERITIES.map((s) => ({
                  label: <Tag color={s.color}>{s.label}</Tag>,
                  value: s.value,
                }))}
              />
            </Form.Item>
          </Flex>

          <Form.Item
            name="noteText"
            label="Note"
            rules={[
              { required: true, message: 'Please enter the note text' },
              { max: 4000, message: 'Note cannot exceed 4000 characters' },
            ]}
          >
            <TextArea
              rows={6}
              placeholder="Describe the observed behavior, mood, or other relevant information..."
              showCount
              maxLength={4000}
            />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex justify="end" gap={8}>
              <Button onClick={() => setCreateModalOpen(false)}>Cancel</Button>
              <Button type="primary" htmlType="submit" loading={submitting}>
                Save Note
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>

      {/* View Modal */}
      <Modal
        title="Behavior Note Details"
        open={viewModalOpen}
        onCancel={() => setViewModalOpen(false)}
        footer={<Button onClick={() => setViewModalOpen(false)}>Close</Button>}
        width={600}
      >
        {selectedNote && (
          <Space direction="vertical" size={16} style={{ width: '100%' }}>
            <Descriptions column={2} bordered size="small">
              <Descriptions.Item label="Date & Time" span={2}>
                {dayjs.utc(selectedNote.timestamp).local().format('MMMM D, YYYY h:mm A')}
              </Descriptions.Item>
              <Descriptions.Item label="Caregiver" span={2}>
                {selectedNote.caregiverName}
              </Descriptions.Item>
              <Descriptions.Item label="Category">
                {getCategoryTag(selectedNote.category)}
              </Descriptions.Item>
              <Descriptions.Item label="Severity">
                {getSeverityTag(selectedNote.severity)}
              </Descriptions.Item>
            </Descriptions>

            <Card title="Note" size="small">
              <Paragraph style={{ whiteSpace: 'pre-wrap' }}>{selectedNote.noteText}</Paragraph>
            </Card>
          </Space>
        )}
      </Modal>
    </>
  );
}
