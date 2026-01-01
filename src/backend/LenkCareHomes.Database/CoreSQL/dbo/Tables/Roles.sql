CREATE TABLE [dbo].[Roles] (
    [Id]               UNIQUEIDENTIFIER NOT NULL,
    [Description]      NVARCHAR (500)   NULL,
    [HasPhiAccess]     BIT              NOT NULL,
    [CreatedAt]        DATETIME2 (7)    NOT NULL,
    [Name]             NVARCHAR (256)   NULL,
    [NormalizedName]   NVARCHAR (256)   NULL,
    [ConcurrencyStamp] NVARCHAR (MAX)   NULL
);
GO

ALTER TABLE [dbo].[Roles]
    ADD CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex]
    ON [dbo].[Roles]([NormalizedName] ASC) WHERE ([NormalizedName] IS NOT NULL);
GO

