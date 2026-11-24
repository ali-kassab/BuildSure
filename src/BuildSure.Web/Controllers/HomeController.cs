using System.Diagnostics;
using BuildSure.Web.Models;
using BuildSure.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuildSure.Web.Controllers;

public class HomeController(IApplicationService applicationService) : Controller
{
    // GET /  (or /Home/Index) - the dashboard
    public async Task<IActionResult> Index()
    {
        var dashboard = await applicationService.GetDashboardAsync();
        return View(dashboard); // strongly typed: Views/Home/Index.cshtml has @model DashboardViewModel
    }

    // GET /Home/Stats - JSON used by the dashboard's "Refresh" button (jQuery AJAX)
    [HttpGet]
    public async Task<IActionResult> Stats()
    {
        var d = await applicationService.GetDashboardAsync();
        return Json(new
        {
            total = d.TotalApplications,
            pending = d.PendingApplications,
            approved = d.ApprovedApplications,
            rejected = d.RejectedApplications,
            customers = d.TotalCustomers
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
