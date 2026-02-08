# Build and package LibraryApi.Web for Azure App Service Zip Deploy
# Usage: .\scripts\Deploy-ToAzure.ps1

param(
    [string]$ProjectPath = "src\LibraryApi.Web\LibraryApi.Web.csproj",
    [string]$Configuration = "Release",
    [string]$PublishDir = "artifacts\publish",
    [string]$ZipPath = "artifacts\deploy\LibraryApi.Web.zip"
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

Set-Location $projectRoot
Write-Host "Working directory: $projectRoot" -ForegroundColor Gray

Write-Host "=== Building and packaging for Azure Zip Deploy ===" -ForegroundColor Cyan

$publishDirPath = Join-Path $projectRoot $PublishDir
$zipDirPath = Split-Path -Path (Join-Path $projectRoot $ZipPath) -Parent

if (-not (Test-Path $publishDirPath)) {
    New-Item -ItemType Directory -Force -Path $publishDirPath | Out-Null
    Write-Host "Created directory: $publishDirPath" -ForegroundColor Gray
}

if (-not (Test-Path $zipDirPath)) {
    New-Item -ItemType Directory -Force -Path $zipDirPath | Out-Null
    Write-Host "Created directory: $zipDirPath" -ForegroundColor Gray
}

Write-Host "`nRestoring dependencies..." -ForegroundColor Yellow
dotnet restore $ProjectPath --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to restore packages" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing application ($Configuration)..." -ForegroundColor Yellow
dotnet publish $ProjectPath `
    -c $Configuration `
    -o $publishDirPath `
    --nologo `
    /p:PublishReadyToRun=false `
    /p:PublishSingleFile=false `
    /p:IncludeSymbols=false `
    /p:SelfContained=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to publish application" -ForegroundColor Red
    exit 1
}

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
    Write-Host "Removed previous ZIP file" -ForegroundColor Gray
}

Write-Host "`nCreating ZIP package..." -ForegroundColor Yellow

if (-not (Test-Path $publishDirPath)) {
    Write-Host "Error: Publish directory does not exist: $publishDirPath" -ForegroundColor Red
    exit 1
}

$items = Get-ChildItem -Path $publishDirPath -Force
if ($items.Count -eq 0) {
    Write-Host "Error: Publish directory is empty: $publishDirPath" -ForegroundColor Red
    exit 1
}

Write-Host "Found $($items.Count) items in publish directory" -ForegroundColor Gray
$zipFullPath = Join-Path $projectRoot $ZipPath

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($publishDirPath, $zipFullPath)

Write-Host "`n[SUCCESS] ZIP created successfully!" -ForegroundColor Green
Write-Host "  Location: $zipFullPath" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host '  1. Open Azure Portal -> App Service -> Deployment Center -> Zip Deploy' -ForegroundColor White
Write-Host "  2. Upload this ZIP file: $ZipPath" -ForegroundColor White
Write-Host "  3. Wait for deployment to complete" -ForegroundColor White
