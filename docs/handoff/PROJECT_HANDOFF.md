# BuildSure – Project Handoff

> **For a new Claude Code (or human) session:** read this file and
> [SESSION_HANDOFF.md](SESSION_HANDOFF.md) **before doing any work**. Then update both files, and
> [CHANGELOG.md](CHANGELOG.md), at the end of every significant work session. Never record
> something as completed or tested unless it was actually verified.

## 1. Purpose

BuildSure is a **training simulation** of a construction-insurance management system. The
owner uses it to prepare for a .NET developer technical interview. They are moving from
Node.js/Express and Java Spring Boot to ASP.NET Core MVC. The main goal is to demonstrate the
technologies below in small, working, well-explained code. It is **not** a real insurance system:
no real insurance calculations, simulated certificate numbers.

## 2. Requirements (agreed scope)

1. **Dashboard:** total, pending, approved and rejected counts, plus recent applications.
2. **Customers CRUD:** Full Name, National ID, Phone, Email, Address.
3. **Applications CRUD:** Number, Customer, Project Name and Address, Floors, Construction Cost, Submission Date,
   Status (Pending/Approved/Rejected), Notes. Search and filter by status.
4. **Workflow:** submit as Pending, employee reviews, approve or reject. Approval generates a simulated certificate number.
5. **MVC concepts:** Models, Views, Controllers, ViewModels, Services (only where needed), DbContext, DI, Data
   Annotations, validation, LINQ, async/await, Razor, strongly typed views.
6. **EF Core:** DbContext, DbSet, Code-First, migrations, one-to-many, LINQ, `Include`, `AsNoTracking`, `SaveChangesAsync`.
7. **SQL Server scripts:** SELECT/INSERT/UPDATE/DELETE, INNER JOIN, GROUP BY, stored procedure(s), view(s),
   PK/FK, indexes, and how they compare with LINQ.
8. **Frontend:** Bootstrap. jQuery for AJAX, confirmation dialogs, dynamic status updates, simple interactions.
9. **Legacy Web Forms demo** (separate project): one ASPX page, code-behind, server controls, click event, PostBack, ViewState.
10. **Windows Forms demo** (separate project): TextBox, Button, DataGridView, SQL integration.
11. **IIS deployment guidance:** publish, Hosting Bundle, site and app pool, web.config, troubleshooting.
12. **Git** with meaningful commits. A **public GitHub repo** under the `ali-kassab` account, on the `main` branch.
13. **Documentation:** README, learning guide, and the handoff files in this folder.

Out of scope unless the owner asks: authentication and roles, real insurance logic, extra features.

## 3. Architecture and technology

See [../architecture.md](../architecture.md) for diagrams and decisions. In short:

- **.NET 10 (LTS)**, ASP.NET Core MVC, EF Core 10 (SqlServer provider), SQL Server **LocalDB**
  (`(localdb)\MSSQLLocalDB`, database `BuildSureDb`).
- Layers: Controllers → (ApplicationService for workflow rules) → BuildSureDbContext → SQL Server.
  Customers and Reports controllers use the DbContext directly. There is no repository layer.
- Bootstrap 5 and jQuery 3 (bundled by the MVC template in `wwwroot/lib`), jQuery Validation Unobtrusive.
- Tests: xUnit and the EF Core InMemory provider.
- Web Forms: .NET Framework 4.8 classic web project, built with VS 2022 MSBuild, run with IIS Express. It is listed in
  `BuildSure.sln` but excluded from the solution build (no `Build.0` entries), so `dotnet build BuildSure.sln` skips it.
- WinForms: .NET 10 Windows, Microsoft.Data.SqlClient (ADO.NET).

## 4. Implemented features (all verified, see SESSION_HANDOFF for how)

| Feature | Location |
|---|---|
| Dashboard with AJAX refresh | `HomeController`, `Views/Home/Index.cshtml` |
| Customers CRUD + search + unique National ID + delete protection | `CustomersController`, `Views/Customers/*` |
| Applications CRUD + AJAX live search/status filter (partial view) | `ApplicationsController`, `Views/Applications/*` |
| Approve / Reject via AJAX with Bootstrap confirmation modal | `Details.cshtml`, `site.js`, `ApplicationService` |
| SQL view + stored procedure consumed by the app | `ReportsController`, `Views/Reports/Index.cshtml` |
| Seed data in Development | `Data/DbSeeder.cs` |
| 11 unit tests for workflow rules | `tests/BuildSure.Tests/ApplicationServiceTests.cs` |
| SQL scripts 00–04 | `database/` |
| Web Forms demo | `demos/BuildSure.WebForms` |
| WinForms demo | `demos/BuildSure.WinForms` |
| Docs | `README.md`, `project-overview.md`, `docs/*` |

