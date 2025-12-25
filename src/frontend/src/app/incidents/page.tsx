'use client';

import React from 'react';
import { Typography, Grid } from 'antd';
import { ProtectedRoute, AuthenticatedLayout, IncidentList } from '@/components';

const { useBreakpoint } = Grid;

const { Title, Paragraph } = Typography;

function IncidentsContent() {
  const screens = useBreakpoint();
  const isSmallMobile = !screens.sm;

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>Incidents</Title>
        {!isSmallMobile && (
          <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
            View and manage incident reports across all homes
          </Paragraph>
        )}
      </div>

      <IncidentList showFilters showCreateButton />
    </div>
  );
}

export default function IncidentsPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <IncidentsContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
