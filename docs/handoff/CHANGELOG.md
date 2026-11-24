# Changelog

All meaningful changes, oldest first. Dates are 2026-11-24 (local time) unless stated otherwise.

## 2026-11-24

- **Scaffold** (`5178b1b`): `BuildSure.sln` with the ASP.NET Core MVC app (`net10.0`), the xUnit test project, the
  WinForms project, and the `dotnet new gitignore` file. Git initialised on `main`.
- **MVC application** (`5465069`):
  - EF Core Code-First model (`Customer`, `InsuranceApplication`, `ApplicationStatus`), DbContext with Fluent API.
  - Migrations `InitialCreate` and `AddReportingViewAndProcedure` (SQL view + stored procedure).
  - `ApplicationService` with the Pending → Approved/Rejected workflow and simulated certificate numbers.
  - Controllers: Home (dashboard + JSON stats), Customers, Applications, Reports.
  - Razor views with Bootstrap. jQuery for AJAX approve/reject, live filter, confirmation modal,
    dashboard refresh and a notes character counter.
  - Development seeding and startup migration.
- **Unit tests** (`4f42c5b`): 11 xUnit tests for the workflow rules (EF Core InMemory).
- **SQL scripts** (`ed80417`): `database/00`–`04`, covering EF-generated schema, keys and indexes, view, stored procedures,
  and CRUD/JOIN/GROUP BY. Data changes are rolled back.
- **Web Forms demo** (`7b43fd4`): .NET Framework 4.8 `Default.aspx` showing server controls, click event, PostBack
  and ViewState, plus ADO.NET calling the stored procedure.
- **Windows Forms demo** (`4d43114`): search screen with TextBox, Button and DataGridView, using a parameterised ADO.NET query.
- **Docs** (`0bfa654`): `README.md`, `docs/LEARNING_GUIDE.md`, `docs/IIS_DEPLOYMENT.md`.
- **Handoff and GitHub**: added `project-overview.md`, `docs/architecture.md`, `docs/handoff/*`,
  and `CLAUDE.md` (read-the-handoff rule). Ignored the machine-specific `.claude/launch.json`. Created the public
  GitHub repository https://github.com/ali-kassab/BuildSure and pushed `main` (`6d8b703`).
- **Handoff update**: recorded the repository URL, pushed hash and verification results in `SESSION_HANDOFF.md`.
