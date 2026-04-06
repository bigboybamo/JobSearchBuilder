# =============================================================================
# validate.ps1 - Claude .NET Project Validator
# Run after setup.ps1, or any time you want to confirm everything still works.
# Exit code 0 = all checks passed. Exit code 1 = one or more checks failed.
# Compatible with: Windows (PowerShell 5.1+), macOS, Linux (PowerShell Core 7+)
#
# Usage:
#   Windows:    .\scripts\validate.ps1
#   Mac/Linux:  pwsh ./scripts/validate.ps1
# =============================================================================

#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# -----------------------------------------------------------------------------
# Helpers
# -----------------------------------------------------------------------------
$PassCount = 0
$FailCount = 0
$WarnCount = 0

function Write-Section($text) {
    Write-Host ""
    Write-Host "-- $text" -ForegroundColor Cyan
    Write-Log "--- $text ---"
}

function Write-Log($text) {
    $timestamp = Get-Date -Format 'HH:mm:ss'
    "[$timestamp] $text" | Add-Content -Path $LogFile
}

function Write-Pass($text) {
    Write-Host "  [OK] $text" -ForegroundColor Green
    Write-Log "PASS: $text"
    $script:PassCount++
}

function Write-Fail($text) {
    Write-Host "  [X]  $text" -ForegroundColor Red
    Write-Log "FAIL: $text"
    $script:FailCount++
}

function Write-Warn($text) {
    Write-Host "  [!]  $text" -ForegroundColor Yellow
    Write-Log "WARN: $text"
    $script:WarnCount++
}

function Get-EnvValue($key) {
    if (-not (Test-Path $EnvFile)) { return $null }
    $line = (Get-Content $EnvFile) | Where-Object { $_ -match "^$key=(.*)$" } | Select-Object -First 1
    if ($line) { return ($line -split '=', 2)[1].Trim().Trim('"').Trim("'") }
    return $null
}

function Invoke-ApiCheck {
    param(
        [string]$Label,
        [string]$Uri,
        [hashtable]$Headers = @{},
        [string]$AuthUser = '',
        [string]$AuthPass = ''
    )
    try {
        $params = @{
            Uri             = $Uri
            Headers         = $Headers
            Method          = 'GET'
            UseBasicParsing = $true
            TimeoutSec      = 10
        }
        if ($AuthUser -or $AuthPass) {
            $encoded = [Convert]::ToBase64String(
                [Text.Encoding]::ASCII.GetBytes("${AuthUser}:${AuthPass}")
            )
            $params.Headers['Authorization'] = "Basic $encoded"
        }
        $response = Invoke-WebRequest @params
        if ($response.StatusCode -eq 200) {
            Write-Pass "$Label - authenticated successfully (HTTP 200)"
        } else {
            Write-Fail "$Label - unexpected status: HTTP $($response.StatusCode)"
        }
    } catch [System.Net.WebException] {
        $code = [int]$_.Exception.Response.StatusCode
        if ($code -eq 401) {
            Write-Fail "$Label - credentials rejected (HTTP 401). Check your token."
        } elseif ($code -eq 403) {
            Write-Fail "$Label - forbidden (HTTP 403). Check token scopes/permissions."
        } else {
            Write-Fail "$Label - request failed (HTTP $code): $($_.Exception.Message)"
        }
    } catch {
        Write-Fail "$Label - could not reach endpoint: $($_.Exception.Message)"
    }
}

# -----------------------------------------------------------------------------
# Paths
# -----------------------------------------------------------------------------
$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot   = Split-Path -Parent $ScriptDir
$ClaudeDir  = Join-Path $RepoRoot '.claude'
$LogFile    = Join-Path $ClaudeDir 'validate.log'
$EnvFile    = Join-Path $RepoRoot '.env'
$EnabledFile = Join-Path $ClaudeDir 'enabled-integrations'
$ProjectEnv = Join-Path $ClaudeDir 'project.env'

New-Item -ItemType Directory -Force -Path $ClaudeDir | Out-Null
"" | Set-Content -Path $LogFile

# -----------------------------------------------------------------------------
# Banner
# -----------------------------------------------------------------------------
Clear-Host
Write-Host ""
Write-Host "  Claude .NET Project Validator" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Checking your environment. Log: $LogFile"
Write-Host ""

Write-Log "Validation started. Repo root: $RepoRoot"
Write-Log "PowerShell version: $($PSVersionTable.PSVersion)"

