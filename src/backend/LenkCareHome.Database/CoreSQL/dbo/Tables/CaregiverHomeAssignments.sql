CREATE TABLE [dbo].[CaregiverHomeAssignments] (
    [Id]           UNIQUEIDENTIFIER NOT NULL,
    [UserId]       UNIQUEIDENTIFIER NOT NULL,
    [HomeId]       UNIQUEIDENTIFIER NOT NULL,
    [AssignedAt]   DATETIME2 (7)    NOT NULL,
    [AssignedById] UNIQUEIDENTIFIER NOT NULL,
    [IsActive]     BIT              NOT NULL
);
GO

ALTER TABLE [dbo].[CaregiverHomeAssignments]
    ADD CONSTRAINT [FK_CaregiverHomeAssignments_Homes_HomeId] FOREIGN KEY ([HomeId]) REFERENCES [dbo].[Homes] ([Id]);
GO

ALTER TABLE [dbo].[CaregiverHomeAssignments]
    ADD CONSTRAINT [FK_CaregiverHomeAssignments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]);
GO

ALTER TABLE [dbo].[CaregiverHomeAssignments]
    ADD CONSTRAINT [PK_CaregiverHomeAssignments] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_CaregiverHomeAssignments_HomeId]
    ON [dbo].[CaregiverHomeAssignments]([HomeId] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_CaregiverHomeAssignments_UserId_HomeId]
    ON [dbo].[CaregiverHomeAssignments]([UserId] ASC, [HomeId] ASC);
GO

