# Phase 4: Incident Reporting and Document Management

## Overview

Implement critical safety and compliance features including incident reporting with workflow and secure document management with granular access control. This phase addresses key regulatory requirements for tracking adverse events and securely storing/accessing client documents.

## Objectives

- Implement incident reporting system with review workflow
- Enable Admin notification for new incidents
- Implement secure document storage in Azure Blob with per-document access control
- Implement view-only document viewer for caregivers
- Create document request workflow for caregivers

## Prerequisites

- Phase 3 completed (all care logging modules functional)
- Azure Blob Storage configured and accessible
- Email service operational for notifications

## Tasks

### 4.1 Incident Reporting Module

**Description:** Implement comprehensive incident reporting with review workflow and status tracking.

**Deliverables:**

- Incidents database table with fields: ID, ClientID (FK), ReportedBy (FK), IncidentType (Fall/MedicationError/Injury/Behavioral/Other), IncidentDate, IncidentTime, Location, Description, ActionTaken, PersonsInvolved, Witnesses, NotificationsMade, Status (New/Reviewed/Closed), ReviewedBy (FK), ReviewedDate, ReviewNotes, CreatedAt, UpdatedAt
- Backend API endpoints:
  - POST /api/incidents - Create incident report
  - GET /api/incidents - List all incidents (filtered by home, status, date)
  - GET /api/incidents/{id} - Get incident details
  - PUT /api/incidents/{id}/status - Update incident status (Admin only)
  - POST /api/incidents/{id}/follow-up - Add follow-up note (Admin only)
- Frontend components:
  - Incident report form with all required fields:
    - Client selector
    - Incident type dropdown
    - Date/time pickers
    - Location field
    - Description (multi-line text, required)
    - Action taken (multi-line text, required)
    - Persons involved (text field)
    - Witnesses (text field)
    - Notifications made (checkboxes: Family, Doctor, Other)
  - Incident list/grid with filters (status, date range, type, client)
  - Incident detail view (read-only for caregivers after submission)
  - Admin incident review interface with:
    - Status change buttons (New → Reviewed → Closed)
    - Follow-up notes section
    - Incident history timeline
- Notification system:
  - Email notification to Admins when new incident is filed
  - In-app notification badge for Admin dashboard

**Acceptance Criteria:**

- Caregiver can create incident report with all required details
- System validates required fields before submission
- Incident is saved with status "New"
- Email notification is sent to all Admins upon incident creation
- Incident appears in Admin's incident queue
- Caregivers cannot edit incident after submission
- Admin can view incident details
- Admin can update status (New → Reviewed → Closed)
- Admin can add follow-up notes
- Status changes are logged in audit trail
- Incident list shows status with visual indicators (color-coded badges)
- Caregivers can view incidents they reported
- Caregivers can view all incidents in their assigned home (configurable)
- Incident reports are retained indefinitely (no deletion)
- All incident operations are logged in audit trail

### 4.2 Document Storage Infrastructure

**Description:** Set up secure Azure Blob Storage integration for client documents.

**Deliverables:**

- Documents database table (metadata) with fields: ID, ClientID (FK), FileName, BlobStoragePath, FileType, DocumentType (CarePlan/MedicalReport/ConsentForm/Other), UploadedBy (FK), UploadDate, FileSize, IsActive
- DocumentAccessPermissions table: DocumentID (FK), CaregiverID (FK), GrantedBy (FK), GrantedDate
- Backend services:
  - Blob storage service with SAS token generation (short-lived, HTTPS-only)
  - Document metadata service
  - Document access permission service
- API endpoints:
  - POST /api/clients/{clientId}/documents - Upload document (returns upload SAS URL)
  - GET /api/clients/{clientId}/documents - List documents (filtered by access)
  - GET /api/documents/{id}/sas - Get time-limited SAS URL for viewing (validates permissions)
  - DELETE /api/documents/{id} - Delete document (Admin only)
  - POST /api/documents/{id}/permissions - Grant access to caregiver
  - DELETE /api/documents/{id}/permissions/{caregiverId} - Revoke access
