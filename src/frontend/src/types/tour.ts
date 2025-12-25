/**
 * Tour system types for the guided onboarding experience
 */

/** Placement options for tour bubbles */
export type TourBubblePlacement =
  | 'top'
  | 'top-start'
  | 'top-end'
  | 'bottom'
  | 'bottom-start'
  | 'bottom-end'
  | 'left'
  | 'left-start'
  | 'left-end'
  | 'right'
  | 'right-start'
  | 'right-end'
  | 'auto';

/** How to locate the target element for a tour step */
export type TourTargetSelector =
  | { type: 'id'; value: string }
  | { type: 'data-tour'; value: string }
  | { type: 'selector'; value: string }
  | { type: 'role'; value: string; name?: string };

/** A single step in a tour */
export interface TourStep {
  /** Unique identifier for the step */
  id: string;
  /** Route where this step should be shown (matches pathname prefix) */
  route?: string;
  /** How to find the target element */
  target: TourTargetSelector;
  /** Title displayed in the bubble */
  title: string;
  /** Explanatory content */
  content: string;
  /** Where to position the bubble relative to target */
  placement?: TourBubblePlacement;
  /** If true, the step can be skipped if target is not found */
  optional?: boolean;
  /** Navigation action to perform before showing this step */
  beforeStep?: TourStepAction;
  /** Navigation action to perform after completing this step */
  afterStep?: TourStepAction;
  /** Custom spotlight padding around the target element */
  spotlightPadding?: number;
  /** Whether to disable interaction with the highlighted element */
  disableInteraction?: boolean;
  /** Scroll behavior when showing this step */
  scrollBehavior?: 'smooth' | 'auto' | 'none';
}

/** Actions that can be performed during tour navigation */
export interface TourStepAction {
  /** Navigate to a specific route */
  navigate?: string;
  /** Click on an element matching this selector */
  click?: string;
  /** Custom action identifier for special handling */
  custom?: string;
  /** Delay in ms before performing the action */
  delay?: number;
}

/** Definition of a complete tour */
export interface TourDefinition {
  /** Unique identifier for the tour */
  id: string;
  /** Human-readable name */
  name: string;
  /** Description of what the tour covers */
  description?: string;
  /** Version number for tracking tour updates */
  version: number;
  /** The steps in order */
  steps: TourStep[];
  /** Roles that should see this tour (empty = all roles) */
  roles?: string[];
  /** Whether this tour auto-starts for new users */
  autoStart?: boolean;
  /** Priority when multiple tours are eligible (higher = more important) */
  priority?: number;
}

/** Current state of an active tour */
export interface TourState {
  /** Whether a tour is currently active */
  isActive: boolean;
  /** The ID of the current tour */
  tourId: string | null;
  /** Current step index (0-based) */
  currentStepIndex: number;
  /** Whether the tour is paused (e.g., waiting for navigation) */
  isPaused: boolean;
}

/** User's tour completion status */
export interface UserTourStatus {
  /** User ID */
  userId: string;
  /** Map of tour ID to completion status */
  completedTours: Record<string, TourCompletionInfo>;
  /** Map of tour ID to dismiss info */
  dismissedTours: Record<string, TourDismissInfo>;
}

/** Information about a completed tour */
export interface TourCompletionInfo {
  /** When the tour was completed */
  completedAt: string;
  /** Version of the tour that was completed */
  version: number;
}

/** Information about a dismissed tour */
export interface TourDismissInfo {
  /** When the tour was dismissed */
  dismissedAt: string;
  /** Which step the user was on when dismissing */
  atStepIndex: number;
  /** Version of the tour that was dismissed */
  version: number;
}

/** Analytics event types for tour tracking */
export type TourAnalyticsEvent =
  | { type: 'tour_started'; tourId: string; version: number }
  | { type: 'tour_step_viewed'; tourId: string; stepId: string; stepIndex: number }
  | { type: 'tour_step_completed'; tourId: string; stepId: string; stepIndex: number }
  | { type: 'tour_completed'; tourId: string; version: number; totalSteps: number }
  | { type: 'tour_skipped'; tourId: string; version: number; atStepIndex: number; stepId: string }
  | { type: 'tour_step_target_not_found'; tourId: string; stepId: string; targetSelector: string };

/** Callback type for analytics events */
export type TourAnalyticsCallback = (event: TourAnalyticsEvent) => void;

/** Context value provided by TourProvider */
export interface TourContextValue {
  /** Current tour state */
  state: TourState;
  /** Start a specific tour */
  startTour: (tourId: string) => void;
  /** End the current tour (skip/dismiss) */
  endTour: () => void;
  /** Complete the current tour */
  completeTour: () => void;
  /** Go to the next step */
  nextStep: () => void;
  /** Go to the previous step */
  prevStep: () => void;
  /** Get the current step definition */
  getCurrentStep: () => TourStep | null;
  /** Get the current tour definition */
  getCurrentTour: () => TourDefinition | null;
  /** Check if a tour has been completed */
  isTourCompleted: (tourId: string) => boolean;
  /** Check if a tour has been dismissed */
  isTourDismissed: (tourId: string) => boolean;
  /** Reset a tour's completion status */
  resetTour: (tourId: string) => void;
  /** Pause the current tour */
  pauseTour: () => void;
  /** Resume the current tour */
  resumeTour: () => void;
  /** Register an analytics callback */
  onAnalytics: (callback: TourAnalyticsCallback) => () => void;
  /** Whether tours are enabled (user preferences) */
  toursEnabled: boolean;
  /** Toggle tour enablement */
  setToursEnabled: (enabled: boolean) => void;
}
