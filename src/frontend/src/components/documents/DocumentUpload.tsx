'use client';

import React, { useState } from 'react';
import {
  Upload,
  Button,
  Form,
  Select,
  Input,
  Modal,
  message,
  Typography,
  Space,
  Progress,
  Alert,
} from 'antd';
import { UploadOutlined, InboxOutlined } from '@ant-design/icons';
import type { UploadFile, UploadProps } from 'antd';

import { documentsApi, ApiError } from '@/lib/api';
import type { DocumentType, Document } from '@/types';

const { Dragger } = Upload;
const { TextArea } = Input;
const { Text } = Typography;

interface DocumentUploadProps {
  clientId: string;
  onSuccess?: (document: Document) => void;
  onCancel?: () => void;
  open?: boolean;
}

const DOCUMENT_TYPES: { label: string; value: DocumentType; description: string }[] = [
  { label: 'Care Plan', value: 'CarePlan', description: 'Client care plans and schedules' },
  { label: 'Medical', value: 'Medical', description: 'Medical records, test results, prescriptions' },
  { label: 'Legal', value: 'Legal', description: 'Legal documents, consent forms, POA' },
  { label: 'Financial', value: 'Financial', description: 'Financial records, insurance info' },
  { label: 'Photo', value: 'Photo', description: 'Client photos, ID photos' },
  { label: 'Assessment', value: 'Assessment', description: 'Assessment forms and evaluations' },
  { label: 'Other', value: 'Other', description: 'Other document types' },
];

const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10 MB
const ALLOWED_TYPES = [
  'application/pdf',
  'image/jpeg',
  'image/png',
  'image/gif',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
];

const ALLOWED_EXTENSIONS = ['.pdf', '.jpg', '.jpeg', '.png', '.gif', '.doc', '.docx'];

export default function DocumentUpload({ clientId, onSuccess, onCancel, open }: DocumentUploadProps) {
  const [form] = Form.useForm();
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);

  const validateFile = (file: File): string | null => {
    if (file.size > MAX_FILE_SIZE) {
      return `File size must be less than ${MAX_FILE_SIZE / 1024 / 1024}MB`;
    }
    if (!ALLOWED_TYPES.includes(file.type)) {
      return `File type not allowed. Supported types: ${ALLOWED_EXTENSIONS.join(', ')}`;
    }
    return null;
  };

  const uploadProps: UploadProps = {
    beforeUpload: (file) => {
      const error = validateFile(file);
      if (error) {
        void message.error(error);
        return Upload.LIST_IGNORE;
      }
      setFileList([file]);
      return false; // Prevent automatic upload
    },
    fileList,
    onRemove: () => {
      setFileList([]);
    },
    maxCount: 1,
    accept: ALLOWED_EXTENSIONS.join(','),
  };

  const handleUpload = async (values: { documentType: DocumentType; description?: string }) => {
    if (fileList.length === 0) {
      void message.error('Please select a file to upload');
      return;
    }

    const file = fileList[0] as unknown as File;
    
    try {
      setUploading(true);
      setUploadProgress(0);

      // Simulate progress for better UX
      const progressInterval = setInterval(() => {
        setUploadProgress((prev) => Math.min(prev + 10, 90));
      }, 200);

      const result = await documentsApi.upload(
        clientId,
        file,
        values.documentType,
        values.description
      );

      clearInterval(progressInterval);
      setUploadProgress(100);

      if (result.success && result.document) {
        void message.success('Document uploaded successfully');
        setFileList([]);
        form.resetFields();
        onSuccess?.(result.document);
      } else {
        void message.error(result.error || 'Failed to upload document');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to upload document';
      void message.error(msg);
    } finally {
      setUploading(false);
      setUploadProgress(0);
    }
  };

  const handleCancel = () => {
    setFileList([]);
    form.resetFields();
    onCancel?.();
  };

  const content = (
    <Form
      form={form}
      layout="vertical"
      onFinish={(values) => void handleUpload(values)}
      disabled={uploading}
    >
      <Form.Item
        name="documentType"
        label="Document Type"
        rules={[{ required: true, message: 'Please select a document type' }]}
      >
        <Select
          placeholder="Select document type"
          options={DOCUMENT_TYPES.map((t) => ({
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
        name="description"
        label="Description (Optional)"
      >
        <TextArea
          rows={2}
          placeholder="Brief description of the document..."
          maxLength={500}
          showCount
        />
      </Form.Item>

      <Form.Item
        label="File"
        required
        extra={
          <Text type="secondary">
            Supported formats: PDF, JPG, PNG, GIF, DOC, DOCX. Max size: 10MB
          </Text>
        }
      >
        <Dragger {...uploadProps}>
          <p className="ant-upload-drag-icon">
            <InboxOutlined />
          </p>
          <p className="ant-upload-text">Click or drag file to this area to upload</p>
          <p className="ant-upload-hint">
            Single file upload only. Sensitive documents are encrypted at rest.
          </p>
        </Dragger>
      </Form.Item>

      {uploading && (
        <Progress percent={uploadProgress} status="active" style={{ marginBottom: 16 }} />
      )}

      <Alert
        type="info"
        message="Document Security"
        description="All uploaded documents are encrypted at rest and protected according to HIPAA requirements. Access is logged for compliance."
        showIcon
        style={{ marginBottom: 16 }}
      />

      <Form.Item style={{ marginBottom: 0 }}>
        <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
          <Button onClick={handleCancel}>Cancel</Button>
          <Button
            type="primary"
            htmlType="submit"
            loading={uploading}
            disabled={fileList.length === 0}
            icon={<UploadOutlined />}
          >
            Upload Document
          </Button>
        </Space>
      </Form.Item>
    </Form>
  );

  if (open !== undefined) {
    return (
      <Modal
        title="Upload Document"
        open={open}
        onCancel={handleCancel}
        footer={null}
        destroyOnHidden
        width={600}
      >
        {content}
      </Modal>
    );
  }

  return content;
}
