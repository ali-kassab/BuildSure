# BuildSure Learning Guide

This guide explains how the code runs. It's written for someone coming from **Node.js/Express**
or **Java Spring Boot**. Read it with the code open: each section points to the files involved.

---

## 1. The big picture: MVC in ASP.NET Core

```
Browser ──HTTP──► Kestrel web server ──► Middleware pipeline ──► Routing ──► Controller action
                                                                              │
                                     ┌────────────────────────────────────────┘
                                     ▼
                     Model binding + validation ──► Service (business rules) ──► DbContext (EF Core) ──► SQL Server
                                     │
                                     ▼
                     IActionResult: View(model) → Razor renders HTML   |  Redirect  |  Json  |  PartialView
```

| Concept | ASP.NET Core MVC | Express | Spring Boot |
|---|---|---|---|
| Entry point / config | `Program.cs` | `app.js` / `server.js` | `@SpringBootApplication` + `application.properties` |
| Middleware | `app.UseRouting()`, `app.UseHttpsRedirection()` | `app.use(...)` | Servlet filters |
| Route → handler | `{controller}/{action}/{id?}` convention | `app.get('/customers/:id', ...)` | `@GetMapping("/customers/{id}")` |
| Controller | `class CustomersController : Controller` | router module | `@Controller` class |
| Request body → object | **Model binding** (automatic) | `req.body` (manual) | `@ModelAttribute` / `@RequestBody` |
| Validation | Data Annotations + `ModelState.IsValid` | Joi / express-validator | Bean Validation `@Valid` + `BindingResult` |
| Templates | **Razor** (`.cshtml`, `@Model`) | EJS / Pug | Thymeleaf |
| Dependency Injection | Built in (`builder.Services.Add...`) | none by default | Spring IoC (`@Autowired`, constructor injection) |
| ORM | **EF Core** (`DbContext`, LINQ) | Sequelize / Prisma | JPA / Hibernate (`EntityManager`, JPQL) |
| Config | `appsettings.json` + environment overrides | `.env` | `application.yml` + profiles |

### Dependency Injection lifetimes (see `Program.cs`)

| Registration | Lifetime | Used for |
|---|---|---|
| `AddDbContext<BuildSureDbContext>` | **Scoped** (one per HTTP request) | `DbContext` isn't thread-safe and tracks changes per request |
| `AddScoped<IApplicationService, ApplicationService>` | Scoped | Depends on the scoped DbContext |
| `AddSingleton(TimeProvider.System)` | Singleton (one for the app) | Stateless clock; tests swap in a fixed clock |

Controllers get their dependencies through **constructor injection**. They use C# 12 *primary
constructors*: `public class ApplicationsController(IApplicationService applicationService) : Controller`.
This works the same way as Spring's constructor injection.

---

## 2. Walkthrough: clicking **"Submit application"**

Files: `Views/Applications/Create.cshtml`, `_ApplicationForm.cshtml`, `ApplicationsController.cs`,
`ApplicationFormViewModel.cs`, `ApplicationService.cs`, `BuildSureDbContext.cs`.

**Step 0: GET the form.** The browser requests `GET /Applications/Create?customerId=4`.
- Routing maps this to `ApplicationsController.Create(int? customerId)`. The `customerId` value comes from the query string.
- The action builds an `ApplicationFormViewModel`, loads the customer dropdown, and returns `View(form)`.
- Razor renders `Create.cshtml`. Tag helpers like `<input asp-for="ProjectName">` generate `name="ProjectName"`, the current value, and `data-val-*` attributes from the Data Annotations.
- The `<form>` tag helper adds a hidden **anti-forgery token** automatically.

**Step 1: Browser (client-side validation).** jQuery Unobtrusive Validation reads the
`data-val-required` and `data-val-range` attributes. If a field is invalid, the form is **not
submitted** and the messages appear immediately. These rules come from the same Data Annotations
the server checks, but client-side checks are only a convenience. The server always validates again.

**Step 2: HTTP POST.** The browser sends `POST /Applications/Create` with form fields
(`CustomerId=4&ProjectName=...&__RequestVerificationToken=...`).

**Step 3: Middleware.** The request passes through the middleware pipeline from `Program.cs`
(HTTPS redirection, routing, authorization). Routing picks the overload marked `[HttpPost]`.

**Step 4: Filters.** `[ValidateAntiForgeryToken]` checks the hidden token against the cookie.
If it's missing or wrong, the request gets **400 Bad Request** (CSRF protection).

**Step 5: Model binding.** ASP.NET creates an `ApplicationFormViewModel` and fills its
properties from the form fields **by name**. This is the step Express makes you do by hand with
`req.body`. The ViewModel has no `Status` or `CertificateNumber` property, so a malicious client
can't set them. This protection against *over-posting* is why the app uses ViewModels.

