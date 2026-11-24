using BuildSure.Web.Models;
using BuildSure.Web.ViewModels;

namespace BuildSure.Web.Services;

/// <summary>
/// Business logic for insurance applications (numbering + the review workflow).
/// Controllers depend on this interface; Program.cs tells DI which class implements it.
/// </summary>
public interface IApplicationService
{
    Task<DashboardViewModel> GetDashboardAsync();
    Task<IReadOnlyList<InsuranceApplication>> SearchAsync(string? search, ApplicationStatus? status);
    Task<InsuranceApplication?> GetByIdAsync(int id);

    /// <summary>Customers for the "select a customer" dropdown.</summary>
    Task<IReadOnlyList<Customer>> GetCustomerLookupAsync();
    Task<bool> CustomerExistsAsync(int customerId);

    Task<InsuranceApplication> CreateAsync(ApplicationFormViewModel form);
    Task<OperationResult> UpdateAsync(int id, ApplicationFormViewModel form);
    Task<OperationResult> DeleteAsync(int id);

    Task<OperationResult> ApproveAsync(int id, string? reviewNote);
    Task<OperationResult> RejectAsync(int id, string? reason);
}
