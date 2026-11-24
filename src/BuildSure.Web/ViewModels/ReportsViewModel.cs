using BuildSure.Web.Models;

namespace BuildSure.Web.ViewModels;

public class ReportsViewModel
{
    public ApplicationStatus? Status { get; set; }

    // Rows read from the SQL VIEW (vw_CustomerApplicationSummary).
    public IReadOnlyList<CustomerApplicationSummary> CustomerSummaries { get; set; } = [];

    // Rows returned by the STORED PROCEDURE (usp_GetApplicationsByStatus).
    public IReadOnlyList<ApplicationReportRow> Applications { get; set; } = [];
}
