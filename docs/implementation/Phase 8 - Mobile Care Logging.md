# Phase 8: Mobile Care Logging

## Overview

Implement the core care logging features for the mobile application. This phase enables caregivers, admins, and organization administrators to log ADLs, vitals, ROM exercises, behavior notes, recreational activities, and incidents directly from their mobile devices while providing care. Incident reporting includes camera integration for capturing photos.

## Objectives

- Implement ADL logging with Katz Index form
- Implement vitals logging (BP, pulse, temperature, O₂)
- Implement ROM exercise logging
- Implement behavior/mood notes
- Implement recreational activity logging
- Implement incident reporting with photo capture
- Create client care timeline view
- Ensure all logs sync with backend immediately

## Prerequisites

- Phase 7 completed (mobile foundation with authentication)
- Backend care logging APIs operational
- Camera permissions configured for iOS and Android

## Tasks

### 8.1 ADL Logging Screen

**Description:** Create the Activities of Daily Living logging form based on Katz Index.

**Deliverables:**

- ADL logging page with:
  - Client info header (name, photo, bed)
  - Timestamp picker (defaults to now)
  - Six ADL categories with level selectors:
    - Bathing
    - Dressing
    - Toileting
    - Transferring
    - Continence
    - Feeding
  - Level options: Independent, Partial Assist, Dependent, N/A
  - Notes field (optional)
  - Submit button
- Mobile-optimized layout:
  - Large touch targets for level selection
  - Segmented controls or toggle buttons
  - Easy one-handed operation
- Katz score calculation displayed
- Success confirmation with option to log another

**Acceptance Criteria:**

- Form displays all six ADL categories
- Level selection is easy on mobile (no tiny buttons)
- Timestamp can be adjusted if logging after the fact
- At least one ADL must be set to submit
- Katz score shows calculated value
- Submission creates ADL log in backend
- Success message confirms submission
- User can quickly log another ADL

### 8.2 Vitals Logging Screen

**Description:** Create the vitals logging form for blood pressure, pulse, temperature, and oxygen saturation.

**Deliverables:**

- Vitals logging page with:
  - Client info header
  - Timestamp picker
  - Blood pressure fields:
    - Systolic (number input)
    - Diastolic (number input)
  - Pulse field (number input, BPM)
  - Temperature field:
    - Number input with decimal
    - Unit toggle (°F default, °C option)
  - Oxygen saturation field (percentage, 0-100)
  - Notes field (optional)
  - Submit button
- Input validation:
  - Reasonable ranges for each vital
  - Warning for abnormal values (not blocking)
- Numeric keyboard for all number fields

**Acceptance Criteria:**

- All vital fields display correctly
- Numeric keyboard appears for number inputs
- Validation warns for out-of-range values
- At least one vital must be entered to submit
- Abnormal values highlighted but submission allowed
- Submission creates vitals log in backend
- Temperature unit preference is remembered

### 8.3 ROM Exercise Logging Screen

**Description:** Create the Range of Motion exercise logging form.

**Deliverables:**

- ROM logging page with:
  - Client info header
  - Timestamp picker
  - Activity description:
    - Quick-select common exercises (dropdown or chips):
      - Upper extremity ROM
      - Lower extremity ROM
      - Shoulder stretches
      - Hip exercises
      - Neck ROM
      - Full body
    - Custom description option
  - Duration field (minutes)
  - Repetitions field (number)
  - Notes field (observations, tolerance)
  - Submit button

**Acceptance Criteria:**

- Common exercises are quick to select
- Custom description allows any exercise
- Duration and/or reps must be provided
- Notes capture important observations
- Submission creates ROM log in backend
- Quick to complete during care activities

### 8.4 Behavior Note Screen

**Description:** Create the behavior and mood note entry form.

**Deliverables:**

- Behavior note page with:
  - Client info header
  - Timestamp picker
  - Category selector:
    - Behavior
    - Mood
    - General
  - Severity level:
    - Low
    - Medium
    - High
  - Note text field:
    - Multi-line text input
    - Character count (max 1000)
    - Placeholder with examples
  - Submit button
- Quick entry for urgent observations

**Acceptance Criteria:**

- Category and severity are easy to select
- Text field supports multi-line input
- Character limit is enforced
- Note text is required
- Submission creates behavior note in backend
- High severity notes are visually distinct

### 8.5 Activity Logging Screen

**Description:** Create the recreational activity logging form.

**Deliverables:**

- Activity logging page with:
  - Activity name field
  - Description field (optional)
  - Date and time picker
  - Duration field (optional)
  - Category selector:
    - Recreational
    - Social
    - Exercise
    - Other
  - Client participant selector:
    - Shows clients from current home
    - Multi-select for group activities
    - Pre-selected with current client if accessed from client detail
  - Submit button
- Group activity support

**Acceptance Criteria:**

- Activity can be logged for single client
- Multiple clients can be selected for group activities
- Category helps organize activities
- Duration is optional but encouraged
- Submission creates activity with participants
- All selected clients appear in their timelines

### 8.6 Incident Reporting Screen

**Description:** Create comprehensive incident reporting form with photo capture.

**Deliverables:**

