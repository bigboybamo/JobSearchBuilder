# =============================================================================
# setup.ps1 - Claude .NET Project Setup
# Interactive first-run script. Run once per project engagement.
# Compatible with: Windows (PowerShell 5.1+), macOS, Linux (PowerShell Core 7+)
#
# Usage:
#   Windows:        .\scripts\setup.ps1
#   Mac/Linux:      pwsh ./scripts/setup.ps1
# =============================================================================

#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# -----------------------------------------------------------------------------
# Colours & helpers
# -----------------------------------------------------------------------------
function Write-Header($text) {
    Write-Host ""
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "  $text" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Log($text) {
    $timestamp = Get-Date -Format 'HH:mm:ss'
    "[$timestamp] $text" | Add-Content -Path $LogFile
}

function Write-Pass($text) {
    Write-Host "  [OK] $text" -ForegroundColor Green
    Write-Log "PASS: $text"
}

function Write-Fail($text) {
    Write-Host "  [X] $text" -ForegroundColor Red
    Write-Log "FAIL: $text"
}

function Write-Warn($text) {
    Write-Host "  [!] $text" -ForegroundColor Yellow
    Write-Log "WARN: $text"
}

function Write-Step($text) {
    Write-Host "  --> $text" -ForegroundColor Cyan
    Write-Log "INFO: $text"
}

function Confirm-Step($prompt) {
    while ($true) {
        $reply = Read-Host "  [ ] $prompt [y/n]"
        if ($reply -match '^[Yy]$') { return $true }
        if ($reply -match '^[Nn]$') { return $false }
        Write-Host "      Please answer y or n." -ForegroundColor Yellow
    }
}

function Read-Secret($prompt) {
    # Reads input without echoing to terminal
    $secure = Read-Host "  [ ] $prompt" -AsSecureString
    $plain  = [System.Net.NetworkCredential]::new('', $secure).Password
    return $plain
}

function Mark-Done($step) {
    Add-Content -Path $StateFile -Value $step
}

function Is-Done($step) {
    if (-not (Test-Path $StateFile)) { return $false }
    return (Get-Content $StateFile) -contains $step
}

# -----------------------------------------------------------------------------
# Paths
# -----------------------------------------------------------------------------
$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot    = Split-Path -Parent $ScriptDir
$ClaudeDir   = Join-Path $RepoRoot '.claude'
$StateFile   = Join-Path $ClaudeDir '.setup-state'
$LogFile     = Join-Path $ClaudeDir 'setup.log'
$EnvFile     = Join-Path $RepoRoot '.env'
$ProjectEnv  = Join-Path $ClaudeDir 'project.env'
$EnabledFile = Join-Path $ClaudeDir 'enabled-integrations'

# Claude Code global settings - location differs by OS
if ($env:OS -eq 'Windows_NT') {
    $ClaudeSettings = Join-Path $env:APPDATA 'Claude\settings.json'
} else {
    $ClaudeSettings = Join-Path $HOME '.claude/settings.json'
}

New-Item -ItemType Directory -Force -Path $ClaudeDir | Out-Null
"" | Set-Content -Path $LogFile

# -----------------------------------------------------------------------------
# Banner
# -----------------------------------------------------------------------------
Clear-Host
Write-Host ""
Write-Host "  +===========================================+" -ForegroundColor Cyan
Write-Host "  |   Claude .NET Project Setup               |" -ForegroundColor Cyan
Write-Host "  |   Interactive configuration wizard        |" -ForegroundColor Cyan
Write-Host "  +===========================================+" -ForegroundColor Cyan
Write-Host ""
Write-Host "  This script configures Claude Code for your .NET project."
Write-Host "  Completed steps are skipped automatically on re-runs."
Write-Host ""
Write-Host "  Log file: $LogFile"
Write-Host ""

Write-Log "Setup started. Repo root: $RepoRoot"
Write-Log "PowerShell version: $($PSVersionTable.PSVersion)"
Write-Log "OS: $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription)"

# =============================================================================
# STEP 1 - Prerequisites
# =============================================================================
Write-Header "Step 1 of 6 - Checking prerequisites"

$PrereqsPass = $true

# Node.js
$node = Get-Command node -ErrorAction SilentlyContinue
if ($node) {
    $nodeVersion = & node --version
    Write-Pass "Node.js found: $nodeVersion"
} else {
    Write-Fail "Node.js is not installed."
    Write-Host "    Install from: https://nodejs.org (LTS recommended)" -ForegroundColor Yellow
    $PrereqsPass = $false
}

# npx
$npxCmd = Get-Command npx -ErrorAction SilentlyContinue
if ($npxCmd) {
    $npxVersion = & npx --version
    Write-Pass "npx found: v$npxVersion"
} else {
    Write-Fail "npx is not available. Install or update Node.js."
    $PrereqsPass = $false
}

# .NET SDK
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnetCmd) {
    $dotnetVersion = & dotnet --version
    Write-Pass ".NET SDK found: $dotnetVersion"

    $major = [int]($dotnetVersion -split '\.')[0]
    if ($major -lt 8) {
        Write-Warn ".NET $dotnetVersion detected. This template targets net8.0+."
        Write-Warn "Consider upgrading: https://dotnet.microsoft.com/download"
    }
} else {
    Write-Fail ".NET SDK is not installed."
    Write-Host "    Install from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    $PrereqsPass = $false
}

