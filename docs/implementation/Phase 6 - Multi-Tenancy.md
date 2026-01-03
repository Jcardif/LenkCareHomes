# Phase 6: Multi-Tenancy

## Overview

Transform LenkCare Homes from a single-operator system into a multi-tenant SaaS platform. This phase introduces organization-based data isolation, enabling multiple care home operators to use the same application infrastructure while keeping their data completely separated. The implementation uses a shared database with row-level security via EF Core global query filters, org-scoped roles, and JWT-based tenant context.

## Objectives

- Introduce Organization entity as the tenant boundary
- Implement OrganizationMembership for user-to-org relationships with org-scoped roles
- Add OrganizationId to all tenant-scoped entities (Azure SQL, Cosmos DB, Blob Storage)
- Implement Row-Level Security via EF Core global query filters
- Modify authentication to include organization context in JWT
- Create organization selection flow for users with multiple memberships
- Migrate existing data across all data stores to the first organization
- Implement organization creation flow for new tenants
- Implement organization settings management
- Update audit logging to include OrganizationId

## Prerequisites

- Phase 5 completed (all core features implemented including reporting)
- Maintenance window scheduled for migration
- Database backup taken prior to migration
- Existing admin user identified for organization ownership
- Staging environment available for migration testing

## Architecture Overview

### Tenant Model

```
┌─────────────────────────────────────────────────────────────────┐
│  Organization (Tenant)                                           │
├─────────────────────────────────────────────────────────────────┤
│  Id, Name, IsActive, CreatedAt, UpdatedAt                       │
└─────────────────────────────────────────────────────────────────┘
         │
         │ Many-to-Many via OrganizationMembership
         ▼
┌─────────────────────────────────────────────────────────────────┐
│  OrganizationMembership                                          │
├─────────────────────────────────────────────────────────────────┤
│  Id, UserId (FK), OrganizationId (FK), Role,                    │
│  JoinedAt, IsActive                                              │
└─────────────────────────────────────────────────────────────────┘
         │
         │ A user can have multiple memberships
         ▼
┌─────────────────────────────────────────────────────────────────┐
│  ApplicationUser                                                 │
├─────────────────────────────────────────────────────────────────┤
│  (Existing fields - no OrganizationId on user directly)         │
│  All roles are org-scoped (via OrganizationMembership)          │
└─────────────────────────────────────────────────────────────────┘
```

### Role Structure

All roles are now **org-scoped** via OrganizationMembership. There are no global/platform roles.

| Role | Scope | PHI Access | Description |
|------|-------|------------|-------------|
| **OrganizationAdministrator** | Org-wide | ✅ Full | Created the org, can transfer ownership, manages org settings and all memberships |
| **Admin** | Org-wide | ✅ Full | Manages homes, clients, caregivers, documents, reports within org |
| **Caregiver** | Assigned homes | ✅ Limited | Logs care activities, limited to assigned homes |
| **OrganizationSysadmin** | Org-wide | ❌ None | Org's IT person, system maintenance, MFA resets, audit logs, no PHI access |

### Entity Ownership

| Entity | Has OrganizationId? | Isolation Method |
|--------|---------------------|------------------|
| Organization | N/A | This IS the tenant |
| OrganizationMembership | Yes | Join table |
| Home | **Yes** | Direct FK |
| ApplicationUser | **No** | Via memberships |
| Client | **Yes** (denormalized) | Direct FK + via Home |
| ADLLog, VitalsLog, etc. | Via Client | Cascaded through relationships |
| Document | **Yes** (denormalized) | Direct FK |
| Incident | **Yes** (denormalized) | Direct FK |
| Audit Logs (Cosmos) | **Yes** | Partition key |
| Blob: Documents | **Yes** | Container path: `{orgId}/{clientId}/` |
| Blob: Incident Photos | **Yes** | Container path: `{orgId}/incidents/{incidentId}/` |

### Row-Level Security (RLS) Enforcement

Data isolation is enforced at multiple layers:

```
┌─────────────────────────────────────────────────────────────────┐
│  Layer 1: API Authorization                                     │
│  - JWT contains orgId claim                                     │
│  - Authorization policies validate org membership               │
│  - Requests without valid org context rejected (403)            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Layer 2: EF Core Global Query Filters (Primary RLS)           │
│  - All queries automatically filtered by OrganizationId        │
│  - Applied at DbContext level before SQL generation            │
│  - Cannot be bypassed by application code (except migrations)  │
│  - Covers: Homes, Clients, Documents, Incidents, Memberships   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Layer 3: Cosmos DB Partition Key                               │
│  - Audit logs partitioned by OrganizationId                     │
│  - Queries must include partition key (efficient + isolated)   │
│  - Cross-partition queries disabled for user requests          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Layer 4: Blob Storage Path Isolation                           │
│  - Documents stored under: {container}/{orgId}/{clientId}/     │
│  - SAS tokens scoped to organization prefix                    │
│  - Service validates org ownership before generating SAS       │
└─────────────────────────────────────────────────────────────────┘
```

**Why EF Core Filters instead of SQL Server RLS?**

| Approach | Pros | Cons |
|----------|------|------|
| **EF Core Global Filters** (chosen) | Works across all databases, testable, no DB-specific syntax, visible in code | Requires discipline to not bypass |
| SQL Server RLS Policies | Database-enforced, harder to bypass | SQL Server specific, complex setup, harder to test |

We use EF Core filters as the primary mechanism with defense-in-depth via API authorization.

## Tasks

### 6.1 Organization Entity and Database Schema

**Description:** Create the Organization entity and OrganizationMembership join table.

**Deliverables:**

- Organization entity with fields:
  - Id (Guid, Primary Key)
  - Name (string, required, max 200 chars)
  - IsActive (bool, default true)
  - CreatedAt (DateTime)
  - UpdatedAt (DateTime, nullable)
- OrganizationMembership entity with fields:
  - Id (Guid, Primary Key)
  - UserId (Guid, FK to Users)
  - OrganizationId (Guid, FK to Organizations)
  - Role (string: "OrganizationAdministrator", "Admin", "Caregiver", or "OrganizationSysadmin")
  - JoinedAt (DateTime)
  - IsActive (bool, default true)
- Database tables: Organizations, OrganizationMemberships
- Unique constraint on (UserId, OrganizationId) in OrganizationMemberships
- Navigation properties on ApplicationUser for Memberships

**Acceptance Criteria:**

- Organizations table is created with proper constraints
- OrganizationMemberships table is created with proper foreign keys
- A user can have multiple memberships (one per organization)
- A user can only have one membership per organization
- Organization has navigation to its memberships
- User has navigation to their memberships
- Migrations apply successfully

### 6.2 Add OrganizationId to Tenant-Scoped Entities

**Description:** Add OrganizationId column to all entities that require tenant isolation.

**Deliverables:**

- Add OrganizationId (Guid, FK) to:
  - Homes
  - Clients (denormalized for query efficiency)
  - Documents (denormalized)
  - DocumentFolders (denormalized)
  - Incidents (denormalized)
- Foreign key constraints to Organizations table
- Indexes on OrganizationId for query performance
- Columns initially nullable for migration compatibility

**Acceptance Criteria:**

- All target tables have OrganizationId column
- Foreign key constraints are in place
- Indexes are created for performance
- Existing queries still function (nullable during migration)
- Migrations apply successfully without data loss

### 6.3 Data Migration for Existing Data (All Data Stores)

**Description:** Backfill existing data across Azure SQL, Cosmos DB, and Blob Storage with organization context.

**Deliverables:**

#### Azure SQL Migration:
- Migration script to:
  1. Create the first Organization record ("LenkCare Homes" or configured name)
  2. Backfill Homes.OrganizationId with the new org ID
  3. Backfill denormalized OrganizationId on Clients, Documents, DocumentFolders, Incidents
  4. Create OrganizationMembership records from existing user roles:
     - First Admin user → Membership with Role="OrganizationAdministrator"
     - Other Admin users → Membership with Role="Admin"
     - Users with Caregiver role → Membership with Role="Caregiver"
     - Users with Sysadmin role → Membership with Role="OrganizationSysadmin"
  5. Make OrganizationId columns non-nullable after backfill

