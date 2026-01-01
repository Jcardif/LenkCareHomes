CREATE TABLE [dbo].[MedicationLogs] (
    [Id]             UNIQUEIDENTIFIER NOT NULL,
    [ClientId]       UNIQUEIDENTIFIER NOT NULL,
    [CaregiverId]    UNIQUEIDENTIFIER NOT NULL,
    [Timestamp]      DATETIME2 (7)    NOT NULL,
    [MedicationName] NVARCHAR (200)   NOT NULL,
    [Dosage]         NVARCHAR (100)   NOT NULL,
    [Route]          NVARCHAR (20)    NOT NULL,
    [Status]         NVARCHAR (20)    NOT NULL,
    [ScheduledTime]  DATETIME2 (7)    NULL,
    [PrescribedBy]   NVARCHAR (200)   NULL,
    [Pharmacy]       NVARCHAR (200)   NULL,
    [RxNumber]       NVARCHAR (50)    NULL,
    [Notes]          NVARCHAR (2000)  NULL,
    [CreatedAt]      DATETIME2 (7)    NOT NULL
);
GO

ALTER TABLE [dbo].[MedicationLogs]
    ADD CONSTRAINT [FK_MedicationLogs_Users_CaregiverId] FOREIGN KEY ([CaregiverId]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[MedicationLogs]
    ADD CONSTRAINT [FK_MedicationLogs_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]);
GO

CREATE NONCLUSTERED INDEX [IX_MedicationLogs_ClientId]
    ON [dbo].[MedicationLogs]([ClientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_MedicationLogs_CaregiverId]
    ON [dbo].[MedicationLogs]([CaregiverId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_MedicationLogs_Timestamp]
    ON [dbo].[MedicationLogs]([Timestamp] ASC);
GO

ALTER TABLE [dbo].[MedicationLogs]
    ADD CONSTRAINT [PK_MedicationLogs] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

