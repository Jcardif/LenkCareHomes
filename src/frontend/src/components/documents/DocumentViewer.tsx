'use client';

import React, { useState, useCallback, useEffect, useRef } from 'react';
import { Modal, Button, Space, Typography, Spin, Result, Flex } from 'antd';
import {
  DownloadOutlined,
  ZoomInOutlined,
  ZoomOutOutlined,
  CloseOutlined,
  FileUnknownOutlined,
} from '@ant-design/icons';

const { Text } = Typography;

// Suppress PDF.js errors that occur during cleanup/unmount
if (typeof window !== 'undefined') {
  const originalError = console.error;
  console.error = (...args: unknown[]) => {
    const message = args[0]?.toString() || '';
    // Suppress PDF.js worker errors during cleanup
    if (
      message.includes('sendWithPromise') ||
      message.includes('getPage') ||
      message.includes('Cannot read properties of null')
    ) {
      return;
    }
    originalError.apply(console, args);
  };

  // Catch unhandled errors from PDF.js worker
  window.addEventListener('error', (event) => {
    const message = event.message || '';
    if (
      message.includes('sendWithPromise') ||
      message.includes('getPage') ||
      message.includes('Cannot read properties of null')
    ) {
      event.preventDefault();
      return true;
    }
  });

  // Catch unhandled promise rejections from PDF.js
  window.addEventListener('unhandledrejection', (event) => {
    const message = event.reason?.message || event.reason?.toString() || '';
    if (
      message.includes('sendWithPromise') ||
      message.includes('getPage') ||
      message.includes('Cannot read properties of null')
    ) {
      event.preventDefault();
    }
  });
}

interface DocumentViewerProps {
  open: boolean;
  onClose: () => void;
  documentUrl: string | null;
  fileName: string;
  contentType: string;
  isAdmin: boolean;
}

// Repeated watermark pattern for full coverage
function WatermarkPattern() {
  const rows = 5;
  const cols = 3;
  const watermarks: React.ReactNode[] = [];

  for (let i = 0; i < rows * cols; i++) {
    watermarks.push(
      <div
        key={i}
        style={{
          transform: 'rotate(-45deg)',
          fontSize: '14px',
          fontWeight: 'bold',
          color: 'rgba(128, 128, 128, 0.12)',
          whiteSpace: 'nowrap',
          userSelect: 'none',
          textTransform: 'uppercase',
          letterSpacing: '2px',
          padding: '40px 20px',
        }}
      >
        Confidential - Internal Use Only
      </div>
    );
  }

  return (
    <div
      style={{
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        pointerEvents: 'none',
        zIndex: 10,
        display: 'grid',
        gridTemplateColumns: `repeat(${cols}, 1fr)`,
        gridTemplateRows: `repeat(${rows}, 1fr)`,
        alignItems: 'center',
        justifyItems: 'center',
        overflow: 'hidden',
      }}
    >
      {watermarks}
    </div>
  );
}

