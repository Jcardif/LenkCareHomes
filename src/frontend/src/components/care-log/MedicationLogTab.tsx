'use client';

import React, { useState, useCallback, useEffect } from 'react';
import {
  Card,
  Button,
  Table,
  Modal,
  Form,
  Input,
  DatePicker,
  TimePicker,
  Select,
  message,
  Space,
  Typography,
  Tag,
  Descriptions,
  Empty,
  Flex,
} from 'antd';
import { PlusOutlined, MedicineBoxOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import type { ColumnsType } from 'antd/es/table';

dayjs.extend(utc);
import { medicationLogsApi, ApiError } from '@/lib/api';
import type { MedicationLog, MedicationRoute, MedicationStatus, CreateMedicationLogRequest } from '@/types';

const { Text, Paragraph } = Typography;
const { TextArea } = Input;

interface MedicationLogTabProps {
  clientId: string;
  clientName: string;
}

const ROUTE_OPTIONS: { label: string; value: MedicationRoute }[] = [
  { label: 'Oral (by mouth)', value: 'Oral' },
  { label: 'Sublingual (under tongue)', value: 'Sublingual' },
  { label: 'Topical (skin)', value: 'Topical' },
  { label: 'Inhalation', value: 'Inhalation' },
  { label: 'Injection', value: 'Injection' },
  { label: 'Transdermal (patch)', value: 'Transdermal' },
  { label: 'Rectal', value: 'Rectal' },
  { label: 'Ophthalmic (eye)', value: 'Ophthalmic' },
  { label: 'Otic (ear)', value: 'Otic' },
  { label: 'Nasal', value: 'Nasal' },
  { label: 'Other', value: 'Other' },
];

const STATUS_OPTIONS: { label: string; value: MedicationStatus }[] = [
  { label: 'Administered', value: 'Administered' },
  { label: 'Refused', value: 'Refused' },
  { label: 'Not Available', value: 'NotAvailable' },
  { label: 'Held', value: 'Held' },
  { label: 'Given Early', value: 'GivenEarly' },
  { label: 'Given Late', value: 'GivenLate' },
];

function getStatusColor(status: MedicationStatus): string {
  switch (status) {
    case 'Administered':
      return 'green';
    case 'Refused':
      return 'red';
    case 'NotAvailable':
      return 'orange';
    case 'Held':
      return 'blue';
    case 'GivenEarly':
      return 'cyan';
    case 'GivenLate':
      return 'gold';
    default:
      return 'default';
  }
}

function getStatusLabel(status: MedicationStatus): string {
  switch (status) {
    case 'NotAvailable':
      return 'Not Available';
    case 'GivenEarly':
      return 'Given Early';
    case 'GivenLate':
      return 'Given Late';
    default:
      return status;
  }
}

function getStatusIcon(status: MedicationStatus): React.ReactNode {
  if (status === 'Administered') {
    return <CheckCircleOutlined style={{ color: '#52c41a' }} />;
  }
  if (status === 'Refused' || status === 'NotAvailable') {
    return <CloseCircleOutlined style={{ color: '#ff4d4f' }} />;
  }
  return null;
}

export default function MedicationLogTab({ clientId, clientName }: MedicationLogTabProps) {
  const [logs, setLogs] = useState<MedicationLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [viewModalOpen, setViewModalOpen] = useState(false);
  const [selectedLog, setSelectedLog] = useState<MedicationLog | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [form] = Form.useForm();

  const fetchLogs = useCallback(async () => {
    try {
      setLoading(true);
      const data = await medicationLogsApi.getAll(clientId);
      setLogs(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load medication logs';
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
    form.setFieldValue('route', 'Oral');
    form.setFieldValue('status', 'Administered');
    setCreateModalOpen(true);
  };

  const handleSubmit = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);
      const timestamp = dayjs(values.timestamp as dayjs.Dayjs)
        .hour((values.time as dayjs.Dayjs).hour())
        .minute((values.time as dayjs.Dayjs).minute())
        .toISOString();

      let scheduledTime: string | undefined;
      if (values.scheduledDate && values.scheduledTime) {
        scheduledTime = dayjs(values.scheduledDate as dayjs.Dayjs)
          .hour((values.scheduledTime as dayjs.Dayjs).hour())
          .minute((values.scheduledTime as dayjs.Dayjs).minute())
          .toISOString();
      }

      const request: CreateMedicationLogRequest = {
        timestamp,
        medicationName: (values.medicationName as string).trim(),
        dosage: (values.dosage as string).trim(),
        route: values.route as MedicationRoute,
        status: values.status as MedicationStatus,
        scheduledTime,
        prescribedBy: (values.prescribedBy as string)?.trim() || undefined,
        pharmacy: (values.pharmacy as string)?.trim() || undefined,
        rxNumber: (values.rxNumber as string)?.trim() || undefined,
        notes: (values.notes as string)?.trim() || undefined,
      };

      const result = await medicationLogsApi.create(clientId, request);
      if (result.success) {
        void message.success('Medication log created successfully');
        setCreateModalOpen(false);
        void fetchLogs();
      } else {
        void message.error(result.error || 'Failed to create medication log');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to create medication log';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleView = (log: MedicationLog) => {
    setSelectedLog(log);
    setViewModalOpen(true);
  };

  const columns: ColumnsType<MedicationLog> = [
    {
      title: 'Date & Time',
      dataIndex: 'timestamp',
      key: 'timestamp',
      render: (value: string) => dayjs.utc(value).local().format('MMM D, YYYY h:mm A'),
      sorter: (a, b) => dayjs.utc(a.timestamp).unix() - dayjs.utc(b.timestamp).unix(),
      defaultSortOrder: 'descend',
    },
    {
      title: 'Medication',
      dataIndex: 'medicationName',
      key: 'medicationName',
      render: (value: string) => <Text strong>{value}</Text>,
    },
    {
      title: 'Dosage',
      dataIndex: 'dosage',
      key: 'dosage',
    },
    {
      title: 'Route',
      dataIndex: 'route',
      key: 'route',
      responsive: ['md'],
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (value: MedicationStatus) => (
        <Tag color={getStatusColor(value)} icon={getStatusIcon(value)}>
          {getStatusLabel(value)}
        </Tag>
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
      <Card
        title={
          <Space>
            <MedicineBoxOutlined />
            Medication Administration
          </Space>
        }
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Record Medication
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
            emptyText: <Empty description="No medications recorded" />,
          }}
        />
      </Card>

      {/* Create Modal */}
      <Modal
        title={`Record Medication - ${clientName}`}
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

          <Card title="Medication Details" size="small" style={{ marginBottom: 16 }}>
            <Form.Item
              name="medicationName"
              label="Medication Name"
              rules={[{ required: true, message: 'Medication name is required' }]}
            >
              <Input placeholder="e.g., Metformin, Lisinopril, Aspirin" />
            </Form.Item>

            <Flex gap={16}>
              <Form.Item
                name="dosage"
                label="Dosage"
                rules={[{ required: true, message: 'Dosage is required' }]}
                style={{ flex: 1 }}
              >
                <Input placeholder="e.g., 500mg, 2 tablets, 10ml" />
              </Form.Item>
              <Form.Item
                name="route"
                label="Route"
                style={{ flex: 1 }}
              >
                <Select options={ROUTE_OPTIONS} />
              </Form.Item>
            </Flex>

            <Form.Item
              name="status"
              label="Administration Status"
            >
              <Select options={STATUS_OPTIONS} />
            </Form.Item>
          </Card>

          <Card title="Scheduled Time (Optional)" size="small" style={{ marginBottom: 16 }}>
            <Flex gap={16}>
              <Form.Item
                name="scheduledDate"
                label="Scheduled Date"
                style={{ flex: 1 }}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item
                name="scheduledTime"
                label="Scheduled Time"
                style={{ flex: 1 }}
              >
                <TimePicker format="h:mm A" style={{ width: '100%' }} use12Hours />
              </Form.Item>
            </Flex>
          </Card>

          <Card title="Prescription Info (Optional)" size="small" style={{ marginBottom: 16 }}>
            <Flex gap={16}>
              <Form.Item
                name="prescribedBy"
                label="Prescribed By"
                style={{ flex: 1 }}
              >
                <Input placeholder="Physician name" />
              </Form.Item>
              <Form.Item
                name="pharmacy"
                label="Pharmacy"
                style={{ flex: 1 }}
              >
                <Input placeholder="Pharmacy name" />
              </Form.Item>
            </Flex>
            <Form.Item
              name="rxNumber"
              label="Rx Number"
            >
              <Input placeholder="Prescription number" />
            </Form.Item>
          </Card>

          <Form.Item name="notes" label="Notes">
            <TextArea rows={3} placeholder="Additional observations..." />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
            <Flex justify="end" gap={8}>
              <Button onClick={() => setCreateModalOpen(false)}>Cancel</Button>
              <Button type="primary" htmlType="submit" loading={submitting}>
                Save Medication
              </Button>
            </Flex>
          </Form.Item>
        </Form>
      </Modal>

      {/* View Modal */}
      <Modal
        title="Medication Details"
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
              <Descriptions.Item label="Medication" span={2}>
                <Text strong>{selectedLog.medicationName}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="Dosage">
                {selectedLog.dosage}
              </Descriptions.Item>
              <Descriptions.Item label="Route">
                {selectedLog.route}
              </Descriptions.Item>
              <Descriptions.Item label="Status" span={2}>
                <Tag color={getStatusColor(selectedLog.status)} icon={getStatusIcon(selectedLog.status)}>
                  {getStatusLabel(selectedLog.status)}
                </Tag>
              </Descriptions.Item>
              {selectedLog.scheduledTime && (
                <Descriptions.Item label="Scheduled Time" span={2}>
                  {dayjs.utc(selectedLog.scheduledTime).local().format('MMMM D, YYYY h:mm A')}
                </Descriptions.Item>
              )}
              <Descriptions.Item label="Caregiver" span={2}>
                {selectedLog.caregiverName}
              </Descriptions.Item>
            </Descriptions>

            {(selectedLog.prescribedBy || selectedLog.pharmacy || selectedLog.rxNumber) && (
              <Card title="Prescription Information" size="small">
                <Descriptions column={2} size="small">
                  {selectedLog.prescribedBy && (
                    <Descriptions.Item label="Prescribed By">
                      {selectedLog.prescribedBy}
                    </Descriptions.Item>
                  )}
                  {selectedLog.pharmacy && (
                    <Descriptions.Item label="Pharmacy">
                      {selectedLog.pharmacy}
                    </Descriptions.Item>
                  )}
                  {selectedLog.rxNumber && (
                    <Descriptions.Item label="Rx Number" span={2}>
                      {selectedLog.rxNumber}
                    </Descriptions.Item>
                  )}
                </Descriptions>
              </Card>
            )}

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
