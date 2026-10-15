param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
function Assert($Ok, $Message) { if (-not $Ok) { throw $Message } }
function Send($Id, $Body) { Invoke-RestMethod "$BaseUrl/api/devices/$Id/commands" -Method Post -ContentType 'application/json' -Body ($Body | ConvertTo-Json) }
try {
    $catalog = Invoke-RestMethod "$BaseUrl/api/devices"
    Assert (($catalog | Where-Object id -eq 'hydraulic-1').type -eq 'HYDRAULIC_ACTUATOR') 'Tipo hidráulico incorrecto'
    Assert (($catalog | Where-Object id -eq 'servo-1').type -eq 'SERVO') 'Tipo servo incorrecto'
    Send 'hydraulic-1' @{ action='RETRACT' } | Out-Null
    Start-Sleep -Seconds 6
    $start = Send 'hydraulic-1' @{ action='EXTEND' }
    Assert ($start.state.position -eq 0 -and $start.state.movement -eq 'EXTENDING') 'Inicio hidráulico incorrecto'
    Start-Sleep -Milliseconds 350
    $mid = Invoke-RestMethod "$BaseUrl/api/devices/hydraulic-1"
    Assert ($mid.position -gt 0 -and $mid.position -lt 100) 'No hay movimiento progresivo'
    $stopped = Send 'hydraulic-1' @{ action='STOP' }
    Start-Sleep -Milliseconds 350
    Assert ((Invoke-RestMethod "$BaseUrl/api/devices/hydraulic-1").position -eq $stopped.state.position) 'STOP no congela'
    foreach ($position in @(0,90,180)) { Assert ((Send 'servo-1' @{ action='SET_POSITION'; position=$position }).state.position -eq $position) 'Ángulo incorrecto' }
    foreach ($body in @(@{action='SET_POSITION';position=181}, @{action='SET_POSITION';position=-1}, @{action='SET_POSITION'})) {
        $r=Invoke-WebRequest "$BaseUrl/api/devices/servo-1/commands" -Method Post -ContentType 'application/json' -Body ($body|ConvertTo-Json) -SkipHttpErrorCheck
        Assert ($r.StatusCode -eq 400 -and ($r.Content|ConvertFrom-Json).error -eq 'INVALID_PARAMETER') 'Posición inválida aceptada'
    }
    Send 'hydraulic-1' @{ action='EXTEND' } | Out-Null
    Send 'main-motor' @{ action='start' } | Out-Null
    Send 'main-light-bank' @{ action='BLINK' } | Out-Null
    $snapshot = Invoke-RestMethod "$BaseUrl/api/devices/stop-all?includeState=true" -Method Post
    Assert ($snapshot.Count -eq 6) 'Snapshot incompleto'
    $hydraulic = $snapshot | Where-Object id -eq 'hydraulic-1'
    Assert ($hydraulic.movement -eq 'STOPPED' -and ($snapshot | Where-Object id -eq 'servo-1').position -eq 180) 'Parada incoherente'
    Start-Sleep -Milliseconds 350
    Assert ((Invoke-RestMethod "$BaseUrl/api/devices/hydraulic-1").position -eq $hydraulic.position) 'Movimiento tras STOP ALL'
    $doc = Invoke-RestMethod "$BaseUrl/swagger/v1/swagger.json"
    foreach ($status in @('400','500','503','504')) { Assert ($doc.paths.'/api/devices/{id}/commands'.post.responses.$status) "Falta error $status" }
    Assert ($doc.components.schemas.DeviceCommandRequest.properties.position) 'Falta position en OpenAPI'
    Assert ($doc.paths.'/api/devices/stop-all'.post.responses.'200') 'Falta snapshot en OpenAPI'
    Write-Output 'OK: hidráulico progresivo/STOP, servo 0/90/180, parámetros inválidos, snapshot STOP ALL y Swagger.'
}
finally { Invoke-RestMethod "$BaseUrl/api/devices/stop-all" -Method Post | Out-Null }
