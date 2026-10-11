param([string]$BaseUrl = 'http://127.0.0.1:5080')
$ErrorActionPreference = 'Stop'
function Assert($ok, $message) { if (-not $ok) { throw $message } }
function Control($body) { Invoke-RestMethod "$BaseUrl/api/telemetry/simulation" -Method Post -ContentType 'application/json' -Body ($body | ConvertTo-Json) }
function Rejected($path,$body) {
    $r=Invoke-WebRequest "$BaseUrl/api/$path" -Method Post -ContentType 'application/json' -Body ($body|ConvertTo-Json) -SkipHttpErrorCheck
    Assert ($r.StatusCode -in @(400,409)) "Aceptó operación bloqueada: $path"
}
$codes=@('OVER_TEMPERATURE','OVER_CURRENT','VOLTAGE_RANGE','COMMUNICATION_LOSS','INVALID_SENSOR','STALE_SENSOR')
try {
    Invoke-RestMethod "$BaseUrl/api/devices/stop-all" -Method Post | Out-Null
    $normal=Invoke-RestMethod "$BaseUrl/api/telemetry"
    Assert ($normal.source -eq 'SIMULATED' -and $normal.voltage -eq 24 -and $normal.canInjectFaults) 'Telemetría inicial incorrecta'
    Invoke-RestMethod "$BaseUrl/api/devices/main-motor/commands" -Method Post -ContentType 'application/json' -Body '{"action":"start","speed":50}' | Out-Null
    $run=Invoke-RestMethod "$BaseUrl/api/telemetry"
    Assert ($run.voltage -eq 23.5 -and $run.current -eq 5 -and $run.temperature -eq 40 -and $run.motorSpeed -eq 50) 'Evolución de telemetría incorrecta'
    foreach ($code in $codes) {
        Invoke-RestMethod "$BaseUrl/api/sequences/SHOW_FNE/start" -Method Post | Out-Null
        Start-Sleep -Milliseconds 250
        $trip=Control @{action='SET_FAULT';code=$code;active=$true}
        Assert ($trip.protectionLatched -and $trip.stopConfirmed) "Sin protección/parada: $code"
        Assert ((Invoke-RestMethod "$BaseUrl/api/sequences/execution").status -eq 'CANCELLED') 'Secuencia no cancelada'
        if ($code -eq 'COMMUNICATION_LOSS') { Assert (!$trip.online -and $trip.quality -eq 'OFFLINE' -and $null -eq $trip.voltage) 'Offline inventa datos' }
        if ($code -eq 'INVALID_SENSOR') { Assert ($trip.quality -eq 'INVALID' -and $null -eq $trip.temperature) 'Sensor inválido no identificado' }
        if ($code -eq 'STALE_SENSOR') { Assert ($trip.quality -eq 'STALE' -and $trip.ageMs -ge 5000) 'Lectura obsoleta no identificada' }
        Rejected 'devices/main-motor/commands' @{action='start'}
        Rejected 'sequences/SHOW_FNE/start' @{}
        Rejected 'telemetry/simulation' @{action='RESET'}
        $states=Invoke-RestMethod "$BaseUrl/api/devices/stop-all?includeState=true" -Method Post
        Assert (@($states | Where-Object { $_.state -notin @('off','stopped') -or $_.speed -ne 0 }).Count -eq 0) 'Snapshot no seguro'
        Control @{action='ACKNOWLEDGE';code=$code} | Out-Null
        Rejected 'telemetry/simulation' @{action='RESET'}
        $clear=Control @{action='SET_FAULT';code=$code;active=$false}
        Assert $clear.protectionLatched 'Retirar falla liberó protección'
        $reset=Control @{action='RESET'}
        Assert (!$reset.protectionLatched) 'No restableció protección'
        Assert ((Invoke-RestMethod "$BaseUrl/api/sequences/execution").status -eq 'CANCELLED') 'Reanudación automática'
    }
    Start-Sleep -Seconds 6
    $final=Invoke-RestMethod "$BaseUrl/api/devices"
    Assert (@($final | Where-Object { $_.state -notin @('off','stopped') }).Count -eq 0) 'Step posterior reactivó componentes'
    $swagger=Invoke-RestMethod "$BaseUrl/swagger/v1/swagger.json"
    Assert ($swagger.paths.'/api/telemetry'.get -and $swagger.paths.'/api/telemetry/simulation'.post -and $swagger.components.schemas.TelemetryDto) 'OpenAPI telemetría incompleto'
    Write-Output "OK: $($normal.gateway) telemetría, seis fallas, reconocimiento/reset, protección, cancelación, sin reactivación y Swagger."
}
finally {
    foreach ($code in $codes) { Control @{action='SET_FAULT';code=$code;active=$false} | Out-Null }
    Control @{action='ACKNOWLEDGE'} | Out-Null
    Invoke-RestMethod "$BaseUrl/api/devices/stop-all" -Method Post | Out-Null
    Control @{action='RESET'} | Out-Null
}