# Claude Code
$claudeCmd = Get-Command claude -ErrorAction SilentlyContinue
if ($claudeCmd) {
    try   { $claudeVersion = & claude --version 2>$null } catch { $claudeVersion = 'unknown' }
    Write-Pass "Claude Code found: $claudeVersion"
} else {
    Write-Fail "Claude Code (claude CLI) is not installed."
    Write-Host ""
    Write-Host "    Install it by running:" -ForegroundColor Yellow
    Write-Host "      npm install -g @anthropic-ai/claude-code" -ForegroundColor White
    Write-Host ""
    $PrereqsPass = $false
}

# Git
$gitCmd = Get-Command git -ErrorAction SilentlyContinue
if ($gitCmd) {
    $gitVersion = & git --version
    Write-Pass "$gitVersion"
} else {
    Write-Fail "Git is not installed. Install from: https://git-scm.com"
    $PrereqsPass = $false
}

if (-not $PrereqsPass) {
    Write-Host ""
    Write-Fail "One or more prerequisites are missing. Fix the issues above and re-run setup.ps1."
    exit 1
}

Write-Pass "All prerequisites satisfied."

# =============================================================================
# STEP 2 - Project identity
# =============================================================================
Write-Header "Step 2 of 6 - Project identity"

if (Is-Done "project-identity") {
    Write-Pass "Project identity already configured - skipping."
} else {
    Write-Host "  Claude needs a few details about this project."
    Write-Host ""

    do {
        $ProjectName = Read-Host "  [ ] Project name (e.g. Acme.OrderService)"
    } while ([string]::IsNullOrWhiteSpace($ProjectName))

    $SolutionPath = Read-Host "  [ ] Solution file path relative to repo root (e.g. src\Acme.OrderService.sln)"

    $TargetFramework = Read-Host "  [ ] Target framework (press Enter for net8.0)"
    if ([string]::IsNullOrWhiteSpace($TargetFramework)) { $TargetFramework = 'net8.0' }

    @"
PROJECT_NAME=$ProjectName
SOLUTION_PATH=$SolutionPath
TARGET_FRAMEWORK=$TargetFramework
"@ | Set-Content -Path $ProjectEnv

    Write-Pass "Project identity saved to .claude\project.env"
    Write-Log "Project: $ProjectName | Solution: $SolutionPath | TFM: $TargetFramework"
    Mark-Done "project-identity"
}

# =============================================================================
# STEP 3 - MCP integrations
# =============================================================================
Write-Header "Step 3 of 6 - MCP integrations"

