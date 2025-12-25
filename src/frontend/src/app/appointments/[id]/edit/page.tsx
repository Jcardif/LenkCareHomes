'use client';

import React, { useState, useEffect, useCallback, use } from 'react';
import { Card, Typography, Breadcrumb, Spin, message } from 'antd';
import { HomeOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import Link from 'next/link';

import { ProtectedRoute, AuthenticatedLayout, AppointmentForm } from '@/components';
import { appointmentsApi, ApiError } from '@/lib/api';
import type { Appointment } from '@/types';

const { Title } = Typography;

interface PageProps {
  params: Promise<{ id: string }>;
}

function EditAppointmentContent({ params }: PageProps) {
  const { id } = use(params);
  const router = useRouter();

  const [appointment, setAppointment] = useState<Appointment | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchAppointment = useCallback(async () => {
    try {
      setLoading(true);
      const data = await appointmentsApi.getById(id);
      
      // Can only edit scheduled appointments
      if (data.status !== 'Scheduled') {
        void message.error('Only scheduled appointments can be edited');
        router.push(`/appointments/${id}`);
        return;
      }
      
      setAppointment(data);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Failed to load appointment';
      void message.error(msg);
      router.push('/appointments');
    } finally {
      setLoading(false);
    }
  }, [id, router]);

  useEffect(() => {
    void fetchAppointment();
  }, [fetchAppointment]);

  if (loading) {
    return (
      <Card>
        <div style={{ textAlign: 'center', padding: 48 }}>
          <Spin size="large" />
          <div style={{ marginTop: 16 }}>Loading appointment...</div>
        </div>
      </Card>
    );
  }

  if (!appointment) {
    return null;
  }

  return (
    <div>
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <Link href="/dashboard"><HomeOutlined /></Link> },
          { title: <Link href="/appointments">Appointments</Link> },
          { title: <Link href={`/appointments/${id}`}>{appointment.title}</Link> },
          { title: 'Edit' },
        ]}
      />

      <Card>
        <Title level={3} style={{ marginBottom: 24 }}>
          Edit Appointment
        </Title>
        <AppointmentForm mode="edit" appointment={appointment} />
      </Card>
    </div>
  );
}

export default function EditAppointmentPage(props: PageProps) {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Caregiver']}>
      <AuthenticatedLayout>
        <EditAppointmentContent {...props} />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
