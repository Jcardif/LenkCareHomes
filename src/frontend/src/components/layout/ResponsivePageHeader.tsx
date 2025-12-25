'use client';

import React from 'react';
import { Typography, Button, Dropdown, Grid } from 'antd';
import { MoreOutlined } from '@ant-design/icons';
import type { MenuProps, ButtonProps } from 'antd';

const { Title, Paragraph } = Typography;
const { useBreakpoint } = Grid;

export interface PageAction {
  key: string;
  label: string;
  icon?: React.ReactNode;
  type?: ButtonProps['type'];
  danger?: boolean;
  onClick: () => void;
  /** If true, this action is primary and always visible */
  primary?: boolean;
  /** If true, hide this action on mobile (collapsed into dropdown) */
  hideOnMobile?: boolean;
}

export interface ResponsivePageHeaderProps {
  /** Page title */
  title: React.ReactNode;
  /** Optional subtitle/description */
  description?: React.ReactNode;
  /** Icon to display before the title */
  icon?: React.ReactNode;
  /** Action buttons */
  actions?: PageAction[];
  /** Additional content to render in the header area */
  extra?: React.ReactNode;
  /** Custom styles for the wrapper */
  style?: React.CSSProperties;
}

/**
 * A responsive page header component that handles the common pattern of
 * title + description on the left and action buttons on the right.
 * 
 * On mobile:
 * - Title and description stack above actions
 * - Secondary actions collapse into a "More" dropdown
 * - Primary actions remain visible
 * 
 * @example
 * ```tsx
 * <ResponsivePageHeader
 *   title="Clients"
 *   description="Manage residents across all homes"
 *   actions={[
 *     { key: 'refresh', label: 'Refresh', icon: <ReloadOutlined />, onClick: handleRefresh },
 *     { key: 'add', label: 'Add Client', icon: <PlusOutlined />, type: 'primary', primary: true, onClick: handleAdd },
 *   ]}
 * />
 * ```
 */
export default function ResponsivePageHeader({
  title,
  description,
  icon,
  actions = [],
  extra,
  style,
}: ResponsivePageHeaderProps) {
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  // Separate primary actions (always visible) from secondary actions
  const primaryActions = actions.filter(a => a.primary);
  const secondaryActions = actions.filter(a => !a.primary);

  // On mobile, collapse non-primary actions into dropdown
  const visibleActions = isMobile ? primaryActions : actions;
  const collapsedActions = isMobile ? secondaryActions : [];

  // Build dropdown menu items for collapsed actions
  const dropdownItems: MenuProps['items'] = collapsedActions.map(action => ({
    key: action.key,
    label: action.label,
    icon: action.icon,
    danger: action.danger,
    onClick: action.onClick,
  }));

  return (
    <div 
      className="page-header-wrapper"
      style={{
        display: 'flex',
        flexDirection: isMobile ? 'column' : 'row',
        justifyContent: 'space-between',
        alignItems: isMobile ? 'stretch' : 'center',
        gap: isMobile ? 16 : 24,
        marginBottom: 24,
        ...style,
      }}
    >
      {/* Title Section */}
      <div style={{ minWidth: 0 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          {icon && (
            <span style={{ fontSize: 24, color: '#5a7a6b', flexShrink: 0 }}>
              {icon}
            </span>
          )}
          <Title 
            level={2} 
            style={{ 
              margin: 0, 
              color: '#2d3732',
              fontSize: isMobile ? 20 : 24,
              lineHeight: 1.3,
            }}
          >
            {title}
          </Title>
        </div>
        {description && (
          <Paragraph 
            style={{ 
              color: '#6b7770', 
              marginBottom: 0, 
              marginTop: 4,
              marginLeft: icon ? 32 : 0,
              fontSize: isMobile ? 13 : 14,
            }}
          >
            {description}
          </Paragraph>
        )}
      </div>

      {/* Actions Section */}
      {(actions.length > 0 || extra) && (
        <div 
          className="page-header-actions"
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: 8,
            flexDirection: isSmallMobile ? 'column' : 'row',
            alignItems: isSmallMobile ? 'stretch' : 'center',
          }}
        >
          {extra}
          
          {/* Collapsed actions dropdown (mobile only) */}
          {collapsedActions.length > 0 && (
            <Dropdown menu={{ items: dropdownItems }} trigger={['click']}>
              <Button 
                icon={<MoreOutlined />}
                style={{ minWidth: 44, minHeight: 44 }}
              >
                More
              </Button>
            </Dropdown>
          )}
          
          {/* Visible action buttons */}
          {visibleActions.map(action => (
            <Button
              key={action.key}
              type={action.type}
              danger={action.danger}
              icon={action.icon}
              onClick={action.onClick}
              style={{ 
                minHeight: 44,
                width: isSmallMobile ? '100%' : 'auto',
              }}
            >
              {action.label}
            </Button>
          ))}
        </div>
      )}
    </div>
  );
}
