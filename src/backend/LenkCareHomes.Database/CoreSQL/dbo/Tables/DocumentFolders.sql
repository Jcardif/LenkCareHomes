CREATE TABLE [dbo].[DocumentFolders] (
    [Id]             UNIQUEIDENTIFIER NOT NULL,
    [Name]           NVARCHAR (200)   NOT NULL,
    [Description]    NVARCHAR (1000)  NULL,
    [Scope]          NVARCHAR (20)    NOT NULL,
    [ParentFolderId] UNIQUEIDENTIFIER NULL,
    [ClientId]       UNIQUEIDENTIFIER NULL,
    [HomeId]         UNIQUEIDENTIFIER NULL,
    [IsSystemFolder] BIT              NOT NULL,
    [IsActive]       BIT              NOT NULL,
    [CreatedById]    UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt]      DATETIME2 (7)    NOT NULL,
    [UpdatedAt]      DATETIME2 (7)    NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentFolders_ClientId]
    ON [dbo].[DocumentFolders]([ClientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentFolders_ParentFolderId]
    ON [dbo].[DocumentFolders]([ParentFolderId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentFolders_CreatedById]
    ON [dbo].[DocumentFolders]([CreatedById] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_DocumentFolders_ParentFolderId_Name_Scope_ClientId_HomeId]
    ON [dbo].[DocumentFolders]([ParentFolderId] ASC, [Name] ASC, [Scope] ASC, [ClientId] ASC, [HomeId] ASC) WHERE ([IsActive]=(1));
GO

CREATE NONCLUSTERED INDEX [IX_DocumentFolders_HomeId]
    ON [dbo].[DocumentFolders]([HomeId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentFolders_IsActive]
    ON [dbo].[DocumentFolders]([IsActive] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentFolders_Scope]
    ON [dbo].[DocumentFolders]([Scope] ASC);
GO

ALTER TABLE [dbo].[DocumentFolders]
    ADD CONSTRAINT [FK_DocumentFolders_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]);
GO

ALTER TABLE [dbo].[DocumentFolders]
    ADD CONSTRAINT [FK_DocumentFolders_DocumentFolders_ParentFolderId] FOREIGN KEY ([ParentFolderId]) REFERENCES [dbo].[DocumentFolders] ([Id]);
GO

ALTER TABLE [dbo].[DocumentFolders]
    ADD CONSTRAINT [FK_DocumentFolders_Homes_HomeId] FOREIGN KEY ([HomeId]) REFERENCES [dbo].[Homes] ([Id]);
GO

ALTER TABLE [dbo].[DocumentFolders]
    ADD CONSTRAINT [FK_DocumentFolders_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[DocumentFolders]
    ADD CONSTRAINT [PK_DocumentFolders] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

