# BuildSure – Construction Insurance Management (training project)

> **Training simulation only.** BuildSure is not a real insurance system. It has no real premium
> calculations, and its certificate numbers (`SIM-CERT-…`) are simulated.

BuildSure is a small ASP.NET Core MVC app for practising for a .NET developer interview. It models
an organisation that manages insurance applications for construction projects:

1. A customer submits an application. It starts as **Pending**.
2. An employee reviews it, then **approves** or **rejects** it. A rejection needs a reason.
3. An approved application gets a **simulated certificate number**.

The repository also has a small legacy **ASP.NET Web Forms** demo, a **Windows Forms** demo and
**SQL Server scripts**. Each one is separate from the main app.

---

## Technologies

| Area | Used for |
|---|---|
| C# / .NET 10 (LTS) | Main MVC app, unit tests, Windows Forms demo |
| ASP.NET Core MVC | Controllers, Razor views, model binding, validation, DI |
| Entity Framework Core 10 | Code-First, migrations, LINQ, `Include`, `AsNoTracking`, `SaveChangesAsync` |
| SQL Server LocalDB | Database (`(localdb)\MSSQLLocalDB`, database `BuildSureDb`) |
| T-SQL | View, stored procedures, keys, indexes, CRUD/JOIN/GROUP BY scripts |
| Razor, HTML, CSS, Bootstrap 5 | Responsive UI |
| jQuery (+ jQuery Validation Unobtrusive) | AJAX, confirmation dialogs, live filter, client-side validation |
| xUnit + EF Core InMemory | Unit tests for the workflow rules |
| ASP.NET Web Forms (.NET Framework 4.8) | Legacy demo: PostBack, ViewState, server controls |
| Windows Forms (.NET 10) + ADO.NET | Desktop search demo |
| IIS / ASP.NET Core Module | Deployment guide ([docs/IIS_DEPLOYMENT.md](docs/IIS_DEPLOYMENT.md)) |

---

## Project structure

```
BuildSure/
├── BuildSure.sln                      # MVC app + tests + WinForms (Web Forms is built separately)
├── src/BuildSure.Web/                 # ★ the main ASP.NET Core MVC application
│   ├── Program.cs                     # DI registrations + middleware pipeline (start here)
│   ├── appsettings*.json              # connection string, startup migration flag, logging
│   ├── Models/                        # entities: Customer, InsuranceApplication, ApplicationStatus, report rows
│   ├── Data/
│   │   ├── BuildSureDbContext.cs      # DbContext, DbSets, Fluent API (indexes, relationship, enum→string)
│   │   ├── DbSeeder.cs                # sample data for an empty database
│   │   └── Migrations/                # InitialCreate + AddReportingViewAndProcedure (raw SQL)
│   ├── Services/                      # IApplicationService / ApplicationService = workflow rules
│   ├── ViewModels/                    # form + page models (prevent over-posting, shape data for views)
│   ├── Controllers/                   # Home (dashboard), Customers, Applications, Reports
│   ├── Views/                         # Razor views, partials (_ApplicationTable, _StatusBadge, ...)
│   └── wwwroot/js/site.js             # all jQuery behaviour
├── tests/BuildSure.Tests/             # xUnit tests for ApplicationService
├── database/                          # SQL Server scripts (00–04)
├── demos/
│   ├── BuildSure.WebForms/            # .NET Framework 4.8 Web Forms (Default.aspx + code-behind)
│   └── BuildSure.WinForms/            # .NET 10 Windows Forms search screen
└── docs/
    ├── LEARNING_GUIDE.md              # request flow walkthroughs, EF/SQL concepts, Node/Spring comparisons
    └── IIS_DEPLOYMENT.md              # publish + IIS setup + troubleshooting
```

---

## Prerequisites

- **.NET SDK 10** (`dotnet --list-sdks`)
- **SQL Server LocalDB** (`sqllocaldb info` should list `MSSQLLocalDB`). It's installed with Visual
  Studio's "Data storage and processing" workload or SQL Server Express.
