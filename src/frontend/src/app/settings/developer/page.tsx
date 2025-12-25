'use client';

import React, { useEffect, useState, useCallback, useRef } from 'react';
import { 
  Typography, 
  Card, 
  Button, 
  Alert, 
  Statistic, 
  Row, 
  Col, 
  Space, 
  Modal,
  Spin,
  Result,
  Divider,
  Tag,
  Progress,
  Steps,
  Grid,
} from 'antd';
import { 
  DatabaseOutlined, 
  CloudUploadOutlined, 
  DeleteOutlined,
  ExclamationCircleOutlined,
  CheckCircleOutlined,
  WarningOutlined,
  HomeOutlined,
  UserOutlined,
  TeamOutlined,
  MedicineBoxOutlined,
  FileTextOutlined,
  LoadingOutlined,
  CloseCircleOutlined,
  CalendarOutlined,
} from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { useAuth } from '@/contexts/AuthContext';
import { 
  syntheticDataApi, 
  type SyntheticDataAvailability, 
  type DataStatistics,
  type LoadSyntheticDataResult,
  type ClearDataResult,
  type LoadProgressUpdate,
  ApiError,
} from '@/lib/api';

const { Title, Paragraph, Text } = Typography;
const { useBreakpoint } = Grid;

// Phase descriptions for display
const phaseDescriptions: Record<string, { icon: React.ReactNode; description: string }> = {
  Initialization: { icon: <DatabaseOutlined />, description: 'Preparing data files' },
  Parsing: { icon: <FileTextOutlined />, description: 'Reading JSON data' },
  Homes: { icon: <HomeOutlined />, description: 'Creating adult family homes' },
  Beds: { icon: <HomeOutlined />, description: 'Setting up beds' },
  Users: { icon: <UserOutlined />, description: 'Creating caregiver accounts' },
  Assignments: { icon: <TeamOutlined />, description: 'Assigning caregivers to homes' },
  Clients: { icon: <TeamOutlined />, description: 'Adding residents' },
  'ADL Logs': { icon: <MedicineBoxOutlined />, description: 'Loading ADL records' },
  'Vitals Logs': { icon: <MedicineBoxOutlined />, description: 'Loading vitals records' },
  'Medication Logs': { icon: <MedicineBoxOutlined />, description: 'Loading medication records' },
  'ROM Logs': { icon: <MedicineBoxOutlined />, description: 'Loading ROM exercise records' },
  'Behavior Notes': { icon: <FileTextOutlined />, description: 'Loading behavior notes' },
  Activities: { icon: <FileTextOutlined />, description: 'Loading activities' },
  Incidents: { icon: <ExclamationCircleOutlined />, description: 'Loading incident reports' },
  'Incident Photos': { icon: <FileTextOutlined />, description: 'Uploading incident photos' },
  Documents: { icon: <CloudUploadOutlined />, description: 'Uploading PDF documents' },
  Appointments: { icon: <CalendarOutlined />, description: 'Loading appointments' },
  Complete: { icon: <CheckCircleOutlined />, description: 'All data loaded successfully' },
  Error: { icon: <CloseCircleOutlined />, description: 'An error occurred' },
};

