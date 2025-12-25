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
  Select,
  message,
  Space,
  Typography,
  Tag,
  Descriptions,
  Alert,
  Empty,
  Flex,
  Statistic,
} from 'antd';
import { PlusOutlined, HeartOutlined, WarningOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import type { ColumnsType } from 'antd/es/table';

dayjs.extend(utc);
import { vitalsLogsApi, ApiError } from '@/lib/api';
import type { VitalsLog, TemperatureUnit, CreateVitalsLogRequest } from '@/types';

const { Text, Paragraph } = Typography;
const { TextArea } = Input;

interface VitalsLogTabProps {
  clientId: string;
  clientName: string;
}

function getBPStatus(systolic?: number, diastolic?: number): { text: string; color: string } | null {
  if (!systolic || !diastolic) return null;
  if (systolic < 90 || diastolic < 60) return { text: 'Low', color: 'blue' };
  if (systolic > 180 || diastolic > 120) return { text: 'Crisis', color: 'red' };
  if (systolic > 140 || diastolic > 90) return { text: 'High', color: 'volcano' };
  if (systolic > 130 || diastolic > 80) return { text: 'Elevated', color: 'orange' };
  return { text: 'Normal', color: 'green' };
}

function getPulseStatus(pulse?: number): { text: string; color: string } | null {
  if (!pulse) return null;
  if (pulse < 60) return { text: 'Low', color: 'blue' };
  if (pulse > 100) return { text: 'High', color: 'orange' };
  return { text: 'Normal', color: 'green' };
}

function getO2Status(o2?: number): { text: string; color: string } | null {
  if (!o2) return null;
  if (o2 < 90) return { text: 'Critical', color: 'red' };
  if (o2 < 95) return { text: 'Low', color: 'orange' };
  return { text: 'Normal', color: 'green' };
}

function getTempStatus(temp?: number, unit?: TemperatureUnit): { text: string; color: string } | null {
  if (!temp) return null;
  const tempF = unit === 'Celsius' ? (temp * 9) / 5 + 32 : temp;
  if (tempF < 96) return { text: 'Low', color: 'blue' };
  if (tempF > 100.4) return { text: 'Fever', color: 'red' };
  return { text: 'Normal', color: 'green' };
}

export default function VitalsLogTab({ clientId, clientName }: VitalsLogTabProps) {
  const [logs, setLogs] = useState<VitalsLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [viewModalOpen, setViewModalOpen] = useState(false);
  const [selectedLog, setSelectedLog] = useState<VitalsLog | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [form] = Form.useForm();

  const fetchLogs = useCallback(async () => {
    try {
      setLoading(true);
      const data = await vitalsLogsApi.getAll(clientId);
      setLogs(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load vitals logs';
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
    form.setFieldValue('temperatureUnit', 'Fahrenheit');
    setCreateModalOpen(true);
  };

  const handleSubmit = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);
      const timestamp = dayjs(values.timestamp as dayjs.Dayjs)
        .hour((values.time as dayjs.Dayjs).hour())
        .minute((values.time as dayjs.Dayjs).minute())
        .toISOString();

      const request: CreateVitalsLogRequest = {
        timestamp,
        systolicBP: values.systolicBP as number | undefined,
        diastolicBP: values.diastolicBP as number | undefined,
        pulse: values.pulse as number | undefined,
        temperature: values.temperature as number | undefined,
        temperatureUnit: values.temperatureUnit as TemperatureUnit | undefined,
        oxygenSaturation: values.oxygenSaturation as number | undefined,
        notes: values.notes as string | undefined,
      };

      // Validate at least one vital is filled
      const hasAtLeastOne = [
        request.systolicBP,
        request.diastolicBP,
        request.pulse,
        request.temperature,
        request.oxygenSaturation,
      ].some((v) => v !== undefined && v !== null);

      if (!hasAtLeastOne) {
        void message.error('Please enter at least one vital sign');
        return;
      }

      const result = await vitalsLogsApi.create(clientId, request);
      if (result.success) {
        void message.success('Vitals log created successfully');
        setCreateModalOpen(false);
        void fetchLogs();
      } else {
        void message.error(result.error || 'Failed to create vitals log');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to create vitals log';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleView = (log: VitalsLog) => {
    setSelectedLog(log);
    setViewModalOpen(true);
  };

  // Check for any abnormal readings in the most recent log
  const latestLog = logs[0];
  const hasAbnormalReadings = latestLog && (
    getBPStatus(latestLog.systolicBP, latestLog.diastolicBP)?.color !== 'green' ||
    getPulseStatus(latestLog.pulse)?.color !== 'green' ||
    getO2Status(latestLog.oxygenSaturation)?.color !== 'green' ||
    getTempStatus(latestLog.temperature, latestLog.temperatureUnit)?.color !== 'green'
  );

  const columns: ColumnsType<VitalsLog> = [
    {
      title: 'Date & Time',
      dataIndex: 'timestamp',
      key: 'timestamp',
      render: (value: string) => dayjs.utc(value).local().format('MMM D, YYYY h:mm A'),
      sorter: (a, b) => dayjs.utc(a.timestamp).unix() - dayjs.utc(b.timestamp).unix(),
      defaultSortOrder: 'descend',
    },
    {
      title: 'Blood Pressure',
      key: 'bp',
      render: (_, record) => {
        if (!record.systolicBP && !record.diastolicBP) return <Text type="secondary">—</Text>;
        const status = getBPStatus(record.systolicBP, record.diastolicBP);
        return (
          <Space>
            <Text>{record.bloodPressure || `${record.systolicBP}/${record.diastolicBP}`}</Text>
            {status && <Tag color={status.color}>{status.text}</Tag>}
          </Space>
        );
      },
    },
    {
      title: 'Pulse',
      dataIndex: 'pulse',
      key: 'pulse',
      render: (value?: number) => {
        if (!value) return <Text type="secondary">—</Text>;
        const status = getPulseStatus(value);
        return (
          <Space>
            <Text>{value} bpm</Text>
            {status && <Tag color={status.color}>{status.text}</Tag>}
          </Space>
        );
      },
      responsive: ['md'],
    },
    {
      title: 'O₂ Sat',
      dataIndex: 'oxygenSaturation',
      key: 'oxygenSaturation',
      render: (value?: number) => {
        if (!value) return <Text type="secondary">—</Text>;
        const status = getO2Status(value);
        return (
          <Space>
            <Text>{value}%</Text>
            {status && <Tag color={status.color}>{status.text}</Tag>}
          </Space>
        );
      },
      responsive: ['md'],
    },
    {
      title: 'Temp',
      key: 'temperature',
      render: (_, record) => {
        if (!record.temperature) return <Text type="secondary">—</Text>;
        const status = getTempStatus(record.temperature, record.temperatureUnit);
        const unit = record.temperatureUnit === 'Celsius' ? '°C' : '°F';
        return (
          <Space>
            <Text>{record.temperature}{unit}</Text>
            {status && <Tag color={status.color}>{status.text}</Tag>}
          </Space>
        );
      },
      responsive: ['lg'],
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
      {hasAbnormalReadings && latestLog && (
        <Alert
          type="warning"
          message="Abnormal Vital Signs Detected"
          description={`The most recent vitals recorded on ${dayjs.utc(latestLog.timestamp).local().format('MMM D, h:mm A')} show readings outside normal ranges. Please review.`}
          icon={<WarningOutlined />}
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      <Card
        title={
          <Space>
            <HeartOutlined />
            Vital Signs
          </Space>
        }
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Record Vitals
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
            emptyText: <Empty description="No vital signs recorded" />,
          }}
        />
      </Card>

      {/* Create Modal */}
      <Modal
        title={`Record Vital Signs - ${clientName}`}
        open={createModalOpen}
        onCancel={() => setCreateModalOpen(false)}
        footer={null}
        destroyOnHidden
        width={600}
      >
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

          <Card title="Blood Pressure" size="small" style={{ marginBottom: 16 }}>
            <Flex gap={16}>
              <Form.Item
                name="systolicBP"
                label="Systolic (top number)"
                style={{ flex: 1 }}
                rules={[
                  { type: 'number', min: 50, max: 300, message: 'Must be between 50-300' },
                ]}
              >
                <InputNumber
                  style={{ width: '100%' }}
                  placeholder="e.g., 120"
                  addonAfter="mmHg"
                />
              </Form.Item>
              <Form.Item
                name="diastolicBP"
                label="Diastolic (bottom number)"
                style={{ flex: 1 }}
                rules={[
                  { type: 'number', min: 30, max: 200, message: 'Must be between 30-200' },
                ]}
              >
                <InputNumber
                  style={{ width: '100%' }}
                  placeholder="e.g., 80"
                  addonAfter="mmHg"
                />
              </Form.Item>
            </Flex>
          </Card>

          <Flex gap={16}>
            <Form.Item
              name="pulse"
              label="Pulse"
              style={{ flex: 1 }}
              rules={[
                { type: 'number', min: 30, max: 200, message: 'Must be between 30-200' },
              ]}
            >
              <InputNumber
                style={{ width: '100%' }}
                placeholder="e.g., 72"
                addonAfter="bpm"
              />
            </Form.Item>
            <Form.Item
              name="oxygenSaturation"
              label="Oxygen Saturation"
              style={{ flex: 1 }}
              rules={[
                { type: 'number', min: 70, max: 100, message: 'Must be between 70-100' },
              ]}
            >
              <InputNumber
                style={{ width: '100%' }}
                placeholder="e.g., 98"
                addonAfter="%"
              />
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
                        return Promise.reject(new Error('Must be between 32-43°C'));
                      }
                    } else {
                      if (value < 90 || value > 110) {
                        return Promise.reject(new Error('Must be between 90-110°F'));
                      }
                    }
                    return Promise.resolve();
                  },
                }),
              ]}
            >
              <InputNumber
                style={{ width: '100%' }}
                placeholder="e.g., 98.6"
                step={0.1}
              />
            </Form.Item>
            <Form.Item
              name="temperatureUnit"
              label="Unit"
              style={{ flex: 1 }}
            >
              <Select
                options={[
                  { label: 'Fahrenheit (°F)', value: 'Fahrenheit' },
                  { label: 'Celsius (°C)', value: 'Celsius' },
                ]}
                onChange={() => {
                  // Re-validate temperature when unit changes
                  void form.validateFields(['temperature']);
                }}
              />
            </Form.Item>
          </Flex>

          <Form.Item name="notes" label="Notes">
            <TextArea rows={3} placeholder="Additional observations..." />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex justify="end" gap={8}>
              <Button onClick={() => setCreateModalOpen(false)}>Cancel</Button>
              <Button type="primary" htmlType="submit" loading={submitting}>
                Save Vitals
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>

      {/* View Modal */}
      <Modal
        title="Vital Signs Details"
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
            </Descriptions>

            <Card size="small">
              <Flex wrap="wrap" gap={24} justify="space-around">
                {(selectedLog.systolicBP || selectedLog.diastolicBP) && (
                  <Statistic
                    title="Blood Pressure"
                    value={selectedLog.bloodPressure || `${selectedLog.systolicBP}/${selectedLog.diastolicBP}`}
                    suffix="mmHg"
                  />
                )}
                {selectedLog.pulse && (
                  <Statistic
                    title="Pulse"
                    value={selectedLog.pulse}
                    suffix="bpm"
                  />
                )}
                {selectedLog.oxygenSaturation && (
                  <Statistic
                    title="O₂ Saturation"
                    value={selectedLog.oxygenSaturation}
                    suffix="%"
                  />
                )}
                {selectedLog.temperature && (
                  <Statistic
                    title="Temperature"
                    value={selectedLog.temperature}
                    suffix={selectedLog.temperatureUnit === 'Celsius' ? '°C' : '°F'}
                  />
                )}
              </Flex>
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