# =============================================================================
# CHECK 1 - Prerequisites
# =============================================================================
Write-Section "Prerequisites"

if (Get-Command node -ErrorAction SilentlyContinue) {
    Write-Pass "Node.js: $(& node --version)"
} else {
    Write-Fail "Node.js not found - install from https://nodejs.org"
}

if (Get-Command npx -ErrorAction SilentlyContinue) {
    Write-Pass "npx: v$(& npx --version)"
} else {
    Write-Fail "npx not found - install or update Node.js"
}

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Pass ".NET SDK: $(& dotnet --version)"
} else {
    Write-Fail ".NET SDK not found - install from https://dotnet.microsoft.com/download"
}

if (Get-Command claude -ErrorAction SilentlyContinue) {
    try { $v = & claude --version 2>$null } catch { $v = 'unknown' }
    Write-Pass "Claude Code: $v"
} else {
    Write-Fail "Claude Code not found - run: npm install -g @anthropic-ai/claude-code"
}

if (Get-Command git -ErrorAction SilentlyContinue) {
    Write-Pass "$(& git --version)"
} else {
    Write-Fail "Git not found - install from https://git-scm.com"
}

# =============================================================================
# CHECK 2 - .env keys are non-empty
# =============================================================================
Write-Section "Environment variables (.env)"

if (-not (Test-Path $EnvFile)) {
    Write-Fail ".env file not found at repo root. Run setup.ps1 first."
} else {
    Write-Pass ".env file exists."

    function Check-EnvKey($key) {
        $val = Get-EnvValue $key
        if (-not [string]::IsNullOrWhiteSpace($val)) {
            Write-Pass "$key is set."
        } else {
            Write-Fail "$key is missing or empty in .env"
        }
    }

    if (Test-Path $EnabledFile) {
        $integrations = Get-Content $EnabledFile
        foreach ($integration in $integrations) {
            switch ($integration) {
                'github' {
                    Check-EnvKey 'GITHUB_PERSONAL_ACCESS_TOKEN'
                }
                'slack' {
                    Check-EnvKey 'SLACK_BOT_TOKEN'
                    Check-EnvKey 'SLACK_TEAM_ID'
                }
                'azure-devops' {
                    Check-EnvKey 'AZURE_DEVOPS_ORG_URL'
                    Check-EnvKey 'AZURE_DEVOPS_PAT'
                    Check-EnvKey 'AZURE_DEVOPS_DEFAULT_PROJECT'
                }
                'jira' {
                    Check-EnvKey 'JIRA_URL'
                    Check-EnvKey 'JIRA_USERNAME'
                    Check-EnvKey 'JIRA_API_TOKEN'
                    Check-EnvKey 'JIRA_DEFAULT_PROJECT_KEY'
                }
            }
        }
    } else {
        Write-Warn "No enabled-integrations file found - skipping per-integration env checks."
        Write-Warn "Run setup.ps1 to register your integrations."
    }
}

# =============================================================================
# CHECK 3 - MCP server connectivity
# =============================================================================
Write-Section "MCP server connectivity"

