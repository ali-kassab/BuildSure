# Session Handoff

> Read [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md) first, then this file.

## Latest session: 2026-11-24 (Web Forms / WinForms demo re-verification)

### Context

The owner asked for the two remaining demos (Web Forms on .NET Framework, Windows Forms) to be built. They already
existed from the previous session (`7b43fd4`, `4d43114`). This session re-verified them in the current environment
and fixed the one structural gap: Web Forms was not in `BuildSure.sln`. **No application code was changed**; the MVC
app is untouched.

### Environment checked

- .NET SDKs 9.0.318 / 10.0.204 / 10.0.401. .NET Framework 4.8 runtime (release 533509) and the 4.8 targeting pack.
- VS 2022 Community MSBuild (with WebApplication targets), IIS Express, LocalDB `MSSQLLocalDB` v17, `sqlcmd`.
- VS 2026 ("18") Community is installed **without** MSBuild. Python isn't installed.

### Done

1. `BuildSure.sln`: added `demos\BuildSure.WebForms\BuildSure.WebForms.csproj` (GUID `{6C1B5E2A-…1A40}`) under the
   `demos` folder, with `ActiveCfg` entries only (no `Build.0`). `dotnet build` therefore skips it.
2. Docs: README (structure + Web Forms section), `docs/architecture.md`, `PROJECT_HANDOFF.md` (§3, §5, §8), `CHANGELOG.md`.

### Files changed

`BuildSure.sln`, `README.md`, `docs/architecture.md`, `docs/handoff/PROJECT_HANDOFF.md`,
`docs/handoff/SESSION_HANDOFF.md`, `docs/handoff/CHANGELOG.md`.

### Verification (actually run this session)

| Check | Result |
|---|---|
| `dotnet build BuildSure.sln` (before and after the sln change) | Succeeded, 0 warnings, 0 errors |
| `dotnet sln list` | 4 projects, including `BuildSure.WebForms` |
| `dotnet test` | Passed 11 / 11 |
| VS 2022 MSBuild `BuildSure.WebForms.csproj -restore` | Exit code 0 |
| VS 2022 MSBuild on the whole `BuildSure.sln` | **Fails** (NETSDK1045: it resolves SDK 9 and can't target .NET 10). Expected; documented in PROJECT_HANDOFF §8 |
| Web Forms under IIS Express, port 5310 (scripted HTTP GET/POST with `__VIEWSTATE` / `__EVENTVALIDATION`) | First GET: `IsPostBack = false`, 4 rows. Search "Omar": `IsPostBack = true`, 2 rows. Status "Approved": 2 rows (matches `SELECT COUNT(*) … WHERE Status='Approved'` = 2). Counter after 2 clicks = 2. "Set both labels" set both labels; after a plain PostBack the ViewState label kept its value, the non-ViewState label went back to "(not set)", the counter stayed 2, and the GridView kept its rows without a DB query |
| WinForms (UI Automation) | Window title correct. Load: "4 application(s) found." Search "Omar" via Button Invoke: "2 application(s) found." DataGridView cell values read through `ValuePattern` (e.g. `APP-SEED-0002 | Omar Haddad | Hamra Office Block | 12 | 5,750,000 | Pending`) |

### Unresolved

- Same as the previous session: no auth (by design), IIS untested, no SQL Server integration tests.
- No automated tests for either demo; their checks above were scripted by hand, not committed.

### Next steps

Same as the list in the previous session below. Expect 11 passing tests and 4 projects in `dotnet sln list`.

## Previous session: 2026-11-24 (initial build + GitHub publication)

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

| Item | Value (verified 2026-11-24) |
|---|---|
| Repository | https://github.com/ali-kassab/BuildSure |
| Visibility / default branch | Public / `main` (checked with `gh repo view`) |
| Pushed by | GitHub CLI account `ali-kassab` (`gh auth status`) |
| First push: last commit | `6d8b7033f8ee4ed6e159484bbfa6c037e8ffa3c3` ("Add handoff docs, architecture, project overview and session rules") |
| Remote matches local | `git rev-parse main` = `git rev-parse origin/main` = `git ls-remote origin refs/heads/main` = `6d8b703…`. The GitHub API shows 8 commits on `main`, attributed to `ali-kassab` |
| Working tree after push | Clean (`git status`) |
| Pre-push secret scan | `git grep` over tracked files and `git log -p --all` for passwords, user ids, API keys, tokens, private keys and cloud keys. Only placeholder text in docs matched. Connection strings use Windows integrated auth (no credentials). No bin/obj, database files, `.env`, `.user` or publish output tracked |

This file was then updated with the table above and pushed as one more commit, "Record GitHub publication
details in session handoff". That commit is the current `main` head. Use `git log -1` to get its hash. It can't
contain its own hash.