function DeveloperToolsContent() {
  const router = useRouter();
  const { hasRole } = useAuth();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;
  
  const [availability, setAvailability] = useState<SyntheticDataAvailability | null>(null);
  const [statistics, setStatistics] = useState<DataStatistics | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingData, setLoadingData] = useState(false);
  const [clearingData, setClearingData] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loadResult, setLoadResult] = useState<LoadSyntheticDataResult | null>(null);
  const [clearResult, setClearResult] = useState<ClearDataResult | null>(null);
  
  // Progress modal state
  const [progressModalOpen, setProgressModalOpen] = useState(false);
  const [currentProgress, setCurrentProgress] = useState<LoadProgressUpdate | null>(null);
  const [progressOperation, setProgressOperation] = useState<'load' | 'clear'>('load');
  const abortControllerRef = useRef<(() => void) | null>(null);

  // Check if user has Sysadmin role
  const isSysadmin = hasRole('Sysadmin');

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      // Handle availability check gracefully - 404 means the feature is not available in this environment
      const availabilityData = await syntheticDataApi.checkAvailability().catch(() => {
        // If we get 404 or any error, the feature is not available
        // This is expected in production/staging environments
        return { 
          isAvailable: false, 
          message: 'Synthetic data operations are not available in this environment.' 
        } as SyntheticDataAvailability;
      });

      // Only fetch statistics if available
      const statsData = availabilityData.isAvailable 
        ? await syntheticDataApi.getStatistics().catch(() => null)
        : null;

      setAvailability(availabilityData);
      setStatistics(statsData);
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to fetch data';
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isSysadmin) {
      fetchData();
    }
  }, [isSysadmin, fetchData]);

  const handleLoadData = async () => {
    Modal.confirm({
      title: 'Load Synthetic Data',
      icon: <ExclamationCircleOutlined />,
      content: (
        <div>
          <Paragraph>
            This will load synthetic test data into the database. This includes:
          </Paragraph>
          <ul>
            <li>6 adult family homes (opening organically over 2 years)</li>
            <li>Multiple caregivers per home</li>
            <li>Residents with realistic health profiles</li>
            <li>Care logs, activities, and incidents</li>
            <li>PDF documents for each client</li>
          </ul>
          <Alert
            type="info"
            message="You'll see real-time progress as data is loaded."
            style={{ marginTop: 16 }}
          />
        </div>
      ),
      okText: 'Load Data',
      cancelText: 'Cancel',
      onOk: () => {
        setLoadingData(true);
        setLoadResult(null);
        setError(null);
        setProgressModalOpen(true);
        setProgressOperation('load');
        setCurrentProgress({
          phase: 'Starting',
          message: 'Connecting to server...',
          currentStep: 0,
          totalSteps: 15,
          percentComplete: 0,
          itemsProcessed: 0,
          isComplete: false,
          isError: false,
        });

        const abort = syntheticDataApi.loadDataWithProgress(
          // On progress
          (progress) => {
            setCurrentProgress(progress);
          },
          // On complete
          async (result) => {
            setLoadResult(result);
            setLoadingData(false);
            // Keep modal open briefly to show completion
            setTimeout(() => {
              setProgressModalOpen(false);
              setCurrentProgress(null);
            }, 2000);

            // Refresh statistics
            try {
              const stats = await syntheticDataApi.getStatistics();
              setStatistics(stats);
            } catch {
              // Ignore stats refresh errors
            }
          },
          // On error
          (errorMessage) => {
            setError(errorMessage);
            setLoadingData(false);
            setCurrentProgress(prev => prev ? {
              ...prev,
              isError: true,
              errorMessage,
              phase: 'Error',
              message: errorMessage,
            } : null);
            // Keep modal open to show error
          }
        );

        abortControllerRef.current = abort;
      },
    });
  };

  const handleCancelLoad = () => {
    if (abortControllerRef.current) {
      abortControllerRef.current();
      abortControllerRef.current = null;
    }
    setProgressModalOpen(false);
    setLoadingData(false);
    setCurrentProgress(null);
  };

  const handleClearData = async () => {
    Modal.confirm({
      title: 'Clear All Data',
      icon: <ExclamationCircleOutlined style={{ color: '#ff4d4f' }} />,
      content: (
        <div>
          <Alert
            type="error"
            message="Destructive Operation"
            description="This will permanently delete ALL data from the database, blob storage, and audit logs except your user account. This action cannot be undone!"
            style={{ marginBottom: 16 }}
          />
          <Paragraph>
            The following will be deleted:
          </Paragraph>
          <ul>
            <li>All homes and beds</li>
            <li>All clients and their records</li>
            <li>All care logs (ADLs, vitals, medications, etc.)</li>
            <li>All activities and incidents</li>
            <li>All documents (from Azure Blob Storage)</li>
            <li>All audit logs (from Cosmos DB)</li>
            <li>All caregivers (except you)</li>
          </ul>
        </div>
      ),
      okText: 'Clear All Data',
      okButtonProps: { danger: true },
      cancelText: 'Cancel',
      onOk: () => {
        setClearingData(true);
        setClearResult(null);
        setError(null);
        setProgressModalOpen(true);
        setProgressOperation('clear');
        setCurrentProgress({
          phase: 'Starting',
          message: 'Preparing to clear data...',
          currentStep: 0,
          totalSteps: 8,
          percentComplete: 0,
          itemsProcessed: 0,
          isComplete: false,
          isError: false,
        });

        const abort = syntheticDataApi.clearDataWithProgress(
          // On progress
          (progress) => {
            setCurrentProgress(progress);
          },
          // On complete
          async (result) => {
            setClearResult(result);
            setClearingData(false);
            // Keep modal open briefly to show completion
            setTimeout(() => {
              setProgressModalOpen(false);
              setCurrentProgress(null);
            }, 2000);

            // Refresh statistics
            try {
              const stats = await syntheticDataApi.getStatistics();
              setStatistics(stats);
            } catch {
              // Ignore stats refresh errors
            }
          },
          // On error
          (errorMessage) => {
            setError(errorMessage);
            setClearingData(false);
            setCurrentProgress(prev => prev ? {
              ...prev,
              isError: true,
              errorMessage,
              phase: 'Error',
              message: errorMessage,
            } : null);
          }
        );

        abortControllerRef.current = abort;
      },
    });
  };

  // Redirect if not Sysadmin
  if (!isSysadmin) {
    return (
      <Result
        status="403"
        title="Access Denied"
        subTitle="Developer tools are only available to System Administrators."
        extra={
          <Button type="primary" onClick={() => router.push('/settings')}>
            Back to Settings
          </Button>
        }
      />
    );
  }

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: 48 }}>
        <Spin size="large" />
        <Paragraph style={{ marginTop: 16 }}>Loading developer tools...</Paragraph>
      </div>
    );
  }

  const isAvailable = availability?.isAvailable ?? false;

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <Button 
          type="link" 
          onClick={() => router.push('/settings')}
          style={{ padding: 0, marginBottom: 8, minHeight: 44 }}
        >
          ← Back to Settings
        </Button>
        <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>
          Developer Tools
        </Title>
        {!isSmallMobile && (
          <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
            System administration and synthetic data management
          </Paragraph>
        )}
      </div>

      {/* Environment Status */}
      <Card style={{ marginBottom: 24 }}>
        <Space orientation="vertical" style={{ width: '100%' }}>
          <Space>
            {isAvailable ? (
              <Tag color="success" icon={<CheckCircleOutlined />}>
                Development Environment
              </Tag>
            ) : (
              <Tag color="error" icon={<WarningOutlined />}>
                Production Environment
              </Tag>
            )}
          </Space>
          <Text type="secondary">
            {availability?.message || 'Checking environment...'}
          </Text>
        </Space>
      </Card>

      {error && (
        <Alert
          type="error"
          message="Error"
          description={error}
          closable
          onClose={() => setError(null)}
          style={{ marginBottom: 24 }}
        />
      )}

      {loadResult && (
        <Alert
          type={loadResult.success ? 'success' : 'error'}
          message={loadResult.success ? 'Data Loaded Successfully' : 'Load Failed'}
          description={
            loadResult.success ? (
              <div>
                <Paragraph style={{ marginBottom: 8 }}>
                  Loaded in {loadResult.duration}:
                </Paragraph>
                <ul style={{ marginBottom: 0 }}>
                  <li>{loadResult.homesLoaded} homes, {loadResult.bedsLoaded} beds</li>
                  <li>{loadResult.usersLoaded} users</li>
                  <li>{loadResult.clientsLoaded} clients</li>
                  <li>{loadResult.careLogsLoaded.toLocaleString()} care logs</li>
                  <li>{loadResult.activitiesLoaded} activities</li>
                  <li>{loadResult.incidentsLoaded} incidents</li>
                </ul>
              </div>
            ) : loadResult.error
          }
          closable
          onClose={() => setLoadResult(null)}
          style={{ marginBottom: 24 }}
        />
      )}

      {clearResult && (
        <Alert
          type={clearResult.success ? 'success' : 'error'}
          message={clearResult.success ? 'Data Cleared' : 'Clear Failed'}
          description={
            clearResult.success
              ? `Successfully deleted ${clearResult.recordsDeleted.toLocaleString()} records.${clearResult.details ? ` (${clearResult.details})` : ''}`
              : clearResult.error
          }
          closable
          onClose={() => setClearResult(null)}
          style={{ marginBottom: 24 }}
        />
      )}

      {/* Database Statistics */}
      {statistics && (
        <Card 
          title={
            <Space>
              <DatabaseOutlined />
              <span>Database Statistics</span>
            </Space>
          }
          style={{ marginBottom: 24 }}
        >
          <Row gutter={[16, 16]}>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Homes"
                value={statistics.homeCount}
                prefix={<HomeOutlined />}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Beds"
                value={statistics.bedCount}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Users"
                value={statistics.userCount}
                prefix={<UserOutlined />}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Active Clients"
                value={statistics.activeClientCount}
                suffix={`/ ${statistics.clientCount}`}
                prefix={<TeamOutlined />}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="ADL Logs"
                value={statistics.adlLogCount}
                prefix={<MedicineBoxOutlined />}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Vitals Logs"
                value={statistics.vitalsLogCount}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Medication Logs"
                value={statistics.medicationLogCount}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Incidents"
                value={statistics.incidentCount}
                prefix={<FileTextOutlined />}
              />
            </Col>
          </Row>
          <Divider />
          <Row gutter={[16, 16]}>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="ROM Logs"
                value={statistics.romLogCount}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Behavior Notes"
                value={statistics.behaviorNoteCount}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Activities"
                value={statistics.activityCount}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Documents"
                value={statistics.documentCount}
                prefix={<FileTextOutlined />}
              />
            </Col>
            <Col xs={12} sm={8} md={6}>
              <Statistic
                title="Appointments"
                value={statistics.appointmentCount}
                prefix={<CalendarOutlined />}
              />
            </Col>
          </Row>
        </Card>
      )}

      {/* Synthetic Data Operations */}
      <Card 
        title={
          <Space>
            <CloudUploadOutlined />
            <span>Synthetic Data Operations</span>
          </Space>
        }
      >
        {!isAvailable ? (
          <Alert
            type="warning"
            message="Not Available"
            description="Synthetic data operations are disabled in production environment for data safety."
            showIcon
          />
        ) : (
          <Space direction="vertical" style={{ width: '100%' }} size="large">
            <div>
              <Title level={5}>Load Synthetic Data</Title>
              <Paragraph type="secondary">
                Generate and load realistic test data including homes, clients, caregivers,
                and 2 years of care logs. This is useful for testing and demos.
              </Paragraph>
              <Button
                type="primary"
                icon={<CloudUploadOutlined />}
                onClick={handleLoadData}
                loading={loadingData}
                disabled={clearingData}
              >
                Load Synthetic Data
              </Button>
            </div>

            <Divider />

            <div>
              <Title level={5} style={{ color: '#ff4d4f' }}>
                Clear All Data
              </Title>
              <Paragraph type="secondary">
                Remove all data from the database. This is a destructive operation
                and cannot be undone. Your user account will be preserved.
              </Paragraph>
              <Button
                danger
                icon={<DeleteOutlined />}
                onClick={handleClearData}
                loading={clearingData}
                disabled={loadingData}
              >
                Clear All Data
              </Button>
            </div>
          </Space>
        )}
      </Card>

      {/* Progress Modal */}
      <Modal
        title={
          <Space>
            {currentProgress?.isComplete ? (
              <CheckCircleOutlined style={{ color: '#52c41a' }} />
            ) : currentProgress?.isError ? (
              <CloseCircleOutlined style={{ color: '#ff4d4f' }} />
            ) : (
              <LoadingOutlined spin style={{ color: progressOperation === 'clear' ? '#ff4d4f' : '#1890ff' }} />
            )}
            <span>
              {currentProgress?.isComplete
                ? progressOperation === 'clear' ? 'Data Cleared' : 'Load Complete'
                : currentProgress?.isError
                ? progressOperation === 'clear' ? 'Clear Failed' : 'Load Failed'
                : progressOperation === 'clear' ? 'Clearing Data...' : 'Loading Synthetic Data...'}
            </span>
          </Space>
        }
        open={progressModalOpen}
        closable={currentProgress?.isComplete || currentProgress?.isError}
        onCancel={handleCancelLoad}
        footer={
          currentProgress?.isComplete ? (
            <Button type="primary" onClick={() => {
              setProgressModalOpen(false);
              setCurrentProgress(null);
            }} style={{ minWidth: 44, minHeight: 44 }}>
              Done
            </Button>
          ) : currentProgress?.isError ? (
            <Button onClick={handleCancelLoad} style={{ minWidth: 44, minHeight: 44 }}>
              Close
            </Button>
          ) : (
            <Button onClick={handleCancelLoad} danger style={{ minWidth: 44, minHeight: 44 }}>
              Cancel
            </Button>
          )
        }
        maskClosable={false}
        width={isMobile ? '100%' : 600}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        {currentProgress && (
          <div style={{ padding: '16px 0' }}>
            {/* Progress bar */}
            <Progress
              percent={currentProgress.percentComplete}
              status={
                currentProgress.isComplete
                  ? 'success'
                  : currentProgress.isError
                  ? 'exception'
                  : 'active'
              }
              strokeColor={currentProgress.isError ? '#ff4d4f' : progressOperation === 'clear' ? '#ff4d4f' : undefined}
              style={{ marginBottom: 24 }}
            />

            {/* Current phase info */}
            <Card size="small" style={{ marginBottom: 16 }}>
              <Space direction="vertical" style={{ width: '100%' }}>
                <Space>
                  {phaseDescriptions[currentProgress.phase]?.icon || <DatabaseOutlined />}
                  <Text strong style={{ fontSize: 16 }}>
                    {currentProgress.phase}
                  </Text>
                  <Tag color={currentProgress.isComplete ? 'success' : currentProgress.isError ? 'error' : 'processing'}>
                    Step {currentProgress.currentStep} of {currentProgress.totalSteps}
                  </Tag>
                </Space>
                <Text type="secondary">
                  {currentProgress.message}
                </Text>
                {currentProgress.itemsProcessed > 0 && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {progressOperation === 'clear' ? 'Records deleted' : 'Items processed'}: {currentProgress.itemsProcessed.toLocaleString()}
                  </Text>
                )}
              </Space>
            </Card>

            {/* Error message */}
            {currentProgress.isError && currentProgress.errorMessage && (
              <Alert
                type="error"
                message="Error Details"
                description={currentProgress.errorMessage}
                style={{ marginBottom: 16 }}
              />
            )}

            {/* Steps indicator - different for load vs clear */}
            {progressOperation === 'load' ? (
              <Steps
                direction="vertical"
                size="small"
                current={
                  currentProgress.currentStep <= 2 ? 0 :
                  currentProgress.currentStep <= 4 ? 1 :
                  currentProgress.currentStep <= 7 ? 2 :
                  currentProgress.currentStep <= 14 ? 3 : 4
                }
                status={currentProgress.isError ? 'error' : currentProgress.isComplete ? 'finish' : 'process'}
                items={[
                  { title: 'Initialize', description: 'Locate and parse data files' },
                  { title: 'Foundation', description: 'Homes, beds, users' },
                  { title: 'Clients', description: 'Residents and assignments' },
                  { title: 'Care Logs', description: 'ADLs, vitals, medications' },
                  { title: 'Documents', description: 'Upload PDFs to storage' },
                ]}
              />
            ) : (
              <Steps
                direction="vertical"
                size="small"
                current={
                  currentProgress.currentStep <= 2 ? 0 :
                  currentProgress.currentStep <= 4 ? 1 :
                  currentProgress.currentStep <= 6 ? 2 : 3
                }
                status={currentProgress.isError ? 'error' : currentProgress.isComplete ? 'finish' : 'process'}
                items={[
                  { title: 'Care Logs', description: 'Delete ADLs, vitals, medications, etc.' },
                  { title: 'Clients & Docs', description: 'Remove clients and documents' },
                  { title: 'Foundation', description: 'Delete homes, beds, users' },
                  { title: 'Storage', description: 'Clear blob storage and audit logs' },
                ]}
              />
            )}
          </div>
        )}
      </Modal>
    </div>
  );
}

export default function DeveloperToolsPage() {
  return (
    <ProtectedRoute requiredRoles={['Sysadmin']}>
      <AuthenticatedLayout>
        <DeveloperToolsContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
