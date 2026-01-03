# Mobile Application Architecture

## Overview

This document describes the architecture for the LenkCare Homes mobile application, built using .NET MAUI to target iOS and Android platforms from a shared C# codebase. The mobile app is designed for **caregivers only**, enabling them to log care activities while providing care to clients.

## Platform Decision

### Why .NET MAUI?

| Factor | Decision |
|--------|----------|
| **Shared Codebase** | ~95% code sharing between iOS and Android |
| **Technology Alignment** | Same language (C#) as backend API |
| **Development Efficiency** | Single team can maintain mobile and backend |
| **Native Performance** | Compiles to native code, not interpreted |
| **Native UI** | Uses platform-native controls |
| **Microsoft Support** | Long-term support from Microsoft |

### Target Platforms

| Platform | Minimum Version | Target |
|----------|-----------------|--------|
| iOS | 15.0+ | Latest |
| Android | API 24 (Android 7.0)+ | API 34 (Android 14) |

## User Scope

### Supported Roles

| Role | Mobile Access | Capabilities |
|------|---------------|--------------|
| **OrganizationAdministrator** | ✅ Full access | View all org homes/clients, log care, report incidents, receive incident alerts, approve MFA |
| **Admin** | ✅ Full access | View all org homes/clients, log care, report incidents, receive incident alerts, approve MFA |
| **Caregiver** | ✅ Assigned homes | View assigned homes/clients, log care, report incidents, approve MFA |
| **OrganizationSysadmin** | ❌ Not supported | No PHI access, must use web application |

### Mobile App Features (All Care Roles)

These features are available to OrganizationAdministrator, Admin, and Caregiver:

| Feature | Description |
|---------|-------------|
| View Homes | OrgAdmin/Admin: all org homes; Caregiver: assigned homes only |
| View Clients | See clients in accessible homes with details |
| View Appointments | Read-only view of upcoming appointments |
| Log ADLs | Katz Index activities of daily living |
| Log Vitals | Blood pressure, pulse, temperature, O₂ |
| Log ROM | Range of motion exercises |
| Log Notes | Behavior and mood observations |
| Log Activities | Recreational and social activities |
| Report Incidents | Full incident reporting with photos |
| View Timeline | Chronological care history |

### Explicitly NOT Supported on Mobile

- ❌ Home and bed management
- ❌ Client admission/discharge
- ❌ Caregiver invitation/management
- ❌ Document upload
- ❌ Document viewing
- ❌ Report generation
- ❌ Audit log viewing
- ❌ User management
- ❌ Settings administration

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    .NET MAUI Application                        │
├─────────────────────────────────────────────────────────────────┤
│  Views (XAML)                                                   │
│  ├── LoginPage                                                  │
│  ├── HomeListPage                                               │
│  ├── ClientListPage                                             │
│  ├── ClientDetailPage                                           │
│  ├── AppointmentsPage                                           │
│  ├── ADLLogPage                                                 │
│  ├── VitalsLogPage                                              │
│  ├── ROMLogPage                                                 │
│  ├── BehaviorNotePage                                           │
│  ├── ActivityLogPage                                            │
│  ├── IncidentReportPage                                         │
│  ├── TimelinePage                                               │
│  ├── MfaApprovalPage                                            │
│  └── SettingsPage                                               │
├─────────────────────────────────────────────────────────────────┤
│  ViewModels (MVVM with CommunityToolkit.Mvvm)                   │
│  ├── LoginViewModel                                             │
│  ├── HomeListViewModel                                          │
│  ├── ClientListViewModel                                        │
│  ├── ClientDetailViewModel                                      │
│  ├── AppointmentsViewModel                                      │
│  ├── Care Log ViewModels (ADL, Vitals, ROM, etc.)              │
│  ├── IncidentReportViewModel                                    │
│  └── SettingsViewModel                                          │
├─────────────────────────────────────────────────────────────────┤
│  Services                                                       │
│  ├── IAuthService          → Authentication, token management   │
│  ├── IBiometricService     → Face ID, Touch ID, Fingerprint    │
│  ├── IApiClient            → HTTP communication with backend    │
│  ├── ISecureStorageService → Keychain (iOS), EncryptedPrefs    │
│  ├── IPushService          → Push notification handling        │
│  ├── ICameraService        → Photo capture for incidents       │
│  └── INavigationService    → Shell navigation                  │
├─────────────────────────────────────────────────────────────────┤
│  Platform-Specific Code                                         │
│  ├── iOS/                                                       │
│  │   ├── Biometric (Face ID, Touch ID)                         │
│  │   ├── Push (APNs)                                           │
│  │   └── Keychain Storage                                      │
│  └── Android/                                                   │
│      ├── Biometric (Fingerprint, Face)                         │
│      ├── Push (FCM)                                            │
│      └── EncryptedSharedPreferences                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS (REST API)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Existing Backend API                         │
├─────────────────────────────────────────────────────────────────┤
│  Existing Endpoints (reused)                                    │
│  ├── POST /api/auth/login                                       │
│  ├── GET  /api/caregivers/dashboard                            │
│  ├── GET  /api/homes/{id}                                      │
│  ├── GET  /api/clients/{id}                                    │
│  ├── GET  /api/clients/{id}/timeline                           │
│  ├── POST /api/care-log/adl                                    │
│  ├── POST /api/care-log/vitals                                 │
│  ├── POST /api/care-log/rom                                    │
│  ├── POST /api/care-log/behavior-notes                         │
│  ├── POST /api/activities                                      │
│  ├── POST /api/incidents                                       │
│  └── GET  /api/appointments                                    │
├─────────────────────────────────────────────────────────────────┤
│  New Endpoints (for mobile)                                     │
│  ├── POST /api/devices/register                                │
│  ├── PUT  /api/devices/{id}/push-token                         │
│  ├── GET  /api/devices                                         │
│  ├── DELETE /api/devices/{id}                                  │
│  ├── POST /api/auth/mfa/request                                │
│  ├── POST /api/auth/mfa/approve                                │
│  ├── POST /api/auth/mfa/deny                                   │
│  └── GET  /api/auth/mfa/status/{challengeId}                   │
└─────────────────────────────────────────────────────────────────┘
```

### Project Structure

```
src/mobile/
└── LenkCareHomes.Mobile/
    ├── LenkCareHomes.Mobile.sln
    │
    ├── LenkCareHomes.Mobile/                    # Main MAUI Project
    │   ├── App.xaml                             # Application entry
    │   ├── App.xaml.cs
    │   ├── AppShell.xaml                        # Shell navigation
    │   ├── AppShell.xaml.cs
    │   ├── MauiProgram.cs                       # DI configuration
    │   │
    │   ├── Views/                               # XAML Pages
    │   │   ├── Auth/
    │   │   │   ├── LoginPage.xaml
    │   │   │   └── MfaApprovalPage.xaml
    │   │   ├── Clients/
    │   │   │   ├── ClientListPage.xaml
    │   │   │   ├── ClientDetailPage.xaml
    │   │   │   └── TimelinePage.xaml
    │   │   ├── CareLog/
    │   │   │   ├── ADLLogPage.xaml
    │   │   │   ├── VitalsLogPage.xaml
    │   │   │   ├── ROMLogPage.xaml
    │   │   │   ├── BehaviorNotePage.xaml
    │   │   │   └── ActivityLogPage.xaml
    │   │   ├── Incidents/
    │   │   │   └── IncidentReportPage.xaml
    │   │   ├── Appointments/
    │   │   │   └── AppointmentsPage.xaml
    │   │   └── Settings/
    │   │       └── SettingsPage.xaml
    │   │
    │   ├── ViewModels/                          # MVVM ViewModels
    │   │   ├── BaseViewModel.cs
    │   │   ├── Auth/
    │   │   ├── Clients/
    │   │   ├── CareLog/
    │   │   ├── Incidents/
    │   │   ├── Appointments/
    │   │   └── Settings/
    │   │
    │   ├── Services/                            # Business Services
    │   │   ├── Auth/
    │   │   │   ├── IAuthService.cs
    │   │   │   ├── AuthService.cs
    │   │   │   ├── IBiometricService.cs
    │   │   │   └── BiometricService.cs
    │   │   ├── Api/
    │   │   │   ├── IApiClient.cs
    │   │   │   ├── ApiClient.cs
    │   │   │   ├── AuthenticatedHttpHandler.cs
    │   │   │   └── Clients/
    │   │   │       ├── IHomesApiClient.cs
    │   │   │       ├── IClientsApiClient.cs
    │   │   │       └── ... (other API clients)
    │   │   ├── Storage/
    │   │   │   ├── ISecureStorageService.cs
    │   │   │   └── SecureStorageService.cs
    │   │   ├── Push/
    │   │   │   ├── IPushService.cs
    │   │   │   └── PushService.cs
    │   │   └── Camera/
    │   │       ├── ICameraService.cs
    │   │       └── CameraService.cs
    │   │
    │   ├── Models/                              # DTOs (matching backend)
    │   │   ├── Auth/
    │   │   ├── Clients/
    │   │   ├── CareLog/
    │   │   └── Incidents/
    │   │
    │   ├── Controls/                            # Custom Controls
    │   │   ├── ClientCard.xaml
    │   │   ├── TimelineEntry.xaml
    │   │   └── PhotoCapture.xaml
    │   │
    │   ├── Resources/                           # Assets
    │   │   ├── Styles/
    │   │   │   ├── Colors.xaml
    │   │   │   └── Styles.xaml
    │   │   ├── Fonts/
    │   │   ├── Images/
    │   │   └── Raw/
    │   │
    │   └── Platforms/                           # Platform-Specific
    │       ├── iOS/
    │       │   ├── Info.plist
    │       │   ├── Entitlements.plist
    │       │   └── AppDelegate.cs
    │       └── Android/
    │           ├── AndroidManifest.xml
    │           ├── MainActivity.cs
    │           └── MainApplication.cs
    │
    └── LenkCareHomes.Mobile.Tests/              # Unit Tests
        ├── ViewModels/
        └── Services/
```

## Authentication Flow

### Initial Login

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Mobile    │     │   Backend   │     │  Database   │
│    App      │     │    API      │     │             │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       │  POST /api/auth/login                 │
       │  {email, password}                    │
       │──────────────────▶│                   │
       │                   │  Validate creds   │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │                   │                   │
       │  Check role (must be OrgAdmin, Admin, or Caregiver)
       │  (OrganizationSysadmin is blocked)
       │                   │                   │
       │  {token, refreshToken, user}          │
       │◀──────────────────│                   │
       │                   │                   │
       │  Store token in   │                   │
       │  Secure Storage   │                   │
       │                   │                   │
       │  POST /api/devices/register           │
       │  {deviceId, name, platform, pushToken}│
       │──────────────────▶│                   │
       │                   │  Store device     │
       │                   │──────────────────▶│
       │                   │                   │
       │  Prompt for       │                   │
       │  Biometric Setup  │                   │
       │                   │                   │
```

### Subsequent App Launch (Biometric)

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Mobile    │     │  Platform   │     │   Backend   │
│    App      │     │ Biometric   │     │    API      │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       │  Check token      │                   │
       │  in Secure Storage│                   │
       │                   │                   │
       │  Token exists?    │                   │
       │  Yes              │                   │
       │                   │                   │
       │  Request biometric│                   │
       │──────────────────▶│                   │
       │                   │                   │
       │  Biometric prompt │                   │
       │  (Face ID/Touch ID│                   │
       │   /Fingerprint)   │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │  User authenticates                   │
       │──────────────────▶│                   │
       │                   │                   │
       │  Success          │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │  Use stored token │                   │
       │  for API calls    │                   │
       │─────────────────────────────────────▶│
       │                   │                   │
```

## Push Notification Architecture

### Notification Types

| Type | Recipients | Trigger | Content |
|------|------------|---------|---------|
| **Incident Alert** | OrgAdmins and Admins in org | New incident submitted | Client name, incident type, home |
| **MFA Approval** | Specific user (any mobile role) | Web login attempt | "Approve sign-in?" with numbers |

### Push Infrastructure

```
┌─────────────────────────────────────────────────────────────────┐
│                    Azure Notification Hubs                      │
├─────────────────────────────────────────────────────────────────┤
│  Hub: lenkcare-notifications                                    │
│  ├── iOS Credential: APNs Certificate                          │
│  └── Android Credential: FCM Server Key                        │
└─────────────────────────────────────────────────────────────────┘
         │                                    │
         ▼                                    ▼
┌─────────────────────┐            ┌─────────────────────┐
│  Apple Push         │            │  Firebase Cloud     │
│  Notification       │            │  Messaging          │
│  Service (APNs)     │            │  (FCM)              │
└─────────────────────┘            └─────────────────────┘
         │                                    │
         ▼                                    ▼
┌─────────────────────┐            ┌─────────────────────┐
│  iOS Device         │            │  Android Device     │
│  LenkCare Homes App │            │  LenkCare Homes App │
└─────────────────────┘            └─────────────────────┘
```

## MFA Approval Flow

### Number Matching Security

The MFA approval uses number matching to prevent accidental or malicious approvals:

1. Web login generates a random 2-digit number (10-99)
2. Push notification sent to user's mobile device(s)
3. Mobile app shows 3 numbers (correct + 2 decoys)
4. User must select the matching number
5. Wrong selection = failed attempt
6. Correct selection = login approved

```
┌─────────────────────────────────────────────────────────────────┐
│  Web Browser (Login Screen)                                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│     ┌─────────────────────────────────────────┐                │
│     │  Approve sign-in on your mobile device  │                │
│     │                                         │                │
│     │           Enter this number:            │                │
│     │                                         │                │
│     │              ╔═══════╗                  │                │
│     │              ║  47   ║                  │                │
│     │              ╚═══════╝                  │                │
│     │                                         │                │
│     │     Waiting for approval... (1:23)      │                │
│     │                                         │                │
│     │     [Use passkey instead]               │                │
│     └─────────────────────────────────────────┘                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  Mobile App (MFA Approval Screen)                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│     ┌─────────────────────────────────────────┐                │
│     │  Sign-in Request                        │                │
│     │                                         │                │
│     │  Someone is trying to sign in to        │                │
│     │  your LenkCare Homes account.           │                │
│     │                                         │                │
│     │  Select the number shown on screen:     │                │
│     │                                         │                │
│     │   ┌─────┐   ┌─────┐   ┌─────┐          │                │
│     │   │ 23  │   │ 47  │   │ 85  │          │                │
│     │   └─────┘   └─────┘   └─────┘          │                │
│     │                                         │                │
│     │           [Deny Request]                │                │
│     │                                         │                │
│     │     Request expires in 1:23             │                │
│     └─────────────────────────────────────────┘                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Timeout

- MFA approval requests expire after **90 seconds**
- User can request a new approval after timeout
- Only one active approval request per user at a time

## Security Considerations

### Token Storage

| Platform | Storage Mechanism | Security |
|----------|-------------------|----------|
| iOS | Keychain | Hardware-backed encryption |
| Android | EncryptedSharedPreferences | AES-256 encryption |

### Biometric Security

- Biometric is used to unlock token access, not as authentication itself
- Failed biometric falls back to password
- Token still validates with backend
- No bypass of server-side authentication

### Device Management

- Users can register multiple devices
- Users can view and remove devices from settings
- Push tokens are updated when refreshed by platform
- Inactive devices can be cleaned up

### Data Protection

- No PHI cached locally (always fetched fresh)
- Secure HTTPS for all API communication
- Certificate pinning (optional, recommended)
- Screenshot prevention on sensitive screens
- Session timeout after inactivity

## App Store Requirements

### iOS (App Store)

| Requirement | Implementation |
|-------------|----------------|
| Privacy Policy | Required URL in App Store Connect |
| Data Usage Labels | Declare health data collection |
| HIPAA Compliance | Document in app review notes |
| Camera Permission | NSCameraUsageDescription |
| Push Permission | Request at appropriate time |
| Biometric Permission | NSFaceIDUsageDescription |

### Android (Play Store)

| Requirement | Implementation |
|-------------|----------------|
| Privacy Policy | Required URL in Play Console |
| Data Safety | Declare health data handling |
| Target API Level | API 34 (Android 14) |
| Camera Permission | CAMERA permission |
| Storage Permission | READ_EXTERNAL_STORAGE |
| Push Permission | POST_NOTIFICATIONS (Android 13+) |

## Future Considerations

### Not in Initial Scope

| Feature | Reason for Deferral |
|---------|---------------------|
| Offline Support | Complexity; always-online for HIPAA compliance |
| Document Viewing | Security concerns on personal devices |
| OrganizationSysadmin Mobile Access | No PHI access; web-only for system tasks |
| Shift Management | Not currently in web app |
| Medication Logging | Available but prioritized lower |

### Potential Future Enhancements

- Offline draft saving with sync
- Quick-log widgets (iOS/Android home screen)
- Apple Watch / Wear OS companion
- Voice-to-text for notes
- Barcode scanning for medications
- Location-based home detection
