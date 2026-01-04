# Implementation Phases

This directory contains detailed implementation plans for each phase of the LenkCare Homes application development.

## Phase Overview

| Phase | Name | Description | Status |
|-------|------|-------------|--------|
| 1 | [Foundation and Authentication](Phase%201%20-%20Foundation%20and%20Authentication.md) | Azure infrastructure, authentication with MFA, RBAC framework, audit logging | ✅ Complete |
| 2 | [Core Data Management](Phase%202%20-%20Core%20Data%20Management.md) | Home & Bed management, Client management, Caregiver assignments, Admin dashboard | In Progress |
| 3 | [Daily Care Logging](Phase%203%20-%20Daily%20Care%20Logging.md) | ADL tracking (Katz Index), Vitals monitoring, ROM exercises, Behavior notes, Activities | Not Started |
| 4 | [Incident Reporting and Document Management](Phase%204%20-%20Incident%20Reporting%20and%20Document%20Management.md) | Incident reporting with workflow, Document storage with Azure Blob, Access control, View-only viewer | Not Started |
| 5 | [Reporting Module](Phase%205%20-%20Reporting%20Module.md) | Data aggregation, PDF generation, Client and Home summary reports | Not Started |
| 6 | [Multi-Tenancy](Phase%206%20-%20Multi-Tenancy.md) | Organization entity, org-scoped roles, EF Core query filters, data isolation, migration | Not Started |
| 7 | [Mobile Foundation and Authentication](Phase%207%20-%20Mobile%20Foundation%20and%20Authentication.md) | .NET MAUI setup, mobile auth with biometrics, device registration, core navigation | Not Started |
| 8 | [Mobile Care Logging](Phase%208%20-%20Mobile%20Care%20Logging.md) | ADL, vitals, ROM, notes, activities, incident reporting with camera | Not Started |
| 9 | [Push Notifications and MFA Approval](Phase%209%20-%20Push%20Notifications%20and%20MFA%20Approval.md) | Admin incident alerts, MFA approval from mobile, app store preparation | Not Started |
| 10 | [Finalization and Production Readiness](Phase%2010%20-%20Finalization%20and%20Production%20Readiness.md) | Audit log UI, Accessibility (WCAG 2.1 AA), Security hardening, UAT, Go-live | Not Started |

## Phase Dependencies

```
Phase 1: Foundation and Authentication
    └── Phase 2: Core Data Management
            └── Phase 3: Daily Care Logging
                    └── Phase 4: Incident Reporting and Document Management
                            └── Phase 5: Reporting Module
                                    └── Phase 6: Multi-Tenancy
                                            └── Phase 7: Mobile Foundation and Authentication
                                                    └── Phase 8: Mobile Care Logging
                                                            └── Phase 9: Push Notifications and MFA Approval
                                                                    └── Phase 10: Finalization and Production Readiness
```

## Key Milestones

### Phase 1 - Foundation
- Azure infrastructure provisioned
- Authentication with TOTP MFA working
- Role-based access control (Admin, Caregiver, Sysadmin)
- Audit logging to Cosmos DB

### Phase 2 - Core Data
- Homes and beds can be managed
- Clients can be admitted and discharged
- Caregivers can be invited and assigned to homes
- Home-scoped data access enforced

### Phase 3 - Care Logging
- ADLs trackable using Katz Index
- Vitals monitoring (BP, pulse, temp, O₂)
- ROM exercises logging
- Behavior and mood notes
- Recreational activity tracking

### Phase 4 - Incidents & Documents
- Incident reporting with status workflow (New → Reviewed → Closed)
- Admin notifications for new incidents
- Secure document storage in Azure Blob
- Per-document access control for caregivers
- View-only document viewer with watermark

### Phase 5 - Reporting
- Data aggregation services
- PDF generation for reports
- Client Summary Report
- Home Summary Report
- Professional formatting with confidentiality notices

### Phase 6 - Multi-Tenancy
- Organization entity as tenant boundary
- OrganizationMembership for user-org relationships
- Four org-scoped roles: OrganizationAdministrator, Admin, Caregiver, OrganizationSysadmin
- Row-Level Security via EF Core global query filters
- JWT-based organization context
- Organization selection for multi-org users
- Complete data migration across Azure SQL, Cosmos DB, and Blob Storage
- Existing audit logs in Cosmos DB updated with OrganizationId
- Blob storage documents reorganized under org prefixes
- Organization creation flow for new tenants
- Organization settings management (timezone, logo, contact info)
- OrganizationSysadmin: IT support without PHI access

### Phase 7 - Mobile Foundation
- .NET MAUI project for iOS and Android
- Mobile authentication with biometric support
- Device registration for push notifications
- Role-based access: OrgAdmin/Admin see all homes, Caregiver sees assigned only
- OrganizationSysadmin blocked (no PHI on mobile)
- View upcoming appointments (read-only)

### Phase 8 - Mobile Care Logging
- ADL logging (Katz Index)
- Vitals logging (BP, pulse, temp, O₂)
- ROM exercise logging
- Behavior and mood notes
- Recreational activity logging
- Incident reporting with camera/photos
- Client care timeline view
- Available to OrgAdmin, Admin, and Caregiver roles

### Phase 9 - Notifications & MFA
- Push notification infrastructure
- Incident alerts to OrgAdmin and Admin users only
- MFA approval from mobile (number matching)
- 90-second approval timeout
- iOS App Store submission
- Google Play Store submission

### Phase 10 - Production Ready
- Audit log viewing interface
- WCAG 2.1 AA accessibility compliance
- Security hardening and penetration testing
- Performance optimization
- User documentation and training
- Production deployment and hypercare

## Related Documentation

- [Architecture and Design](../Architecture%20and%20Design.md) - Full system specification
- [Multi-Tenancy Architecture](../Multi-Tenancy%20Architecture.md) - Multi-tenant design and data isolation
- [Mobile Application Architecture](../Mobile%20Application%20Architecture.md) - .NET MAUI mobile app design (Phases 7-9)
- [AGENTS.md](../../AGENTS.md) - Development guidelines for coding agents
