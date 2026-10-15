param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
function Assert($Ok, $Message) { if (-not $Ok) { throw $Message } }
function Current { Invoke-RestMethod "$BaseUrl/api/sequences/execution" }
function SafeStates {
    $states = Invoke-RestMethod "$BaseUrl/api/devices"
    Assert (@($states | Where-Object { $_.state -notin @('off','stopped') }).Count -eq 0) 'Estado inseguro'
    Assert (($states | Where-Object id -eq 'main-motor').speed -eq 0) 'Motor no detenido'
    $bank = $states | Where-Object id -eq 'main-light-bank'
    Assert ($bank.effect -eq 'NONE' -and $bank.channels -notcontains $true) 'Banco activo'
    Assert (($states | Where-Object id -eq 'hydraulic-1').movement -eq 'STOPPED') 'Hidráulico activo'
    return $states
}
try {
    Invoke-RestMethod "$BaseUrl/api/sequences/cancel" -Method Post | Out-Null
    $definition = Invoke-RestMethod "$BaseUrl/api/sequences/SHOW_FNE"
    Assert ($definition.steps.Count -eq 14 -and ($definition.steps.delayMs | Measure-Object -Sum).Sum -eq 19000) 'Definición incorrecta'
    Assert ((Invoke-WebRequest "$BaseUrl/api/sequences/missing" -SkipHttpErrorCheck).StatusCode -eq 404) 'Falta 404'
    $started = Invoke-RestMethod "$BaseUrl/api/sequences/SHOW_FNE/start" -Method Post
    Assert ($started.status -eq 'RUNNING') 'Inicio no confirmado'
    Assert ((Invoke-WebRequest "$BaseUrl/api/sequences/SHOW_FNE/start" -Method Post -SkipHttpErrorCheck).StatusCode -eq 409) 'Doble inicio aceptado'
    $manual = Invoke-WebRequest "$BaseUrl/api/devices/main-motor/commands" -Method Post -ContentType 'application/json' -Body '{"action":"start"}' -SkipHttpErrorCheck
    Assert ($manual.StatusCode -eq 409) 'Comando manual aceptado durante RUNNING'
    $deadline = [DateTime]::UtcNow.AddSeconds(25)
    do { Start-Sleep -Milliseconds 200; $state = Current } while ($state.status -eq 'RUNNING' -and [DateTime]::UtcNow -lt $deadline)
    Assert ($state.status -eq 'COMPLETED' -and $state.currentStep -eq 14) 'No completó SHOW_FNE'
    $completed = SafeStates
    Assert (($completed | Where-Object id -eq 'hydraulic-1').position -eq 0) 'No completó retracción'
    Assert (($completed | Where-Object id -eq 'servo-1').position -eq 120) 'Servo no confirmó 120 grados'
    $events = @((Invoke-RestMethod "$BaseUrl/api/sequences/events") | Where-Object runId -eq $started.runId)
    Assert ($events.Count -eq 14 -and @($events | Where-Object result -ne 'OK').Count -eq 0) 'Historial incompleto'
    for ($i=0; $i -lt 14; $i++) {
        Assert ($events[$i].componentId -eq $definition.steps[$i].componentId -and $events[$i].command.action -eq $definition.steps[$i].command.action) 'Orden incorrecto'
        if ($i -gt 0) {
            $delta = ([DateTimeOffset]$events[$i].time - [DateTimeOffset]$events[$i-1].time).TotalMilliseconds
            Assert ($delta -ge $definition.steps[$i].delayMs - 100) 'Delay acortado'
        }
    }
    foreach ($path in @('api/sequences/cancel','api/devices/stop-all?includeState=true')) {
        $run = Invoke-RestMethod "$BaseUrl/api/sequences/SHOW_FNE/start" -Method Post
        $deadline = [DateTime]::UtcNow.AddSeconds(12)
        do { Start-Sleep -Milliseconds 100; $active = Current } while ($active.currentStep -lt 6 -and [DateTime]::UtcNow -lt $deadline)
        Assert ($active.status -eq 'RUNNING' -and $active.currentStep -ge 6) 'No avanzó hasta hidráulico y motor activos'
        Invoke-RestMethod "$BaseUrl/$path" -Method Post | Out-Null
        Assert ((Current).status -eq 'CANCELLED') 'No canceló'
        $safe = SafeStates | ConvertTo-Json -Depth 5 -Compress
        $count = @((Invoke-RestMethod "$BaseUrl/api/sequences/events") | Where-Object runId -eq $run.runId).Count
        # Observe beyond the original 19-second show, not only until the next step.
        Start-Sleep -Seconds 13
        Assert (@((Invoke-RestMethod "$BaseUrl/api/sequences/events") | Where-Object runId -eq $run.runId).Count -eq $count) 'Paso posterior a cancelación'
        Assert ((Current).runId -eq $run.runId -and (Current).status -eq 'CANCELLED') 'Otra ejecución interfirió o cambió el estado final'
        Assert ((SafeStates | ConvertTo-Json -Depth 5 -Compress) -eq $safe) 'Componentes cambiaron después de la parada'
    }
    $doc = Invoke-RestMethod "$BaseUrl/swagger/v1/swagger.json"
    foreach ($path in @('/api/sequences','/api/sequences/{id}','/api/sequences/{id}/start','/api/sequences/execution','/api/sequences/cancel','/api/sequences/events')) { Assert ($doc.paths.$path) "Falta $path en Swagger" }
    Assert ($doc.paths.'/api/devices/{id}/commands'.post.responses.'409') 'Falta 409 en Swagger'
    Write-Output 'OK: SHOW_FNE completo, orden/timing real, 409 doble inicio/manual, cancelación, STOP ALL, historial y Swagger.'
}
finally { Invoke-RestMethod "$BaseUrl/api/devices/stop-all" -Method Post | Out-Null }
