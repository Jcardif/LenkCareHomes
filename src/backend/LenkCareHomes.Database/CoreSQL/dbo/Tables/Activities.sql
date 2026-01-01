CREATE TABLE [dbo].[Activities] (
    [Id]              UNIQUEIDENTIFIER NOT NULL,
    [ActivityName]    NVARCHAR (200)   NOT NULL,
    [Description]     NVARCHAR (2000)  NULL,
    [Date]            DATETIME2 (7)    NOT NULL,
    [StartTime]       TIME (7)         NULL,
    [EndTime]         TIME (7)         NULL,
    [Duration]        INT              NULL,
    [Category]        NVARCHAR (20)    NOT NULL,
    [IsGroupActivity] BIT              NOT NULL,
    [HomeId]          UNIQUEIDENTIFIER NULL,
    [CreatedById]     UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt]       DATETIME2 (7)    NOT NULL,
    [UpdatedAt]       DATETIME2 (7)    NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_Activities_Date]
    ON [dbo].[Activities]([Date] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Activities_CreatedById]
    ON [dbo].[Activities]([CreatedById] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Activities_HomeId]
    ON [dbo].[Activities]([HomeId] ASC);
GO

ALTER TABLE [dbo].[Activities]
    ADD CONSTRAINT [PK_Activities] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

ALTER TABLE [dbo].[Activities]
    ADD CONSTRAINT [FK_Activities_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[Activities]
    ADD CONSTRAINT [FK_Activities_Homes_HomeId] FOREIGN KEY ([HomeId]) REFERENCES [dbo].[Homes] ([Id]);
GO

