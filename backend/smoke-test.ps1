param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message } }
function Command($Id, $Body) {
    Invoke-RestMethod "$BaseUrl/api/devices/$Id/commands" -Method Post -ContentType 'application/json' -Body ($Body | ConvertTo-Json)
}
try {
    $devices = Invoke-RestMethod "$BaseUrl/api/devices"
    Assert ($devices.Count -eq 3) 'Se esperaban tres componentes.'
    foreach ($id in @('front-lights', 'side-lights')) {
        foreach ($action in @('on', 'off')) {
            $result = Command $id @{ action = $action }
            Assert ($result.success -and $result.state.state -eq $action) "Comando $action falló para $id."
            $state = Invoke-RestMethod "$BaseUrl/api/devices/$id"
            Assert ($state.state -eq $action) 'El estado consultado no coincide con el confirmado.'
        }
    }
    foreach ($direction in @('forward', 'reverse')) {
        $result = Command 'main-motor' @{ action = 'start'; direction = $direction; speed = 70 }
        Assert ($result.state.state -eq 'running' -and $result.state.speed -eq 70 -and $result.state.direction -eq $direction) 'Estado del motor incorrecto.'
    }
    $stopped = Command 'main-motor' @{ action = 'stop' }
    Assert ($stopped.state.state -eq 'stopped' -and $stopped.state.speed -eq 0) 'El motor no se detuvo.'
    Command 'front-lights' @{ action = 'on' } | Out-Null
    Command 'side-lights' @{ action = 'on' } | Out-Null
    Command 'main-motor' @{ action = 'start'; speed = 70 } | Out-Null
    $stop = Invoke-WebRequest "$BaseUrl/api/devices/stop-all" -Method Post
    Assert ($stop.StatusCode -eq 204) 'STOP ALL no confirmó la parada.'
    $devices = Invoke-RestMethod "$BaseUrl/api/devices"
    Assert (@($devices | Where-Object { $_.state -notin @('off', 'stopped') -or $_.speed -ne 0 }).Count -eq 0) 'Parada general incompleta.'
    Write-Output 'OK: tres componentes, ambas luces ON/OFF, motor adelante/reversa a 70%, stop, consulta y STOP ALL.'
}
finally { Invoke-RestMethod "$BaseUrl/api/devices/stop-all" -Method Post | Out-Null }
