# Download a known compatible model for LLamaSharp 0.17.0
# This script downloads a smaller, more compatible GGUF model

param(
    [string]$ModelsDir = "d:\Dev\EmailCampaignReporting\models",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Create models directory if it doesn't exist
if (!(Test-Path $ModelsDir)) {
    New-Item -ItemType Directory -Path $ModelsDir -Force
    Write-Host "Created models directory: $ModelsDir" -ForegroundColor Green
}

# Model options - smaller, more compatible models
$models = @(
    @{
        Name = "TinyLlama-1.1B-Chat-v1.0.Q4_K_M.gguf"
        Url = "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf"
        Size = "~669MB"
        Description = "TinyLlama 1.1B - Small, fast, compatible model"
    },
    @{
        Name = "Llama-2-7B-Chat-GGUF-q4_0.gguf"
        Url = "https://huggingface.co/TheBloke/Llama-2-7B-Chat-GGUF/resolve/main/llama-2-7b-chat.q4_0.gguf"
        Size = "~3.56GB"
        Description = "Llama-2 7B Chat - Well tested, highly compatible"
    },
    @{
        Name = "Mistral-7B-Instruct-v0.2.Q4_K_M.gguf"
        Url = "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf"
        Size = "~4.37GB"
        Description = "Mistral 7B Instruct - Modern, efficient model"
    }
)

Write-Host "Available compatible models:" -ForegroundColor Cyan
for ($i = 0; $i -lt $models.Count; $i++) {
    $model = $models[$i]
    Write-Host "$($i + 1). $($model.Name)" -ForegroundColor Yellow
    Write-Host "   Size: $($model.Size)" -ForegroundColor Gray
    Write-Host "   Description: $($model.Description)" -ForegroundColor Gray
    Write-Host
}

# Get user choice
do {
    $choice = Read-Host "Select a model to download (1-$($models.Count)) or 'q' to quit"
    if ($choice -eq 'q') {
        Write-Host "Download cancelled." -ForegroundColor Yellow
        exit 0
    }
    $choiceNum = $choice -as [int]
} while ($choiceNum -lt 1 -or $choiceNum -gt $models.Count)

$selectedModel = $models[$choiceNum - 1]
$outputPath = Join-Path $ModelsDir $selectedModel.Name

Write-Host "Selected: $($selectedModel.Name)" -ForegroundColor Green
Write-Host "Size: $($selectedModel.Size)" -ForegroundColor Gray

# Check if file already exists
if ((Test-Path $outputPath) -and !$Force) {
    $overwrite = Read-Host "Model file already exists. Overwrite? (y/n)"
    if ($overwrite -ne 'y') {
        Write-Host "Download cancelled." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "Downloading $($selectedModel.Name)..." -ForegroundColor Cyan
Write-Host "URL: $($selectedModel.Url)" -ForegroundColor Gray
Write-Host "Destination: $outputPath" -ForegroundColor Gray

try {
    # Use Invoke-WebRequest with progress
    $progressPreference = $ProgressPreference
    $ProgressPreference = 'Continue'
    
    Invoke-WebRequest -Uri $selectedModel.Url -OutFile $outputPath -TimeoutSec 3600
    
    $ProgressPreference = $progressPreference
    
    # Verify download
    if (Test-Path $outputPath) {
        $fileSize = (Get-Item $outputPath).Length
        $fileSizeMB = [math]::Round($fileSize / 1024 / 1024, 2)
        Write-Host "✅ Download completed successfully!" -ForegroundColor Green
        Write-Host "File size: $fileSizeMB MB" -ForegroundColor Gray
        Write-Host "Location: $outputPath" -ForegroundColor Gray
        
        # Update appsettings to use the new model
        $appsettingsPath = "d:\Dev\EmailCampaignReporting\backend\appsettings.Development.json"
        if (Test-Path $appsettingsPath) {
            $content = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
            $content.LLM.ModelPath = $outputPath
            $content | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
            Write-Host "✅ Updated appsettings.Development.json with new model path" -ForegroundColor Green
        }
    } else {
        Write-Host "❌ Download failed - file not found after download" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Download failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎉 Model download complete!" -ForegroundColor Green
Write-Host "You can now restart the backend to use the new model." -ForegroundColor Cyan
