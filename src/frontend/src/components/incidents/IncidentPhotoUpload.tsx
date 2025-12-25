'use client';

import React, { useState, useCallback } from 'react';
import {
  Upload,
  Modal,
  message,
  Typography,
  Popconfirm,
  Spin,
  Card,
  Empty,
  Row,
  Col,
} from 'antd';
import {
  PlusOutlined,
  DeleteOutlined,
  EyeOutlined,
  LoadingOutlined,
} from '@ant-design/icons';
import type { RcFile } from 'antd/es/upload/interface';
import { incidentsApi, ApiError } from '@/lib/api';
import type { IncidentPhoto } from '@/types';

const { Text } = Typography;

interface IncidentPhotoUploadProps {
  incidentId: string;
  photos: IncidentPhoto[];
  onPhotosChange?: (photos: IncidentPhoto[]) => void;
  canEdit?: boolean;
  maxPhotos?: number;
}

const ALLOWED_IMAGE_TYPES = [
  'image/jpeg',
  'image/png',
  'image/gif',
  'image/webp',
  'image/heic',
  'image/heif',
];

const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10 MB

interface PhotoWithUrl extends IncidentPhoto {
  viewUrl?: string;
  loading?: boolean;
}

export default function IncidentPhotoUpload({
  incidentId,
  photos,
  onPhotosChange,
  canEdit = false,
  maxPhotos = 10,
}: IncidentPhotoUploadProps) {
  const [uploading, setUploading] = useState(false);
  const [previewVisible, setPreviewVisible] = useState(false);
  const [previewImage, setPreviewImage] = useState('');
  const [previewTitle, setPreviewTitle] = useState('');
  const [photosWithUrls, setPhotosWithUrls] = useState<Record<string, PhotoWithUrl>>({});
  const [loadingUrls, setLoadingUrls] = useState<Record<string, boolean>>({});

  const getPhotoViewUrl = useCallback(async (photo: IncidentPhoto): Promise<string | null> => {
    try {
      const response = await incidentsApi.getPhotoViewUrl(incidentId, photo.id);
      if (response.success && response.url) {
        return response.url;
      }
      return null;
    } catch {
      return null;
    }
  }, [incidentId]);

  const handlePreview = async (photo: IncidentPhoto) => {
    // Check if we already have the URL cached
    if (photosWithUrls[photo.id]?.viewUrl) {
      setPreviewImage(photosWithUrls[photo.id].viewUrl!);
      setPreviewTitle(photo.fileName);
      setPreviewVisible(true);
      return;
    }

    // Load the URL
    setLoadingUrls(prev => ({ ...prev, [photo.id]: true }));
    const url = await getPhotoViewUrl(photo);
    setLoadingUrls(prev => ({ ...prev, [photo.id]: false }));

    if (url) {
      setPhotosWithUrls(prev => ({ ...prev, [photo.id]: { ...photo, viewUrl: url } }));
      setPreviewImage(url);
      setPreviewTitle(photo.fileName);
      setPreviewVisible(true);
    } else {
      void message.error('Failed to load photo');
    }
  };

  const handleBeforeUpload = (file: RcFile): boolean => {
    // Check file type
    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
      void message.error('Please upload an image file (JPEG, PNG, GIF, WebP, HEIC)');
      return false;
    }

    // Check file size
    if (file.size > MAX_FILE_SIZE) {
      void message.error('Photo must be smaller than 10MB');
      return false;
    }

    // Check max photos
    if (photos.length >= maxPhotos) {
      void message.error(`Maximum of ${maxPhotos} photos allowed`);
      return false;
    }

    return true;
  };

  const handleUpload = async (file: RcFile) => {
    if (!handleBeforeUpload(file)) {
      return;
    }

    try {
      setUploading(true);

      const result = await incidentsApi.uploadPhoto(incidentId, file);

      if (result.success && result.photo) {
        void message.success('Photo uploaded successfully');
        onPhotosChange?.([...photos, result.photo]);
      } else {
        void message.error(result.error || 'Failed to upload photo');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to upload photo';
      void message.error(msg);
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async (photo: IncidentPhoto) => {
    try {
      const result = await incidentsApi.deletePhoto(incidentId, photo.id);
      if (result.success) {
        void message.success('Photo deleted');
        onPhotosChange?.(photos.filter(p => p.id !== photo.id));
        // Remove from cache
        setPhotosWithUrls(prev => {
          const updated = { ...prev };
          delete updated[photo.id];
          return updated;
        });
      } else {
        void message.error(result.error || 'Failed to delete photo');
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to delete photo';
      void message.error(msg);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  if (photos.length === 0 && !canEdit) {
    return (
      <Empty
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        description="No photos attached"
      />
    );
  }

  return (
    <>
      <Row gutter={[16, 16]}>
        {photos.map((photo) => (
          <Col key={photo.id} xs={12} sm={8} md={6} lg={4}>
            <Card
              size="small"
              hoverable
              cover={
                <div
                  style={{
                    position: 'relative',
                    height: 120,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    backgroundColor: '#f5f5f5',
                    cursor: 'pointer',
                  }}
                  onClick={() => void handlePreview(photo)}
                >
                  {loadingUrls[photo.id] ? (
                    <Spin indicator={<LoadingOutlined />} />
                  ) : photosWithUrls[photo.id]?.viewUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element -- Dynamic blob URLs not compatible with next/image
                    <img
                      src={photosWithUrls[photo.id].viewUrl}
                      alt={photo.fileName}
                      style={{
                        maxWidth: '100%',
                        maxHeight: 120,
                        objectFit: 'contain',
                      }}
                    />
                  ) : (
                    <div style={{ textAlign: 'center', padding: 8 }}>
                      <EyeOutlined style={{ fontSize: 24, color: '#999' }} />
                      <br />
                      <Text type="secondary" style={{ fontSize: 12 }}>
                        Click to view
                      </Text>
                    </div>
                  )}
                </div>
              }
              actions={
                canEdit
                  ? [
                      <Popconfirm
                        key="delete"
                        title="Delete this photo?"
                        onConfirm={() => void handleDelete(photo)}
                        okText="Delete"
                        okButtonProps={{ danger: true }}
                      >
                        <DeleteOutlined style={{ color: '#ff4d4f' }} />
                      </Popconfirm>,
                    ]
                  : [
                      <EyeOutlined
                        key="view"
                        onClick={() => void handlePreview(photo)}
                      />,
                    ]
              }
            >
              <Card.Meta
                title={
                  <Text ellipsis style={{ fontSize: 12 }}>
                    {photo.fileName}
                  </Text>
                }
                description={
                  <Text type="secondary" style={{ fontSize: 11 }}>
                    {formatFileSize(photo.fileSizeBytes)}
                  </Text>
                }
              />
            </Card>
          </Col>
        ))}

        {canEdit && photos.length < maxPhotos && (
          <Col xs={12} sm={8} md={6} lg={4}>
            <Upload
              accept={ALLOWED_IMAGE_TYPES.join(',')}
              showUploadList={false}
              customRequest={({ file }) => void handleUpload(file as RcFile)}
              disabled={uploading}
            >
              <Card
                size="small"
                hoverable
                style={{
                  height: '100%',
                  minHeight: 180,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  cursor: 'pointer',
                  borderStyle: 'dashed',
                }}
                styles={{ body: { display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' } }}
              >
                <div style={{ textAlign: 'center' }}>
                  {uploading ? (
                    <>
                      <LoadingOutlined style={{ fontSize: 24 }} />
                      <div style={{ marginTop: 8 }}>Uploading...</div>
                    </>
                  ) : (
                    <>
                      <PlusOutlined style={{ fontSize: 24 }} />
                      <div style={{ marginTop: 8 }}>Upload Photo</div>
                      <Text type="secondary" style={{ fontSize: 11 }}>
                        {photos.length}/{maxPhotos}
                      </Text>
                    </>
                  )}
                </div>
              </Card>
            </Upload>
          </Col>
        )}
      </Row>

      <Modal
        open={previewVisible}
        title={previewTitle}
        footer={null}
        onCancel={() => setPreviewVisible(false)}
        width="80%"
        style={{ maxWidth: 800 }}
        centered
      >
        {/* eslint-disable-next-line @next/next/no-img-element -- Dynamic blob URLs not compatible with next/image */}
        <img
          alt={previewTitle}
          style={{ width: '100%' }}
          src={previewImage}
        />
      </Modal>
    </>
  );
}
