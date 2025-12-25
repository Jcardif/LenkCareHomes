'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Table,
  Card,
  Button,
  Space,
  Tag,
  Typography,
  Select,
  Flex,
  Empty,
  Popconfirm,
  message,
  Tooltip,
  Modal,
  Checkbox,
  Timeline,
  Spin,
} from 'antd';
import {
  PlusOutlined,
  ReloadOutlined,
  FileOutlined,
  FilePdfOutlined,
  FileImageOutlined,
  FileWordOutlined,
  DownloadOutlined,
  DeleteOutlined,
  LockOutlined,
  TeamOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  HistoryOutlined,
  DownOutlined,
  RightOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import type { ColumnsType } from 'antd/es/table';

import { documentsApi, caregiversApi, ApiError } from '@/lib/api';
import type { DocumentSummary, DocumentType, CaregiverSummary, DocumentAccessHistory } from '@/types';
import { useAuth } from '@/contexts/AuthContext';
import DocumentViewer from './DocumentViewer';

const { Text, Title } = Typography;

interface DocumentListProps {
  clientId: string;
  homeId?: string;
  onUploadClick?: () => void;
  onRefresh?: () => void;
}

const DOCUMENT_TYPES: { label: string; value: DocumentType; color: string }[] = [
  { label: 'Care Plan', value: 'CarePlan', color: 'blue' },
  { label: 'Medical', value: 'Medical', color: 'red' },
  { label: 'Legal', value: 'Legal', color: 'purple' },
  { label: 'Financial', value: 'Financial', color: 'green' },
  { label: 'Photo', value: 'Photo', color: 'orange' },
  { label: 'Assessment', value: 'Assessment', color: 'cyan' },
  { label: 'Other', value: 'Other', color: 'default' },
];

function getTypeTag(type: DocumentType) {
  const t = DOCUMENT_TYPES.find((x) => x.value === type);
  return <Tag color={t?.color}>{t?.label || type}</Tag>;
}

function getFileIcon(fileName: string) {
  const ext = fileName.split('.').pop()?.toLowerCase();
  if (ext === 'pdf') return <FilePdfOutlined style={{ color: '#ff4d4f' }} />;
  if (['jpg', 'jpeg', 'png', 'gif'].includes(ext || '')) return <FileImageOutlined style={{ color: '#1890ff' }} />;
  if (['doc', 'docx'].includes(ext || '')) return <FileWordOutlined style={{ color: '#2f54eb' }} />;
  return <FileOutlined />;
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

// Component to render access history in expanded row
function AccessHistoryTimeline({ documentId }: { documentId: string }) {
  const [history, setHistory] = useState<DocumentAccessHistory[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchHistory = async () => {
      try {
        setLoading(true);
        const data = await documentsApi.getAccessHistory(documentId);
        setHistory(data);
      } catch {
        // Silent fail
      } finally {
        setLoading(false);
      }
    };
    void fetchHistory();
  }, [documentId]);

  if (loading) {
    return (
      <div style={{ padding: '16px 48px', textAlign: 'center' }}>
        <Spin size="small" />
        <Text type="secondary" style={{ marginLeft: 8 }}>Loading access history...</Text>
      </div>
    );
  }

  if (history.length === 0) {
    return (
      <div style={{ padding: '16px 48px' }}>
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description="No access history yet"
        />
      </div>
    );
  }

  return (
    <div style={{ padding: '16px 48px' }}>
      <Space style={{ marginBottom: 12 }}>
        <HistoryOutlined />
        <Title level={5} style={{ margin: 0 }}>Access Permission History</Title>
      </Space>
      <Timeline
        items={history.map((entry) => ({
          color: entry.action === 'Granted' ? 'green' : 'red',
          dot: entry.action === 'Granted' 
            ? <CheckCircleOutlined style={{ fontSize: 16 }} />
            : <CloseCircleOutlined style={{ fontSize: 16 }} />,
          children: (
            <div>
              <Text strong>
                {entry.action === 'Granted' ? 'Access Granted' : 'Access Revoked'}
              </Text>
              <br />
              <Text>
                <Text type="secondary">Caregiver:</Text> {entry.caregiverName}
                {entry.caregiverEmail && <Text type="secondary"> ({entry.caregiverEmail})</Text>}
              </Text>
              <br />
              <Text>
                <Text type="secondary">By:</Text> {entry.performedByName}
              </Text>
              <br />
              <Text type="secondary" style={{ fontSize: 12 }}>
                {dayjs(entry.performedAt).format('MMM D, YYYY [at] h:mm A')}
              </Text>
            </div>
          ),
        }))}
      />
    </div>
  );
}

export default function DocumentList({ clientId, homeId, onUploadClick, onRefresh }: DocumentListProps) {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Admin');

  const [documents, setDocuments] = useState<DocumentSummary[]>([]);
  const [caregivers, setCaregivers] = useState<CaregiverSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [filterType, setFilterType] = useState<DocumentType | undefined>();
  const [expandedRowKeys, setExpandedRowKeys] = useState<string[]>([]);

  // Access modal state
  const [accessModalOpen, setAccessModalOpen] = useState(false);
  const [selectedDocumentId, setSelectedDocumentId] = useState<string | null>(null);
  const [selectedCaregivers, setSelectedCaregivers] = useState<string[]>([]);
  const [originalCaregivers, setOriginalCaregivers] = useState<string[]>([]);
  const [savingAccess, setSavingAccess] = useState(false);
  const [loadingPermissions, setLoadingPermissions] = useState(false);

  // Document viewer state
  const [viewerOpen, setViewerOpen] = useState(false);
  const [viewerUrl, setViewerUrl] = useState<string | null>(null);
  const [viewerFileName, setViewerFileName] = useState('');
  const [viewerContentType, setViewerContentType] = useState('');

  const pageSize = 10;

  const fetchDocuments = useCallback(async () => {
    try {
      setLoading(true);
      const result = await documentsApi.getByClient(clientId, {
        documentType: filterType,
        pageNumber: currentPage,
        pageSize,
      });
      setDocuments(result.items);
      setTotalCount(result.totalCount);
    } catch {
      setDocuments([]);
    } finally {
      setLoading(false);
    }
  }, [clientId, filterType, currentPage]);

  const fetchCaregivers = useCallback(async () => {
    if (!isAdmin) return;
    try {
      // Fetch only caregivers assigned to this home if homeId is provided
      const data = homeId 
        ? await caregiversApi.getByHome(homeId, false)
        : await caregiversApi.getAll(false);
      setCaregivers(data);
    } catch {
      // Silent fail
    }
  }, [isAdmin, homeId]);

  useEffect(() => {
    void fetchDocuments();
  }, [fetchDocuments]);

  useEffect(() => {
    void fetchCaregivers();
  }, [fetchCaregivers]);

  const handleView = async (doc: DocumentSummary) => {
    // Check if document type is viewable in app
    const isPdf = doc.contentType === 'application/pdf' || doc.fileName.toLowerCase().endsWith('.pdf');
    const isImage = doc.contentType?.startsWith('image/') || 
      /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(doc.fileName);

    if (!isPdf && !isImage) {
      // Unsupported file type for in-app viewing
      if (!isAdmin) {
        void message.info('This document type cannot be viewed in the app. Please contact an administrator to request access.');
        return;
      }
      // Admin can download or open in new tab
    }

    try {
      const result = await documentsApi.getSasUrl(doc.id);
      if (result.success && result.viewUrl) {
        if (!isPdf && !isImage && isAdmin) {
          // Admin viewing non-viewable file - open in new tab
          window.open(result.viewUrl, '_blank');
        } else {
          // Open in-app viewer
          setViewerUrl(result.viewUrl);
          setViewerFileName(doc.fileName);
          setViewerContentType(result.contentType || doc.contentType || '');
          setViewerOpen(true);
        }
      } else {
        void message.error(result.error || 'Failed to get view URL');
      }
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        void message.warning('You do not have permission to view this document. Please contact an administrator to request access.');
      } else {
        const msg = err instanceof ApiError ? err.message : 'Failed to get view URL';
        void message.error(msg);
      }
    }
  };

  const handleDownload = async (doc: DocumentSummary) => {
    // Only admins can download
    if (!isAdmin) {
      void message.warning('Download is not available. Please contact an administrator if you need a copy of this document.');
      return;
    }

    try {
      const result = await documentsApi.getSasUrl(doc.id);
      if (result.success && result.viewUrl) {
        // Fetch the file as a blob to force actual download
        const response = await fetch(result.viewUrl);
        if (!response.ok) {
          throw new Error('Failed to download file');
        }
        const blob = await response.blob();
        
        // Create a blob URL and trigger download
        const blobUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = blobUrl;
        link.download = doc.fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        // Clean up the blob URL
        window.URL.revokeObjectURL(blobUrl);
        void message.success('Download started');
      } else {
        void message.error(result.error || 'Failed to get download URL');
      }
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        void message.warning('You do not have permission to download this document.');
      } else {
        const msg = err instanceof ApiError ? err.message : 'Failed to download document';
        void message.error(msg);
      }
    }
  };

  const handleDelete = async (doc: DocumentSummary) => {
    try {
      const result = await documentsApi.delete(doc.id);
      if (result.success) {
        void message.success('Document deleted successfully');
        void fetchDocuments();
      } else {
        void message.error(result.error || 'Failed to delete document');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to delete document';
      void message.error(msg);
    }
  };

  const handleManageAccess = async (doc: DocumentSummary) => {
    setSelectedDocumentId(doc.id);
    setSelectedCaregivers([]);
    setOriginalCaregivers([]);
    setAccessModalOpen(true);
    setLoadingPermissions(true);
    
    try {
      // Fetch document details to get existing permissions
      const document = await documentsApi.getById(doc.id);
      if (document.accessPermissions && document.accessPermissions.length > 0) {
        const existingCaregiverIds = document.accessPermissions.map((p) => p.caregiverId);
        setSelectedCaregivers(existingCaregiverIds);
        setOriginalCaregivers(existingCaregiverIds);
      }
    } catch {
      // Silent fail - just show empty selection
    } finally {
      setLoadingPermissions(false);
    }
  };

  const handleSaveAccess = async () => {
    if (!selectedDocumentId) return;

    // Calculate additions and removals
    const toGrant = selectedCaregivers.filter((id) => !originalCaregivers.includes(id));
    const toRevoke = originalCaregivers.filter((id) => !selectedCaregivers.includes(id));

    // No changes made
    if (toGrant.length === 0 && toRevoke.length === 0) {
      setAccessModalOpen(false);
      setSelectedDocumentId(null);
      setSelectedCaregivers([]);
      setOriginalCaregivers([]);
      return;
    }

    try {
      setSavingAccess(true);
      const docIdToRefresh = selectedDocumentId;
      let hasError = false;

      // Grant access to new caregivers
      if (toGrant.length > 0) {
        const grantResult = await documentsApi.grantAccess(selectedDocumentId, {
          caregiverIds: toGrant,
        });
        if (!grantResult.success) {
          void message.error(grantResult.error || 'Failed to grant access');
          hasError = true;
        }
      }

      // Revoke access from removed caregivers
      for (const caregiverId of toRevoke) {
        try {
          const revokeResult = await documentsApi.revokeAccess(selectedDocumentId, caregiverId);
          if (!revokeResult.success) {
            void message.error(revokeResult.error || 'Failed to revoke access');
            hasError = true;
          }
        } catch {
          hasError = true;
        }
      }

      if (!hasError) {
        void message.success('Access permissions updated successfully');
      }

      setAccessModalOpen(false);
      setSelectedDocumentId(null);
      setSelectedCaregivers([]);
      setOriginalCaregivers([]);

      // Refresh the expanded row if it's open to show the new history
      if (expandedRowKeys.includes(docIdToRefresh)) {
        setExpandedRowKeys([]);
        setTimeout(() => {
          setExpandedRowKeys([docIdToRefresh]);
        }, 100);
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to update access';
      void message.error(msg);
    } finally {
      setSavingAccess(false);
    }
  };

  const handleExpandRow = (expanded: boolean, record: DocumentSummary) => {
    if (expanded) {
      setExpandedRowKeys([record.id]);
    } else {
      setExpandedRowKeys([]);
    }
  };

  const columns: ColumnsType<DocumentSummary> = [
    {
      title: 'Document',
      key: 'document',
      render: (_, record) => (
        <Space 
          style={{ cursor: 'pointer' }} 
          onClick={() => void handleView(record)}
        >
          {getFileIcon(record.fileName)}
          <div>
            <Text strong style={{ color: '#1890ff' }}>
              {record.fileName}
            </Text>
            <Text type="secondary" style={{ fontSize: 12, display: 'block' }}>
              {formatFileSize(record.fileSizeBytes)}
            </Text>
          </div>
        </Space>
      ),
    },
    {
      title: 'Type',
      dataIndex: 'documentType',
      key: 'documentType',
      width: 120,
      render: getTypeTag,
    },
    {
      title: 'Uploaded By',
      dataIndex: 'uploadedByName',
      key: 'uploadedByName',
      responsive: ['lg'],
    },
    {
      title: 'Uploaded',
      dataIndex: 'uploadedAt',
      key: 'uploadedAt',
      width: 150,
      render: (value) => dayjs(value).format('MMM D, YYYY'),
      sorter: (a, b) => dayjs(a.uploadedAt).unix() - dayjs(b.uploadedAt).unix(),
      defaultSortOrder: 'descend',
    },
    {
      title: 'Actions',
      key: 'actions',
      width: isAdmin ? 150 : 50,
      render: (_, record) => (
        <Space>
          {isAdmin && (
            <>
              <Tooltip title="Download">
                <Button
                  type="text"
                  icon={<DownloadOutlined />}
                  onClick={(e) => {
                    e.stopPropagation();
                    void handleDownload(record);
                  }}
                />
              </Tooltip>
              <Tooltip title="Manage Access">
                <Button
                  type="text"
                  icon={<TeamOutlined />}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleManageAccess(record);
                  }}
                />
              </Tooltip>
              <Popconfirm
                title="Delete this document?"
                description="This action cannot be undone."
                onConfirm={() => void handleDelete(record)}
                okText="Delete"
                okType="danger"
                cancelText="Cancel"
              >
                <Tooltip title="Delete">
                  <Button 
                    type="text" 
                    danger 
                    icon={<DeleteOutlined />} 
                    onClick={(e) => e.stopPropagation()}
                  />
                </Tooltip>
              </Popconfirm>
            </>
          )}
        </Space>
      ),
    },
  ];

  return (
    <>
      <Card
        title={
          <Space>
            <FileOutlined />
            <span>Documents</span>
            {totalCount > 0 && <Tag>{totalCount}</Tag>}
          </Space>
        }
        extra={
          <Space>
            <Button
              icon={<ReloadOutlined />}
              onClick={() => {
                void fetchDocuments();
                onRefresh?.();
              }}
            >
              Refresh
            </Button>
            {isAdmin && (
              <Button type="primary" icon={<PlusOutlined />} onClick={onUploadClick}>
                Upload Document
              </Button>
            )}
          </Space>
        }
      >
        <Flex gap={16} style={{ marginBottom: 16 }}>
          <Select
            placeholder="Filter by type"
            allowClear
            style={{ width: 160 }}
            value={filterType}
            onChange={setFilterType}
            options={DOCUMENT_TYPES.map((t) => ({ label: t.label, value: t.value }))}
          />
        </Flex>

        <Table
          columns={columns}
          dataSource={documents}
          rowKey="id"
          loading={loading}
          expandable={
            isAdmin
              ? {
                  expandedRowKeys,
                  onExpand: handleExpandRow,
                  expandedRowRender: (record) => <AccessHistoryTimeline documentId={record.id} />,
                  expandIcon: ({ expanded, onExpand, record }) => (
                    <Button
                      type="text"
                      size="small"
                      icon={expanded ? <DownOutlined /> : <RightOutlined />}
                      onClick={(e) => {
                        e.stopPropagation();
                        onExpand(record, e);
                      }}
                      style={{ marginRight: 8 }}
                    />
                  ),
                }
              : undefined
          }
          pagination={{
            current: currentPage,
            pageSize,
            total: totalCount,
            onChange: setCurrentPage,
            showSizeChanger: false,
            showTotal: (total) => `${total} documents`,
          }}
          locale={{
            emptyText: (
              <Empty
                image={<FileOutlined style={{ fontSize: 48, color: '#d9d9d9' }} />}
                description="No documents uploaded"
              >
                {isAdmin && (
                  <Button type="primary" icon={<PlusOutlined />} onClick={onUploadClick}>
                    Upload First Document
                  </Button>
                )}
              </Empty>
            ),
          }}
        />
      </Card>

      {/* Manage Access Modal */}
      <Modal
        title={
          <Space>
            <LockOutlined />
            <span>Manage Document Access</span>
          </Space>
        }
        open={accessModalOpen}
        onCancel={() => {
          setAccessModalOpen(false);
          setSelectedDocumentId(null);
          setSelectedCaregivers([]);
          setOriginalCaregivers([]);
        }}
        onOk={() => void handleSaveAccess()}
        okText="Save Access"
        okButtonProps={{ loading: savingAccess }}
        destroyOnHidden
      >
        <Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
          Select caregivers who should have view-only access to this document. Admins have access to all documents by default.
          {loadingPermissions && <Spin size="small" style={{ marginLeft: 8 }} />}
        </Text>
        <Checkbox.Group
          style={{ width: '100%' }}
          value={selectedCaregivers}
          onChange={(values) => setSelectedCaregivers(values as string[])}
        >
          <Space direction="vertical" style={{ width: '100%' }}>
            {caregivers.map((c) => (
              <Checkbox key={c.id} value={c.id}>
                {c.fullName} ({c.email})
              </Checkbox>
            ))}
          </Space>
        </Checkbox.Group>
        {caregivers.length === 0 && (
          <Text type="secondary">No caregivers available.</Text>
        )}
      </Modal>

      {/* Document Viewer Modal */}
      <DocumentViewer
        open={viewerOpen}
        onClose={() => {
          setViewerOpen(false);
          setViewerUrl(null);
          setViewerFileName('');
          setViewerContentType('');
        }}
        documentUrl={viewerUrl}
        fileName={viewerFileName}
        contentType={viewerContentType}
        isAdmin={isAdmin}
      />
    </>
  );
}
