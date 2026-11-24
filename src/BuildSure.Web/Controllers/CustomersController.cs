using BuildSure.Web.Data;
using BuildSure.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildSure.Web.Controllers;

/// <summary>
/// Plain CRUD with no real business rules, so this controller uses the DbContext directly
/// (no service layer needed). Compare with ApplicationsController, which delegates to a service.
/// </summary>
public class CustomersController(BuildSureDbContext db) : Controller
{
    // GET /Customers?search=omar
    public async Task<IActionResult> Index(string? search)
    {
        var query = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.FullName.Contains(term) || c.NationalId.Contains(term) || c.Email.Contains(term));
        }

        ViewData["Search"] = search;
        var customers = await query
            .Include(c => c.Applications) // needed to show the application count per customer
            .OrderBy(c => c.FullName)
            .ToListAsync();
        return View(customers);
    }

    // GET /Customers/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .Include(c => c.Applications.OrderByDescending(a => a.SubmissionDate)) // filtered/ordered Include
            .FirstOrDefaultAsync(c => c.Id == id);

        return customer is null ? NotFound() : View(customer);
    }

    // GET /Customers/Create - show the empty form
    public IActionResult Create() => View(new Customer());

    // POST /Customers/Create - model binding fills "customer" from the posted form fields.
    // [Bind] lists the only fields we accept (protects Id, CreatedAt and Applications from over-posting).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,NationalId,PhoneNumber,Email,Address")] Customer customer)
    {
        await ValidateUniqueNationalIdAsync(customer);
        if (!ModelState.IsValid)
            return View(customer); // redisplay the form with validation messages

        customer.CreatedAt = DateTime.UtcNow;
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Customer \"{customer.FullName}\" was created.";
        return RedirectToAction(nameof(Details), new { id = customer.Id }); // Post/Redirect/Get pattern
    }

    // GET /Customers/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await db.Customers.FindAsync(id);
        return customer is null ? NotFound() : View(customer);
    }

    // POST /Customers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("FullName,NationalId,PhoneNumber,Email,Address")] Customer input)
    {
        var customer = await db.Customers.FindAsync(id); // tracked entity
        if (customer is null)
            return NotFound();

        input.Id = id;
        await ValidateUniqueNationalIdAsync(input);
        if (!ModelState.IsValid)
            return View(input);

        // Copy only the editable fields onto the tracked entity; EF generates an UPDATE for changed columns.
        customer.FullName = input.FullName;
        customer.NationalId = input.NationalId;
        customer.PhoneNumber = input.PhoneNumber;
        customer.Email = input.Email;
        customer.Address = input.Address;
        await db.SaveChangesAsync();

        TempData["Success"] = "Customer details were updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Customers/Delete/5 - the jQuery confirmation dialog runs before this form is submitted.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await db.Customers.FindAsync(id);
        if (customer is null)
            return NotFound();

        // Business rule (also enforced by the FK with ON DELETE NO ACTION in SQL Server).
        if (await db.Applications.AnyAsync(a => a.CustomerId == id))
        {
            TempData["Error"] = $"\"{customer.FullName}\" has insurance applications and cannot be deleted. Delete their applications first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        db.Customers.Remove(customer);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Customer \"{customer.FullName}\" was deleted.";
        return RedirectToAction(nameof(Index));
    }

    // National ID has a UNIQUE index; checking first gives a friendly message instead of a SQL error.
    private async Task ValidateUniqueNationalIdAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.NationalId))
            return;

        bool taken = await db.Customers.AnyAsync(c => c.NationalId == customer.NationalId && c.Id != customer.Id);
        if (taken)
            ModelState.AddModelError(nameof(Customer.NationalId), "Another customer already uses this National ID.");
    }
}