// Lazy-loaded PDF viewer component
function PDFViewer({
  documentUrl,
  scale,
  onLoadSuccess,
  onLoadError,
  isOpen,
}: {
  documentUrl: string | null;
  scale: number;
  onLoadSuccess: (data: { numPages: number }) => void;
  onLoadError: (error: Error) => void;
  isOpen: boolean;
}) {
  const [pdfComponents, setPdfComponents] = useState<{
    Document: React.ComponentType<{
      file: string | null;
      onLoadSuccess: (data: { numPages: number }) => void;
      onLoadError: (error: Error) => void;
      loading: null;
      options: object;
      children: React.ReactNode;
    }>;
    Page: React.ComponentType<{
      pageNumber: number;
      width?: number;
      renderTextLayer: boolean;
      renderAnnotationLayer: boolean;
      className?: string;
    }>;
    pdfjs: { version: string; GlobalWorkerOptions: { workerSrc: string } };
  } | null>(null);
  const [numPages, setNumPages] = useState<number>(0);
  const [pageWidth, setPageWidth] = useState<number | undefined>(undefined);
  const [pdfLoading, setPdfLoading] = useState(true);
  const mountedRef = useRef(true);
  const documentUrlRef = useRef(documentUrl);

  // Keep the URL ref updated
  useEffect(() => {
    documentUrlRef.current = documentUrl;
  }, [documentUrl]);

  // Track if component is mounted to prevent state updates after unmount
  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  // Reset state when modal closes/opens
  useEffect(() => {
    if (!isOpen) {
      setNumPages(0);
      setPageWidth(undefined);
      setPdfLoading(true);
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen || !documentUrl) return;
    
    let isCancelled = false;
    
    // Dynamically import react-pdf and set up worker
    const loadPdf = async () => {
      try {
        const reactPdf = await import('react-pdf');
        // Import CSS
        await import('react-pdf/dist/Page/AnnotationLayer.css');
        await import('react-pdf/dist/Page/TextLayer.css');
        
        if (isCancelled || !mountedRef.current) return;
        
        // Set up PDF.js worker
        reactPdf.pdfjs.GlobalWorkerOptions.workerSrc = '/pdf.worker.min.mjs';
        
        setPdfComponents({
          Document: reactPdf.Document,
          Page: reactPdf.Page,
          pdfjs: reactPdf.pdfjs,
        });
        setPdfLoading(false);
      } catch (err) {
        if (isCancelled || !mountedRef.current) return;
        console.error('Failed to load PDF library:', err);
        onLoadError(err instanceof Error ? err : new Error('Failed to load PDF library'));
        setPdfLoading(false);
      }
    };
    
    void loadPdf();
    
    return () => {
      isCancelled = true;
    };
  }, [onLoadError, isOpen, documentUrl]);

  const handleDocumentLoadSuccess = useCallback(({ numPages: pages }: { numPages: number }) => {
    if (!mountedRef.current) return;
    setNumPages(pages);
    // Set initial width based on viewport
    const viewportWidth = window.innerWidth * 0.85;
    setPageWidth(Math.min(viewportWidth, 1200));
    onLoadSuccess({ numPages: pages });
  }, [onLoadSuccess]);

  const handleDocumentLoadError = useCallback((error: Error) => {
    if (!mountedRef.current) return;
    // Ignore errors that occur when the component is being destroyed or URL changed
    if (
      error.message?.includes('sendWithPromise') || 
      error.message?.includes('getPage') ||
      error.message?.includes('Cannot read properties of null')
    ) {
      console.warn('PDF viewer closed during loading, ignoring error');
      return;
    }
    onLoadError(error);
  }, [onLoadError]);

  // Don't render anything if not open or URL is empty/null
  if (!isOpen || !documentUrl) {
    return null;
  }

  if (pdfLoading || !pdfComponents) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
        <Spin size="large" tip="Loading PDF viewer..." />
      </div>
    );
  }

  const { Document, Page, pdfjs } = pdfComponents;
  const documentOptions = {
    cMapUrl: `https://unpkg.com/pdfjs-dist@${pdfjs.version}/cmaps/`,
    cMapPacked: true,
    standardFontDataUrl: `https://unpkg.com/pdfjs-dist@${pdfjs.version}/standard_fonts/`,
  };

  return (
    <Document
      file={documentUrl}
      onLoadSuccess={handleDocumentLoadSuccess}
      onLoadError={handleDocumentLoadError}
      loading={null}
      options={documentOptions}
    >
      {Array.from(new Array(numPages), (_, index) => (
        <div
          key={`page_${index + 1}`}
          style={{
            backgroundColor: 'white',
            boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
            marginBottom: 16,
          }}
        >
          <Page
            pageNumber={index + 1}
            width={pageWidth ? pageWidth * scale : undefined}
            renderTextLayer={true}
            renderAnnotationLayer={true}
            className="pdf-page"
          />
        </div>
      ))}
    </Document>
  );
}

