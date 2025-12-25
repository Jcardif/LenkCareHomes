'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Typography,
  Tabs,
  Card,
  Breadcrumb,
  Button,
  Empty,
  Spin,
  message,
  Space,
  Table,
  Tag,
  Dropdown,
  Modal,
  Form,
  Input,
  Select,
  Upload,
  Row,
  Col,
  Statistic,
  Grid,
} from 'antd';
import type { TableColumnsType, MenuProps, UploadFile } from 'antd';

const { useBreakpoint } = Grid;
import {
  FolderOutlined,
  FolderOpenOutlined,
  FileOutlined,
  FilePdfOutlined,
  FileImageOutlined,
  FileWordOutlined,
  FileExcelOutlined,
  UploadOutlined,
  FolderAddOutlined,
  HomeOutlined,
  TeamOutlined,
  BankOutlined,
  GlobalOutlined,
  MoreOutlined,
  DeleteOutlined,
  EyeOutlined,
} from '@ant-design/icons';
import { useAuth } from '@/contexts/AuthContext';
import {
  ProtectedRoute,
  AuthenticatedLayout,
} from '@/components';
import DocumentViewer from '@/components/documents/DocumentViewer';
import { foldersApi, documentsApi, homesApi } from '@/lib/api';
import type {
  DocumentScope,
  FolderSummary,
  DocumentSummary,
  BrowseDocumentsResponse,
  BreadcrumbItem,
  DocumentType,
  HomeSummary,
} from '@/types';

const { Title, Paragraph, Text } = Typography;

// Scope configuration
const SCOPE_CONFIG: Record<DocumentScope, { label: string; icon: React.ReactNode; color: string }> = {
  Client: { label: 'Client Documents', icon: <TeamOutlined />, color: 'blue' },
  Home: { label: 'Facility (Home)', icon: <HomeOutlined />, color: 'green' },
  Business: { label: 'Business Documents', icon: <BankOutlined />, color: 'purple' },
  General: { label: 'General Documents', icon: <GlobalOutlined />, color: 'orange' },
};

// Document type options
const DOCUMENT_TYPES: { value: DocumentType; label: string }[] = [
  { value: 'CarePlan', label: 'Care Plan' },
  { value: 'Medical', label: 'Medical' },
  { value: 'Legal', label: 'Legal' },
  { value: 'Financial', label: 'Financial' },
  { value: 'Assessment', label: 'Assessment' },
  { value: 'Photo', label: 'Photo' },
  { value: 'Other', label: 'Other' },
];

// Helper to get file icon based on content type
function getFileIcon(contentType: string) {
  if (contentType.includes('pdf')) return <FilePdfOutlined style={{ fontSize: 24, color: '#ff4d4f' }} />;
  if (contentType.includes('image')) return <FileImageOutlined style={{ fontSize: 24, color: '#1890ff' }} />;
  if (contentType.includes('word') || contentType.includes('document')) return <FileWordOutlined style={{ fontSize: 24, color: '#2f54eb' }} />;
  if (contentType.includes('excel') || contentType.includes('spreadsheet')) return <FileExcelOutlined style={{ fontSize: 24, color: '#52c41a' }} />;
  return <FileOutlined style={{ fontSize: 24, color: '#8c8c8c' }} />;
}

// Helper to format file size
function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Combined item type for display
type BrowseItem = 
  | { type: 'folder'; data: FolderSummary }
  | { type: 'document'; data: DocumentSummary };

