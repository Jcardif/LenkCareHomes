CREATE TABLE [dbo].[Homes] (
    [Id]          UNIQUEIDENTIFIER NOT NULL,
    [Name]        NVARCHAR (200)   NOT NULL,
    [Address]     NVARCHAR (500)   NOT NULL,
    [City]        NVARCHAR (100)   NOT NULL,
    [State]       NVARCHAR (50)    NOT NULL,
    [ZipCode]     NVARCHAR (20)    NOT NULL,
    [PhoneNumber] NVARCHAR (20)    NULL,
    [Capacity]    INT              NOT NULL,
    [IsActive]    BIT              NOT NULL,
    [CreatedAt]   DATETIME2 (7)    NOT NULL,
    [UpdatedAt]   DATETIME2 (7)    NULL,
    [CreatedById] UNIQUEIDENTIFIER NOT NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_Homes_Name]
    ON [dbo].[Homes]([Name] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Homes_IsActive]
    ON [dbo].[Homes]([IsActive] ASC);
GO

ALTER TABLE [dbo].[Homes]
    ADD CONSTRAINT [PK_Homes] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

