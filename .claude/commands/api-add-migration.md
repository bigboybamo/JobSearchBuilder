# /add-migration

Guide an EF Core migration — generate, review, and apply it safely.

## Usage
```
/add-migration <MigrationName>
```

## Examples
```
/add-migration AddOrderStatusColumn
/add-migration CreateInvoiceTable
/add-migration RenameCustomerEmailIndex
```

## Instructions

You are guiding an EF Core migration workflow. Follow all steps — do not skip the review step.

---

### Step 1 — Confirm context and projects

Before running anything, confirm:
- Which `DbContext` class this migration targets (if the project has more than one)
- The Infrastructure project path
- The API or Worker startup project path

If these are clear from CLAUDE.md, proceed. If not, ask the user.

---

### Step 2 — Generate the migration

Run this command (adapt paths from CLAUDE.md):

```bash
dotnet ef migrations add {MigrationName} \
  --project src/{Project}.Infrastructure \
  --startup-project src/{Project}.API \
  --context {DbContextName}
```

---

### Step 3 — Review the generated migration file

Open the generated `{timestamp}_{MigrationName}.cs` file and check:

```
Review checklist:
[ ] Does the Up() method match what you expected? Describe what it does.
[ ] Does the Down() method correctly reverse Up()?
[ ] Are there any unexpected table or column drops?
[ ] Are nullable columns correctly marked nullable?
[ ] Are indexes created where needed for foreign keys and frequently queried columns?
[ ] Are any string columns missing a MaxLength? (EF defaults to nvarchar(max) on SQL Server)
[ ] Are default values set correctly for new non-nullable columns on existing tables?
```

**Stop here and show the review checklist output to the user before proceeding.**

---

### Step 4 — Apply the migration (only after user confirms review)

```bash
dotnet ef database update \
  --project src/{Project}.Infrastructure \
  --startup-project src/{Project}.API \
  --context {DbContextName}
```

---

### Step 5 — Remind the user what to do next

```
Next steps:
[ ] Verify the schema change in the database (check the table/column directly)
[ ] Update any seed data scripts if required
[ ] Check that existing integration tests still pass: dotnet test
[ ] Commit the migration file alongside the model change — never commit them separately
```

## Rules
- Never apply a migration without reviewing the generated file first (Step 3 is mandatory).
- Never delete or edit a migration file that has already been applied to any environment. Create a new corrective migration instead.
- If the migration contains an unexpected DROP — stop and ask the user to confirm before applying.
- Migration names must be descriptive of what changed, not the date or a ticket number alone.
- Always commit the `.cs` migration file and the `ModelSnapshot` together in the same commit as the model change.
