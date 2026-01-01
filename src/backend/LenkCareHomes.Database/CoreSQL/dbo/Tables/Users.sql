CREATE TABLE [dbo].[Users] (
    [Id]                   UNIQUEIDENTIFIER   NOT NULL,
    [FirstName]            NVARCHAR (100)     NOT NULL,
    [LastName]             NVARCHAR (100)     NOT NULL,
    [IsMfaSetupComplete]   BIT                NOT NULL,
    [RequiresPasskeySetup] BIT                DEFAULT (CONVERT([bit],(0))) NOT NULL,
    [BackupCodesHash]      NVARCHAR (2000)    NULL,
    [RemainingBackupCodes] INT                NOT NULL,
    [CreatedAt]            DATETIME2 (7)      NOT NULL,
    [UpdatedAt]            DATETIME2 (7)      NULL,
    [IsActive]             BIT                NOT NULL,
    [InvitationToken]      NVARCHAR (500)     NULL,
    [InvitationExpiresAt]  DATETIME2 (7)      NULL,
    [InvitationAccepted]   BIT                NOT NULL,
    [TourCompleted]        BIT                NOT NULL,
    [UserName]             NVARCHAR (256)     NULL,
    [NormalizedUserName]   NVARCHAR (256)     NULL,
    [Email]                NVARCHAR (256)     NULL,
    [NormalizedEmail]      NVARCHAR (256)     NULL,
    [EmailConfirmed]       BIT                NOT NULL,
    [PasswordHash]         NVARCHAR (MAX)     NULL,
    [SecurityStamp]        NVARCHAR (MAX)     NULL,
    [ConcurrencyStamp]     NVARCHAR (MAX)     NULL,
    [PhoneNumber]          NVARCHAR (MAX)     NULL,
    [PhoneNumberConfirmed] BIT                NOT NULL,
    [TwoFactorEnabled]     BIT                NOT NULL,
    [LockoutEnd]           DATETIMEOFFSET (7) NULL,
    [LockoutEnabled]       BIT                NOT NULL,
    [AccessFailedCount]    INT                NOT NULL
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email]
    ON [dbo].[Users]([Email] ASC) WHERE ([Email] IS NOT NULL);
GO

CREATE NONCLUSTERED INDEX [EmailIndex]
    ON [dbo].[Users]([NormalizedEmail] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Users_InvitationToken]
    ON [dbo].[Users]([InvitationToken] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex]
    ON [dbo].[Users]([NormalizedUserName] ASC) WHERE ([NormalizedUserName] IS NOT NULL);
GO

ALTER TABLE [dbo].[Users]
    ADD CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

