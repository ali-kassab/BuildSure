/* =====================================================================
   BuildSure - 03: STORED PROCEDURES
   A stored procedure is named, precompiled T-SQL stored in the database.
   It can take parameters, contain several statements and business logic.

   - usp_GetApplicationsByStatus   : also created by the EF migration and
                                     called from ReportsController.
   - usp_GetApplicationStatistics  : GROUP BY demo (same numbers as the dashboard).
   - usp_ApproveApplication        : the approve workflow written in T-SQL,
                                     to compare with ApplicationService.ApproveAsync.
   Safe to run more than once (CREATE OR ALTER).
   ===================================================================== */
USE BuildSureDb;
GO

-- Required for tables with filtered indexes (IX_Applications_CertificateNumber).
-- SSMS and EF turn these ON by default; the classic sqlcmd tool does not.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetApplicationsByStatus
    @Status NVARCHAR(20) = NULL   -- NULL = all statuses
AS
BEGIN
    SET NOCOUNT ON;   -- don't send "(n rows affected)" messages

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
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetApplicationStatistics
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  Status,
            COUNT(*)               AS ApplicationCount,
            SUM(ConstructionCost)  AS TotalConstructionCost,
            AVG(NumberOfFloors)    AS AverageFloors
    FROM    dbo.Applications
    GROUP BY Status
    ORDER BY Status;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_ApproveApplication
    @ApplicationId INT,
    @ReviewNote    NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Same business rule as the C# service: only Pending applications can be approved.
    UPDATE dbo.Applications
    SET    Status            = N'Approved',
           ReviewedAt        = SYSUTCDATETIME(),
           ReviewNote        = @ReviewNote,
           CertificateNumber = CONCAT(N'SIM-CERT-', YEAR(SYSUTCDATETIME()), N'-', RIGHT(CONCAT(N'000000', @ApplicationId), 6))
    WHERE  Id = @ApplicationId
      AND  Status = N'Pending';

    IF @@ROWCOUNT = 0
    BEGIN
        -- THROW stops the procedure and returns an error to the caller (C#, sqlcmd, SSMS...).
        THROW 50001, N'Application not found or not pending.', 1;
    END

    SELECT Id, ApplicationNumber, Status, CertificateNumber
    FROM   dbo.Applications
    WHERE  Id = @ApplicationId;
END
GO

/* ---- Try them ---- */
EXEC dbo.usp_GetApplicationsByStatus;                       -- all
EXEC dbo.usp_GetApplicationsByStatus @Status = N'Pending';  -- filtered
EXEC dbo.usp_GetApplicationStatistics;

/* usp_ApproveApplication changes data, so this example runs inside a transaction
   and rolls it back - the database is left unchanged. */
BEGIN TRANSACTION;
    DECLARE @PendingId INT = (SELECT TOP (1) Id FROM dbo.Applications WHERE Status = N'Pending' ORDER BY Id);
    IF @PendingId IS NOT NULL
        EXEC dbo.usp_ApproveApplication @ApplicationId = @PendingId, @ReviewNote = N'Approved from T-SQL demo';
ROLLBACK TRANSACTION;
