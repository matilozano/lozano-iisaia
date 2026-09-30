param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message } }
function Post($Id, $Body) {
    Invoke-WebRequest "$BaseUrl/api/devices/$Id/commands" -Method Post -ContentType 'application/json' -Body ($Body | ConvertTo-Json) -SkipHttpErrorCheck
}
function Keys($Value, [string[]]$Names) {
    Assert ((($Value.PSObject.Properties.Name | Sort-Object) -join ',') -eq (($Names | Sort-Object) -join ',')) 'Contrato JSON inesperado.'
}
try {
    $devices = Invoke-RestMethod "$BaseUrl/api/devices"
    Assert (($devices.id -join ',') -eq 'front-lights,side-lights,main-motor,main-light-bank,hydraulic-1,servo-1') 'Catálogo incorrecto.'
    foreach ($device in ($devices | Where-Object { $_.type -in @('light','motor') })) { Keys $device @('id', 'name', 'type', 'state', 'online', 'direction', 'speed') }
    foreach ($speed in @(0, 100)) {
        $response = Post 'main-motor' @{ action = 'start'; speed = $speed; direction = 'reverse' }
        Assert ($response.StatusCode -eq 200) 'Límite de velocidad rechazado.'
        $result = $response.Content | ConvertFrom-Json
        Keys $result @('deviceId', 'success', 'state', 'executedAt')
        Keys $result.state @('id', 'state', 'online', 'direction', 'speed')
        Assert ($result.state.speed -eq $speed -and $result.state.direction -eq 'reverse') 'Velocidad o dirección incorrecta.'
    }
    $defaultMotor = (Post 'main-motor' @{ action = 'start' }).Content | ConvertFrom-Json
    Assert ($defaultMotor.state.speed -eq 50 -and $defaultMotor.state.direction -eq 'forward') 'Valores predeterminados incorrectos.'
    $invalid = @(
        @{ Id = 'front-lights'; Body = @{ action = 'start' } },
        @{ Id = 'side-lights'; Body = @{ action = 'on'; speed = 50 } },
        @{ Id = 'main-motor'; Body = @{ action = 'start'; speed = -1 } },
        @{ Id = 'main-motor'; Body = @{ action = 'start'; speed = 101 } },
        @{ Id = 'main-motor'; Body = @{ action = 'start'; direction = 'invalid' } },
        @{ Id = 'main-motor'; Body = @{ action = 'stop'; speed = 20 } },
        @{ Id = 'main-motor'; Body = @{ action = 'on' } },
        @{ Id = 'front-lights'; Body = @{} }
    )
    foreach ($case in $invalid) {
        $before = Invoke-RestMethod "$BaseUrl/api/devices/$($case.Id)" | ConvertTo-Json -Compress
        $response = Post $case.Id $case.Body
        Assert ($response.StatusCode -eq 400) 'Se esperaba HTTP 400.'
        $errorBody = $response.Content | ConvertFrom-Json
        Keys $errorBody @('status', 'error', 'message')
        Assert ($errorBody.error -eq 'INVALID_COMMAND') 'Error de comando incorrecto.'
        $after = Invoke-RestMethod "$BaseUrl/api/devices/$($case.Id)" | ConvertTo-Json -Compress
        Assert ($before -eq $after) 'El comando inválido alteró el estado.'
    }
    Assert ((Post 'missing' @{ action = 'on' }).StatusCode -eq 404) 'Componente inexistente debe devolver 404.'
    Assert ((Invoke-WebRequest "$BaseUrl/api/devices/missing" -SkipHttpErrorCheck).StatusCode -eq 404) 'Consulta inexistente debe devolver 404.'
    Assert ((Invoke-RestMethod "$BaseUrl/api/sequences").id -contains 'SHOW_FNE') 'Falta SHOW_FNE.'
    $stop = Invoke-WebRequest "$BaseUrl/api/devices/stop-all" -Method Post
    Assert ($stop.StatusCode -eq 204 -and $stop.Content.Length -eq 0) 'STOP ALL debe devolver 204 sin cuerpo.'
    Write-Output 'OK: contrato JSON, límites 0/100, defaults, errores 400/404 sin mutación, STOP ALL y catálogo de secuencias.'
}
finally { Invoke-RestMethod "$BaseUrl/api/devices/stop-all" -Method Post | Out-Null }
