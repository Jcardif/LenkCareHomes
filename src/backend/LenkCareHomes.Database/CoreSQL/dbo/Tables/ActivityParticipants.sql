CREATE TABLE [dbo].[ActivityParticipants] (
    [Id]         UNIQUEIDENTIFIER NOT NULL,
    [ActivityId] UNIQUEIDENTIFIER NOT NULL,
    [ClientId]   UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt]  DATETIME2 (7)    NOT NULL
);
GO

ALTER TABLE [dbo].[ActivityParticipants]
    ADD CONSTRAINT [PK_ActivityParticipants] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ActivityParticipants_ActivityId_ClientId]
    ON [dbo].[ActivityParticipants]([ActivityId] ASC, [ClientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ActivityParticipants_ClientId]
    ON [dbo].[ActivityParticipants]([ClientId] ASC);
GO

ALTER TABLE [dbo].[ActivityParticipants]
    ADD CONSTRAINT [FK_ActivityParticipants_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [dbo].[Activities] ([Id]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ActivityParticipants]
    ADD CONSTRAINT [FK_ActivityParticipants_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]);
GO

