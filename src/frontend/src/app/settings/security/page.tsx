'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { Typography, Card, Button, Divider, Flex, Table, Modal, Input, message, Tag, Empty, Spin, Alert, Popconfirm, Tooltip, Grid, Dropdown } from 'antd';
import { ArrowLeftOutlined, KeyOutlined, LockOutlined, PlusOutlined, DeleteOutlined, EditOutlined, DesktopOutlined, MobileOutlined, TabletOutlined, CheckCircleOutlined, QuestionCircleOutlined, MoreOutlined, ReloadOutlined, CopyOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import { ProtectedRoute, AuthenticatedLayout, BackupCodesStep } from '@/components';
import { passkeyApi, authApi, getUserFriendlyError } from '@/lib/api';
import type { PasskeyDto } from '@/types/passkey';
import {
  isWebAuthnSupported,
  prepareCredentialCreationOptions,
  serializeAttestationResponse,
  type AuthenticatorAttachment,
} from '@/types/passkey';
import { useAuth } from '@/contexts/AuthContext';
import type { ColumnsType } from 'antd/es/table';
import type { MenuProps } from 'antd';

const { Title, Paragraph, Text } = Typography;
const { useBreakpoint } = Grid;

function SecurityContent() {
  const router = useRouter();
  const { user, hasRole } = useAuth();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;
  
  // Passkey management state
  const [passkeys, setPasskeys] = useState<PasskeyDto[]>([]);
  const [passkeysLoading, setPasskeysLoading] = useState(true);
  const [passkeysError, setPasskeysError] = useState<string | null>(null);
  
  // Registration state
  const [isRegistering, setIsRegistering] = useState(false);
  const [registrationError, setRegistrationError] = useState<string | null>(null);
  
  // Rename modal state
  const [renameModalOpen, setRenameModalOpen] = useState(false);
  const [renamingPasskey, setRenamingPasskey] = useState<PasskeyDto | null>(null);
  const [newName, setNewName] = useState('');
  const [renamingLoading, setRenamingLoading] = useState(false);
  
  // New passkey naming modal state
  const [namingModalOpen, setNamingModalOpen] = useState(false);
  const [pendingPasskeyName, setPendingPasskeyName] = useState('');
  const [pendingSessionId, setPendingSessionId] = useState<string | null>(null);
  const [pendingAttestationResponse, setPendingAttestationResponse] = useState<string | null>(null);
  const [namingLoading, setNamingLoading] = useState(false);
  
  // Delete state
  const [deletingId, setDeletingId] = useState<string | null>(null);
  
  // Backup codes state
  const [regeneratingBackupCodes, setRegeneratingBackupCodes] = useState(false);
  const [backupCodesModalOpen, setBackupCodesModalOpen] = useState(false);
  const [newBackupCodes, setNewBackupCodes] = useState<string[]>([]);
  const [backupCodesCopied, setBackupCodesCopied] = useState(false);
  
  const webAuthnSupported = isWebAuthnSupported();
  const isSysadmin = hasRole('Sysadmin');

  // Fetch passkeys on mount
  const fetchPasskeys = useCallback(async () => {
    setPasskeysLoading(true);
    setPasskeysError(null);
    try {
      const response = await passkeyApi.getMyPasskeys();
      if (response.passkeys) {
        setPasskeys(response.passkeys);
      } else {
        setPasskeysError('Unable to load your passkeys. Please try again.');
      }
    } catch {
      setPasskeysError('Unable to load your passkeys. Please try again.');
    } finally {
      setPasskeysLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchPasskeys();
  }, [fetchPasskeys]);

  // Register new passkey
  const handleRegisterPasskey = async () => {
    if (!user?.email) {
      message.error('Unable to register passkey. Please try again.');
      return;
    }

    setIsRegistering(true);
    setRegistrationError(null);

    try {
      // Step 1: Start registration
      const startResponse = await passkeyApi.startRegistration({ deviceName: 'New Device' });
      
      if (!startResponse.success || !startResponse.options || !startResponse.sessionId) {
        throw new Error(startResponse.error || 'Failed to start passkey registration');
      }

      // Step 2: Convert options for browser API
      const creationOptions = prepareCredentialCreationOptions(
        startResponse.options as unknown as Record<string, unknown>
      );

      // Step 3: Create credential with authenticator
      const credential = await navigator.credentials.create({
        publicKey: creationOptions,
      }) as PublicKeyCredential | null;

      if (!credential) {
        throw new Error('Passkey creation was cancelled');
      }

      // Step 4: Serialize and store for naming modal
      const attestationResponse = serializeAttestationResponse(credential);
      const attestationResponseJson = JSON.stringify(attestationResponse);
      
      // Detect device type for suggested name based on authenticator attachment
      const suggestedName = getDeviceNameFromCredential(credential.authenticatorAttachment as AuthenticatorAttachment | null);

      // Store pending data and show naming modal
      setPendingSessionId(startResponse.sessionId);
      setPendingAttestationResponse(attestationResponseJson);
      setPendingPasskeyName(suggestedName);
      setNamingModalOpen(true);

    } catch (err) {
      if (err instanceof Error && err.name === 'NotAllowedError') {
        setRegistrationError('Passkey registration was cancelled. Please try again.');
      } else {
        setRegistrationError(getUserFriendlyError(err, 'Unable to add passkey. Please try again.'));
      }
    } finally {
      setIsRegistering(false);
    }
  };

  // Complete passkey registration with name
  const handleNamingConfirm = async () => {
    if (!pendingSessionId || !pendingAttestationResponse) return;

    const deviceName = pendingPasskeyName.trim() || 'Passkey';
    setNamingLoading(true);

    try {
      const completeResponse = await passkeyApi.completeRegistration({
        sessionId: pendingSessionId,
        attestationResponse: pendingAttestationResponse,
        deviceName,
      });

      if (!completeResponse.success) {
        throw new Error(completeResponse.error || 'Failed to complete passkey registration');
      }

      message.success('Passkey added successfully');
      setNamingModalOpen(false);
      setPendingSessionId(null);
      setPendingAttestationResponse(null);
      setPendingPasskeyName('');
      fetchPasskeys();
    } catch (err) {
      setRegistrationError(getUserFriendlyError(err, 'Unable to add passkey. Please try again.'));
      setNamingModalOpen(false);
    } finally {
      setNamingLoading(false);
    }
  };

  // Handle naming modal cancel - still complete with suggested name
  const handleNamingCancel = async () => {
    await handleNamingConfirm();
  };

  // Rename passkey
  const handleRenameClick = (passkey: PasskeyDto) => {
    setRenamingPasskey(passkey);
    setNewName(passkey.deviceName);
    setRenameModalOpen(true);
  };

  const handleRenameConfirm = async () => {
    if (!renamingPasskey || !newName.trim()) return;

    setRenamingLoading(true);
    try {
      const response = await passkeyApi.rename(renamingPasskey.id, { deviceName: newName.trim() });
      if (response.success) {
        message.success('Passkey renamed successfully');
        setRenameModalOpen(false);
        setRenamingPasskey(null);
        fetchPasskeys();
      } else {
        message.error('Unable to rename passkey. Please try again.');
      }
    } catch {
      message.error('Unable to rename passkey. Please try again.');
    } finally {
      setRenamingLoading(false);
    }
  };

  // Delete passkey
  const handleDeletePasskey = async (id: string) => {
    if (passkeys.length === 1) {
      message.error('You cannot delete your last passkey. You need at least one for authentication.');
      return;
    }

    setDeletingId(id);
    try {
      const response = await passkeyApi.delete(id);
      if (response.success) {
        message.success('Passkey removed');
        fetchPasskeys();
      } else {
        message.error('Unable to remove passkey. Please try again.');
      }
    } catch {
      message.error('Unable to remove passkey. Please try again.');
    } finally {
      setDeletingId(null);
    }
  };

  // Regenerate backup codes
  const handleRegenerateBackupCodes = async () => {
    setRegeneratingBackupCodes(true);
    try {
      const response = await authApi.regenerateBackupCodes();
      if (response.success && response.backupCodes) {
        setNewBackupCodes(response.backupCodes);
        setBackupCodesCopied(false);
        setBackupCodesModalOpen(true);
        message.success('New backup codes generated');
      } else {
        message.error(response.error || 'Failed to regenerate backup codes');
      }
    } catch {
      message.error('Failed to regenerate backup codes. Please try again.');
    } finally {
      setRegeneratingBackupCodes(false);
    }
  };

  // Copy backup codes to clipboard
  const handleCopyBackupCodes = async () => {
    try {
      await navigator.clipboard.writeText(newBackupCodes.join('\n'));
      setBackupCodesCopied(true);
      message.success('Backup codes copied to clipboard');
    } catch {
      message.error('Failed to copy backup codes');
    }
  };

  // Get device name based on authenticator attachment and platform
  const getDeviceNameFromCredential = (authenticatorAttachment: AuthenticatorAttachment | null): string => {
    const ua = navigator.userAgent;
    const dateStr = new Date().toLocaleDateString();
    
    // If it's a cross-platform authenticator (security key, password manager, phone)
    if (authenticatorAttachment === 'cross-platform') {
      return `Password Manager / Security Key - ${dateStr}`;
    }
    
    // Platform authenticator - use device-specific names
    if (ua.includes('iPhone')) return `iPhone (Face ID / Touch ID) - ${dateStr}`;
    if (ua.includes('iPad')) return `iPad (Touch ID / Face ID) - ${dateStr}`;
    if (ua.includes('Mac')) return `Mac (Touch ID) - ${dateStr}`;
    if (ua.includes('Windows')) return `Windows (Windows Hello) - ${dateStr}`;
    if (ua.includes('Android')) return `Android Device - ${dateStr}`;
    if (ua.includes('Linux')) return `Linux PC - ${dateStr}`;
    return `Security Key - ${dateStr}`;
  };

  // Get device icon
  const getDeviceIcon = (aaguid?: string, deviceName?: string) => {
    // Simple heuristic based on name or AAGUID patterns
    const name = deviceName?.toLowerCase() || '';
    if (name.includes('mobile') || name.includes('iphone') || name.includes('android')) {
      return <MobileOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />;
    }
    if (name.includes('tablet') || name.includes('ipad')) {
      return <TabletOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />;
    }
    return <DesktopOutlined style={{ fontSize: 20, color: '#5a7a6b' }} />;
  };

  // Format date helper
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  // Table columns
  const columns: ColumnsType<PasskeyDto> = [
    {
      title: 'Device',
      key: 'device',
      render: (_, record) => (
        <Flex align="center" gap={12}>
          <div style={{ 
            width: 40, 
            height: 40, 
            borderRadius: 8, 
            background: 'rgba(90, 122, 107, 0.1)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            flexShrink: 0,
          }}>
            {getDeviceIcon(undefined, record.deviceName)}
          </div>
          <div style={{ minWidth: 0 }}>
            <Text strong style={{ display: 'block' }}>{record.deviceName}</Text>
            <Text type="secondary" style={{ fontSize: 12 }}>
              Added {formatDate(record.createdAt)}
            </Text>
          </div>
        </Flex>
      ),
    },
    {
      title: 'Last Used',
      key: 'lastUsed',
      width: 180,
      responsive: ['md'],
      render: (_, record) => (
        <Text type="secondary">
          {record.lastUsedAt ? formatDate(record.lastUsedAt) : 'Never used'}
        </Text>
      ),
    },
    {
      title: 'Status',
      key: 'status',
      width: 100,
      responsive: ['sm'],
      render: () => (
        <Tag icon={<CheckCircleOutlined />} color="success">Active</Tag>
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: isMobile ? 60 : 120,
      render: (_, record) => {
        if (isMobile) {
          const menuItems: MenuProps['items'] = [
            {
              key: 'rename',
              icon: <EditOutlined />,
              label: 'Rename',
              onClick: () => handleRenameClick(record),
            },
            {
              key: 'delete',
              icon: <DeleteOutlined />,
              label: 'Delete',
              danger: true,
              disabled: passkeys.length === 1,
              onClick: () => handleDeletePasskey(record.id),
            },
          ];
          return (
            <Dropdown menu={{ items: menuItems }} trigger={['click']}>
              <Button 
                type="text" 
                icon={<MoreOutlined />}
                style={{ minWidth: 44, minHeight: 44 }}
              />
            </Dropdown>
          );
        }
        return (
          <Flex gap={8}>
            <Tooltip title="Rename">
              <Button 
                type="text" 
                size="small"
                icon={<EditOutlined />} 
                onClick={() => handleRenameClick(record)}
                style={{ minWidth: 44, minHeight: 44 }}
              />
            </Tooltip>
            <Popconfirm
              title="Delete passkey"
              description={passkeys.length === 1 
                ? "You cannot delete your only passkey." 
                : "Are you sure you want to remove this passkey?"
              }
              onConfirm={() => handleDeletePasskey(record.id)}
              okText="Delete"
              cancelText="Cancel"
              okButtonProps={{ danger: true, disabled: passkeys.length === 1 }}
            >
              <Tooltip title="Delete">
                <Button 
                  type="text" 
                  size="small"
                  danger
                  icon={<DeleteOutlined />}
                  loading={deletingId === record.id}
                  style={{ minWidth: 44, minHeight: 44 }}
                />
              </Tooltip>
            </Popconfirm>
          </Flex>
        );
      },
    },
  ];

  return (
    <div>
      <Flex style={{ marginBottom: 24 }}>
        <Button 
          type="text" 
          icon={<ArrowLeftOutlined />} 
          onClick={() => router.push('/settings')}
          style={{ color: '#6b7770', minWidth: 44, minHeight: 44 }}
        >
          {isSmallMobile ? '' : 'Back to Settings'}
        </Button>
      </Flex>

      <div style={{ marginBottom: 24 }}>
        <Title level={isMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>Security</Title>
        <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
          Manage your passkeys and authentication settings
        </Paragraph>
      </div>

      {/* Passkeys Section */}
      <Card 
        title={
          <Flex align="center" gap={8}>
            <KeyOutlined style={{ color: '#5a7a6b' }} />
            <span>Passkeys</span>
          </Flex>
        }
        extra={
          <Button 
            type="primary" 
            icon={<PlusOutlined />}
            onClick={handleRegisterPasskey}
            loading={isRegistering}
            disabled={!webAuthnSupported}
            style={{ minHeight: 44 }}
          >
            {isSmallMobile ? '' : 'Add Passkey'}
          </Button>
        }
        style={{ marginBottom: 24 }}
        size={isMobile ? 'small' : 'default'}
      >
        <Paragraph style={{ marginBottom: 16 }}>
          Passkeys are a more secure alternative to passwords. They use your device&apos;s 
          biometrics (fingerprint or face recognition) or PIN to authenticate you.
        </Paragraph>

        {!webAuthnSupported && (
          <Alert
            title="Passkeys Not Supported"
            description="Your browser doesn't support passkeys. Please use a modern browser like Chrome, Safari, Firefox, or Edge."
            type="warning"
            showIcon
            style={{ marginBottom: 16 }}
          />
        )}

        {registrationError && (
          <Alert
            title="Registration Failed"
            description={registrationError}
            type="error"
            showIcon
            closable
            onClose={() => setRegistrationError(null)}
            style={{ marginBottom: 16 }}
          />
        )}

        {passkeysLoading ? (
          <div style={{ textAlign: 'center', padding: 40 }}>
            <Spin size="large" />
          </div>
        ) : passkeysError ? (
          <Alert
            title="Error Loading Passkeys"
            description={passkeysError}
            type="error"
            showIcon
            action={
              <Button size="small" onClick={fetchPasskeys}>Retry</Button>
            }
          />
        ) : passkeys.length === 0 ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="No passkeys registered"
          >
            <Button 
              type="primary" 
              icon={<PlusOutlined />}
              onClick={handleRegisterPasskey}
              loading={isRegistering}
              disabled={!webAuthnSupported}
            >
              Register Your First Passkey
            </Button>
          </Empty>
        ) : (
          <Table
            columns={columns}
            dataSource={passkeys}
            rowKey="id"
            pagination={false}
            size="middle"
          />
        )}
      </Card>

      {/* Password Section */}
      <Card 
        title={
          <Flex align="center" gap={8}>
            <LockOutlined style={{ color: '#5a7a6b' }} />
            <span>Password</span>
          </Flex>
        }
        style={{ marginBottom: 24 }}
        size={isMobile ? 'small' : 'default'}
      >
        <Flex 
          align={isMobile ? 'stretch' : 'center'} 
          justify="space-between"
          vertical={isMobile}
          gap={isMobile ? 12 : 0}
        >
          <div>
            <Text strong style={{ display: 'block' }}>Change Password</Text>
            <Text type="secondary">Update your password to keep your account secure</Text>
          </div>
          <Button disabled style={{ minHeight: 44 }}>Change Password</Button>
        </Flex>
        <Divider />
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Password change functionality will be available in a future update.
        </Paragraph>
      </Card>

      {/* Backup Codes Section - Only for Sysadmins */}
      {isSysadmin && (
        <Card 
          title={
            <Flex align="center" gap={8}>
              <KeyOutlined style={{ color: '#c9a227' }} />
              <span>Backup Codes</span>
              <Tooltip title="Backup codes are only available for Sysadmin accounts">
                <QuestionCircleOutlined style={{ color: '#9ca5a0', fontSize: 14 }} />
              </Tooltip>
            </Flex>
          }
        >
          <Paragraph>
            Backup codes provide emergency access to your account if you lose access to all your passkeys.
            Each code can only be used once.
          </Paragraph>
          <Alert
            title="Regenerating codes will invalidate all previous backup codes"
            type="warning"
            showIcon
            style={{ marginBottom: 16 }}
          />
          <Flex align="center" justify="space-between" wrap="wrap" gap={12}>
            <Text type="secondary">
              Use backup codes only when you cannot access any of your passkeys.
            </Text>
            <Popconfirm
              title="Regenerate Backup Codes?"
              description="This will invalidate all your existing backup codes. Make sure to save the new ones."
              onConfirm={handleRegenerateBackupCodes}
              okText="Regenerate"
              cancelText="Cancel"
              okButtonProps={{ danger: true }}
            >
              <Button 
                icon={<ReloadOutlined />}
                loading={regeneratingBackupCodes}
              >
                Regenerate Backup Codes
              </Button>
            </Popconfirm>
          </Flex>
        </Card>
      )}

      {/* Rename Modal */}
      <Modal
        title="Rename Passkey"
        open={renameModalOpen}
        onOk={handleRenameConfirm}
        onCancel={() => {
          setRenameModalOpen(false);
          setRenamingPasskey(null);
          setNewName('');
        }}
        confirmLoading={renamingLoading}
        okText="Save"
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Paragraph>
          Enter a new name for this passkey to help you identify it.
        </Paragraph>
        <Input
          placeholder="e.g., MacBook Pro, iPhone 15"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          maxLength={100}
        />
      </Modal>

      {/* Name New Passkey Modal */}
      <Modal
        title="Name Your Passkey"
        open={namingModalOpen}
        onOk={handleNamingConfirm}
        onCancel={handleNamingCancel}
        confirmLoading={namingLoading}
        okText="Save"
        cancelText="Use Suggested Name"
        closable={false}
        maskClosable={false}
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
      >
        <Paragraph>
          Give your passkey a name to help you identify it later. We&apos;ve suggested a name based on your device.
        </Paragraph>
        <Input
          placeholder="e.g., NordPass, YubiKey, MacBook Pro"
          value={pendingPasskeyName}
          onChange={(e) => setPendingPasskeyName(e.target.value)}
          maxLength={100}
          onPressEnter={handleNamingConfirm}
        />
        <Text type="secondary" style={{ fontSize: 12, display: 'block', marginTop: 8 }}>
          Tip: Use a descriptive name like &quot;NordPass&quot;, &quot;Work Laptop&quot;, or &quot;YubiKey 5&quot;
        </Text>
      </Modal>

      {/* Backup Codes Modal */}
      <Modal
        title="Your New Backup Codes"
        open={backupCodesModalOpen}
        onCancel={() => setBackupCodesModalOpen(false)}
        footer={[
          <Button
            key="copy"
            icon={backupCodesCopied ? <CheckCircleOutlined /> : <CopyOutlined />}
            onClick={handleCopyBackupCodes}
            style={backupCodesCopied ? { background: '#d4edda', borderColor: '#28a745', color: '#28a745' } : undefined}
          >
            {backupCodesCopied ? 'Copied!' : 'Copy All Codes'}
          </Button>,
          <Button 
            key="close" 
            type="primary" 
            onClick={() => setBackupCodesModalOpen(false)}
            disabled={!backupCodesCopied}
          >
            I&apos;ve Saved My Codes
          </Button>
        ]}
        width={isMobile ? '100%' : 520}
        style={isMobile ? { margin: 16, maxWidth: 'calc(100vw - 32px)' } : undefined}
        closable={false}
        maskClosable={false}
      >
        <BackupCodesStep
          backupCodes={newBackupCodes}
          copied={backupCodesCopied}
          onCopy={handleCopyBackupCodes}
          onContinue={() => setBackupCodesModalOpen(false)}
          compact
          hideActions
        />
      </Modal>
    </div>
  );
}

export default function SecurityPage() {
  return (
    <ProtectedRoute>
      <AuthenticatedLayout>
        <SecurityContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
