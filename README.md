# JobSearchBuilder

A Windows desktop application that builds targeted Google search queries for job hunting. Instead of trawling job boards one by one, JobSearchBuilder generates a single `site:`-scoped Google query that searches directly inside the career portals of the most popular Applicant Tracking Systems (ATS) — Greenhouse, Lever, Workable, Ashby, Workday, and more.

---

## How it works

Google supports the `site:` operator to restrict results to a specific domain. JobSearchBuilder assembles a query that combines multiple `site:` filters with your tech stack, role, seniority, location, visa, and remote preferences:

```
(
  site:boards.greenhouse.io
  OR site:jobs.lever.co
  OR site:apply.workable.com
)
("C#" OR ".NET" OR "ASP.NET Core")
(Developer OR Engineer)
(Senior)
("United Kingdom" OR UK OR London OR Manchester)
("visa sponsorship" OR "sponsor visa")
(remote OR hybrid)
```

Clicking **Open in Google** fires this query with `&tbs=li:1` (past 24 hours) so only fresh postings appear.

---

## Features

- **Search profiles** — save, edit and delete named profiles so you can switch between different job searches instantly
- **Five keyword categories** — Tech Stack, Roles, Locations, Visa filters, Remote/Hybrid filters
- **ATS source groups** — pick which job board platforms to search; groups can be combined freely
- **Live query preview** — the query rebuilds as you type, before you save anything
- **One-click Google search** — opens the assembled query directly in your browser
- **Copy to clipboard** — paste the raw query into any search engine
- **Persistent storage** — profiles are saved to a local SQL Server database and survive restarts

---

## Prerequisites

| Requirement | Version |
|---|---|
| Windows | 10 or 11 |
| .NET Framework | 4.8 |
| SQL Server | Any edition (Express, Developer, LocalDB) |

---

## Setup

### 1. Database

Run `Scripts/CreateTables.sql` against your SQL Server instance. The script creates the `JobSearchBuilder` database and all three tables if they don't already exist, and seeds a default **UK Sponsorship .NET** profile so the app isn't empty on first launch.

```powershell
sqlcmd -S .\SQLEXPRESS -i Scripts\CreateTables.sql
```

### 2. Connection string

Edit `appsettings.json` and set `ConnectionString` to match your instance:

```json
{
  "ConnectionString": "Server=.\\SQLEXPRESS;Database=JobSearchBuilder;Integrated Security=true;TrustServerCertificate=True;"
}
```

### 3. Build & run

Open `JobSearchBuilder.sln` in Visual Studio 2022 and press **F5**, or build from the command line:

```powershell
msbuild JobSearchBuilder.sln /p:Configuration=Release
```

---

## Project structure

```
JobSearchBuilder/
├── Interfaces/
│   └── IProfileStore.cs           # Profile persistence contract
├── Models/
│   ├── AtsSourceGroup.cs          # A named set of ATS domains
│   ├── SearchProfile.cs           # All the criteria for one search
│   └── SearchQuery.cs             # Query result model
├── Services/
│   ├── AppSettingsLoader.cs       # Reads appsettings.json
│   ├── IDbConnectionFactory.cs    # Database connection abstraction
│   ├── SqlConnectionFactory.cs    # SQL Server implementation
│   ├── SqlProfileStore.cs         # Persists profiles to SQL Server
│   ├── InMemoryProfileStore.cs    # In-memory store (used in unit tests)
│   └── QueryBuilder.cs            # Assembles the Google query string
├── Scripts/
│   └── CreateTables.sql           # One-time database setup script
├── MainForm.cs                    # Main UI
└── appsettings.json               # Runtime configuration
```

---

## Database schema

```
SearchProfiles          ProfileKeywords             ProfileSourceGroups
─────────────────       ──────────────────────      ───────────────────────
Id   (PK, identity)     Id   (PK, identity)         ProfileId  (PK, FK)
Name                    ProfileId  (FK → cascade)   SourceGroupId  (PK)
Seniority               Category   (Stack | Role
CreatedAt                           Location | Visa
UpdatedAt                           Remote)
                        Keyword
```

All keyword lists are stored in a single `ProfileKeywords` table distinguished by `Category`, keeping the schema flat and easy to query.

---

## Adding ATS source groups

ATS groups are defined in `appsettings.json` under `AtsSourceGroups`. Add a new entry to cover additional platforms:

```json
{
  "Id": 6,
  "Name": "ATS Set #6 – Personio / Factorial",
  "Domains": [
    "jobs.personio.com",
    "jobs.factorialhr.com"
  ]
}
```

No code changes required — the app reads the groups at startup.

---

## Running the tests

```powershell
dotnet test JobSearchBuilder.Tests
```

The test suite uses an in-memory SQLite database so no SQL Server is needed to run tests. Tests cover `SqlProfileStore` (insert, update, delete, all read operations) and `QueryBuilder` (query assembly and Google URL generation).

---

## Default search profile

The setup script seeds one profile out of the box:

| Field | Value |
|---|---|
| Name | UK Sponsorship .NET |
| Seniority | Senior |
| Stack | C#, .NET, ASP.NET Core |
| Roles | Developer, Engineer |
| Locations | United Kingdom, UK, London, Manchester, Leeds, Birmingham |
| Visa | visa sponsorship, sponsor visa |
| Remote | remote, hybrid |
| ATS groups | Set #1 (Greenhouse / Lever / Workable), Set #2 (BambooHR / Rippling / Ashby) |

Edit or delete it once you have the app running and want to tailor it to your own search.
