CREATE TABLE [dbo].[Clients] (
    [Id]                           UNIQUEIDENTIFIER NOT NULL,
    [FirstName]                    NVARCHAR (100)   NOT NULL,
    [LastName]                     NVARCHAR (100)   NOT NULL,
    [DateOfBirth]                  DATETIME2 (7)    NOT NULL,
    [Gender]                       NVARCHAR (50)    NOT NULL,
    [SsnEncrypted]                 NVARCHAR (500)   NULL,
    [AdmissionDate]                DATETIME2 (7)    NOT NULL,
    [DischargeDate]                DATETIME2 (7)    NULL,
    [DischargeReason]              NVARCHAR (500)   NULL,
    [HomeId]                       UNIQUEIDENTIFIER NOT NULL,
    [BedId]                        UNIQUEIDENTIFIER NULL,
    [PrimaryPhysician]             NVARCHAR (200)   NULL,
    [PrimaryPhysicianPhone]        NVARCHAR (20)    NULL,
    [EmergencyContactName]         NVARCHAR (200)   NULL,
    [EmergencyContactPhone]        NVARCHAR (20)    NULL,
    [EmergencyContactRelationship] NVARCHAR (100)   NULL,
    [Allergies]                    NVARCHAR (2000)  NULL,
    [Diagnoses]                    NVARCHAR (2000)  NULL,
    [MedicationList]               NVARCHAR (4000)  NULL,
    [PhotoUrl]                     NVARCHAR (500)   NULL,
    [Notes]                        NVARCHAR (4000)  NULL,
    [IsActive]                     BIT              NOT NULL,
    [CreatedAt]                    DATETIME2 (7)    NOT NULL,
    [UpdatedAt]                    DATETIME2 (7)    NULL,
    [CreatedById]                  UNIQUEIDENTIFIER NOT NULL
);
GO

CREATE NONCLUSTERED INDEX [IX_Clients_LastName_FirstName]
    ON [dbo].[Clients]([LastName] ASC, [FirstName] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Clients_BedId]
    ON [dbo].[Clients]([BedId] ASC) WHERE ([BedId] IS NOT NULL);
GO

CREATE NONCLUSTERED INDEX [IX_Clients_HomeId]
    ON [dbo].[Clients]([HomeId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Clients_IsActive]
    ON [dbo].[Clients]([IsActive] ASC);
GO

ALTER TABLE [dbo].[Clients]
    ADD CONSTRAINT [PK_Clients] PRIMARY KEY CLUSTERED ([Id] ASC);
GO

ALTER TABLE [dbo].[Clients]
    ADD CONSTRAINT [FK_Clients_Homes_HomeId] FOREIGN KEY ([HomeId]) REFERENCES [dbo].[Homes] ([Id]);
GO

ALTER TABLE [dbo].[Clients]
    ADD CONSTRAINT [FK_Clients_Beds_BedId] FOREIGN KEY ([BedId]) REFERENCES [dbo].[Beds] ([Id]) ON DELETE SET NULL;
GO