#### Cosmos DB Audit Log Migration:
- Script to update all existing audit log documents:
  1. Query all audit log entries without OrganizationId
  2. Add OrganizationId field to each document (set to first org ID)
  3. Re-partition documents if partition key strategy changes
  4. Verify all documents have OrganizationId
- Handle documents that cannot be updated (log and flag for review)
- Consider batch processing for large audit log volumes

#### Blob Storage Migration (All Containers):

**Container: `documents`** (Client documents - PDFs, care plans, etc.)
- Script to reorganize existing document blobs:
  1. List all blobs in documents container
  2. Copy each blob to new path: `{orgId}/{clientId}/{filename}`
  3. Update Document table references to new blob paths
  4. Verify all blobs accessible at new locations
  5. Delete old blob paths after verification (or archive)
- Generate new SAS URL patterns for migrated documents

**Container: `incident-photos`** (Incident report photos)
- Script to reorganize existing incident photo blobs:
  1. List all blobs in incident-photos container
  2. Copy each blob to new path: `{orgId}/incidents/{incidentId}/{filename}`
  3. Update IncidentPhoto table BlobPath references
  4. Verify all photos accessible at new locations
  5. Delete old blob paths after verification

**SAS Token Updates:**
- Update SAS generation to scope tokens to org prefix
- Validate org ownership before generating any SAS URL
- Update all services that generate blob URLs

#### Verification and Rollback:
- Verification queries to validate migration completeness across all stores
- Rollback scripts for each data store
- Migration logs with success/failure counts

**Acceptance Criteria:**

- First organization is created successfully
- **Azure SQL:** All homes, clients, documents, incidents have OrganizationId
- **Cosmos DB:** All existing audit logs have OrganizationId appended
- **Blob Storage:** All blobs in `documents` and `incident-photos` containers moved to org-prefixed paths
- All admins and caregivers have organization memberships
- First admin is assigned OrganizationAdministrator role
- Existing Sysadmins become OrganizationSysadmin within the org
- OrganizationId columns are non-nullable after migration
- Verification queries confirm 100% data coverage across all stores
- Document URLs continue to work (or are updated in database)

### 6.4 Current Organization Service

**Description:** Create a service to manage and provide the current organization context per request.

**Deliverables:**

- ICurrentOrganizationService interface with:
  - OrganizationId property (Guid?)
  - OrganizationName property (string?)
  - UserRole property (string?) - role in current org
  - IsOrganizationAdministrator property (bool)
  - SetOrganization(Guid orgId) method
- CurrentOrganizationService implementation:
  - Reads organization from JWT claims
  - Validates user has membership to the organization
  - Provides organization context to DbContext and services
- Scoped lifetime registration in DI container
- Middleware to extract and validate organization from JWT

**Acceptance Criteria:**

- Service correctly reads organization from JWT
- Service validates user has active membership
- Invalid organization context returns 403 Forbidden
- Service is available via DI throughout request pipeline
- All users require organization context (no global roles)
- Organization context is available before DbContext is used

### 6.5 EF Core Global Query Filters

**Description:** Implement automatic data isolation using EF Core global query filters.

**Deliverables:**

- Modify ApplicationDbContext to accept ICurrentOrganizationService
- Add query filters to all tenant-scoped entities:
  - Home: filter by OrganizationId
  - Client: filter by OrganizationId
  - Document: filter by OrganizationId
  - DocumentFolder: filter by OrganizationId
  - Incident: filter by OrganizationId
  - OrganizationMembership: filter by OrganizationId
- Method to bypass filters for migrations only (no cross-tenant user access)
- Child entities (ADLLog, VitalsLog, etc.) filtered via parent relationships

**Acceptance Criteria:**

- All queries automatically filter by current organization
- Admin in Org A cannot see Org B's homes, clients, or data
- Migrations can bypass filters when needed (no user role can bypass)
- Query filters do not impact performance significantly
- Eager loading and includes work correctly with filters
- No data leakage between organizations
- Unit tests verify filter behavior

