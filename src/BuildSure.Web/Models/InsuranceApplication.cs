using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildSure.Web.Models;

/// <summary>
/// A construction insurance application (training simulation - no real insurance logic).
/// Workflow: Pending -> Approved (simulated certificate number generated) or Pending -> Rejected.
/// </summary>
public class InsuranceApplication
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Application Number")]
    public string ApplicationNumber { get; set; } = string.Empty;

    // Foreign key + navigation property => one-to-many relationship with Customer.
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Project Name")]
    public string ProjectName { get; set; } = string.Empty;

    [Required, StringLength(250)]
    [Display(Name = "Project Address")]
    public string ProjectAddress { get; set; } = string.Empty;

    [Range(1, 200)]
    [Display(Name = "Number of Floors")]
    public int NumberOfFloors { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Construction Cost")]
    public decimal ConstructionCost { get; set; }

    [Display(Name = "Submission Date")]
    public DateTime SubmissionDate { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    [StringLength(1000)]
    public string? Notes { get; set; }

    // --- Review (workflow) fields, filled in when an employee approves/rejects ---

    [Display(Name = "Reviewed At")]
    public DateTime? ReviewedAt { get; set; }

    [StringLength(500)]
    [Display(Name = "Review Note")]
    public string? ReviewNote { get; set; }

    [StringLength(40)]
    [Display(Name = "Certificate Number")]
    public string? CertificateNumber { get; set; }
}
