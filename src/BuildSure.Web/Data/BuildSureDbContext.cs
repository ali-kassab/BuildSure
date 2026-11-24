using BuildSure.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildSure.Web.Data;

/// <summary>
/// The DbContext is EF Core's gateway to the database (similar to a JPA EntityManager).
/// It is registered with Dependency Injection in Program.cs (AddDbContext) with a
/// *scoped* lifetime: one instance per HTTP request.
/// </summary>
public class BuildSureDbContext(DbContextOptions<BuildSureDbContext> options) : DbContext(options)
{
    // Each DbSet<T> represents a table you can query with LINQ.
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InsuranceApplication> Applications => Set<InsuranceApplication>();

    // Read-only, keyless set backed by a SQL VIEW (created in a migration).
    public DbSet<CustomerApplicationSummary> CustomerSummaries => Set<CustomerApplicationSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent API: configuration that Data Annotations cannot express (indexes, delete rules...).
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.NationalId).IsUnique();
        });

        modelBuilder.Entity<InsuranceApplication>(entity =>
        {
            entity.ToTable("Applications");

            entity.HasIndex(a => a.ApplicationNumber).IsUnique();
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.CertificateNumber).IsUnique().HasFilter("[CertificateNumber] IS NOT NULL");

            // Store the enum as text ('Pending') instead of a number (0).
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            // One-to-many: Customer (1) -> Applications (many).
            // Restrict = SQL Server refuses to delete a customer who still has applications.
            entity.HasOne(a => a.Customer)
                  .WithMany(c => c.Applications)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerApplicationSummary>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_CustomerApplicationSummary");
            entity.Property(s => s.TotalConstructionCost).HasColumnType("decimal(18,2)");
        });
    }
}