## 5. Project structure

```
BuildSure/
├── BuildSure.sln        Web, Tests, WinForms (built) + WebForms (listed, not built by the sln)
├── README.md, project-overview.md, CLAUDE.md
├── src/BuildSure.Web/   Program.cs, appsettings*.json, Models/, Data/(DbContext, DbSeeder, Migrations/),
│                        Services/, ViewModels/, Controllers/, Views/, wwwroot/(css, js/site.js, lib/)
├── tests/BuildSure.Tests/
├── database/            00_schema_generated_by_ef.sql … 04_crud_joins_groupby.sql
├── demos/BuildSure.WebForms/   Default.aspx(.cs/.designer.cs), Web.config, csproj
├── demos/BuildSure.WinForms/   ApplicationSearchForm(.Designer).cs, ApplicationRepository.cs, Program.cs
└── docs/                architecture.md, LEARNING_GUIDE.md, IIS_DEPLOYMENT.md, handoff/
```

## 6. Database design

Two tables: `Customers` (1) → `Applications` (many), joined by FK `CustomerId` with `ON DELETE NO ACTION`.
Unique indexes: `Customers.NationalId`, `Applications.ApplicationNumber`, and a filtered unique
index on `Applications.CertificateNumber`. Non-unique indexes: `Status` and `CustomerId`.
`Status` is stored as nvarchar text. Migrations:

1. `20261124132633_InitialCreate`: tables, keys, indexes.
2. `20261124132641_AddReportingViewAndProcedure`: `vw_CustomerApplicationSummary`, `usp_GetApplicationsByStatus` (raw SQL).

Database objects created only by the scripts, not by migrations: `usp_GetApplicationStatistics`,
`usp_ApproveApplication`, `IX_Applications_SubmissionDate`. A database built only from migrations
won't have them until `database/01` and `database/03` are run.

## 7. Important technical decisions

- Workflow rules live in one service and return `OperationResult` rather than throwing.
- Application forms use a ViewModel; customer forms use `[Bind]`. Both prevent over-posting.
- AJAX endpoints detect `X-Requested-With: XMLHttpRequest`. They return JSON or a partial view, and fall back to redirects/full pages otherwise.
- Development auto-migrates and seeds (`Database:MigrateAndSeedOnStartup=true` in `appsettings.Development.json`). It's off in base `appsettings.json`.
- SQL scripts set `QUOTED_IDENTIFIER ON`, because the filtered unique index needs it and classic `sqlcmd` defaults it to OFF.
- The connection string uses Windows integrated auth, so the repo contains no password.

## 8. Known issues and limitations

- **No authentication or authorisation.** Any user can approve or reject.
- **IIS deployment not tested.** IIS isn't installed on the dev machine. `dotnet publish` and running the published output in Production were tested.
- Unit tests use the InMemory provider. They don't exercise SQL Server FKs, unique indexes, the view or the SP; those were verified manually in the browser and with sqlcmd.
- WinForms was verified through Windows UI Automation: the status label row counts, and the DataGridView cell values
  (read through the cells' UIA `ValuePattern`). There are no automated WinForms or Web Forms tests in `dotnet test`.
- VS 2022 MSBuild can't build the whole `BuildSure.sln`, because it resolves the .NET 9 SDK and the other projects target
  .NET 10. Use `dotnet build` for the solution and VS 2022 MSBuild for `BuildSure.WebForms.csproj` alone. The VS 2026 ("18")
  Community install on the dev machine has no MSBuild component.
- The application-number suffix is random (6 hex characters). A collision would raise a unique-index error and isn't retried.
- LocalDB doesn't work under IIS; a server deployment needs SQL Server.
- Razor views are compiled at build time, so view changes need an app restart (no runtime compilation package).

## 9. Remaining work

None within the agreed scope. Possible future work, only if the owner asks:
- ASP.NET Core Identity with a Reviewer role.
- Integration tests against SQL Server (e.g. Testcontainers or LocalDB).
- An actual IIS deployment test on a machine with IIS.
