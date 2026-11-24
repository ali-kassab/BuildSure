/* =====================================================================
   BuildSure - 01: Primary keys, foreign keys and indexes
   Database: BuildSureDb (created by EF Core migrations)

   The tables were created by EF Core (see 00_schema_generated_by_ef.sql).
   This script shows the equivalent hand-written DDL, how to inspect keys
   and indexes in the system catalog, and adds one extra index.
   Safe to run more than once.
   ===================================================================== */
USE BuildSureDb;
GO

-- Required for tables with filtered indexes (IX_Applications_CertificateNumber).
-- SSMS and EF turn these ON by default; the classic sqlcmd tool does not.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------------------------------------------------------------------
   1. What EF generated, written by hand (FOR READING ONLY - do not run;
      the tables already exist):

   CREATE TABLE dbo.Customers (
       Id          INT IDENTITY(1,1) NOT NULL,
       FullName    NVARCHAR(100)     NOT NULL,
       NationalId  NVARCHAR(20)      NOT NULL,
       PhoneNumber NVARCHAR(20)      NOT NULL,
       Email       NVARCHAR(150)     NOT NULL,
       Address     NVARCHAR(250)     NOT NULL,
       CreatedAt   DATETIME2         NOT NULL,
       CONSTRAINT PK_Customers PRIMARY KEY (Id)          -- PRIMARY KEY: unique, not null, clustered index
   );

   CREATE TABLE dbo.Applications (
       Id                INT IDENTITY(1,1) NOT NULL,
       ApplicationNumber NVARCHAR(30)      NOT NULL,
       CustomerId        INT               NOT NULL,
       ...
       CONSTRAINT PK_Applications PRIMARY KEY (Id),
       CONSTRAINT FK_Applications_Customers_CustomerId  -- FOREIGN KEY: every application must
           FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)  -- point to an existing customer
           ON DELETE NO ACTION                          -- (EF "Restrict"): cannot delete a customer that has applications
   );

   CREATE UNIQUE INDEX IX_Customers_NationalId ON dbo.Customers (NationalId);
   CREATE UNIQUE INDEX IX_Applications_ApplicationNumber ON dbo.Applications (ApplicationNumber);
   CREATE INDEX IX_Applications_CustomerId ON dbo.Applications (CustomerId);
   CREATE INDEX IX_Applications_Status ON dbo.Applications (Status);
   --------------------------------------------------------------------- */

/* 2. Inspect primary keys and unique constraints */
SELECT  t.name AS TableName, kc.name AS ConstraintName, kc.type_desc
FROM    sys.key_constraints AS kc
INNER JOIN sys.tables AS t ON t.object_id = kc.parent_object_id
ORDER BY t.name;

/* 3. Inspect foreign keys (child column -> parent table/column) */
SELECT  fk.name                                   AS ForeignKey,
        OBJECT_NAME(fk.parent_object_id)          AS ChildTable,
        COL_NAME(fkc.parent_object_id, fkc.parent_column_id)        AS ChildColumn,
        OBJECT_NAME(fk.referenced_object_id)      AS ParentTable,
        COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ParentColumn,
        fk.delete_referential_action_desc         AS OnDelete
FROM    sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id;

/* 4. Inspect indexes on the two tables */
SELECT  OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName, i.type_desc, i.is_unique, i.has_filter
FROM    sys.indexes AS i
WHERE   OBJECT_NAME(i.object_id) IN (N'Customers', N'Applications') AND i.name IS NOT NULL
ORDER BY TableName, IndexName;

/* 5. Add a basic non-clustered index that speeds up "latest applications" queries
      (ORDER BY SubmissionDate DESC on the dashboard).
      INCLUDE stores extra columns in the index so the query does not need to read the table.
      NOTE: in a real project you would add this with HasIndex(...) in the DbContext so that
      EF migrations stay the single source of truth for the schema. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Applications_SubmissionDate'
               AND object_id = OBJECT_ID(N'dbo.Applications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Applications_SubmissionDate
        ON dbo.Applications (SubmissionDate DESC)
        INCLUDE (ApplicationNumber, ProjectName, Status, CustomerId);
END
GO

/* To remove it again:
   DROP INDEX IX_Applications_SubmissionDate ON dbo.Applications; */