- Security:
  - SAS tokens expire in 5 minutes
  - SAS tokens restricted to specific blob and operation (read or write)
  - All SAS generation logged in audit trail
  - HTTPS-only enforcement on SAS URLs

**Acceptance Criteria:**

- Admin can upload document for a client via secure SAS URL
- Document metadata is saved in database
- Document file is stored in Azure Blob Storage
- Blob container is not publicly accessible
- SAS tokens are short-lived (5 minutes) and HTTPS-only
- Document list shows only documents user has permission to see
- Document uploads and SAS generations are logged in audit trail
- Large files (up to 50MB) can be uploaded successfully

### 4.3 Document Access Control Module

**Description:** Implement granular per-document access control for caregivers.

**Deliverables:**

- Frontend interfaces:
  - Document list in client profile showing all documents for Admin
  - Document list showing only accessible documents for Caregivers
  - Permission management UI for Admin:
    - Grant access modal with caregiver multi-select
    - Revoke access button
    - Visual indicators showing which caregivers have access
- Backend logic:
  - Check permissions before issuing SAS URLs
  - Filter document lists based on user role and permissions
- Access request workflow (optional):
  - Caregiver can request access to a document
  - Admin receives notification and can approve/deny

**Acceptance Criteria:**

- Admin sees all documents for all clients
- Admin can grant document access to specific caregivers (only caregivers in same home as client)
- Admin can revoke document access
- Caregivers see only documents they have been granted access to
- Caregivers see document name/type but cannot open without permission
- Attempting to access unpermitted document returns 403
- Permission grants and revocations are logged in audit trail
- Permission management UI is intuitive and clearly shows current permissions

### 4.4 Document Viewer (View-Only)

**Description:** Implement secure view-only document viewing for caregivers using embedded PDF viewer.

**Deliverables:**

- Frontend document viewer component:
  - PDF.js embedded viewer with download button hidden
  - Watermark overlay: "Confidential - For Internal Use Only"
  - No right-click menu (via CSS/JavaScript)
  - Viewer opens in modal or dedicated page
- Support for document types:
  - PDF files: in-app viewer
  - Image files: in-app image viewer
  - Other formats: show message "Contact admin for access to this document type"
- Backend:
  - Endpoint returns document via streaming (not direct blob URL)
  - Or returns short-lived SAS for viewer to fetch directly

**Acceptance Criteria:**

- PDFs open in embedded viewer with download disabled
- Viewer displays PDF clearly and allows page navigation/zoom
- Watermark appears on all pages
- Right-click menu is disabled in viewer
- Image files display in image viewer
- Non-PDF/image files show appropriate message
- Admin can download documents (download button enabled for Admin)
- Caregiver cannot download but can view
- Document access events are logged (who viewed which document when)
- Viewer works on desktop and tablet browsers

## Testing Requirements

- Unit tests for incident workflow logic
- Integration tests for document upload/access control
- Security tests for SAS token generation and expiration
- Authorization tests for document access
- UI tests for incident reporting workflow
- End-to-end tests for document upload → permission grant → viewing

## Dependencies

- Phase 3 completed (care logging data available)
- Azure Blob Storage configured with proper encryption
- Email service operational for incident notifications

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Document viewer security (screenshots) | Medium | Accept limitation; watermark and audit logs provide deterrence |
| Incident notification delivery failures | Medium | Log notification attempts; provide in-app notification as backup |
| SAS token security | High | Short expiration, HTTPS-only, audit all generations |
| Large file upload failures | Medium | Implement chunked uploads; provide clear error messages |

## Success Criteria

- Incidents can be reported and tracked through review workflow
- Admins receive timely notifications of new incidents
- Documents are securely stored and accessible only to authorized users
- Document viewer prevents casual downloading by caregivers
- All document and incident operations are fully audited
- System meets HIPAA requirements for document access logging
- UI is intuitive for reporting incidents and managing documents

## Next Phase

Phase 5 will build upon this foundation by implementing the Reporting Module with data aggregation and PDF generation capabilities.
