'use client';

import React, { useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { Spin } from 'antd';
import { useAuth } from '@/contexts/AuthContext';
import type { UserRole } from '@/types';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRoles?: UserRole[];
  requireAnyRole?: boolean;
}

/**
 * Higher-order component that protects routes by requiring authentication
 * and optionally specific roles.
 */
export default function ProtectedRoute({
  children,
  requiredRoles,
  requireAnyRole = true,
}: ProtectedRouteProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { isLoading, isAuthenticated, hasRole, hasAnyRole } = useAuth();

  useEffect(() => {
    if (isLoading) return;

    // If not authenticated, redirect to login
    if (!isAuthenticated) {
      router.push(`/auth/login?redirect=${encodeURIComponent(pathname)}`);
      return;
    }

    // If roles are required, check them
    if (requiredRoles && requiredRoles.length > 0) {
      const hasAccess = requireAnyRole
        ? hasAnyRole(requiredRoles)
        : requiredRoles.every((role) => hasRole(role));

      if (!hasAccess) {
        // Redirect to unauthorized page or dashboard
        router.push('/unauthorized');
      }
    }
  }, [isLoading, isAuthenticated, requiredRoles, requireAnyRole, hasRole, hasAnyRole, router, pathname]);

  // Show loading spinner while checking authentication
  if (isLoading) {
    return (
      <Spin
        size="large"
        tip="Loading..."
        fullscreen
      />
    );
  }

  // If not authenticated, show nothing (redirect will happen)
  if (!isAuthenticated) {
    return null;
  }

  // If roles are required and user doesn't have them, show nothing
  if (requiredRoles && requiredRoles.length > 0) {
    const hasAccess = requireAnyRole
      ? hasAnyRole(requiredRoles)
      : requiredRoles.every((role) => hasRole(role));

    if (!hasAccess) {
      return null;
    }
  }

  return <>{children}</>;
}
