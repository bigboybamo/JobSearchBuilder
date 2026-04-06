# /generate-claude-md

Generates a filled CLAUDE.md for this project by reading the actual codebase.
Replaces the existing CLAUDE.md at the repo root — no placeholders left behind.

## Usage

Run this command from the repo root inside Claude Code:

```
/generate-claude-md
```

No arguments needed. Claude discovers everything by reading the repo directly.

---

## Instructions

You are generating a filled `CLAUDE.md` for the project in the current working directory.
Do not ask the user to answer questions you can find by reading the repo.
Work through every discovery step below before writing a single line of output.

---

### Step 1 — Discover project identity

Read the following in order:

1. Find the `.sln` file — its name is the solution/project name
2. Find all `.csproj` files — note their names, folders, and `<TargetFramework>` values
3. Read `README.md` or `README.txt` if present — extract any description of what the system does
4. Check the GitHub remote: `git remote get-url origin` — note the repo URL
5. Note the default branch: `git branch --show-current`

---

### Step 2 — Discover folder structure

1. Run a folder tree from the repo root — list all folders up to 3 levels deep,
   ignoring `bin/`, `obj/`, `.git/`, `node_modules/`
2. Identify the project type from the folder names and `.csproj` contents:
   - `Controllers/` or `Endpoints/` → Web API
   - `Pages/` or `Views/` → MVC / Razor Pages
   - `*.Worker.csproj` or `BackgroundService` in code → Worker Service
   - `MainWindow.xaml` or `.xaml` files → WPF
   - `*.Designer.cs` or `Form1.cs` → WinForms

---

### Step 3 — Discover architecture pattern

Read the folder structure and key files to determine which pattern is in use:

**Clean Architecture signals:**
- Multiple projects named `*.Domain`, `*.Application`, `*.Infrastructure`, `*.API`
- `IRepository` interfaces in a Domain project
- `IRequest` / `IRequestHandler` (MediatR) in an Application project

**CQRS + MediatR signals:**
- `Commands/` and `Queries/` folders
- Files named `*Command.cs`, `*Query.cs`, `*Handler.cs`
- `MediatR` in any `.csproj`

**Repository Pattern signals:**
- `Interfaces/` folder containing `I*Repository.cs` files
- Corresponding implementations in `Repositories/` or `Infrastructure/`

**Simple folder structure signals:**
- Single project with `Controllers/`, `Services/`, `Interfaces/`, `Models/`
- No separate Domain or Application project

Read at least 2-3 files per pattern signal before concluding — do not assume from folder
names alone.

---

### Step 4 — Discover coding standards

1. Read `.editorconfig` if present — note indent size, charset, naming rules
2. Read `stylecop.json` or `*.ruleset` if present
3. Check any `.csproj` for:
   - `<Nullable>` — nullable reference types setting
   - `<TreatWarningsAsErrors>`
   - `<ImplicitUsings>`
4. Read 3-5 existing C# files across the project and note:
   - Actual naming patterns in use (fields, properties, methods)
   - Whether `var` is used freely or sparingly
   - Whether async methods consistently have the `Async` suffix
   - Whether file-scoped namespaces are used

---

### Step 5 — Discover database and ORM

1. Check all `.csproj` files for these packages:
   - `Microsoft.EntityFrameworkCore` → EF Core
   - `Npgsql.EntityFrameworkCore.PostgreSQL` → PostgreSQL
   - `Microsoft.EntityFrameworkCore.SqlServer` → SQL Server
   - `Microsoft.EntityFrameworkCore.Sqlite` → SQLite
   - `Dapper` → Dapper
2. Find the `DbContext` class — read it to identify entity sets and any config
3. Check `appsettings.json` and `appsettings.Development.json` for connection string keys
4. Look for a `Migrations/` folder — note where it lives

---

### Step 6 — Discover testing setup

1. Look for any project with `Test`, `Tests`, `Spec`, or `Specs` in the name
2. If found, read its `.csproj` for:
   - `NUnit` / `xunit` / `MSTest` → test framework
   - `Moq` / `NSubstitute` / `FakeItEasy` → mock library
   - `FluentAssertions` → assertion library
3. Read 1-2 existing test files to confirm naming conventions actually in use
4. If no test project exists, note that clearly

---

### Step 7 — Discover NuGet packages

Read every `.csproj` and collect all `<PackageReference>` entries.
Group them by purpose:
- Logging: Serilog, NLog, etc.
- Validation: FluentValidation
- Mapping: AutoMapper, Mapperly
- Resilience: Polly
- API: Swashbuckle, NSwag
- Auth: Microsoft.AspNetCore.Authentication.*
- Any others not in the Microsoft.* namespace

---

### Step 8 — Discover environment variables and secrets

1. Read `appsettings.json` — find every key that has an empty, placeholder, or
   obviously fake value (e.g. `""`, `"YOUR_KEY_HERE"`, `"changeme"`)
2. Look for `IOptions<T>` classes — read them to extract all configuration property names
3. Check `Program.cs` or `Startup.cs` for any direct `IConfiguration["key"]` access
4. Check for `dotnet user-secrets` usage: `<UserSecretsId>` in any `.csproj`
5. Check for a `.env.example` file if present

---

### Step 9 — Discover git workflow

1. Read `.github/workflows/` if present — note what CI/CD steps exist
2. Read `azure-pipelines.yml` if present
3. Check existing branch names: `git branch -a`
4. Check recent commit messages: `git log --oneline -10` — infer commit message style

---

### Step 10 — Write the CLAUDE.md

Now generate the filled `CLAUDE.md` using the CLAUDE.md template at the repo root as the
base structure. Apply these rules:

- Fill every `[PLACEHOLDER]` with what you discovered — leave none behind
- If you could not determine something with confidence, write a short inline comment:
  `<!-- TODO: confirm this with the client -->`
- In the Architecture section: keep only the patterns you found evidence for, delete the rest
- In the NuGet packages table: list only packages actually present in the project
- In the Environment Variables section: list only keys you found evidence for
- In the MCP Integrations section: keep GitHub by default, note others as commented-out options
- In the Build & Local Commands section: use the actual project names from the `.csproj` files
- In the Git Workflow section: use the actual default branch name from discovery
- Do not invent conventions you did not find evidence for — mark them as TODOs instead
- Delete the template instruction block at the top

Write the completed file to `CLAUDE.md` at the repo root, replacing the existing file.

---

### Step 11 — Report what was done

After writing the file, output a short summary:

```
CLAUDE.md generated for {ProjectName}

Discovered:
  Project type:    {e.g. Web API}
  Architecture:    {e.g. Clean Architecture + CQRS + MediatR}
  Framework:       {e.g. net8.0}
  Database:        {e.g. PostgreSQL via EF Core}
  Testing:         {e.g. No test project found / xUnit + Moq}
  Branch:          {e.g. main}

Items needing your review (<!-- TODO --> comments in the file):
  - {list any gaps found}

Next step: run .\scripts\validate.ps1 to confirm connections are live.
```

---

## Rules

- Never ask the user questions you can answer by reading the repo.
- If a file is too large to read fully, read the first 100 lines and the last 20.
- If you cannot find evidence for a section, write a clear TODO comment rather than guessing.
- Do not copy-paste large blocks of existing code into CLAUDE.md — summarise the pattern.
- Always run `git remote get-url origin` and `git branch --show-current` — these are fast
  commands that give you facts you should not guess.
- The generated CLAUDE.md must be immediately usable — open Claude Code, read it, and start
  working without any manual edits required for the core sections.
