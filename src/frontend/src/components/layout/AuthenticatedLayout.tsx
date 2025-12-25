'use client';

import React from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { Layout, Menu, Dropdown, Avatar, Typography, Space, Button, Drawer, Grid } from 'antd';
import {
  HomeOutlined,
  TeamOutlined,
  UserOutlined,
  SettingOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  MenuOutlined,
  AuditOutlined,
  SafetyCertificateOutlined,
  AppstoreOutlined,
  ContactsOutlined,
  ExclamationCircleOutlined,
  FileTextOutlined,
  QuestionCircleOutlined,
  FolderOutlined,
  CloseOutlined,
  CalendarOutlined,
} from '@ant-design/icons';
import { useAuth } from '@/contexts/AuthContext';
import type { MenuProps } from 'antd';

const { Header, Sider, Content } = Layout;
const { Text } = Typography;
const { useBreakpoint } = Grid;

interface AuthenticatedLayoutProps {
  children: React.ReactNode;
}

interface NavigationMenuProps {
  mode?: 'inline' | 'vertical';
  selectedKey: string;
  menuItems: MenuProps['items'];
  onMenuClick: MenuProps['onClick'];
}

// Extracted NavigationMenu component
function NavigationMenu({ mode = 'inline', selectedKey, menuItems, onMenuClick }: NavigationMenuProps) {
  return (
    <Menu
      mode={mode}
      theme="dark"
      selectedKeys={[selectedKey]}
      items={menuItems}
      onClick={onMenuClick}
      style={{ 
        border: 0,
        background: 'transparent',
      }}
    />
  );
}

interface LogoProps {
  showText?: boolean;
}

// Extracted Logo component
function Logo({ showText = true }: LogoProps) {
  return (
    <div
      style={{
        height: 64,
        display: 'flex',
        alignItems: 'center',
        padding: showText ? '0 24px' : '0 16px',
        borderBottom: '1px solid rgba(255, 255, 255, 0.08)',
        gap: 12,
      }}
    >
      <div style={{ 
        width: 36, 
        height: 36, 
        borderRadius: 8,
        background: '#FFFFFF',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        flexShrink: 0,
        boxShadow: '0 2px 8px rgba(201, 162, 39, 0.3)',
        overflow: 'hidden',
      }}>
        {/* Inline SVG logo mark matching favicon design */}
        <svg width="32" height="32" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="sidebarGold" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" style={{ stopColor: '#D4AF37' }}/>
              <stop offset="50%" style={{ stopColor: '#C9A227' }}/>
              <stop offset="100%" style={{ stopColor: '#B8860B' }}/>
            </linearGradient>
          </defs>
          <path d="M 4 24 Q 16 30 28 24" stroke="url(#sidebarGold)" strokeWidth="3" strokeLinecap="round" fill="none"/>
          <path d="M 6 17 L 16 7 L 26 17" stroke="url(#sidebarGold)" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" fill="none"/>
          <path d="M 9 16 L 9 22 L 23 22 L 23 16" stroke="url(#sidebarGold)" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" fill="none"/>
          <path d="M 16 20 C 16 20 12.5 17.5 12.5 15 C 12.5 13.5 13.5 12.5 14.75 12.5 C 15.5 12.5 16 13 16 13 C 16 13 16.5 12.5 17.25 12.5 C 18.5 12.5 19.5 13.5 19.5 15 C 19.5 17.5 16 20 16 20 Z" fill="#E85A4F"/>
        </svg>
      </div>
      {showText && (
        <Text strong style={{ fontSize: 17, color: '#ffffff' }}>
          LenkCare
        </Text>
      )}
    </div>
  );
}

interface UserProfileSectionProps {
  user: { firstName?: string; lastName?: string; roles?: string[] } | null;
  getUserInitials: () => string;
}

// Extracted UserProfileSection component
function UserProfileSection({ user, getUserInitials }: UserProfileSectionProps) {
  return (
    <div style={{ 
      padding: '16px 24px', 
      borderTop: '1px solid rgba(255, 255, 255, 0.08)',
      background: 'rgba(0, 0, 0, 0.1)',
    }}>
      <Space style={{ width: '100%' }}>
        <Avatar 
          size={40}
          style={{ 
            backgroundColor: 'rgba(90, 122, 107, 0.3)',
            color: '#ffffff',
            fontWeight: 500,
          }}
        >
          {getUserInitials()}
        </Avatar>
        <div style={{ lineHeight: 1.3 }}>
          <Text style={{ fontSize: 14, fontWeight: 500, color: '#ffffff', display: 'block' }}>
            {user?.firstName} {user?.lastName}
          </Text>
          <Text style={{ fontSize: 12, color: 'rgba(255, 255, 255, 0.65)' }}>
            {user?.roles?.[0] || 'User'}
          </Text>
        </div>
      </Space>
    </div>
  );
}

