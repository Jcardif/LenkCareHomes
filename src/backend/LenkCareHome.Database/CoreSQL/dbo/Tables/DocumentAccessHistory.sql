CREATE TABLE [dbo].[DocumentAccessHistory] (
    [Id]            UNIQUEIDENTIFIER NOT NULL,
    [DocumentId]    UNIQUEIDENTIFIER NOT NULL,
    [CaregiverId]   UNIQUEIDENTIFIER NOT NULL,
    [Action]        NVARCHAR (20)    NOT NULL,
    [PerformedById] UNIQUEIDENTIFIER NOT NULL,
    [PerformedAt]   DATETIME2 (7)    NOT NULL
);
GO

ALTER TABLE [dbo].[DocumentAccessHistory]
    ADD CONSTRAINT [FK_DocumentAccessHistory_Users_CaregiverId] FOREIGN KEY ([CaregiverId]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[DocumentAccessHistory]
    ADD CONSTRAINT [FK_DocumentAccessHistory_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[Documents] ([Id]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[DocumentAccessHistory]
    ADD CONSTRAINT [FK_DocumentAccessHistory_Users_PerformedById] FOREIGN KEY ([PerformedById]) REFERENCES [dbo].[Users] ([Id]);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentAccessHistory_PerformedById]
    ON [dbo].[DocumentAccessHistory]([PerformedById] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentAccessHistory_DocumentId]
    ON [dbo].[DocumentAccessHistory]([DocumentId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentAccessHistory_PerformedAt]
    ON [dbo].[DocumentAccessHistory]([PerformedAt] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_DocumentAccessHistory_CaregiverId]
    ON [dbo].[DocumentAccessHistory]([CaregiverId] ASC);
GO

ALTER TABLE [dbo].[DocumentAccessHistory]
    ADD CONSTRAINT [PK_DocumentAccessHistory] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

