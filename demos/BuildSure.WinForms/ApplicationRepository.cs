using System.Data;
using Microsoft.Data.SqlClient;

namespace BuildSure.WinForms;

/// <summary>
/// Plain ADO.NET data access (no EF Core) - the style you will often meet in older desktop apps.
/// Always use parameters (@Search) instead of string concatenation to avoid SQL injection.
/// </summary>
public class ApplicationRepository(string connectionString)
{
    public async Task<DataTable> SearchAsync(string searchText)
    {
        const string sql = """
            SELECT  a.ApplicationNumber AS [Number],
                    c.FullName          AS [Customer],
                    a.ProjectName       AS [Project],
                    a.NumberOfFloors    AS [Floors],
                    a.ConstructionCost  AS [Cost],
                    a.Status,
                    a.SubmissionDate    AS [Submitted (UTC)],
                    a.CertificateNumber AS [Certificate]
            FROM    dbo.Applications AS a
            INNER JOIN dbo.Customers AS c ON c.Id = a.CustomerId
            WHERE   @Search = N''
               OR   a.ApplicationNumber LIKE N'%' + @Search + N'%'
               OR   a.ProjectName       LIKE N'%' + @Search + N'%'
               OR   c.FullName          LIKE N'%' + @Search + N'%'
            ORDER BY a.SubmissionDate DESC;
            """;

        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = searchText.Trim();

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        var table = new DataTable();
        table.Load(reader); // DataTable is easy to bind to a DataGridView
        return table;
    }
}
