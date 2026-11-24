using BuildSure.Web.Models;

namespace BuildSure.Web.ViewModels;

/// <summary>Search criteria (bound from the query string) + the results for the Applications list.</summary>
public class ApplicationListViewModel
{
    public string? Search { get; set; }
    public ApplicationStatus? Status { get; set; }
    public IReadOnlyList<InsuranceApplication> Applications { get; set; } = [];
}