**Step 6: Validation.** During binding, every Data Annotation (`[Required]`, `[Range]`,
`[StringLength]`) is evaluated and errors go into `ModelState`. The controller then adds its own
check (`ValidateCustomerAsync`: does the customer exist?) with `ModelState.AddModelError`.
If `!ModelState.IsValid`, the action returns `View(form)` and the same page is shown again with
the messages and the user's input.

**Step 7: Service.** `applicationService.CreateAsync(form)` applies the business rules:
it generates the application number, sets `SubmissionDate` from the injected clock, and sets
`Status = Pending`. The controller doesn't need to know these rules.

**Step 8: DbContext.**
```csharp
db.Applications.Add(application);   // change tracker marks the entity as Added
await db.SaveChangesAsync();        // EF builds INSERT ... ; SELECT the new Id
```
`SaveChangesAsync` opens a connection from the pool, starts a transaction, runs the SQL, and
copies the generated IDENTITY `Id` back into `application.Id`.

**Step 9: SQL Server.** It runs something like
`INSERT INTO [Applications] (...) OUTPUT INSERTED.[Id] VALUES (@p0, @p1, ...)`.
The values are **parameters**, which prevents SQL injection. The FOREIGN KEY is checked, and
unique indexes are enforced. In Development the console logs the exact SQL.

**Step 10: Redirect (Post/Redirect/Get).**
```csharp
TempData["Success"] = $"Application {application.ApplicationNumber} was submitted...";
return RedirectToAction(nameof(Details), new { id = application.Id });
```
The browser receives **302 → /Applications/Details/5** and performs a GET. Refreshing that page
won't submit the form again. `TempData` keeps the message for exactly one redirect, and
`_Alerts.cshtml` displays it.

**Step 11: View.** `Details(int id)` loads the application with `Include(a => a.Customer)` and
`AsNoTracking()`. It then returns `View(application)`, and the strongly typed `Details.cshtml`
(`@model InsuranceApplication`) renders the HTML.

---

## 3. Walkthrough: clicking **"Approve"** (AJAX + JSON)

Files: `Views/Applications/Details.cshtml`, `wwwroot/js/site.js`, `ApplicationsController.Approve`,
`ApplicationService.ApproveAsync`.

1. The Approve `<form>` has the classes `js-confirm js-review`.
2. **Confirmation dialog:** the `form.js-confirm` submit handler in `site.js` stops the submit
   and opens the Bootstrap modal. When you click **Approve** in the modal, it sets
   `data('confirmed')` and triggers `submit` again.
3. **AJAX:** the `form.js-review` handler calls `$.ajax({ type: 'POST', data: $form.serialize() })`.
   The serialized data includes the anti-forgery token. The handler also sends the header
   `X-Requested-With: XMLHttpRequest`.
4. **Controller:** `Approve(int id, string? reviewNote)` binds `id` from the route and
   `reviewNote` from the form, then calls the service.
5. **Service rule:** it loads the entity *with tracking* (`FindAsync`). If the status isn't
   `Pending`, it returns `OperationResult.Failure(...)`. Otherwise it sets `Approved`,
   `ReviewedAt` and `CertificateNumber = SIM-CERT-{year}-{Id:D6}`, and calls `SaveChangesAsync()`.
   EF sends an **UPDATE for the changed columns only**.
6. **Response:** because the request is AJAX, the controller returns `Json(new { success, status, badgeCss, certificateNumber, ... })`.
   A failure returns `400` with `{ success: false, message }`.
7. **Dynamic update:** jQuery swaps the badge class and text, shows the certificate, removes the
   Edit/Delete buttons and the review panel, and shows a success alert. The page doesn't reload.
8. **Without JavaScript**, the same form does a normal POST, and the controller redirects back to
   Details with a TempData message. This is *progressive enhancement*.

**Reject** works the same way. The reason is checked in **three places**: jQuery (empty
textarea), the service (`RejectAsync` returns a failure for a blank reason), and unit tests.

## 4. Walkthrough: typing in the Applications **search box**

1. `#application-filter` is an ordinary `GET` form, so `/Applications?search=omar&status=Pending` works without JS.
2. `site.js` listens to `input` events and *debounces* them (300 ms). It then sends `GET` with `X-Requested-With`.
3. `ApplicationsController.Index(string? search, ApplicationStatus? status)` binds both values from the
   query string. The string `"Pending"` is converted to the enum automatically.