### 6.6 Authentication Flow Updates

**Description:** Modify authentication to include organization context in JWT and handle org selection.

**Deliverables:**

- Update JWT claims to include:
  - org: Organization ID
  - orgRole: Role in selected organization ("OrganizationAdministrator", "Admin", "Caregiver", or "OrganizationSysadmin")
  - Note: No global roles - all roles are org-scoped
- Login flow changes:
  - After credential validation, check user's organization memberships
  - If 0 memberships → error (orphan user, must belong to an org)
  - If 1 membership → auto-select, include in JWT
  - If 2+ memberships → return list, require selection
- Organization selection endpoint:
  - POST /api/auth/select-organization
  - Validates user has membership
  - Issues new JWT with selected organization
- Organization switching:
  - Allow switching without full re-authentication
  - Re-validate membership on switch
  - Issue new JWT with new organization context

**Acceptance Criteria:**

- JWT includes organization context for authenticated users
- Users with single org are auto-directed to that org
- Users with multiple orgs see organization picker
- Organization selection issues valid JWT with org context
- Organization switching works without re-entering credentials
- All users require organization context to access the system
- All auth events are logged with organization context

### 6.7 Authorization Policy Updates

**Description:** Update authorization to use org-scoped roles from memberships.

**Deliverables:**

- New authorization policies:
  - OrgAdministrator: checks orgRole claim = "OrganizationAdministrator"
  - OrgAdmin: checks orgRole claim = "Admin" OR "OrganizationAdministrator"
  - OrgCaregiver: checks orgRole claim = "Caregiver" OR "Admin" OR "OrganizationAdministrator"
  - OrgSysadmin: checks orgRole claim = "OrganizationSysadmin"
  - OrgMember: checks org claim exists (any role)
  - PHIAccess: checks orgRole is NOT "OrganizationSysadmin" (excludes IT from PHI)
- Update existing [Authorize(Roles = "Admin")] to use OrgAdmin policy
- Update existing [Authorize(Roles = "Caregiver")] to use OrgCaregiver policy
- Authorization handler to validate org membership on all operations

**Acceptance Criteria:**

- OrganizationAdministrator has full org management + Admin capabilities
- Admin in Org A has admin access in Org A only
- Admin in Org A does not have admin access in Org B
- Caregiver role is scoped to organization and assigned homes
- OrganizationSysadmin can access non-PHI functions only (audit logs, MFA resets, user management)
- OrganizationSysadmin CANNOT access client data, documents, care logs, or incidents
- Authorization failures return 403 with appropriate message
- Role checks are consistent across all controllers
- Unit tests verify authorization behavior and PHI restrictions

### 6.8 Frontend Organization Context

**Description:** Implement organization context management in the frontend.

**Deliverables:**

- OrganizationContext React context provider
- Organization selector component (for multi-org users)
- Organization switcher in header/sidebar
- Update AuthContext to include organization information
- Update API client to include organization in headers (if needed)
- Local storage for last selected organization (convenience)
- Update ProtectedRoute to validate organization context

**Acceptance Criteria:**

- Users with multiple orgs see organization picker after login
- Selected organization is stored in context
- Organization name displays in UI header
- Users can switch organizations without logout
- API calls include organization context
- Organization switcher only shows for multi-org users
- UI correctly reflects current organization's data

### 6.9 User Management Updates

**Description:** Update user management to work with organization memberships.

**Deliverables:**

- Update user invitation to create OrganizationMembership:
  - Invite user to specific organization
  - Set role in membership (Admin, Caregiver, or OrganizationSysadmin)
  - Only OrganizationAdministrator can invite new OrganizationAdministrators
- Update user listing:
  - Filter by current organization
  - Show user's role in current organization
- Update user editing:
  - Change user's role within organization
  - Add/remove user from organizations (OrganizationAdministrator only)
- New organization management page (for OrganizationAdministrator):
  - View organization details
  - Invite users to organization
  - Manage user roles within organization
  - Transfer OrganizationAdministrator role to another Admin

**Acceptance Criteria:**

