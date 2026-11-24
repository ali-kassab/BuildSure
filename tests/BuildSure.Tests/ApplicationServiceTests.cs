using BuildSure.Web.Data;
using BuildSure.Web.Models;
using BuildSure.Web.Services;
using BuildSure.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BuildSure.Tests;

/// <summary>
/// Unit tests for the workflow rules in ApplicationService.
/// Uses EF Core's InMemory provider so no SQL Server is needed (fast, isolated per test).
/// </summary>
public class ApplicationServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    // Each test gets its own fresh in-memory database.
    private static BuildSureDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BuildSureDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BuildSureDbContext(options);
    }

    private static async Task<(BuildSureDbContext Db, ApplicationService Service, int CustomerId)> ArrangeAsync()
    {
        var db = CreateDb();
        var customer = new Customer
        {
            FullName = "Test Customer", NationalId = "123456789", PhoneNumber = "+961 1 000 000",
            Email = "test@example.com", Address = "Test Street"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return (db, new ApplicationService(db, new FixedClock(FixedNow)), customer.Id);
    }

    private static ApplicationFormViewModel ValidForm(int customerId) => new()
    {
        CustomerId = customerId,
        ProjectName = "Test Tower",
        ProjectAddress = "Test Avenue",
        NumberOfFloors = 10,
        ConstructionCost = 1_500_000m,
        Notes = "  some notes  "
    };

    [Fact]
    public async Task Create_StartsAsPending_WithGeneratedNumberAndSubmissionDate()
    {
        var (db, service, customerId) = await ArrangeAsync();

        var app = await service.CreateAsync(ValidForm(customerId));

        Assert.Equal(ApplicationStatus.Pending, app.Status);
        Assert.StartsWith("APP-20261124-", app.ApplicationNumber);
        Assert.Equal(FixedNow.UtcDateTime, app.SubmissionDate);
        Assert.Equal("some notes", app.Notes);
        Assert.Null(app.CertificateNumber);
        Assert.Equal(1, await db.Applications.CountAsync());
    }

    [Fact]
    public async Task Approve_PendingApplication_GeneratesSimulatedCertificate()
    {
        var (_, service, customerId) = await ArrangeAsync();
        var app = await service.CreateAsync(ValidForm(customerId));

        var result = await service.ApproveAsync(app.Id, "Looks good");

        Assert.True(result.Succeeded);
        var saved = await service.GetByIdAsync(app.Id);
        Assert.Equal(ApplicationStatus.Approved, saved!.Status);
        Assert.Equal($"SIM-CERT-2026-{app.Id:D6}", saved.CertificateNumber);
        Assert.Equal(FixedNow.UtcDateTime, saved.ReviewedAt);
        Assert.Equal("Looks good", saved.ReviewNote);
    }

    [Fact]
    public async Task Approve_AlreadyApproved_Fails()
    {
        var (_, service, customerId) = await ArrangeAsync();
        var app = await service.CreateAsync(ValidForm(customerId));
        await service.ApproveAsync(app.Id, null);

        var second = await service.ApproveAsync(app.Id, null);

        Assert.False(second.Succeeded);
        Assert.Contains("Only pending", second.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Reject_WithoutReason_Fails(string? reason)
    {
        var (_, service, customerId) = await ArrangeAsync();
        var app = await service.CreateAsync(ValidForm(customerId));

        var result = await service.RejectAsync(app.Id, reason);

        Assert.False(result.Succeeded);
        Assert.Equal(ApplicationStatus.Pending, (await service.GetByIdAsync(app.Id))!.Status);
    }

    [Fact]
    public async Task Reject_WithReason_SetsRejectedAndNoCertificate()
    {
        var (_, service, customerId) = await ArrangeAsync();
        var app = await service.CreateAsync(ValidForm(customerId));

        var result = await service.RejectAsync(app.Id, "Missing documents");

        Assert.True(result.Succeeded);
        var saved = await service.GetByIdAsync(app.Id);
        Assert.Equal(ApplicationStatus.Rejected, saved!.Status);
        Assert.Equal("Missing documents", saved.ReviewNote);
        Assert.Null(saved.CertificateNumber);
    }

    [Fact]
    public async Task Update_NonPendingApplication_Fails()
    {
        var (_, service, customerId) = await ArrangeAsync();
        var app = await service.CreateAsync(ValidForm(customerId));
        await service.RejectAsync(app.Id, "No");

        var form = ValidForm(customerId);
        form.ProjectName = "Changed";
        var result = await service.UpdateAsync(app.Id, form);

        Assert.False(result.Succeeded);
        Assert.Equal("Test Tower", (await service.GetByIdAsync(app.Id))!.ProjectName);
    }

    [Fact]
    public async Task Delete_ApprovedApplication_Fails_ButRejectedCanBeDeleted()
    {
        var (db, service, customerId) = await ArrangeAsync();
        var approved = await service.CreateAsync(ValidForm(customerId));
        var rejected = await service.CreateAsync(ValidForm(customerId));
        await service.ApproveAsync(approved.Id, null);
        await service.RejectAsync(rejected.Id, "No");

        Assert.False((await service.DeleteAsync(approved.Id)).Succeeded);
        Assert.True((await service.DeleteAsync(rejected.Id)).Succeeded);
        Assert.Equal(1, await db.Applications.CountAsync());
    }

    [Fact]
    public async Task Dashboard_CountsApplicationsByStatus()
    {
        var (_, service, customerId) = await ArrangeAsync();
        var a1 = await service.CreateAsync(ValidForm(customerId));
        var a2 = await service.CreateAsync(ValidForm(customerId));
        await service.CreateAsync(ValidForm(customerId));
        await service.ApproveAsync(a1.Id, null);
        await service.RejectAsync(a2.Id, "No");

        var dashboard = await service.GetDashboardAsync();

        Assert.Equal(3, dashboard.TotalApplications);
        Assert.Equal(1, dashboard.PendingApplications);
        Assert.Equal(1, dashboard.ApprovedApplications);
        Assert.Equal(1, dashboard.RejectedApplications);
        Assert.Equal(3, dashboard.RecentApplications.Count);
    }

    [Fact]
    public async Task Search_FiltersByStatusAndText()
    {
        var (_, service, customerId) = await ArrangeAsync();
        var form = ValidForm(customerId);
        form.ProjectName = "Harbor Warehouse";
        var harbor = await service.CreateAsync(form);
        await service.CreateAsync(ValidForm(customerId));
        await service.ApproveAsync(harbor.Id, null);

        var approved = await service.SearchAsync(null, ApplicationStatus.Approved);
        var byText = await service.SearchAsync("Harbor", null);

        Assert.Single(approved);
        Assert.Single(byText);
        Assert.Equal(harbor.Id, byText[0].Id);
    }
}
