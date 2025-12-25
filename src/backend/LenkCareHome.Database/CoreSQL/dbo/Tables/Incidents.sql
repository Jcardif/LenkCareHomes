CREATE TABLE [dbo].[Incidents] (
    [Id]              UNIQUEIDENTIFIER NOT NULL,
    [IncidentNumber]  NVARCHAR (50)    NOT NULL,
    [ClientId]        UNIQUEIDENTIFIER NULL,
    [HomeId]          UNIQUEIDENTIFIER NOT NULL,
    [ReportedById]    UNIQUEIDENTIFIER NOT NULL,
    [IncidentType]    NVARCHAR (30)    NOT NULL,
    [Severity]        INT              NOT NULL,
    [OccurredAt]      DATETIME2 (7)    NOT NULL,
    [Location]        NVARCHAR (200)   NOT NULL,
    [Description]     NVARCHAR (4000)  NOT NULL,
    [ActionsTaken]    NVARCHAR (2000)  NULL,
    [WitnessNames]    NVARCHAR (500)   NULL,
    [NotifiedParties] NVARCHAR (500)   NULL,
    [AdminNotifiedAt] DATETIME2 (7)    NULL,
    [Status]          NVARCHAR (20)    DEFAULT (N'Draft') NOT NULL,
    [ClosedById]      UNIQUEIDENTIFIER NULL,
    [ClosedAt]        DATETIME2 (7)    NULL,
    [ClosureNotes]    NVARCHAR (2000)  NULL,
    [CreatedAt]       DATETIME2 (7)    NOT NULL,
    [UpdatedAt]       DATETIME2 (7)    NULL
);
GO

ALTER TABLE [dbo].[Incidents]
    ADD CONSTRAINT [FK_Incidents_Users_ReportedById] FOREIGN KEY ([ReportedById]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[Incidents]
    ADD CONSTRAINT [FK_Incidents_Homes_HomeId] FOREIGN KEY ([HomeId]) REFERENCES [dbo].[Homes] ([Id]);
GO

ALTER TABLE [dbo].[Incidents]
    ADD CONSTRAINT [FK_Incidents_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [dbo].[Incidents]
    ADD CONSTRAINT [FK_Incidents_Users_ClosedById] FOREIGN KEY ([ClosedById]) REFERENCES [dbo].[Users] ([Id]);
GO

CREATE NONCLUSTERED INDEX [IX_Incidents_ReportedById]
    ON [dbo].[Incidents]([ReportedById] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Incidents_Severity]
    ON [dbo].[Incidents]([Severity] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Incidents_OccurredAt]
    ON [dbo].[Incidents]([OccurredAt] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Incidents_IncidentNumber]
    ON [dbo].[Incidents]([IncidentNumber] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Incidents_HomeId]
    ON [dbo].[Incidents]([HomeId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Incidents_Status]
    ON [dbo].[Incidents]([Status] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Incidents_ClosedById]
    ON [dbo].[Incidents]([ClosedById] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Incidents_ClientId]
    ON [dbo].[Incidents]([ClientId] ASC);
GO

ALTER TABLE [dbo].[Incidents]
    ADD CONSTRAINT [PK_Incidents] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

