# GitHub Copilot Instructions - İzollu Dayanışma Merkezi CRM

## Project Overview
Blazor Server (.NET 8) scholarship management system for a Turkish NGO. SQLite backend with MudBlazor UI. Turkish language throughout (UI, code comments, database fields).

## Architecture Patterns

### Service Layer Architecture
All business logic lives in `/Services/`. Services are scoped, injected in `Program.cs` in dependency order:
```csharp
// Student scholarship status MUST be registered before Meeting/Transcript services
builder.Services.AddScoped<StudentScholarshipStatusService>(); 
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<TranscriptService>();
```

### Cross-Component State Management
Use `TermChangeNotifier` singleton for term selection changes:
```csharp
// Services/TermChangeNotifier.cs - Subscribe in components
protected override void OnInitialized()
{
    TermChangeNotifier.OnTermChanged += RefreshData;
}
```

### Database Context
`ApplicationDbContext` in `/Data/` with entities in `/Data/Entities/`. Turkish property names match database schema (e.g., `AdSoyad`, `BursBitisTarihi`, `MezunMu`). Use decimal(18,2) for currency fields.

## UI Patterns

### Modern Filter Pattern
List pages use `MudDrawer` filter panels (see `FILTERING_UPGRADE_README.md`):
- Filter models in `/Models/` (e.g., `StudentFilterModel.cs`)
- MudDrawer slides from right (450px width)
- Filter chips with individual removal
- LINQ-based filtering for performance
- **Never use classic dropdowns** - use radio buttons, checkboxes, searchable multi-selects

### List Page Layout
Use `ListPageLayout.razor` component for all CRUD pages:
```razor
<ListPageLayout>
    <Header><!-- Page title --></Header>
    <Stats><!-- Statistics cards --></Stats>
    <ToolbarLeft><!-- Action buttons --></ToolbarLeft>
    <ToolbarRight><!-- Search + filter button --></ToolbarRight>
    <Body><!-- MudTable with pagination --></Body>
</ListPageLayout>
```

### Dialog Pattern
All dialogs in `/Shared/` follow consistent structure:
- Use `MudDialog` with `MudDialogInstance`
- Turkish labels and validation messages
- `@bind-Value` for two-way binding
- Call `StateHasChanged()` after async operations in dialogs

## Critical Workflows

### Database Migrations
SQLite with EF Core. Migration tools in root-level utility projects:
```bash
# From IzolluCRM/IzolluDayanismaMerkezi/
dotnet ef migrations add MigrationName
dotnet ef database update
```
**Never run migrations on production DB without backup** - use `Backup-Database.ps1`.

### Term-Based Data
System uses academic terms (`Terms` table). Many entities have `TermId` foreign key. Always filter by active term:
```csharp
var activeTerm = await SystemSettingsService.GetActiveTermAsync();
var data = await _context.Students.Where(s => s.TermId == activeTerm.Id).ToListAsync();
```

### Activity Logging
All CRUD operations must log to `ActivityLogs` via `ActivityLogService`:
```csharp
await _logService.LogAsync("Öğrenci", "Güncelleme", $"{student.AdSoyad} güncellendi");
```

### Term-Based Reporting (CRITICAL)
**Core Architecture**: Students/Members are term-independent entities. Term-based scholarship tracking uses:
- `ScholarshipPayment` - Links Student + Member + Term for actual payments
- `MemberScholarshipCommitment` - Member's pledge per term (PledgedCount vs GivenCount)
- `TermScholarshipConfig` - Defines MonthlyAmount/YearlyAmount per term

**Opening New Terms** (`TermService.OpenNewTermAsync()`):
1. Validates date range and checks for duplicate start dates (unique constraint)
2. Deactivates previous active term (only one `IsActive=true` at a time)
3. Creates new Term entity with `DisplayName` (e.g., "2025-2026")
4. Copies or creates default `TermScholarshipConfig` for amounts

**Report Generation** (`TermReportService`):
```csharp
// Get active scholarship count - uses DISTINCT StudentId from ScholarshipPayments
var count = await TermReportService.GetActiveScholarshipCountByTermAsync(termId);

// Get full report with payment details
var summary = await TermReportService.GetScholarshipSummaryByTermAsync(termId);
// Returns: ActiveScholarshipStudents, TotalCommitted, TotalRealized, TotalMonthlyAmount, etc.
```

**Critical Pattern**: Never query Students directly for term counts. Always use `ScholarshipPayment.TermId` with `.Select(sp => sp.StudentId).Distinct()` for accurate term-based student counts.

## Data Conventions

### Turkish Field Names
Follow existing Turkish naming:
- `AdSoyad` (full name), `TCNo` (ID number), `DogumTarihi` (birth date)
- `AktifBursMu`, `MezunMu` (boolean flags with "Mu/Mı" suffix)
- `OlusturmaTarihi` (created date), `GuncellemeTarihi` (updated date)

### Date Handling
Turkish culture configured in `Program.cs`:
```csharp
// dd/MM/yyyy format everywhere
var cultureInfo = new CultureInfo("tr-TR");
cultureInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
```

### Decimal Formatting
NumberDecimalSeparator set to "." for SVG/chart compatibility, but UI displays Turkish format.

## Utility Projects
Root-level console apps for database maintenance:
- `FixStudents/` - Data correction scripts
- `DbMigrationTool/` - Manual schema changes (use when EF migrations fail)
- `DbCompare/` - Schema validation

These directly connect to SQLite with `Microsoft.Data.Sqlite`.

## Common Pitfalls

1. **Service Registration Order** - StudentScholarshipStatusService must be registered before services that depend on it
2. **StateHasChanged Calls** - Required after async operations in Blazor components for UI refresh
3. **Filter Model Complexity** - Use dedicated filter models (`/Models/`) with enums and helper methods, not inline logic
4. **Term Context Missing** - Most queries need active term filter; use `SystemSettingsService.GetActiveTermAsync()`
5. **Backup Before Migrations** - Production database is `izolluvakfi.db`, always backup first
6. **Term Reporting Queries** - Never count students by `Student.TermId`; always use `ScholarshipPayment.TermId` with distinct StudentId
7. **Active Term Constraint** - Only one term can have `IsActive=true`; deactivate old term before activating new one

## Key Files
- [`Program.cs`](IzolluCRM/IzolluDayanismaMerkezi/Program.cs) - DI configuration, Kestrel setup, culture settings
- [`ApplicationDbContext.cs`](IzolluCRM/IzolluDayanismaMerkezi/Data/ApplicationDbContext.cs) - EF Core configuration, entity relationships
- [`FILTERING_UPGRADE_README.md`](IzolluCRM/IzolluDayanismaMerkezi/FILTERING_UPGRADE_README.md) - Modern filter pattern guide
- [`LAYOUT_REFACTOR_GUIDE.md`](IzolluCRM/IzolluDayanismaMerkezi/LAYOUT_REFACTOR_GUIDE.md) - ListPageLayout usage guide
- [`CLAUDE_CODE_SKILL.md`](CLAUDE_CODE_SKILL.md) - Engineering discipline and change management rules

## Development Commands
```bash
# Run main application
cd IzolluCRM/IzolluDayanismaMerkezi
dotnet run

# Run on all network interfaces (configured in Program.cs)
# Listens on http://0.0.0.0:5000

# Export Excel reports (via ExcelService using ClosedXML)
# Generate PDF reports (via PdfService using QuestPDF)
```
