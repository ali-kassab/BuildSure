using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BuildSure.Web.ViewModels;

/// <summary>
/// What the Create/Edit form posts back. Using a ViewModel instead of the entity means the user
/// can only send the fields we allow (no Status, CertificateNumber...) - this prevents "over-posting".
/// </summary>
public class ApplicationFormViewModel
{
    public int? Id { get; set; }

    // Shown read-only on the Edit page; never bound back to the database.
    public string? ApplicationNumber { get; set; }

    [Required(ErrorMessage = "Please select a customer.")]
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Required, StringLength(150, MinimumLength = 3)]
    [Display(Name = "Project Name")]
    public string ProjectName { get; set; } = string.Empty;

    [Required, StringLength(250)]
    [Display(Name = "Project Address")]
    public string ProjectAddress { get; set; } = string.Empty;

    // Nullable so an empty field shows "required" instead of silently becoming 0.
    [Required, Range(1, 200, ErrorMessage = "Number of floors must be between 1 and 200.")]
    [Display(Name = "Number of Floors")]
    public int? NumberOfFloors { get; set; }

    [Required, Range(1000, 10_000_000_000d, ErrorMessage = "Construction cost must be between 1,000 and 10,000,000,000.")]
    [Display(Name = "Construction Cost")]
    public decimal? ConstructionCost { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Data for the customer <select>; filled by the controller, not posted by the browser.
    [ValidateNever]
    public IEnumerable<SelectListItem> Customers { get; set; } = [];
}