if (Is-Done "mcp-integrations") {
    Write-Pass "MCP integrations already configured - skipping."
} else {
    Write-Host "  Select which integrations to enable for this project."
    Write-Host "  Credentials will be collected and saved to your .env file."
    Write-Host ""

    $EnabledIntegrations = @()

    function Append-Env($key, $value) {
        "$key=$value" | Add-Content -Path $EnvFile
    }

    function Env-Has($key) {
        if (-not (Test-Path $EnvFile)) { return $false }
        return (Get-Content $EnvFile) -match "^$key="
    }

    # --- GitHub ---
    if (Confirm-Step "Enable GitHub integration ") {
        $EnabledIntegrations += 'github'
        Write-Log "Integration enabled: github"

        if (-not (Env-Has 'GITHUB_PERSONAL_ACCESS_TOKEN')) {
            Write-Host ""
            Write-Host "    GitHub Personal Access Token" -ForegroundColor Cyan
            Write-Host "    Required scopes: repo, read:org, read:user, read:project" -ForegroundColor Gray
            Write-Host "    Generate at: https://github.com/settings/tokens" -ForegroundColor Gray
            Write-Host ""
            $pat = Read-Secret "Paste your GitHub PAT (input hidden)"
            Append-Env 'GITHUB_PERSONAL_ACCESS_TOKEN' $pat
            Write-Pass "GitHub PAT saved to .env"
        } else {
            Write-Pass "GitHub PAT already in .env - skipping."
        }

        Write-Step "Installing GitHub MCP server..."
        try {
            & npx -y "@modelcontextprotocol/server-github" --version 2>$null | Out-Null
            Write-Pass "GitHub MCP server installed."
        } catch {
            Write-Warn "GitHub MCP server install may need a manual check."
        }
    }

    # --- Slack ---
    if (Confirm-Step "Enable Slack integration ") {
        $EnabledIntegrations += 'slack'
        Write-Log "Integration enabled: slack"

        if (-not (Env-Has 'SLACK_BOT_TOKEN')) {
            Write-Host ""
            Write-Host "    Slack Bot Token - starts with xoxb-" -ForegroundColor Cyan
            Write-Host "    Required scopes: channels:history, channels:read, chat:write, users:read" -ForegroundColor Gray
            Write-Host "    Generate at: https://api.slack.com/apps -> OAuth & Permissions" -ForegroundColor Gray
            Write-Host ""
            $slackToken = Read-Secret "Paste your Slack Bot Token (input hidden)"
            $slackTeam  = Read-Host  "  [ ] Slack Team ID (format: T01AB2CD3EF)"
            Append-Env 'SLACK_BOT_TOKEN' $slackToken
            Append-Env 'SLACK_TEAM_ID'   $slackTeam
            Write-Pass "Slack credentials saved to .env"
        } else {
            Write-Pass "Slack credentials already in .env - skipping."
        }

        Write-Step "Installing Slack MCP server..."
        try {
            & npx -y "@modelcontextprotocol/server-slack" --version 2>$null | Out-Null
            Write-Pass "Slack MCP server installed."
        } catch {
            Write-Warn "Slack MCP server install may need a manual check."
        }
    }

    # --- Azure DevOps ---
    if (Confirm-Step "Enable Azure DevOps integration ") {
        $EnabledIntegrations += 'azure-devops'
        Write-Log "Integration enabled: azure-devops"

        if (-not (Env-Has 'AZURE_DEVOPS_PAT')) {
            Write-Host ""
            Write-Host "    Azure DevOps configuration" -ForegroundColor Cyan
            Write-Host "    Generate a PAT at: https://dev.azure.com/{org}/_usersSettings/tokens" -ForegroundColor Gray
            Write-Host "    Required scopes: Code (R/W), Work Items (R/W), Build (Read)" -ForegroundColor Gray
            Write-Host ""
            $adoUrl     = Read-Host  "  [ ] Azure DevOps org URL (e.g. https://dev.azure.com/myorg)"
            $adoPat     = Read-Secret "Azure DevOps PAT (input hidden)"
            $adoProject = Read-Host  "  [ ] Default project name"
            Append-Env 'AZURE_DEVOPS_ORG_URL'          $adoUrl
            Append-Env 'AZURE_DEVOPS_AUTH_METHOD'       'pat'
            Append-Env 'AZURE_DEVOPS_PAT'               $adoPat
            Append-Env 'AZURE_DEVOPS_DEFAULT_PROJECT'   $adoProject
            Write-Pass "Azure DevOps credentials saved to .env"
        } else {
            Write-Pass "Azure DevOps credentials already in .env - skipping."
        }

        Write-Step "Installing Azure DevOps MCP server..."
        try {
            & npx -y "@tiberriver256/mcp-server-azure-devops" --version 2>$null | Out-Null
            Write-Pass "Azure DevOps MCP server installed."
        } catch {
            Write-Warn "Azure DevOps MCP server install may need a manual check."
        }
    }

    # --- Jira ---
    if (Confirm-Step "Enable Jira integration ") {
        $EnabledIntegrations += 'jira'
        Write-Log "Integration enabled: jira"

        if (-not (Env-Has 'JIRA_API_TOKEN')) {
            Write-Host ""
            Write-Host "    Jira configuration" -ForegroundColor Cyan
            Write-Host "    Generate an API token at: https://id.atlassian.com/manage-profile/security/api-tokens" -ForegroundColor Gray
            Write-Host ""
            $jiraUrl     = Read-Host  "  [ ] Jira URL (e.g. https://myorg.atlassian.net)"
            $jiraUser    = Read-Host  "  [ ] Jira account email"
            $jiraToken   = Read-Secret "Jira API token (input hidden)"
            $jiraProject = Read-Host  "  [ ] Default project key (e.g. ACME)"
            Append-Env 'JIRA_URL'                 $jiraUrl
            Append-Env 'JIRA_USERNAME'            $jiraUser
            Append-Env 'JIRA_API_TOKEN'           $jiraToken
            Append-Env 'JIRA_DEFAULT_PROJECT_KEY' $jiraProject
            Write-Pass "Jira credentials saved to .env"
        } else {
            Write-Pass "Jira credentials already in .env - skipping."
        }

        Write-Step "Installing Jira MCP server..."
        try {
            & npx -y "@modelcontextprotocol/server-jira" --version 2>$null | Out-Null
            Write-Pass "Jira MCP server installed."
        } catch {
            Write-Warn "Jira MCP server install may need a manual check."
        }
    }

    $EnabledIntegrations | Set-Content -Path $EnabledFile
    Mark-Done "mcp-integrations"
}

