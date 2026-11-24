using BuildSure.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildSure.Web.Data;

/// <summary>
/// Inserts a few sample rows the first time the app runs against an empty database,
/// so the dashboard and lists have something to show. All names are fictional.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(BuildSureDbContext db)
    {
        if (await db.Customers.AnyAsync())
            return;

        var omar = new Customer { FullName = "Omar Haddad", NationalId = "1985031200", PhoneNumber = "+961 70 111 222", Email = "omar.haddad@example.com", Address = "Hamra Street, Beirut" };
        var lina = new Customer { FullName = "Lina Mansour", NationalId = "1990072455", PhoneNumber = "+961 71 333 444", Email = "lina.mansour@example.com", Address = "Main Road, Byblos" };
        var karim = new Customer { FullName = "Karim Saleh", NationalId = "1978110930", PhoneNumber = "+961 76 555 666", Email = "karim.saleh@example.com", Address = "Port Area, Tripoli" };

        var now = DateTime.UtcNow;
        db.Customers.AddRange(omar, lina, karim);
        db.Applications.AddRange(
            new InsuranceApplication
            {
                ApplicationNumber = "APP-SEED-0001", Customer = omar, ProjectName = "Cedar Residence",
                ProjectAddress = "Achrafieh, Beirut", NumberOfFloors = 8, ConstructionCost = 2_400_000m,
                SubmissionDate = now.AddDays(-12), Status = ApplicationStatus.Approved,
                ReviewedAt = now.AddDays(-10), ReviewNote = "All documents complete.", CertificateNumber = "SIM-CERT-SEED-0001"
            },
            new InsuranceApplication
            {
                ApplicationNumber = "APP-SEED-0002", Customer = omar, ProjectName = "Hamra Office Block",
                ProjectAddress = "Hamra, Beirut", NumberOfFloors = 12, ConstructionCost = 5_750_000m,
                SubmissionDate = now.AddDays(-3), Status = ApplicationStatus.Pending
            },
            new InsuranceApplication
            {
                ApplicationNumber = "APP-SEED-0003", Customer = lina, ProjectName = "Seaside Villas",
                ProjectAddress = "Coastal Road, Byblos", NumberOfFloors = 2, ConstructionCost = 900_000m,
                SubmissionDate = now.AddDays(-7), Status = ApplicationStatus.Rejected,
                ReviewedAt = now.AddDays(-5), ReviewNote = "Missing structural engineering report."
            },
            new InsuranceApplication
            {
                ApplicationNumber = "APP-SEED-0004", Customer = karim, ProjectName = "Harbor Warehouse",
                ProjectAddress = "Port Area, Tripoli", NumberOfFloors = 1, ConstructionCost = 650_000m,
                SubmissionDate = now.AddDays(-1), Status = ApplicationStatus.Pending
            });

        await db.SaveChangesAsync();
    }
}