export default function AuthenticatedLayout({ children }: AuthenticatedLayoutProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { user, logout, hasRole } = useAuth();
  const [collapsed, setCollapsed] = React.useState(false);
  const [mobileDrawerOpen, setMobileDrawerOpen] = React.useState(false);
  
  // Use Ant Design's breakpoint hook for responsive behavior
  const screens = useBreakpoint();
  
  // isMobile: below lg breakpoint (< 992px)
  const isMobile = !screens.lg;

  const handleLogout = async () => {
    await logout();
    router.push('/auth/login');
  };

  // Build menu items based on user role
  const menuItems: MenuProps['items'] = [
    {
      key: '/dashboard',
      icon: <AppstoreOutlined />,
      label: <span data-tour="menu-dashboard">Dashboard</span>,
    },
  ];

  // Admin-only menu items
  if (hasRole('Admin')) {
    menuItems.push(
      {
        key: '/homes',
        icon: <HomeOutlined />,
        label: <span data-tour="menu-homes">Homes</span>,
      },
      {
        key: '/clients',
        icon: <ContactsOutlined />,
        label: <span data-tour="menu-clients">Clients</span>,
      },
      {
        key: '/appointments',
        icon: <CalendarOutlined />,
        label: <span data-tour="menu-appointments">Appointments</span>,
      },
      {
        key: '/caregivers',
        icon: <TeamOutlined />,
        label: <span data-tour="menu-caregivers">Caregivers</span>,
      },
      {
        key: '/documents',
        icon: <FolderOutlined />,
        label: <span data-tour="menu-documents">Documents</span>,
      },
      {
        key: '/incidents',
        icon: <ExclamationCircleOutlined />,
        label: <span data-tour="menu-incidents">Incidents</span>,
      },
      {
        key: '/reports',
        icon: <FileTextOutlined />,
        label: <span data-tour="menu-reports">Reports</span>,
      }
    );
  } else if (hasRole('Caregiver')) {
    // Caregivers can view clients in their assigned homes
    menuItems.push(
      {
        key: '/clients',
        icon: <ContactsOutlined />,
        label: <span data-tour="menu-clients">My Clients</span>,
      },
      {
        key: '/appointments',
        icon: <CalendarOutlined />,
        label: <span data-tour="menu-appointments">Appointments</span>,
      },
      {
        key: '/documents',
        icon: <FolderOutlined />,
        label: <span data-tour="menu-documents">Documents</span>,
      },
      {
        key: '/incidents',
        icon: <ExclamationCircleOutlined />,
        label: <span data-tour="menu-incidents">Incidents</span>,
      }
    );
  }

  // Admin and Sysadmin can view audit logs
  if (hasRole('Admin') || hasRole('Sysadmin')) {
    menuItems.push({
      key: '/audit-logs',
      icon: <AuditOutlined />,
      label: <span data-tour="menu-audit">Audit Logs</span>,
    });
  }

  // Settings (available to all authenticated users)
  menuItems.push({
    key: '/settings',
    icon: <SettingOutlined />,
    label: <span data-tour="menu-settings">Settings</span>,
  });

  // Help section (available to all users)
  menuItems.push({
    key: '/help',
    icon: <QuestionCircleOutlined />,
    label: <span data-tour="menu-help">Help & Docs</span>,
  });

  const userMenuItems: MenuProps['items'] = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: 'My Profile',
      onClick: () => router.push('/settings/profile'),
    },
    {
      key: 'security',
      icon: <SafetyCertificateOutlined />,
      label: 'Security',
      onClick: () => router.push('/settings/security'),
    },
    {
      type: 'divider',
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Sign Out',
      danger: true,
      onClick: handleLogout,
    },
  ];

  const handleMenuClick: MenuProps['onClick'] = (e) => {
    router.push(e.key);
    // Close mobile drawer after navigation
    if (isMobile) {
      setMobileDrawerOpen(false);
    }
  };

  // Determine selected key from pathname
  const selectedKey = '/' + (pathname.split('/')[1] || 'dashboard');

  // Get user initials for avatar
  const getUserInitials = () => {
    if (user?.firstName && user?.lastName) {
      return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
    }
    return user?.email?.[0]?.toUpperCase() || 'U';
  };

  // Props for extracted components
  const navigationMenuProps: NavigationMenuProps = {
    selectedKey,
    menuItems,
    onMenuClick: handleMenuClick,
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {/* Skip links for keyboard navigation (WCAG 2.1 AA) */}
      <a 
        href="#main-content" 
        className="skip-link"
        style={{
          position: 'absolute',
          left: '-10000px',
          top: 'auto',
          width: '1px',
          height: '1px',
          overflow: 'hidden',
          zIndex: 9999,
        }}
        onFocus={(e) => {
          e.currentTarget.style.left = '16px';
          e.currentTarget.style.top = '16px';
          e.currentTarget.style.width = 'auto';
          e.currentTarget.style.height = 'auto';
          e.currentTarget.style.padding = '8px 16px';
          e.currentTarget.style.background = '#5a7a6b';
          e.currentTarget.style.color = '#ffffff';
          e.currentTarget.style.borderRadius = '4px';
          e.currentTarget.style.textDecoration = 'none';
          e.currentTarget.style.fontWeight = '500';
        }}
        onBlur={(e) => {
          e.currentTarget.style.left = '-10000px';
          e.currentTarget.style.width = '1px';
          e.currentTarget.style.height = '1px';
        }}
      >
        Skip to main content
      </a>

      {/* Mobile Navigation Drawer */}
      {isMobile && (
        <Drawer
          placement="left"
          open={mobileDrawerOpen}
          onClose={() => setMobileDrawerOpen(false)}
          className="mobile-nav-drawer"
          styles={{
            body: { padding: 0, background: '#2d3732' },
            header: { background: '#2d3732', borderBottom: '1px solid rgba(255, 255, 255, 0.08)' },
            wrapper: { width: 280 },
          }}
          closeIcon={<CloseOutlined style={{ color: '#ffffff' }} />}
          title={
            <Text strong style={{ color: '#ffffff', fontSize: 17 }}>
              LenkCare
            </Text>
          }
        >
          <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <div style={{ flex: 1, padding: '16px 12px', overflowY: 'auto' }}>
              <NavigationMenu {...navigationMenuProps} />
            </div>
            <UserProfileSection user={user} getUserInitials={getUserInitials} />
          </div>
        </Drawer>
      )}

      {/* Desktop Sidebar - Only render on desktop */}
      {!isMobile && (
        <Sider
          trigger={null}
          collapsible
          collapsed={collapsed}
          width={260}
          collapsedWidth={72}
          style={{
            overflow: 'auto',
            height: '100vh',
            position: 'fixed',
            left: 0,
            top: 0,
            bottom: 0,
            background: '#2d3732',
          }}
          role="navigation"
          aria-label="Main navigation"
        >
          <Logo showText={!collapsed} />
          <div style={{ padding: '16px 12px' }}>
            <NavigationMenu {...navigationMenuProps} />
          </div>
        </Sider>
      )}

      <Layout 
        style={{ 
          marginLeft: isMobile ? 0 : (collapsed ? 72 : 260), 
          transition: 'all 0.2s ease', 
          background: '#f8f9fa' 
        }}
      >
        <Header
          style={{
            padding: isMobile ? '0 16px' : '0 32px',
            background: '#ffffff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            borderBottom: '1px solid #ebeeed',
            position: 'sticky',
            top: 0,
            zIndex: 10,
            height: 64,
          }}
          role="banner"
        >
          <Button
            type="text"
            icon={
              isMobile 
                ? <MenuOutlined aria-hidden="true" />
                : (collapsed ? <MenuUnfoldOutlined aria-hidden="true" /> : <MenuFoldOutlined aria-hidden="true" />)
            }
            onClick={() => isMobile ? setMobileDrawerOpen(true) : setCollapsed(!collapsed)}
            style={{ 
              fontSize: 16,
              width: 44,
              height: 44,
              color: '#6b7770',
            }}
            aria-label={
              isMobile 
                ? 'Open navigation menu'
                : (collapsed ? 'Expand navigation menu' : 'Collapse navigation menu')
            }
            aria-expanded={isMobile ? mobileDrawerOpen : !collapsed}
          />

          <Dropdown menu={{ items: userMenuItems }} placement="bottomRight" trigger={['click']}>
            <Space 
              data-tour="user-menu"
              style={{ cursor: 'pointer', padding: '4px 8px', borderRadius: 8, minHeight: 44 }}
              role="button"
              aria-label={`User menu for ${user?.firstName} ${user?.lastName}`}
              tabIndex={0}
            >
              <Avatar 
                size={36}
                style={{ 
                  backgroundColor: 'rgba(90, 122, 107, 0.1)',
                  color: '#5a7a6b',
                  fontWeight: 500,
                  fontSize: 14,
                }}
                aria-hidden="true"
              >
                {getUserInitials()}
              </Avatar>
              {/* Hide name/role on mobile to save space */}
              {!isMobile && (
                <div style={{ lineHeight: 1.3, textAlign: 'left' }}>
                  <Text style={{ fontSize: 14, fontWeight: 500, color: '#2d3732', display: 'block' }}>
                    {user?.firstName} {user?.lastName}
                  </Text>
                  <Text style={{ fontSize: 12, color: '#6b7770' }}>
                    {user?.roles?.[0] || 'User'}
                  </Text>
                </div>
              )}
            </Space>
          </Dropdown>
        </Header>

        <Content
          id="main-content"
          style={{
            margin: isMobile ? 8 : (screens.md ? 16 : 24),
            padding: isMobile ? 16 : (screens.md ? 24 : 32),
            background: '#ffffff',
            borderRadius: isMobile ? 8 : 12,
            minHeight: 280,
            boxShadow: '0 4px 20px rgba(45, 55, 50, 0.06)',
            border: '1px solid #ebeeed',
          }}
          role="main"
          aria-label="Main content area"
          tabIndex={-1}
        >
          {children}
        </Content>
      </Layout>
    </Layout>
  );
}
