param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
function Assert($Ok, $Message) { if (-not $Ok) { throw $Message } }
function Current { Invoke-RestMethod "$BaseUrl/api/sequences/execution" }
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
        Start-Sleep -Milliseconds 200
        Invoke-RestMethod "$BaseUrl/$path" -Method Post | Out-Null
        Assert ((Current).status -eq 'CANCELLED') 'No canceló'
        $count = @((Invoke-RestMethod "$BaseUrl/api/sequences/events") | Where-Object runId -eq $run.runId).Count
        Start-Sleep -Milliseconds 1200
        Assert (@((Invoke-RestMethod "$BaseUrl/api/sequences/events") | Where-Object runId -eq $run.runId).Count -eq $count) 'Paso posterior a cancelación'
        Assert (@((Invoke-RestMethod "$BaseUrl/api/devices") | Where-Object { $_.state -notin @('off','stopped') }).Count -eq 0) 'Estado inseguro'
    }
    $doc = Invoke-RestMethod "$BaseUrl/swagger/v1/swagger.json"
    foreach ($path in @('/api/sequences','/api/sequences/{id}','/api/sequences/{id}/start','/api/sequences/execution','/api/sequences/cancel','/api/sequences/events')) { Assert ($doc.paths.$path) "Falta $path en Swagger" }
    Assert ($doc.paths.'/api/devices/{id}/commands'.post.responses.'409') 'Falta 409 en Swagger'
    Write-Output 'OK: SHOW_FNE completo, orden/timing real, 409 doble inicio/manual, cancelación, STOP ALL, historial y Swagger.'
}
finally { Invoke-RestMethod "$BaseUrl/api/devices/stop-all" -Method Post | Out-Null }
