param([string]$SamplesRoot = "..\Rivet.Samples")
$src = Join-Path $SamplesRoot "showcases\Rivet.Kickback\assets\ThirdParty\Quaternius"
$dst = Join-Path $PSScriptRoot "..\assets\ThirdParty\Quaternius"
if (!(Test-Path $src)) { throw "Kickback Quaternius asset folder not found: $src" }
New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item "$src\*" $dst -Recurse -Force
Write-Host "Imported existing CC0 Quaternius assets into Homeland."
