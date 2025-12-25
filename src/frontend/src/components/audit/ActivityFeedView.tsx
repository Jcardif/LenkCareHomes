'use client';

import React from 'react';
import {
  Typography,
  Card,
  Timeline,
  Tag,
  Space,
  Avatar,
  Empty,
  Spin,
  Alert,
  Tooltip,
} from 'antd';
import {
  UserOutlined,
  LoginOutlined,
  LogoutOutlined,
  FileTextOutlined,
  EditOutlined,
  EyeOutlined,
  SafetyOutlined,
  WarningOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  TeamOutlined,
  HomeOutlined,
  MedicineBoxOutlined,
  FileAddOutlined,
  DeleteOutlined,
  KeyOutlined,
  MailOutlined,
  ExclamationCircleOutlined,
} from '@ant-design/icons';
import type { AuditLogEntry } from '@/types';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import calendar from 'dayjs/plugin/calendar';

dayjs.extend(relativeTime);
dayjs.extend(calendar);

const { Text, Title } = Typography;

interface ActivityFeedViewProps {
  logs: AuditLogEntry[];
  loading: boolean;
  error: string | null;
  hasMore: boolean;
  onLoadMore: () => void;
}

// Human-friendly action descriptions
const getActivityDescription = (log: AuditLogEntry): { icon: React.ReactNode; text: string; color: string; isAlert?: boolean } => {
  const userName = log.userEmail?.split('@')[0] || 'Someone';
  const resourceName = log.resourceId ? `#${log.resourceId.slice(0, 8)}` : '';
  
  switch (log.action) {
    // Authentication events
    case 'LOGIN_SUCCESS':
      return {
        icon: <LoginOutlined />,
        text: `${userName} signed in`,
        color: 'green',
      };
    case 'LOGIN_FAILED':
      return {
        icon: <CloseCircleOutlined />,
        text: `Failed sign-in attempt for ${log.userEmail || 'unknown user'}`,
        color: 'red',
        isAlert: true,
      };
    case 'LOGOUT':
      return {
        icon: <LogoutOutlined />,
        text: `${userName} signed out`,
        color: 'default',
      };
    case 'MFA_SETUP':
      return {
        icon: <SafetyOutlined />,
        text: `${userName} set up two-factor authentication`,
        color: 'blue',
      };
    case 'MFA_VERIFIED':
      return {
        icon: <CheckCircleOutlined />,
        text: `${userName} verified two-factor code`,
        color: 'green',
      };
    case 'MFA_FAILED':
      return {
        icon: <WarningOutlined />,
        text: `${userName} entered incorrect two-factor code`,
        color: 'orange',
        isAlert: true,
      };
    case 'BACKUP_CODE_USED':
      return {
        icon: <KeyOutlined />,
        text: `${userName} used a backup code to sign in`,
        color: 'orange',
        isAlert: true,
      };
    case 'PASSWORD_RESET':
      return {
        icon: <KeyOutlined />,
        text: `${userName} reset their password`,
        color: 'purple',
      };
    case 'PASSWORD_RESET_REQUESTED':
      return {
        icon: <MailOutlined />,
        text: `Password reset requested for ${log.userEmail || 'a user'}`,
        color: 'purple',
      };

    // User management
    case 'USER_INVITED':
      return {
        icon: <MailOutlined />,
        text: `${userName} invited a new user`,
        color: 'cyan',
      };
    case 'INVITATION_ACCEPTED':
      return {
        icon: <CheckCircleOutlined />,
        text: `${userName} accepted their invitation`,
        color: 'green',
      };
    case 'ACCOUNT_SETUP_COMPLETED':
      return {
        icon: <CheckCircleOutlined />,
        text: `${userName} completed account setup`,
        color: 'green',
      };
    case 'USER_CREATED':
      return {
        icon: <UserOutlined />,
        text: `${userName} created a new user account`,
        color: 'cyan',
      };
    case 'USER_UPDATED':
      return {
        icon: <EditOutlined />,
        text: `${userName} updated a user's information`,
        color: 'blue',
      };
    case 'USER_DEACTIVATED':
      return {
        icon: <UserOutlined />,
        text: `${userName} deactivated a user account`,
        color: 'orange',
        isAlert: true,
      };
    case 'USER_REACTIVATED':
      return {
        icon: <UserOutlined />,
        text: `${userName} reactivated a user account`,
        color: 'green',
      };
    case 'USER_DELETED':
      return {
        icon: <DeleteOutlined />,
        text: `${userName} deleted a user account`,
        color: 'red',
        isAlert: true,
      };
    case 'ROLE_ASSIGNED':
      return {
        icon: <SafetyOutlined />,
        text: `${userName} assigned a role to a user`,
        color: 'blue',
      };
    case 'ROLE_REMOVED':
      return {
        icon: <SafetyOutlined />,
        text: `${userName} removed a role from a user`,
        color: 'orange',
      };

    // PHI/Client access
    case 'PHI_ACCESSED':
      return {
        icon: <EyeOutlined />,
        text: `${userName} viewed protected health information`,
        color: 'gold',
      };
    case 'PHI_MODIFIED':
      return {
        icon: <EditOutlined />,
        text: `${userName} modified protected health information`,
        color: 'volcano',
      };
    case 'CLIENT_VIEWED':
      return {
        icon: <EyeOutlined />,
        text: `${userName} viewed a client's record ${resourceName}`,
        color: 'cyan',
      };
    case 'CLIENT_ADMITTED':
      return {
        icon: <HomeOutlined />,
        text: `${userName} admitted a new client ${resourceName}`,
        color: 'green',
      };
    case 'CLIENT_DISCHARGED':
      return {
        icon: <HomeOutlined />,
        text: `${userName} discharged a client ${resourceName}`,
        color: 'orange',
      };
    case 'CLIENT_UPDATED':
      return {
        icon: <EditOutlined />,
        text: `${userName} updated a client's information ${resourceName}`,
        color: 'blue',
      };

    // Documents
    case 'DOCUMENT_VIEWED':
      return {
        icon: <FileTextOutlined />,
        text: `${userName} viewed a document ${resourceName}`,
        color: 'cyan',
      };
    case 'DOCUMENT_UPLOADED':
      return {
        icon: <FileAddOutlined />,
        text: `${userName} uploaded a document ${resourceName}`,
        color: 'blue',
      };
    case 'DOCUMENT_DELETED':
      return {
        icon: <DeleteOutlined />,
        text: `${userName} deleted a document ${resourceName}`,
        color: 'red',
        isAlert: true,
      };
    case 'DOCUMENT_ACCESS_GRANTED':
      return {
        icon: <CheckCircleOutlined />,
        text: `${userName} granted document access`,
        color: 'green',
      };
    case 'DOCUMENT_ACCESS_REVOKED':
      return {
        icon: <CloseCircleOutlined />,
        text: `${userName} revoked document access`,
        color: 'orange',
      };

    // Incidents
    case 'INCIDENT_CREATED':
      return {
        icon: <ExclamationCircleOutlined />,
        text: `${userName} reported an incident ${resourceName}`,
        color: 'red',
        isAlert: true,
      };
    case 'INCIDENT_UPDATED':
      return {
        icon: <EditOutlined />,
        text: `${userName} updated an incident report ${resourceName}`,
        color: 'orange',
      };
    case 'INCIDENT_SUBMITTED':
      return {
        icon: <CheckCircleOutlined />,
        text: `${userName} submitted an incident for review ${resourceName}`,
        color: 'blue',
      };

    // Care activities
    case 'ADL_LOGGED':
      return {
        icon: <MedicineBoxOutlined />,
        text: `${userName} logged a care activity`,
        color: 'green',
      };
    case 'VITALS_LOGGED':
      return {
        icon: <MedicineBoxOutlined />,
        text: `${userName} recorded vital signs`,
        color: 'green',
      };

    // API operations - simplify for admins
    case 'API_READ':
      return {
        icon: <EyeOutlined />,
        text: `${userName} viewed ${log.resourceType || 'data'}`,
        color: 'default',
      };
    case 'API_CREATE':
      return {
        icon: <FileAddOutlined />,
        text: `${userName} created a new ${log.resourceType || 'record'}`,
        color: 'blue',
      };
    case 'API_UPDATE':
      return {
        icon: <EditOutlined />,
        text: `${userName} updated ${log.resourceType || 'a record'}`,
        color: 'orange',
      };
    case 'API_DELETE':
      return {
        icon: <DeleteOutlined />,
        text: `${userName} deleted ${log.resourceType || 'a record'}`,
        color: 'red',
        isAlert: true,
      };
    case 'API_REQUEST':
      return {
        icon: <EyeOutlined />,
        text: `${userName} accessed the system`,
        color: 'default',
      };

    // Homes
    case 'USER_MANAGEMENT':
      return {
        icon: <TeamOutlined />,
        text: `${userName} managed user settings`,
        color: 'purple',
      };

    default:
      return {
        icon: <EyeOutlined />,
        text: `${userName} performed an action: ${log.action.replace(/_/g, ' ').toLowerCase()}`,
        color: 'default',
      };
  }
};