4. `ApplicationService.SearchAsync` builds an `IQueryable` step by step:
   ```csharp
   IQueryable<InsuranceApplication> query = db.Applications.AsNoTracking().Include(a => a.Customer);
   if (status.HasValue) query = query.Where(a => a.Status == status.Value);
   if (!string.IsNullOrWhiteSpace(search)) query = query.Where(a => a.ProjectName.Contains(term) || ...);
   return await query.OrderByDescending(a => a.SubmissionDate).ToListAsync(); // SQL runs HERE
   ```
   No SQL runs until `ToListAsync()`. EF then translates the whole chain into **one** SELECT with
   a JOIN, a WHERE ... LIKE and an ORDER BY.
5. For AJAX requests the controller returns `PartialView("_ApplicationTable", applications)`.
   That's just the table HTML, and jQuery puts it into `#application-results`.

## 5. Walkthrough: **Delete customer**

`CustomersController.Delete` first checks `db.Applications.AnyAsync(a => a.CustomerId == id)`
(an `EXISTS` query) and returns a friendly error if the customer has applications. Even if that
check were missing, the database would refuse the delete, because the foreign key is configured
with `OnDelete(DeleteBehavior.Restrict)` (`ON DELETE NO ACTION`). This is **defence in depth**:
friendly rules in code, hard guarantees in the database.

---

## 6. Where each EF Core concept is used

| Concept | Where | What it does |
|---|---|---|
| `DbContext` | `Data/BuildSureDbContext.cs` | Session with the database: queries, change tracking, `SaveChanges` |
| `DbSet<T>` | `Customers`, `Applications`, `CustomerSummaries` | A queryable "table". LINQ on it becomes SQL |
| Code-First | `Models/*.cs` + `OnModelCreating` | C# classes define the schema. Migrations create the tables |
| Migrations | `Data/Migrations/` | Versioned schema changes. `__EFMigrationsHistory` records which ones were applied |
| Raw SQL in a migration | `AddReportingViewAndProcedure` | `migrationBuilder.Sql(...)` creates the VIEW and STORED PROCEDURE |
| One-to-many relationship | `Customer.Applications` ↔ `InsuranceApplication.CustomerId/Customer` | FK `FK_Applications_Customers_CustomerId`, configured with `HasOne().WithMany()` |
| Data Annotations | `[Required]`, `[StringLength]`, `[Column(TypeName="decimal(18,2)")]` | Define both the schema (NOT NULL, nvarchar(n)) and validation |
| Fluent API | `HasIndex(...).IsUnique()`, `HasConversion<string>()`, `OnDelete(Restrict)` | Configuration that attributes can't express |
| LINQ | Every service and controller query | `Where`, `OrderBy`, `GroupBy`, `Select`, `AnyAsync`, `CountAsync` |
| `Include()` | `SearchAsync`, `GetByIdAsync`, `CustomersController.Details` | Eager-loads the related entity with a JOIN (avoids N+1 queries) |
| Filtered Include | `Include(c => c.Applications.OrderByDescending(...))` | Orders the loaded child collection |
| `AsNoTracking()` | All read-only queries | Skips change tracking, which is faster and uses less memory. Use it when you won't save |
| Tracking queries | `FindAsync` in Update/Approve/Reject/Delete | Load, modify properties, then `SaveChangesAsync` generates the UPDATE |
| `SaveChangesAsync()` | Create/Update/Delete/Approve/Reject | Writes all pending changes in one transaction |
| Projection | `GetCustomerLookupAsync` → `Select(c => new Customer { Id, FullName, NationalId })` | SELECTs only the needed columns |
| `GroupBy` | `GetDashboardAsync` | `SELECT Status, COUNT(*) ... GROUP BY Status` |
| Keyless entity → SQL view | `CustomerApplicationSummary` + `ToView(...)` | Read-only LINQ over a view |
| Raw SQL query | `ReportsController` → `Database.SqlQuery<ApplicationReportRow>($"EXEC ...")` | Runs a stored procedure. Interpolated values become parameters |
| InMemory provider | `tests/` | Fast tests without SQL Server. It doesn't enforce FKs or run raw SQL, so test SQL-specific behaviour against a real database |

### async/await

Every database call is `async` (`ToListAsync`, `SaveChangesAsync`). While SQL Server works, the
request thread goes back to the pool and can serve other requests. This is the same idea as
`await` in Node.js, and it lets the server handle more concurrent requests with fewer threads.

---

## 7. EF Core LINQ vs SQL views vs stored procedures

