using BuildSure.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BuildSure.Web.ViewModels;

/// <summary>Display helpers for ApplicationStatus, shared by Razor views and the JSON returned to jQuery.</summary>
public static class StatusStyles
{
    /// <summary>Maps a status to a Bootstrap badge colour.</summary>
    public static string BadgeCss(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Approved => "text-bg-success",
        ApplicationStatus.Rejected => "text-bg-danger",
        _ => "text-bg-warning"
    };

    /// <summary>&lt;option&gt;s for a status filter, using names ("Pending") as values so URLs stay readable.</summary>
    public static IEnumerable<SelectListItem> StatusOptions() =>
        Enum.GetValues<ApplicationStatus>().Select(s => new SelectListItem(s.ToString(), s.ToString()));
}
