'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Form,
  Input,
  Select,
  DatePicker,
  InputNumber,
  Button,
  Card,
  Space,
  message,
  Typography,
  Row,
  Col,
  Grid,
  Spin,
} from 'antd';
import { SaveOutlined, CloseOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import dayjs from 'dayjs';

import { appointmentsApi, clientsApi, ApiError } from '@/lib/api';
import { APPOINTMENT_TYPES } from './AppointmentList';
import type {
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
  Appointment,
  AppointmentType,
  ClientSummary,
} from '@/types';

const { Title } = Typography;
const { TextArea } = Input;
const { useBreakpoint } = Grid;

interface AppointmentFormProps {
  appointmentId?: string;
  clientId?: string;
  appointment?: Appointment;
  mode?: 'create' | 'edit';
  onSuccess?: () => void;
  onCancel?: () => void;
}

interface FormValues {
  clientId: string;
  appointmentType: AppointmentType;
  title: string;
  scheduledAt: dayjs.Dayjs;
  durationMinutes: number;
  location?: string;
  providerName?: string;
  providerPhone?: string;
  notes?: string;
  transportationNotes?: string;
}

export default function AppointmentForm({
  appointmentId,
  clientId: initialClientId,
  appointment: passedAppointment,
  mode,
  onSuccess,
  onCancel,
}: AppointmentFormProps) {
  const router = useRouter();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const [form] = Form.useForm<FormValues>();

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [clients, setClients] = useState<ClientSummary[]>([]);
  const [, setExistingAppointment] = useState<Appointment | null>(passedAppointment ?? null);

  const isEditing = mode === 'edit' || !!appointmentId || !!passedAppointment;
  const effectiveAppointmentId = appointmentId ?? passedAppointment?.id;

  const fetchClients = useCallback(async () => {
    try {
      const data = await clientsApi.getAll({ isActive: true });
      setClients(data);
    } catch {
      // Silent fail
    }
  }, []);

  const fetchAppointment = useCallback(async () => {
    // Don't fetch if we already have an appointment passed in
    if (passedAppointment || !appointmentId) return;
    try {
      setLoading(true);
      const data = await appointmentsApi.getById(appointmentId);
      setExistingAppointment(data);
      form.setFieldsValue({
        clientId: data.clientId,
        appointmentType: data.appointmentType,
        title: data.title,
        scheduledAt: dayjs(data.scheduledAt),
        durationMinutes: data.durationMinutes,
        location: data.location,
        providerName: data.providerName,
        providerPhone: data.providerPhone,
        notes: data.notes,
        transportationNotes: data.transportationNotes,
      });
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load appointment';
      void message.error(msg);
      router.push('/appointments');
    } finally {
      setLoading(false);
    }
  }, [appointmentId, passedAppointment, form, router]);

  // Populate form from passed appointment
  useEffect(() => {
    if (passedAppointment) {
      form.setFieldsValue({
        clientId: passedAppointment.clientId,
        appointmentType: passedAppointment.appointmentType,
        title: passedAppointment.title,
        scheduledAt: dayjs(passedAppointment.scheduledAt),
        durationMinutes: passedAppointment.durationMinutes,
        location: passedAppointment.location,
        providerName: passedAppointment.providerName,
        providerPhone: passedAppointment.providerPhone,
        notes: passedAppointment.notes,
        transportationNotes: passedAppointment.transportationNotes,
      });
    }
  }, [passedAppointment, form]);

  useEffect(() => {
    void fetchClients();
    if (appointmentId && !passedAppointment) {
      void fetchAppointment();
    } else if (initialClientId) {
      form.setFieldValue('clientId', initialClientId);
    }
  }, [fetchClients, fetchAppointment, appointmentId, passedAppointment, initialClientId, form]);

  const handleSubmit = async (values: FormValues) => {
    try {
      setSaving(true);

      if (isEditing) {
        const updateRequest: UpdateAppointmentRequest = {
          appointmentType: values.appointmentType,
          title: values.title,
          scheduledAt: values.scheduledAt.toISOString(),
          durationMinutes: values.durationMinutes,
          location: values.location,
          providerName: values.providerName,
          providerPhone: values.providerPhone,
          notes: values.notes,
          transportationNotes: values.transportationNotes,
        };

        const result = await appointmentsApi.update(effectiveAppointmentId!, updateRequest);
        if (result.success) {
          void message.success('Appointment updated successfully');
          if (onSuccess) {
            onSuccess();
          } else {
            router.push(`/appointments/${effectiveAppointmentId}`);
          }
        } else {
          void message.error(result.error || 'Failed to update appointment');
        }
      } else {
        const createRequest: CreateAppointmentRequest = {
          clientId: values.clientId,
          appointmentType: values.appointmentType,
          title: values.title,
          scheduledAt: values.scheduledAt.toISOString(),
          durationMinutes: values.durationMinutes,
          location: values.location,
          providerName: values.providerName,
          providerPhone: values.providerPhone,
          notes: values.notes,
          transportationNotes: values.transportationNotes,
        };

        const result = await appointmentsApi.create(createRequest);
        if (result.success && result.appointment) {
          void message.success('Appointment created successfully');
          if (onSuccess) {
            onSuccess();
          } else {
            router.push(`/appointments/${result.appointment.id}`);
          }
        } else {
          void message.error(result.error || 'Failed to create appointment');
        }
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to save appointment';
      void message.error(msg);
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    if (onCancel) {
      onCancel();
    } else {
      router.back();
    }
  };

  if (loading) {
    return (
      <Card>
        <div style={{ textAlign: 'center', padding: 48 }}>
          <Spin size="large" />
          <div style={{ marginTop: 16 }}>Loading appointment...</div>
        </div>
      </Card>
    );
  }

  return (
    <Card>
      <Title level={4} style={{ marginBottom: 24 }}>
        {isEditing ? 'Edit Appointment' : 'Schedule New Appointment'}
      </Title>

      <Form
        form={form}
        layout="vertical"
        onFinish={(values) => void handleSubmit(values)}
        initialValues={{
          durationMinutes: 30,
          scheduledAt: dayjs().add(1, 'day').hour(9).minute(0),
        }}
      >
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item
              name="clientId"
              label="Client"
              rules={[{ required: true, message: 'Please select a client' }]}
            >
              <Select
                placeholder="Select client"
                showSearch
                optionFilterProp="children"
                disabled={isEditing}
                options={clients.map((c) => ({
                  label: `${c.fullName} (${c.homeName})`,
                  value: c.id,
                }))}
                filterOption={(input, option) =>
                  (option?.label as string)?.toLowerCase().includes(input.toLowerCase())
                }
              />
            </Form.Item>
          </Col>

          <Col xs={24} md={12}>
            <Form.Item
              name="appointmentType"
              label="Appointment Type"
              rules={[{ required: true, message: 'Please select appointment type' }]}
            >
              <Select
                placeholder="Select type"
                options={APPOINTMENT_TYPES.map((t) => ({
                  label: t.label,
                  value: t.value,
                }))}
              />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item
          name="title"
          label="Title"
          rules={[{ required: true, message: 'Please enter a title' }]}
        >
          <Input placeholder="e.g., Annual physical checkup with Dr. Smith" />
        </Form.Item>

        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item
              name="scheduledAt"
              label="Date & Time"
              rules={[{ required: true, message: 'Please select date and time' }]}
            >
              <DatePicker
                showTime={{ format: 'h:mm A', use12Hours: true }}
                format="MMMM D, YYYY h:mm A"
                style={{ width: '100%' }}
                placeholder="Select date and time"
              />
            </Form.Item>
          </Col>

          <Col xs={24} md={12}>
            <Form.Item
              name="durationMinutes"
              label="Duration (minutes)"
              rules={[{ required: true, message: 'Please enter duration' }]}
            >
              <InputNumber
                min={15}
                max={480}
                step={15}
                style={{ width: '100%' }}
                placeholder="30"
              />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="location" label="Location">
          <Input placeholder="e.g., Dr. Smith's Office, 123 Medical Plaza, Suite 456" />
        </Form.Item>

        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item name="providerName" label="Provider Name">
              <Input placeholder="e.g., Dr. John Smith" />
            </Form.Item>
          </Col>

          <Col xs={24} md={12}>
            <Form.Item name="providerPhone" label="Provider Phone">
              <Input placeholder="e.g., (555) 123-4567" />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="transportationNotes" label="Transportation Notes">
          <TextArea
            rows={2}
            placeholder="e.g., Client requires wheelchair accessible transport, pickup at 8:30 AM"
          />
        </Form.Item>

        <Form.Item name="notes" label="Notes">
          <TextArea
            rows={3}
            placeholder="Any additional notes about the appointment..."
          />
        </Form.Item>

        <Form.Item style={{ marginBottom: 0, marginTop: 24 }}>
          <Space style={{ width: '100%', justifyContent: isMobile ? 'center' : 'flex-end' }}>
            <Button onClick={handleCancel} icon={<CloseOutlined />}>
              Cancel
            </Button>
            <Button type="primary" htmlType="submit" loading={saving} icon={<SaveOutlined />}>
              {isEditing ? 'Save Changes' : 'Schedule Appointment'}
            </Button>
          </Space>
        </Form.Item>
      </Form>
    </Card>
  );
}
