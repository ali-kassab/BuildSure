using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSure.Web.Data.Migrations
{
    /// <summary>
    /// Hand-written migration: EF cannot generate SQL VIEWs or STORED PROCEDUREs from C# classes,
    /// so we add them with raw SQL. Keeping them in a migration means every environment gets them
    /// automatically with "dotnet ef database update". The same SQL is in database/02 and database/03.
    /// </summary>
    public partial class AddReportingViewAndProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
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
                """);

            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetApplicationsByStatus;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_CustomerApplicationSummary;");
        }
    }
}