- Incident reporting page with:
  - Client selector (or pre-filled from client detail)
  - Home selector (auto-filled based on client)
  - Incident type dropdown:
    - Fall
    - Medication
    - Behavioral
    - Medical
    - Injury
    - Elopement
    - Other
  - Severity level (1-5):
    - Visual scale with descriptions
    - 1 = Minor, 5 = Critical
  - Date/time of incident
  - Location field (where in facility)
  - Description field (multi-line, required)
  - Actions taken field (what was done)
  - Witness names field (optional)
  - Photo capture section:
    - Take photo button (camera)
    - Choose from gallery button
    - Photo preview with remove option
    - Multiple photos supported (up to 5)
    - Caption for each photo
  - Submit options:
    - Save as Draft
    - Submit Immediately
- Draft support for completing later

**Acceptance Criteria:**

- All required fields are validated
- Severity scale is clear and visual
- Description captures full incident details
- Photos can be taken directly from camera
- Photos can be selected from gallery
- Photos are compressed for upload
- Multiple photos supported
- Captions can be added to photos
- Draft saves locally until submitted
- Submission triggers admin notification
- Incident appears in backend with photos

### 8.7 Camera and Photo Handling

**Description:** Implement camera integration and photo upload for incidents.

**Deliverables:**

- Camera service for:
  - Capturing photos
  - Selecting from gallery
  - Image compression
  - Preview display
- Platform permissions:
  - iOS: NSCameraUsageDescription, NSPhotoLibraryUsageDescription
  - Android: CAMERA, READ_EXTERNAL_STORAGE permissions
- Photo upload flow:
  - Compress to reasonable size (max 1MB per photo)
  - Get SAS URL from backend
  - Upload directly to Azure Blob
  - Confirm upload to backend
- Error handling for camera failures

**Acceptance Criteria:**

- Camera opens and captures photos
- Gallery selection works
- Photos are compressed appropriately
- Upload progress is shown
- Failed uploads can be retried
- Photos display correctly in incident preview
- Permissions are requested appropriately
- Graceful handling if permissions denied

### 8.8 Client Care Timeline

**Description:** Display chronological history of all care entries for a client.

**Deliverables:**

- Timeline page showing:
  - Chronological list of all care entries
  - Entry types with icons:
    - ADL (clipboard icon)
    - Vitals (heart icon)
    - ROM (exercise icon)
    - Behavior Note (speech icon)
    - Activity (star icon)
    - Incident (warning icon)
  - Date grouping (Today, Yesterday, This Week, etc.)
  - Each entry shows:
    - Type and icon
    - Timestamp
    - Brief summary
    - Caregiver name
  - Tap to expand entry details
- Filtering by entry type
- Date range selection
- Pull-to-refresh

**Acceptance Criteria:**

- All entry types display in timeline
- Entries are ordered chronologically (newest first)
- Entry type icons are clear and distinct
- Tap expands to show full details
- Filtering works correctly
- Date range limits data shown
- Pull-to-refresh updates timeline
- Scrolls smoothly with many entries

### 8.9 Quick Log Actions

**Description:** Implement quick-access logging from client detail and dashboard.

**Deliverables:**

- Floating action button (FAB) on client detail:
  - Expands to show log options
  - ADL, Vitals, ROM, Note, Activity, Incident
- Recent clients quick access:
  - Shows last 3 clients interacted with
  - One-tap to open client detail
- Quick log from notification (future: Phase 9)

**Acceptance Criteria:**

- FAB is accessible but not intrusive
- Expansion animation is smooth
- Each action opens correct form
- Client context is preserved
- Recent clients update based on activity
- Navigation back returns to correct screen

### 8.10 Form Validation and Error Handling

**Description:** Implement consistent validation and error handling across all forms.

**Deliverables:**

- Validation framework:
  - Required field indicators
  - Inline error messages
  - Field highlighting on error
  - Submit button disabled until valid
- Network error handling:
  - Retry option for failed submissions
  - Clear error messages
  - Draft save on failure
- Loading states:
  - Submit button shows loading
  - Prevent double-submission
- Success feedback:
  - Confirmation message
  - Haptic feedback on success

**Acceptance Criteria:**

- Required fields are clearly marked
- Validation errors appear inline
- Invalid fields are highlighted
- Network errors prompt retry
- Failed submissions are not lost
- Success provides clear feedback
- Double-tap submission prevented

## Dependencies

- Phase 7 completed (authentication and navigation)
- Backend care logging APIs available
- Azure Blob Storage configured for incident photos
- Camera permissions in app manifests

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Photo upload failures | Medium | Implement retry; queue for later upload |
| Large forms difficult on small screens | Medium | Optimize layout; test on various screen sizes |
| Camera permission denied | Low | Graceful fallback; clear permission request messaging |
| Data loss on form submission failure | High | Auto-save drafts; retry mechanism |
| Performance with large timelines | Medium | Pagination; virtualized lists |

## Success Criteria

- All care log types can be created from mobile by authorized roles
- OrganizationAdministrator, Admin, and Caregiver can all log care activities
- Forms are easy to use on mobile devices
- Photos can be captured and uploaded with incidents
- Timeline shows complete care history
- Submissions sync immediately with backend
- Validation prevents incomplete submissions
- Error handling preserves user data
- Forms complete quickly for busy care staff

## Next Phase

Phase 9 will implement push notifications for admin incident alerts and MFA approval flow for web authentication, followed by app store preparation.
