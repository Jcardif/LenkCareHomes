CREATE TABLE [dbo].[DocumentAccessPermissions] (
    [Id]          UNIQUEIDENTIFIER NOT NULL,
    [DocumentId]  UNIQUEIDENTIFIER NOT NULL,
    [CaregiverId] UNIQUEIDENTIFIER NOT NULL,
    [GrantedById] UNIQUEIDENTIFIER NOT NULL,
    [GrantedDate] DATETIME2 (7)    NOT NULL
);
GO

ALTER TABLE [dbo].[DocumentAccessPermissions]
    ADD CONSTRAINT [PK_DocumentAccessPermissions] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_DocumentAccessPermissions_DocumentId_CaregiverId]
    ON [dbo].[DocumentAccessPermissions]([DocumentId] ASC, [CaregiverId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentAccessPermissions_CaregiverId]
    ON [dbo].[DocumentAccessPermissions]([CaregiverId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentAccessPermissions_GrantedById]
    ON [dbo].[DocumentAccessPermissions]([GrantedById] ASC);
GO

ALTER TABLE [dbo].[DocumentAccessPermissions]
    ADD CONSTRAINT [FK_DocumentAccessPermissions_Users_CaregiverId] FOREIGN KEY ([CaregiverId]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[DocumentAccessPermissions]
    ADD CONSTRAINT [FK_DocumentAccessPermissions_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[Documents] ([Id]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[DocumentAccessPermissions]
    ADD CONSTRAINT [FK_DocumentAccessPermissions_Users_GrantedById] FOREIGN KEY ([GrantedById]) REFERENCES [dbo].[Users] ([Id]);
GO

