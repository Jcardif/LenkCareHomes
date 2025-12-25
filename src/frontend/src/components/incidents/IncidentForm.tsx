'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Form,
  Input,
  Select,
  DatePicker,
  TimePicker,
  Button,
  Flex,
  Typography,
  Slider,
  message,
  Card,
  Grid,
  Row,
  Col,
} from 'antd';
import { SaveOutlined, SendOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { useRouter } from 'next/navigation';

import { incidentsApi, clientsApi, homesApi, ApiError } from '@/lib/api';
import type {
  CreateIncidentRequest,
  UpdateIncidentRequest,
  Incident,
  IncidentType,
  IncidentSeverity,
  ClientSummary,
  HomeSummary,
} from '@/types';

const { TextArea } = Input;
const { Paragraph, Text } = Typography;
const { useBreakpoint } = Grid;

interface IncidentFormProps {
  incidentId?: string;
  incident?: Incident;
  clientId?: string;
  homeId?: string;
  onSuccess?: (incident: Incident) => void;
  onCancel?: () => void;
}

const INCIDENT_TYPES: { label: string; value: IncidentType; description: string }[] = [
  { label: 'Fall', value: 'Fall', description: 'Client fell or slipped' },
  { label: 'Medication', value: 'Medication', description: 'Medication error or adverse reaction' },
  { label: 'Behavioral', value: 'Behavioral', description: 'Aggressive or disruptive behavior' },
  { label: 'Medical', value: 'Medical', description: 'Medical emergency or health concern' },
  { label: 'Injury', value: 'Injury', description: 'Physical injury from any cause' },
  { label: 'Elopement', value: 'Elopement', description: 'Client wandered or left unsupervised' },
  { label: 'Other', value: 'Other', description: 'Other incident type' },
];

const SEVERITY_MARKS: Record<number, { style: React.CSSProperties; label: string }> = {
  1: { style: { color: '#52c41a' }, label: '1 - Minor' },
  2: { style: { color: '#95de64' }, label: '2' },
  3: { style: { color: '#faad14' }, label: '3 - Moderate' },
  4: { style: { color: '#ff7a45' }, label: '4' },
  5: { style: { color: '#ff4d4f' }, label: '5 - Severe' },
};

export default function IncidentForm({ incident, clientId, onSuccess, onCancel }: IncidentFormProps) {
  const router = useRouter();
  const [form] = Form.useForm();
  const [homes, setHomes] = useState<HomeSummary[]>([]);
  const [clients, setClients] = useState<ClientSummary[]>([]);
  const [loadingHomes, setLoadingHomes] = useState(false);
  const [loadingClients, setLoadingClients] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitAction, setSubmitAction] = useState<'draft' | 'submit'>('draft');
  const [selectedHomeId, setSelectedHomeId] = useState<string | undefined>();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const isEditing = !!incident;
  const isDraft = !incident || incident.status === 'Draft';

  const fetchHomes = useCallback(async () => {
    try {
      setLoadingHomes(true);
      const data = await homesApi.getAll(false);
      setHomes(data);
    } catch {
      // Silently fail
    } finally {
      setLoadingHomes(false);
    }
  }, []);

  const fetchClients = useCallback(async (homeId?: string) => {
    try {
      setLoadingClients(true);
      const data = await clientsApi.getAll({ isActive: true, homeId });
      setClients(data);
    } catch {
      // Silently fail
    } finally {
      setLoadingClients(false);
    }
  }, []);

  useEffect(() => {
    if (!clientId) {
      void fetchHomes();
    }
  }, [clientId, fetchHomes]);

  // When a home is selected, fetch clients for that home
  useEffect(() => {
    if (selectedHomeId) {
      void fetchClients(selectedHomeId);
    } else {
      setClients([]);
    }
  }, [selectedHomeId, fetchClients]);

  useEffect(() => {
    if (incident) {
      setSelectedHomeId(incident.homeId);
      form.setFieldsValue({
        homeId: incident.homeId,
        clientId: incident.clientId,
        incidentType: incident.incidentType,
        severity: incident.severity,
        occurredDate: dayjs(incident.occurredAt),
        occurredTime: dayjs(incident.occurredAt),
        location: incident.location,
        description: incident.description,
        actionsTaken: incident.actionsTaken,
        witnessNames: incident.witnessNames,
      });
    } else if (clientId) {
      // If a clientId is provided, find the client's home
      void (async () => {
        try {
          const allClients = await clientsApi.getAll({ isActive: true });
          const client = allClients.find(c => c.id === clientId);
          if (client) {
            setSelectedHomeId(client.homeId);
            form.setFieldsValue({
              clientId: clientId,
              homeId: client.homeId,
              severity: 3,
              occurredDate: dayjs(),
              occurredTime: dayjs(),
            });
          }
        } catch {
          // Silently fail
        }
      })();
    } else {
      form.setFieldsValue({
        severity: 3,
        occurredDate: dayjs(),
        occurredTime: dayjs(),
      });
    }
  }, [incident, clientId, form]);

  const handleSubmit = async (values: Record<string, unknown>) => {
    try {
      setSubmitting(true);

      const occurredAt = (values.occurredDate as dayjs.Dayjs)
        .hour((values.occurredTime as dayjs.Dayjs).hour())
        .minute((values.occurredTime as dayjs.Dayjs).minute())
        .toISOString();

      if (isEditing) {
        // Update the existing draft
        const request: UpdateIncidentRequest = {
          incidentType: values.incidentType as IncidentType,
          severity: values.severity as IncidentSeverity,
          occurredAt,
          location: values.location as string,
          description: values.description as string,
          actionsTaken: values.actionsTaken as string | undefined,
          witnessNames: values.witnessNames as string | undefined,
        };

        const result = await incidentsApi.update(incident.id, request);
        if (!result.success) {
          void message.error(result.error || 'Failed to update incident');
          return;
        }

        // If user clicked "Submit", also submit the draft
        if (submitAction === 'submit') {
          const submitResult = await incidentsApi.submit(incident.id);
          if (submitResult.success && submitResult.incident) {
            void message.success('Incident submitted successfully');
            if (onSuccess) {
              onSuccess(submitResult.incident);
            } else {
              router.push(`/incidents/${incident.id}`);
            }
          } else {
            void message.error(submitResult.error || 'Failed to submit incident');
          }
        } else {
          if (result.incident) {
            void message.success('Incident draft saved');
            if (onSuccess) {
              onSuccess(result.incident);
            } else {
              router.push(`/incidents/${incident.id}`);
            }
          }
        }
      } else {
        // Determine homeId - use from form if available, otherwise from selectedHomeId state
        const homeId = (values.homeId as string) || selectedHomeId;
        
        if (!homeId) {
          void message.error('Home is required');
          return;
        }

        const request: CreateIncidentRequest = {
          homeId,
          clientId: clientId || (values.clientId as string | undefined),
          incidentType: values.incidentType as IncidentType,
          severity: values.severity as IncidentSeverity,
          occurredAt,
          location: values.location as string,
          description: values.description as string,
          actionsTaken: values.actionsTaken as string | undefined,
          witnessNames: values.witnessNames as string | undefined,
          submitImmediately: submitAction === 'submit',
        };

        const result = await incidentsApi.create(request);
        if (result.success && result.incident) {
          const msg = submitAction === 'submit'
            ? 'Incident submitted successfully'
            : 'Incident saved as draft';
          void message.success(msg);
          if (onSuccess) {
            onSuccess(result.incident);
          } else {
            router.push(`/incidents/${result.incident.id}`);
          }
        } else {
          void message.error(result.error || 'Failed to create incident');
        }
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to save incident';
      void message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Form
      form={form}
      layout="vertical"
      onFinish={(values) => void handleSubmit(values)}
      disabled={submitting}
    >
      {!clientId && (
        <>
          <Form.Item
            name="homeId"
            label="Home"
            rules={[{ required: true, message: 'Please select a home' }]}
          >
            <Select
              placeholder="Select a home"
              loading={loadingHomes}
              showSearch
              optionFilterProp="label"
              options={homes.map((h) => ({
                label: h.name,
                value: h.id,
              }))}
              onChange={(value) => {
                setSelectedHomeId(value);
                form.setFieldValue('clientId', undefined);
              }}
              disabled={isEditing}
            />
          </Form.Item>

          <Form.Item
            name="clientId"
            label="Client (Optional)"
            extra="Leave empty for home-level incidents not tied to a specific client"
          >
            <Select
              placeholder={selectedHomeId ? "Select a client (optional)" : "Select a home first"}
              loading={loadingClients}
              showSearch
              optionFilterProp="label"
              allowClear
              options={clients.map((c) => ({
                label: c.fullName,
                value: c.id,
              }))}
              disabled={isEditing || !selectedHomeId}
            />
          </Form.Item>
        </>
      )}

      <Form.Item
        name="incidentType"
        label="Incident Type"
        rules={[{ required: true, message: 'Please select an incident type' }]}
      >
        <Select
          placeholder="Select incident type"
          options={INCIDENT_TYPES.map((t) => ({
            label: (
              <div>
                <Text strong>{t.label}</Text>
                <br />
                <Text type="secondary" style={{ fontSize: 12 }}>{t.description}</Text>
              </div>
            ),
            value: t.value,
          }))}
          listHeight={300}
        />
      </Form.Item>

      <Form.Item
        name="severity"
        label="Severity Level"
        rules={[{ required: true, message: 'Please select severity' }]}
        extra={
          <Text type="secondary">
            1 = Minor (no injury), 5 = Severe (significant harm or hospitalization)
          </Text>
        }
      >
        <Slider
          min={1}
          max={5}
          marks={SEVERITY_MARKS}
          step={1}
          tooltip={{ formatter: (value) => SEVERITY_MARKS[value as number]?.label }}
        />
      </Form.Item>

      <Row gutter={16}>
        <Col xs={24} sm={12}>
          <Form.Item
            name="occurredDate"
            label="Date of Incident"
            rules={[{ required: true, message: 'Required' }]}
          >
            <DatePicker
              style={{ width: '100%' }}
              disabledDate={(current) => current && current > dayjs().endOf('day')}
              size={isMobile ? 'large' : 'middle'}
            />
          </Form.Item>
        </Col>
        <Col xs={24} sm={12}>
          <Form.Item
            name="occurredTime"
            label="Time of Incident"
            rules={[{ required: true, message: 'Required' }]}
          >
            <TimePicker format="h:mm A" style={{ width: '100%' }} use12Hours size={isMobile ? 'large' : 'middle'} />
          </Form.Item>
        </Col>
      </Row>

      <Form.Item
        name="location"
        label="Location"
        rules={[{ required: true, message: 'Please enter the location' }]}
      >
        <Input placeholder="e.g., Living room, Bathroom, Outside front porch" />
      </Form.Item>

      <Form.Item
        name="description"
        label="Description"
        rules={[
          { required: true, message: 'Please describe the incident' },
          { min: 20, message: 'Description must be at least 20 characters' },
        ]}
      >
        <TextArea
          rows={5}
          placeholder="Describe what happened in detail. Include what the client was doing before, during, and after the incident..."
          showCount
          maxLength={4000}
        />
      </Form.Item>

      <Form.Item
        name="actionsTaken"
        label="Actions Taken"
        extra="Describe immediate actions taken in response to the incident"
      >
        <TextArea
          rows={3}
          placeholder="e.g., Applied ice pack, called physician, monitored for 30 minutes..."
          showCount
          maxLength={2000}
        />
      </Form.Item>

      <Form.Item
        name="witnessNames"
        label="Witnesses"
        extra="Names of anyone who witnessed the incident (staff or visitors)"
      >
        <Input placeholder="e.g., Jane Smith (Staff), John Doe (Visitor)" />
      </Form.Item>

      <Card size="small" style={{ marginBottom: 24, backgroundColor: '#e6f7ff', borderColor: '#91caff' }}>
        <Paragraph style={{ margin: 0 }}>
          <Text strong>📷 Want to attach photos?</Text>
          <br />
          <Text>Save as draft first, then you can upload photos from the incident detail page.</Text>
        </Paragraph>
      </Card>

      <Card size="small" style={{ marginBottom: 24, backgroundColor: '#f6ffed', borderColor: '#b7eb8f' }}>
        <Paragraph style={{ margin: 0 }}>
          <Text strong>Tips for Documentation:</Text>
          <ul style={{ marginBottom: 0, paddingLeft: 20 }}>
            <li>Be objective and factual - avoid opinions or assumptions</li>
            <li>Include exact times and locations when known</li>
            <li>Document what you observed, not what you think happened</li>
            <li>Note any pre-existing conditions that may be relevant</li>
          </ul>
        </Paragraph>
      </Card>

      <Form.Item style={{ marginBottom: 0 }}>
        <Flex justify="end" gap={8} wrap="wrap">
          {onCancel && (
            <Button onClick={onCancel} style={{ minHeight: 44 }}>
              Cancel
            </Button>
          )}
          {isDraft && (
            <>
              <Button
                icon={<SaveOutlined />}
                onClick={() => {
                  setSubmitAction('draft');
                  form.submit();
                }}
                loading={submitting && submitAction === 'draft'}
                style={{ minHeight: 44 }}
              >
                {isSmallMobile ? '' : 'Save Draft'}
              </Button>
              <Button
                type="primary"
                icon={<SendOutlined />}
                onClick={() => {
                  setSubmitAction('submit');
                  form.submit();
                }}
                loading={submitting && submitAction === 'submit'}
                style={{ minHeight: 44 }}
              >
                {isEditing ? 'Submit' : 'Submit'}
              </Button>
            </>
          )}
          {!isDraft && (
            <Button
              type="primary"
              htmlType="submit"
              loading={submitting}
              style={{ minHeight: 44 }}
            >
              {isSmallMobile ? 'Update' : 'Update Incident'}
            </Button>
          )}
        </Flex>
      </Form.Item>
    </Form>
  );
}
