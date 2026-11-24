using Microsoft.Data.SqlClient;

namespace BuildSure.WinForms;

/// <summary>
/// Event-driven desktop UI: controls are created in ApplicationSearchForm.Designer.cs,
/// and this file contains the event handlers (button click, Enter key, form load).
/// </summary>
public partial class ApplicationSearchForm : Form
{
    private readonly ApplicationRepository _repository = new(Program.ConnectionString);

    public ApplicationSearchForm()
    {
        InitializeComponent();
    }

    private async void ApplicationSearchForm_Load(object? sender, EventArgs e) => await SearchAsync();

    // "async void" is normally avoided, but it is the accepted pattern for UI event handlers.
    private async void btnSearch_Click(object? sender, EventArgs e) => await SearchAsync();

    private async Task SearchAsync()
    {
        btnSearch.Enabled = false;
        UseWaitCursor = true;
        lblStatus.Text = "Searching...";
        try
        {
            // await keeps the UI responsive while SQL Server works; execution resumes on the UI thread.
            var results = await _repository.SearchAsync(txtSearch.Text);
            gridApplications.DataSource = results;
            if (gridApplications.Columns["Cost"] is { } costColumn)
                costColumn.DefaultCellStyle.Format = "N0";
            lblStatus.Text = $"{results.Rows.Count} application(s) found.";
        }
        catch (SqlException ex)
        {
            lblStatus.Text = "Database error.";
            MessageBox.Show(this,
                "Could not connect to BuildSureDb. Run the MVC app (or 'dotnet ef database update') first.\n\n" + ex.Message,
                "BuildSure", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSearch.Enabled = true;
            UseWaitCursor = false;
        }
    }
}
