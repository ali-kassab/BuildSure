using BuildSure.Web.Data;
using BuildSure.Web.Models;
using BuildSure.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BuildSure.Web.Services;

/// <summary>
/// Holds the application workflow rules in one place so controllers stay thin:
///  - a new application always starts as Pending,
///  - only Pending applications can be edited, approved or rejected,
///  - rejecting requires a reason,
///  - approving generates a SIMULATED certificate number,
///  - approved applications cannot be deleted.
/// </summary>
public class ApplicationService(BuildSureDbContext db, TimeProvider clock) : IApplicationService
{
    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        // GROUP BY in LINQ -> one SQL query: SELECT Status, COUNT(*) ... GROUP BY Status
        var countsByStatus = await db.Applications
            .AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var recent = await db.Applications
            .AsNoTracking()                 // read-only: EF skips change tracking (faster, less memory)
            .Include(a => a.Customer)       // JOIN Customers so a.Customer.FullName is available
            .OrderByDescending(a => a.SubmissionDate)
            .Take(5)
            .ToListAsync();

        return new DashboardViewModel
        {
            PendingApplications = countsByStatus.GetValueOrDefault(ApplicationStatus.Pending),
            ApprovedApplications = countsByStatus.GetValueOrDefault(ApplicationStatus.Approved),
            RejectedApplications = countsByStatus.GetValueOrDefault(ApplicationStatus.Rejected),
            TotalApplications = countsByStatus.Values.Sum(),
            TotalCustomers = await db.Customers.CountAsync(),
            RecentApplications = recent
        };
    }

    public async Task<IReadOnlyList<InsuranceApplication>> SearchAsync(string? search, ApplicationStatus? status)
    {
        // IQueryable: nothing is sent to SQL Server until ToListAsync(). Each Where() adds to the SQL WHERE clause.
        IQueryable<InsuranceApplication> query = db.Applications.AsNoTracking().Include(a => a.Customer);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                a.ApplicationNumber.Contains(term) ||
                a.ProjectName.Contains(term) ||
                a.Customer!.FullName.Contains(term));
        }

        return await query.OrderByDescending(a => a.SubmissionDate).ToListAsync();
    }

    public Task<InsuranceApplication?> GetByIdAsync(int id) =>
        db.Applications
          .AsNoTracking()
          .Include(a => a.Customer)
          .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IReadOnlyList<Customer>> GetCustomerLookupAsync() =>
        await db.Customers
            .AsNoTracking()
            .OrderBy(c => c.FullName)
            // Projection: only the 3 columns the dropdown needs are SELECTed.
            .Select(c => new Customer { Id = c.Id, FullName = c.FullName, NationalId = c.NationalId })
            .ToListAsync();

    public Task<bool> CustomerExistsAsync(int customerId) =>
        db.Customers.AnyAsync(c => c.Id == customerId);

    public async Task<InsuranceApplication> CreateAsync(ApplicationFormViewModel form)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var application = new InsuranceApplication
        {
            ApplicationNumber = GenerateApplicationNumber(now),
            CustomerId = form.CustomerId!.Value,
            ProjectName = form.ProjectName.Trim(),
            ProjectAddress = form.ProjectAddress.Trim(),
            NumberOfFloors = form.NumberOfFloors!.Value,
            ConstructionCost = form.ConstructionCost!.Value,
            Notes = form.Notes?.Trim(),
            SubmissionDate = now,
            Status = ApplicationStatus.Pending // workflow step 2: always starts as Pending
        };

        db.Applications.Add(application);   // tracked as "Added"
        await db.SaveChangesAsync();        // INSERT ... ; SQL Server returns the new IDENTITY Id
        return application;
    }

    public async Task<OperationResult> UpdateAsync(int id, ApplicationFormViewModel form)
    {
        // No AsNoTracking here: we WANT tracking so SaveChangesAsync can detect what changed.
        var application = await db.Applications.FindAsync(id);
        if (application is null)
            return OperationResult.Failure("Application not found.");
        if (application.Status != ApplicationStatus.Pending)
            return OperationResult.Failure($"Only pending applications can be edited. This one is {application.Status}.");

        application.CustomerId = form.CustomerId!.Value;
        application.ProjectName = form.ProjectName.Trim();
        application.ProjectAddress = form.ProjectAddress.Trim();
        application.NumberOfFloors = form.NumberOfFloors!.Value;
        application.ConstructionCost = form.ConstructionCost!.Value;
        application.Notes = form.Notes?.Trim();

        await db.SaveChangesAsync(); // UPDATE only the columns that actually changed
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var application = await db.Applications.FindAsync(id);
        if (application is null)
            return OperationResult.Failure("Application not found.");
        if (application.Status == ApplicationStatus.Approved)
            return OperationResult.Failure("Approved applications have a certificate and cannot be deleted.");

        db.Applications.Remove(application);
        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    public async Task<OperationResult> ApproveAsync(int id, string? reviewNote)
    {
        var application = await db.Applications.FindAsync(id);
        if (application is null)
            return OperationResult.Failure("Application not found.");
        if (application.Status != ApplicationStatus.Pending)
            return OperationResult.Failure($"Only pending applications can be approved. This one is {application.Status}.");

        var now = clock.GetUtcNow().UtcDateTime;
        application.Status = ApplicationStatus.Approved;
        application.ReviewedAt = now;
        application.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
        // Simulated certificate number - the Id is unique, so the certificate number is unique too.
        application.CertificateNumber = $"SIM-CERT-{now:yyyy}-{application.Id:D6}";

        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    public async Task<OperationResult> RejectAsync(int id, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return OperationResult.Failure("A reason is required to reject an application.");
        if (reason.Trim().Length > 500)
            return OperationResult.Failure("The reason cannot be longer than 500 characters.");

        var application = await db.Applications.FindAsync(id);
        if (application is null)
            return OperationResult.Failure("Application not found.");
        if (application.Status != ApplicationStatus.Pending)
            return OperationResult.Failure($"Only pending applications can be rejected. This one is {application.Status}.");

        application.Status = ApplicationStatus.Rejected;
        application.ReviewedAt = clock.GetUtcNow().UtcDateTime;
        application.ReviewNote = reason.Trim();

        await db.SaveChangesAsync();
        return OperationResult.Success();
    }

    // e.g. APP-20261124-7F3A9C. The unique index on ApplicationNumber guarantees no duplicates are stored.
    private static string GenerateApplicationNumber(DateTime now) =>
        $"APP-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