// Get outcome badge
const getOutcomeBadge = (outcome: string) => {
  switch (outcome) {
    case 'Success':
      return null; // Don't show badge for success - it's the norm
    case 'Failure':
      return <Tag color="red" style={{ marginLeft: 8 }}>Failed</Tag>;
    case 'Denied':
      return <Tag color="orange" style={{ marginLeft: 8 }}>Access Denied</Tag>;
    default:
      return null;
  }
};

// Group logs by date
const groupLogsByDate = (logs: AuditLogEntry[]): Map<string, AuditLogEntry[]> => {
  const groups = new Map<string, AuditLogEntry[]>();
  
  logs.forEach(log => {
    const date = dayjs(log.timestamp).format('YYYY-MM-DD');
    const existing = groups.get(date) || [];
    existing.push(log);
    groups.set(date, existing);
  });
  
  return groups;
};

// Format date header
const formatDateHeader = (dateStr: string): string => {
  const date = dayjs(dateStr);
  const today = dayjs().startOf('day');
  const yesterday = today.subtract(1, 'day');
  
  if (date.isSame(today, 'day')) {
    return 'Today';
  } else if (date.isSame(yesterday, 'day')) {
    return 'Yesterday';
  } else if (date.isAfter(today.subtract(7, 'day'))) {
    return date.format('dddd'); // Day name
  } else {
    return date.format('MMMM D, YYYY');
  }
};