export default function DocumentViewer({
  open,
  onClose,
  documentUrl,
  fileName,
  contentType,
  isAdmin,
}: DocumentViewerProps) {
  const [numPages, setNumPages] = useState<number>(0);
  const [scale, setScale] = useState<number>(1.2);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const isPdf = contentType === 'application/pdf' || fileName.toLowerCase().endsWith('.pdf');
  const isImage = contentType.startsWith('image/') || 
    /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(fileName);

  const onDocumentLoadSuccess = useCallback(({ numPages: pages }: { numPages: number }) => {
    setNumPages(pages);
    setLoading(false);
    setError(null);
  }, []);

  const onDocumentLoadError = useCallback((err: Error) => {
    setLoading(false);
    setError(`Failed to load PDF: ${err.message}`);
    console.error('PDF load error:', err);
  }, []);

  const zoomIn = () => {
    setScale((prev) => Math.min(prev + 0.25, 3.0));
  };

  const zoomOut = () => {
    setScale((prev) => Math.max(prev - 0.25, 0.5));
  };

  const handleClose = () => {
    setScale(1.2);
    setLoading(true);
    setError(null);
    setNumPages(0);
    onClose();
  };

  const handleDownload = async () => {
    if (!documentUrl || !isAdmin) return;
    
    try {
      // Fetch the file as a blob to force actual download
      const response = await fetch(documentUrl);
      if (!response.ok) {
        throw new Error('Failed to download file');
      }
      const blob = await response.blob();
      
      // Create a blob URL and trigger download
      const blobUrl = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = blobUrl;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      
      // Clean up the blob URL
      window.URL.revokeObjectURL(blobUrl);
    } catch (err) {
      console.error('Download failed:', err);
      // Fallback: open in new tab if blob download fails
      window.open(documentUrl, '_blank');
    }
  };

  // Disable right-click context menu
  const handleContextMenu = (e: React.MouseEvent) => {
    e.preventDefault();
    return false;
  };

  // Unsupported file type
  if (!isPdf && !isImage) {
    return (
      <Modal
        open={open}
        onCancel={handleClose}
        footer={null}
        title={fileName}
        width={600}
        centered
      >
        <Result
          icon={<FileUnknownOutlined style={{ color: '#faad14' }} />}
          title="Unsupported File Type"
          subTitle="This document type cannot be viewed in the app. Please contact an administrator to request access to this document."
        />
      </Modal>
    );
  }

  return (
    <Modal
      open={open}
      onCancel={handleClose}
      footer={null}
      destroyOnHidden
      title={
        <Flex justify="space-between" align="center" style={{ paddingRight: 32 }}>
          <Text strong ellipsis style={{ maxWidth: 400 }}>
            {fileName}
          </Text>
          <Space>
            {/* Zoom controls for both PDF and images */}
            <Button
              icon={<ZoomOutOutlined />}
              onClick={zoomOut}
              disabled={scale <= 0.5}
              size="small"
            />
            <Text style={{ minWidth: 50, textAlign: 'center' }}>
              {Math.round(scale * 100)}%
            </Text>
            <Button
              icon={<ZoomInOutlined />}
              onClick={zoomIn}
              disabled={scale >= 3.0}
              size="small"
            />
            {isAdmin && (
              <Button
                type="primary"
                icon={<DownloadOutlined />}
                onClick={handleDownload}
                size="small"
              >
                Download
              </Button>
            )}
          </Space>
        </Flex>
      }
      width="95vw"
      style={{ top: 20, maxWidth: 1600 }}
      styles={{
        body: {
          height: '85vh',
          overflow: 'auto',
          padding: 0,
          position: 'relative',
          backgroundColor: '#525659',
        },
      }}
      closeIcon={<CloseOutlined />}
      centered={false}
    >
      <div
        onContextMenu={handleContextMenu}
        style={{
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'flex-start',
          padding: '16px',
          position: 'relative',
          userSelect: 'none',
          overflow: 'auto',
        }}
      >
        {/* Watermark overlay */}
        <WatermarkPattern />

        {isPdf && (
          <>
            {loading && !error && (
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
                <Spin size="large" tip="Loading document..." />
              </div>
            )}
            {error && (
              <Result
                status="error"
                title="Failed to Load Document"
                subTitle={error}
              />
            )}
            <div 
              style={{ 
                display: loading || error ? 'none' : 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: 16,
                paddingBottom: 16,
              }}
            >
              {/* Key ensures clean unmount/remount when URL changes */}
              <PDFViewer
                key={documentUrl || 'no-url'}
                documentUrl={documentUrl}
                scale={scale}
                onLoadSuccess={onDocumentLoadSuccess}
                onLoadError={onDocumentLoadError}
                isOpen={open}
              />
            </div>
            {!loading && !error && numPages > 0 && (
              <Flex
                justify="center"
                align="center"
                gap={16}
                style={{
                  position: 'sticky',
                  bottom: 16,
                  padding: '8px 16px',
                  backgroundColor: 'white',
                  borderRadius: 8,
                  boxShadow: '0 2px 8px rgba(0,0,0,0.15)',
                }}
              >
                <Text>{numPages} {numPages === 1 ? 'page' : 'pages'}</Text>
              </Flex>
            )}
          </>
        )}

        {isImage && (
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              height: '100%',
              width: '100%',
              overflow: 'auto',
            }}
          >
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={documentUrl || ''}
              alt={fileName}
              style={{
                transform: `scale(${scale})`,
                transformOrigin: 'center center',
                transition: 'transform 0.2s ease',
                maxWidth: scale <= 1 ? '100%' : 'none',
                maxHeight: scale <= 1 ? '100%' : 'none',
                objectFit: 'contain',
              }}
              onLoad={() => setLoading(false)}
              onError={() => {
                setLoading(false);
                setError('Failed to load image');
              }}
              draggable={false}
            />
            {loading && (
              <div style={{ position: 'absolute' }}>
                <Spin size="large" tip="Loading image..." />
              </div>
            )}
            {error && (
              <Result
                status="error"
                title="Failed to Load Image"
                subTitle={error}
              />
            )}
          </div>
        )}
      </div>
    </Modal>
  );
}
