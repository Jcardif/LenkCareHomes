'use client';

import React, { useState, useCallback, useEffect } from 'react';
import {
  Modal,
  Tabs,
  Form,
  Input,
  Select,
  InputNumber,
  DatePicker,
  TimePicker,
  Button,
  message,
  Space,
  Typography,
  Flex,
  Tag,
  Checkbox,
} from 'antd';
import {
  MedicineBoxOutlined,
  HeartOutlined,
  SmileOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  adlLogsApi,
  vitalsLogsApi,
  romLogsApi,
  behaviorNotesApi,
  activitiesApi,
  clientsApi,
  ApiError,
} from '@/lib/api';
import type {
  ADLLevel,
  TemperatureUnit,
  BehaviorCategory,
  NoteSeverity,
  ActivityCategory,
  CreateADLLogRequest,
  CreateVitalsLogRequest,
  CreateROMLogRequest,
  CreateBehaviorNoteRequest,
  CreateActivityRequest,
  ClientSummary,
} from '@/types';

const { Text } = Typography;
const { TextArea } = Input;

interface QuickLogModalProps {
  open: boolean;
  onClose: () => void;
  clientId: string;
  clientName: string;
  homeId: string;
  defaultTab?: string;
  onSuccess?: () => void;
}

type ADLTaskKey = 'bathing' | 'dressing' | 'toileting' | 'transferring' | 'continence' | 'feeding';

const ADL_TASKS: { key: ADLTaskKey; label: string; icon: string }[] = [
  { key: 'bathing', label: 'Bathing', icon: '🛁' },
  { key: 'dressing', label: 'Dressing', icon: '👕' },
  { key: 'toileting', label: 'Toileting', icon: '🚽' },
  { key: 'transferring', label: 'Transferring', icon: '🚶' },
  { key: 'continence', label: 'Continence', icon: '🩹' },
  { key: 'feeding', label: 'Feeding', icon: '🍽️' },
];

const ADL_LEVELS: { label: string; value: ADLLevel; color: string }[] = [
  { label: 'No Assistance Needed', value: 'Independent', color: 'green' },
  { label: 'Some Assistance', value: 'PartialAssist', color: 'orange' },
  { label: 'Full Assistance', value: 'Dependent', color: 'red' },
];

const BEHAVIOR_CATEGORIES: { label: string; value: BehaviorCategory; color: string }[] = [
  { label: 'Behavior', value: 'Behavior', color: 'blue' },
  { label: 'Mood', value: 'Mood', color: 'purple' },
  { label: 'General', value: 'General', color: 'default' },
];

const SEVERITIES: { label: string; value: NoteSeverity; color: string }[] = [
  { label: 'Low', value: 'Low', color: 'green' },
  { label: 'Medium', value: 'Medium', color: 'orange' },
  { label: 'High', value: 'High', color: 'red' },
];

const ACTIVITY_CATEGORIES: { label: string; value: ActivityCategory; color: string }[] = [
  { label: 'Recreational', value: 'Recreational', color: 'blue' },
  { label: 'Social', value: 'Social', color: 'purple' },
  { label: 'Exercise', value: 'Exercise', color: 'green' },
  { label: 'Other', value: 'Other', color: 'default' },
];

