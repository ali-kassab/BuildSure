namespace BuildSure.Web.Models;

/// <summary>
/// Keyless entity mapped to the SQL VIEW dbo.vw_CustomerApplicationSummary (read-only).
/// The view itself is created by the migration "AddReportingViewAndProcedure".
/// </summary>
public class CustomerApplicationSummary
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public int TotalApplications { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public decimal TotalConstructionCost { get; set; }
}

/// <summary>
/// Plain class that receives rows from the STORED PROCEDURE dbo.usp_GetApplicationsByStatus.
/// It is not an entity: Database.SqlQuery maps result columns to properties by name.
/// </summary>
public class ApplicationReportRow
{
    public string ApplicationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int NumberOfFloors { get; set; }
    public decimal ConstructionCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public string? CertificateNumber { get; set; }
}