if (-not (Test-Path $EnabledFile)) {
    Write-Warn "No integrations registered - skipping connectivity checks."
} else {
    $integrations = Get-Content $EnabledFile

    foreach ($integration in $integrations) {
        switch ($integration) {
            'github' {
                $token = Get-EnvValue 'GITHUB_PERSONAL_ACCESS_TOKEN'
                if ([string]::IsNullOrWhiteSpace($token)) {
                    Write-Fail "GitHub - cannot check, token is empty."
                } else {
                    Invoke-ApiCheck `
                        -Label   "GitHub API" `
                        -Uri     "https://api.github.com/user" `
                        -Headers @{
                            'Authorization' = "Bearer $token"
                            'Accept'        = 'application/vnd.github+json'
                            'User-Agent'    = 'claude-dotnet-setup'
                        }
                }
            }
            'slack' {
                $token = Get-EnvValue 'SLACK_BOT_TOKEN'
                if ([string]::IsNullOrWhiteSpace($token)) {
                    Write-Fail "Slack - cannot check, token is empty."
                } else {
                    try {
                        $response = Invoke-RestMethod `
                            -Uri     'https://slack.com/api/auth.test' `
                            -Method  POST `
                            -Headers @{ 'Authorization' = "Bearer $token" } `
                            -TimeoutSec 10
                        if ($response.ok) {
                            Write-Pass "Slack API - authenticated successfully (team: $($response.team))"
                        } else {
                            Write-Fail "Slack API - authentication failed: $($response.error)"
                        }
                    } catch {
                        Write-Fail "Slack API - could not reach endpoint: $($_.Exception.Message)"
                    }
                }
            }
            'azure-devops' {
                $pat    = Get-EnvValue 'AZURE_DEVOPS_PAT'
                $orgUrl = Get-EnvValue 'AZURE_DEVOPS_ORG_URL'
                if ([string]::IsNullOrWhiteSpace($pat) -or [string]::IsNullOrWhiteSpace($orgUrl)) {
                    Write-Fail "Azure DevOps - cannot check, PAT or ORG_URL is empty."
                } else {
                    Invoke-ApiCheck `
                        -Label    "Azure DevOps API" `
                        -Uri      "$orgUrl/_apis/projects?api-version=7.1" `
                        -AuthUser "" `
                        -AuthPass $pat
                }
            }
            'jira' {
                $token = Get-EnvValue 'JIRA_API_TOKEN'
                $user  = Get-EnvValue 'JIRA_USERNAME'
                $url   = Get-EnvValue 'JIRA_URL'
                if ([string]::IsNullOrWhiteSpace($token) -or
                    [string]::IsNullOrWhiteSpace($user)  -or
                    [string]::IsNullOrWhiteSpace($url)) {
                    Write-Fail "Jira - cannot check, one or more credentials are empty."
                } else {
                    Invoke-ApiCheck `
                        -Label    "Jira API" `
                        -Uri      "$url/rest/api/3/myself" `
                        -Headers  @{ 'Accept' = 'application/json' } `
                        -AuthUser $user `
                        -AuthPass $token
                }
            }
            default {
                Write-Warn "Unknown integration '$integration' - skipping."
            }
        }
    }
}

# =============================================================================
# CHECK 4 - dotnet build
# =============================================================================
Write-Section "dotnet build"

if (-not (Test-Path $ProjectEnv)) {
    Write-Warn "project.env not found - skipping dotnet build check."
    Write-Warn "Run setup.ps1 first to register project identity."
} else {
    $projectConfig = @{}
    Get-Content $ProjectEnv | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            $projectConfig[$Matches[1]] = $Matches[2]
        }
    }

    $solutionRelPath  = $projectConfig['SOLUTION_PATH']
    $solutionFullPath = if ($solutionRelPath) { Join-Path $RepoRoot $solutionRelPath } else { $null }

    if ($solutionFullPath -and (Test-Path $solutionFullPath)) {
        $buildTarget = $solutionFullPath
    } elseif ($solutionRelPath) {
        Write-Warn "Solution file not found at $solutionFullPath - attempting build from repo root."
        $buildTarget = $RepoRoot
    } else {
        Write-Warn "SOLUTION_PATH not set - attempting build from repo root."
        $buildTarget = $RepoRoot
    }

    Write-Host "  Running: dotnet build $buildTarget --no-incremental -v q" -ForegroundColor Gray
    Write-Host ""

    try {
        $buildOutput = & dotnet build $buildTarget --no-incremental -v q 2>&1
        $buildOutput | ForEach-Object { Write-Host "  $_" }
        $buildOutput | Add-Content -Path $LogFile

        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Pass "dotnet build succeeded."
        } else {
            Write-Host ""
            Write-Fail "dotnet build failed. See output above or $LogFile for details."
        }
    } catch {
        Write-Fail "dotnet build threw an exception: $($_.Exception.Message)"
    }
}

# =============================================================================
# Summary report
# =============================================================================
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Validation Summary"                   -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Passed:   $PassCount" -ForegroundColor Green
Write-Host "  Warnings: $WarnCount" -ForegroundColor Yellow
Write-Host "  Failed:   $FailCount" -ForegroundColor Red
Write-Host ""

Write-Log "Validation finished - Passed: $PassCount | Warnings: $WarnCount | Failed: $FailCount"

if ($FailCount -gt 0) {
    Write-Host "  Some checks failed. Review the output above." -ForegroundColor Red
    Write-Host "  Full log: $LogFile"
    Write-Host ""
    exit 1
} elseif ($WarnCount -gt 0) {
    Write-Host "  All checks passed with warnings. Review warnings above." -ForegroundColor Yellow
    Write-Host "  Full log: $LogFile"
    Write-Host ""
    exit 0
} else {
    Write-Host "  All checks passed. Your environment is ready." -ForegroundColor Green
    Write-Host "  Full log: $LogFile"
    Write-Host ""
    exit 0
}