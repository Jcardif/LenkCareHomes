# Multi-Tenancy Architecture

This document describes the multi-tenancy architecture for LenkCare Homes, extending the core [Architecture and Design](Architecture%20and%20Design.md) document. It defines how the system isolates data between organizations (tenants) while sharing the same application infrastructure.

## Purpose

LenkCare Homes is evolving from a single-operator system to a multi-tenant SaaS platform. This enables multiple care home operators to use the same application while maintaining complete data isolation. Each organization (tenant) operates as if they have their own dedicated system, but the underlying infrastructure is shared for cost efficiency and maintainability.

## Multi-Tenancy Model

### Tenant Definition

In LenkCare Homes, a **tenant** is an **Organization** — typically a care home operator or company that manages one or more adult family homes. Each organization has its own:

- Homes and beds
- Clients (residents)
- Staff members (caregivers, admins)
- Documents
- Incidents
- Audit trail

### Isolation Strategy

The system uses a **shared database with row-level isolation** approach:

| Approach | Description | Used By LenkCare |
|----------|-------------|------------------|
| Database per tenant | Separate database for each org | ❌ Too expensive |
| Schema per tenant | Separate schema in shared DB | ❌ Complex migrations |
| **Row-level isolation** | Shared tables with OrganizationId filter | ✅ **Selected** |

**Rationale:** Row-level isolation provides sufficient data separation for HIPAA compliance while keeping infrastructure costs low and deployments simple. EF Core global query filters ensure all queries are automatically scoped to the current organization.

## Data Model

### Organization Entity

The Organization entity represents a tenant in the system:

```
Organization
├── Id (Guid) - Primary Key
├── Name (string) - Organization display name
├── IsActive (bool) - Whether org is active
├── CreatedAt (DateTime) - When org was created
└── UpdatedAt (DateTime?) - Last modification
```

### Organization Membership

Users can belong to multiple organizations with different roles in each:

```
OrganizationMembership
├── Id (Guid) - Primary Key
├── UserId (Guid, FK) - Reference to User
├── OrganizationId (Guid, FK) - Reference to Organization
├── Role (string) - "Admin" or "Caregiver" (org-scoped)
├── IsOwner (bool) - Can manage org settings and users
├── JoinedAt (DateTime) - When user joined org
└── IsActive (bool) - Whether membership is active
```

**Key Constraints:**
- A user can have at most one membership per organization
- A user can have different roles in different organizations
- An organization must have at least one owner

### Entity Ownership

Entities are scoped to organizations through the OrganizationId foreign key:

| Entity | Has OrganizationId? | Notes |
|--------|---------------------|-------|
| Organization | N/A | This IS the tenant |
| OrganizationMembership | Yes | Join table |
| Home | **Yes** | Root tenant-scoped entity |
| Bed | Via Home | Inherits from Home |
| Client | **Yes** (denormalized) | For query efficiency |
| CaregiverHomeAssignment | Via Home | Inherits from Home |
| ADLLog, VitalsLog, etc. | Via Client | Inherits from Client |
| Document | **Yes** (denormalized) | For query efficiency and RLS |
| DocumentFolder | **Yes** (denormalized) | For query efficiency |
| Incident | **Yes** (denormalized) | For query efficiency |
| Appointment | Via Home/Client | Inherits from relationships |
| ApplicationUser | **No** | Users are cross-organization |
| Audit Logs (Cosmos) | **Yes** | Partition key for isolation |

**Denormalization Rationale:** While the OrganizationId could be derived from parent relationships (e.g., Client → Home → OrganizationId), denormalizing to key entities improves query performance and simplifies EF Core query filters.

## Role-Based Access Control

### Role Types

LenkCare Homes has two types of roles:

| Role Type | Storage Location | Scope | Examples |
|-----------|------------------|-------|----------|
| **Organization-Scoped** | OrganizationMembership.Role | Per organization | Admin, Caregiver |
| **Global** | ASP.NET Identity UserRoles | Platform-wide | Sysadmin |

### Organization-Scoped Roles

