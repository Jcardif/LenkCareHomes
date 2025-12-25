import type { TourDefinition } from '@/types/tour';

/**
 * Welcome tour for first-time Admin users
 * Walks through the main dashboard, navigation, and key features
 */
export const adminWelcomeTour: TourDefinition = {
  id: 'admin-welcome',
  name: 'Welcome to LenkCare Homes',
  description: 'Get familiar with the main features of LenkCare Homes',
  version: 1,
  autoStart: true,
  priority: 100,
  roles: ['Admin'],
  steps: [
    {
      id: 'welcome',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'dashboard-welcome' },
      title: 'Welcome to LenkCare Homes! 👋',
      content: 'This guided tour will help you get familiar with the key features of the application. Let\'s start by exploring your dashboard.',
      placement: 'bottom',
      spotlightPadding: 16,
    },
    {
      id: 'sidebar-navigation',
      route: '/dashboard',
      target: { type: 'role', value: 'navigation', name: 'Main navigation' },
      title: 'Navigation Menu',
      content: 'Use this sidebar to navigate between different sections of the app. It includes Homes, Clients, Caregivers, Incidents, Reports, and more.',
      placement: 'right',
      spotlightPadding: 8,
    },
    {
      id: 'dashboard-stats',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'dashboard-stats' },
      title: 'Dashboard Overview',
      content: 'Your dashboard shows key metrics at a glance: active homes, bed occupancy, clients, and caregivers. Quickly assess your operations.',
      placement: 'bottom',
      spotlightPadding: 12,
    },
    {
      id: 'quick-actions',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'quick-actions' },
      title: 'Quick Actions',
      content: 'Access common tasks quickly from here. Navigate to manage homes, clients, caregivers, or view audit logs.',
      placement: 'left',
      spotlightPadding: 12,
    },
    {
      id: 'homes-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'menu-homes' },
      title: 'Manage Homes',
      content: 'Click here to view and manage your adult family homes. You can add new homes, configure beds, and see occupancy status.',
      placement: 'right',
      spotlightPadding: 4,
    },
    {
      id: 'clients-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'menu-clients' },
      title: 'Client Management',
      content: 'Access all client records here. You can admit new clients, view profiles, track care activities, and manage documents.',
      placement: 'right',
      spotlightPadding: 4,
    },
    {
      id: 'caregivers-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'menu-caregivers' },
      title: 'Caregiver Management',
      content: 'Manage your care staff here. Invite new caregivers, assign them to homes, and view their activity.',
      placement: 'right',
      spotlightPadding: 4,
    },
    {
      id: 'incidents-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'menu-incidents' },
      title: 'Incident Reporting',
      content: 'View and manage incident reports. Track falls, medication errors, and other safety events. Review status and follow-ups.',
      placement: 'right',
      spotlightPadding: 4,
    },
    {
      id: 'audit-logs-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'menu-audit' },
      title: 'Audit Logs',
      content: 'For HIPAA compliance, all PHI access is logged. View detailed audit trails of who accessed what and when.',
      placement: 'right',
      spotlightPadding: 4,
    },
    {
      id: 'user-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'user-menu' },
      title: 'Your Account',
      content: 'Access your profile, security settings, and sign out from here. You can also change your password and manage MFA.',
      placement: 'bottom-end',
      spotlightPadding: 8,
    },
    {
      id: 'help-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'menu-help' },
      title: 'Help & Documentation',
      content: 'Need help? Find FAQs, user guides, and keyboard shortcuts here. You can also restart this tour anytime from the Help page.',
      placement: 'right',
      spotlightPadding: 4,
    },
  ],
};

/**
 * Welcome tour for first-time Caregiver users
 * Focused on daily care logging and client management
 */
export const caregiverWelcomeTour: TourDefinition = {
  id: 'caregiver-welcome',
  name: 'Welcome to LenkCare Homes',
  description: 'Learn how to log daily care activities and access client information',
  version: 1,
  autoStart: true,
  priority: 100,
  roles: ['Caregiver'],
  steps: [
    {
      id: 'welcome',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'dashboard-welcome' },
      title: 'Welcome to LenkCare Homes! 👋',
      content: 'This tour will help you learn how to log care activities and access client information. Let\'s get started!',
      placement: 'bottom',
      spotlightPadding: 16,
    },
    {
      id: 'assigned-homes',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'assigned-homes' },
      title: 'Your Assigned Homes',
      content: 'Here you can see the homes you\'re assigned to. You\'ll only have access to clients in these homes.',
      placement: 'bottom',
      spotlightPadding: 12,
      optional: true,
    },
    {
      id: 'my-clients',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'my-clients' },
      title: 'Your Clients',
      content: 'View all clients in your assigned homes. Click on a client name to access their full profile and log care activities.',
      placement: 'bottom',
      spotlightPadding: 12,
      optional: true,
    },
    {
      id: 'clients-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'menu-clients' },
      title: 'Client Records',
      content: 'Click here to see all your clients. From there, you can view profiles, log ADLs, vitals, medications, and more.',
      placement: 'right',
      spotlightPadding: 4,
    },
    {
      id: 'incidents-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'menu-incidents' },
      title: 'Report Incidents',
      content: 'Report any safety incidents such as falls, medication errors, or behavioral issues here. Timely reporting is important for client safety.',
      placement: 'right',
      spotlightPadding: 4,
    },
    {
      id: 'help-menu',
      route: '/dashboard',
      target: { type: 'data-tour', value: 'menu-help' },
      title: 'Get Help',
      content: 'Find answers to common questions and learn about app features. You can restart this tour anytime from the Help page.',
      placement: 'right',
      spotlightPadding: 4,
    },
  ],
};

/**
 * All available tour definitions
 */
export const tourDefinitions: TourDefinition[] = [
  adminWelcomeTour,
  caregiverWelcomeTour,
];

/**
 * Get a tour definition by ID
 */
export function getTourById(tourId: string): TourDefinition | undefined {
  return tourDefinitions.find(tour => tour.id === tourId);
}

/**
 * Get tours available for a specific role
 */
export function getToursForRole(role: string): TourDefinition[] {
  return tourDefinitions.filter(
    tour => !tour.roles || tour.roles.length === 0 || tour.roles.includes(role)
  );
}

/**
 * Get auto-start tours for a specific role, sorted by priority
 */
export function getAutoStartToursForRole(role: string): TourDefinition[] {
  return getToursForRole(role)
    .filter(tour => tour.autoStart)
    .sort((a, b) => (b.priority ?? 0) - (a.priority ?? 0));
}
