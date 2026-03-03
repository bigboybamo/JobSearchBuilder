-- Run this script against a LocalDB instance (e.g. sqlcmd -S "(localdb)\MSSQLLocalDB" -i CreateTables.sql)
-- It creates the database if it does not already exist, then creates the three tables.

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'JobSearchBuilder')
BEGIN
    CREATE DATABASE JobSearchBuilder;
END
GO

USE JobSearchBuilder;
GO

-- Core profile record
CREATE TABLE SearchProfiles (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(200)  NOT NULL,
    Seniority   NVARCHAR(50)   NOT NULL DEFAULT 'Any',
    CreatedAt   DATETIME2      NOT NULL,
    UpdatedAt   DATETIME2      NOT NULL
);

-- All keyword lists collapsed into one table, distinguished by Category.
-- Category values: 'Stack' | 'Role' | 'Location' | 'Visa' | 'Remote'
CREATE TABLE ProfileKeywords (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ProfileId   INT            NOT NULL REFERENCES SearchProfiles(Id) ON DELETE CASCADE,
    Category    NVARCHAR(20)   NOT NULL,
    Keyword     NVARCHAR(200)  NOT NULL
);

-- Junction table for selected ATS source groups
CREATE TABLE ProfileSourceGroups (
    ProfileId     INT NOT NULL REFERENCES SearchProfiles(Id) ON DELETE CASCADE,
    SourceGroupId INT NOT NULL,
    CONSTRAINT PK_ProfileSourceGroups PRIMARY KEY (ProfileId, SourceGroupId)
);
GO

-- -----------------------------------------------------------------------
-- Default seed: "UK Sponsorship .NET" profile
-- -----------------------------------------------------------------------
DECLARE @ProfileId INT;

INSERT INTO SearchProfiles (Name, Seniority, CreatedAt, UpdatedAt)
VALUES ('UK Sponsorship .NET', 'Senior', SYSUTCDATETIME(), SYSUTCDATETIME());

SET @ProfileId = SCOPE_IDENTITY();

-- Tech stack
INSERT INTO ProfileKeywords (ProfileId, Category, Keyword) VALUES
    (@ProfileId, 'Stack', 'C#'),
    (@ProfileId, 'Stack', '.NET'),
    (@ProfileId, 'Stack', 'ASP.NET Core');

-- Roles
INSERT INTO ProfileKeywords (ProfileId, Category, Keyword) VALUES
    (@ProfileId, 'Role', 'Developer'),
    (@ProfileId, 'Role', 'Engineer');

-- Locations
INSERT INTO ProfileKeywords (ProfileId, Category, Keyword) VALUES
    (@ProfileId, 'Location', 'United Kingdom'),
    (@ProfileId, 'Location', 'UK'),
    (@ProfileId, 'Location', 'London'),
    (@ProfileId, 'Location', 'Manchester'),
    (@ProfileId, 'Location', 'Leeds'),
    (@ProfileId, 'Location', 'Birmingham');

-- Visa filters
INSERT INTO ProfileKeywords (ProfileId, Category, Keyword) VALUES
    (@ProfileId, 'Visa', 'visa sponsorship'),
    (@ProfileId, 'Visa', 'sponsor visa');

-- Remote filters
INSERT INTO ProfileKeywords (ProfileId, Category, Keyword) VALUES
    (@ProfileId, 'Remote', 'remote'),
    (@ProfileId, 'Remote', 'hybrid');

-- ATS source groups (Set #1 and Set #2)
INSERT INTO ProfileSourceGroups (ProfileId, SourceGroupId) VALUES
    (@ProfileId, 1),
    (@ProfileId, 2);
