# Phase 9: Push Notifications and MFA Approval

## Overview

Implement push notification infrastructure to alert admins when incidents are reported and enable mobile-based MFA approval for web frontend logins. This phase also covers app store preparation for iOS App Store and Google Play Store deployment.

## Objectives

- Implement push notification infrastructure (Azure Notification Hubs or Firebase)
- Send push notifications to admins when any incident is reported
- Implement MFA approval flow for web authentication via mobile
- Add number matching security for MFA approval
- Prepare application for app store submission
- Complete app store listings and compliance requirements

## Prerequisites

- Phase 8 completed (care logging features functional)
- Azure Notification Hubs or Firebase Cloud Messaging configured
- Apple Push Notification service (APNs) certificates
- Firebase Cloud Messaging (FCM) server key
- Backend device registration storing push tokens

## Architecture Overview

### Push Notification Flow

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Incident       │     │  Backend API    │     │  Azure          │
│  Created        │────▶│  (Trigger)      │────▶│  Notification   │
│                 │     │                 │     │  Hubs           │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                        ┌────────────────────────────────┴────────┐
                        ▼                                         ▼
                ┌───────────────┐                         ┌───────────────┐
                │  APNs (iOS)   │                         │  FCM (Android)│
                └───────┬───────┘                         └───────┬───────┘
                        ▼                                         ▼
                ┌───────────────┐                         ┌───────────────┐
                │  Admin's      │                         │  Admin's      │
                │  iPhone       │                         │  Android      │
                └───────────────┘                         └───────────────┘
```

### MFA Approval Flow

```
1. User initiates login on Web Frontend
         │
         ▼
2. Backend generates MFA challenge with 2-digit number
         │
         ▼
3. Push notification sent to user's registered device(s)
         │
         ▼
4. User opens notification, sees 3 number options
         │
         ▼
5. User taps matching number (number matching)
         │
         ▼
6. Backend validates approval, completes login
         │
         ▼
7. Web frontend receives success, redirects to dashboard
         │
         ▼
   Timeout: 90 seconds - if no response, challenge expires
