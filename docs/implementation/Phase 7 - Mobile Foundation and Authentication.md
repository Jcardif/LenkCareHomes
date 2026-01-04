# Phase 7: Mobile Foundation and Authentication

## Overview

Establish the mobile application foundation using .NET MAUI, targeting iOS and Android platforms. This phase focuses on project structure, authentication with biometric support, and core navigation to enable caregivers, admins, and organization administrators to view their assigned homes, clients, and upcoming appointments from personal mobile devices.

## Objectives

- Set up .NET MAUI project structure with shared codebase for iOS and Android
- Implement mobile authentication flow with biometric support (Face ID, Touch ID, fingerprint)
- Create device registration for push notifications and MFA approval
- Build core navigation (home list → client list → client detail)
- Display upcoming appointments for assigned clients (read-only)
- Integrate with existing backend API

## Prerequisites

- Phase 6 completed (multi-tenancy implemented)
- Backend API operational and accessible
- Apple Developer account for iOS deployment
- Google Play Developer account for Android deployment
- Azure Notification Hubs configured for push notifications

## Architecture Overview

### Mobile Platform

```
┌─────────────────────────────────────────────────────────────────┐
│  .NET MAUI Application                                          │
├─────────────────────────────────────────────────────────────────┤
│  Platforms: iOS, Android                                        │
│  Shared Code: ~95% (UI, ViewModels, Services)                  │
│  Platform-Specific: Biometrics, Push Notifications             │
└─────────────────────────────────────────────────────────────────┘
         │
         │ HTTPS (REST API)
         ▼
┌─────────────────────────────────────────────────────────────────┐
│  Existing .NET 10 Backend API                                   │
├─────────────────────────────────────────────────────────────────┤
│  + New endpoints for device registration                        │
│  + Push notification service integration                        │
│  + MFA approval endpoints                                        │
└─────────────────────────────────────────────────────────────────┘
```

### Project Structure

```
src/
├── mobile/
│   └── LenkCareHomes.Mobile/
│       ├── LenkCareHomes.Mobile.sln
│       ├── LenkCareHomes.Mobile/              # Shared MAUI project
│       │   ├── App.xaml
│       │   ├── MauiProgram.cs
│       │   ├── Views/                         # XAML pages
│       │   ├── ViewModels/                    # MVVM ViewModels
│       │   ├── Services/                      # API clients, auth
│       │   ├── Models/                        # DTOs (shared with backend)
│       │   ├── Controls/                      # Custom controls
│       │   ├── Resources/                     # Styles, images, fonts
│       │   └── Platforms/
│       │       ├── iOS/
│       │       └── Android/
│       └── LenkCareHomes.Mobile.Tests/        # Unit tests
```

### Device Registration Model

```
┌─────────────────────────────────────────────────────────────────┐
│  UserDevice                                                      │
├─────────────────────────────────────────────────────────────────┤
│  Id (Guid, PK)                                                  │
│  UserId (Guid, FK to Users)                                     │
│  DeviceId (string) - Unique device identifier                   │
│  DeviceName (string) - "iPhone 15 Pro", "Samsung Galaxy S24"   │
│  Platform (string) - "iOS" or "Android"                         │
│  PushToken (string) - FCM/APNs token for push notifications    │
│  BiometricEnabled (bool)                                        │
│  LastActiveAt (DateTime)                                        │
│  RegisteredAt (DateTime)                                        │
│  IsActive (bool)                                                │
└─────────────────────────────────────────────────────────────────┘
```

## Tasks

### 7.1 MAUI Project Setup

**Description:** Create the .NET MAUI solution with proper project structure and dependencies.

**Deliverables:**

- New solution: `LenkCareHomes.Mobile.sln`
- MAUI project targeting iOS and Android
- Project references and NuGet packages:
  - CommunityToolkit.Mvvm (MVVM pattern)
  - CommunityToolkit.Maui (UI helpers)
  - Microsoft.Extensions.Http (HttpClient factory)
  - Plugin.Fingerprint or similar (biometric auth)
  - Azure.Messaging (push notifications)
- Base styles and theming matching web app colors
- App icons and splash screens
- Development provisioning profiles (iOS) and signing (Android)

**Acceptance Criteria:**

- Solution builds successfully for iOS and Android
- App launches on iOS simulator and Android emulator
- Project structure follows MVVM pattern
- Styles are consistent with web application branding
- App icons display correctly on both platforms

### 7.2 Backend Device Registration API

**Description:** Create backend endpoints for mobile device registration and management.

**Deliverables:**

