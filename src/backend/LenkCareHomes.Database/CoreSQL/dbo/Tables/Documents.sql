CREATE TABLE [dbo].[Documents] (
    [Id]               UNIQUEIDENTIFIER NOT NULL,
    [Scope]            NVARCHAR (20)    DEFAULT (N'Client') NOT NULL,
    [ClientId]         UNIQUEIDENTIFIER NULL,
    [HomeId]           UNIQUEIDENTIFIER NULL,
    [FolderId]         UNIQUEIDENTIFIER NULL,
    [FileName]         NVARCHAR (255)   NOT NULL,
    [OriginalFileName] NVARCHAR (255)   NOT NULL,
    [ContentType]      NVARCHAR (100)   NOT NULL,
    [FileSizeBytes]    BIGINT           NOT NULL,
    [DocumentType]     NVARCHAR (30)    NOT NULL,
    [Description]      NVARCHAR (1000)  NULL,
    [BlobPath]         NVARCHAR (500)   NOT NULL,
    [UploadedById]     UNIQUEIDENTIFIER NOT NULL,
    [UploadedAt]       DATETIME2 (7)    NOT NULL,
    [IsActive]         BIT              NOT NULL,
    [CreatedAt]        DATETIME2 (7)    NOT NULL,
    [UpdatedAt]        DATETIME2 (7)    NULL
);
GO

ALTER TABLE [dbo].[Documents]
    ADD CONSTRAINT [PK_Documents] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Documents_DocumentType]
    ON [dbo].[Documents]([DocumentType] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Documents_ClientId]
    ON [dbo].[Documents]([ClientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Documents_FolderId]
    ON [dbo].[Documents]([FolderId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Documents_UploadedById]
    ON [dbo].[Documents]([UploadedById] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Documents_HomeId]
    ON [dbo].[Documents]([HomeId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Documents_IsActive]
    ON [dbo].[Documents]([IsActive] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Documents_Scope]
    ON [dbo].[Documents]([Scope] ASC);
GO

ALTER TABLE [dbo].[Documents]
    ADD CONSTRAINT [FK_Documents_DocumentFolders_FolderId] FOREIGN KEY ([FolderId]) REFERENCES [dbo].[DocumentFolders] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [dbo].[Documents]
    ADD CONSTRAINT [FK_Documents_Users_UploadedById] FOREIGN KEY ([UploadedById]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[Documents]
    ADD CONSTRAINT [FK_Documents_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]);
GO

ALTER TABLE [dbo].[Documents]
    ADD CONSTRAINT [FK_Documents_Homes_HomeId] FOREIGN KEY ([HomeId]) REFERENCES [dbo].[Homes] ([Id]);
GO

