param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message } }
function Command($Action, $Speed = $null) {
    $body = @{ action = $Action }
    if ($null -ne $Speed) { $body.speed = $Speed }
    Invoke-RestMethod "$BaseUrl/api/devices/main-light-bank/commands" -Method Post -ContentType 'application/json' -Body ($body | ConvertTo-Json)
}
try {
    $ui = Invoke-WebRequest "$BaseUrl/swagger/index.html"
    Assert ($ui.StatusCode -eq 200 -and $ui.Content -match 'swagger-ui') 'Swagger UI no disponible.'
    Assert ((Invoke-WebRequest "$BaseUrl/swagger/swagger-ui-bundle.js").StatusCode -eq 200) 'Bundle UI no disponible.'
    $doc = Invoke-RestMethod "$BaseUrl/swagger/v1/swagger.json"
    foreach ($path in @('/api/devices','/api/devices/{id}','/api/devices/{id}/commands','/api/devices/stop-all')) {
        Assert ($null -ne $doc.paths.$path) "Falta endpoint en OpenAPI: $path"
    }
    foreach ($schema in @('DeviceDto','DeviceStateDto','DeviceResultDto','DeviceCommandRequest','CommandErrorDto')) {
        Assert ($null -ne $doc.components.schemas.$schema) "Falta DTO: $schema"
    }
    Assert ($doc.paths.'/api/devices/{id}/commands'.post.responses.'400') 'Falta respuesta 400.'
    Assert ($doc.components.schemas.DeviceCommandRequest.properties.action.description -match 'SWEEP_RIGHT') 'Falta documentación de comandos.'
    Assert ($doc.components.schemas.DeviceStateDto.properties.channels) 'Faltan canales en OpenAPI.'
    Assert ((Command 'ALL_ON').state.channels -notcontains $false) 'ALL_ON falló.'
    Assert ((Command 'ALL_OFF').state.channels -notcontains $true) 'ALL_OFF falló.'
    Command 'SET_SPEED' 100 | Out-Null
    foreach ($effect in @('SWEEP_RIGHT','SWEEP_LEFT','PING_PONG','BLINK')) {
        $initial = (Command $effect).state
        Assert ($initial.channels.Count -eq 8 -and $initial.effect -eq $effect) 'Confirmación del efecto incorrecta.'
        Start-Sleep -Milliseconds 380
        $next = Invoke-RestMethod "$BaseUrl/api/devices/main-light-bank"
        Assert (($initial.channels -join ',') -ne ($next.channels -join ',')) "El efecto $effect no avanzó."
        $held = (Command 'STOP_EFFECT').state
        Start-Sleep -Milliseconds 380
        $next = Invoke-RestMethod "$BaseUrl/api/devices/main-light-bank"
        Assert ($next.effect -eq 'NONE' -and ($next.channels -join ',') -eq ($held.channels -join ',')) 'STOP_EFFECT no congeló el patrón.'
    }
    Command 'ALL_OFF' | Out-Null
    foreach ($speed in @(0, 101)) {
        $response = Invoke-WebRequest "$BaseUrl/api/devices/main-light-bank/commands" -Method Post -ContentType 'application/json' -Body (@{ action = 'SET_SPEED'; speed = $speed } | ConvertTo-Json) -SkipHttpErrorCheck
        Assert ($response.StatusCode -eq 400) 'Velocidad inválida aceptada.'
        Assert ((Invoke-RestMethod "$BaseUrl/api/devices/main-light-bank").channels -notcontains $true) 'Error modificó canales.'
    }
    Command 'BLINK' | Out-Null
    Invoke-RestMethod "$BaseUrl/api/devices/stop-all" -Method Post | Out-Null
    Start-Sleep -Milliseconds 380
    $bank = Invoke-RestMethod "$BaseUrl/api/devices/main-light-bank"
    Assert ($bank.effect -eq 'NONE' -and $bank.channels -notcontains $true) 'STOP ALL no canceló el banco.'
    Write-Output 'OK: Swagger UI/assets/OpenAPI, cuatro endpoints, cinco DTOs, todos los comandos del banco, avance temporal, errores y STOP ALL.'
}
finally { Invoke-RestMethod "$BaseUrl/api/devices/stop-all" -Method Post | Out-Null }