- UserDevice entity in database schema
- Migration to add UserDevices table
- API endpoints:
  - POST /api/devices/register - Register new device
  - PUT /api/devices/{id}/push-token - Update push notification token
  - GET /api/devices - List user's registered devices
  - DELETE /api/devices/{id} - Unregister device
- Device registration captures:
  - Device ID (unique per device)
  - Device name (user-friendly name)
  - Platform (iOS/Android)
  - Push notification token
  - Biometric capability flag
- Support for multiple devices per user

**Acceptance Criteria:**

- User can register multiple mobile devices
- Push tokens are stored securely
- Device list shows all registered devices
- User can remove devices they no longer use
- Device registration is logged in audit trail
- Old devices with expired tokens are handled gracefully

### 7.3 Mobile Authentication Service

**Description:** Implement authentication flow for mobile with secure token storage, including first-time device setup flow.

**Deliverables:**

- AuthService for handling login/logout
- Secure token storage:
  - iOS: Keychain
  - Android: EncryptedSharedPreferences
- Login flow (existing device with passkey):
  1. User enters email/password
  2. Passkey challenge presented
  3. User authenticates with device passkey
  4. JWT token returned and stored securely
  5. Device registered with backend
- **First-time device setup flow** (no passkey on device):
  1. User enters email/password on mobile
  2. Backend detects no passkey registered for this device
  3. Mobile app shows "No passkey on this device" screen with instructions
  4. User logs into **web app** (where passkey exists)
  5. Web app: User navigates to Profile → Mobile Devices → "Generate Setup Code"
  6. Web app generates a single-use, time-limited code (6 digits, expires in 10 minutes)
  7. User enters code in mobile app
  8. Mobile app validates code → completes login → immediately prompts to register passkey on this device
  9. Passkey registered on mobile device for future logins
- Mobile setup code restrictions:
  - **Mobile-only**: Cannot be used for web authentication
  - **Single-use**: Invalidated after use or expiration
  - **Time-limited**: 10-minute expiration
  - **Requires passkey setup**: Login only completes after passkey is registered on device
- Token refresh logic
- Logout clears tokens and unregisters push token
- Backend endpoints:
  - POST /api/auth/mobile-setup-code - Generate code (web only, requires passkey auth)
  - POST /api/auth/validate-mobile-setup-code - Validate code and issue token (mobile only)

**Acceptance Criteria:**

- User with passkey on device can log in normally
- User without passkey sees setup code instructions
- Setup code can only be generated from web app (authenticated with passkey)
- Setup code cannot be used on web app login
- Setup code expires after 10 minutes
- Setup code is single-use
- After using setup code, user must register passkey on mobile device
- Tokens are stored securely (not in plain text)
- App remembers logged-in state between launches
- Token refresh works before expiration
- Logout clears all stored credentials
- Login failures show appropriate error messages
- Setup code generation is logged in audit trail

### 7.4 Biometric Authentication

**Description:** Implement biometric authentication for quick app access after initial login.

**Deliverables:**

- Biometric check on app launch (if previously logged in)
- Settings to enable/disable biometric auth
- Support for:
  - iOS: Face ID, Touch ID
  - Android: Fingerprint, Face Unlock
- Fallback to password if biometric fails
- Biometric prompt customization

**Acceptance Criteria:**

- Users can enable biometric authentication after login
- App prompts for biometric on subsequent launches
- Fallback to password works when biometric fails or is cancelled
- Users can disable biometric auth in settings
- Biometric preference is stored per device
- Appropriate error handling for devices without biometric hardware

### 7.5 Core Navigation and Shell

**Description:** Implement the main navigation structure using MAUI Shell.

**Deliverables:**

- Shell-based navigation with:
  - Bottom tab bar for main sections
  - Tab: Clients (home list → client list → client detail)
  - Tab: Appointments (upcoming appointments)
  - Tab: Settings (profile, logout, device management)
- Navigation service for programmatic navigation
- Back navigation handling

**Acceptance Criteria:**

- Smooth navigation between tabs
- Deep linking works for client details
- Navigation state preserved appropriately
- Hardware back button works on Android
- Swipe-to-go-back works on iOS

### 7.6 Home and Client List Views

**Description:** Display homes and clients based on user role.

**Deliverables:**

- Home list page:
  - Caregivers: Shows only homes they are assigned to
  - Admins/OrgAdmins: Shows all homes in organization
  - Home name, address, client count
  - Tap to see clients in that home
- Client list page:
  - Shows clients in selected home
  - Client photo, name, bed label
  - Allergy indicator if present
  - Tap to see client detail
- Pull-to-refresh on both lists
- Empty state handling

