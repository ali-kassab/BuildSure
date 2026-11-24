using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace BuildSure.WebForms
{
    /// <summary>
    /// Code-behind for Default.aspx. Web Forms is event-driven (like a desktop app):
    /// the page runs a life cycle on every request (Init -> Load -> control events -> PreRender -> Render),
    /// and button clicks arrive as server-side event handlers.
    /// </summary>
    public partial class _Default : Page
    {
        // Typed wrapper around ViewState: the value is serialized into the hidden __VIEWSTATE field.
        private int Clicks
        {
            get { return ViewState["Clicks"] == null ? 0 : (int)ViewState["Clicks"]; }
            set { ViewState["Clicks"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Page_Load runs on EVERY request - first load and every PostBack.
            if (!IsPostBack)
            {
                // First visit only: initial data binding. On PostBacks the GridView
                // rebuilds itself from ViewState, so we don't query the database again.
                BindApplications(null, null);
                lblPostBack.Text = "IsPostBack = false. This is the first GET request for the page.";
            }
            else
            {
                lblPostBack.Text = "IsPostBack = true. The form was posted back to the same page at " +
                                   DateTime.Now.ToString("HH:mm:ss") + ".";
            }
        }

        // Button click event: wired in markup with OnClick="btnSearch_Click".
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindApplications(txtSearch.Text.Trim(), ddlStatus.SelectedValue);
        }

        protected void btnIncrement_Click(object sender, EventArgs e)
        {
            Clicks++;
            lblClicks.Text = Clicks.ToString();
        }

        protected void btnSetLabels_Click(object sender, EventArgs e)
        {
            string value = "Set at " + DateTime.Now.ToString("HH:mm:ss");
            lblWithViewState.Text = value;
            lblWithoutViewState.Text = value;
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            // Show the current ViewState counter even when another button caused the PostBack.
            lblClicks.Text = Clicks.ToString();
        }

        /// <summary>
        /// Classic ADO.NET: SqlConnection + SqlCommand + SqlDataAdapter filling a DataTable.
        /// Calls the same stored procedure the MVC Reports page uses.
        /// </summary>
        private void BindApplications(string search, string status)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["BuildSureDb"].ConnectionString;
                var table = new DataTable();

                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand("dbo.usp_GetApplicationsByStatus", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value =
                        string.IsNullOrEmpty(status) ? (object)DBNull.Value : status;

                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(table); // opens and closes the connection for us
                    }
                }

                // Free-text filter applied in memory with DataView.RowFilter (escape quotes for the filter syntax).
                if (!string.IsNullOrEmpty(search))
                {
                    string term = search.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
                    table.DefaultView.RowFilter = string.Format(
                        "ApplicationNumber LIKE '%{0}%' OR ProjectName LIKE '%{0}%' OR CustomerName LIKE '%{0}%'", term);
                }

                gvApplications.DataSource = table.DefaultView;
                gvApplications.DataBind();
                lblSearchResult.CssClass = "muted";
                lblSearchResult.Text = table.DefaultView.Count + " application(s) found.";
            }
            catch (SqlException ex)
            {
                lblSearchResult.CssClass = "error";
                lblSearchResult.Text = "Could not reach the BuildSure database. Run the MVC app migrations first. (" + ex.Message + ")";
            }
        }
    }
}
