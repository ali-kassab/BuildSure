# Deploying BuildSure.Web to IIS

> **Status:** `dotnet publish -c Release` and running the published output in Production mode
> were **tested**. Deployment to a full IIS server was **not** tested, because IIS (W3SVC) isn't
> installed on the development machine. Use the steps below on a machine with IIS.

## 1. How ASP.NET Core runs on IIS

```
Browser ──► IIS (http.sys, port 80/443) ──► ASP.NET Core Module V2 (ANCM) ──► your app
```

- **In-process hosting** (the default here, `hostingModel="inprocess"`): the app runs **inside**
  the IIS worker process `w3wp.exe`. It's the fastest option.
- **Out-of-process hosting**: IIS forwards requests to a separate Kestrel process (`dotnet BuildSure.Web.dll`).
- ASP.NET Core does **not** use the .NET Framework CLR in IIS. That's why the app pool is set to **No Managed Code**.

## 2. Install the prerequisites (on the server)

1. **Enable IIS:** *Turn Windows features on or off* → *Internet Information Services*. On
   Windows Server, use *Server Manager → Add Roles → Web Server (IIS)*.
2. Install the **ASP.NET Core 10.0 Hosting Bundle** (from https://dotnet.microsoft.com/download/dotnet/10.0,
   *Runtime → Hosting Bundle*). It installs:
   - the .NET runtime and ASP.NET Core runtime
   - the **ASP.NET Core Module V2** (`AspNetCoreModuleV2`) for IIS

   Install IIS **before** the Hosting Bundle. If you installed IIS afterwards, run the bundle
   installer again and choose *Repair*.
3. Restart IIS so it picks up the module and the new PATH:
   ```powershell
   net stop was /y
   net start w3svc
   ```
4. Check the installation: `dotnet --list-runtimes` should list `Microsoft.AspNetCore.App 10.x`.
   IIS Manager → server → *Modules* should list `AspNetCoreModuleV2`.

## 3. Publish

On the development machine:

```powershell
dotnet publish src/BuildSure.Web -c Release -o C:\inetpub\BuildSure
```

This is a **framework-dependent** publish, so the server needs the Hosting Bundle. Copy the
folder to the server if you published elsewhere. It contains `BuildSure.Web.dll`, `wwwroot\`,
`appsettings.json` and a generated **`web.config`**.

Visual Studio alternative: right-click the project → *Publish* → *Folder* or *IIS/Web Deploy*.

## 4. web.config explained

`dotnet publish` generates this file. You normally don't edit it in source control.

```xml
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <!-- Send every request (path="*") to the ASP.NET Core Module -->
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <!-- processPath + arguments: how to start the app ("dotnet .\BuildSure.Web.dll")
           stdoutLogEnabled: set to true temporarily to capture startup errors in .\logs\stdout_*.log
           hostingModel: inprocess (inside w3wp.exe) or outofprocess (separate Kestrel process) -->
      <aspNetCore processPath="dotnet" arguments=".\BuildSure.Web.dll"
                  stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

You can add environment variables without changing code:

```xml
<aspNetCore ...>
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ConnectionStrings__BuildSureDb" value="Server=SQLSERVER01;Database=BuildSureDb;Trusted_Connection=True;TrustServerCertificate=True" />
  </environmentVariables>
</aspNetCore>
```

(`__` maps to `:` in configuration keys, so this overrides `ConnectionStrings:BuildSureDb`.)

## 5. Create the Application Pool and Site

In IIS Manager:

1. **Application Pools → Add Application Pool**
   - Name: `BuildSurePool`
   - .NET CLR version: **No Managed Code**
   - Managed pipeline mode: Integrated
2. Optional: *Advanced Settings* → *Identity* → keep **ApplicationPoolIdentity** (recommended).
   The pool then runs as the virtual account `IIS AppPool\BuildSurePool`.
3. **Sites → Add Website**
   - Site name: `BuildSure`
   - Application pool: `BuildSurePool`
   - Physical path: `C:\inetpub\BuildSure`
   - Binding: http, port `8080` (or 80/443 with a host name and certificate)
