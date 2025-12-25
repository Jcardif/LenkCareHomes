# Phase 2: Core Data Management

## Overview

Implement the core data management modules for Homes, Beds, Clients, and Caregivers. This phase establishes the primary entities and their relationships, enabling the system to manage the fundamental organizational structure and personnel assignments.

## Objectives

- Implement Home and Bed management functionality
- Implement Client (resident) management with admission and discharge workflows
- Implement Caregiver management with home assignments
- Establish data access patterns enforcing home-scoped permissions
- Create admin dashboard with key performance indicators (KPIs)

## Prerequisites

- Phase 1 completed (authentication, RBAC, infrastructure)
- Admin user can log in successfully
- Database and Azure services are operational

## Tasks

### 2.1 Home Management Module

**Description:** Enable Admins to create and manage adult family homes.

**Deliverables:**

- Homes database table with fields: ID, Name, Address, City, State, ZIP, Phone, Email, Capacity, Active status
- Backend API endpoints:
  - POST /api/homes - Create home
  - GET /api/homes - List all homes
  - GET /api/homes/{id} - Get home details
  - PUT /api/homes/{id} - Update home
  - DELETE /api/homes/{id} - Deactivate home
- Frontend pages:
  - Homes list page with table (Ant Design Table component)
  - Create/Edit home form (Modal or separate page)
  - Home detail view
- Validation: Prevent deactivation if active clients exist

**Acceptance Criteria:**

- Admin can create a new home with all required details
- Home appears in the list after creation
- Admin can edit home information
- Admin can deactivate a home (with validation check)
- Caregivers cannot access home management functions
- All home operations are logged in audit trail
- Deactivation is prevented if home has active clients (shows error message)

### 2.2 Bed Management Module

**Description:** Enable Admins to configure beds within each home.

**Deliverables:**

- Beds database table with fields: ID, HomeID (FK), Label, Status (Available/Occupied), Active
- Backend API endpoints:
  - POST /api/homes/{homeId}/beds - Add bed to home
  - GET /api/homes/{homeId}/beds - List beds for home
  - PUT /api/beds/{id} - Update bed (rename, activate/inactivate)
- Frontend components:
  - Bed management section within home detail page
  - Add bed form
  - Bed list with status indicators (color-coded: green=available, red=occupied)
- Business logic: Prevent duplicate bed labels within same home

**Acceptance Criteria:**

- Admin can add multiple beds to a home
- Each bed has a unique label within its home
- Bed labels persist (do not change when clients are discharged)
- Bed status shows Available or Occupied
- Admin can inactivate a bed (if not currently occupied)
- System prevents duplicate bed labels in same home
- Bed operations are logged in audit trail

### 2.3 Client Management Module

**Description:** Implement client (resident) management with admission and discharge workflows.

**Deliverables:**

- Clients database table with fields: ID, FirstName, LastName, DateOfBirth, Gender, SSN (encrypted), AdmissionDate, DischargeDate, HomeID (FK), BedID (FK), PrimaryPhysician, EmergencyContact, Allergies, Diagnoses, MedicationList, Photo, Active status
- Backend API endpoints:
  - POST /api/clients - Admit new client
  - GET /api/clients - List clients (filtered by role/home)
  - GET /api/clients/{id} - Get client details
  - PUT /api/clients/{id} - Update client information
  - POST /api/clients/{id}/discharge - Discharge client
  - POST /api/clients/{id}/transfer - Transfer client to different bed
- Frontend pages:
  - Client list page with filters (by home, active/inactive)
  - Admit client form with bed selection dropdown (shows only available beds)
  - Client profile page with tabs for: Personal Info, Medical Info, Care Logs, Documents, Activities
  - Edit client form
  - Discharge client dialog with date and reason
- Business logic:
  - Validate bed is available before admission
  - Mark bed as Occupied on admission
  - Mark bed as Available on discharge
  - Retain discharged client data (set Active=false)
  - Caregivers can only see clients in their assigned homes

**Acceptance Criteria:**

- Admin can admit a client and assign to available bed
- Bed status changes to Occupied upon admission
- Client appears in active client list
- Admin can edit client information (logged in audit)
- Admin can discharge client with date and reason
- Bed becomes Available after discharge
- Discharged client moves to inactive list (not deleted)
- Caregivers only see clients in their assigned home(s)
- Attempting to access unauthorized client returns 403
- Client profile shows all relevant information in organized layout
- All client operations are logged in audit trail

### 2.4 Caregiver Management and Assignment Module

**Description:** Implement caregiver invitation, management, and home assignment functionality.

