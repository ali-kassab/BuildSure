using System.ComponentModel.DataAnnotations;

namespace BuildSure.Web.Models;

/// <summary>
/// Entity class: EF Core maps this to the "Customers" table (Code-First).
/// Data Annotations drive BOTH the database schema (StringLength -> nvarchar(n), Required -> NOT NULL)
/// and validation (ModelState in the controller + jQuery validation in the browser).
/// </summary>
public class Customer
{
    public int Id { get; set; } // "Id" naming convention => primary key, IDENTITY column

    [Required, StringLength(100, MinimumLength = 3)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [RegularExpression(@"^[0-9]{6,20}$", ErrorMessage = "National ID must contain 6 to 20 digits.")]
    [Display(Name = "National ID")]
    public string NationalId { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Created")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: Customer (1) -> Applications (many).
    public ICollection<InsuranceApplication> Applications { get; set; } = new List<InsuranceApplication>();
}
