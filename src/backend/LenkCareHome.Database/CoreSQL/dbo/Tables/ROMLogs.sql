CREATE TABLE [dbo].[ROMLogs] (
    [Id]                  UNIQUEIDENTIFIER NOT NULL,
    [ClientId]            UNIQUEIDENTIFIER NOT NULL,
    [CaregiverId]         UNIQUEIDENTIFIER NOT NULL,
    [Timestamp]           DATETIME2 (7)    NOT NULL,
    [ActivityDescription] NVARCHAR (200)   NOT NULL,
    [Duration]            INT              NULL,
    [Repetitions]         INT              NULL,
    [Notes]               NVARCHAR (2000)  NULL,
    [CreatedAt]           DATETIME2 (7)    NOT NULL
);
GO

ALTER TABLE [dbo].[ROMLogs]
    ADD CONSTRAINT [FK_ROMLogs_Users_CaregiverId] FOREIGN KEY ([CaregiverId]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[ROMLogs]
    ADD CONSTRAINT [FK_ROMLogs_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]);
GO

CREATE NONCLUSTERED INDEX [IX_ROMLogs_ClientId]
    ON [dbo].[ROMLogs]([ClientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ROMLogs_Timestamp]
    ON [dbo].[ROMLogs]([Timestamp] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ROMLogs_CaregiverId]
    ON [dbo].[ROMLogs]([CaregiverId] ASC);
GO

ALTER TABLE [dbo].[ROMLogs]
    ADD CONSTRAINT [PK_ROMLogs] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

