
BEGIN TRANSACTION;

-- ============================================================================
-- DELETE DATA (in dependency order - children first, then parents)
-- ============================================================================

-- 1. Delete care logs (depend on Clients + Users)
DELETE FROM [dbo].[ADLLogs];
DELETE FROM [dbo].[VitalsLogs];
DELETE FROM [dbo].[MedicationLogs];
DELETE FROM [dbo].[ROMLogs];
DELETE FROM [dbo].[BehaviorNotes];

-- 2. Delete activity-related tables
DELETE FROM [dbo].[ActivityParticipants];  -- FK to Activities + Clients
DELETE FROM [dbo].[Activities];            -- FK to Homes + Users

-- 3. Delete incident-related tables
DELETE FROM [dbo].[IncidentFollowUps];     -- FK to Incidents + Users
DELETE FROM [dbo].[IncidentPhotos];        -- FK to Incidents + Users
DELETE FROM [dbo].[Incidents];             -- FK to Clients + Homes + Users

-- 3a. Delete appointments
DELETE FROM [dbo].[Appointments];          -- FK to Clients + Homes + Users

-- 4. Delete document-related tables
DELETE FROM [dbo].[DocumentAccessHistory];     -- FK to Documents + Users
DELETE FROM [dbo].[DocumentAccessPermissions]; -- FK to Documents + Users
DELETE FROM [dbo].[Documents];                 -- FK to DocumentFolders + Clients + Homes + Users
DELETE FROM [dbo].[DocumentFolders];           -- FK to Clients + Homes + Users (self-referencing)

-- 5. Delete clients (depend on Homes + Beds + Users)
DELETE FROM [dbo].[Clients];

-- 6. Delete home-related tables (depend on Homes + Users)
DELETE FROM [dbo].[CaregiverHomeAssignments];  -- FK to Homes + Users
DELETE FROM [dbo].[Beds];                      -- FK to Homes

-- 7. Delete ASP.NET Identity child tables and passkeys (depend on Users + Roles)
DELETE FROM [dbo].[UserPasskeys];          -- FK to Users
DELETE FROM [dbo].[AspNetUserTokens];
DELETE FROM [dbo].[AspNetUserLogins];
DELETE FROM [dbo].[AspNetUserRoles];
DELETE FROM [dbo].[AspNetUserClaims];
DELETE FROM [dbo].[AspNetRoleClaims];

-- 8. Delete parent tables
DELETE FROM [dbo].[Homes];
DELETE FROM [dbo].[Users];
DELETE FROM [dbo].[Roles];

-- 9. Delete Migration history
DELETE FROM [dbo].[__EFMigrationsHistory];

COMMIT TRANSACTION;


-- ============================================================================
-- DROP TABLES (in dependency order - children first, then parents)
-- ============================================================================

-- Care logs
IF OBJECT_ID(N'dbo.ADLLogs', N'U') IS NOT NULL DROP TABLE [dbo].[ADLLogs];
IF OBJECT_ID(N'dbo.VitalsLogs', N'U') IS NOT NULL DROP TABLE [dbo].[VitalsLogs];
IF OBJECT_ID(N'dbo.MedicationLogs', N'U') IS NOT NULL DROP TABLE [dbo].[MedicationLogs];
IF OBJECT_ID(N'dbo.ROMLogs', N'U') IS NOT NULL DROP TABLE [dbo].[ROMLogs];
IF OBJECT_ID(N'dbo.BehaviorNotes', N'U') IS NOT NULL DROP TABLE [dbo].[BehaviorNotes];

-- Activities
IF OBJECT_ID(N'dbo.ActivityParticipants', N'U') IS NOT NULL DROP TABLE [dbo].[ActivityParticipants];
IF OBJECT_ID(N'dbo.Activities', N'U') IS NOT NULL DROP TABLE [dbo].[Activities];

-- Incidents
IF OBJECT_ID(N'dbo.IncidentFollowUps', N'U') IS NOT NULL DROP TABLE [dbo].[IncidentFollowUps];
IF OBJECT_ID(N'dbo.IncidentPhotos', N'U') IS NOT NULL DROP TABLE [dbo].[IncidentPhotos];
IF OBJECT_ID(N'dbo.Incidents', N'U') IS NOT NULL DROP TABLE [dbo].[Incidents];

-- Appointments
IF OBJECT_ID(N'dbo.Appointments', N'U') IS NOT NULL DROP TABLE [dbo].[Appointments];

-- Documents
IF OBJECT_ID(N'dbo.DocumentAccessHistory', N'U') IS NOT NULL DROP TABLE [dbo].[DocumentAccessHistory];
IF OBJECT_ID(N'dbo.DocumentAccessPermissions', N'U') IS NOT NULL DROP TABLE [dbo].[DocumentAccessPermissions];
IF OBJECT_ID(N'dbo.Documents', N'U') IS NOT NULL DROP TABLE [dbo].[Documents];
IF OBJECT_ID(N'dbo.DocumentFolders', N'U') IS NOT NULL DROP TABLE [dbo].[DocumentFolders];

-- Clients
IF OBJECT_ID(N'dbo.Clients', N'U') IS NOT NULL DROP TABLE [dbo].[Clients];

-- Homes and assignments
IF OBJECT_ID(N'dbo.CaregiverHomeAssignments', N'U') IS NOT NULL DROP TABLE [dbo].[CaregiverHomeAssignments];
IF OBJECT_ID(N'dbo.Beds', N'U') IS NOT NULL DROP TABLE [dbo].[Beds];

-- Identity tables and passkeys
IF OBJECT_ID(N'dbo.UserPasskeys', N'U') IS NOT NULL DROP TABLE [dbo].[UserPasskeys];
IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserTokens];
IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserLogins];
IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserRoles];
IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserClaims];
IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetRoleClaims];

-- Parent tables
IF OBJECT_ID(N'dbo.Homes', N'U') IS NOT NULL DROP TABLE [dbo].[Homes];
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE [dbo].[Users];
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE [dbo].[Roles];

-- Migration history
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL DROP TABLE [dbo].[__EFMigrationsHistory];