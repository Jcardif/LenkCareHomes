CREATE TABLE [dbo].[ADLLogs] (
    [Id]           UNIQUEIDENTIFIER NOT NULL,
    [ClientId]     UNIQUEIDENTIFIER NOT NULL,
    [CaregiverId]  UNIQUEIDENTIFIER NOT NULL,
    [Timestamp]    DATETIME2 (7)    NOT NULL,
    [Bathing]      NVARCHAR (20)    NULL,
    [Dressing]     NVARCHAR (20)    NULL,
    [Toileting]    NVARCHAR (20)    NULL,
    [Transferring] NVARCHAR (20)    NULL,
    [Continence]   NVARCHAR (20)    NULL,
    [Feeding]      NVARCHAR (20)    NULL,
    [Notes]        NVARCHAR (2000)  NULL,
    [CreatedAt]    DATETIME2 (7)    NOT NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_ADLLogs_Timestamp]
    ON [dbo].[ADLLogs]([Timestamp] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ADLLogs_ClientId]
    ON [dbo].[ADLLogs]([ClientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ADLLogs_CaregiverId]
    ON [dbo].[ADLLogs]([CaregiverId] ASC);
GO

ALTER TABLE [dbo].[ADLLogs]
    ADD CONSTRAINT [PK_ADLLogs] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

ALTER TABLE [dbo].[ADLLogs]
    ADD CONSTRAINT [FK_ADLLogs_Users_CaregiverId] FOREIGN KEY ([CaregiverId]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[ADLLogs]
    ADD CONSTRAINT [FK_ADLLogs_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]);
GO

