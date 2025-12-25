'use client';

import React, { useState, useCallback, useEffect } from 'react';
import {
  Card,
  Button,
  Table,
  Modal,
  Form,
  Input,
  InputNumber,
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
  Checkbox,
} from 'antd';
import { PlusOutlined, EyeOutlined, TeamOutlined, UserOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';

dayjs.extend(utc);
import type { ColumnsType } from 'antd/es/table';
import { activitiesApi, clientsApi, ApiError } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import type { Activity, ActivityCategory, CreateActivityRequest, UpdateActivityRequest, ClientSummary } from '@/types';

const { Text, Paragraph } = Typography;
const { TextArea } = Input;

interface ActivitiesTabProps {
  clientId: string;
  clientName: string;
  homeId: string;
}

const CATEGORIES: { label: string; value: ActivityCategory; color: string }[] = [
  { label: 'Recreational', value: 'Recreational', color: 'blue' },
  { label: 'Social', value: 'Social', color: 'purple' },
  { label: 'Exercise', value: 'Exercise', color: 'green' },
  { label: 'Other', value: 'Other', color: 'default' },
];

function getCategoryTag(category: ActivityCategory) {
  const cat = CATEGORIES.find((c) => c.value === category);
  return <Tag color={cat?.color}>{cat?.label || category}</Tag>;
}

export default function ActivitiesTab({ clientId, clientName, homeId }: ActivitiesTabProps) {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Admin');

  const [activities, setActivities] = useState<Activity[]>([]);
  const [loading, setLoading] = useState(true);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [viewModalOpen, setViewModalOpen] = useState(false);
  const [selectedActivity, setSelectedActivity] = useState<Activity | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [form] = Form.useForm();
  const [editForm] = Form.useForm();

  const [homeClients, setHomeClients] = useState<ClientSummary[]>([]);
  const [selectedClientKeys, setSelectedClientKeys] = useState<string[]>([]);

  const fetchActivities = useCallback(async () => {
    try {
      setLoading(true);
      const data = await activitiesApi.getByClient(clientId);
      setActivities(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load activities';
      void message.error(msg);
    } finally {
      setLoading(false);
    }
  }, [clientId]);

  const fetchHomeClients = useCallback(async () => {
    try {
      const data = await clientsApi.getAll({ homeId, isActive: true });
      setHomeClients(data);
    } catch {
      // Silently fail
    }
  }, [homeId]);

  useEffect(() => {
    void fetchActivities();
    void fetchHomeClients();
  }, [fetchActivities, fetchHomeClients]);

  const handleCreate = () => {
    form.resetFields();
    form.setFieldValue('date', dayjs());
    form.setFieldValue('isGroupActivity', false);
    setSelectedClientKeys([clientId]); // Default to current client
    setCreateModalOpen(true);
  };

  const handleSubmit = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);

      // Calculate endTime from startTime + duration
      let endTime: string | undefined;
      if (values.startTime) {
        const startMoment = values.startTime as dayjs.Dayjs;
        const durationHours = (values.durationHours as number) || 0;
        const durationMinutes = (values.durationMinutes as number) || 0;
        if (durationHours > 0 || durationMinutes > 0) {
          endTime = startMoment.add(durationHours, 'hour').add(durationMinutes, 'minute').format('HH:mm');
        }
      }

      const request: CreateActivityRequest = {
        homeId,
        activityName: values.activityName as string,
        description: values.description as string | undefined,
        date: (values.date as dayjs.Dayjs).format('YYYY-MM-DD'),
        startTime: values.startTime ? (values.startTime as dayjs.Dayjs).format('HH:mm') : undefined,
        endTime,
        category: values.category as ActivityCategory,
        isGroupActivity: values.isGroupActivity as boolean | undefined,
        clientIds: selectedClientKeys,
      };

      const result = await activitiesApi.create(request);
      if (result.success) {
        void message.success('Activity created successfully');
        setCreateModalOpen(false);
        void fetchActivities();
      } else {
        void message.error(result.error || 'Failed to create activity');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to create activity';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleEdit = (activity: Activity) => {
    setSelectedActivity(activity);

    // Calculate duration from startTime and endTime
    let durationHours: number | undefined;
    let durationMinutes: number | undefined;
    if (activity.startTime && activity.endTime) {
      const start = dayjs(activity.startTime, 'HH:mm');
      const end = dayjs(activity.endTime, 'HH:mm');
      const diffMinutes = end.diff(start, 'minute');
      if (diffMinutes > 0) {
        durationHours = Math.floor(diffMinutes / 60);
        durationMinutes = diffMinutes % 60;
      }
    }

    editForm.setFieldsValue({
      activityName: activity.activityName,
      description: activity.description,
      date: dayjs(activity.date),
      startTime: activity.startTime ? dayjs(activity.startTime, 'HH:mm') : undefined,
      durationHours,
      durationMinutes,
      category: activity.category,
      isGroupActivity: activity.isGroupActivity,
    });
    setSelectedClientKeys(activity.participants.map((p) => p.clientId));
    setEditModalOpen(true);
  };

  const handleUpdate = async (values: Record<string, unknown>) => {
    if (!selectedActivity) return;
    try {
      setSubmitting(true);

      // Calculate endTime from startTime + duration
      let endTime: string | undefined;
      if (values.startTime) {
        const startMoment = values.startTime as dayjs.Dayjs;
        const durationHours = (values.durationHours as number) || 0;
        const durationMinutes = (values.durationMinutes as number) || 0;
        if (durationHours > 0 || durationMinutes > 0) {
          endTime = startMoment.add(durationHours, 'hour').add(durationMinutes, 'minute').format('HH:mm');
        }
      }

      const request: UpdateActivityRequest = {
        activityName: values.activityName as string,
        description: values.description as string | undefined,
        date: (values.date as dayjs.Dayjs).format('YYYY-MM-DD'),
        startTime: values.startTime ? (values.startTime as dayjs.Dayjs).format('HH:mm') : undefined,
        endTime,
        category: values.category as ActivityCategory,
        isGroupActivity: values.isGroupActivity as boolean | undefined,
        clientIds: selectedClientKeys,
      };

      const result = await activitiesApi.update(selectedActivity.id, request);
      if (result.success) {
        void message.success('Activity updated successfully');
        setEditModalOpen(false);
        void fetchActivities();
      } else {
        void message.error(result.error || 'Failed to update activity');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to update activity';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (activity: Activity) => {
    Modal.confirm({
      title: 'Delete Activity',
      content: `Are you sure you want to delete "${activity.activityName}"?`,
      okText: 'Delete',
      okType: 'danger',
      cancelText: 'Cancel',
      onOk: async () => {
        try {
          const result = await activitiesApi.delete(activity.id);
          if (result.success) {
            void message.success('Activity deleted successfully');
            void fetchActivities();
          } else {
            void message.error(result.error || 'Failed to delete activity');
          }
        } catch (err) {
          const msg = err instanceof ApiError ? err.message : 'Failed to delete activity';
          void message.error(msg);
        }
      },
    });
  };

  const handleView = (activity: Activity) => {
    setSelectedActivity(activity);
    setViewModalOpen(true);
  };

  const clientOptions = homeClients.map((c) => ({
    label: c.id === clientId ? `${c.fullName} (current)` : c.fullName,
    value: c.id,
    disabled: c.id === clientId, // Current client cannot be removed
  }));

  const columns: ColumnsType<Activity> = [
    {
      title: 'Date',
      dataIndex: 'date',
      key: 'date',
      render: (value: string) => dayjs(value).format('MMM D, YYYY'),
      sorter: (a, b) => dayjs(a.date).unix() - dayjs(b.date).unix(),
      defaultSortOrder: 'descend',
    },
    {
      title: 'Activity',
      dataIndex: 'activityName',
      key: 'activityName',
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
      title: 'Type',
      key: 'type',
      render: (_, record) => (
        <Tag icon={record.isGroupActivity ? <TeamOutlined /> : <UserOutlined />}>
          {record.isGroupActivity ? 'Group' : 'Individual'}
        </Tag>
      ),
      responsive: ['md'],
    },
    {
      title: 'Participants',
      key: 'participants',
      render: (_, record) => (
        <Text>{record.participants.length} client(s)</Text>
      ),
      responsive: ['lg'],
    },
    {
      title: 'Time',
      key: 'time',
      render: (_, record) => {
        if (record.startTime && record.endTime) {
          return <Text>{record.startTime} - {record.endTime}</Text>;
        }
        if (record.startTime) {
          return <Text>{record.startTime}</Text>;
        }
        return <Text type="secondary">—</Text>;
      },
      responsive: ['lg'],
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Button type="link" icon={<EyeOutlined />} onClick={() => handleView(record)}>
            View
          </Button>
          {isAdmin && (
            <>
              <Button type="link" icon={<EditOutlined />} onClick={() => handleEdit(record)} />
              <Button type="link" danger icon={<DeleteOutlined />} onClick={() => handleDelete(record)} />
            </>
          )}
        </Space>
      ),
    },
  ];

  const renderActivityForm = (formInstance: typeof form, onSubmit: (values: Record<string, unknown>) => void) => (
    <Form
      form={formInstance}
      layout="vertical"
      onFinish={(values) => void onSubmit(values)}
      disabled={submitting}
    >
      <Form.Item
        name="activityName"
        label="Activity Name"
        rules={[{ required: true, message: 'Please enter the activity name' }]}
      >
        <Input placeholder="e.g., Bingo, Arts & Crafts, Walking Group" />
      </Form.Item>

      <Form.Item
        name="description"
        label="Description"
      >
        <TextArea rows={2} placeholder="Details about the activity..." />
      </Form.Item>

      <Flex gap={16}>
        <Form.Item
          name="date"
          label="Date"
          rules={[{ required: true, message: 'Required' }]}
          style={{ flex: 1 }}
        >
          <DatePicker style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item
          name="category"
          label="Category"
          rules={[{ required: true, message: 'Required' }]}
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
      </Flex>

      <Flex gap={16}>
        <Form.Item
          name="startTime"
          label="Start Time"
          style={{ flex: 1 }}
        >
          <TimePicker format="h:mm A" style={{ width: '100%' }} use12Hours />
        </Form.Item>
        <Form.Item label="Duration" style={{ flex: 1 }}>
          <Flex gap={8}>
            <Form.Item name="durationHours" noStyle>
              <InputNumber min={0} max={12} placeholder="0" addonAfter="hr" style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="durationMinutes" noStyle>
              <InputNumber min={0} max={59} placeholder="0" addonAfter="min" style={{ width: '100%' }} />
            </Form.Item>
          </Flex>
        </Form.Item>
      </Flex>

      <Form.Item
        name="isGroupActivity"
        valuePropName="checked"
      >
        <Checkbox>This is a group activity</Checkbox>
      </Form.Item>

      <Form.Item noStyle shouldUpdate={(prev, curr) => prev.isGroupActivity !== curr.isGroupActivity}>
        {({ getFieldValue }) =>
          getFieldValue('isGroupActivity') ? (
            <Form.Item label="Additional Participants">
              <Select
                mode="multiple"
                placeholder="Select additional participants"
                options={clientOptions}
                value={selectedClientKeys}
                onChange={(keys: string[]) => {
                  // Ensure current client is always included
                  if (!keys.includes(clientId)) {
                    keys = [clientId, ...keys];
                  }
                  setSelectedClientKeys(keys);
                }}
                style={{ width: '100%' }}
                optionFilterProp="label"
                showSearch
              />
            </Form.Item>
          ) : null
        }
      </Form.Item>

      <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
        <Flex justify="end" gap={8}>
          <Button onClick={() => { setCreateModalOpen(false); setEditModalOpen(false); }}>Cancel</Button>
          <Button type="primary" htmlType="submit" loading={submitting}>
            Save Activity
          </Button>
        </Flex>
      </Form.Item>
    </Form>
  );

  return (
    <>
      <Card
        title={
          <Space>
            <TeamOutlined />
            Activities
          </Space>
        }
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Log Activity
          </Button>
        }
      >
        <Table
          columns={columns}
          dataSource={activities}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 10 }}
          locale={{
            emptyText: <Empty description="No activities recorded" />,
          }}
        />
      </Card>

      {/* Create Modal */}
      <Modal
        title={`Log Activity - ${clientName}`}
        open={createModalOpen}
        onCancel={() => setCreateModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={700}
      >
        {renderActivityForm(form, handleSubmit)}
      </Modal>

      {/* Edit Modal */}
      <Modal
        title="Edit Activity"
        open={editModalOpen}
        onCancel={() => setEditModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={700}
      >
        {renderActivityForm(editForm, handleUpdate)}
      </Modal>

      {/* View Modal */}
      <Modal
        title="Activity Details"
        open={viewModalOpen}
        onCancel={() => setViewModalOpen(false)}
        footer={<Button onClick={() => setViewModalOpen(false)}>Close</Button>}
        width={600}
      >
        {selectedActivity && (
          <Space direction="vertical" size={16} style={{ width: '100%' }}>
            <Descriptions column={2} bordered size="small">
              <Descriptions.Item label="Activity Name" span={2}>
                {selectedActivity.activityName}
              </Descriptions.Item>
              <Descriptions.Item label="Date">
                {dayjs(selectedActivity.date).format('MMMM D, YYYY')}
              </Descriptions.Item>
              <Descriptions.Item label="Category">
                {getCategoryTag(selectedActivity.category)}
              </Descriptions.Item>
              <Descriptions.Item label="Time">
                {selectedActivity.startTime && selectedActivity.endTime
                  ? `${selectedActivity.startTime} - ${selectedActivity.endTime}`
                  : selectedActivity.startTime || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Duration">
                {selectedActivity.duration ? `${selectedActivity.duration} min` : '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Type" span={2}>
                <Tag icon={selectedActivity.isGroupActivity ? <TeamOutlined /> : <UserOutlined />}>
                  {selectedActivity.isGroupActivity ? 'Group Activity' : 'Individual Activity'}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Created By" span={2}>
                {selectedActivity.createdByName}
              </Descriptions.Item>
            </Descriptions>

            {selectedActivity.description && (
              <Card title="Description" size="small">
                <Paragraph>{selectedActivity.description}</Paragraph>
              </Card>
            )}

            <Card title="Participants" size="small">
              {selectedActivity.participants.length > 0 ? (
                <Space wrap>
                  {selectedActivity.participants.map((p) => (
                    <Tag key={p.clientId} icon={<UserOutlined />}>
                      {p.clientName}
                    </Tag>
                  ))}
                </Space>
              ) : (
                <Text type="secondary">No participants recorded</Text>
              )}
            </Card>
          </Space>
        )}
      </Modal>
    </>
  );
}