```

## Tasks

### 9.1 Push Notification Infrastructure Setup

**Description:** Configure Azure Notification Hubs and integrate with both iOS and Android.

**Deliverables:**

- Azure Notification Hubs resource configuration
- APNs certificate upload for iOS
- FCM server key configuration for Android
- Backend service for sending notifications:
  - IPushNotificationService interface
  - AzureNotificationHubService implementation
- Notification templates:
  - Incident alert template
  - MFA approval request template
- Cross-platform notification handling

**Acceptance Criteria:**

- Notifications deliver to iOS devices via APNs
- Notifications deliver to Android devices via FCM
- Backend can send targeted notifications by user
- Backend can send to all devices of a user
- Failed deliveries are logged
- Push tokens are refreshed when needed

### 9.2 Device Push Token Management

**Description:** Implement push token registration and refresh on mobile.

**Deliverables:**

- Push token acquisition on app launch
- Token registration with backend:
  - POST /api/devices/{id}/push-token
- Token refresh on change:
  - iOS: didRegisterForRemoteNotifications
  - Android: onNewToken callback
- Token cleanup on logout
- Handle token expiration

**Acceptance Criteria:**

- Push token is acquired on first launch
- Token is sent to backend after authentication
- Token updates when platform refreshes it
- Token is cleared on logout
- Multiple devices per user all receive notifications

### 9.3 Incident Alert Notifications for Admins

**Description:** Send push notifications to Admin and OrganizationAdministrator users when incidents are reported.

**Deliverables:**

- Backend incident notification service:
  - Triggered when incident is submitted (not draft)
  - Fetches all Admin and OrganizationAdministrator users in the organization
  - Sends notification to all their registered devices
- Notification content:
  - Title: "New Incident Reported"
  - Body: "{Client Name} - {Incident Type} at {Home Name}"
  - Severity indicator
  - Deep link to incident in app (or web URL)
- Notification handling in mobile app:
  - Foreground: Show in-app alert
  - Background: System notification
  - Tap: Open incident detail (if admin has mobile access) or deep link to web

**Acceptance Criteria:**

- All Admins and OrganizationAdministrators in organization receive notification
- Caregivers do NOT receive incident notifications (they create them)
- OrganizationSysadmin does NOT receive incident notifications (no PHI)
- Notification contains relevant incident summary
- Notifications arrive within seconds of submission
- Tapping notification opens relevant content
- Notifications work when app is backgrounded
- Notifications work when app is closed
- High severity incidents show with priority

### 9.4 MFA Approval Backend

**Description:** Implement backend support for push-based MFA approval.

**Deliverables:**

- MFA challenge generation:
  - Generate random 2-digit number (10-99)
  - Store challenge in cache with:
    - Challenge ID
    - User ID
    - Correct number
    - Created timestamp
    - 90-second expiration
  - Generate 2 decoy numbers
- API endpoints:
  - POST /api/auth/mfa/request - Initiate MFA challenge
  - POST /api/auth/mfa/approve - Approve from mobile
  - POST /api/auth/mfa/deny - Deny from mobile
  - GET /api/auth/mfa/status/{challengeId} - Poll for result
- Challenge expiration after 90 seconds
- One-time use (approve or deny once only)

**Acceptance Criteria:**

- Challenge is generated with random 2-digit number
- Three numbers presented (1 correct, 2 decoys)
- Challenge expires after 90 seconds
- Only one approval/denial allowed per challenge
- Expired challenges return appropriate error
- Challenge result is available to polling web client

### 9.5 Web Frontend MFA Integration

**Description:** Update web frontend to support push-based MFA.

**Deliverables:**

- Login flow update:
  - After password validation, check if user has registered mobile devices
  - If yes, offer "Approve on mobile" option
  - If no devices, fall back to passkey/TOTP
- MFA approval UI:
  - Display the 2-digit number prominently
  - Show waiting spinner
  - Poll for approval status
  - 90-second countdown timer
  - "Didn't receive notification?" fallback link
- Fallback options:
  - Use passkey instead
  - Resend notification
- Success/failure handling

**Acceptance Criteria:**

- Users with mobile devices see push MFA option
- Number is displayed clearly on web
- Countdown shows time remaining
- Successful approval logs user in
- Denial shows rejection message
- Timeout shows expiration message
- Fallback to passkey works

### 9.6 Mobile MFA Approval Screen

**Description:** Implement the MFA approval interface on mobile.

**Deliverables:**

- MFA approval notification:
  - Title: "Sign-in Request"
  - Body: "Approve sign-in to LenkCare Homes?"
  - Actionable notification (approve/deny buttons)
- MFA approval screen:
  - Login attempt details:
    - "Someone is trying to sign in to your account"
    - Location/IP if available
    - Timestamp
  - Number selection:
    - Display 3 numbers in random order
    - User must select the one shown on web
  - Approve/Deny buttons
  - Countdown timer (90 seconds)
- Security features:
  - Require biometric before showing numbers
  - Prevent screenshot (if possible)
  - Clear screen on backgrounding

**Acceptance Criteria:**

- Push notification arrives on all user devices
- Tapping opens MFA approval screen
- Three numbers are displayed
- Only matching number approves
- Wrong number shows error (limited attempts)
- Deny button rejects the login
- Biometric required to view numbers
- Expiration handled gracefully

### 9.7 Notification Preferences

**Description:** Allow users to manage notification settings.

**Deliverables:**

- Settings page options:
  - Enable/disable incident notifications (admin only)
  - Enable/disable MFA approval notifications
  - Quiet hours setting (optional)
- Backend storage of preferences
- Notification filtering based on preferences

**Acceptance Criteria:**

- Users can toggle notification types
- Preferences are synced with backend
- Disabled notifications are not sent
- Quiet hours respected (if implemented)
- Preferences are per-device

### 9.8 App Store Preparation - iOS

**Description:** Prepare iOS app for App Store submission.

**Deliverables:**

- App Store Connect setup:
  - App ID and bundle identifier
  - App name: "LenkCare Homes"
  - Category: Medical (or Business)
  - Privacy policy URL
  - Support URL
- Screenshots for all required sizes:
  - iPhone 6.7" (14 Pro Max)
  - iPhone 6.5" (11 Pro Max)
  - iPhone 5.5" (8 Plus)
  - iPad Pro 12.9"
- App description and keywords
- App icon (1024x1024)
- Privacy nutrition labels:
  - Data types collected
  - Data usage purposes
  - Data linked to user
- HIPAA compliance documentation
- TestFlight beta testing setup

**Acceptance Criteria:**

- App Store listing is complete
- All screenshots uploaded
- Privacy labels accurately reflect data usage
- TestFlight builds distribute successfully
- App passes App Store review guidelines check
- HIPAA/healthcare compliance addressed

### 9.9 App Store Preparation - Android

**Description:** Prepare Android app for Google Play Store submission.

**Deliverables:**

- Google Play Console setup:
  - App package name
  - App name: "LenkCare Homes"
  - Category: Medical (or Business)
  - Privacy policy URL
- Screenshots for required form factors:
  - Phone (16:9)
  - 7" tablet
  - 10" tablet
- Feature graphic (1024x500)
- App icon (512x512)
- App description (short and full)
- Data safety section:
  - Data types collected
  - Security practices
  - Data sharing
- Release track setup:
  - Internal testing
  - Closed testing
  - Open testing (optional)
  - Production

**Acceptance Criteria:**

- Play Console listing is complete
- All graphics uploaded
- Data safety section completed
- Internal testing track works
- Closed testing distributes to testers
- App passes pre-launch report checks

### 9.10 Security Audit and Hardening

**Description:** Security review of mobile application before release.

**Deliverables:**

- Security audit checklist:
  - [ ] No secrets in code or logs
  - [ ] Tokens stored in secure storage
  - [ ] Certificate pinning (optional)
  - [ ] Sensitive screens prevent screenshots
  - [ ] Session timeout implemented
  - [ ] Biometric bypasses prevented
  - [ ] Network calls use HTTPS only
  - [ ] Input validation on all forms
  - [ ] Error messages don't leak information
- ProGuard/R8 obfuscation (Android)
- App Transport Security (iOS)
- Jailbreak/root detection (optional)
- Vulnerability scan results

**Acceptance Criteria:**

- All security checklist items pass
- No sensitive data in logs
- Code obfuscation enabled
- Network security configured
- Security vulnerabilities addressed
- Third-party library audit complete

## Dependencies

- Phase 8 completed (care logging features)
- Azure Notification Hubs provisioned
- Apple Developer account with push certificates
- Google Play Developer account
- Privacy policy and support pages published

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Push notification delays | Medium | Monitor delivery; implement timeout handling |
| App Store rejection | High | Follow guidelines carefully; prepare appeal |
| MFA approval not received | Medium | Provide fallback authentication options |
| Security vulnerabilities found | High | Security audit before submission; bug bounty |
| Platform certification requirements | Medium | Research healthcare app requirements early |

## Success Criteria

- Push notifications deliver reliably to both platforms
- Admins and OrganizationAdministrators receive incident alerts within seconds
- OrganizationSysadmin does not receive PHI-related notifications
- MFA approval flow works end-to-end for all mobile-enabled roles
- Number matching prevents fraudulent approvals
- 90-second timeout is enforced
- iOS app approved in App Store
- Android app approved in Play Store
- Security audit passes with no critical issues
- Beta testers validate functionality

## Post-Launch

After app store approval:
- Monitor crash reports and analytics
- Respond to user feedback
- Plan feature updates based on usage
- Regular security updates
- Keep dependencies current
