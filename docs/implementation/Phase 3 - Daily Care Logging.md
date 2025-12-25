# Phase 3: Daily Care Logging

## Overview

Implement the core caregiving documentation modules that enable caregivers to record daily care activities, vital signs, exercises, behavioral observations, and recreational activities for clients. These modules form the heart of the day-to-day care documentation system.

## Objectives

- Implement Activities of Daily Living (ADL) tracking based on Katz Index
- Implement Vitals monitoring (blood pressure, pulse, temperature, oxygen saturation)
- Implement Range of Motion (ROM) exercises logging
- Implement Behavior and Mood notes documentation
- Implement Recreational Activity logging (individual and group)
- Create unified client activity timeline view

## Prerequisites

- Phase 2 completed (clients, caregivers, homes are manageable)
- Caregivers can log in and view their assigned clients
- Client profile pages are functional

## Tasks

### 3.1 ADL Tracking Module (Katz Index)

**Description:** Implement Activities of Daily Living tracking based on the Katz Index of Independence.

**Deliverables:**

- ADLLogs database table with fields: ID, ClientID (FK), CaregiverID (FK), Timestamp, Bathing, Dressing, Toileting, Transferring, Continence, Feeding, Notes, CreatedAt
- Enum or lookup table for ADL levels: Independent (2), Partial Assist (1), Dependent (0), N/A
- Backend API endpoints:
  - POST /api/clients/{clientId}/adls - Create ADL log entry
  - GET /api/clients/{clientId}/adls - List ADL history (with date filtering)
  - GET /api/clients/{clientId}/adls/{id} - Get specific ADL entry
- Frontend components:
  - ADL logging form modal with six categories (bathing, dressing, toileting, transferring, continence, feeding)
  - Radio buttons or dropdown for each category (Independent/Partial/Dependent/N/A)
  - Optional notes field
  - Timestamp field (defaults to now, adjustable)
  - ADL history view (table or timeline showing past entries)
  - Katz score calculation (optional: sum of independent activities)
- Validation: Require at least one ADL category to be filled

**Acceptance Criteria:**

- Caregiver can open ADL form from client profile
- Caregiver can select level for each of six ADL categories
- Form defaults timestamp to current time (editable)
- Caregiver can add optional notes
- Form validates at least one category is filled
- ADL entry is saved with caregiver ID and timestamp
- Entry appears immediately in ADL history
- ADL history shows date, time, caregiver name, and summary of activities
- ADL entries are immutable (cannot be edited after submission)
- All ADL logging is recorded in audit trail
- Caregivers can only log ADLs for clients in their assigned homes

### 3.2 Vitals Monitoring Module

**Description:** Implement vital signs tracking for blood pressure, pulse, temperature, and oxygen saturation.

**Deliverables:**

- Vitals database table with fields: ID, ClientID (FK), CaregiverID (FK), Timestamp, SystolicBP, DiastolicBP, Pulse, Temperature, TemperatureUnit (F/C), OxygenSaturation, Notes, CreatedAt
- Backend API endpoints:
  - POST /api/clients/{clientId}/vitals - Record vital signs
  - GET /api/clients/{clientId}/vitals - List vitals history
  - GET /api/clients/{clientId}/vitals/{id} - Get specific vitals entry
- Frontend components:
  - Vitals entry form modal with fields:
    - Blood Pressure (two inputs: systolic/diastolic)
    - Pulse (BPM)
    - Temperature (decimal input with unit selector F/C)
    - Oxygen Saturation (percentage)
  - Optional notes field
  - Timestamp field (defaults to now)
  - Vitals history view with table/chart showing trends
  - Visual indicators for out-of-range values (optional warning colors)
- Validation:
  - Range checks (e.g., BP 50-300, Pulse 30-200, Temp 90-110°F, O2 70-100%)
  - At least one vital must be recorded

**Acceptance Criteria:**

- Caregiver can open vitals form from client profile
- Form includes all four vital sign inputs
- Temperature unit defaults to Fahrenheit (configurable)
- System validates inputs are in reasonable ranges
- Caregiver can submit with partial vitals (but at least one required)
- Vitals entry is saved with timestamp and caregiver ID
- Vitals history displays all entries in chronological order
- History shows trend visualization (simple table or line chart)
- Out-of-range vitals are highlighted (optional)
- All vitals recording is logged in audit trail
- Vitals entries are immutable after submission

### 3.3 Range of Motion (ROM) Exercises Logging

**Description:** Implement logging for Range of Motion exercises performed with clients.

**Deliverables:**

- ROMLogs database table with fields: ID, ClientID (FK), CaregiverID (FK), Timestamp, ActivityDescription, Duration, Repetitions, Notes, CreatedAt
- Backend API endpoints:
  - POST /api/clients/{clientId}/rom - Log ROM activity
  - GET /api/clients/{clientId}/rom - List ROM history
- Frontend components:
  - ROM logging form modal with fields:
    - Activity description (dropdown with common exercises + "Other" option for free text)
    - Duration (minutes) or Repetitions (number)
    - Notes (observations, pain complaints, etc.)
    - Timestamp
  - ROM history view showing past exercises

**Acceptance Criteria:**

