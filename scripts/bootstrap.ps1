# =============================================================================
# bootstrap.ps1 - Claude .NET Engagement Bootstrap
# Copies the Claude template (CLAUDE.md, commands, agents, integrations,
# scripts) into a target client repo and prepares it for /generate-claude-md.
#
# =============================================================================

#Requires -Version 5.1

param(
    [Parameter(Mandatory = $true)]
    [string]$TargetRepo
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Header($text) {
    Write-Host ""
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "  $text" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Pass($text) { Write-Host "  [OK] $text" -ForegroundColor Green }
function Write-Fail($text) { Write-Host "  [X]  $text" -ForegroundColor Red }
function Write-Warn($text) { Write-Host "  [!]  $text" -ForegroundColor Yellow }

function Confirm-Step($prompt) {
    while ($true) {
        $reply = Read-Host "  [?] $prompt [y/n]"
        if ($reply -match '^[Yy]$') { return $true }
        if ($reply -match '^[Nn]$') { return $false }
        Write-Host "      Please answer y or n." -ForegroundColor Yellow
    }
}

# Paths
$ScriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$TemplateRoot  = Split-Path -Parent $ScriptDir
$resolvedPath = Resolve-Path $TargetRepo -ErrorAction SilentlyContinue
$Target = if ($resolvedPath) { $resolvedPath.Path } else { $null }

# Banner
Clear-Host
Write-Host ""
Write-Host "  Claude .NET Engagement Bootstrap" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Template : $TemplateRoot"
Write-Host "  Target   : $TargetRepo"
Write-Host ""

# =============================================================================
# STEP 1 - Validate inputs
# =============================================================================
Write-Header "Step 1 of 3 - Validating target repo"

if (-not $Target -or -not (Test-Path $Target)) {
    Write-Fail "Target folder not found: $TargetRepo"
    Write-Host ""
    Write-Host "  Clone the client repo first:" -ForegroundColor Yellow
    Write-Host "    git clone https://github.com/client/repo.git" -ForegroundColor White
    Write-Host ""
    exit 1
}
Write-Pass "Target folder exists: $Target"

if (-not (Test-Path (Join-Path $Target '.git'))) {
    Write-Fail "Target is not a git repository: $Target"
    Write-Host ""
    Write-Host "  Clone the client repo first before running bootstrap." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
Write-Pass "Target is a git repository."

Push-Location $Target
$gitStatus = & git status --porcelain 2>$null
Pop-Location

if ($gitStatus) {
    Write-Warn "Target repo has uncommitted changes."
    Write-Warn "Bootstrap will add new files - existing uncommitted work will not be affected."
    $continue = Confirm-Step "Continue anyway?"
    if (-not $continue) {
        Write-Host ""
        Write-Host "  Commit or stash your changes first, then re-run bootstrap." -ForegroundColor Yellow
        exit 0
    }
}

$templateResolved = (Resolve-Path $TemplateRoot).Path
if ($templateResolved -eq $Target) {
    Write-Fail "Target cannot be the template repo itself."
    exit 1
}

Write-Pass "All validations passed."

# =============================================================================
# STEP 2 - Copy template files
# =============================================================================
Write-Header "Step 2 of 3 - Copying template into target repo"

$CopyItems = @(
    @{ Src = "CLAUDE.md";    Dst = "CLAUDE.md";    Type = "File"   },
    @{ Src = ".claude";      Dst = ".claude";       Type = "Folder" },
    @{ Src = "integrations"; Dst = "integrations";  Type = "Folder" },
    @{ Src = "scripts";      Dst = "scripts";       Type = "Folder" }
)

$Conflicts = @()

foreach ($item in $CopyItems) {
    $srcPath = Join-Path $TemplateRoot $item.Src
    $dstPath = Join-Path $Target $item.Dst

    if (-not (Test-Path $srcPath)) {
        Write-Warn "Template source not found, skipping: $($item.Src)"
        continue
    }

    if (Test-Path $dstPath) {
        $Conflicts += $item.Dst
        Write-Warn "Already exists in target - skipping: $($item.Dst)"
        continue
    }

    if ($item.Type -eq 'Folder') {
        Copy-Item $srcPath -Destination $dstPath -Recurse -Force
        Write-Pass "Copied folder : $($item.Dst)"
    } else {
        Copy-Item $srcPath -Destination $dstPath -Force
        Write-Pass "Copied file   : $($item.Dst)"
    }
}

if ($Conflicts.Count -gt 0) {
    Write-Host ""
    Write-Warn "The following already existed and were NOT overwritten:"
    foreach ($c in $Conflicts) {
        Write-Host "    $c" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "  To update manually, copy from: $TemplateRoot" -ForegroundColor Gray
    Write-Host ""
}

# =============================================================================
# STEP 3 - Update .gitignore in target repo
# =============================================================================
Write-Header "Step 3 of 3 - Updating .gitignore in target repo"

$gitignorePath = Join-Path $Target '.gitignore'
if (-not (Test-Path $gitignorePath)) {
    "" | Set-Content $gitignorePath
}

$existing = Get-Content $gitignorePath -ErrorAction SilentlyContinue

$entries = @(
    "# Claude setup - never commit secrets or state",
    ".env",
    ".claude/setup.log",
    ".claude/validate.log",
    ".claude/.setup-state",
    ".claude/project.env",
    ".claude/enabled-integrations"
)

$added = $false
foreach ($entry in $entries) {
    if ($existing -notcontains $entry) {
        Add-Content -Path $gitignorePath -Value $entry
        $added = $true
    }
}

if ($added) {
    Write-Pass "Added Claude entries to .gitignore"
} else {
    Write-Pass ".gitignore already has all required entries."
}

# =============================================================================
# Summary
# =============================================================================
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Bootstrap complete" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Target repo is ready:" -ForegroundColor Green
Write-Host "    $Target"
Write-Host ""
Write-Host "  Next steps:"
Write-Host ""
Write-Host "  1. Open Claude Code in the client repo:"
Write-Host "       cd `"$Target`""
Write-Host "       claude"
Write-Host ""
Write-Host "  2. Generate the filled CLAUDE.md:"
Write-Host "       /generate-claude-md"
Write-Host ""
Write-Host "  3. Review any TODO comments Claude flagged, confirm with client"
Write-Host ""
Write-Host "  4. Run the setup wizard:"
Write-Host "       .\scripts\setup.ps1"
Write-Host ""
Write-Host "  5. Validate everything is live:"
Write-Host "       .\scripts\validate.ps1"
Write-Host ""

if ($Conflicts.Count -gt 0) {
    Write-Host "  NOTE: $($Conflicts.Count) item(s) skipped due to conflicts." -ForegroundColor Yellow
    Write-Host "  Review them manually before proceeding." -ForegroundColor Yellow
    Write-Host ""
}