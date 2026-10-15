param([string]$BaseUrl = 'http://127.0.0.1:5080')
$ErrorActionPreference = 'Stop'
# Start host.py and the API in ESP32 mode as documented before running this script.
$backend = Join-Path $PSScriptRoot '../../backend'
foreach ($script in @('smoke-test.ps1', 'contract-test.ps1', 'iteration-3-test.ps1', 'iteration-4-test.ps1', 'sequence-test.ps1')) {
    Write-Output "Firmware HTTP: $script"
    & (Join-Path $backend $script) -BaseUrl $BaseUrl
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "$script falló ($LASTEXITCODE)" }
}
Write-Output 'OK: backend ESP32 -> HTTP -> firmware/HAL simulada. No prueba física.'
