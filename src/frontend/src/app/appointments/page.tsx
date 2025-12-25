'use client';

import React from 'react';
import { Typography, Grid } from 'antd';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import AppointmentList from '@/components/appointments/AppointmentList';

const { useBreakpoint } = Grid;
const { Title, Paragraph } = Typography;

function AppointmentsContent() {
  const screens = useBreakpoint();
  const isSmallMobile = !screens.sm;

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>
          Appointments
        </Title>
        {!isSmallMobile && (
          <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
            Schedule and manage client appointments for medical visits, therapies, and other care needs
          </Paragraph>
        )}
      </div>

      <AppointmentList showFilters showCreateButton />
    </div>
  );
}

export default function AppointmentsPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <AppointmentsContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