These roles are stored in the `OrganizationMembership.Role` field:

- **Admin:** Full control within the organization. Can manage homes, beds, clients, caregivers, documents, reports, and org-level settings. Has PHI access.
- **Caregiver:** Limited to assigned homes within the organization. Can view clients, log care activities, view permitted documents. Has PHI access.

A user can have different roles in different organizations:
- Alice: Admin in Org A, Caregiver in Org B
- Bob: Caregiver in Org A, Admin in Org C

### Global Roles

These roles remain in ASP.NET Identity and are not organization-scoped:

- **Sysadmin:** Platform administrator for system maintenance. Cannot access or modify PHI. Can view audit logs across all organizations. Does not require organization membership.

### Owner Flag

The `IsOwner` flag on OrganizationMembership indicates users who can:
- Manage organization settings
- Invite and remove users from the organization
- Transfer ownership to another admin
- Access billing and subscription settings (future)

An organization can have multiple owners for redundancy.

## Authentication and Authorization

### JWT Claims

The JWT token includes organization context:

```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "org": "organization-guid",
  "orgRole": "Admin",
  "isOwner": true,
  "globalRoles": ["Sysadmin"],
  "iat": 1735689600,
  "exp": 1735693200
}
```

| Claim | Description |
|-------|-------------|
| `org` | Currently selected organization ID |
| `orgRole` | User's role in the current organization |
| `isOwner` | Whether user is an owner of the current organization |
| `globalRoles` | Array of global roles (e.g., Sysadmin) |

### Authentication Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  1. User logs in with email/password + MFA                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. Server validates credentials                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. Server checks OrganizationMemberships for user               │
└─────────────────────────────────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  0 memberships  │  │  1 membership   │  │  2+ memberships │
│  + not Sysadmin │  │                 │  │                 │
│       ↓         │  │       ↓         │  │       ↓         │
│  Error: Orphan  │  │  Auto-select    │  │  Show picker    │
│  user           │  │  organization   │  │  UI             │
└─────────────────┘  └─────────────────┘  └─────────────────┘
                              │                    │
                              ▼                    ▼
                     ┌─────────────────────────────────────────┐
                     │  4. Issue JWT with organization context  │
                     └─────────────────────────────────────────┘
```

### Organization Switching

Users with multiple organization memberships can switch without re-authenticating:

1. User requests organization switch via `/api/auth/select-organization`
2. Server validates user has active membership in target organization
3. Server issues new JWT with new organization context
4. Frontend updates context and refreshes data

### Authorization Policies

| Policy | Checks | Usage |
|--------|--------|-------|
| `OrgMember` | User has `org` claim | Any authenticated org action |
| `OrgAdmin` | `orgRole` claim = "Admin" | Home management, user management |
| `OrgCaregiver` | `orgRole` claim = "Caregiver" | Care logging, client viewing |
| `OrgOwner` | `isOwner` claim = true | Org settings, user invitations |
| `GlobalSysadmin` | `globalRoles` contains "Sysadmin" | Platform administration |

## Data Isolation

### EF Core Global Query Filters

All tenant-scoped queries are automatically filtered by OrganizationId:

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    // Automatic organization filtering
    builder.Entity<Home>()
        .HasQueryFilter(h => h.OrganizationId == _currentOrgId);
    
    builder.Entity<Client>()
        .HasQueryFilter(c => c.OrganizationId == _currentOrgId);
    
    builder.Entity<Document>()
        .HasQueryFilter(d => d.OrganizationId == _currentOrgId);
    
    // ... applied to all tenant-scoped entities
}
```

### Current Organization Service

A scoped service provides organization context throughout the request:

```csharp
public interface ICurrentOrganizationService
{
    Guid? OrganizationId { get; }
    string? OrganizationName { get; }
    string? UserRole { get; }
    bool IsOwner { get; }
}
```

The service:
- Extracts organization from JWT claims
- Validates user has active membership
- Provides context to DbContext for query filters
- Is registered with scoped lifetime in DI

### Bypassing Filters

In rare cases (migrations, Sysadmin operations), filters must be bypassed:

```csharp
// For cross-organization queries (Sysadmin only)
var allHomes = await _db.Homes
    .IgnoreQueryFilters()
    .ToListAsync();
```

**Security Note:** Filter bypass is restricted to Sysadmin role and specific administrative operations.

## Audit Logging

### Organization Context in Audit Logs

All audit log entries include the organization context:

```json
{
  "id": "log-entry-id",
  "organizationId": "organization-guid",
  "userId": "user-guid",
  "action": "CLIENT_CREATED",
  "resourceId": "client-guid",
  "resourceType": "Client",
  "timestamp": "2026-01-02T10:30:00Z",
  "outcome": "Success",
  "ipAddress": "192.168.1.1"
}
```

### Cosmos DB Partitioning

Audit logs are partitioned by OrganizationId for:
- Efficient per-organization queries
- Data isolation at storage level
- Improved query performance

### Cross-Organization Access

Sysadmin users can query audit logs across organizations for platform-wide investigations, but this is logged separately as a privileged action.

## Security Considerations

### Defense in Depth

Multiple layers protect against cross-tenant data access:

1. **JWT Signature:** Token cannot be tampered with
2. **Membership Validation:** Server verifies user belongs to claimed organization
3. **EF Query Filters:** All database queries automatically scoped
4. **Authorization Policies:** Role checks per organization
5. **Audit Logging:** All access attempts logged with organization context

### HIPAA Compliance

Multi-tenancy maintains HIPAA compliance through:

- **Data Isolation:** Each organization's PHI is logically separated
- **Access Controls:** Users only access organizations they're members of
- **Audit Trail:** All access is logged with organization context
- **Encryption:** Data encrypted at rest and in transit (unchanged)
- **BAA Coverage:** Azure BAA covers all tenants in shared infrastructure

### Security Testing

Prior to production deployment, verify:

- [ ] Query filters cannot be bypassed by regular users
- [ ] JWT tampering is detected and rejected
- [ ] Cross-organization URLs return 403 Forbidden
- [ ] Audit logs capture all cross-organization access attempts
- [ ] Sysadmin actions are logged with elevated privilege flag

## Migration Strategy

### From Single-Tenant to Multi-Tenant

For existing LenkCare deployments:

1. **Schema Migration:** Add Organization table and OrganizationId columns
2. **Data Backfill:** Create first organization and assign all existing data
3. **Membership Creation:** Convert existing role assignments to memberships
4. **Code Deployment:** Deploy multi-tenant aware application
5. **Cleanup:** Remove legacy role assignments

See [Phase 7 - Multi-Tenancy](implementation/Phase%207%20-%20Multi-Tenancy.md) for detailed migration steps.

### Data Backfill Rules

| Existing Data | Migration Action |
|---------------|------------------|
| All Homes | Set OrganizationId to first org |
| All Clients | Set OrganizationId to first org |
| All Documents | Set OrganizationId to first org |
| Users with Admin role | Create membership (Role=Admin, IsOwner=true for first) |
| Users with Caregiver role | Create membership (Role=Caregiver) |
| Users with Sysadmin role | Keep in UserRoles (global role) |

## Future Considerations

### Out of Scope for Initial Release

- **Organization Self-Registration:** New operators creating their own organizations
- **Billing and Subscriptions:** Per-organization billing and feature tiers
- **Custom Branding:** Organization-specific logos and color themes
- **Data Residency:** Multi-region deployment with data locality requirements
- **Cross-Organization Features:** Shared users across organizations with explicit consent

### Scalability

The shared database approach scales well for:
- Hundreds of organizations
- Thousands of users
- Millions of care log entries

For extreme scale (10,000+ organizations), consider:
- Database sharding by organization
- Read replicas per region
- Caching layer for frequently accessed data

## Related Documentation

- [Architecture and Design](Architecture%20and%20Design.md) - Core system architecture
- [Phase 7 - Multi-Tenancy](implementation/Phase%207%20-%20Multi-Tenancy.md) - Implementation plan
- [AGENTS.md](../AGENTS.md) - Development guidelines
