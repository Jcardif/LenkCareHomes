'use client';

import React, { use } from 'react';
import { Typography, Grid, Breadcrumb } from 'antd';
import { HomeOutlined, CalendarOutlined } from '@ant-design/icons';
import Link from 'next/link';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import AppointmentForm from '@/components/appointments/AppointmentForm';

const { useBreakpoint } = Grid;
const { Title } = Typography;

interface PageProps {
  searchParams: Promise<{ clientId?: string }>;
}

function NewAppointmentContent({ searchParams }: PageProps) {
  const params = use(searchParams);
  const screens = useBreakpoint();
  const isSmallMobile = !screens.sm;

  return (
    <div>
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <Link href="/dashboard"><HomeOutlined /></Link> },
          { title: <Link href="/appointments">Appointments</Link> },
          { title: 'New Appointment' },
        ]}
      />

      <div style={{ marginBottom: 24 }}>
        <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>
          <CalendarOutlined style={{ marginRight: 12 }} />
          Schedule Appointment
        </Title>
      </div>

      <AppointmentForm clientId={params.clientId} />
    </div>
  );
}

export default function NewAppointmentPage(props: PageProps) {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <NewAppointmentContent {...props} />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
