CREATE TABLE [dbo].[Appointments] (
    [Id]                  UNIQUEIDENTIFIER NOT NULL,
    [ClientId]            UNIQUEIDENTIFIER NOT NULL,
    [HomeId]              UNIQUEIDENTIFIER NOT NULL,
    [AppointmentType]     NVARCHAR (30)    NOT NULL,
    [Status]              NVARCHAR (20)    DEFAULT (N'Scheduled') NOT NULL,
    [Title]               NVARCHAR (200)   NOT NULL,
    [ScheduledAt]         DATETIME2 (7)    NOT NULL,
    [DurationMinutes]     INT              NULL,
    [Location]            NVARCHAR (300)   NULL,
    [ProviderName]        NVARCHAR (200)   NULL,
    [ProviderPhone]       NVARCHAR (20)    NULL,
    [Notes]               NVARCHAR (2000)  NULL,
    [TransportationNotes] NVARCHAR (500)   NULL,
    [ReminderSent]        BIT              NOT NULL,
    [CreatedById]         UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt]           DATETIME2 (7)    NOT NULL,
    [UpdatedAt]           DATETIME2 (7)    NULL,
    [OutcomeNotes]        NVARCHAR (2000)  NULL,
    [CompletedById]       UNIQUEIDENTIFIER NULL,
    [CompletedAt]         DATETIME2 (7)    NULL
);
GO

ALTER TABLE [dbo].[Appointments]
    ADD CONSTRAINT [FK_Appointments_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]);
GO

ALTER TABLE [dbo].[Appointments]
    ADD CONSTRAINT [FK_Appointments_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[Appointments]
    ADD CONSTRAINT [FK_Appointments_Users_CompletedById] FOREIGN KEY ([CompletedById]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[Appointments]
    ADD CONSTRAINT [FK_Appointments_Homes_HomeId] FOREIGN KEY ([HomeId]) REFERENCES [dbo].[Homes] ([Id]);
GO

CREATE NONCLUSTERED INDEX [IX_Appointments_HomeId_ScheduledAt]
    ON [dbo].[Appointments]([HomeId] ASC, [ScheduledAt] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Appointments_HomeId]
    ON [dbo].[Appointments]([HomeId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Appointments_Status]
    ON [dbo].[Appointments]([Status] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Appointments_ScheduledAt]
    ON [dbo].[Appointments]([ScheduledAt] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Appointments_ClientId]
    ON [dbo].[Appointments]([ClientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Appointments_AppointmentType]
    ON [dbo].[Appointments]([AppointmentType] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Appointments_CompletedById]
    ON [dbo].[Appointments]([CompletedById] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Appointments_CreatedById]
    ON [dbo].[Appointments]([CreatedById] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Appointments_ClientId_ScheduledAt]
    ON [dbo].[Appointments]([ClientId] ASC, [ScheduledAt] ASC);
GO

ALTER TABLE [dbo].[Appointments]
    ADD CONSTRAINT [PK_Appointments] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

