using BuildSure.Web.Data;
using BuildSure.Web.Models;
using BuildSure.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildSure.Web.Controllers;

/// <summary>
/// Shows two ways of using database objects written in plain SQL (see database/ folder and
/// the "AddReportingViewAndProcedure" migration):
///  1. a SQL VIEW mapped to a keyless entity  -> queried with normal LINQ
///  2. a STORED PROCEDURE                     -> executed with Database.SqlQuery
/// </summary>
public class ReportsController(BuildSureDbContext db) : Controller
{
    // GET /Reports?status=Approved
    public async Task<IActionResult> Index(ApplicationStatus? status)
    {
        // 1. SQL VIEW: LINQ works on it like a table; EF generates SELECT ... FROM vw_CustomerApplicationSummary ORDER BY ...
        var summaries = await db.CustomerSummaries
            .OrderByDescending(s => s.TotalApplications)
            .ThenBy(s => s.FullName)
            .ToListAsync();

        // 2. STORED PROCEDURE: the interpolated value becomes a SQL parameter (@p0), NOT string concatenation,
        //    so this is safe from SQL injection. EXEC results cannot be composed with more LINQ.
        string? statusName = status?.ToString();
        var rows = await db.Database
            .SqlQuery<ApplicationReportRow>($"EXEC dbo.usp_GetApplicationsByStatus @Status = {statusName}")
            .ToListAsync();

        return View(new ReportsViewModel { Status = status, CustomerSummaries = summaries, Applications = rows });
    }
}