- **EF Core CLI**: `dotnet tool install --global dotnet-ef`
- Optional: **Visual Studio 2022+** with the *ASP.NET and web development* workload and
  *.NET Framework 4.8 targeting pack* (needed only for the Web Forms demo)

## Run the MVC application

```powershell
cd D:\Workspace\BuildSure
dotnet run --project src/BuildSure.Web --launch-profile http
```

Open <http://localhost:5299>.

In **Development**, `appsettings.Development.json` sets `Database:MigrateAndSeedOnStartup = true`.
On startup the app then creates the database, applies all migrations and inserts sample data.
Executed SQL is printed to the console, so you can watch what EF Core does.

### Configure SQL Server

The connection string is in `src/BuildSure.Web/appsettings.json`:

```json
"ConnectionStrings": {
  "BuildSureDb": "Server=(localdb)\\MSSQLLocalDB;Database=BuildSureDb;Trusted_Connection=True;MultipleActiveResultSets=false;TrustServerCertificate=True"
}
```

To use a full SQL Server instance, change it to something like
`Server=localhost;Database=BuildSureDb;Trusted_Connection=True;TrustServerCertificate=True`. For SQL
logins, use `User Id=...;Password=...` and keep the password out of Git (user secrets or an
environment variable `ConnectionStrings__BuildSureDb`).

### Migrations (EF Core CLI)

Run these from `src/BuildSure.Web`:

```powershell
dotnet ef migrations list                      # show migrations and whether they are applied
dotnet ef database update                      # create/upgrade the database
dotnet ef migrations add <Name> -o Data/Migrations   # after changing a model
dotnet ef migrations script --idempotent -o ..\..\database\00_schema_generated_by_ef.sql
dotnet ef database drop                        # start over (asks for confirmation)
```

## Run the tests

```powershell
dotnet test
```

11 tests cover the workflow rules: a new application is Pending, approval generates a
certificate, an application can't be approved twice, a rejection needs a reason, only Pending
applications can be edited, approved applications can't be deleted, plus dashboard counts and
search.

> Stop the running web app first. Otherwise the build can't overwrite `BuildSure.Web.exe`.

## How to test each feature manually

| Feature | Steps | Expected |
|---|---|---|
| Dashboard | Open `/` | Totals per status and the 5 most recent applications. **Refresh** reloads the numbers with AJAX. Cards link to filtered lists. |
| Create customer | Customers → New customer → submit empty | Client-side validation messages appear (jQuery Validation). |
| Unique National ID | Create a customer with National ID `1985031200` | Server-side error "Another customer already uses this National ID." |
| Customer details / edit | Click a customer → Edit | Changes saved. A green TempData message appears after the redirect. |
| Delete customer | Delete a customer who has applications | Blocked with an error message. A customer with no applications is deleted after the confirmation dialog. |
| Create application | Applications → New application, or "New application" on a customer page | Status **Pending**. An `APP-yyyyMMdd-XXXXXX` number and the submission date are generated. |
| Validation | Floors `500`, cost `5` | "Number of floors must be between 1 and 200", "Construction cost must be between …" |
| Search / filter | Applications list: type in the search box or choose a status | The table reloads with AJAX (partial view) and the URL updates. It also works without JS. |
| Edit application | Edit a Pending application | Saved. Opening `/Applications/Edit/{id}` for an approved one redirects with an error. |
| Approve | Details of a Pending application → Approve → confirm | The badge turns green and a `SIM-CERT-…` number appears without a page reload. Edit/Delete disappear. |
| Reject | Enter a reason → Reject → confirm | The badge turns red and the reason is shown. Rejecting with an empty reason is blocked. |
| Delete application | Delete a Pending or Rejected one | Deleted after confirmation. Approved applications can't be deleted. |
| SQL reports | SQL Reports | Top table comes from the SQL **view**. Bottom table comes from the **stored procedure** (status filter = SP parameter). |

## SQL Server scripts

