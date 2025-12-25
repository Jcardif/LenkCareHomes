'use client';

import React from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { Typography, Card, Breadcrumb, Button, Grid } from 'antd';
import { ArrowLeftOutlined, HomeOutlined, WarningOutlined } from '@ant-design/icons';
import Link from 'next/link';

import { ProtectedRoute, AuthenticatedLayout, IncidentForm } from '@/components';
import type { Incident } from '@/types';

const { Title, Paragraph } = Typography;
const { useBreakpoint } = Grid;

function NewIncidentContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const clientId = searchParams.get('clientId') || undefined;
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const handleSuccess = (incident: Incident) => {
    router.push(`/incidents/${incident.id}`);
  };

  const handleCancel = () => {
    router.back();
  };

  return (
    <div>
      {!isSmallMobile && (
        <Breadcrumb
          style={{ marginBottom: 16 }}
          items={[
            { title: <Link href="/dashboard"><HomeOutlined /></Link> },
            { title: <Link href="/incidents">Incidents</Link> },
            { title: 'New Incident' },
          ]}
        />
      )}

      <Button
        icon={<ArrowLeftOutlined />}
        onClick={() => router.back()}
        style={{ marginBottom: 16, minWidth: 44, minHeight: 44 }}
      >
        {isSmallMobile ? '' : 'Back'}
      </Button>

      <div style={{ marginBottom: 24 }}>
        <Title level={isMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>
          <WarningOutlined style={{ marginRight: 12 }} />
          Report New Incident
        </Title>
        {!isSmallMobile && (
          <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
            Document incident details carefully and completely. All incidents are logged for compliance.
          </Paragraph>
        )}
      </div>

      <Card size={isMobile ? 'small' : 'default'}>
        <IncidentForm
          clientId={clientId}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </Card>
    </div>
  );
}

export default function NewIncidentPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <NewIncidentContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