# =============================================================================
# STEP 4 - Merge MCP config into Claude Code settings
# =============================================================================
Write-Header "Step 4 of 6 - Merging MCP config into Claude Code settings"

if (Is-Done "mcp-config-merged") {
    Write-Pass "MCP config already merged - skipping."
} else {
    $settingsDir = Split-Path -Parent $ClaudeSettings
    if (-not (Test-Path $settingsDir)) {
        New-Item -ItemType Directory -Force -Path $settingsDir | Out-Null
    }
    if (-not (Test-Path $ClaudeSettings)) {
        '{"mcpServers":{}}' | Set-Content -Path $ClaudeSettings
        Write-Log "Created empty Claude Code settings at $ClaudeSettings"
    }

    if (Test-Path $EnabledFile) {
        $integrations = Get-Content $EnabledFile
        foreach ($integration in $integrations) {
            $configPath = Join-Path $RepoRoot "integrations\$integration\mcp-config.json"
            if (Test-Path $configPath) {
                Write-Step "Merging $integration MCP config..."
                try {
                    $settings = Get-Content $ClaudeSettings -Raw | ConvertFrom-Json
                    $config   = Get-Content $configPath     -Raw | ConvertFrom-Json

                    # Ensure mcpServers property exists
                    if (-not $settings.PSObject.Properties['mcpServers']) {
                        $settings | Add-Member -NotePropertyName 'mcpServers' -NotePropertyValue ([PSCustomObject]@{})
                    }

                    # Merge each server entry
                    foreach ($server in $config.mcpServers.PSObject.Properties) {
                        $settings.mcpServers | Add-Member `
                            -NotePropertyName  $server.Name `
                            -NotePropertyValue $server.Value `
                            -Force
                    }

                    $settings | ConvertTo-Json -Depth 10 | Set-Content -Path $ClaudeSettings
                    Write-Pass "Merged $integration into Claude Code settings."
                } catch {
                    Write-Warn "Could not merge $integration - check $ClaudeSettings manually."
                    Write-Log "Merge error for ${integration}: $_"
                }
            } else {
                Write-Warn "No mcp-config.json found for $integration at $configPath"
            }
        }
    } else {
        Write-Warn "No integrations registered - nothing to merge."
    }

    Mark-Done "mcp-config-merged"
}

# =============================================================================
# STEP 5 - .gitignore guardrails
# =============================================================================
Write-Header "Step 5 of 6 - .gitignore guardrails"

if (Is-Done "gitignore") {
    Write-Pass ".gitignore already updated - skipping."
} else {
    $gitignorePath = Join-Path $RepoRoot '.gitignore'
    if (-not (Test-Path $gitignorePath)) { "" | Set-Content $gitignorePath }

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
        Write-Pass "Added Claude-specific entries to .gitignore"
    } else {
        Write-Pass ".gitignore already has all required entries."
    }

    Mark-Done "gitignore"
}

# =============================================================================
# STEP 6 - Summary
# =============================================================================
Write-Header "Step 6 of 6 - Setup complete"

Write-Host "  Your Claude .NET environment is ready." -ForegroundColor Green
Write-Host ""
Write-Host "  What was configured:"
Write-Host "  * Prerequisites verified (Node.js, .NET SDK, Claude Code, Git)"
Write-Host "  * Project identity saved to .claude\project.env"
Write-Host "  * MCP servers installed and merged into Claude Code settings"
Write-Host "  * .gitignore updated to protect secrets and state files"
Write-Host ""
Write-Host "  Next steps:"
Write-Host "  1. Fill in the remaining [PLACEHOLDER] sections in CLAUDE.md"
Write-Host "  2. Run .\scripts\validate.ps1 to confirm all connections are live"
Write-Host "  3. Open Claude Code in this repo: claude"
Write-Host ""
Write-Host "  Log saved to: $LogFile"
Write-Host ""

Write-Log "Setup completed successfully."