'use client';

import React, {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  useRef,
  type ReactNode,
} from 'react';
import { usePathname, useRouter } from 'next/navigation';
import type {
  TourState,
  TourStep,
  TourDefinition,
  TourContextValue,
  TourCompletionInfo,
  TourDismissInfo,
  TourAnalyticsCallback,
  TourAnalyticsEvent,
} from '@/types/tour';
import { getTourById, getAutoStartToursForRole, tourDefinitions } from '@/lib/tour-definitions';
import { useAuth } from '@/contexts/AuthContext';
import { tourApi } from '@/lib/api';

const TOUR_STATUS_STORAGE_KEY = 'lenkcare_tour_status';
const TOURS_ENABLED_KEY = 'lenkcare_tours_enabled';

interface StoredTourStatus {
  completedTours: Record<string, TourCompletionInfo>;
  dismissedTours: Record<string, TourDismissInfo>;
}

const TourContext = createContext<TourContextValue | undefined>(undefined);

interface TourProviderProps {
  children: ReactNode;
}

export function TourProvider({ children }: TourProviderProps) {
  const pathname = usePathname();
  const router = useRouter();
  const { isAuthenticated, user, roles } = useAuth();
  
  const [state, setState] = useState<TourState>({
    isActive: false,
    tourId: null,
    currentStepIndex: 0,
    isPaused: false,
  });
  
  // Initialize from localStorage using lazy state initialization
  const [completedTours, setCompletedTours] = useState<Record<string, TourCompletionInfo>>(() => {
    if (typeof window === 'undefined') return {};
    try {
      const stored = localStorage.getItem(TOUR_STATUS_STORAGE_KEY);
      if (stored) {
        const parsed: StoredTourStatus = JSON.parse(stored);
        return parsed.completedTours || {};
      }
    } catch {
      // Invalid stored data, use empty
    }
    return {};
  });
  
  const [dismissedTours, setDismissedTours] = useState<Record<string, TourDismissInfo>>(() => {
    if (typeof window === 'undefined') return {};
    try {
      const stored = localStorage.getItem(TOUR_STATUS_STORAGE_KEY);
      if (stored) {
        const parsed: StoredTourStatus = JSON.parse(stored);
        return parsed.dismissedTours || {};
      }
    } catch {
      // Invalid stored data, use empty
    }
    return {};
  });
  
  const [toursEnabled, setToursEnabledState] = useState(() => {
    if (typeof window === 'undefined') return true;
    const enabledStored = localStorage.getItem(TOURS_ENABLED_KEY);
    return enabledStored !== null ? enabledStored === 'true' : true;
  });
  const [hasCheckedBackend, setHasCheckedBackend] = useState(false);
  
  const analyticsCallbacks = useRef<Set<TourAnalyticsCallback>>(new Set());

  // Sync with backend when user is authenticated
  useEffect(() => {
    if (!isAuthenticated || !user || hasCheckedBackend) return;

    const syncWithBackend = async () => {
      try {
        // First check the user's tourCompleted status from auth context
        // This is more efficient as it's already loaded with the user
        const tourCompletedFromUser = user.tourCompleted ?? false;
        
        // Helper to save to localStorage
        const saveStatus = (completed: Record<string, TourCompletionInfo>, dismissed: Record<string, TourDismissInfo>) => {
          const status: StoredTourStatus = {
            completedTours: completed,
            dismissedTours: dismissed,
          };
          localStorage.setItem(TOUR_STATUS_STORAGE_KEY, JSON.stringify(status));
        };
        
        // If user.tourCompleted is true, update local state without API call
        if (tourCompletedFromUser) {
          const userRole = roles[0] || 'Admin';
          const autoStartTours = getAutoStartToursForRole(userRole);
          
          const newCompleted = { ...completedTours };
          autoStartTours.forEach(tour => {
            if (!newCompleted[tour.id]) {
              newCompleted[tour.id] = {
                completedAt: new Date().toISOString(),
                version: tour.version,
              };
            }
          });
          
          setCompletedTours(newCompleted);
          saveStatus(newCompleted, dismissedTours);
          setHasCheckedBackend(true);
          return;
        }

        // If user hasn't completed tour according to auth context,
        // verify with a dedicated API call (handles edge cases)
        const backendStatus = await tourApi.getTourStatus();
        
        if (backendStatus.tourCompleted) {
          const userRole = roles[0] || 'Admin';
          const autoStartTours = getAutoStartToursForRole(userRole);
          
          const newCompleted = { ...completedTours };
          autoStartTours.forEach(tour => {
            if (!newCompleted[tour.id]) {
              newCompleted[tour.id] = {
                completedAt: new Date().toISOString(),
                version: tour.version,
              };
            }
          });
          
          setCompletedTours(newCompleted);
          saveStatus(newCompleted, dismissedTours);
        }
        
        setHasCheckedBackend(true);
      } catch {
        // If backend check fails, continue with local state only
        setHasCheckedBackend(true);
      }
    };

    void syncWithBackend();
  }, [isAuthenticated, user, hasCheckedBackend, roles, completedTours, dismissedTours]);

  const saveTourStatus = useCallback((
    completed: Record<string, TourCompletionInfo>,
    dismissed: Record<string, TourDismissInfo>
  ) => {
    const status: StoredTourStatus = {
      completedTours: completed,
      dismissedTours: dismissed,
    };
    localStorage.setItem(TOUR_STATUS_STORAGE_KEY, JSON.stringify(status));
  }, []);

  const emitAnalytics = useCallback((event: TourAnalyticsEvent) => {
    // Log to console for now (TODO: Integrate with Seq for system logs)
    console.log('[Tour Analytics]', event);
    
    analyticsCallbacks.current.forEach(callback => {
      try {
        callback(event);
      } catch {
        // Ignore callback errors
      }
    });
  }, []);

  const getCurrentTour = useCallback((): TourDefinition | null => {
    if (!state.tourId) return null;
    return getTourById(state.tourId) ?? null;
  }, [state.tourId]);

  const getCurrentStep = useCallback((): TourStep | null => {
    const tour = getCurrentTour();
    if (!tour || state.currentStepIndex >= tour.steps.length) return null;
    return tour.steps[state.currentStepIndex];
  }, [getCurrentTour, state.currentStepIndex]);

  const startTour = useCallback((tourId: string) => {
    const tour = getTourById(tourId);
    if (!tour) {
      console.warn(`Tour not found: ${tourId}`);
      return;
    }
    
    setState({
      isActive: true,
      tourId,
      currentStepIndex: 0,
      isPaused: false,
    });
    
    emitAnalytics({
      type: 'tour_started',
      tourId,
      version: tour.version,
    });
    
    // Navigate to first step's route if needed
    const firstStep = tour.steps[0];
    if (firstStep?.route && pathname !== firstStep.route) {
      router.push(firstStep.route);
    }
  }, [pathname, router, emitAnalytics]);

  // Auto-start tour for new users after checking backend
  useEffect(() => {
    if (!isAuthenticated || !user || !hasCheckedBackend || !toursEnabled) return;
    if (state.isActive) return; // Don't auto-start if a tour is already active
    
    const userRole = roles[0] || 'Admin';
    const autoStartTours = getAutoStartToursForRole(userRole);
    
    // Find the first auto-start tour that hasn't been completed or dismissed
    for (const tour of autoStartTours) {
      const isCompleted = completedTours[tour.id]?.version === tour.version;
      const isDismissed = dismissedTours[tour.id]?.version === tour.version;
      
      if (!isCompleted && !isDismissed) {
        // Wait a bit for the page to render before starting the tour
        const timer = setTimeout(() => {
          startTour(tour.id);
        }, 1000);
        
        return () => clearTimeout(timer);
      }
    }
  }, [isAuthenticated, user, hasCheckedBackend, toursEnabled, state.isActive, roles, completedTours, dismissedTours, startTour]);

  const endTour = useCallback(() => {
    const tour = getCurrentTour();
    const currentStep = getCurrentStep();
    
    if (tour && currentStep) {
      const newDismissed = {
        ...dismissedTours,
        [tour.id]: {
          dismissedAt: new Date().toISOString(),
          atStepIndex: state.currentStepIndex,
          version: tour.version,
        },
      };
      setDismissedTours(newDismissed);
      saveTourStatus(completedTours, newDismissed);
      
      emitAnalytics({
        type: 'tour_skipped',
        tourId: tour.id,
        version: tour.version,
        atStepIndex: state.currentStepIndex,
        stepId: currentStep.id,
      });
    }
    
    setState({
      isActive: false,
      tourId: null,
      currentStepIndex: 0,
      isPaused: false,
    });
  }, [getCurrentTour, getCurrentStep, state.currentStepIndex, dismissedTours, completedTours, saveTourStatus, emitAnalytics]);

  const completeTour = useCallback(async () => {
    const tour = getCurrentTour();
    
    if (tour) {
      const newCompleted = {
        ...completedTours,
        [tour.id]: {
          completedAt: new Date().toISOString(),
          version: tour.version,
        },
      };
      setCompletedTours(newCompleted);
      saveTourStatus(newCompleted, dismissedTours);
      
      emitAnalytics({
        type: 'tour_completed',
        tourId: tour.id,
        version: tour.version,
        totalSteps: tour.steps.length,
      });
      
      // If this is an auto-start welcome tour, mark as completed on backend
      if (tour.autoStart && isAuthenticated) {
        try {
          await tourApi.completeTour();
        } catch {
          // Silently fail - we've already saved locally
          console.warn('Failed to save tour completion to backend');
        }
      }
    }
    
    setState({
      isActive: false,
      tourId: null,
      currentStepIndex: 0,
      isPaused: false,
    });
  }, [getCurrentTour, completedTours, dismissedTours, saveTourStatus, emitAnalytics, isAuthenticated]);

  const nextStep = useCallback(() => {
    const tour = getCurrentTour();
    const currentStep = getCurrentStep();
    
    if (!tour || !currentStep) return;
    
    emitAnalytics({
      type: 'tour_step_completed',
      tourId: tour.id,
      stepId: currentStep.id,
      stepIndex: state.currentStepIndex,
    });
    
    const nextIndex = state.currentStepIndex + 1;
    
    if (nextIndex >= tour.steps.length) {
      void completeTour();
      return;
    }
    
    const nextStepDef = tour.steps[nextIndex];
    
    // Navigate if the next step is on a different route
    if (nextStepDef.route && pathname !== nextStepDef.route) {
      setState(prev => ({ ...prev, isPaused: true }));
      router.push(nextStepDef.route);
      
      // Resume after navigation
      setTimeout(() => {
        setState(prev => ({
          ...prev,
          currentStepIndex: nextIndex,
          isPaused: false,
        }));
        
        emitAnalytics({
          type: 'tour_step_viewed',
          tourId: tour.id,
          stepId: nextStepDef.id,
          stepIndex: nextIndex,
        });
      }, 500);
    } else {
      setState(prev => ({
        ...prev,
        currentStepIndex: nextIndex,
      }));
      
      emitAnalytics({
        type: 'tour_step_viewed',
        tourId: tour.id,
        stepId: nextStepDef.id,
        stepIndex: nextIndex,
      });
    }
  }, [getCurrentTour, getCurrentStep, state.currentStepIndex, pathname, router, completeTour, emitAnalytics]);

  const prevStep = useCallback(() => {
    const tour = getCurrentTour();
    
    if (!tour || state.currentStepIndex <= 0) return;
    
    const prevIndex = state.currentStepIndex - 1;
    const prevStepDef = tour.steps[prevIndex];
    
    // Navigate if the previous step is on a different route
    if (prevStepDef.route && pathname !== prevStepDef.route) {
      setState(prev => ({ ...prev, isPaused: true }));
      router.push(prevStepDef.route);
      
      setTimeout(() => {
        setState(prev => ({
          ...prev,
          currentStepIndex: prevIndex,
          isPaused: false,
        }));
      }, 500);
    } else {
      setState(prev => ({
        ...prev,
        currentStepIndex: prevIndex,
      }));
    }
  }, [getCurrentTour, state.currentStepIndex, pathname, router]);

  const isTourCompleted = useCallback((tourId: string): boolean => {
    const tour = getTourById(tourId);
    if (!tour) return false;
    return completedTours[tourId]?.version === tour.version;
  }, [completedTours]);

  const isTourDismissed = useCallback((tourId: string): boolean => {
    const tour = getTourById(tourId);
    if (!tour) return false;
    return dismissedTours[tourId]?.version === tour.version;
  }, [dismissedTours]);

  const resetTour = useCallback(async (tourId: string) => {
    const newCompleted = { ...completedTours };
    const newDismissed = { ...dismissedTours };
    delete newCompleted[tourId];
    delete newDismissed[tourId];
    
    setCompletedTours(newCompleted);
    setDismissedTours(newDismissed);
    saveTourStatus(newCompleted, newDismissed);
    
    // Reset on backend if authenticated
    if (isAuthenticated) {
      try {
        await tourApi.resetTour();
      } catch {
        console.warn('Failed to reset tour on backend');
      }
    }
  }, [completedTours, dismissedTours, saveTourStatus, isAuthenticated]);

  const pauseTour = useCallback(() => {
    setState(prev => ({ ...prev, isPaused: true }));
  }, []);

  const resumeTour = useCallback(() => {
    setState(prev => ({ ...prev, isPaused: false }));
  }, []);

  const onAnalytics = useCallback((callback: TourAnalyticsCallback) => {
    analyticsCallbacks.current.add(callback);
    return () => {
      analyticsCallbacks.current.delete(callback);
    };
  }, []);

  const setToursEnabled = useCallback((enabled: boolean) => {
    setToursEnabledState(enabled);
    localStorage.setItem(TOURS_ENABLED_KEY, String(enabled));
    
    // If disabling while a tour is active, end it
    if (!enabled && state.isActive) {
      endTour();
    }
  }, [state.isActive, endTour]);

  const value: TourContextValue = {
    state,
    startTour,
    endTour,
    completeTour,
    nextStep,
    prevStep,
    getCurrentStep,
    getCurrentTour,
    isTourCompleted,
    isTourDismissed,
    resetTour,
    pauseTour,
    resumeTour,
    onAnalytics,
    toursEnabled,
    setToursEnabled,
  };

  return (
    <TourContext.Provider value={value}>
      {children}
    </TourContext.Provider>
  );
}

export function useTour() {
  const context = useContext(TourContext);
  if (context === undefined) {
    throw new Error('useTour must be used within a TourProvider');
  }
  return context;
}

/**
 * Hook to get all available tours for the current user
 */
export function useAvailableTours() {
  const { roles } = useAuth();
  const { isTourCompleted, isTourDismissed } = useTour();
  
  const userRole = roles[0] || 'Admin';
  
  return tourDefinitions
    .filter(tour => !tour.roles || tour.roles.length === 0 || tour.roles.includes(userRole))
    .map(tour => ({
      ...tour,
      isCompleted: isTourCompleted(tour.id),
      isDismissed: isTourDismissed(tour.id),
    }));
}
