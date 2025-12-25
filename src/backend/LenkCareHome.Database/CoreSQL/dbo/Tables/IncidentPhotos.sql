CREATE TABLE [dbo].[IncidentPhotos] (
    [Id]            UNIQUEIDENTIFIER NOT NULL,
    [IncidentId]    UNIQUEIDENTIFIER NOT NULL,
    [BlobPath]      NVARCHAR (500)   NOT NULL,
    [FileName]      NVARCHAR (255)   NOT NULL,
    [ContentType]   NVARCHAR (100)   NOT NULL,
    [FileSizeBytes] BIGINT           NOT NULL,
    [DisplayOrder]  INT              NOT NULL,
    [Caption]       NVARCHAR (500)   NULL,
    [CreatedAt]     DATETIME2 (7)    NOT NULL,
    [CreatedById]   UNIQUEIDENTIFIER NOT NULL
);
GO

ALTER TABLE [dbo].[IncidentPhotos]
    ADD CONSTRAINT [PK_IncidentPhotos] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_IncidentPhotos_CreatedById]
    ON [dbo].[IncidentPhotos]([CreatedById] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_IncidentPhotos_IncidentId]
    ON [dbo].[IncidentPhotos]([IncidentId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_IncidentPhotos_CreatedAt]
    ON [dbo].[IncidentPhotos]([CreatedAt] ASC);
GO

ALTER TABLE [dbo].[IncidentPhotos]
    ADD CONSTRAINT [FK_IncidentPhotos_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[IncidentPhotos]
    ADD CONSTRAINT [FK_IncidentPhotos_Incidents_IncidentId] FOREIGN KEY ([IncidentId]) REFERENCES [dbo].[Incidents] ([Id]) ON DELETE CASCADE;
GO