- Caregiver can log ROM activity from client profile
- Form provides dropdown of common ROM exercises (e.g., "Shoulder ROM", "Leg ROM", "Ankle rotation")
- Caregiver can specify duration or repetitions
- Caregiver can add notes about client's response
- ROM entry is saved and appears in history
- Multiple ROM entries can be logged per day
- ROM entries are immutable after submission
- All ROM logging is recorded in audit trail

### 3.4 Behavior and Mood Notes Module

**Description:** Implement free-form logging for behavioral observations and mood changes.

**Deliverables:**

- BehaviorNotes database table with fields: ID, ClientID (FK), CaregiverID (FK), Timestamp, Category (Behavior/Mood/General), NoteText, Severity (Low/Medium/High - optional), CreatedAt
- Backend API endpoints:
  - POST /api/clients/{clientId}/behavior-notes - Create behavior note
  - GET /api/clients/{clientId}/behavior-notes - List behavior notes
- Frontend components:
  - Behavior note form modal with:
    - Category selector (Behavior, Mood, General)
    - Multi-line text area for note content (max ~1000 chars)
    - Optional severity/importance indicator
    - Timestamp
  - Notes history view in reverse chronological order

**Acceptance Criteria:**

- Caregiver can create behavior/mood note from client profile
- Form includes category selection and text area
- Notes are timestamped automatically
- Notes display with caregiver name and timestamp in history
- Notes are visible to all caregivers of the same home
- Notes are immutable after submission (no edit/delete for caregivers)
- Admin can view all notes across all clients
- Character limit is enforced with counter
- All note creation is logged in audit trail

### 3.5 Recreational Activity Logging Module

**Description:** Implement activity participation tracking for individual and group recreational activities.

**Deliverables:**

- Activities database table with fields: ID, ActivityName, Description, Date, StartTime, EndTime, Duration, Category (Recreational/Social/Exercise/Other), IsGroupActivity, CreatedBy (FK), CreatedAt
- ActivityParticipants junction table: ActivityID (FK), ClientID (FK)
- Backend API endpoints:
  - POST /api/activities - Create activity (individual or group)
  - GET /api/activities - List all activities (filtered by home/date)
  - GET /api/clients/{clientId}/activities - List activities for specific client
  - PUT /api/activities/{id} - Update activity details
  - DELETE /api/activities/{id} - Delete activity (admin only)
- Frontend components:
  - Activity logging form with:
    - Activity name/description
    - Date and time/duration
    - Category selector
    - Client selector (multi-select for group activities)
    - Individual vs. Group toggle
  - Client activity history showing participation
  - Group activity report showing all participants

**Acceptance Criteria:**

- Caregiver can log individual activity for one client
- Caregiver can log group activity with multiple participants
- For group activities, all selected clients appear in each client's activity history
- Activity history shows activity name, date, duration, and other participants (if group)
- Admin can generate activity participation reports
- Admin can edit/delete activities with audit logging
- Multiple clients can be selected easily for group activities
- Activity logging is recorded in audit trail

### 3.6 Unified Client Activity Timeline

**Description:** Create a unified timeline view showing all care activities for a client.

**Deliverables:**

- Client Activity Timeline component showing all logs chronologically:
  - ADL entries
  - Vitals entries
  - ROM exercises
  - Behavior notes
  - Recreational activities
- Filterable by date range and activity type
- Exportable view for reporting (optional)
- Visual timeline with color-coded activity types

**Acceptance Criteria:**

- Timeline displays all activity types in chronological order
- Each entry shows type, timestamp, caregiver, and brief summary
- Clicking entry shows full details
- Timeline can be filtered by date range
- Timeline can be filtered by activity type (checkboxes)
- Timeline loads efficiently (pagination or virtual scrolling for large datasets)
- Timeline is accessible from client profile
- Visual design is clear and easy to read

### 3.7 Quick Logging Interface

**Description:** Create a streamlined interface for caregivers to quickly log common activities.

**Deliverables:**

- Quick Log modal accessible from client card or profile
- Tabs or sections for: ADLs, Vitals, ROM, Notes, Activities
- Recent entries preview for each category
- One-click access to each logging form

**Acceptance Criteria:**

- Quick Log modal opens from client card
- All logging forms are accessible within modal
- Recent entries are visible for context
- Caregivers can log multiple activities in sequence without closing modal
- Modal is responsive and works on tablets

## Testing Requirements

- Unit tests for all logging business logic
- Integration tests for all API endpoints
- Validation tests (range checks, required fields)
- Authorization tests (caregivers can only log for assigned clients)
- UI tests for logging workflows
- Performance tests for timeline with large datasets

## Dependencies

- Phase 2 completed (clients and caregivers are manageable)
- Client profile pages exist and are accessible

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Form complexity overwhelming caregivers | Medium | Keep forms simple; provide tooltips; gather user feedback |
| Timeline performance with large datasets | Medium | Implement pagination or virtual scrolling; optimize queries |
| Data validation too strict | Low | Use reasonable ranges; allow override with warning if needed |

## Success Criteria

- Caregivers can log ADLs, vitals, ROM, notes, and activities efficiently
- All logging forms validate inputs appropriately
- Historical data is easily viewable and filterable
- Unified timeline provides comprehensive view of care activities
- Forms are intuitive and require minimal training
- All logging operations are audited
- System performs well with typical daily logging volumes
- Mobile/tablet experience is smooth for caregivers

## Next Phase

Phase 4 will build upon this foundation by implementing Incident Reporting with review workflow and secure Document Management with Azure Blob Storage.
