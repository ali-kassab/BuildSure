IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132633_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [NationalId] nvarchar(20) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Address] nvarchar(250) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132633_InitialCreate'
)
BEGIN
    CREATE TABLE [Applications] (
        [Id] int NOT NULL IDENTITY,
        [ApplicationNumber] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [ProjectName] nvarchar(150) NOT NULL,
        [ProjectAddress] nvarchar(250) NOT NULL,
        [NumberOfFloors] int NOT NULL,
        [ConstructionCost] decimal(18,2) NOT NULL,
        [SubmissionDate] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [ReviewedAt] datetime2 NULL,
        [ReviewNote] nvarchar(500) NULL,
        [CertificateNumber] nvarchar(40) NULL,
        CONSTRAINT [PK_Applications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Applications_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132633_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Applications_ApplicationNumber] ON [Applications] ([ApplicationNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132633_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Applications_CertificateNumber] ON [Applications] ([CertificateNumber]) WHERE [CertificateNumber] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132633_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Applications_CustomerId] ON [Applications] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132633_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Applications_Status] ON [Applications] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132633_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_NationalId] ON [Customers] ([NationalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132633_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261124132633_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132641_AddReportingViewAndProcedure'
)
BEGIN
    CREATE OR ALTER VIEW dbo.vw_CustomerApplicationSummary
    AS
    SELECT
        c.Id                                                        AS CustomerId,
        c.FullName,
        c.NationalId,
        COUNT(a.Id)                                                 AS TotalApplications,
        SUM(CASE WHEN a.Status = N'Pending'  THEN 1 ELSE 0 END)     AS PendingCount,
        SUM(CASE WHEN a.Status = N'Approved' THEN 1 ELSE 0 END)     AS ApprovedCount,
        SUM(CASE WHEN a.Status = N'Rejected' THEN 1 ELSE 0 END)     AS RejectedCount,
        CAST(ISNULL(SUM(a.ConstructionCost), 0) AS decimal(18,2))   AS TotalConstructionCost
    FROM dbo.Customers AS c
    LEFT JOIN dbo.Applications AS a ON a.CustomerId = c.Id
    GROUP BY c.Id, c.FullName, c.NationalId;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132641_AddReportingViewAndProcedure'
)
BEGIN
    CREATE OR ALTER PROCEDURE dbo.usp_GetApplicationsByStatus
        @Status NVARCHAR(20) = NULL   -- NULL = all statuses
    AS
    BEGIN
        SET NOCOUNT ON;

        SELECT
            a.ApplicationNumber,
            c.FullName AS CustomerName,
            a.ProjectName,
            a.NumberOfFloors,
            a.ConstructionCost,
            a.Status,
            a.SubmissionDate,
            a.CertificateNumber
        FROM dbo.Applications AS a
        INNER JOIN dbo.Customers AS c ON c.Id = a.CustomerId
        WHERE @Status IS NULL OR a.Status = @Status
        ORDER BY a.SubmissionDate DESC;
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124132641_AddReportingViewAndProcedure'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261124132641_AddReportingViewAndProcedure', N'10.0.15');
END;

COMMIT;
GO

