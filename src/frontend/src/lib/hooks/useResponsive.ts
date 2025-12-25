'use client';

import { Grid } from 'antd';

const { useBreakpoint } = Grid;

/**
 * Ant Design breakpoint widths:
 * - xs: < 576px (mobile phones)
 * - sm: ≥ 576px (landscape phones)
 * - md: ≥ 768px (tablets portrait)
 * - lg: ≥ 992px (tablets landscape / small desktops)
 * - xl: ≥ 1200px (desktops)
 * - xxl: ≥ 1600px (large desktops)
 */

export interface ResponsiveFlags {
  /** Raw breakpoints from Ant Design */
  breakpoints: ReturnType<typeof useBreakpoint>;
  
  /** True when screen width is below 576px (xs only) */
  isMobile: boolean;
  
  /** True when screen width is between 576px and 991px (sm and md) */
  isTablet: boolean;
  
  /** True when screen width is 992px or above (lg, xl, xxl) */
  isDesktop: boolean;
  
  /** True when screen width is below 992px (xs, sm, md) */
  isMobileOrTablet: boolean;
  
  /** True when screen width is below 768px (xs, sm) */
  isSmallScreen: boolean;
  
  /** True when screen width is 1200px or above (xl, xxl) */
  isLargeDesktop: boolean;
}

/**
 * Custom hook that wraps Ant Design's Grid.useBreakpoint() and provides
 * convenient boolean flags for responsive design decisions.
 * 
 * @example
 * ```tsx
 * function MyComponent() {
 *   const { isMobile, isTablet, isDesktop } = useResponsive();
 *   
 *   return (
 *     <div style={{ padding: isMobile ? 16 : 32 }}>
 *       {isMobile ? <MobileNav /> : <DesktopNav />}
 *     </div>
 *   );
 * }
 * ```
 */
export function useResponsive(): ResponsiveFlags {
  const breakpoints = useBreakpoint();
  
  // isMobile: xs only (< 576px)
  // When only xs is true, no other breakpoints should be active
  const isMobile = breakpoints.xs === true && breakpoints.sm !== true;
  
  // isTablet: sm or md active, but not lg (576px - 991px)
  const isTablet = (breakpoints.sm === true || breakpoints.md === true) && breakpoints.lg !== true;
  
  // isDesktop: lg or above (≥ 992px)
  const isDesktop = breakpoints.lg === true;
  
  // isMobileOrTablet: below lg (< 992px)
  const isMobileOrTablet = breakpoints.lg !== true;
  
  // isSmallScreen: below md (< 768px)
  const isSmallScreen = breakpoints.md !== true;
  
  // isLargeDesktop: xl or above (≥ 1200px)
  const isLargeDesktop = breakpoints.xl === true;
  
  return {
    breakpoints,
    isMobile,
    isTablet,
    isDesktop,
    isMobileOrTablet,
    isSmallScreen,
    isLargeDesktop,
  };
}

export default useResponsive;
