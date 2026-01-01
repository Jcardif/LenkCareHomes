CREATE TABLE [dbo].[VitalsLogs] (
    [Id]               UNIQUEIDENTIFIER NOT NULL,
    [ClientId]         UNIQUEIDENTIFIER NOT NULL,
    [CaregiverId]      UNIQUEIDENTIFIER NOT NULL,
    [Timestamp]        DATETIME2 (7)    NOT NULL,
    [SystolicBP]       INT              NULL,
    [DiastolicBP]      INT              NULL,
    [Pulse]            INT              NULL,
    [Temperature]      DECIMAL (5, 2)   NULL,
    [TemperatureUnit]  NVARCHAR (20)    DEFAULT (N'Fahrenheit') NOT NULL,
    [OxygenSaturation] INT              NULL,
    [Notes]            NVARCHAR (2000)  NULL,
    [CreatedAt]        DATETIME2 (7)    NOT NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_VitalsLogs_ClientId]
    ON [dbo].[VitalsLogs]([ClientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_VitalsLogs_CaregiverId]
    ON [dbo].[VitalsLogs]([CaregiverId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_VitalsLogs_Timestamp]
    ON [dbo].[VitalsLogs]([Timestamp] ASC);
GO

ALTER TABLE [dbo].[VitalsLogs]
    ADD CONSTRAINT [FK_VitalsLogs_Users_CaregiverId] FOREIGN KEY ([CaregiverId]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[VitalsLogs]
    ADD CONSTRAINT [FK_VitalsLogs_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]);
GO

ALTER TABLE [dbo].[VitalsLogs]
    ADD CONSTRAINT [PK_VitalsLogs] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