function DocumentsContent() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Admin');
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;
  
  // State
  const [activeScope, setActiveScope] = useState<DocumentScope>('General');
  const [currentFolderId, setCurrentFolderId] = useState<string | null>(null);
  const [browseResponse, setBrowseResponse] = useState<BrowseDocumentsResponse | null>(null);
  const [breadcrumbs, setBreadcrumbs] = useState<BreadcrumbItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [homes, setHomes] = useState<HomeSummary[]>([]);
  const [selectedHomeId, setSelectedHomeId] = useState<string | null>(null);
  const [selectedClientId, setSelectedClientId] = useState<string | null>(null);
  
  // Modal states
  const [createFolderOpen, setCreateFolderOpen] = useState(false);
  const [uploadModalOpen, setUploadModalOpen] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  
  // Document viewer state
  const [viewerOpen, setViewerOpen] = useState(false);
  const [viewerUrl, setViewerUrl] = useState<string | null>(null);
  const [viewerFileName, setViewerFileName] = useState('');
  const [viewerContentType, setViewerContentType] = useState('');
  
  // Forms
  const [folderForm] = Form.useForm();
  const [uploadForm] = Form.useForm();

  // Load homes for Home scope
  useEffect(() => {
    if (isAdmin) {
      homesApi.getAll().then(setHomes).catch(console.error);
    }
  }, [isAdmin]);

  // Load documents/folders for current view
  const loadBrowseData = useCallback(async () => {
    setLoading(true);
    try {
      const response = await foldersApi.browse({
        scope: activeScope,
        folderId: currentFolderId || undefined,
        homeId: activeScope === 'Home' ? selectedHomeId || undefined : undefined,
        clientId: activeScope === 'Client' ? selectedClientId || undefined : undefined,
      });
      setBrowseResponse(response);
      setBreadcrumbs(response.breadcrumbs);
    } catch (error) {
      console.error('Failed to load documents:', error);
      message.error('Failed to load documents');
    } finally {
      setLoading(false);
    }
  }, [activeScope, currentFolderId, selectedHomeId, selectedClientId]);

  useEffect(() => {
    // Reset folder and selected client/home when switching scopes
    setCurrentFolderId(null);
    setSelectedClientId(null);
    setSelectedHomeId(null);
    setBreadcrumbs([]);
  }, [activeScope]);

  useEffect(() => {
    loadBrowseData();
  }, [loadBrowseData]);

  // Navigate to folder (handles both real folders and virtual client/home folders)
  const handleFolderClick = (folder: FolderSummary) => {
    // For Client scope - if this is a virtual client folder (isSystemFolder and clientId matches id)
    if (activeScope === 'Client' && folder.isSystemFolder && folder.clientId === folder.id) {
      setSelectedClientId(folder.id);
      setCurrentFolderId(null); // Reset real folder
      return;
    }
    
    // For Home scope - if this is a virtual home folder (isSystemFolder and homeId matches id)
    if (activeScope === 'Home' && folder.isSystemFolder && folder.homeId === folder.id) {
      setSelectedHomeId(folder.id);
      setCurrentFolderId(null); // Reset real folder
      return;
    }
    
    // Regular folder navigation
    setCurrentFolderId(folder.id);
  };

  // Navigate via breadcrumb
  const handleBreadcrumbClick = (folderId: string | null) => {
    if (folderId === null) {
      // Going back to root - reset client/home selection too
      setCurrentFolderId(null);
      if (activeScope === 'Client') {
        setSelectedClientId(null);
      }
      if (activeScope === 'Home') {
        setSelectedHomeId(null);
      }
    } else {
      setCurrentFolderId(folderId);
    }
  };

  // View document
  const handleViewDocument = async (doc: DocumentSummary) => {
    // Check if document type is viewable in app
    const isPdf = doc.contentType === 'application/pdf' || doc.fileName.toLowerCase().endsWith('.pdf');
    const isImage = doc.contentType?.startsWith('image/') || 
      /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(doc.fileName);

    if (!isPdf && !isImage) {
      // Unsupported file type for in-app viewing
      if (!isAdmin) {
        message.info('This document type cannot be viewed in the app. Please contact an administrator to request access.');
        return;
      }
      // Admin can download or open in new tab
    }

    try {
      const response = await documentsApi.getSasUrl(doc.id);
      if (response.success && response.viewUrl) {
        if (!isPdf && !isImage && isAdmin) {
          // Admin viewing non-viewable file - open in new tab
          window.open(response.viewUrl, '_blank');
        } else {
          // Open in-app viewer
          setViewerUrl(response.viewUrl);
          setViewerFileName(doc.fileName);
          setViewerContentType(response.contentType || doc.contentType || '');
          setViewerOpen(true);
        }
      } else {
        message.error(response.error || 'Failed to get document URL');
      }
    } catch (error) {
      console.error('Failed to view document:', error);
      message.error('Failed to view document');
    }
  };

  // Delete document
  const handleDeleteDocument = async (doc: DocumentSummary) => {
    Modal.confirm({
      title: 'Delete Document',
      content: `Are you sure you want to delete "${doc.fileName}"?`,
      okText: 'Delete',
      okType: 'danger',
      onOk: async () => {
        try {
          const response = await documentsApi.delete(doc.id);
          if (response.success) {
            message.success('Document deleted');
            loadBrowseData();
          } else {
            message.error(response.error || 'Failed to delete document');
          }
        } catch (error) {
          console.error('Failed to delete document:', error);
          message.error('Failed to delete document');
        }
      },
    });
  };

  // Delete folder
  const handleDeleteFolder = async (folder: FolderSummary) => {
    if (folder.isSystemFolder) {
      message.warning('System folders cannot be deleted');
      return;
    }
    
    Modal.confirm({
      title: 'Delete Folder',
      content: `Are you sure you want to delete "${folder.name}"? This will delete all documents inside.`,
      okText: 'Delete',
      okType: 'danger',
      onOk: async () => {
        try {
          const response = await foldersApi.delete(folder.id);
          if (response.success) {
            message.success('Folder deleted');
            loadBrowseData();
          } else {
            message.error(response.error || 'Failed to delete folder');
          }
        } catch (error) {
          console.error('Failed to delete folder:', error);
          message.error('Failed to delete folder');
        }
      },
    });
  };

  // Create folder
  const handleCreateFolder = async (values: { name: string }) => {
    try {
      const response = await foldersApi.create({
        name: values.name,
        scope: activeScope,
        parentFolderId: currentFolderId || undefined,
        homeId: activeScope === 'Home' ? selectedHomeId || undefined : undefined,
        clientId: activeScope === 'Client' ? selectedClientId || undefined : undefined,
      });
      
      if (response.success) {
        message.success('Folder created');
        setCreateFolderOpen(false);
        folderForm.resetFields();
        loadBrowseData();
      } else {
        message.error(response.error || 'Failed to create folder');
      }
    } catch (error) {
      console.error('Failed to create folder:', error);
      message.error('Failed to create folder');
    }
  };

  // Upload document
  const handleUploadDocument = async (values: { documentType: DocumentType; description?: string }) => {
    if (fileList.length === 0) {
      message.error('Please select a file');
      return;
    }

    const file = fileList[0].originFileObj as File;
    if (!file) {
      message.error('Invalid file');
      return;
    }

    setUploading(true);
    try {
      const response = await documentsApi.uploadGeneral(
        file,
        values.documentType,
        activeScope,
        {
          description: values.description,
          folderId: currentFolderId || undefined,
          homeId: activeScope === 'Home' ? selectedHomeId || undefined : undefined,
          clientId: activeScope === 'Client' ? selectedClientId || undefined : undefined,
        }
      );

      if (response.success) {
        message.success('Document uploaded');
        setUploadModalOpen(false);
        setFileList([]);
        uploadForm.resetFields();
        loadBrowseData();
      } else {
        message.error(response.error || 'Failed to upload document');
      }
    } catch (error) {
      console.error('Failed to upload document:', error);
      message.error('Failed to upload document');
    } finally {
      setUploading(false);
    }
  };

  // Build combined items list
  const items: BrowseItem[] = [
    ...(browseResponse?.folders || []).map((f): BrowseItem => ({ type: 'folder', data: f })),
    ...(browseResponse?.documents || []).map((d): BrowseItem => ({ type: 'document', data: d })),
  ];

  // Table columns
  const columns: TableColumnsType<BrowseItem> = [
    {
      title: 'Name',
      key: 'name',
      ellipsis: true,
      render: (_, item) => (
        <Space size={isMobile ? 4 : 8}>
          {item.type === 'folder' ? (
            <>
              <FolderOutlined style={{ fontSize: isMobile ? 16 : 20, color: '#faad14' }} />
              <Button 
                type="link" 
                style={{ padding: 0, color: '#2d3732', fontWeight: 500 }}
                onClick={() => handleFolderClick(item.data as FolderSummary)}
              >
                {item.data.name}
              </Button>
              {!isMobile && (item.data as FolderSummary).isSystemFolder && (
                <Tag color="default" style={{ fontSize: 10 }}>System</Tag>
              )}
            </>
          ) : (
            <>
              {getFileIcon((item.data as DocumentSummary).contentType)}
              <Button 
                type="link" 
                style={{ padding: 0, color: '#2d3732', marginLeft: isMobile ? 4 : 8 }}
                onClick={() => handleViewDocument(item.data as DocumentSummary)}
              >
                <Text ellipsis>{(item.data as DocumentSummary).fileName}</Text>
              </Button>
            </>
          )}
        </Space>
      ),
    },
    {
      title: 'Type',
      key: 'type',
      width: isMobile ? 80 : 120,
      responsive: ['sm'],
      render: (_, item) => {
        if (item.type === 'folder') {
          return <Tag>Folder</Tag>;
        }
        const doc = item.data as DocumentSummary;
        return <Tag color="blue">{isSmallMobile ? doc.documentType.substring(0, 4) : doc.documentType}</Tag>;
      },
    },
    {
      title: 'Size',
      key: 'size',
      width: 100,
      responsive: ['md'],
      render: (_, item) => {
        if (item.type === 'folder') {
          const folder = item.data as FolderSummary;
          return <Text type="secondary">{folder.documentCount} files</Text>;
        }
        return <Text type="secondary">{formatFileSize((item.data as DocumentSummary).fileSizeBytes)}</Text>;
      },
    },
    {
      title: 'Modified',
      key: 'modified',
      width: 150,
      responsive: ['lg'],
      render: (_, item) => {
        if (item.type === 'document') {
          const doc = item.data as DocumentSummary;
          return <Text type="secondary">{new Date(doc.uploadedAt).toLocaleDateString()}</Text>;
        }
        return null;
      },
    },
    {
      title: '',
      key: 'actions',
      width: isMobile ? 50 : 80,
      render: (_, item) => {
        const menuItems: MenuProps['items'] = [];

        if (item.type === 'document') {
          const doc = item.data as DocumentSummary;
          menuItems.push(
            { key: 'view', label: 'View', icon: <EyeOutlined />, onClick: () => handleViewDocument(doc) },
          );
          if (isAdmin) {
            menuItems.push(
              { type: 'divider' },
              { key: 'delete', label: 'Delete', icon: <DeleteOutlined />, danger: true, onClick: () => handleDeleteDocument(doc) },
            );
          }
        } else {
          const folder = item.data as FolderSummary;
          menuItems.push(
            { key: 'open', label: 'Open', icon: <FolderOpenOutlined />, onClick: () => handleFolderClick(folder) },
          );
          if (isAdmin && !folder.isSystemFolder) {
            menuItems.push(
              { type: 'divider' },
              { key: 'delete', label: 'Delete', icon: <DeleteOutlined />, danger: true, onClick: () => handleDeleteFolder(folder) },
            );
          }
        }

        return (
          <Dropdown menu={{ items: menuItems }} trigger={['click']}>
            <Button type="text" icon={<MoreOutlined />} style={{ minWidth: 44, minHeight: 44 }} />
          </Dropdown>
        );
      },
    },
  ];

  // Render scope-specific header
  const renderScopeHeader = () => {
    if (activeScope === 'Home' && isAdmin) {
      return (
        <div style={{ marginBottom: 16 }}>
          <Select
            placeholder="Select a home"
            style={{ width: 300 }}
            value={selectedHomeId}
            onChange={setSelectedHomeId}
            options={homes.map(h => ({ value: h.id, label: h.name }))}
            allowClear
          />
        </div>
      );
    }
    return null;
  };

  return (
    <div>
      {/* Header */}
      <div className="page-header-wrapper" style={{ marginBottom: 24 }}>
        <div>
          <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>
            <FolderOutlined style={{ marginRight: 12 }} />
            {isSmallMobile ? 'Docs' : 'Documents'}
          </Title>
          {!isSmallMobile && (
            <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
              Manage and organize documents across the organization
            </Paragraph>
          )}
        </div>
      </div>

      {/* Scope Tabs */}
      <Tabs
        activeKey={activeScope}
        onChange={(key) => setActiveScope(key as DocumentScope)}
        size={isMobile ? 'small' : 'middle'}
        items={Object.entries(SCOPE_CONFIG)
          .filter(([scope]) => isAdmin || scope === 'General' || scope === 'Home')
          .map(([scope, config]) => ({
            key: scope,
            label: (
              <span>
                {config.icon}
                {!isSmallMobile && <span style={{ marginLeft: 8 }}>{config.label.split(' ')[0]}</span>}
              </span>
            ),
          }))}
      />

      {/* Scope-specific header (e.g., home selector) */}
      {renderScopeHeader()}

      {/* Toolbar */}
      <Card size="small" className="responsive-card" style={{ marginBottom: 16 }}>
        {isMobile ? (
          <Space direction="vertical" style={{ width: '100%' }} size="middle">
            {/* Breadcrumbs */}
            <Breadcrumb style={{ fontSize: 12 }}>
              <Breadcrumb.Item>
                <Button 
                  type="link" 
                  size="small"
                  style={{ padding: 0 }}
                  onClick={() => handleBreadcrumbClick(null)}
                >
                  {SCOPE_CONFIG[activeScope].icon}
                </Button>
              </Breadcrumb.Item>
              {breadcrumbs.slice(-2).map((crumb) => (
                <Breadcrumb.Item key={crumb.id}>
                  <Button
                    type="link"
                    size="small"
                    style={{ padding: 0 }}
                    onClick={() => handleBreadcrumbClick(crumb.id)}
                  >
                    {crumb.name}
                  </Button>
                </Breadcrumb.Item>
              ))}
            </Breadcrumb>

            {/* Actions */}
            {isAdmin && (
              <Row gutter={8}>
                {(activeScope === 'Business' || activeScope === 'General') && (
                  <Col span={12}>
                    <Button
                      icon={<FolderAddOutlined />}
                      onClick={() => setCreateFolderOpen(true)}
                      style={{ width: '100%' }}
                    >
                      New Folder
                    </Button>
                  </Col>
                )}
                <Col span={(activeScope === 'Business' || activeScope === 'General') ? 12 : 24}>
                  <Button
                    type="primary"
                    icon={<UploadOutlined />}
                    onClick={() => setUploadModalOpen(true)}
                    style={{ width: '100%' }}
                    disabled={
                      (activeScope === 'Client' && !selectedClientId) ||
                      (activeScope === 'Home' && !selectedHomeId)
                    }
                  >
                    Upload
                  </Button>
                </Col>
              </Row>
            )}
          </Space>
        ) : (
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            {/* Breadcrumbs */}
            <Breadcrumb>
              <Breadcrumb.Item>
                <Button 
                  type="link" 
                  size="small"
                  style={{ padding: 0 }}
                  onClick={() => handleBreadcrumbClick(null)}
                >
                  {SCOPE_CONFIG[activeScope].icon}
                  <span style={{ marginLeft: 4 }}>{SCOPE_CONFIG[activeScope].label}</span>
                </Button>
              </Breadcrumb.Item>
              {breadcrumbs.map((crumb) => (
                <Breadcrumb.Item key={crumb.id}>
                  <Button
                    type="link"
                    size="small"
                    style={{ padding: 0 }}
                    onClick={() => handleBreadcrumbClick(crumb.id)}
                  >
                    {crumb.name}
                  </Button>
                </Breadcrumb.Item>
              ))}
            </Breadcrumb>

            {/* Actions */}
            {isAdmin && (
              <Space>
                {/* Only show New Folder for Business and General scopes */}
                {(activeScope === 'Business' || activeScope === 'General') && (
                  <Button
                    icon={<FolderAddOutlined />}
                    onClick={() => setCreateFolderOpen(true)}
                  >
                    New Folder
                  </Button>
                )}
                <Button
                  type="primary"
                  icon={<UploadOutlined />}
                  onClick={() => setUploadModalOpen(true)}
                  disabled={
                    (activeScope === 'Client' && !selectedClientId) ||
                    (activeScope === 'Home' && !selectedHomeId)
                  }
                >
                  Upload
                </Button>
              </Space>
            )}
          </div>
        )}
      </Card>

      {/* Content */}
      <Card className="responsive-card">
        {loading ? (
          <div style={{ textAlign: 'center', padding: isMobile ? 24 : 48 }}>
            <Spin size="large" />
          </div>
        ) : items.length === 0 ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={
              <span>
                No documents or folders here.
                {isAdmin && !isMobile && ' Click "New Folder" or "Upload" to add content.'}
              </span>
            }
          />
        ) : (
          <Table
            dataSource={items}
            columns={columns}
            rowKey={(item) => item.data.id}
            pagination={false}
            scroll={{ x: 'max-content' }}
            size={isMobile ? 'small' : 'middle'}
          />
        )}
      </Card>

      {/* Summary Stats */}
      {browseResponse && !isMobile && (
        <Row gutter={16} style={{ marginTop: 16 }}>
          <Col span={8}>
            <Card size="small">
              <Statistic
                title="Folders"
                value={browseResponse.folders.length}
                prefix={<FolderOutlined />}
              />
            </Card>
          </Col>
          <Col span={8}>
            <Card size="small">
              <Statistic
                title="Documents"
                value={browseResponse.totalDocuments}
                prefix={<FileOutlined />}
              />
            </Card>
          </Col>
          <Col span={8}>
            <Card size="small">
              <Statistic
                title="Current Page"
                value={`${browseResponse.pageNumber} of ${browseResponse.totalPages || 1}`}
              />
            </Card>
          </Col>
        </Row>
      )}

      {/* Create Folder Modal */}
      <Modal
        title="Create New Folder"
        open={createFolderOpen}
        onCancel={() => {
          setCreateFolderOpen(false);
          folderForm.resetFields();
        }}
        onOk={() => folderForm.submit()}
        okText="Create"
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Form
          form={folderForm}
          layout="vertical"
          onFinish={handleCreateFolder}
        >
          <Form.Item
            name="name"
            label="Folder Name"
            rules={[{ required: true, message: 'Please enter a folder name' }]}
          >
            <Input placeholder="Enter folder name" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Upload Modal */}
      <Modal
        title="Upload Document"
        open={uploadModalOpen}
        onCancel={() => {
          setUploadModalOpen(false);
          setFileList([]);
          uploadForm.resetFields();
        }}
        onOk={() => uploadForm.submit()}
        okText="Upload"
        confirmLoading={uploading}
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Form
          form={uploadForm}
          layout="vertical"
          onFinish={handleUploadDocument}
        >
          <Form.Item
            label="File"
            required
          >
            <Upload.Dragger
              fileList={fileList}
              onChange={({ fileList }) => setFileList(fileList)}
              beforeUpload={() => false}
              maxCount={1}
            >
              <p className="ant-upload-drag-icon">
                <UploadOutlined />
              </p>
              <p className="ant-upload-text">{isMobile ? 'Tap to select file' : 'Click or drag file to upload'}</p>
              <p className="ant-upload-hint">Maximum file size: 50MB</p>
            </Upload.Dragger>
          </Form.Item>
          <Form.Item
            name="documentType"
            label="Document Type"
            rules={[{ required: true, message: 'Please select a document type' }]}
          >
            <Select
              placeholder="Select document type"
              options={DOCUMENT_TYPES}
            />
          </Form.Item>
          <Form.Item
            name="description"
            label="Description (Optional)"
          >
            <Input.TextArea rows={3} placeholder="Enter description" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Document Viewer Modal */}
      <DocumentViewer
        open={viewerOpen}
        onClose={() => {
          setViewerOpen(false);
          setViewerUrl(null);
        }}
        fileName={viewerFileName}
        documentUrl={viewerUrl}
        contentType={viewerContentType}
        isAdmin={isAdmin}
      />
    </div>
  );
}

export default function DocumentsPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin']}>
      <AuthenticatedLayout>
        <DocumentsContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