Run them in order in SSMS, Azure Data Studio or `sqlcmd` after the database exists:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i database\01_keys_and_indexes.sql
```

| Script | Contents |
|---|---|
| `00_schema_generated_by_ef.sql` | Idempotent DDL generated from the migrations (tables, PK, FK, indexes, view, SP) |
| `01_keys_and_indexes.sql` | Hand-written DDL explained, catalog queries for PK/FK/indexes, an extra non-clustered index |
| `02_view_customer_application_summary.sql` | `vw_CustomerApplicationSummary` (LEFT JOIN + GROUP BY) |
| `03_stored_procedures.sql` | `usp_GetApplicationsByStatus`, `usp_GetApplicationStatistics`, `usp_ApproveApplication` |
| `04_crud_joins_groupby.sql` | SELECT / INSERT / UPDATE / DELETE / INNER JOIN / GROUP BY / HAVING, each with the matching LINQ |

Scripts 03 and 04 make their data changes inside a transaction that is **rolled back**, so you
can run them repeatedly without changing your data.

## Legacy Web Forms demo (`demos/BuildSure.WebForms`)

This is a .NET Framework 4.8 project, so build it with Visual Studio or Visual Studio's MSBuild.
`dotnet build` won't work.

- **Visual Studio:** File → Open → Project → `BuildSure.WebForms.csproj`, then press F5. It runs on IIS Express at http://localhost:5310.
- **Command line:**
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" demos\BuildSure.WebForms\BuildSure.WebForms.csproj
  & "C:\Program Files\IIS Express\iisexpress.exe" /path:D:\Workspace\BuildSure\demos\BuildSure.WebForms /port:5310
  ```
  Open <http://localhost:5310/Default.aspx>.

The page demonstrates:
- **Server controls:** `asp:TextBox`, `asp:DropDownList`, `asp:Button`, `asp:GridView`, `asp:Label`.
- **Button click event:** `btnSearch_Click` calls the `usp_GetApplicationsByStatus` stored procedure with ADO.NET.
- **PostBack:** `IsPostBack`.
- **ViewState:** a click counter, and two labels, one with ViewState and one without.

## Windows Forms demo (`demos/BuildSure.WinForms`)

```powershell
dotnet run --project demos/BuildSure.WinForms
```

You can also open it in Visual Studio to use the visual designer. The window has a TextBox, a
Search button (Enter also works) and a DataGridView. Results come from SQL Server through a
parameterised ADO.NET query with an INNER JOIN. It needs the `BuildSureDb` database, so run the
MVC app once first.

## Publish to IIS

See **[docs/IIS_DEPLOYMENT.md](docs/IIS_DEPLOYMENT.md)**. In short:

```powershell
dotnet publish src/BuildSure.Web -c Release -o C:\inetpub\BuildSure
```

Then install the ASP.NET Core **Hosting Bundle**, create a site whose app pool is set to
*No Managed Code*, and give the pool's identity access to SQL Server.

## Learning guide

**[docs/LEARNING_GUIDE.md](docs/LEARNING_GUIDE.md)** walks through what happens on every
important click (Browser → Controller → Model Binding → Validation → Service → DbContext →
SQL Server → Redirect/View). It also compares each piece to Express and Spring Boot.

## More documentation

- [project-overview.md](project-overview.md): a one-page summary
- [docs/architecture.md](docs/architecture.md): layers, data model, workflow, design decisions
- [docs/handoff/PROJECT_HANDOFF.md](docs/handoff/PROJECT_HANDOFF.md) and [SESSION_HANDOFF.md](docs/handoff/SESSION_HANDOFF.md): read these first when continuing the work
- [docs/handoff/CHANGELOG.md](docs/handoff/CHANGELOG.md)

## Known limitations

- **No authentication or roles.** Anyone using the app acts as the "employee". Adding ASP.NET Core
  Identity with `[Authorize(Roles = "Reviewer")]` would be the natural next step.
- Times are stored in **UTC** and shown in the server's local time zone.
- The application number uses a random 6-character suffix. The unique index guarantees no
  duplicates are stored, and a collision would surface as an error (extremely unlikely at this scale).
- Full IIS was not installed on the development machine, so **IIS deployment was not tested**.
  `dotnet publish` and running the published output were tested.
