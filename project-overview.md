# BuildSure – Project Overview

**One line:** BuildSure is a small, beginner-friendly *Construction Insurance Management* training app. It's
built with ASP.NET Core MVC and EF Core on SQL Server, to practise for a .NET developer interview.

> Training simulation only. It's not a real insurance system and has no real insurance calculations.
> Certificate numbers are simulated.

## What it does

- **Dashboard:** total, pending, approved and rejected application counts, plus the five most recent applications.
- **Customers:** full CRUD with a unique National ID.
- **Insurance applications:** full CRUD, search, and a status filter.
- **Workflow:** an application is submitted as Pending. An employee then approves it, which generates a simulated certificate number, or rejects it, which requires a reason.
- **SQL reports:** one page reads a SQL **view** and calls a **stored procedure**.

## Where things are

| Path | What |
|---|---|
| `src/BuildSure.Web` | Main ASP.NET Core MVC app (.NET 10) |
| `tests/BuildSure.Tests` | xUnit tests for the workflow rules |
| `database/` | T-SQL scripts (keys and indexes, view, stored procedures, CRUD/JOIN/GROUP BY) |
| `demos/BuildSure.WebForms` | Legacy ASP.NET Web Forms demo (.NET Framework 4.8) |
| `demos/BuildSure.WinForms` | Windows Forms search demo (.NET 10) |
| `docs/LEARNING_GUIDE.md` | Request-flow walkthroughs for learners |
| `docs/architecture.md` | Architecture, data model, design decisions |
| `docs/IIS_DEPLOYMENT.md` | Publishing to IIS |
| `docs/handoff/` | Project and session handoff notes, changelog |

## Quick start

```powershell
dotnet run --project src/BuildSure.Web --launch-profile http   # http://localhost:5299
dotnet test
```

Full instructions are in [README.md](README.md). If you're continuing the work, read
[docs/handoff/PROJECT_HANDOFF.md](docs/handoff/PROJECT_HANDOFF.md) and
[docs/handoff/SESSION_HANDOFF.md](docs/handoff/SESSION_HANDOFF.md) first.
