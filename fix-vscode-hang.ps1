# VS Code Performance Optimization Script
# Run this script to resolve VS Code hanging issues

Write-Host "=== VS Code Performance Optimization ===" -ForegroundColor Yellow

# 1. Stop any running VS Code processes
Write-Host "1. Stopping VS Code processes..." -ForegroundColor Cyan
Get-Process "Code" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# 2. Clear VS Code workspace cache
Write-Host "2. Clearing VS Code workspace cache..." -ForegroundColor Cyan
$vscodeWorkspace = "$env:APPDATA\Code\User\workspaceStorage"
if (Test-Path $vscodeWorkspace) {
    Get-ChildItem $vscodeWorkspace | Where-Object {$_.Name -like "*EmailCampaignReporting*"} | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

# 3. Quick project size check
Write-Host "3. Project size check..." -ForegroundColor Cyan
$projectSize = (Get-ChildItem -Recurse | Measure-Object -Property Length -Sum).Sum
Write-Host "Total project size: $([math]::Round($projectSize / 1MB, 2)) MB" -ForegroundColor Green

# 4. Verify settings are applied
Write-Host "4. Verifying VS Code settings..." -ForegroundColor Cyan
if (Test-Path ".vscode\settings.json") {
    Write-Host "✓ VS Code settings file exists" -ForegroundColor Green
} else {
    Write-Host "✗ VS Code settings file missing" -ForegroundColor Red
}

# 5. Start VS Code with optimizations
Write-Host "5. Starting VS Code with performance optimizations..." -ForegroundColor Cyan
Write-Host "Running: code . --disable-extensions --max-memory=4096" -ForegroundColor Gray

# Start VS Code with performance flags
Start-Process "code" -ArgumentList ".", "--disable-extensions", "--max-memory=4096" -NoNewWindow

Write-Host "`n=== Optimization Complete ===" -ForegroundColor Green
Write-Host "VS Code should now open without hanging. If issues persist:"
Write-Host "1. Close VS Code completely"
Write-Host "2. Open just the backend folder: code backend"
Write-Host "3. Or open just the frontend folder: code frontend"