- Admin and OrganizationAdministrator can invite users to their organization
- Only OrganizationAdministrator can invite/promote to OrganizationAdministrator role
- Invited users are added to the inviting organization
- User list shows only users in current organization
- Admin can change user roles (except OrganizationAdministrator) within their organization
- OrganizationAdministrator can remove users from organization
- OrganizationAdministrator can transfer their role to another Admin
- Removing user from org does not delete the user account
- User management operations are logged with organization context

### 6.10 Audit Logging Updates

**Description:** Update audit logging to include organization context for multi-tenant compliance.

**Deliverables:**

- Add OrganizationId field to audit log entries
- Update Cosmos DB container partition key to include OrganizationId
- Update audit log queries to filter by organization
- OrganizationSysadmin can view audit logs for their organization only
- Update audit log viewer to show organization context
- Filter audit logs by organization in the UI

**Acceptance Criteria:**

- All new audit entries include OrganizationId
- Audit log viewer filters by current organization
- OrganizationSysadmin can view logs for their organization only
- Cosmos DB queries are efficient with new partition key
- Historical audit entries (pre-migration) are handled gracefully
- Organization context is visible in audit log detail view

### 6.11 CaregiverHomeAssignment Compatibility

**Description:** Ensure caregiver home assignments work correctly with multi-tenancy.

**Deliverables:**

- Validate home assignment within organization context:
  - Caregiver can only be assigned to homes in their organization membership
  - Home selector shows only homes in current organization
- Query filters ensure assignments only return for current org
- Update assignment API to validate organization context

**Acceptance Criteria:**

- Caregivers can only be assigned to homes in orgs they belong to
- Home assignment dropdown shows only current org's homes
- Cross-organization assignments are prevented
- Existing assignments continue to work
- Assignment queries respect organization filters

### 6.12 Organization Creation Flow

**Description:** Implement the ability for new tenants to create organizations and onboard.

**Deliverables:**

- Organization creation API:
  - POST /api/organizations - Create new organization
  - Requires authenticated user (becomes OrganizationAdministrator)
  - Creates Organization record with provided name
  - Creates OrganizationMembership for creator as OrganizationAdministrator
  - Initializes organization settings with defaults
- Organization creation UI:
  - "Create Organization" page accessible after signup
  - Organization name input with validation
  - Terms and conditions acceptance
  - Success redirects to new org dashboard
- First-time user flow:
  - New users without org membership see "Create or Join Organization" screen
  - Option to create new org or enter invite code
- Cosmos DB container setup:
  - Create audit log partition for new organization
- Blob Storage setup:
  - Create organization folder/prefix in documents container

**Acceptance Criteria:**

- New user can create an organization
- Creator automatically becomes OrganizationAdministrator
- New org has default settings configured
- Audit logging works immediately for new org
- Document storage is ready for new org
- Organization name must be unique (or allow duplicates with different IDs)
- Creation is logged in audit trail

### 6.13 Organization Settings

**Description:** Implement organization-level settings management.

**Deliverables:**

- OrganizationSettings entity:
  - Id (Guid, PK)
  - OrganizationId (Guid, FK, unique)
  - DisplayName (string) - for UI display
  - TimeZone (string) - organization's timezone
  - DateFormat (string) - preferred date format
  - ContactEmail (string) - org contact email
  - ContactPhone (string, nullable) - org contact phone
  - Address (string, nullable) - org address
  - CreatedAt, UpdatedAt
- Settings API:
  - GET /api/organizations/settings - Get current org settings
  - PUT /api/organizations/settings - Update settings (OrgAdmin only)
- Settings UI:
  - Organization Settings page (Admin section)
  - Timezone selector
  - Contact information form
- Apply settings:
  - Display org name in header
  - Use timezone for date displays
  - Include contact info in reports

**Acceptance Criteria:**

- OrganizationAdministrator can view and edit org settings
- Admin can view but not edit org settings
- Caregiver and OrganizationSysadmin cannot access settings page
- Timezone affects date/time displays throughout app
- Settings changes are logged in audit trail
- Default settings created when organization is created

### 6.14 Cleanup of Legacy Role Assignments

