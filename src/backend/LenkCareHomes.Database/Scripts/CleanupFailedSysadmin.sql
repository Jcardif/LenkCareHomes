/*
    Cleanup Failed Sysadmin User
    ============================
    
    Run this script to clean up any partial data from a failed sysadmin user creation,
    then re-run the SeedInitialSysadmin.sql script.
*/

DECLARE @SysadminEmail NVARCHAR(256) = 'sysadmin@lenkcarehomes.com';  -- Must match the email used in SeedInitialSysadmin.sql
DECLARE @UserId UNIQUEIDENTIFIER;

-- Find the user ID if it exists
SELECT @UserId = [Id] FROM [dbo].[Users] WHERE [NormalizedEmail] = UPPER(@SysadminEmail);

IF @UserId IS NOT NULL
BEGIN
    -- Delete role assignments first (FK constraint)
    DELETE FROM [dbo].[AspNetUserRoles] WHERE [UserId] = @UserId;
    PRINT 'Deleted role assignments for user: ' + @SysadminEmail;
    
    -- Delete the user
    DELETE FROM [dbo].[Users] WHERE [Id] = @UserId;
    PRINT 'Deleted user: ' + @SysadminEmail;
END
ELSE
BEGIN
    PRINT 'User not found: ' + @SysadminEmail;
END

PRINT '';
PRINT 'Cleanup complete. You can now re-run SeedInitialSysadmin.sql';
GO
