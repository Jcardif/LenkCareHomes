'use client';

import React, { useEffect, useState, useRef, useCallback, useSyncExternalStore } from 'react';
import { createPortal } from 'react-dom';
import { Button, Typography, Space, Flex, Progress } from 'antd';
import {
  CloseOutlined,
  ArrowLeftOutlined,
  ArrowRightOutlined,
  CheckOutlined,
} from '@ant-design/icons';
import { useTour } from '@/contexts/TourContext';
import type { TourBubblePlacement, TourTargetSelector } from '@/types/tour';

const { Title, Paragraph } = Typography;

interface Position {
  top: number;
  left: number;
  width: number;
  height: number;
}

interface BubblePosition {
  top: number;
  left: number;
  arrowPosition: 'top' | 'bottom' | 'left' | 'right';
  arrowOffset: number;
}

/**
 * Find the target element based on the selector configuration
 */
function findTargetElement(selector: TourTargetSelector): HTMLElement | null {
  switch (selector.type) {
    case 'id':
      return document.getElementById(selector.value);
    case 'data-tour':
      return document.querySelector(`[data-tour="${selector.value}"]`);
    case 'selector':
      return document.querySelector(selector.value);
    case 'role': {
      const elements = document.querySelectorAll(`[role="${selector.value}"]`);
      if (selector.name) {
        return Array.from(elements).find(
          el => el.getAttribute('aria-label')?.includes(selector.name!) ||
                el.textContent?.includes(selector.name!)
        ) as HTMLElement || null;
      }
      return elements[0] as HTMLElement || null;
    }
    default:
      return null;
  }
}

/**
 * Calculate optimal bubble placement to keep it on screen
 */
function calculateBubblePosition(
  targetRect: DOMRect,
  bubbleSize: { width: number; height: number },
  preferredPlacement: TourBubblePlacement = 'auto',
  spotlightPadding: number = 8
): BubblePosition {
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;
  const padding = 16;
  const arrowSize = 12;
  
  const targetCenter = {
    x: targetRect.left + targetRect.width / 2,
    y: targetRect.top + targetRect.height / 2,
  };

  // Calculate available space in each direction
  const spaceAbove = targetRect.top - spotlightPadding;
  const spaceBelow = viewportHeight - targetRect.bottom - spotlightPadding;
  const spaceLeft = targetRect.left - spotlightPadding;
  const spaceRight = viewportWidth - targetRect.right - spotlightPadding;

  let placement = preferredPlacement;
  
  // If auto, pick the best placement based on available space
  if (placement === 'auto') {
    const spaces = [
      { placement: 'bottom' as const, space: spaceBelow },
      { placement: 'top' as const, space: spaceAbove },
      { placement: 'right' as const, space: spaceRight },
      { placement: 'left' as const, space: spaceLeft },
    ];
    
    const sorted = spaces.sort((a, b) => b.space - a.space);
    placement = sorted[0].placement;
  }

  let top: number;
  let left: number;
  let arrowPosition: 'top' | 'bottom' | 'left' | 'right';
  let arrowOffset: number;

  const basePlacement = placement.split('-')[0] as 'top' | 'bottom' | 'left' | 'right';
  const alignment = placement.split('-')[1] as 'start' | 'end' | undefined;

  switch (basePlacement) {
    case 'top':
      top = targetRect.top - spotlightPadding - bubbleSize.height - arrowSize - padding;
      arrowPosition = 'bottom';
      break;
    case 'bottom':
      top = targetRect.bottom + spotlightPadding + arrowSize + padding;
      arrowPosition = 'top';
      break;
    case 'left':
      left = targetRect.left - spotlightPadding - bubbleSize.width - arrowSize - padding;
      arrowPosition = 'right';
      break;
    case 'right':
      left = targetRect.right + spotlightPadding + arrowSize + padding;
      arrowPosition = 'left';
      break;
    default:
      top = targetRect.bottom + spotlightPadding + arrowSize + padding;
      arrowPosition = 'top';
  }

  // Calculate horizontal position for top/bottom placements
  if (basePlacement === 'top' || basePlacement === 'bottom') {
    if (alignment === 'start') {
      left = targetRect.left - spotlightPadding;
    } else if (alignment === 'end') {
      left = targetRect.right + spotlightPadding - bubbleSize.width;
    } else {
      left = targetCenter.x - bubbleSize.width / 2;
    }
    
    // Keep within viewport
    left = Math.max(padding, Math.min(left, viewportWidth - bubbleSize.width - padding));
    arrowOffset = Math.max(24, Math.min(targetCenter.x - left, bubbleSize.width - 24));
  }
  
  // Calculate vertical position for left/right placements
  if (basePlacement === 'left' || basePlacement === 'right') {
    if (alignment === 'start') {
      top = targetRect.top - spotlightPadding;
    } else if (alignment === 'end') {
      top = targetRect.bottom + spotlightPadding - bubbleSize.height;
    } else {
      top = targetCenter.y - bubbleSize.height / 2;
    }
    
    // Keep within viewport
    top = Math.max(padding, Math.min(top!, viewportHeight - bubbleSize.height - padding));
    arrowOffset = Math.max(24, Math.min(targetCenter.y - top!, bubbleSize.height - 24));
  }

  // Ensure top is set for top/bottom
  if (top! < 0) {
    top = targetRect.bottom + spotlightPadding + arrowSize + padding;
    arrowPosition = 'top';
  }

  return {
    top: top!,
    left: left!,
    arrowPosition,
    arrowOffset: arrowOffset!,
  };
}