**Description:** Remove legacy global role assignments for Admin and Caregiver after migration.

**Deliverables:**

- Script to remove all entries from ASP.NET Identity UserRoles table
- All roles are now in OrganizationMembership, not Identity Roles
- Remove or mark inactive all entries in Roles table (no longer used)
- Update any hardcoded role checks in code to use OrganizationMembership

**Acceptance Criteria:**

- UserRoles table is empty (all roles via OrganizationMembership)
- Authentication correctly uses membership roles for all users
- All users must have at least one OrganizationMembership to login
- No orphaned role references in code
- Cleanup is reversible during validation period

## Migration Execution Plan

### Staging Environment (Dress Rehearsal)

1. Take database backup
2. Run Phase 1-2 migrations (schema changes)
3. Run data backfill scripts
4. Deploy new code (Phase 3 code changes)
5. Full testing of all scenarios
6. Run cleanup scripts (Phase 4)
7. Document timing and issues
8. Fix any issues found

### Production Environment

1. Announce maintenance window
2. Take database backup
3. Run Phase 1-2 migrations (schema + data)
4. Deploy new code
5. Smoke test (admin login, basic operations)
6. End maintenance window
7. Monitor for issues
8. Phase 4 cleanup (days later, after validation)

### Validation Checkpoints

**After Data Migration (Azure SQL):**

- [ ] All homes have OrganizationId
- [ ] All clients have OrganizationId
- [ ] All documents have OrganizationId
- [ ] All incidents have OrganizationId
- [ ] All non-Sysadmin users have at least one membership
- [ ] At least one user has OrganizationAdministrator role
- [ ] Membership count ≈ previous role assignment count

**After Data Migration (Cosmos DB):**

- [ ] All existing audit log entries have OrganizationId
- [ ] Partition key queries work correctly
- [ ] No orphaned audit entries without OrganizationId
- [ ] Audit log viewer shows migrated entries

**After Data Migration (Blob Storage):**

- [ ] All blobs in `documents` container moved to org-prefixed paths
- [ ] All blobs in `incident-photos` container moved to org-prefixed paths
- [ ] Document references in SQL updated to new paths
- [ ] IncidentPhoto references in SQL updated to new paths
- [ ] Documents viewable through application
- [ ] Incident photos viewable through application
- [ ] Old blob paths cleaned up or archived

**After Code Deployment:**

- [ ] Admin can log in and see their organization's data
- [ ] Caregivers can log in and access assigned homes
- [ ] OrganizationSysadmin can log in and access non-PHI functions
- [ ] Data is properly isolated (cannot see other orgs)
- [ ] New records are created with OrganizationId in all stores
- [ ] Audit logs include OrganizationId
- [ ] New organizations can be created
- [ ] Organization settings can be viewed and edited

## Dependencies

- All previous phases completed
- Maintenance window scheduled
- Stakeholder approval for multi-tenant architecture

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Data migration fails | Critical | Full backup before migration; tested rollback procedure |
| Query filter bypass leads to data leak | Critical | Thorough code review; security testing |
| Performance degradation from filters | Medium | Index optimization; query analysis |
| Users locked out during migration | High | Communication; short maintenance window |
| Orphaned data (missing OrgId) | Medium | Validation queries; constraint enforcement |

## Success Criteria

- Organization entity and memberships are functioning
- All existing data is migrated to the first organization (SQL + Cosmos + Blob)
- Query filters prevent cross-organization data access
- Authentication includes organization context in JWT
- Authorization uses org-scoped roles correctly
- OrganizationSysadmin cannot access PHI
- Audit logs include organization context (including migrated historical logs)
- Blob storage documents are org-isolated
- New organizations can be created by new users
- Organization settings can be configured
- Users with multiple orgs can switch between them
- No global roles exist - all users scoped to organizations
- Performance is not significantly impacted
- Security testing confirms data isolation across all data stores

## Future Enhancements (Out of Scope for Phase 6)

- Organization logo and custom branding
- Subscription and billing per organization
- Cross-organization user sharing
- Organization-level feature flags
- Multi-region data residency
- Custom domain per organization
- White-label branding options
