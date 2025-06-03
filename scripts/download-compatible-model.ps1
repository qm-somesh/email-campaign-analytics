# Download Compatible LLM Model Script
# This script downloads a model known to work well with LLamaSharp

param(
    [string]$ModelPath = "d:\Dev\EmailCampaignReporting\models",
    [string]$ModelName = "llama-2-7b-chat.Q4_K_M.gguf"
)

Write-Host "Email Campaign Reporting - LLM Model Download Script" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green

# Create models directory if it doesn't exist
if (!(Test-Path $ModelPath)) {
    Write-Host "Creating models directory: $ModelPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $ModelPath | Out-Null
}

# Define available models with their download URLs and compatibility info
$AvailableModels = @{
    "llama-2-7b-chat.Q4_K_M.gguf" = @{
        Url = "https://huggingface.co/TheBloke/Llama-2-7B-Chat-GGUF/resolve/main/llama-2-7b-chat.Q4_K_M.gguf"
        Size = "4.1 GB"
        Description = "Llama 2 7B Chat model - Well tested with LLamaSharp"
        Compatibility = "Excellent"
    }
    "phi-2.Q4_K_M.gguf" = @{
        Url = "https://huggingface.co/microsoft/phi-2-gguf/resolve/main/phi-2.q4_k_m.gguf"
        Size = "1.7 GB"
        Description = "Microsoft Phi-2 model - Smaller and faster"
        Compatibility = "Good"
    }
    "tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf" = @{
        Url = "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf"
        Size = "669 MB"
        Description = "TinyLlama 1.1B - Very fast, good for testing"
        Compatibility = "Excellent"
    }
}

# Display available models
Write-Host "`nAvailable Models:" -ForegroundColor Cyan
$i = 1
$ModelList = @()
foreach ($model in $AvailableModels.Keys) {
    $info = $AvailableModels[$model]
    Write-Host "$i. $model" -ForegroundColor White
    Write-Host "   Size: $($info.Size) | Compatibility: $($info.Compatibility)" -ForegroundColor Gray
    Write-Host "   Description: $($info.Description)" -ForegroundColor Gray
    Write-Host ""
    $ModelList += $model
    $i++
}

# Get user choice
if ($AvailableModels.ContainsKey($ModelName)) {
    $SelectedModel = $ModelName
} else {
    do {
        $choice = Read-Host "Select a model to download (1-$($ModelList.Count)) or press Enter for default (1)"
        if ([string]::IsNullOrEmpty($choice)) { $choice = "1" }
    } while ($choice -notmatch '^\d+$' -or [int]$choice -lt 1 -or [int]$choice -gt $ModelList.Count)
    
    $SelectedModel = $ModelList[[int]$choice - 1]
}

$ModelInfo = $AvailableModels[$SelectedModel]
$FullPath = Join-Path $ModelPath $SelectedModel

Write-Host "Selected Model: $SelectedModel" -ForegroundColor Green
Write-Host "Download Size: $($ModelInfo.Size)" -ForegroundColor Yellow
Write-Host "Target Path: $FullPath" -ForegroundColor Gray

# Check if model already exists
if (Test-Path $FullPath) {
    $overwrite = Read-Host "`nModel already exists. Overwrite? (y/N)"
    if ($overwrite -ne 'y' -and $overwrite -ne 'Y') {
        Write-Host "Download cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Download the model
Write-Host "`nDownloading $SelectedModel..." -ForegroundColor Green
Write-Host "This may take several minutes depending on your internet connection." -ForegroundColor Yellow

try {
    # Use Invoke-WebRequest with progress bar
    $ProgressPreference = 'Continue'
    Invoke-WebRequest -Uri $ModelInfo.Url -OutFile $FullPath -UseBasicParsing
    
    Write-Host "`nDownload completed successfully!" -ForegroundColor Green
    Write-Host "Model saved to: $FullPath" -ForegroundColor Gray
    
    # Get file size
    $FileSize = (Get-Item $FullPath).Length
    $FileSizeMB = [math]::Round($FileSize / 1MB, 2)
    Write-Host "File size: $FileSizeMB MB" -ForegroundColor Gray
    
    # Update appsettings.Development.json
    $AppSettingsPath = "d:\Dev\EmailCampaignReporting\backend\appsettings.Development.json"
    if (Test-Path $AppSettingsPath) {
        Write-Host "`nUpdating appsettings.Development.json..." -ForegroundColor Yellow
        
        $AppSettings = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json
        if (-not $AppSettings.LLM) {
            $AppSettings | Add-Member -Type NoteProperty -Name "LLM" -Value @{}
        }
        
        $AppSettings.LLM.ModelPath = $FullPath.Replace('\', '/')
        $AppSettings.LLM.ContextSize = 4096
        $AppSettings.LLM.TimeoutSeconds = 120
        
        $AppSettings | ConvertTo-Json -Depth 10 | Set-Content $AppSettingsPath
        Write-Host "Configuration updated successfully!" -ForegroundColor Green
    }
    
    Write-Host "`nNext Steps:" -ForegroundColor Cyan
    Write-Host "1. Restart your backend application" -ForegroundColor White
    Write-Host "2. Test the LLM service at: http://localhost:5037/api/dashboard/natural-language-query" -ForegroundColor White
    Write-Host "3. Try a query like: 'Show me campaign performance for the last month'" -ForegroundColor White
    
} catch {
    Write-Host "`nDownload failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please check your internet connection and try again." -ForegroundColor Yellow
    exit 1
}
