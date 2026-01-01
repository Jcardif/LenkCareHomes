CREATE TABLE [dbo].[Beds] (
    [Id]        UNIQUEIDENTIFIER NOT NULL,
    [HomeId]    UNIQUEIDENTIFIER NOT NULL,
    [Label]     NVARCHAR (100)   NOT NULL,
    [Status]    NVARCHAR (20)    DEFAULT (N'Available') NOT NULL,
    [IsActive]  BIT              NOT NULL,
    [CreatedAt] DATETIME2 (7)    NOT NULL,
    [UpdatedAt] DATETIME2 (7)    NULL
);
GO

ALTER TABLE [dbo].[Beds]
    ADD CONSTRAINT [FK_Beds_Homes_HomeId] FOREIGN KEY ([HomeId]) REFERENCES [dbo].[Homes] ([Id]);
GO

ALTER TABLE [dbo].[Beds]
    ADD CONSTRAINT [PK_Beds] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Beds_HomeId_Label]
    ON [dbo].[Beds]([HomeId] ASC, [Label] ASC);
GO

