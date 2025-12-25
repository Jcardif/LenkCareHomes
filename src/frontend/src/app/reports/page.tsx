'use client';

import React, { useState, useEffect } from 'react';
import {
  Typography,
  Card,
  Form,
  Select,
  DatePicker,
  Button,
  Space,
  message,
  Alert,
  Spin,
  Divider,
  Grid,
} from 'antd';
import {
  FileTextOutlined,
  DownloadOutlined,
  HomeOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { homesApi, clientsApi, reportsApi, ApiError } from '@/lib/api';
import type { HomeSummary, ClientSummary, ReportType } from '@/types';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';

const { Title, Paragraph } = Typography;
const { RangePicker } = DatePicker;
const { useBreakpoint } = Grid;

interface ReportFormValues {
  reportType: ReportType;
  homeId?: string;
  clientId?: string;
  dateRange: [Dayjs, Dayjs];
}

function ReportsContent() {
  const [form] = Form.useForm<ReportFormValues>();
  const [homes, setHomes] = useState<HomeSummary[]>([]);
  const [clients, setClients] = useState<ClientSummary[]>([]);
  const [filteredClients, setFilteredClients] = useState<ClientSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [reportType, setReportType] = useState<ReportType>('Client');
  const [selectedHomeId, setSelectedHomeId] = useState<string | undefined>();
  const screens = useBreakpoint();
  const isMobile = !screens.md;

  // Load homes and clients on mount
  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        const [homesData, clientsData] = await Promise.all([
          homesApi.getAll(),
          clientsApi.getAll({ isActive: true }),
        ]);
        setHomes(homesData);
        setClients(clientsData);
        setFilteredClients(clientsData);
      } catch (error) {
        console.error('Failed to load data:', error);
        message.error('Failed to load homes and clients');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, []);

  // Filter clients when home selection changes
  useEffect(() => {
    if (selectedHomeId) {
      const filtered = clients.filter(c => c.homeId === selectedHomeId);
      setFilteredClients(filtered);
      // Clear client selection if it's no longer valid
      const currentClient = form.getFieldValue('clientId');
      if (currentClient && !filtered.some(c => c.id === currentClient)) {
        form.setFieldValue('clientId', undefined);
      }
    } else {
      setFilteredClients(clients);
    }
  }, [selectedHomeId, clients, form]);

  const handleReportTypeChange = (type: ReportType) => {
    setReportType(type);
    // Reset dependent fields
    form.setFieldsValue({
      homeId: undefined,
      clientId: undefined,
    });
    setSelectedHomeId(undefined);
  };

  const handleHomeChange = (homeId: string | undefined) => {
    setSelectedHomeId(homeId);
    if (reportType === 'Client') {
      form.setFieldValue('clientId', undefined);
    }
  };

  const handleGenerateReport = async (values: ReportFormValues) => {
    setGenerating(true);
    try {
      const [startDate, endDate] = values.dateRange;
      const startDateStr = startDate.startOf('day').toISOString();
      const endDateStr = endDate.endOf('day').toISOString();

      let blob: Blob;
      let fileName: string;

      if (values.reportType === 'Client') {
        if (!values.clientId) {
          message.error('Please select a client');
          return;
        }
        
        const client = clients.find(c => c.id === values.clientId);
        blob = await reportsApi.generateClientReport({
          clientId: values.clientId,
          startDate: startDateStr,
          endDate: endDateStr,
        });
        fileName = `ClientReport_${client?.fullName.replace(/\s+/g, '_') || 'Unknown'}_${startDate.format('YYYYMMDD')}_${endDate.format('YYYYMMDD')}.pdf`;
      } else {
        if (!values.homeId) {
          message.error('Please select a home');
          return;
        }

        const home = homes.find(h => h.id === values.homeId);
        blob = await reportsApi.generateHomeReport({
          homeId: values.homeId,
          startDate: startDateStr,
          endDate: endDateStr,
        });
        fileName = `HomeReport_${home?.name.replace(/\s+/g, '_') || 'Unknown'}_${startDate.format('YYYYMMDD')}_${endDate.format('YYYYMMDD')}.pdf`;
      }

      // Download the PDF
      reportsApi.downloadBlob(blob, fileName);
      message.success('Report generated successfully!');
    } catch (error) {
      console.error('Failed to generate report:', error);
      if (error instanceof ApiError) {
        message.error(error.message);
      } else {
        message.error('Failed to generate report. Please try again.');
      }
    } finally {
      setGenerating(false);
    }
  };

  // Default date range: last 30 days
  const defaultDateRange: [Dayjs, Dayjs] = [
    dayjs().subtract(30, 'days'),
    dayjs(),
  ];

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0, color: '#2d3732' }}>
          Reports
        </Title>
        <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
          Generate summary reports for clients or homes
        </Paragraph>
      </div>

      <Alert
        message="Confidential Reports"
        description="Reports contain Protected Health Information (PHI). Generated reports should be handled according to HIPAA guidelines and shared only with authorized personnel."
        type="warning"
        showIcon
        style={{ marginBottom: 24 }}
      />

      <Card>
        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}>
            <Spin size="large" />
            <div style={{ marginTop: 16 }}>Loading...</div>
          </div>
        ) : (
          <Form
            form={form}
            layout="vertical"
            onFinish={handleGenerateReport}
            initialValues={{
              reportType: 'Client',
              dateRange: defaultDateRange,
            }}
          >
            <Form.Item
              name="reportType"
              label="Report Type"
              rules={[{ required: true, message: 'Please select a report type' }]}
            >
              <Select
                placeholder="Select report type"
                onChange={handleReportTypeChange}
                options={[
                  {
                    value: 'Client',
                    label: (
                      <Space>
                        <UserOutlined />
                        Client Summary Report
                      </Space>
                    ),
                  },
                  {
                    value: 'Home',
                    label: (
                      <Space>
                        <HomeOutlined />
                        Home Summary Report
                      </Space>
                    ),
                  },
                ]}
              />
            </Form.Item>

            {reportType === 'Client' && (
              <>
                <Form.Item
                  name="homeId"
                  label="Filter by Home (Optional)"
                >
                  <Select
                    placeholder="All homes"
                    allowClear
                    onChange={handleHomeChange}
                    options={homes.map(h => ({
                      value: h.id,
                      label: h.name,
                    }))}
                  />
                </Form.Item>

                <Form.Item
                  name="clientId"
                  label="Select Client"
                  rules={[{ required: true, message: 'Please select a client' }]}
                >
                  <Select
                    placeholder="Select a client"
                    showSearch
                    optionFilterProp="label"
                    options={filteredClients.map(c => ({
                      value: c.id,
                      label: `${c.fullName} (${homes.find(h => h.id === c.homeId)?.name || 'Unknown Home'})`,
                    }))}
                    notFoundContent={
                      filteredClients.length === 0 ? 'No clients found' : null
                    }
                  />
                </Form.Item>
              </>
            )}

            {reportType === 'Home' && (
              <Form.Item
                name="homeId"
                label="Select Home"
                rules={[{ required: true, message: 'Please select a home' }]}
              >
                <Select
                  placeholder="Select a home"
                  showSearch
                  optionFilterProp="label"
                  options={homes.map(h => ({
                    value: h.id,
                    label: `${h.name} (${h.activeClients} clients)`,
                  }))}
                />
              </Form.Item>
            )}

            <Form.Item
              name="dateRange"
              label="Report Period"
              rules={[{ required: true, message: 'Please select a date range' }]}
            >
              <RangePicker
                style={{ width: '100%' }}
                disabledDate={(current) => current && current > dayjs().endOf('day')}
                size={isMobile ? 'large' : 'middle'}
                presets={[
                  { label: 'Last 7 Days', value: [dayjs().subtract(7, 'days'), dayjs()] },
                  { label: 'Last 30 Days', value: [dayjs().subtract(30, 'days'), dayjs()] },
                  { label: 'Last 90 Days', value: [dayjs().subtract(90, 'days'), dayjs()] },
                  { label: 'This Month', value: [dayjs().startOf('month'), dayjs()] },
                  { label: 'Last Month', value: [dayjs().subtract(1, 'month').startOf('month'), dayjs().subtract(1, 'month').endOf('month')] },
                  { label: 'This Year', value: [dayjs().startOf('year'), dayjs()] },
                ]}
              />
            </Form.Item>

            <Divider />

            <Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                icon={<DownloadOutlined />}
                loading={generating}
                size="large"
                block={isMobile}
                style={{ backgroundColor: '#5a7a6b', borderColor: '#5a7a6b', minHeight: 44 }}
              >
                {generating ? 'Generating Report...' : 'Generate & Download PDF'}
              </Button>
            </Form.Item>
          </Form>
        )}
      </Card>

      <Card style={{ marginTop: 24 }}>
        <Title level={4} style={{ color: '#2d3732' }}>
          <FileTextOutlined style={{ marginRight: 8 }} />
          Report Information
        </Title>
        <Divider />
        
        <Title level={5}>Client Summary Report</Title>
        <Paragraph>
          Includes comprehensive care data for a single client:
        </Paragraph>
        <ul style={{ color: '#6b7770' }}>
          <li>Client demographic information</li>
          <li>Summary statistics (record counts, averages)</li>
          <li>ADL logs with Katz scores</li>
          <li>Vital signs history</li>
          <li>ROM exercises log</li>
          <li>Behavior notes</li>
          <li>Activities participated in</li>
          <li>Incident reports</li>
        </ul>

        <Title level={5} style={{ marginTop: 16 }}>Home Summary Report</Title>
        <Paragraph>
          Provides an overview of all activity at a home:
        </Paragraph>
        <ul style={{ color: '#6b7770' }}>
          <li>Home information</li>
          <li>Overall statistics across all clients</li>
          <li>Client activity summary table</li>
          <li>All activities at the home</li>
          <li>All incident reports for the home</li>
          <li>Incidents grouped by type</li>
        </ul>

        <Alert
          message="Note"
          description="Reports may take a few seconds to generate for longer date ranges. The PDF will automatically download when ready."
          type="info"
          showIcon
          style={{ marginTop: 16 }}
        />
      </Card>
    </div>
  );
}

export default function ReportsPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin']}>
      <AuthenticatedLayout>
        <ReportsContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
