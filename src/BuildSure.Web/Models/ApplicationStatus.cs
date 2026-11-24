namespace BuildSure.Web.Models;

/// <summary>
/// The three states an application can be in. Stored as text in SQL Server
/// (see BuildSureDbContext) so the database stays readable: 'Pending', 'Approved', 'Rejected'.
/// </summary>
public enum ApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
