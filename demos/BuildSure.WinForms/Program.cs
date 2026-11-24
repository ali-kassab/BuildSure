namespace BuildSure.WinForms;

static class Program
{
    // Same LocalDB database the MVC app uses. Change it here if your SQL Server is different.
    public const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=BuildSureDb;Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>The main entry point for the application.</summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize(); // high DPI, visual styles, default font
        Application.Run(new ApplicationSearchForm());
    }
}
