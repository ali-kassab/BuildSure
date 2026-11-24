using BuildSure.Web.Models;

namespace BuildSure.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalApplications { get; set; }
    public int PendingApplications { get; set; }
    public int ApprovedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public int TotalCustomers { get; set; }
    public IReadOnlyList<InsuranceApplication> RecentApplications { get; set; } = [];
}
