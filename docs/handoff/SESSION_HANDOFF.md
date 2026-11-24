# Session Handoff

> Read [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md) first, then this file.

## Latest session: 2026-11-24 (initial build + GitHub publication)

### Completed

1. Inspected the environment:
   - .NET SDKs 9.0.318, 10.0.204 and 10.0.401
   - SQL Server LocalDB `MSSQLLocalDB` (v17)
   - `sqlcmd` (ODBC 18 tools)
   - VS 2022 Community MSBuild with WebApplication targets, plus VS 2026 Community installed
   - IIS Express present; full IIS (W3SVC) **not** installed
   - Chose .NET 10 (LTS)
2. Built the ASP.NET Core MVC app (dashboard, customers, applications, workflow, SQL reports).
3. Added EF Core migrations (including the view and stored procedure) and applied them to LocalDB `BuildSureDb`.
4. Added 11 xUnit tests.
5. Added the SQL scripts `database/00`–`04`.
6. Added the Web Forms demo (.NET Framework 4.8) and the WinForms demo (.NET 10).
7. Wrote the docs: README, learning guide, IIS guide, architecture, project overview, handoff files, and `CLAUDE.md`.
8. Created the public GitHub repository and pushed `main` (details below).

### Files created or modified (this session)

Everything in the repository was created this session. The main files:

- `src/BuildSure.Web/`:
  - `Program.cs`, `appsettings.json`, `appsettings.Development.json`
  - `Models/*`, `Data/*` (including `Migrations/*`), `Services/*`, `ViewModels/*`, `Controllers/*`
  - `Views/**/*.cshtml`, `wwwroot/css/site.css`, `wwwroot/js/site.js`
- `tests/BuildSure.Tests/ApplicationServiceTests.cs`
- `database/00_schema_generated_by_ef.sql` … `04_crud_joins_groupby.sql`
- `demos/BuildSure.WebForms/*`, `demos/BuildSure.WinForms/*`
- `README.md`, `project-overview.md`, `CLAUDE.md`, `.gitignore`, `docs/**`

### Commands executed (main ones)

```powershell
dotnet new sln/mvc/xunit/winforms/gitignore; dotnet sln add ...
dotnet add package Microsoft.EntityFrameworkCore.SqlServer|Design|InMemory (10.0.*), Microsoft.Data.SqlClient
dotnet tool install --global dotnet-ef --version 10.0.*
dotnet ef migrations add InitialCreate -o Data/Migrations
dotnet ef migrations add AddReportingViewAndProcedure -o Data/Migrations   # body hand-written
dotnet ef database update
dotnet ef migrations script --idempotent -o database/00_schema_generated_by_ef.sql
dotnet build BuildSure.sln
dotnet test
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -i database\0X_*.sql            # scripts 01-04
MSBuild.exe demos\BuildSure.WebForms\BuildSure.WebForms.csproj             # VS 2022 MSBuild
iisexpress.exe /path:...\demos\BuildSure.WebForms /port:5310 /clr:v4.0
dotnet publish src/BuildSure.Web -c Release -o <scratch folder>
gh repo create ali-kassab/BuildSure --public --source . --remote origin --push
```

### Build and test results (actually run)

| Check | Result |
|---|---|
| `dotnet build BuildSure.sln` | **Succeeded, 0 warnings, 0 errors** |
| `dotnet test` | **Passed 11 / 11** (see the test list below) |
| Web Forms MSBuild (VS 2022) | **Succeeded** |
| SQL scripts 01–04 via `sqlcmd -b` | **All exit code 0.** Demo writes in 03/04 rolled back; data was unchanged afterwards |
| MVC browser checks (dev server, LocalDB) | All verified: dashboard; customer create with client and server validation and the duplicate National ID error; customer edit; customer delete, both blocked and allowed; application create with validation messages; edit; edit blocked when not Pending; AJAX approve with confirmation (certificate shown); AJAX reject; AJAX filter and live search; delete with confirmation; delete of an approved application blocked; SQL Reports (view + SP, with and without a status); JSON stats refresh; 404s; anti-forgery missing → 400 |
| README run path on a **fresh** database | Verified: a temporary DB `BuildSureDb_ReadmeCheck` was created, migrated and seeded (4 applications), all pages returned 200, then the DB was dropped |
| `dotnet ef migrations list` | Both migrations listed |
| Web Forms under IIS Express | Verified: initial load (4 rows from the SP), search postback (2 rows), ViewState counter, the label with ViewState kept its value and the one without reset |
| WinForms (UI Automation) | Verified: load shows "4 application(s) found." Searching "Omar" shows "2 application(s) found." |
| `dotnet publish -c Release` + run in Production (Kestrel) | Verified: `web.config` generated; `/`, `/Applications`, `/Reports` → 200; unknown id → 404 |
| Deployment to real IIS | **Not tested.** IIS isn't installed |

Unit tests run:
- `Create_StartsAsPending_WithGeneratedNumberAndSubmissionDate`
- `Approve_PendingApplication_GeneratesSimulatedCertificate`
- `Approve_AlreadyApproved_Fails`
- `Reject_WithoutReason_Fails` (3 cases: null, "", "   ")
- `Reject_WithReason_SetsRejectedAndNoCertificate`
- `Update_NonPendingApplication_Fails`
- `Delete_ApprovedApplication_Fails_ButRejectedCanBeDeleted`
- `Dashboard_CountsApplicationsByStatus`
- `Search_FiltersByStatusAndText`

### Git / GitHub

- Branch: `main`
- Author and committer on every commit: `ali-kassab`, using the configured global Git email. That email was checked
  to be the verified primary email of GitHub account `ali-kassab`. No AI co-author trailers.
- Repository URL, latest commit hash and push verification: see **Publication record** below.

### Unresolved issues

- No authentication (by design, out of scope).
- IIS deployment untested (no IIS on the machine).
- Tests don't cover SQL Server–specific behaviour (InMemory provider).
- The local `BuildSureDb` contains test data created while verifying (customer "Rana Khoury", an approved
  application `APP-20261124-430061`, a deleted customer "Karim Saleh", one edited phone number) and the extra index
  `IX_Applications_SubmissionDate` from script 01. To reset: `dotnet ef database drop`, then run the app.

### Exact next steps for the next session

1. Read `PROJECT_HANDOFF.md` and this file.
2. `git pull` on `main`, then `dotnet build BuildSure.sln` and `dotnet test`. Expect 11 passing tests.
3. `dotnet run --project src/BuildSure.Web --launch-profile http` and open http://localhost:5299 to confirm the app still runs.
4. Only if the owner asks: practise extensions (Identity + Reviewer role, SQL Server integration tests, real IIS deployment).
   Don't add features otherwise.
5. Update this file, `PROJECT_HANDOFF.md` and `CHANGELOG.md` at the end of the session.

## Publication record

_Filled in after the push. See the next section._
