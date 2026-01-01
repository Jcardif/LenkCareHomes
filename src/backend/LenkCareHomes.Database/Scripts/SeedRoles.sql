/*
    Seed Application Roles for Production
    ======================================
    
    This script creates the three application roles required by LenkCare Homes.
    Run this script BEFORE SeedInitialSysadmin.sql to ensure roles exist.
    
    Roles Created:
    - Admin: Full system access, can manage homes, beds, clients, caregivers, 
             documents, reports, and audit logs. Has PHI access.
    - Caregiver: Limited home-scoped access, can view clients, log ADLs/vitals/notes,
                 view permitted documents (no download). Has PHI access.
    - Sysadmin: System maintenance only. Cannot access or modify PHI - only 
                system configuration and audit logs.
    
    IMPORTANT:
    - This script is idempotent - safe to run multiple times
    - Run this ONCE after initial database deployment
    - Run BEFORE creating any users
*/

DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

-- Seed Admin role
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [NormalizedName] = 'ADMIN')
BEGIN
    INSERT INTO [dbo].[Roles] (
        [Id],
        [Name],
        [NormalizedName],
        [Description],
        [HasPhiAccess],
        [CreatedAt],
        [ConcurrencyStamp]
    )
    VALUES (
        NEWID(),
        'Admin',
        'ADMIN',
        'Administrator with full system access. Can manage homes, beds, clients, caregivers, documents, reports, and audit logs.',
        1,  -- HasPhiAccess = true
        @Now,
        CONVERT(NVARCHAR(MAX), NEWID())
    );
    
    PRINT 'Created Admin role';
END
ELSE
BEGIN
    PRINT 'Admin role already exists';
END

-- Seed Caregiver role
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [NormalizedName] = 'CAREGIVER')
BEGIN
    INSERT INTO [dbo].[Roles] (
        [Id],
        [Name],
        [NormalizedName],
        [Description],
        [HasPhiAccess],
        [CreatedAt],
        [ConcurrencyStamp]
    )
    VALUES (
        NEWID(),
        'Caregiver',
        'CAREGIVER',
        'Caregiver with limited home-scoped access. Can view clients, log ADLs/vitals/notes, view permitted documents (no download).',
        1,  -- HasPhiAccess = true
        @Now,
        CONVERT(NVARCHAR(MAX), NEWID())
    );
    
    PRINT 'Created Caregiver role';
END
ELSE
BEGIN
    PRINT 'Caregiver role already exists';
END

-- Seed Sysadmin role
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [NormalizedName] = 'SYSADMIN')
BEGIN
    INSERT INTO [dbo].[Roles] (
        [Id],
        [Name],
        [NormalizedName],
        [Description],
        [HasPhiAccess],
        [CreatedAt],
        [ConcurrencyStamp]
    )
    VALUES (
        NEWID(),
        'Sysadmin',
        'SYSADMIN',
        'System administrator for maintenance only. Cannot access or modify PHI - only system configuration and audit logs.',
        0,  -- HasPhiAccess = false
        @Now,
        CONVERT(NVARCHAR(MAX), NEWID())
    );
    
    PRINT 'Created Sysadmin role';
END
ELSE
BEGIN
    PRINT 'Sysadmin role already exists';
END

PRINT '';
PRINT '=== ROLE SEEDING COMPLETE ===';
PRINT 'All application roles are now available.';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. Run SeedInitialSysadmin.sql to create the initial sysadmin user';
GO