| | EF Core LINQ | SQL VIEW | STORED PROCEDURE |
|---|---|---|---|
| Written in | C# (compile-time checked, refactor-safe) | T-SQL | T-SQL |
| Lives in | Application code | Database | Database |
| Composable? | Yes, you keep adding `Where`/`OrderBy` | Yes, you can SELECT/WHERE on it (in EF too) | No. `EXEC` returns a fixed result |
| Parameters | C# variables → SQL parameters | None (filter with WHERE outside) | Yes (`@Status`) |
| Can modify data / contain logic | Through `SaveChanges` | No (read-only here) | Yes (`usp_ApproveApplication`) |
| Versioning | Migrations | Created in migration / script | Created in migration / script |
| Typical use | Everyday CRUD and queries | Reusable read models, reporting, hiding joins | Complex/batch logic, DBA-owned logic, legacy systems, permissions per procedure |

In BuildSure:
- The dashboard counts use **LINQ** `GroupBy`.
- The *SQL Reports* top table reads the **view** through a keyless entity, so you can still add `OrderBy` in LINQ.
- The bottom table calls the **stored procedure** with `Database.SqlQuery<T>($"EXEC dbo.usp_GetApplicationsByStatus @Status = {statusName}")`.
  The interpolated value is turned into a parameter. It is **not** string concatenation.
- `database/03_stored_procedures.sql` has `usp_ApproveApplication`, the same approve rule as
  `ApplicationService.ApproveAsync` written in T-SQL. Comparing the two is a good interview talking point.

---

## 8. Legacy Web Forms in 2 minutes (`demos/BuildSure.WebForms`)

| Web Forms | MVC equivalent |
|---|---|
| `Default.aspx` (markup + `<asp:...>` controls) | Razor view |
| `Default.aspx.cs` code-behind with `btnSearch_Click` | Controller action |
| **PostBack**: every button POSTs the single `<form runat="server">` back to the same page | Separate GET/POST actions |
| **ViewState**: hidden `__VIEWSTATE` field holding control state between requests | No hidden state. You use the model, TempData or the database |
| Page life cycle: `Page_Init` → `Page_Load` → control events → `Page_PreRender` → Render | Middleware → action → view |
| `if (!IsPostBack) { ... }` for first-load-only code | GET action |
| `Web.config` for everything | `appsettings.json` + `Program.cs` |

When maintaining Web Forms apps, keep these in mind:
- Large ViewState makes pages heavy, so set `EnableViewState="false"` where you don't need it.
- Data binding usually belongs inside `!IsPostBack`.
- Event handlers run **after** `Page_Load`.
- Most data access is ADO.NET (`SqlConnection`, `SqlCommand`, `SqlDataAdapter`). Always use parameters.

## 9. Windows Forms in 1 minute (`demos/BuildSure.WinForms`)

- `ApplicationSearchForm.Designer.cs` creates the controls (Visual Studio's designer normally writes it).
- `ApplicationSearchForm.cs` contains the event handlers. `btnSearch_Click` is `async void`, which is the only acceptable place for `async void` in C#.
- `await` keeps the UI thread free, so the window doesn't freeze, and execution continues on the UI thread afterwards.
- `ApplicationRepository` uses ADO.NET with a parameterised query and returns a `DataTable` bound to the `DataGridView`.

---

## 10. Suggested study order

1. `Program.cs`: DI and the pipeline.
2. `Models/Customer.cs`, `Models/InsuranceApplication.cs`, `Data/BuildSureDbContext.cs`.
3. `Data/Migrations/*InitialCreate.cs`: what EF generated from the models.
4. `Controllers/CustomersController.cs`: plain CRUD with the DbContext directly.
5. `Services/ApplicationService.cs` + `Controllers/ApplicationsController.cs`: business rules in a service, thin controller.
6. `Views/Applications/Details.cshtml` + `wwwroot/js/site.js`: Razor and jQuery AJAX.
7. `tests/BuildSure.Tests/ApplicationServiceTests.cs`.
8. `database/*.sql` and `Controllers/ReportsController.cs`.

## 11. Likely interview questions this project lets you answer

- Difference between `AddScoped`, `AddTransient` and `AddSingleton`. Why is DbContext scoped?
- What is model binding? What is over-posting, and how do ViewModels and `[Bind]` prevent it?
- How does `ModelState` work? Why validate on the server when there's client-side validation?
- What does `[ValidateAntiForgeryToken]` protect against?
- What is the Post/Redirect/Get pattern? What is `TempData` for?
- `IQueryable` vs `IEnumerable`. When does EF actually execute the query?
- What does `Include` solve (N+1)? When should you use `AsNoTracking`?
- How do migrations work? What's in `__EFMigrationsHistory`?
- View vs stored procedure vs LINQ. How do you call an SP from EF Core safely?
- Clustered vs non-clustered index. Why a unique index on `NationalId`? What is a filtered index?
- `INNER JOIN` vs `LEFT JOIN` (see the view: customers with 0 applications still appear).
- In-process vs out-of-process hosting on IIS. What does the Hosting Bundle install?
- PostBack and ViewState in Web Forms, and the page life cycle.
