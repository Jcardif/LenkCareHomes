'use client';

import React from 'react';
import { Button, Typography, Alert, Flex } from 'antd';
import { SafetyCertificateOutlined, CopyOutlined, CheckCircleOutlined } from '@ant-design/icons';

const { Title, Paragraph, Text } = Typography;

interface BackupCodesStepProps {
  /** The backup codes to display */
  backupCodes: string[];
  /** Whether the codes have been copied */
  copied: boolean;
  /** Handler for copying codes */
  onCopy: () => void;
  /** Handler for continuing to next step */
  onContinue: () => void;
  /** Whether the continue action is loading */
  loading?: boolean;
  /** Text for the continue button */
  continueText?: string;
  /** Whether this is for a mobile view */
  isSmallMobile?: boolean;
  /** Optional back button handler */
  onBack?: () => void;
  /** Text for the back button */
  backText?: string;
  /** Compact mode for use in modals (no header icon) */
  compact?: boolean;
  /** Hide the continue button (for modal footers) */
  hideActions?: boolean;
}

/**
 * Shared component for displaying backup codes during MFA setup.
 * Used in accept-invitation, setup-passkey flows, and security settings.
 */
export function BackupCodesStep({
  backupCodes,
  copied,
  onCopy,
  onContinue,
  loading = false,
  continueText = "I've Saved My Backup Codes - Continue",
  isSmallMobile = false,
  onBack,
  backText = '← Back',
  compact = false,
  hideActions = false,
}: BackupCodesStepProps) {
  return (
    <div>
      {!compact && (
        <div style={{ textAlign: 'center', marginBottom: 28 }}>
          <div style={{ 
            width: 72, 
            height: 72, 
            borderRadius: 18, 
            background: 'rgba(201, 162, 39, 0.1)', 
            display: 'flex', 
            alignItems: 'center', 
            justifyContent: 'center',
            margin: '0 auto 20px'
          }}>
            <SafetyCertificateOutlined style={{ fontSize: 36, color: '#c9a227' }} />
          </div>
          <Title level={3} style={{ color: '#2d3732', marginBottom: 12 }}>Save Your Backup Codes</Title>
          <Paragraph style={{ color: '#6b7770', margin: 0, fontSize: 16 }}>
            As a Sysadmin, you have emergency backup codes. Save these securely - they&apos;re your only recovery option if you lose all your passkeys.
          </Paragraph>
        </div>
      )}

      <Alert
        title="Important: These codes will only be shown once"
        description="Store these backup codes in a secure location like a password manager. Each code can only be used once."
        type="warning"
        showIcon
        style={{ marginBottom: compact ? 16 : 24, borderRadius: 10 }}
      />

      <div style={{ 
        background: '#fffbeb',
        border: '1px solid #fde68a',
        borderRadius: 10, 
        padding: compact ? 16 : 20,
        marginBottom: hideActions ? 0 : 24,
      }}>
        <div style={{ 
          display: 'grid', 
          gridTemplateColumns: 'repeat(2, 1fr)', 
          gap: 8,
          marginBottom: hideActions ? 0 : 16,
        }}>
          {backupCodes.map((code, i) => (
            <Text key={i} code style={{ fontSize: 14, color: '#2d3732', fontFamily: 'monospace' }}>{code}</Text>
          ))}
        </div>
        {!hideActions && (
          <Button 
            type={copied ? 'default' : 'primary'}
            icon={copied ? <CheckCircleOutlined /> : <CopyOutlined />}
            onClick={onCopy}
            style={{ 
              borderRadius: 8,
              background: copied ? '#d4edda' : undefined,
              borderColor: copied ? '#28a745' : undefined,
              color: copied ? '#28a745' : undefined,
            }}
          >
            {copied ? 'Copied to Clipboard!' : 'Copy All Codes'}
          </Button>
        )}
      </div>

      {!hideActions && (
        <Flex vertical gap={14}>
          <Button 
            type="primary" 
            onClick={onContinue} 
            block 
            loading={loading}
            disabled={!copied}
            style={{ 
              borderRadius: 10, 
              height: isSmallMobile ? 44 : 52, 
              fontSize: isSmallMobile ? 14 : 16,
              whiteSpace: 'normal',
              lineHeight: 1.3,
              padding: isSmallMobile ? '8px 12px' : '8px 16px',
            }}
          >
            {continueText}
          </Button>
          
          {!copied && (
            <Text style={{ color: '#9ca5a0', textAlign: 'center', fontSize: 15 }}>
              Please copy your backup codes before continuing
            </Text>
          )}
          
          {onBack && (
            <Button type="text" onClick={onBack} block style={{ color: '#6b7770', fontSize: 15 }}>
              {backText}
            </Button>
          )}
        </Flex>
      )}
      
      {hideActions && !copied && (
        <Text style={{ color: '#9ca5a0', textAlign: 'center', fontSize: 14, display: 'block', marginTop: 12 }}>
          Please copy your backup codes before closing
        </Text>
      )}
    </div>
  );
}

export default BackupCodesStep;