/**
 * TourOverlay component - renders the spotlight, dimming, and bubble
 */
export function TourOverlay() {
  const {
    state,
    getCurrentStep,
    getCurrentTour,
    nextStep,
    prevStep,
    endTour,
  } = useTour();

  const [targetPosition, setTargetPosition] = useState<Position | null>(null);
  const [bubblePosition, setBubblePosition] = useState<BubblePosition | null>(null);
  const [isVisible, setIsVisible] = useState(false);
  const bubbleRef = useRef<HTMLDivElement>(null);

  const currentStep = getCurrentStep();
  const currentTour = getCurrentTour();

  // Use useSyncExternalStore for hydration-safe client-side detection
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false
  );

  // Find and track target element
  const updatePositions = useCallback(() => {
    if (!currentStep || state.isPaused) {
      setIsVisible(false);
      return;
    }

    const target = findTargetElement(currentStep.target);
    
    if (!target) {
      // If step is optional and target not found, skip to next
      if (currentStep.optional) {
        nextStep();
        return;
      }
      
      // Hide overlay if target not found
      setIsVisible(false);
      return;
    }

    const rect = target.getBoundingClientRect();
    const padding = currentStep.spotlightPadding ?? 8;

    setTargetPosition({
      top: rect.top - padding,
      left: rect.left - padding,
      width: rect.width + padding * 2,
      height: rect.height + padding * 2,
    });

    // Scroll target into view if needed
    if (currentStep.scrollBehavior !== 'none') {
      target.scrollIntoView({
        behavior: currentStep.scrollBehavior || 'smooth',
        block: 'center',
        inline: 'center',
      });
    }

    // Calculate bubble position after a short delay to allow DOM updates
    setTimeout(() => {
      if (bubbleRef.current) {
        const bubbleRect = bubbleRef.current.getBoundingClientRect();
        const newBubblePos = calculateBubblePosition(
          rect,
          { width: bubbleRect.width || 340, height: bubbleRect.height || 180 },
          currentStep.placement,
          padding
        );
        setBubblePosition(newBubblePos);
        setIsVisible(true);
      }
    }, 50);
  }, [currentStep, state.isPaused, nextStep]);

  // Update positions when step changes or window resizes
  useEffect(() => {
    if (!state.isActive || state.isPaused) return;

    // Schedule initial position update for next frame to avoid synchronous setState in effect
    const rafId = requestAnimationFrame(() => {
      updatePositions();
    });

    const handleResize = () => updatePositions();
    window.addEventListener('resize', handleResize);
    window.addEventListener('scroll', handleResize, true);

    return () => {
      cancelAnimationFrame(rafId);
      window.removeEventListener('resize', handleResize);
      window.removeEventListener('scroll', handleResize, true);
    };
  }, [state.isActive, state.isPaused, state.currentStepIndex, updatePositions]);

  // Handle keyboard navigation
  useEffect(() => {
    if (!state.isActive) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      switch (e.key) {
        case 'Escape':
          e.preventDefault();
          endTour();
          break;
        case 'ArrowRight':
        case 'Enter':
          e.preventDefault();
          nextStep();
          break;
        case 'ArrowLeft':
          e.preventDefault();
          prevStep();
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [state.isActive, nextStep, prevStep, endTour]);

  // Focus management for accessibility
  useEffect(() => {
    if (isVisible && bubbleRef.current) {
      bubbleRef.current.focus();
    }
  }, [isVisible, state.currentStepIndex]);

  if (!mounted || !state.isActive || !currentStep || !currentTour || state.isPaused) {
    return null;
  }

  const isLastStep = state.currentStepIndex >= currentTour.steps.length - 1;
  const isFirstStep = state.currentStepIndex === 0;
  const progress = ((state.currentStepIndex + 1) / currentTour.steps.length) * 100;

  const overlayContent = (
    <div
      role="dialog"
      aria-modal="true"
      aria-label={`${currentTour.name} - Step ${state.currentStepIndex + 1} of ${currentTour.steps.length}`}
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 10000,
        pointerEvents: 'none',
      }}
    >
      {/* Dimmed backdrop with spotlight cutout */}
      <svg
        width="100%"
        height="100%"
        style={{
          position: 'absolute',
          inset: 0,
          pointerEvents: isVisible ? 'auto' : 'none',
        }}
      >
        <defs>
          <mask id="tour-spotlight-mask">
            <rect width="100%" height="100%" fill="white" />
            {targetPosition && (
              <rect
                x={targetPosition.left}
                y={targetPosition.top}
                width={targetPosition.width}
                height={targetPosition.height}
                rx={8}
                fill="black"
              />
            )}
          </mask>
        </defs>
        <rect
          width="100%"
          height="100%"
          fill="rgba(0, 0, 0, 0.65)"
          mask="url(#tour-spotlight-mask)"
          style={{
            transition: 'opacity 0.3s ease',
            opacity: isVisible ? 1 : 0,
          }}
          onClick={endTour}
        />
      </svg>

      {/* Spotlight border */}
      {targetPosition && isVisible && (
        <div
          style={{
            position: 'absolute',
            top: targetPosition.top,
            left: targetPosition.left,
            width: targetPosition.width,
            height: targetPosition.height,
            borderRadius: 8,
            boxShadow: '0 0 0 3px rgba(90, 122, 107, 0.6), 0 0 20px rgba(90, 122, 107, 0.3)',
            transition: 'all 0.3s ease',
            pointerEvents: currentStep.disableInteraction ? 'none' : 'auto',
          }}
        />
      )}

      {/* Tour bubble */}
      <div
        ref={bubbleRef}
        tabIndex={-1}
        role="tooltip"
        aria-live="polite"
        style={{
          position: 'absolute',
          top: bubblePosition?.top ?? 0,
          left: bubblePosition?.left ?? 0,
          width: 340,
          backgroundColor: '#ffffff',
          borderRadius: 12,
          boxShadow: '0 8px 32px rgba(0, 0, 0, 0.2), 0 4px 16px rgba(0, 0, 0, 0.1)',
          pointerEvents: 'auto',
          opacity: isVisible ? 1 : 0,
          transform: isVisible ? 'scale(1)' : 'scale(0.95)',
          transition: 'opacity 0.3s ease, transform 0.3s ease',
          outline: 'none',
        }}
      >
        {/* Arrow */}
        {bubblePosition && (
          <div
            style={{
              position: 'absolute',
              width: 16,
              height: 16,
              backgroundColor: '#ffffff',
              transform: 'rotate(45deg)',
              boxShadow: bubblePosition.arrowPosition === 'top' || bubblePosition.arrowPosition === 'left'
                ? '-2px -2px 4px rgba(0, 0, 0, 0.08)'
                : '2px 2px 4px rgba(0, 0, 0, 0.08)',
              ...(bubblePosition.arrowPosition === 'top' && {
                top: -8,
                left: bubblePosition.arrowOffset - 8,
              }),
              ...(bubblePosition.arrowPosition === 'bottom' && {
                bottom: -8,
                left: bubblePosition.arrowOffset - 8,
              }),
              ...(bubblePosition.arrowPosition === 'left' && {
                left: -8,
                top: bubblePosition.arrowOffset - 8,
              }),
              ...(bubblePosition.arrowPosition === 'right' && {
                right: -8,
                top: bubblePosition.arrowOffset - 8,
              }),
            }}
          />
        )}

        {/* Content */}
        <div style={{ padding: 20 }}>
          {/* Header */}
          <Flex justify="space-between" align="flex-start" style={{ marginBottom: 12 }}>
            <Title
              level={5}
              style={{ margin: 0, color: '#2d3732', fontSize: 16, flex: 1 }}
            >
              {currentStep.title}
            </Title>
            <Button
              type="text"
              size="small"
              icon={<CloseOutlined aria-hidden="true" />}
              onClick={endTour}
              aria-label="Close tour"
              style={{ marginLeft: 8, marginTop: -4 }}
            />
          </Flex>

          {/* Body */}
          <Paragraph style={{ margin: 0, color: '#6b7770', fontSize: 14, lineHeight: 1.6 }}>
            {currentStep.content}
          </Paragraph>

          {/* Progress */}
          <div style={{ marginTop: 16, marginBottom: 12 }}>
            <Progress
              percent={progress}
              showInfo={false}
              strokeColor="#5a7a6b"
              trailColor="#ebeeed"
              size="small"
            />
            <div style={{ textAlign: 'center', fontSize: 12, color: '#9ca5a0', marginTop: 4 }}>
              Step {state.currentStepIndex + 1} of {currentTour.steps.length}
            </div>
          </div>

          {/* Footer actions */}
          <Flex justify="space-between" align="center">
            <Button
              type="text"
              size="small"
              onClick={endTour}
              style={{ color: '#9ca5a0' }}
            >
              Skip tour
            </Button>
            <Space>
              {!isFirstStep && (
                <Button
                  icon={<ArrowLeftOutlined aria-hidden="true" />}
                  onClick={prevStep}
                  aria-label="Previous step"
                >
                  Back
                </Button>
              )}
              <Button
                type="primary"
                icon={isLastStep ? <CheckOutlined aria-hidden="true" /> : <ArrowRightOutlined aria-hidden="true" />}
                onClick={nextStep}
                aria-label={isLastStep ? 'Finish tour' : 'Next step'}
                style={{
                  backgroundColor: '#5a7a6b',
                  borderColor: '#5a7a6b',
                }}
              >
                {isLastStep ? 'Done' : 'Next'}
              </Button>
            </Space>
          </Flex>
        </div>
      </div>
    </div>
  );

  return createPortal(overlayContent, document.body);
}

export default TourOverlay;
