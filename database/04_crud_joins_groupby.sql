/* =====================================================================
   BuildSure - 04: SELECT, INSERT, UPDATE, DELETE, INNER JOIN, GROUP BY
   Everything that changes data runs inside a transaction that is ROLLED BACK
   at the end, so you can run this script as often as you like.
   Each statement has the LINQ it corresponds to in the MVC app.
   ===================================================================== */
USE BuildSureDb;
GO

-- Required for tables with filtered indexes (IX_Applications_CertificateNumber).
-- SSMS and EF turn these ON by default; the classic sqlcmd tool does not.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------- SELECT ---------- */
-- LINQ: db.Customers.AsNoTracking().OrderBy(c => c.FullName).ToListAsync()
SELECT Id, FullName, NationalId, Email
FROM   dbo.Customers
ORDER BY FullName;

-- LINQ: db.Applications.Where(a => a.Status == ApplicationStatus.Pending)
SELECT ApplicationNumber, ProjectName, ConstructionCost
FROM   dbo.Applications
WHERE  Status = N'Pending';

/* ---------- INNER JOIN ---------- */
-- LINQ: db.Applications.Include(a => a.Customer)  (EF generates a JOIN like this)
SELECT  a.ApplicationNumber, a.ProjectName, a.Status, c.FullName AS CustomerName, c.PhoneNumber
FROM    dbo.Applications AS a
INNER JOIN dbo.Customers AS c ON c.Id = a.CustomerId     -- only rows that match on both sides
ORDER BY a.SubmissionDate DESC;

/* ---------- GROUP BY ---------- */
-- LINQ: db.Applications.GroupBy(a => a.Status).Select(g => new { g.Key, Count = g.Count() })
SELECT  Status, COUNT(*) AS ApplicationCount
FROM    dbo.Applications
GROUP BY Status;

-- GROUP BY + JOIN + HAVING: customers with more than one application
SELECT  c.FullName, COUNT(a.Id) AS Applications, SUM(a.ConstructionCost) AS TotalCost
FROM    dbo.Customers AS c
INNER JOIN dbo.Applications AS a ON a.CustomerId = c.Id
GROUP BY c.FullName
HAVING  COUNT(a.Id) > 1;


/* ---------- INSERT / UPDATE / DELETE (rolled back) ---------- */
BEGIN TRANSACTION;

    -- INSERT  (LINQ: db.Customers.Add(customer); await db.SaveChangesAsync();)
    INSERT INTO dbo.Customers (FullName, NationalId, PhoneNumber, Email, Address, CreatedAt)
    VALUES (N'SQL Demo Customer', N'900000001', N'+961 1 234 567', N'sql.demo@example.com', N'Demo Street', SYSUTCDATETIME());

    DECLARE @CustomerId INT = SCOPE_IDENTITY();   -- the new IDENTITY value (EF reads it back the same way)

    INSERT INTO dbo.Applications
        (ApplicationNumber, CustomerId, ProjectName, ProjectAddress, NumberOfFloors, ConstructionCost, SubmissionDate, Status)
    VALUES
        (N'APP-SQL-DEMO-1', @CustomerId, N'Demo Tower', N'Demo Street', 5, 750000, SYSUTCDATETIME(), N'Pending');

    -- UPDATE  (LINQ: load entity, change property, SaveChangesAsync())
    UPDATE dbo.Applications
    SET    NumberOfFloors = 6, Notes = N'Updated from SQL script'
    WHERE  ApplicationNumber = N'APP-SQL-DEMO-1';

    SELECT ApplicationNumber, NumberOfFloors, Notes FROM dbo.Applications WHERE ApplicationNumber = N'APP-SQL-DEMO-1';

    -- The FOREIGN KEY prevents deleting a customer who still has applications:
    BEGIN TRY
        DELETE FROM dbo.Customers WHERE Id = @CustomerId;
    END TRY
    BEGIN CATCH
        PRINT CONCAT(N'Expected FK error: ', ERROR_MESSAGE());
    END CATCH

    -- DELETE child first, then parent  (LINQ: db.Applications.Remove(app); ...)
    DELETE FROM dbo.Applications WHERE ApplicationNumber = N'APP-SQL-DEMO-1';
    DELETE FROM dbo.Customers    WHERE Id = @CustomerId;

ROLLBACK TRANSACTION;   -- undo everything above; change to COMMIT to keep the changes
