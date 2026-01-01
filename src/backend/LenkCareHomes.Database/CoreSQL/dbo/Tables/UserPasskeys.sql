CREATE TABLE [dbo].[UserPasskeys] (
    [Id]               UNIQUEIDENTIFIER NOT NULL,
    [UserId]           UNIQUEIDENTIFIER NOT NULL,
    [CredentialId]     NVARCHAR (500)   NOT NULL,
    [PublicKey]        NVARCHAR (2000)  NOT NULL,
    [SignatureCounter] BIGINT           NOT NULL,
    [AaGuid]           NVARCHAR (100)   NULL,
    [DeviceName]       NVARCHAR (200)   NOT NULL,
    [CredentialType]   NVARCHAR (50)    NOT NULL,
    [Transports]       NVARCHAR (200)   NULL,
    [CreatedAt]        DATETIME2 (7)    NOT NULL,
    [LastUsedAt]       DATETIME2 (7)    NULL,
    [IsActive]         BIT              NOT NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_UserPasskeys_UserId_IsActive]
    ON [dbo].[UserPasskeys]([UserId] ASC, [IsActive] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_UserPasskeys_CredentialId]
    ON [dbo].[UserPasskeys]([CredentialId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_UserPasskeys_UserId]
    ON [dbo].[UserPasskeys]([UserId] ASC);
GO

ALTER TABLE [dbo].[UserPasskeys]
    ADD CONSTRAINT [PK_UserPasskeys] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

ALTER TABLE [dbo].[UserPasskeys]
    ADD CONSTRAINT [FK_UserPasskeys_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE;
GO

