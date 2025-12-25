'use client';

import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Typography, Breadcrumb, Button, Spin, Alert, Flex, Grid, message } from 'antd';
import { ArrowLeftOutlined, HomeOutlined, LoadingOutlined, FilePdfOutlined } from '@ant-design/icons';
import Link from 'next/link';

import { ProtectedRoute, AuthenticatedLayout, IncidentDetail } from '@/components';
import { incidentsApi, ApiError } from '@/lib/api';
import type { Incident } from '@/types';

const { Title } = Typography;
const { useBreakpoint } = Grid;

function IncidentDetailContent() {
  const router = useRouter();
  const params = useParams();
  const incidentId = params.id as string;
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [incident, setIncident] = useState<Incident | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  const handleExportPdf = async () => {
    if (!incident) return;
    try {
      setExporting(true);
      const blob = await incidentsApi.exportPdf(incident.id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Incident-${incident.incidentNumber}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      void message.success('PDF exported successfully');
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to export PDF';
      void message.error(msg);
    } finally {
      setExporting(false);
    }
  };

  const fetchIncident = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await incidentsApi.getById(incidentId);
      setIncident(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load incident';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [incidentId]);

  useEffect(() => {
    void fetchIncident();
  }, [fetchIncident]);

  const handleUpdate = (updatedIncident: Incident) => {
    setIncident(updatedIncident);
  };

  if (loading) {
    return (
      <Flex justify="center" align="center" style={{ minHeight: 300 }}>
        <Spin indicator={<LoadingOutlined style={{ fontSize: 48 }} spin />} tip="Loading incident..." />
      </Flex>
    );
  }

  if (error || !incident) {
    return (
      <Alert
        type="error"
        message="Error Loading Incident"
        description={error || 'Incident not found'}
        showIcon
        action={
          <Button size="small" onClick={() => router.push('/incidents')}>
            Back to Incidents
          </Button>
        }
      />
    );
  }

  return (
    <div>
      {!isSmallMobile && (
        <Breadcrumb
          style={{ marginBottom: 16 }}
          items={[
            { title: <Link href="/dashboard"><HomeOutlined /></Link> },
            { title: <Link href="/incidents">Incidents</Link> },
            { title: incident.incidentNumber },
          ]}
        />
      )}

      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }} wrap="wrap" gap={8}>
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => router.push('/incidents')}
          style={{ minWidth: 44, minHeight: 44 }}
        >
          {isSmallMobile ? '' : 'Back to Incidents'}
        </Button>
        
        {incident.status !== 'Draft' && (
          <Button
            icon={<FilePdfOutlined />}
            onClick={() => void handleExportPdf()}
            loading={exporting}
            style={{ minHeight: 44 }}
          >
            {isSmallMobile ? '' : 'Export PDF'}
          </Button>
        )}
      </Flex>

      <div style={{ marginBottom: 24 }}>
        <Title level={isMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>
          Incident {incident.incidentNumber}
        </Title>
      </div>

      <IncidentDetail incident={incident} onUpdate={handleUpdate} />
    </div>
  );
}

export default function IncidentDetailPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <IncidentDetailContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
