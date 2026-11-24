/* =====================================================================
   BuildSure - 02: SQL VIEW
   A view is a saved SELECT statement that you query like a table.
   This exact view is also created by the EF migration
   "AddReportingViewAndProcedure" and is used by the SQL Reports page
   (ReportsController -> db.CustomerSummaries).
   Safe to run more than once (CREATE OR ALTER).
   ===================================================================== */
USE BuildSureDb;
GO

-- Required for tables with filtered indexes (IX_Applications_CertificateNumber).
-- SSMS and EF turn these ON by default; the classic sqlcmd tool does not.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

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
LEFT JOIN dbo.Applications AS a ON a.CustomerId = c.Id   -- LEFT JOIN keeps customers with 0 applications
GROUP BY c.Id, c.FullName, c.NationalId;
GO

/* Use the view like a table: */
SELECT * FROM dbo.vw_CustomerApplicationSummary ORDER BY TotalApplications DESC;

SELECT FullName, ApprovedCount
FROM   dbo.vw_CustomerApplicationSummary
WHERE  ApprovedCount > 0;
