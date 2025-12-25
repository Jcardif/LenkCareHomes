'use client';

import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Typography, Breadcrumb, Button, Spin, Alert, Flex, Grid } from 'antd';
import { ArrowLeftOutlined, HomeOutlined, LoadingOutlined } from '@ant-design/icons';
import Link from 'next/link';

const { useBreakpoint } = Grid;

import { ProtectedRoute, AuthenticatedLayout, IncidentForm } from '@/components';
import { incidentsApi, ApiError } from '@/lib/api';
import type { Incident } from '@/types';
import { useAuth } from '@/contexts/AuthContext';

const { Title, Paragraph } = Typography;

function EditIncidentContent() {
  const router = useRouter();
  const params = useParams();
  const incidentId = params.id as string;
  const { user } = useAuth();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const [incident, setIncident] = useState<Incident | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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

  // Only drafts can be edited
  if (incident.status !== 'Draft') {
    return (
      <Alert
        type="warning"
        message="Cannot Edit Incident"
        description="Only draft incidents can be edited. This incident has already been submitted."
        showIcon
        action={
          <Button size="small" onClick={() => router.push(`/incidents/${incidentId}`)}>
            View Incident
          </Button>
        }
      />
    );
  }

  // Only the author can edit
  if (incident.reportedById !== user?.id) {
    return (
      <Alert
        type="error"
        message="Access Denied"
        description="You can only edit incidents that you created."
        showIcon
        action={
          <Button size="small" onClick={() => router.push(`/incidents/${incidentId}`)}>
            View Incident
          </Button>
        }
      />
    );
  }

  return (
    <div>
      {!isMobile && (
        <Breadcrumb
          style={{ marginBottom: 16 }}
          items={[
            { title: <Link href="/dashboard"><HomeOutlined /></Link> },
            { title: <Link href="/incidents">Incidents</Link> },
            { title: <Link href={`/incidents/${incidentId}`}>{incident.incidentNumber}</Link> },
            { title: 'Edit' },
          ]}
        />
      )}

      <Button
        icon={<ArrowLeftOutlined />}
        onClick={() => router.push(`/incidents/${incidentId}`)}
        style={{ marginBottom: 16, minWidth: 44, minHeight: 44 }}
      >
        {isSmallMobile ? 'Back' : 'Back to Incident'}
      </Button>

      <div style={{ marginBottom: 24 }}>
        <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>
          Edit Incident
        </Title>
        {!isSmallMobile && (
          <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
            Update the draft incident report
          </Paragraph>
        )}
      </div>

      <IncidentForm
        incidentId={incidentId}
        incident={incident}
        homeId={incident.homeId}
        clientId={incident.clientId}
      />
    </div>
  );
}

export default function EditIncidentPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <EditIncidentContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
