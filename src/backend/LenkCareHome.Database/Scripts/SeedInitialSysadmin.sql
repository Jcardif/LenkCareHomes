/*
    Seed Initial Sysadmin User for Production
    ==========================================
    
    This script creates the initial Sysadmin user who can then log in and create
    Admin users through the application. The Sysadmin role has NO PHI access -
    they can only manage system configuration and create other users.
    
    IMPORTANT: 
    - Run this script ONCE after initial database deployment
    - The user will need to reset their password on first login
    - After creating an Admin user, consider disabling this Sysadmin account
    
    BEFORE RUNNING:
    1. Generate a password hash using ASP.NET Core Identity (see below)
    2. Replace the placeholder values for email and password hash
    
    To generate a password hash, you can use this C# code:
    
        var hasher = new PasswordHasher<object>();
        var hash = hasher.HashPassword(null, "YourSecurePassword123!");
        Console.WriteLine(hash);
    
    Or use the Azure Cloud Shell / local .NET CLI:
    
        dotnet new console -n HashGenerator
        cd HashGenerator
        dotnet add package Microsoft.AspNetCore.Identity
        // Add code to Program.cs and run
*/

-- Configuration variables (REPLACE THESE VALUES)
DECLARE @SysadminEmail NVARCHAR(256) = 'sysadmin@lenkcarehomes.com';  -- Change this
DECLARE @SysadminFirstName NVARCHAR(100) = 'System';
DECLARE @SysadminLastName NVARCHAR(100) = 'Administrator';
DECLARE @PasswordHash NVARCHAR(MAX) = 'REPLACE_WITH_GENERATED_HASH';  -- Generate using PasswordHasher<T>

-- Generate new GUIDs
DECLARE @UserId UNIQUEIDENTIFIER = NEWID();
DECLARE @RoleId UNIQUEIDENTIFIER;
DECLARE @SecurityStamp NVARCHAR(MAX) = CONVERT(NVARCHAR(MAX), NEWID());
DECLARE @ConcurrencyStamp NVARCHAR(MAX) = CONVERT(NVARCHAR(MAX), NEWID());
DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

-- Ensure the Sysadmin role exists
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [NormalizedName] = 'SYSADMIN')
BEGIN
    SET @RoleId = NEWID();
    
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
        @RoleId,
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
    SELECT @RoleId = [Id] FROM [dbo].[Roles] WHERE [NormalizedName] = 'SYSADMIN';
    PRINT 'Sysadmin role already exists';
END

-- Check if sysadmin user already exists
IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [NormalizedEmail] = UPPER(@SysadminEmail))
BEGIN
    PRINT 'ERROR: User with email ' + @SysadminEmail + ' already exists. Aborting.';
    RETURN;
END

-- Validate password hash is set
IF @PasswordHash = 'REPLACE_WITH_GENERATED_HASH'
BEGIN
    PRINT 'ERROR: You must generate and set a password hash before running this script.';
    PRINT 'See the comments at the top of this script for instructions.';
    RETURN;
END

-- Create the sysadmin user
INSERT INTO [dbo].[Users] (
    [Id],
    [UserName],
    [NormalizedUserName],
    [Email],
    [NormalizedEmail],
    [EmailConfirmed],
    [PasswordHash],
    [SecurityStamp],
    [ConcurrencyStamp],
    [PhoneNumber],
    [PhoneNumberConfirmed],
    [TwoFactorEnabled],
    [LockoutEnd],
    [LockoutEnabled],
    [AccessFailedCount],
    [FirstName],
    [LastName],
    [IsMfaSetupComplete],
    [BackupCodesHash],
    [RemainingBackupCodes],
    [CreatedAt],
    [UpdatedAt],
    [IsActive],
    [InvitationToken],
    [InvitationExpiresAt],
    [InvitationAccepted],
    [TourCompleted]
)
VALUES (
    @UserId,
    @SysadminEmail,                    -- UserName
    UPPER(@SysadminEmail),             -- NormalizedUserName
    @SysadminEmail,                    -- Email
    UPPER(@SysadminEmail),             -- NormalizedEmail
    1,                                 -- EmailConfirmed = true
    @PasswordHash,                     -- PasswordHash (generated externally)
    @SecurityStamp,                    -- SecurityStamp
    @ConcurrencyStamp,                 -- ConcurrencyStamp
    NULL,                              -- PhoneNumber
    0,                                 -- PhoneNumberConfirmed
    0,                                 -- TwoFactorEnabled (will set up MFA on first login)
    NULL,                              -- LockoutEnd
    0,                                 -- LockoutEnabled = false (no lockout per requirements)
    0,                                 -- AccessFailedCount
    @SysadminFirstName,                -- FirstName
    @SysadminLastName,                 -- LastName
    0,                                 -- IsMfaSetupComplete = false (must set up on first login)
    NULL,                              -- BackupCodesHash
    0,                                 -- RemainingBackupCodes
    @Now,                              -- CreatedAt
    NULL,                              -- UpdatedAt
    1,                                 -- IsActive = true
    NULL,                              -- InvitationToken
    NULL,                              -- InvitationExpiresAt
    1,                                 -- InvitationAccepted = true (no invitation needed)
    0                                  -- TourCompleted = false
);

PRINT 'Created sysadmin user: ' + @SysadminEmail;

-- Assign the Sysadmin role to the user
INSERT INTO [dbo].[AspNetUserRoles] ([UserId], [RoleId])
VALUES (@UserId, @RoleId);

PRINT 'Assigned Sysadmin role to user';
PRINT '';
PRINT '=== SETUP COMPLETE ===';
PRINT 'Sysadmin user created successfully.';
PRINT 'Email: ' + @SysadminEmail;
PRINT 'User ID: ' + CONVERT(NVARCHAR(50), @UserId);
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. Log in with the sysadmin credentials';
PRINT '2. Complete MFA setup';
PRINT '3. Create an Admin user through the application';
PRINT '4. Consider disabling this sysadmin account after Admin is created';
GO