export default function QuickLogModal({
  open,
  onClose,
  clientId,
  clientName,
  homeId,
  defaultTab = 'adl',
  onSuccess,
}: QuickLogModalProps) {
  const [activeTab, setActiveTab] = useState(defaultTab);
  const [submitting, setSubmitting] = useState(false);
  const [homeClients, setHomeClients] = useState<ClientSummary[]>([]);
  const [selectedClientKeys, setSelectedClientKeys] = useState<string[]>([clientId]);

  // ADL multi-select state
  const [selectedTasks, setSelectedTasks] = useState<ADLTaskKey[]>([]);
  const [taskLevels, setTaskLevels] = useState<Record<ADLTaskKey, ADLLevel>>({
    bathing: 'Independent',
    dressing: 'Independent',
    toileting: 'Independent',
    transferring: 'Independent',
    continence: 'Independent',
    feeding: 'Independent',
  });
  const [useCurrentTimeAdl, setUseCurrentTimeAdl] = useState(true);

  const [adlForm] = Form.useForm();
  const [vitalsForm] = Form.useForm();
  const [romForm] = Form.useForm();
  const [behaviorForm] = Form.useForm();
  const [activityForm] = Form.useForm();

  const fetchHomeClients = useCallback(async () => {
    try {
      const data = await clientsApi.getAll({ homeId, isActive: true });
      setHomeClients(data);
    } catch {
      // Silently fail
    }
  }, [homeId]);

  useEffect(() => {
    if (open) {
      void fetchHomeClients();
      // Reset forms
      adlForm.resetFields();
      vitalsForm.resetFields();
      romForm.resetFields();
      behaviorForm.resetFields();
      activityForm.resetFields();
      // Reset ADL multi-select state
      setSelectedTasks([]);
      setTaskLevels({
        bathing: 'Independent',
        dressing: 'Independent',
        toileting: 'Independent',
        transferring: 'Independent',
        continence: 'Independent',
        feeding: 'Independent',
      });
      setUseCurrentTimeAdl(true);
      // Set defaults
      const now = dayjs();
      adlForm.setFieldsValue({ timestamp: now, time: now });
      vitalsForm.setFieldsValue({ timestamp: now, time: now, temperatureUnit: 'Fahrenheit' });
      romForm.setFieldsValue({ timestamp: now, time: now });
      behaviorForm.setFieldsValue({ timestamp: now, time: now, severity: 'Low' });
      activityForm.setFieldsValue({ date: now });
      setSelectedClientKeys([clientId]);
    }
  }, [open, adlForm, vitalsForm, romForm, behaviorForm, activityForm, fetchHomeClients, clientId]);

  const getTimestamp = (date: dayjs.Dayjs, time: dayjs.Dayjs) => {
    return date.hour(time.hour()).minute(time.minute()).toISOString();
  };

  const handleSubmitADL = async (values: Record<string, unknown>) => {
    try {
      if (selectedTasks.length === 0) {
        void message.error('Please select at least one ADL task');
        return;
      }

      setSubmitting(true);
      
      // Determine timestamp
      let timestamp: string;
      if (useCurrentTimeAdl) {
        timestamp = dayjs().toISOString();
      } else {
        const date = values.timestamp as dayjs.Dayjs;
        const time = values.time as dayjs.Dayjs;
        timestamp = date.hour(time.hour()).minute(time.minute()).toISOString();
      }

      const request: CreateADLLogRequest = {
        timestamp,
        notes: values.notes as string | undefined,
        bathing: selectedTasks.includes('bathing') ? taskLevels.bathing : undefined,
        dressing: selectedTasks.includes('dressing') ? taskLevels.dressing : undefined,
        toileting: selectedTasks.includes('toileting') ? taskLevels.toileting : undefined,
        transferring: selectedTasks.includes('transferring') ? taskLevels.transferring : undefined,
        continence: selectedTasks.includes('continence') ? taskLevels.continence : undefined,
        feeding: selectedTasks.includes('feeding') ? taskLevels.feeding : undefined,
      };

      const result = await adlLogsApi.create(clientId, request);
      if (result.success) {
        void message.success('ADL logged');
        onSuccess?.();
        onClose();
      } else {
        void message.error(result.error || 'Failed to log ADL');
      }
    } catch (err) {
      void message.error(err instanceof ApiError ? err.message : 'Failed to log ADL');
    } finally {
      setSubmitting(false);
    }
  };

  const handleSubmitVitals = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);
      const request: CreateVitalsLogRequest = {
        timestamp: getTimestamp(values.timestamp as dayjs.Dayjs, values.time as dayjs.Dayjs),
        systolicBP: values.systolicBP as number | undefined,
        diastolicBP: values.diastolicBP as number | undefined,
        pulse: values.pulse as number | undefined,
        temperature: values.temperature as number | undefined,
        temperatureUnit: values.temperatureUnit as TemperatureUnit | undefined,
        oxygenSaturation: values.oxygenSaturation as number | undefined,
        notes: values.notes as string | undefined,
      };

      const hasAtLeastOne = [request.systolicBP, request.diastolicBP, request.pulse, request.temperature, request.oxygenSaturation]
        .some((v) => v !== undefined && v !== null);
      if (!hasAtLeastOne) {
        void message.error('Please enter at least one vital sign');
        return;
      }

      const result = await vitalsLogsApi.create(clientId, request);
      if (result.success) {
        void message.success('Vitals saved');
        onSuccess?.();
        onClose();
      } else {
        void message.error(result.error || 'Failed to save vitals');
      }
    } catch (err) {
      void message.error(err instanceof ApiError ? err.message : 'Failed to save vitals');
    } finally {
      setSubmitting(false);
    }
  };

  const handleSubmitROM = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);
      const request: CreateROMLogRequest = {
        timestamp: getTimestamp(values.timestamp as dayjs.Dayjs, values.time as dayjs.Dayjs),
        activityDescription: values.activityDescription as string,
        duration: values.duration as number | undefined,
        repetitions: values.repetitions as number | undefined,
        notes: values.notes as string | undefined,
      };

      const result = await romLogsApi.create(clientId, request);
      if (result.success) {
        void message.success('ROM exercise saved');
        onSuccess?.();
        onClose();
      } else {
        void message.error(result.error || 'Failed to save ROM exercise');
      }
    } catch (err) {
      void message.error(err instanceof ApiError ? err.message : 'Failed to save ROM exercise');
    } finally {
      setSubmitting(false);
    }
  };

  const handleSubmitBehavior = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);
      const request: CreateBehaviorNoteRequest = {
        timestamp: getTimestamp(values.timestamp as dayjs.Dayjs, values.time as dayjs.Dayjs),
        category: values.category as BehaviorCategory,
        noteText: values.noteText as string,
        severity: values.severity as NoteSeverity | undefined,
      };

      const result = await behaviorNotesApi.create(clientId, request);
      if (result.success) {
        void message.success('Behavior note saved');
        onSuccess?.();
        onClose();
      } else {
        void message.error(result.error || 'Failed to save behavior note');
      }
    } catch (err) {
      void message.error(err instanceof ApiError ? err.message : 'Failed to save behavior note');
    } finally {
      setSubmitting(false);
    }
  };

  const handleSubmitActivity = async (values: Record<string, unknown>) => {
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
        void message.success('Activity saved');
        onSuccess?.();
        onClose();
      } else {
        void message.error(result.error || 'Failed to save activity');
      }
    } catch (err) {
      void message.error(err instanceof ApiError ? err.message : 'Failed to save activity');
    } finally {
      setSubmitting(false);
    }
  };

  const clientOptions = homeClients.map((c) => ({
    label: c.id === clientId ? `${c.fullName} (current)` : c.fullName,
    value: c.id,
    disabled: c.id === clientId, // Current client cannot be removed
  }));

  const dateTimeFields = (
    <Flex gap={16}>
      <Form.Item name="timestamp" label="Date" rules={[{ required: true }]} style={{ flex: 1 }}>
        <DatePicker style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item name="time" label="Time" rules={[{ required: true }]} style={{ flex: 1 }}>
        <TimePicker format="h:mm A" style={{ width: '100%' }} use12Hours />
      </Form.Item>
    </Flex>
  );

  const tabItems = [
    {
      key: 'adl',
      label: (
        <Space>
          <MedicineBoxOutlined />
          ADL
        </Space>
      ),
      children: (
        <Form form={adlForm} layout="vertical" onFinish={(v) => void handleSubmitADL(v)} disabled={submitting}>
          <div style={{ marginBottom: 16 }}>
            <Typography.Text strong style={{ display: 'block', marginBottom: 8 }}>
              What did you help with? (select all that apply)
            </Typography.Text>
            <Checkbox.Group
              value={selectedTasks}
              onChange={(values) => setSelectedTasks(values as ADLTaskKey[])}
              style={{ width: '100%' }}
            >
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
                {ADL_TASKS.map((task) => (
                  <div
                    key={task.key}
                    style={{
                      padding: '8px 12px',
                      borderRadius: 8,
                      border: selectedTasks.includes(task.key) ? '2px solid #1890ff' : '1px solid #d9d9d9',
                      background: selectedTasks.includes(task.key) ? '#e6f7ff' : 'white',
                    }}
                  >
                    <Checkbox value={task.key}>
                      <Space>
                        <span style={{ fontSize: 18 }}>{task.icon}</span>
                        <Text>{task.label}</Text>
                      </Space>
                    </Checkbox>
                    {selectedTasks.includes(task.key) && (
                      <Select
                        size="small"
                        value={taskLevels[task.key]}
                        onChange={(val) => setTaskLevels((prev) => ({ ...prev, [task.key]: val }))}
                        style={{ width: '100%', marginTop: 4 }}
                        options={ADL_LEVELS.map((l) => ({
                          label: <Tag color={l.color}>{l.label}</Tag>,
                          value: l.value,
                        }))}
                        onClick={(e) => e.stopPropagation()}
                      />
                    )}
                  </div>
                ))}
              </div>
            </Checkbox.Group>
          </div>
          <div style={{ marginBottom: 16 }}>
            <Checkbox
              checked={useCurrentTimeAdl}
              onChange={(e) => setUseCurrentTimeAdl(e.target.checked)}
            >
              Use current time
            </Checkbox>
          </div>
          {!useCurrentTimeAdl && (
            <Flex gap={16} style={{ marginBottom: 16 }}>
              <Form.Item name="timestamp" label="Date" style={{ flex: 1, marginBottom: 0 }}>
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="time" label="Time" style={{ flex: 1, marginBottom: 0 }}>
                <TimePicker format="h:mm A" style={{ width: '100%' }} use12Hours />
              </Form.Item>
            </Flex>
          )}
          <Form.Item name="notes" label="Notes (optional)">
            <TextArea rows={2} placeholder="Additional observations..." />
          </Form.Item>
          <Flex justify="end" gap={8}>
            <Button onClick={onClose}>Cancel</Button>
            <Button
              type="primary"
              htmlType="submit"
              loading={submitting}
              disabled={selectedTasks.length === 0}
            >
              Log ADL
            </Button>
          </Flex>
        </Form>
      ),
    },
    {
      key: 'vitals',
      label: (
        <Space>
          <HeartOutlined />
          Vitals
        </Space>
      ),
      children: (
        <Form form={vitalsForm} layout="vertical" onFinish={(v) => void handleSubmitVitals(v)} disabled={submitting}>
          {dateTimeFields}
          <Flex gap={16}>
            <Form.Item name="systolicBP" label="Systolic BP" style={{ flex: 1 }} rules={[{ type: 'number', min: 50, max: 300 }]}>
              <InputNumber style={{ width: '100%' }} placeholder="120" addonAfter="mmHg" />
            </Form.Item>
            <Form.Item name="diastolicBP" label="Diastolic BP" style={{ flex: 1 }} rules={[{ type: 'number', min: 30, max: 200 }]}>
              <InputNumber style={{ width: '100%' }} placeholder="80" addonAfter="mmHg" />
            </Form.Item>
          </Flex>
          <Flex gap={16}>
            <Form.Item name="pulse" label="Pulse" style={{ flex: 1 }} rules={[{ type: 'number', min: 30, max: 200 }]}>
              <InputNumber style={{ width: '100%' }} placeholder="72" addonAfter="bpm" />
            </Form.Item>
            <Form.Item name="oxygenSaturation" label="O₂ Sat" style={{ flex: 1 }} rules={[{ type: 'number', min: 70, max: 100 }]}>
              <InputNumber style={{ width: '100%' }} placeholder="98" addonAfter="%" />
            </Form.Item>
          </Flex>
          <Flex gap={16}>
            <Form.Item
              name="temperature"
              label="Temperature"
              style={{ flex: 1 }}
              dependencies={['temperatureUnit']}
              rules={[
                ({ getFieldValue }) => ({
                  validator(_, value) {
                    if (value === undefined || value === null) {
                      return Promise.resolve();
                    }
                    const unit = getFieldValue('temperatureUnit') as string;
                    if (unit === 'Celsius') {
                      if (value < 32 || value > 43) {
                        return Promise.reject(new Error('32-43°C'));
                      }
                    } else {
                      if (value < 90 || value > 110) {
                        return Promise.reject(new Error('90-110°F'));
                      }
                    }
                    return Promise.resolve();
                  },
                }),
              ]}
            >
              <InputNumber style={{ width: '100%' }} placeholder="98.6" step={0.1} />
            </Form.Item>
            <Form.Item name="temperatureUnit" label="Unit" style={{ flex: 1 }}>
              <Select
                options={[{ label: '°F', value: 'Fahrenheit' }, { label: '°C', value: 'Celsius' }]}
                onChange={() => void vitalsForm.validateFields(['temperature'])}
              />
            </Form.Item>
          </Flex>
          <Form.Item name="notes" label="Notes">
            <TextArea rows={2} placeholder="Additional observations..." />
          </Form.Item>
          <Flex justify="end" gap={8}>
            <Button onClick={onClose}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>Save Vitals</Button>
          </Flex>
        </Form>
      ),
    },
    {
      key: 'rom',
      label: (
        <Space>
          <MedicineBoxOutlined />
          ROM
        </Space>
      ),
      children: (
        <Form form={romForm} layout="vertical" onFinish={(v) => void handleSubmitROM(v)} disabled={submitting}>
          {dateTimeFields}
          <Form.Item name="activityDescription" label="Exercise Description" rules={[{ required: true }]}>
            <TextArea rows={2} placeholder="Describe the ROM exercises..." />
          </Form.Item>
          <Flex gap={16}>
            <Form.Item name="duration" label="Duration (min)" style={{ flex: 1 }}>
              <InputNumber style={{ width: '100%' }} placeholder="15" />
            </Form.Item>
            <Form.Item name="repetitions" label="Repetitions" style={{ flex: 1 }}>
              <InputNumber style={{ width: '100%' }} placeholder="10" />
            </Form.Item>
          </Flex>
          <Form.Item name="notes" label="Notes">
            <TextArea rows={2} placeholder="Client response, tolerance..." />
          </Form.Item>
          <Flex justify="end" gap={8}>
            <Button onClick={onClose}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>Save ROM</Button>
          </Flex>
        </Form>
      ),
    },
    {
      key: 'behavior',
      label: (
        <Space>
          <SmileOutlined />
          Behavior
        </Space>
      ),
      children: (
        <Form form={behaviorForm} layout="vertical" onFinish={(v) => void handleSubmitBehavior(v)} disabled={submitting}>
          {dateTimeFields}
          <Flex gap={16}>
            <Form.Item name="category" label="Category" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Select placeholder="Select" options={BEHAVIOR_CATEGORIES.map((c) => ({ label: <Tag color={c.color}>{c.label}</Tag>, value: c.value }))} />
            </Form.Item>
            <Form.Item name="severity" label="Severity" style={{ flex: 1 }}>
              <Select options={SEVERITIES.map((s) => ({ label: <Tag color={s.color}>{s.label}</Tag>, value: s.value }))} />
            </Form.Item>
          </Flex>
          <Form.Item name="noteText" label="Note" rules={[{ required: true }, { max: 4000 }]}>
            <TextArea rows={4} placeholder="Describe the observation..." showCount maxLength={4000} />
          </Form.Item>
          <Flex justify="end" gap={8}>
            <Button onClick={onClose}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>Save Note</Button>
          </Flex>
        </Form>
      ),
    },
    {
      key: 'activity',
      label: (
        <Space>
          <TeamOutlined />
          Activity
        </Space>
      ),
      children: (
        <Form form={activityForm} layout="vertical" onFinish={(v) => void handleSubmitActivity(v)} disabled={submitting}>
          <Form.Item name="activityName" label="Activity Name" rules={[{ required: true }]}>
            <Input placeholder="e.g., Bingo, Walking Group" />
          </Form.Item>
          <Flex gap={16}>
            <Form.Item name="date" label="Date" rules={[{ required: true }]} style={{ flex: 1 }}>
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="category" label="Category" rules={[{ required: true }]} style={{ flex: 1 }}>
              <Select placeholder="Select" options={ACTIVITY_CATEGORIES.map((c) => ({ label: <Tag color={c.color}>{c.label}</Tag>, value: c.value }))} />
            </Form.Item>
          </Flex>
          <Flex gap={16}>
            <Form.Item name="startTime" label="Start Time" style={{ flex: 1 }}>
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
          <Form.Item name="description" label="Description">
            <TextArea rows={2} placeholder="Details about the activity..." />
          </Form.Item>
          <Form.Item name="isGroupActivity" valuePropName="checked">
            <Checkbox>Group activity</Checkbox>
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
          <Flex justify="end" gap={8}>
            <Button onClick={onClose}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={submitting}>Save Activity</Button>
          </Flex>
        </Form>
      ),
    },
  ];

  return (
    <Modal
      title={`Quick Log - ${clientName}`}
      open={open}
      onCancel={onClose}
      footer={null}
      destroyOnHidden
      width={650}
    >
      <Tabs
        activeKey={activeTab}
        onChange={setActiveTab}
        items={tabItems}
      />
    </Modal>
  );
}