export function ActivityFeedView({ logs, loading, error, hasMore, onLoadMore }: ActivityFeedViewProps) {
  if (loading && logs.length === 0) {
    return (
      <Card>
        <div style={{ textAlign: 'center', padding: 60 }}>
          <Spin size="large" />
          <div style={{ marginTop: 16, color: '#6b7770' }}>Loading activity...</div>
        </div>
      </Card>
    );
  }

  if (error) {
    return (
      <Alert
        message="Unable to load activity"
        description={error}
        type="error"
        showIcon
      />
    );
  }

  if (logs.length === 0) {
    return (
      <Card>
        <Empty
          description={
            <span style={{ color: '#6b7770' }}>
              No activity to show yet. Activity will appear here as users interact with the system.
            </span>
          }
        />
      </Card>
    );
  }

  const groupedLogs = groupLogsByDate(logs);
  const dateGroups = Array.from(groupedLogs.entries());

  return (
    <div role="feed" aria-label="Activity feed">
      {dateGroups.map(([dateStr, dateLogs]) => (
        <Card key={dateStr} style={{ marginBottom: 16 }}>
          <Title level={5} style={{ marginBottom: 16, color: '#2d3732' }}>
            {formatDateHeader(dateStr)}
            <Text type="secondary" style={{ fontWeight: 'normal', marginLeft: 8 }}>
              · {dayjs(dateStr).format('dddd, MMMM D, YYYY')}
            </Text>
          </Title>
          
          <Timeline
            items={dateLogs.map((log) => {
              const activity = getActivityDescription(log);
              const time = dayjs(log.timestamp).format('h:mm A');
              
              return {
                key: log.id,
                color: activity.color,
                dot: activity.isAlert ? (
                  <Avatar 
                    size="small" 
                    style={{ 
                      backgroundColor: activity.color === 'red' ? '#ff4d4f' : 
                                       activity.color === 'orange' ? '#fa8c16' : '#1890ff'
                    }}
                    icon={activity.icon}
                  />
                ) : undefined,
                children: (
                  <div 
                    style={{ 
                      padding: activity.isAlert ? '8px 12px' : '4px 0',
                      backgroundColor: activity.isAlert ? 
                        (activity.color === 'red' ? '#fff2f0' : '#fff7e6') : 'transparent',
                      borderRadius: 6,
                      marginLeft: activity.isAlert ? -12 : 0,
                      marginRight: activity.isAlert ? -12 : 0,
                    }}
                    role="article"
                    aria-label={`${time}: ${activity.text}`}
                  >
                    <Space align="start">
                      {!activity.isAlert && (
                        <Avatar 
                          size="small" 
                          style={{ backgroundColor: '#f0f0f0', color: '#666' }}
                          icon={activity.icon}
                        />
                      )}
                      <div>
                        <div>
                          <Text strong={activity.isAlert}>
                            {activity.text}
                          </Text>
                          {getOutcomeBadge(log.outcome)}
                        </div>
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          {time}
                          {log.outcome === 'Failure' && log.details && (
                            <Tooltip title={log.details}>
                              <Text type="secondary" style={{ marginLeft: 8, cursor: 'help' }}>
                                · View reason
                              </Text>
                            </Tooltip>
                          )}
                        </Text>
                      </div>
                    </Space>
                  </div>
                ),
              };
            })}
          />
        </Card>
      ))}
      
      {hasMore && (
        <div style={{ textAlign: 'center', marginTop: 16 }}>
          <a 
            onClick={onLoadMore} 
            style={{ color: '#5a7a6b', cursor: 'pointer' }}
            role="button"
            tabIndex={0}
            onKeyPress={(e) => e.key === 'Enter' && onLoadMore()}
          >
            {loading ? 'Loading...' : 'Load earlier activity'}
          </a>
        </div>
      )}
    </div>
  );
}
