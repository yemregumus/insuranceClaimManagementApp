param(
    [ValidateRange(0, 100)]
    [double]$MinimumLineCoverage = 80
)

$ErrorActionPreference = "Stop"
$testProject = Join-Path $PSScriptRoot "InsuranceClaimManagement.Tests.csproj"
$settings = Join-Path $PSScriptRoot "CodeCoverage.runsettings"
$resultsDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("InsuranceClaimCoverage-" + [guid]::NewGuid())
$buildDirectory = Join-Path $resultsDirectory "build"

dotnet test $testProject --settings $settings --collect:"Code Coverage" `
    --results-directory $resultsDirectory --output $buildDirectory
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$report = Get-ChildItem -LiteralPath $resultsDirectory -Recurse -Filter "*.cobertura.xml" |
    Select-Object -First 1
if ($null -eq $report) {
    throw "The coverage collector did not produce a Cobertura report."
}

[xml]$coverageReport = Get-Content -LiteralPath $report.FullName
$lineCoverage = [double]$coverageReport.coverage.'line-rate' * 100
Write-Host ("Application line coverage: {0:N2}%" -f $lineCoverage)
Write-Host ("Coverage report: {0}" -f $report.FullName)

if ($lineCoverage -lt $MinimumLineCoverage) {
    throw ("Line coverage {0:N2}% is below the required {1:N2}%." -f $lineCoverage, $MinimumLineCoverage)
}