4. **Folder permissions:** grant `IIS AppPool\BuildSurePool` *Read & execute* on `C:\inetpub\BuildSure`.
   Grant it *Modify* on `logs\` only if you enable stdout logging.

The same steps in PowerShell (run as Administrator):

```powershell
Import-Module WebAdministration
New-WebAppPool -Name BuildSurePool
Set-ItemProperty IIS:\AppPools\BuildSurePool -Name managedRuntimeVersion -Value ''   # No Managed Code
New-Website -Name BuildSure -Port 8080 -PhysicalPath C:\inetpub\BuildSure -ApplicationPool BuildSurePool
icacls C:\inetpub\BuildSure /grant "IIS AppPool\BuildSurePool:(OI)(CI)RX"
```

## 6. Database for the IIS site

- **LocalDB is per-user and isn't suitable for IIS.** It runs under the interactive user's
  profile, not the app pool identity. On the server, use SQL Server Express/Developer/Standard
  and change the connection string.
- Create the schema with one of these:
  - `dotnet ef database update --connection "<server connection string>"` from a dev machine, or
  - run `database/00_schema_generated_by_ef.sql` (idempotent) in SSMS on the server, or
  - `dotnet ef migrations bundle` → copy `efbundle.exe` to the server → `efbundle.exe --connection "..."`.
- Give the app pool identity access with Windows authentication:
  ```sql
  CREATE LOGIN [IIS AppPool\BuildSurePool] FROM WINDOWS;
  USE BuildSureDb;
  CREATE USER [IIS AppPool\BuildSurePool] FOR LOGIN [IIS AppPool\BuildSurePool];
  ALTER ROLE db_datareader ADD MEMBER [IIS AppPool\BuildSurePool];
  ALTER ROLE db_datawriter ADD MEMBER [IIS AppPool\BuildSurePool];
  GRANT EXECUTE ON dbo.usp_GetApplicationsByStatus TO [IIS AppPool\BuildSurePool];
  ```
  If SQL Server is on another machine, the pool reaches it as the computer account
  (`DOMAIN\WEBSERVER$`), or you use a SQL login.
- Keep `Database:MigrateAndSeedOnStartup` **false** in Production. It's already false in
  `appsettings.json`.

## 7. Verify

Browse to `http://localhost:8080`. You should see the dashboard. Also check `/Applications` and `/Reports`.

## 8. Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| **HTTP 500.19** (config error, code 0x8007000d) | ASP.NET Core Module not installed. Install or repair the Hosting Bundle, then `iisreset`. |
| **HTTP 500.30** "In-process start failure" | The app threw during startup (bad connection string, missing config). Set `stdoutLogEnabled="true"`, create a `logs` folder, reproduce, and read `logs\stdout_*.log`. Also check *Event Viewer → Windows Logs → Application*. |
| **HTTP 500.31** "Failed to find native dependencies" / **500.32** | The ASP.NET Core 10 runtime is missing or the bitness doesn't match. Install the matching Hosting Bundle. |
| **HTTP 502.5** / **500.0** with out-of-process | The `dotnet` process failed to start. Run `dotnet .\BuildSure.Web.dll` manually in the site folder to see the error. |
| **HTTP 500.34** "mixed hosting models" | Two apps in the same pool use different hosting models. Give each app its own pool. |
| **HTTP 503** Service Unavailable | The app pool stopped (crashed repeatedly, or bad identity). Check the pool state and the Event Viewer. |
| **HTTP 403.14** / directory listing | `web.config` is missing from the folder, or the site points at the wrong folder (it must be the publish output, not the project). |
| **HTTP 401.3** / access denied | NTFS permissions. Grant the app pool identity read access to the folder. |
| `Login failed for user 'IIS APPPOOL\BuildSurePool'` | Create the SQL login and user (section 6), or use a SQL login in the connection string. |
| `A network-related or instance-specific error` | Wrong server name, SQL Server not allowing remote/TCP connections, or the firewall. LocalDB won't work under IIS. |
| CSS/JS missing | `wwwroot` wasn't copied. Re-publish, and make sure the *Static Content* IIS feature is installed. |
| Changes not visible after a copy | Files locked while the app is running. Put an `app_offline.htm` in the folder (ANCM shuts the app down), copy, then delete it. Or recycle the pool. |
| Data-protection warnings / antiforgery errors after a restart | The keys aren't persisted. Set *Load User Profile = True* on the app pool, or configure `AddDataProtection().PersistKeysToFileSystem(...)`. |

Useful commands:

```powershell
iisreset                                             # restart IIS
Restart-WebAppPool BuildSurePool                     # recycle one pool
Get-WebAppPoolState BuildSurePool
Get-EventLog -LogName Application -Newest 20 -Source "IIS AspNetCore Module V2"
```
