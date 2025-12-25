import type { ThemeConfig } from 'antd';

/**
 * Premium, elegant Ant Design theme with calm sage green accents
 * 
 * Design principles:
 * - Sophisticated neutral backgrounds
 * - Calm, muted sage green as primary
 * - Premium feel with refined typography
 * - Subtle shadows and smooth corners
 * - Healthcare-appropriate calming palette
 */
export const theme: ThemeConfig = {
  token: {
    // Primary color - Calm sage green (muted, sophisticated)
    colorPrimary: '#5a7a6b', // Sage green
    colorLink: '#5a7a6b',
    colorLinkHover: '#4a6a5b',
    
    // Semantic colors - Muted, professional tones
    colorSuccess: '#6b8e6b', // Soft forest green
    colorWarning: '#c9a227', // Muted gold
    colorError: '#c45c5c', // Soft red
    colorInfo: '#5a7a6b', // Same as primary
    
    // Typography - Clean, professional fonts
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
    fontSize: 14,
    fontSizeHeading1: 32,
    fontSizeHeading2: 26,
    fontSizeHeading3: 20,
    fontSizeHeading4: 17,
    fontSizeHeading5: 15,
    fontWeightStrong: 600,
    
    // Border radius - Refined, not too rounded
    borderRadius: 8,
    borderRadiusLG: 12,
    borderRadiusSM: 6,
    borderRadiusXS: 4,
    
    // Backgrounds - Warm neutrals
    colorBgContainer: '#ffffff',
    colorBgLayout: '#f8f9fa',
    colorBgElevated: '#ffffff',
    colorBgSpotlight: 'rgba(45, 55, 50, 0.9)',
    
    // Borders - Subtle warm grays
    colorBorder: '#e0e4e3',
    colorBorderSecondary: '#ebeeed',
    
    // Text colors - Warm, readable
    colorText: '#2d3732', // Dark green-gray
    colorTextSecondary: '#6b7770',
    colorTextTertiary: '#9ca5a0',
    colorTextQuaternary: '#c5cbc8',
    
    // Shadows - Soft, premium feel
    boxShadow: '0 4px 16px rgba(45, 55, 50, 0.08)',
    boxShadowSecondary: '0 2px 8px rgba(45, 55, 50, 0.06)',
    
    // Spacing
    padding: 16,
    paddingLG: 24,
    paddingSM: 12,
    paddingXS: 8,
    paddingXXS: 4,
    margin: 16,
    marginLG: 24,
    marginSM: 12,
    marginXS: 8,
    
    // Motion
    motionDurationFast: '0.15s',
    motionDurationMid: '0.2s',
    motionDurationSlow: '0.3s',
    motionEaseInOut: 'cubic-bezier(0.4, 0, 0.2, 1)',
    motionEaseOut: 'cubic-bezier(0, 0, 0.2, 1)',
    
    // Control heights
    controlHeight: 40,
    controlHeightLG: 44,
    controlHeightSM: 32,
  },
  components: {
    Layout: {
      headerBg: '#ffffff',
      headerColor: '#2d3732',
      headerPadding: '0 24px',
      headerHeight: 64,
      siderBg: '#2d3732', // Dark green-gray sidebar
      bodyBg: '#f8f9fa',
      triggerBg: '#3d4742',
      triggerColor: '#ffffff',
    },
    Menu: {
      darkItemBg: 'transparent',
      darkItemColor: 'rgba(255, 255, 255, 0.75)',
      darkItemHoverBg: 'rgba(255, 255, 255, 0.08)',
      darkItemHoverColor: '#ffffff',
      darkItemSelectedBg: 'rgba(90, 122, 107, 0.4)',
      darkItemSelectedColor: '#ffffff',
      itemMarginInline: 8,
      itemBorderRadius: 6,
      iconSize: 18,
      collapsedIconSize: 20,
    },
    Button: {
      primaryShadow: '0 2px 8px rgba(90, 122, 107, 0.3)',
      defaultBorderColor: '#e0e4e3',
      defaultColor: '#2d3732',
      defaultBg: '#ffffff',
      defaultHoverBg: '#f8f9fa',
      defaultHoverColor: '#5a7a6b',
      defaultHoverBorderColor: '#5a7a6b',
      fontWeight: 500,
      paddingInline: 20,
      paddingInlineLG: 24,
    },
    Card: {
      paddingLG: 24,
      borderRadiusLG: 12,
      boxShadowTertiary: '0 2px 12px rgba(45, 55, 50, 0.06)',
    },
    Input: {
      activeBorderColor: '#5a7a6b',
      hoverBorderColor: '#9ca5a0',
      activeShadow: '0 0 0 3px rgba(90, 122, 107, 0.12)',
      paddingInline: 14,
      paddingBlock: 10,
    },
    Select: {
      optionSelectedBg: 'rgba(90, 122, 107, 0.1)',
      optionActiveBg: 'rgba(45, 55, 50, 0.04)',
    },
    Form: {
      labelFontSize: 14,
      labelColor: '#2d3732',
      verticalLabelPadding: '0 0 8px',
    },
    Table: {
      headerBg: '#f8f9fa',
      headerColor: '#2d3732',
      rowHoverBg: 'rgba(90, 122, 107, 0.04)',
      borderColor: '#ebeeed',
    },
    Modal: {
      borderRadiusLG: 12,
      contentBg: '#ffffff',
      headerBg: '#ffffff',
      titleFontSize: 18,
      titleColor: '#2d3732',
    },
    Notification: {
      borderRadiusLG: 10,
    },
    Message: {
      borderRadiusLG: 8,
    },
    Alert: {
      borderRadiusLG: 8,
    },
    Tabs: {
      itemColor: '#6b7770',
      itemHoverColor: '#2d3732',
      itemSelectedColor: '#5a7a6b',
      inkBarColor: '#5a7a6b',
    },
    Avatar: {
      borderRadius: 100,
    },
    Divider: {
      colorSplit: '#ebeeed',
    },
    Dropdown: {
      borderRadiusLG: 10,
      paddingBlock: 6,
    },
    Statistic: {
      titleFontSize: 14,
      contentFontSize: 28,
    },
    Steps: {
      colorPrimary: '#5a7a6b',
    },
    Progress: {
      defaultColor: '#5a7a6b',
    },
    Spin: {
      colorPrimary: '#5a7a6b',
    },
  },
};

export default theme;
