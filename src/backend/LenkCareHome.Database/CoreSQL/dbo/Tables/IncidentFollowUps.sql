CREATE TABLE [dbo].[IncidentFollowUps] (
    [Id]          UNIQUEIDENTIFIER NOT NULL,
    [IncidentId]  UNIQUEIDENTIFIER NOT NULL,
    [CreatedById] UNIQUEIDENTIFIER NOT NULL,
    [Note]        NVARCHAR (4000)  NOT NULL,
    [CreatedAt]   DATETIME2 (7)    NOT NULL
);
GO

ALTER TABLE [dbo].[IncidentFollowUps]
    ADD CONSTRAINT [FK_IncidentFollowUps_Incidents_IncidentId] FOREIGN KEY ([IncidentId]) REFERENCES [dbo].[Incidents] ([Id]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[IncidentFollowUps]
    ADD CONSTRAINT [FK_IncidentFollowUps_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[IncidentFollowUps]
    ADD CONSTRAINT [PK_IncidentFollowUps] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_IncidentFollowUps_CreatedAt]
    ON [dbo].[IncidentFollowUps]([CreatedAt] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_IncidentFollowUps_CreatedById]
    ON [dbo].[IncidentFollowUps]([CreatedById] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_IncidentFollowUps_IncidentId]
    ON [dbo].[IncidentFollowUps]([IncidentId] ASC);
GO

