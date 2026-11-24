using BuildSure.Web.Models;
using BuildSure.Web.Services;
using BuildSure.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BuildSure.Web.Controllers;

/// <summary>
/// Thin controller: it handles HTTP concerns (binding, validation, choosing a view or JSON)
/// and lets IApplicationService (injected by DI) apply the business rules.
/// </summary>
public class ApplicationsController(IApplicationService applicationService) : Controller
{
    // GET /Applications?search=cedar&status=Pending
    // The parameters are bound from the query string.
    public async Task<IActionResult> Index(string? search, ApplicationStatus? status)
    {
        var applications = await applicationService.SearchAsync(search, status);

        // jQuery's AJAX filter only needs the table, not the whole page.
        if (IsAjaxRequest())
            return PartialView("_ApplicationTable", applications);

        return View(new ApplicationListViewModel { Search = search, Status = status, Applications = applications });
    }

    // GET /Applications/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var application = await applicationService.GetByIdAsync(id);
        return application is null ? NotFound() : View(application);
    }

    // GET /Applications/Create?customerId=3 (customerId optional: pre-selects the customer)
    public async Task<IActionResult> Create(int? customerId)
    {
        var form = new ApplicationFormViewModel { CustomerId = customerId };
        await LoadCustomerOptionsAsync(form);
        return View(form);
    }

    // POST /Applications/Create - workflow step 1: the application is submitted.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApplicationFormViewModel form)
    {
        await ValidateCustomerAsync(form);
        if (!ModelState.IsValid)
        {
            await LoadCustomerOptionsAsync(form); // dropdown data is not posted back, so reload it
            return View(form);
        }

        var application = await applicationService.CreateAsync(form);
        TempData["Success"] = $"Application {application.ApplicationNumber} was submitted and is pending review.";
        return RedirectToAction(nameof(Details), new { id = application.Id });
    }

    // GET /Applications/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var application = await applicationService.GetByIdAsync(id);
        if (application is null)
            return NotFound();

        if (application.Status != ApplicationStatus.Pending)
        {
            TempData["Error"] = $"Only pending applications can be edited. This one is {application.Status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var form = new ApplicationFormViewModel
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            CustomerId = application.CustomerId,
            ProjectName = application.ProjectName,
            ProjectAddress = application.ProjectAddress,
            NumberOfFloors = application.NumberOfFloors,
            ConstructionCost = application.ConstructionCost,
            Notes = application.Notes
        };
        await LoadCustomerOptionsAsync(form);
        return View(form);
    }

    // POST /Applications/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ApplicationFormViewModel form)
    {
        await ValidateCustomerAsync(form);
        if (!ModelState.IsValid)
        {
            form.Id = id;
            await LoadCustomerOptionsAsync(form);
            return View(form);
        }

        var result = await applicationService.UpdateAsync(id, form);
        if (result.Succeeded)
            TempData["Success"] = "Application was updated.";
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Applications/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await applicationService.DeleteAsync(id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Application was deleted.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Applications/Approve/5 - workflow steps 3-5 (called with jQuery AJAX, or as a normal form post).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? reviewNote)
    {
        var result = await applicationService.ApproveAsync(id, reviewNote);
        return await ReviewResponseAsync(id, result, "Application approved. A simulated certificate number was generated.");
    }

    // POST /Applications/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var result = await applicationService.RejectAsync(id, reason);
        return await ReviewResponseAsync(id, result, "Application rejected.");
    }

    // ---------- helpers ----------

    private async Task<IActionResult> ReviewResponseAsync(int id, OperationResult result, string successMessage)
    {
        if (IsAjaxRequest())
        {
            if (!result.Succeeded)
                return BadRequest(new { success = false, message = result.Error });

            var app = (await applicationService.GetByIdAsync(id))!;
            return Json(new
            {
                success = true,
                message = successMessage,
                status = app.Status.ToString(),
                badgeCss = StatusStyles.BadgeCss(app.Status),
                certificateNumber = app.CertificateNumber,
                reviewNote = app.ReviewNote,
                reviewedAt = app.ReviewedAt?.ToLocalTime().ToString("g")
            });
        }

        // Fallback when JavaScript is disabled: classic Post/Redirect/Get.
        if (result.Succeeded) TempData["Success"] = successMessage;
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task ValidateCustomerAsync(ApplicationFormViewModel form)
    {
        if (form.CustomerId.HasValue && !await applicationService.CustomerExistsAsync(form.CustomerId.Value))
            ModelState.AddModelError(nameof(form.CustomerId), "The selected customer does not exist.");
    }

    private async Task LoadCustomerOptionsAsync(ApplicationFormViewModel form)
    {
        var customers = await applicationService.GetCustomerLookupAsync();
        form.Customers = customers.Select(c => new SelectListItem($"{c.FullName} ({c.NationalId})", c.Id.ToString()));
    }

    private bool IsAjaxRequest() => Request.Headers.XRequestedWith == "XMLHttpRequest";
}
