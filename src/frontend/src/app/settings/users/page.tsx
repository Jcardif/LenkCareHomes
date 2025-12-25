'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Typography,
  Card,
  Table,
  Button,
  Space,
  Tag,
  Modal,
  Form,
  Input,
  Select,
  message,
  Tooltip,
  Popconfirm,
  Flex,
  Alert,
  Grid,
  Dropdown,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  StopOutlined,
  CheckCircleOutlined,
  ArrowLeftOutlined,
  UserOutlined,
  ReloadOutlined,
  SearchOutlined,
  KeyOutlined,
  MoreOutlined,
  MailOutlined,
} from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { useAuth } from '@/contexts/AuthContext';
import { usersApi, getUserFriendlyError } from '@/lib/api';
import type { User, UserRole, InviteUserRequest, UpdateUserRequest } from '@/types';
import type { MfaResetRequest } from '@/types/passkey';
import type { ColumnsType } from 'antd/es/table';
import type { MenuProps } from 'antd';

const { Title, Paragraph, Text } = Typography;
const { TextArea } = Input;
const { useBreakpoint } = Grid;

function UsersContent() {
  const router = useRouter();
  const { user: currentUser, hasAnyRole, hasRole } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [inviteModalOpen, setInviteModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [inviteForm] = Form.useForm();
  const [editForm] = Form.useForm();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  // MFA Reset state
  const [mfaResetModalOpen, setMfaResetModalOpen] = useState(false);
  const [mfaResetUser, setMfaResetUser] = useState<User | null>(null);
  const [mfaResetForm] = Form.useForm();
  const [mfaResetLoading, setMfaResetLoading] = useState(false);

  const canManageUsers = hasAnyRole(['Admin', 'Sysadmin']);
  const isSysadmin = hasRole('Sysadmin');

  // Filter users based on search query
  const filteredUsers = users.filter((user) => {
    if (!searchQuery.trim()) return true;
    const query = searchQuery.toLowerCase();
    const fullName = `${user.firstName} ${user.lastName}`.toLowerCase();
    return (
      fullName.includes(query) ||
      user.firstName.toLowerCase().includes(query) ||
      user.lastName.toLowerCase().includes(query) ||
      user.email.toLowerCase().includes(query)
    );
  });

  const fetchUsers = useCallback(async () => {
    try {
      setLoading(true);
      const data = await usersApi.getAll();
      setUsers(data);
    } catch (error) {
      message.error(getUserFriendlyError(error, 'Unable to load users. Please try again.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (canManageUsers) {
      fetchUsers();
    }
  }, [canManageUsers, fetchUsers]);

  const handleInviteUser = async (values: InviteUserRequest) => {
    try {
      setActionLoading('invite');
      const response = await usersApi.invite(values);
      if (response.success) {
        message.success('Invitation sent successfully');
        setInviteModalOpen(false);
        inviteForm.resetFields();
        fetchUsers();
      } else {
        message.error('Unable to send invitation. Please try again.');
      }
    } catch (error) {
      message.error(getUserFriendlyError(error, 'Unable to send invitation. Please try again.'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleEditUser = async (values: UpdateUserRequest) => {
    if (!selectedUser) return;

    try {
      setActionLoading('edit');
      await usersApi.update(selectedUser.id, values);
      message.success('User updated successfully');
      setEditModalOpen(false);
      editForm.resetFields();
      setSelectedUser(null);
      fetchUsers();
    } catch (error) {
      message.error(getUserFriendlyError(error, 'Unable to update user. Please try again.'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleDeactivate = async (userId: string) => {
    try {
      setActionLoading(userId);
      await usersApi.deactivate(userId);
      message.success('User deactivated');
      fetchUsers();
    } catch (error) {
      message.error(getUserFriendlyError(error, 'Unable to deactivate user. Please try again.'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleReactivate = async (userId: string) => {
    try {
      setActionLoading(userId);
      await usersApi.reactivate(userId);
      message.success('User reactivated');
      fetchUsers();
    } catch (error) {
      message.error(getUserFriendlyError(error, 'Unable to reactivate user. Please try again.'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleDelete = async (userId: string) => {
    try {
      setActionLoading(userId);
      await usersApi.delete(userId);
      message.success('User deleted');
      fetchUsers();
    } catch (error) {
      message.error(getUserFriendlyError(error, 'Unable to delete user. Please try again.'));
    } finally {
      setActionLoading(null);
    }
  };

  const openEditModal = (user: User) => {
    setSelectedUser(user);
    editForm.setFieldsValue({
      firstName: user.firstName,
      lastName: user.lastName,
    });
    setEditModalOpen(true);
  };

  // MFA Reset handlers
  const openMfaResetModal = (user: User) => {
    setMfaResetUser(user);
    mfaResetForm.resetFields();
    setMfaResetModalOpen(true);
  };

  const handleResendInvitation = async (userId: string, userName: string) => {
    setActionLoading(userId);
    try {
      const response = await usersApi.resendInvitation(userId);
      if (response.success) {
        message.success(`Invitation email resent to ${userName}.`);
      } else {
        message.error(response.error || 'Unable to resend invitation.');
      }
    } catch (error) {
      message.error(getUserFriendlyError(error, 'Unable to resend invitation. Please try again.'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleMfaReset = async (values: { reason: string; verificationMethod: string; notes?: string }) => {
    if (!mfaResetUser) return;

    const request: MfaResetRequest = {
      userId: mfaResetUser.id,
      reason: values.reason,
      verificationMethod: values.verificationMethod,
      notes: values.notes,
    };

    try {
      setMfaResetLoading(true);
      const response = await usersApi.resetMfa(request);
      
      if (response.success) {
        message.success(
          `Sign-in credentials reset for ${mfaResetUser.firstName} ${mfaResetUser.lastName}. ` +
          `They will need to set up a new passkey.`
        );
        setMfaResetModalOpen(false);
        setMfaResetUser(null);
        mfaResetForm.resetFields();
        fetchUsers();
      } else {
        message.error('Unable to reset credentials. Please try again.');
      }
    } catch (error) {
      message.error(getUserFriendlyError(error, 'Unable to reset credentials. Please try again.'));
    } finally {
      setMfaResetLoading(false);
    }
  };

  const getRoleColor = (role: UserRole): string => {
    switch (role) {
      case 'Admin':
        return '#5a7a6b';
      case 'Sysadmin':
        return '#7a5a6b';
      case 'Caregiver':
        return '#5a6b7a';
      default:
        return '#6b7770';
    }
  };

  const getStatusTag = (user: User) => {
    if (!user.isActive) {
      return <Tag color="error">Inactive</Tag>;
    }
    if (!user.invitationAccepted) {
      return <Tag color="warning">Pending</Tag>;
    }
    if (!user.isMfaSetupComplete) {
      return <Tag color="processing">MFA Pending</Tag>;
    }
    return <Tag color="success">Active</Tag>;
  };

  const columns: ColumnsType<User> = [
    {
      title: 'Name',
      key: 'name',
      render: (_, record) => (
        <Flex align="center" gap={12}>
          <div
            style={{
              width: 36,
              height: 36,
              borderRadius: '50%',
              background: 'rgba(90, 122, 107, 0.1)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            <UserOutlined style={{ color: '#5a7a6b' }} />
          </div>
          <div style={{ minWidth: 0 }}>
            <Text strong style={{ color: '#2d3732', display: 'block' }}>
              {record.firstName} {record.lastName}
            </Text>
            <Text style={{ color: '#6b7770', fontSize: 13 }}>{record.email}</Text>
          </div>
        </Flex>
      ),
    },
    {
      title: 'Role',
      key: 'roles',
      responsive: ['sm'],
      render: (_, record) => (
        <Space size={4}>
          {record.roles.map((role) => (
            <Tag key={role} color={getRoleColor(role)}>
              {role}
            </Tag>
          ))}
        </Space>
      ),
    },
    {
      title: 'Status',
      key: 'status',
      responsive: ['md'],
      render: (_, record) => getStatusTag(record),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: isMobile ? 60 : 220,
      render: (_, record) => {
        const isCurrentUser = record.id === currentUser?.id;
        const isLoading = actionLoading === record.id;
        const isAdmin = hasRole('Admin');
        const canResetMfa = isSysadmin && !isCurrentUser && record.isMfaSetupComplete;
        const canResendInvitation = !record.invitationAccepted && record.isActive;
        // Sysadmin cannot edit user data (PHI protection)
        const canEdit = isAdmin || isCurrentUser;
        // Both Admin and Sysadmin can deactivate/reactivate users
        const canDeactivate = !isCurrentUser;
        // Only Admin can delete users permanently
        const canDelete = isAdmin && !isCurrentUser;

        if (isMobile) {
          const menuItems: MenuProps['items'] = [
            ...(canEdit ? [{
              key: 'edit',
              icon: <EditOutlined />,
              label: 'Edit',
              onClick: () => openEditModal(record),
            }] : []),
            ...(canResendInvitation ? [{
              key: 'resend-invitation',
              icon: <MailOutlined />,
              label: 'Resend Invitation',
              onClick: () => handleResendInvitation(record.id, `${record.firstName} ${record.lastName}`),
            }] : []),
            ...(canResetMfa ? [{
              key: 'reset-mfa',
              icon: <KeyOutlined />,
              label: 'Reset MFA',
              onClick: () => openMfaResetModal(record),
            }] : []),
            ...(record.isActive && canDeactivate ? [{
              key: 'deactivate',
              icon: <StopOutlined />,
              label: 'Deactivate',
              onClick: () => handleDeactivate(record.id),
            }] : []),
            ...(!record.isActive && canDeactivate ? [{
              key: 'reactivate',
              icon: <CheckCircleOutlined />,
              label: 'Reactivate',
              onClick: () => handleReactivate(record.id),
            }] : []),
            ...(canDelete ? [{
              key: 'delete',
              icon: <DeleteOutlined />,
              label: 'Delete',
              danger: true,
              onClick: () => handleDelete(record.id),
            }] : []),
          ];
          return (
            <Dropdown menu={{ items: menuItems }} trigger={['click']} disabled={isLoading}>
              <Button 
                type="text" 
                icon={<MoreOutlined />} 
                loading={isLoading}
                style={{ minWidth: 44, minHeight: 44 }}
              />
            </Dropdown>
          );
        }

        return (
          <Space size={8}>
            {canEdit && (
              <Tooltip title="Edit">
                <Button
                  type="text"
                  icon={<EditOutlined />}
                  onClick={() => openEditModal(record)}
                  disabled={isLoading}
                  style={{ color: '#5a7a6b', minWidth: 44, minHeight: 44 }}
                />
              </Tooltip>
            )}

            {canResendInvitation && (
              <Tooltip title="Resend Invitation">
                <Popconfirm
                  title="Resend invitation"
                  description="Send a new invitation email to this user?"
                  onConfirm={() => handleResendInvitation(record.id, `${record.firstName} ${record.lastName}`)}
                  okText="Yes"
                  cancelText="No"
                  disabled={isLoading}
                >
                  <Button
                    type="text"
                    icon={<MailOutlined />}
                    disabled={isLoading}
                    style={{ color: '#1890ff', minWidth: 44, minHeight: 44 }}
                  />
                </Popconfirm>
              </Tooltip>
            )}

            {canResetMfa && (
              <Tooltip title="Reset MFA (Passkeys)">
                <Button
                  type="text"
                  icon={<KeyOutlined />}
                  onClick={() => openMfaResetModal(record)}
                  disabled={isLoading}
                  style={{ color: '#c9a227', minWidth: 44, minHeight: 44 }}
                />
              </Tooltip>
            )}

            {record.isActive ? (
              canDeactivate && (
                <Tooltip title="Deactivate">
                  <Popconfirm
                    title="Deactivate user"
                    description="Are you sure you want to deactivate this user?"
                    onConfirm={() => handleDeactivate(record.id)}
                    okText="Yes"
                    cancelText="No"
                    disabled={isLoading}
                  >
                    <Button
                      type="text"
                      icon={<StopOutlined />}
                      disabled={isLoading}
                      style={{ color: '#faad14', minWidth: 44, minHeight: 44 }}
                    />
                  </Popconfirm>
                </Tooltip>
              )
            ) : (
              canDeactivate && (
                <Tooltip title="Reactivate">
                  <Button
                    type="text"
                    icon={<CheckCircleOutlined />}
                    onClick={() => handleReactivate(record.id)}
                    disabled={isLoading}
                    style={{ color: '#52c41a', minWidth: 44, minHeight: 44 }}
                  />
                </Tooltip>
              )
            )}

            {canDelete && (
              <Tooltip title="Delete">
                <Popconfirm
                  title="Delete user"
                  description="Are you sure you want to permanently delete this user? This action cannot be undone."
                  onConfirm={() => handleDelete(record.id)}
                  okText="Yes"
                  cancelText="No"
                  okButtonProps={{ danger: true }}
                  disabled={isLoading}
                >
                  <Button
                    type="text"
                    icon={<DeleteOutlined />}
                    disabled={isLoading}
                    style={{ color: '#ff4d4f', minWidth: 44, minHeight: 44 }}
                  />
                </Popconfirm>
              </Tooltip>
            )}
          </Space>
        );
      },
    },
  ];

  // Access denied for caregivers
  if (!canManageUsers) {
    return (
      <div style={{ textAlign: 'center', padding: '60px 20px' }}>
        <Title level={3} style={{ color: '#2d3732' }}>Access Denied</Title>
        <Paragraph style={{ color: '#6b7770' }}>
          You do not have permission to access user management.
        </Paragraph>
        <Button type="primary" onClick={() => router.push('/dashboard')}>
          Go to Dashboard
        </Button>
      </div>
    );
  }

  return (
    <div>
      <Flex align="center" gap={16} style={{ marginBottom: 24 }}>
        <Button
          type="text"
          icon={<ArrowLeftOutlined />}
          onClick={() => router.push('/settings')}
          style={{ color: '#5a7a6b', minWidth: 44, minHeight: 44 }}
        />
        <div>
          <Title level={isMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>User Management</Title>
          {!isSmallMobile && (
            <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
              Manage system users, roles, and permissions
            </Paragraph>
          )}
        </div>
      </Flex>

      <Card size={isMobile ? 'small' : 'default'}>
        <Flex 
          justify="space-between" 
          align={isMobile ? 'stretch' : 'center'} 
          style={{ marginBottom: 16 }}
          vertical={isMobile}
          gap={isMobile ? 12 : 0}
        >
          <Space size="middle" style={{ width: isMobile ? '100%' : 'auto' }}>
            <Input
              placeholder="Search by name or email"
              prefix={<SearchOutlined style={{ color: '#6b7770' }} />}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              allowClear
              style={{ width: isMobile ? '100%' : 280 }}
            />
            {!isMobile && (
              <Text type="secondary">
                {searchQuery ? `${filteredUsers.length} of ${users.length}` : `${users.length}`} user{users.length !== 1 ? 's' : ''}
              </Text>
            )}
          </Space>
          <Space style={{ width: isMobile ? '100%' : 'auto', justifyContent: isMobile ? 'flex-end' : 'flex-start' }}>
            <Button
              icon={<ReloadOutlined />}
              onClick={fetchUsers}
              loading={loading}
              style={{ minHeight: 44 }}
            >
              {isSmallMobile ? '' : 'Refresh'}
            </Button>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setInviteModalOpen(true)}
              style={{ background: '#5a7a6b', borderColor: '#5a7a6b', minHeight: 44 }}
            >
              {isSmallMobile ? '' : 'Invite User'}
            </Button>
          </Space>
        </Flex>

        <Table
          columns={columns}
          dataSource={filteredUsers}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => `Total ${total} users`,
          }}
        />
      </Card>

      {/* Invite User Modal */}
      <Modal
        title="Invite New Admin"
        open={inviteModalOpen}
        onCancel={() => {
          setInviteModalOpen(false);
          inviteForm.resetFields();
        }}
        footer={null}
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Form
          form={inviteForm}
          layout="vertical"
          onFinish={handleInviteUser}
          style={{ marginTop: 16 }}
        >
          <Form.Item
            name="email"
            label="Email"
            rules={[
              { required: true, message: 'Please enter email' },
              { type: 'email', message: 'Please enter a valid email' },
            ]}
          >
            <Input placeholder="user@example.com" />
          </Form.Item>

          <Form.Item
            name="firstName"
            label="First Name"
            rules={[{ required: true, message: 'Please enter first name' }]}
          >
            <Input placeholder="John" />
          </Form.Item>

          <Form.Item
            name="lastName"
            label="Last Name"
            rules={[{ required: true, message: 'Please enter last name' }]}
          >
            <Input placeholder="Doe" />
          </Form.Item>

          <Form.Item
            name="role"
            label="Role"
            rules={[{ required: true, message: 'Please select a role' }]}
          >
            <Select placeholder="Select role">
              <Select.Option value="Admin">Admin</Select.Option>
              <Select.Option value="Sysadmin">Sysadmin</Select.Option>
            </Select>
          </Form.Item>

          <Flex justify="end" gap={8}>
            <Button onClick={() => {
              setInviteModalOpen(false);
              inviteForm.resetFields();
            }}>
              Cancel
            </Button>
            <Button
              type="primary"
              htmlType="submit"
              loading={actionLoading === 'invite'}
              style={{ background: '#5a7a6b', borderColor: '#5a7a6b' }}
            >
              Send Invitation
            </Button>
          </Flex>
        </Form>
      </Modal>

      {/* Edit User Modal */}
      <Modal
        title="Edit User"
        open={editModalOpen}
        onCancel={() => {
          setEditModalOpen(false);
          editForm.resetFields();
          setSelectedUser(null);
        }}
        footer={null}
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Form
          form={editForm}
          layout="vertical"
          onFinish={handleEditUser}
          style={{ marginTop: 16 }}
        >
          <Form.Item
            name="firstName"
            label="First Name"
            rules={[{ required: true, message: 'Please enter first name' }]}
          >
            <Input placeholder="John" />
          </Form.Item>

          <Form.Item
            name="lastName"
            label="Last Name"
            rules={[{ required: true, message: 'Please enter last name' }]}
          >
            <Input placeholder="Doe" />
          </Form.Item>

          <Form.Item
            name="phoneNumber"
            label="Phone Number"
          >
            <Input placeholder="+1 (555) 123-4567" />
          </Form.Item>

          <Flex justify="end" gap={8}>
            <Button onClick={() => {
              setEditModalOpen(false);
              editForm.resetFields();
              setSelectedUser(null);
            }}>
              Cancel
            </Button>
            <Button
              type="primary"
              htmlType="submit"
              loading={actionLoading === 'edit'}
              style={{ background: '#5a7a6b', borderColor: '#5a7a6b' }}
            >
              Save Changes
            </Button>
          </Flex>
        </Form>
      </Modal>

      {/* MFA Reset Modal - Sysadmin Only */}
      <Modal
        title={
          <Flex align="center" gap={8}>
            <KeyOutlined style={{ color: '#c9a227' }} />
            <span>Reset MFA (Passkeys)</span>
          </Flex>
        }
        open={mfaResetModalOpen}
        onCancel={() => {
          setMfaResetModalOpen(false);
          setMfaResetUser(null);
          mfaResetForm.resetFields();
        }}
        footer={null}
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        {mfaResetUser && (
          <Form
            form={mfaResetForm}
            layout="vertical"
            onFinish={handleMfaReset}
            style={{ marginTop: 16 }}
          >
            <Alert
              message="This action will remove all passkeys for this user"
              description={
                <span>
                  You are about to reset MFA for <strong>{mfaResetUser.firstName} {mfaResetUser.lastName}</strong> ({mfaResetUser.email}).
                  They will need to register a new passkey on their next login. This action is logged for HIPAA compliance.
                </span>
              }
              type="warning"
              showIcon
              style={{ marginBottom: 24 }}
            />

            <Form.Item
              name="reason"
              label="Reason for Reset"
              rules={[{ required: true, message: 'Please select a reason' }]}
            >
              <Select placeholder="Select reason">
                <Select.Option value="lost_device">User lost their device</Select.Option>
                <Select.Option value="device_stolen">Device was stolen</Select.Option>
                <Select.Option value="device_damaged">Device is damaged/broken</Select.Option>
                <Select.Option value="new_device">User has a new device</Select.Option>
                <Select.Option value="unable_to_access">User cannot access passkey</Select.Option>
                <Select.Option value="security_concern">Security concern</Select.Option>
                <Select.Option value="other">Other (specify in notes)</Select.Option>
              </Select>
            </Form.Item>

            <Form.Item
              name="verificationMethod"
              label="Identity Verification Method"
              rules={[{ required: true, message: 'Please specify how you verified identity' }]}
              extra="Document how you confirmed this is a legitimate request"
            >
              <Select placeholder="Select verification method">
                <Select.Option value="in_person">In-person verification</Select.Option>
                <Select.Option value="video_call">Video call verification</Select.Option>
                <Select.Option value="phone_callback">Called known phone number</Select.Option>
                <Select.Option value="manager_confirmed">Manager confirmed request</Select.Option>
                <Select.Option value="hr_confirmed">HR department confirmed</Select.Option>
                <Select.Option value="other">Other (specify in notes)</Select.Option>
              </Select>
            </Form.Item>

            <Form.Item
              name="notes"
              label="Additional Notes"
              extra="Any additional context for the audit log"
            >
              <TextArea 
                rows={3} 
                placeholder="Optional: Add any additional context..."
                maxLength={500}
                showCount
              />
            </Form.Item>

            <Flex justify="end" gap={8} style={{ marginTop: 24 }}>
              <Button onClick={() => {
                setMfaResetModalOpen(false);
                setMfaResetUser(null);
                mfaResetForm.resetFields();
              }}>
                Cancel
              </Button>
              <Button
                type="primary"
                htmlType="submit"
                loading={mfaResetLoading}
                danger
              >
                Reset MFA
              </Button>
            </Flex>
          </Form>
        )}
      </Modal>
    </div>
  );
}

export default function UsersPage() {
  return (
    <ProtectedRoute requiredRoles={['Admin', 'Sysadmin']}>
      <AuthenticatedLayout>
        <UsersContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
