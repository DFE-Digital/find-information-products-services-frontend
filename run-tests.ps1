# PowerShell script to run tests with coverage and reporting

param(
    [string]$Filter = "",
    [switch]$Coverage = $false,
    [switch]$Detailed = $false,
    [string]$OutputDir = "TestResults"
)

Write-Host "Running FIPS Frontend Tests..." -ForegroundColor Green

# Create output directory if it doesn't exist
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Prepare test command
$testCommand = "dotnet test --configuration Release --no-build"

if ($Coverage) {
    $testCommand += " --collect:`"XPlat Code Coverage`" --results-directory `"$OutputDir`""
}

if ($Filter -ne "") {
    $testCommand += " --filter `"$Filter`""
}

if ($Detailed) {
    $testCommand += " --logger `"console;verbosity=detailed`""
} else {
    $testCommand += " --logger `"console;verbosity=normal`""
}

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow
Invoke-Expression $testCommand

if ($LASTEXITCODE -eq 0) {
    Write-Host "All tests passed!" -ForegroundColor Green
    
    if ($Coverage) {
        Write-Host "Coverage report generated in: $OutputDir" -ForegroundColor Cyan
        
        # Try to find and display coverage summary
        $coverageFile = Get-ChildItem -Path $OutputDir -Recurse -Name "coverage.cobertura.xml" | Select-Object -First 1
        if ($coverageFile) {
            Write-Host "Coverage file: $OutputDir\$coverageFile" -ForegroundColor Cyan
        }
    }
} else {
    Write-Host "Some tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Test run completed." -ForegroundColor Green