**Acceptance Criteria:**

- Caregiver sees only assigned homes
- Admin and OrganizationAdministrator see all org homes
- Client list shows only clients in selected home
- Photos display correctly (or placeholder if none)
- Lists load quickly and scroll smoothly
- Pull-to-refresh updates data
- Empty states show helpful messages

### 7.7 Client Detail View

**Description:** Display client information and quick actions.

**Deliverables:**

- Client detail page showing:
  - Photo (large)
  - Name, DOB, age
  - Bed label, home name
  - Allergies (highlighted if present)
  - Emergency contact
  - Primary physician
  - Diagnoses summary
- Quick action buttons:
  - Log ADL
  - Log Vitals
  - Log ROM
  - Add Note
  - Log Activity
  - Report Incident
- Recent care timeline preview (last 5 entries)

**Acceptance Criteria:**

- All client information displays correctly
- Allergies are visually prominent (red/warning color)
- Quick action buttons navigate to appropriate forms
- Client detail loads quickly
- Recent timeline shows variety of entry types
- Sensitive data is not cached insecurely

### 7.8 Appointments View

**Description:** Display upcoming appointments for assigned clients (read-only).

**Deliverables:**

- Appointments list page:
  - Shows upcoming appointments (next 7 days)
  - Grouped by date
  - Each appointment shows:
    - Client name and photo
    - Appointment title and type
    - Time and duration
    - Location
    - Provider name
    - Transportation notes
- Filter by home (if assigned to multiple)
- Empty state for no upcoming appointments

**Acceptance Criteria:**

- Appointments display for all assigned clients
- Appointments are read-only (no editing)
- Grouped by date for easy scanning
- Transportation notes visible for planning
- Filter works correctly
- Past appointments not shown

### 7.9 API Client Service

**Description:** Create typed HTTP client for backend API communication.

**Deliverables:**

- HttpClient configuration with:
  - Base URL configuration
  - JWT token injection via handler
  - Retry policy for transient failures
  - Timeout configuration
- Typed API clients for:
  - AuthApiClient
  - HomesApiClient
  - ClientsApiClient
  - AppointmentsApiClient
  - DevicesApiClient
- Error handling with user-friendly messages
- Network connectivity checking

**Acceptance Criteria:**

- All API calls include auth token
- Retries handle transient network issues
- Timeout errors show appropriate message
- 401 errors redirect to login
- Network errors are handled gracefully
- API responses are deserialized correctly

### 7.10 Role-Based Mobile Access

**Description:** Ensure only authorized roles can use the mobile app for care tasks.

**Deliverables:**

- Role check after login
- Allow OrganizationAdministrator, Admin, and Caregiver roles
- Block OrganizationSysadmin from mobile app (no PHI access)
- Friendly error message for blocked roles explaining mobile is for care staff only
- Redirect blocked roles to use web app
- Role-based feature visibility:
  - Caregivers see only assigned homes
  - Admins and OrganizationAdministrators see all homes in organization

**Acceptance Criteria:**

- OrganizationAdministrator can log in and access all org homes/clients
- Admin can log in and access all org homes/clients
- Caregiver can log in and access assigned homes/clients only
- OrganizationSysadmin attempting login sees friendly error
- Error message suggests using web application for system tasks
- Role check happens immediately after authentication

## Dependencies

- Phase 6 completed (multi-tenancy for org context)
- Apple Developer Program membership
- Google Play Developer account
- Push notification infrastructure (Azure Notification Hubs or Firebase)
- Physical iOS and Android devices for biometric testing

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| MAUI bugs on specific devices | Medium | Test on variety of real devices; monitor MAUI GitHub issues |
| iOS App Store review delays | Medium | Submit early; ensure compliance with guidelines |
| Biometric API differences | Low | Use abstraction library (Plugin.Fingerprint) |
| Token storage security | High | Use platform-specific secure storage; security audit |
| Network reliability | Medium | Implement retry logic; clear error messages |

## Success Criteria

- App builds and runs on iOS and Android
- OrganizationAdministrator, Admin, and Caregiver can log in successfully
- OrganizationSysadmin is blocked with friendly message
- Role-based home visibility works (Caregivers see assigned, Admins see all)
- Biometric authentication works on supported devices
- Device registration tracks multiple devices per user
- Appointments display correctly (read-only)
- Navigation is smooth and intuitive
- App follows platform design guidelines
- Security audit shows no token exposure risks

## Next Phase

Phase 8 will implement the core care logging features: ADLs, vitals, ROM, behavior notes, activities, and incident reporting with camera integration.
