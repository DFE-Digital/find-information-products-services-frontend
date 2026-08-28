<#
.SYNOPSIS
  Holds the line on the tests that pass: fails if any test named in known-green.txt ran and did not pass.

.DESCRIPTION
  The suite does not pass in full, so "all green" cannot be the gate. This is the ratchet instead:
  known-green.txt lists the tests that are known to pass, and a run that turns one of them red is
  refused. Tests that stay red are allowed; tests that are removed (absent from the run) are allowed;
  a test brought to green is added to the list by whoever brings it there - or by -Record, which
  rewrites the list from the run's results.

  known-flaky.txt lists tests seen both green and red against the same build. They are reported,
  never failed on, and never recorded as green: each is a signpost to a test to fix.

.PARAMETER Trx
  The .trx file written by: dotnet test tests/FipsFrontend.EndToEnd --logger trx

.PARAMETER Record
  Rewrite known-green.txt with every test that passed in the run, instead of checking against it.

.EXAMPLE
  ./tests/FipsFrontend.EndToEnd/Check-KnownGreen.ps1 -Trx TestResults/run.trx
#>
param(
    [Parameter(Mandatory)] [string] $Trx,
    [switch] $Record
)

$ErrorActionPreference = 'Stop'
$listPath = Join-Path $PSScriptRoot 'known-green.txt'
$flakyPath = Join-Path $PSScriptRoot 'known-flaky.txt'

function Read-List([string] $path) {
    if (Test-Path -LiteralPath $path) { @(Get-Content -LiteralPath $path | Where-Object { $_ -and -not $_.StartsWith('#') }) } else { @() }
}
$flaky = Read-List $flakyPath

[xml] $run = Get-Content -LiteralPath $Trx -Raw
$results = $run.TestRun.Results.UnitTestResult | ForEach-Object {
    [pscustomobject]@{ Name = $_.testName; Outcome = $_.outcome }
}
$passed = $results | Where-Object { $_.Outcome -eq 'Passed' -and $flaky -notcontains $_.Name } | ForEach-Object Name | Sort-Object -Unique

if ($Record) {
    # A header line and one test per line; the file is read back by name, so it must stay plain.
    @("# Tests known to pass; a run that fails one of these is refused by Check-KnownGreen.ps1.") + $passed |
        Set-Content -LiteralPath $listPath -Encoding utf8
    Write-Host "Recorded $($passed.Count) passing tests to $listPath"
    exit 0
}

if (-not (Test-Path -LiteralPath $listPath)) { throw "No $listPath - record one first with -Record." }
$known = Read-List $listPath

$ran = @{}; foreach ($r in $results) { $ran[$r.Name] = $r.Outcome }
$regressed = $known | Where-Object { $ran.ContainsKey($_) -and $ran[$_] -ne 'Passed' -and $flaky -notcontains $_ }
$absent    = $known | Where-Object { -not $ran.ContainsKey($_) }
$newGreen  = $passed | Where-Object { $known -notcontains $_ }

Write-Host ("Run: {0} results, {1} passed. Known green: {2}; absent from this run: {3}; newly green (not yet listed): {4}; known flaky: {5}." -f
    $results.Count, ($results | Where-Object Outcome -eq 'Passed').Count, $known.Count, $absent.Count, $newGreen.Count, $flaky.Count)
if ($newGreen) { $newGreen | ForEach-Object { Write-Host "  newly green: $_" } }
foreach ($f in $flaky) { if ($ran.ContainsKey($f)) { Write-Host "  flaky (not checked): $f ($($ran[$f]))" } }

if ($regressed) {
    $regressed | ForEach-Object { Write-Host "  REGRESSED: $_ ($($ran[$_]))" }
    Write-Error "$($regressed.Count) test(s) known to pass did not pass. Either fix the regression or, if the behaviour was removed on purpose, remove the test and its line from known-green.txt in the same change."
    exit 1
}
Write-Host "No known-green test regressed."