**Deliverables:**

- CaregiverHomeAssignments junction table: CaregiverID (FK), HomeID (FK)
- Backend API endpoints:
  - POST /api/caregivers/invite - Invite new caregiver (sends email)
  - GET /api/caregivers - List all caregivers
  - GET /api/caregivers/{id} - Get caregiver details
  - PUT /api/caregivers/{id} - Update caregiver info
  - POST /api/caregivers/{id}/deactivate - Deactivate caregiver
  - POST /api/caregivers/{id}/assign-homes - Assign caregiver to home(s)
  - DELETE /api/caregivers/{id}/homes/{homeId} - Remove home assignment
- Frontend pages:
  - Caregivers list page
  - Invite caregiver form (email, name, phone)
  - Caregiver detail page showing assigned homes
  - Home assignment interface (multi-select with available homes)
  - Deactivate caregiver confirmation dialog
- Invitation workflow:
  - Generate unique invitation token
  - Send email with setup link (expires in 7 days)
  - User sets password and configures MFA via invitation link
  - Account marked as Active after completion

**Acceptance Criteria:**

- Admin can invite caregiver via email
- Invitation email is sent with secure link
- Caregiver can complete setup via invitation link
- Invitation link expires after 7 days
- Admin can assign multiple homes to a caregiver
- Admin can remove home assignments
- Caregiver sees only clients from assigned homes
- Admin can deactivate caregiver (prevents login, preserves audit history)
- Deactivated caregivers cannot log in
- Caregiver invitation and assignment operations are logged

### 2.5 Data Access Layer Enhancements

**Description:** Implement home-scoped data access patterns to enforce permissions.

**Deliverables:**

- Repository pattern or service layer with home filtering
- Extension methods for IQueryable to apply home scope
- Middleware to inject current user's home assignments into request context
- Helper methods to check caregiver access to specific clients

**Acceptance Criteria:**

- Caregiver queries automatically filter by assigned homes
- Attempting to access data outside assigned homes is blocked
- Admin queries return all data (no home filter)
- Data access patterns are consistent across all modules
- Authorization checks occur at both API and data access layers

### 2.6 Admin Dashboard

**Description:** Create an admin dashboard with KPIs and quick access to key functions.

**Deliverables:**

- Dashboard page with widgets showing:
  - Total number of homes
  - Total residents (active)
  - Available beds count
  - Occupied beds count
  - Recent incidents count (this week)
  - List of upcoming client birthdays (next 30 days)
  - Quick action buttons (Add Client, Add Home, View Reports)
- Responsive card-based layout using Ant Design components
- Real-time data updates (or refresh button)

**Acceptance Criteria:**

- Dashboard displays accurate KPIs
- Data refreshes on page load
- Dashboard is only accessible to Admin role
- Dashboard has clean, professional appearance
- Quick action buttons navigate to appropriate pages

### 2.7 Caregiver Home View

**Description:** Create a simplified view for caregivers to see their assigned home(s) and clients.

**Deliverables:**

- Caregiver dashboard showing:
  - Assigned home(s)
  - List of active clients in assigned home(s)
  - Quick access to log ADLs, vitals, notes
- Home selector (if caregiver assigned to multiple homes)
- Client cards with key info: name, photo, bed, allergies

**Acceptance Criteria:**

- Caregiver sees only assigned homes
- Client list is filtered to assigned home(s)
- If multiple homes assigned, caregiver can switch context
- Client cards are easy to read and navigate
- Quick actions are accessible from client cards

## Testing Requirements

- Unit tests for business logic (admission, discharge, assignments)
- Integration tests for all API endpoints
- Authorization tests verifying home-scoped access
- UI tests for key workflows (admit client, assign caregiver)
- Data integrity tests (bed status changes, client retention after discharge)

## Dependencies

- Phase 1 completed and deployed
- Email service operational for caregiver invitations

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Complex home-scoped authorization | Medium | Implement centralized authorization logic; thorough testing |
| Data integrity issues (bed conflicts) | High | Use database transactions; implement pessimistic locking if needed |
| Caregiver invitation emails not delivered | Medium | Provide alternative admin-assisted setup; monitor delivery rates |

## Success Criteria

- Admin can fully manage homes, beds, and clients
- Admin can invite and assign caregivers to homes
- Caregivers can only access data from their assigned homes
- Admission and discharge workflows function correctly
- Bed occupancy is tracked accurately
- All data operations are logged in audit trail
- Dashboard provides useful at-a-glance information
- UI is intuitive and responsive
