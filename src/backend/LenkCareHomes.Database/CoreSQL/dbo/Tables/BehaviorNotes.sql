CREATE TABLE [dbo].[BehaviorNotes] (
    [Id]          UNIQUEIDENTIFIER NOT NULL,
    [ClientId]    UNIQUEIDENTIFIER NOT NULL,
    [CaregiverId] UNIQUEIDENTIFIER NOT NULL,
    [Timestamp]   DATETIME2 (7)    NOT NULL,
    [Category]    NVARCHAR (20)    NOT NULL,
    [NoteText]    NVARCHAR (4000)  NOT NULL,
    [Severity]    NVARCHAR (20)    NULL,
    [CreatedAt]   DATETIME2 (7)    NOT NULL
);
GO

ALTER TABLE [dbo].[BehaviorNotes]
    ADD CONSTRAINT [FK_BehaviorNotes_Users_CaregiverId] FOREIGN KEY ([CaregiverId]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[BehaviorNotes]
    ADD CONSTRAINT [FK_BehaviorNotes_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients] ([Id]);
GO

ALTER TABLE [dbo].[BehaviorNotes]
    ADD CONSTRAINT [PK_BehaviorNotes] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_BehaviorNotes_CaregiverId]
    ON [dbo].[BehaviorNotes]([CaregiverId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_BehaviorNotes_ClientId]
    ON [dbo].[BehaviorNotes]([ClientId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_BehaviorNotes_Timestamp]
    ON [dbo].[BehaviorNotes]([Timestamp] ASC);
GO

