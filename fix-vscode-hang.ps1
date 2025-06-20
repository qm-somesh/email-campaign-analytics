# Enhanced VS Code Performance Fix Script for EmailCampaignReporting
# This script addresses VS Code hanging/freezing issues when opening large projects

Write-Host "🔧 VS Code Performance Fix Script" -ForegroundColor Green
Write-Host "Addressing VS Code hanging issues with large projects..." -ForegroundColor Yellow

# Step 1: Kill all VS Code processes
Write-Host "`n1. Killing all VS Code processes..." -ForegroundColor Cyan
Get-Process -Name "Code" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "code" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Step 2: Clean problematic cache directories
Write-Host "`n2. Cleaning cache directories..." -ForegroundColor Cyan
$frontendNodeModules = "d:\Dev\EmailCampaignReporting\frontend\node_modules"
$frontendCache = "d:\Dev\EmailCampaignReporting\frontend\.cache"
$backendBin = "d:\Dev\EmailCampaignReporting\backend\bin"
$backendObj = "d:\Dev\EmailCampaignReporting\backend\obj"

if (Test-Path $frontendCache) {
    Write-Host "Removing frontend .cache directory..." -ForegroundColor Yellow
    Remove-Item $frontendCache -Recurse -Force -ErrorAction SilentlyContinue
}

if (Test-Path $backendBin) {
    Write-Host "Removing backend bin directory..." -ForegroundColor Yellow
    Remove-Item $backendBin -Recurse -Force -ErrorAction SilentlyContinue
}

if (Test-Path $backendObj) {
    Write-Host "Removing backend obj directory..." -ForegroundColor Yellow
    Remove-Item $backendObj -Recurse -Force -ErrorAction SilentlyContinue
}

# Step 3: Clear VS Code workspace state
Write-Host "`n3. Clearing VS Code workspace state..." -ForegroundColor Cyan
$vsCodeUserData = "$env:APPDATA\Code\User\workspaceStorage"
if (Test-Path $vsCodeUserData) {
    $workspaceHash = [System.Security.Cryptography.SHA256]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes("d:\Dev\EmailCampaignReporting"))
    $hashString = [BitConverter]::ToString($workspaceHash) -replace '-'
    $workspaceDir = Get-ChildItem $vsCodeUserData | Where-Object { $_.Name -like "*$($hashString.Substring(0,8))*" }
    if ($workspaceDir) {
        Write-Host "Removing VS Code workspace cache..." -ForegroundColor Yellow
        Remove-Item $workspaceDir.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Step 4: Memory and system info
Write-Host "`n4. System Information:" -ForegroundColor Cyan
$memory = Get-WmiObject -Class Win32_ComputerSystem
$totalRam = [math]::Round($memory.TotalPhysicalMemory / 1GB, 2)
Write-Host "Total RAM: $totalRam GB" -ForegroundColor White

$nodeModulesSize = if (Test-Path $frontendNodeModules) { 
    $size = (Get-ChildItem $frontendNodeModules -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
    [math]::Round($size / 1MB, 2)
} else { 0 }
Write-Host "Frontend node_modules size: $nodeModulesSize MB" -ForegroundColor White

# Step 5: Recommendations
Write-Host "`n5. Performance Recommendations:" -ForegroundColor Cyan
Write-Host "• Open individual folders (frontend/ or backend/) instead of the entire solution" -ForegroundColor Yellow
Write-Host "• Use the workspace file: EmailCampaignReporting.code-workspace" -ForegroundColor Yellow
Write-Host "• If VS Code still hangs, restart with: code --disable-extensions" -ForegroundColor Yellow
Write-Host "• Consider excluding more directories in .vscode/settings.json" -ForegroundColor Yellow

# Step 6: Safe restart options
Write-Host "`n6. VS Code Restart Options:" -ForegroundColor Cyan
Write-Host "A. Open frontend only: code frontend/" -ForegroundColor Green
Write-Host "B. Open backend only: code backend/" -ForegroundColor Green
Write-Host "C. Open workspace file: code EmailCampaignReporting.code-workspace" -ForegroundColor Green
Write-Host "D. Open with disabled extensions: code --disable-extensions ." -ForegroundColor Yellow

Write-Host "`n✅ Performance optimization complete!" -ForegroundColor Green
Write-Host "Try opening VS Code using one of the recommended methods above." -ForegroundColor Green
