param([int]$Port = 5081)
$ErrorActionPreference = 'Stop'
function Assert($Ok, $Message) { if (-not $Ok) { throw $Message } }
$apiDirectory = Join-Path $PSScriptRoot 'Carroza.Api/bin/Debug/net9.0'
$executable = Join-Path $apiDirectory 'Carroza.Api.exe'
$baseUrl = "http://127.0.0.1:$Port"
$cases = @(
    @{Id='front-lights'; Code='DEVICE_OFFLINE'; Status=503; Body=@{action='on'}},
    @{Id='side-lights'; Code='TIMEOUT'; Status=504; Body=@{action='on'}},
    @{Id='main-motor'; Code='INVALID_COMMAND'; Status=400; Body=@{action='start'}},
    @{Id='main-light-bank'; Code='INVALID_PARAMETER'; Status=400; Body=@{action='ALL_ON'}},
    @{Id='servo-1'; Code='INTERNAL_ERROR'; Status=500; Body=@{action='SET_POSITION';position=180}}
)
$process = $null
try {
    # Arguments affect only this isolated process; no environment or appsettings mutation.
    $arguments = @('--urls', $baseUrl)
    foreach ($case in $cases) { $arguments += "--Simulator:Faults:$($case.Id)=$($case.Code)" }
    $process = Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory $apiDirectory -WindowStyle Hidden -PassThru
    $ready = $false
    for ($attempt=0; $attempt -lt 40; $attempt++) {
        if ($process.HasExited) { throw 'El backend de fallas no pudo iniciar (puerto ocupado u otro error).' }
        try { $devices = Invoke-RestMethod "$baseUrl/api/devices" -TimeoutSec 1; $ready=$true; break } catch { Start-Sleep -Milliseconds 200 }
    }
    Assert $ready 'Backend de fallas no disponible.'
    foreach ($case in $cases) {
        $before = Invoke-RestMethod "$baseUrl/api/devices/$($case.Id)"
        $response = Invoke-WebRequest "$baseUrl/api/devices/$($case.Id)/commands" -Method Post -ContentType 'application/json' -Body ($case.Body|ConvertTo-Json) -SkipHttpErrorCheck
        Assert ($response.StatusCode -eq $case.Status) "HTTP incorrecto para $($case.Code)"
        $errorBody = $response.Content | ConvertFrom-Json
        Assert ($errorBody.error -eq $case.Code -and $errorBody.message.Contains($case.Id)) 'Falla no identifica código/componente.'
        $after = Invoke-RestMethod "$baseUrl/api/devices/$($case.Id)"
        Assert (($before|ConvertTo-Json -Compress) -eq ($after|ConvertTo-Json -Compress)) 'Falla modificó estado.'
        Assert ($after.online -eq ($case.Code -ne 'DEVICE_OFFLINE')) 'Online incorrecto.'
    }
    $healthy = Invoke-RestMethod "$baseUrl/api/devices/hydraulic-1/commands" -Method Post -ContentType 'application/json' -Body '{"action":"EXTEND"}'
    Assert $healthy.success 'Las fallas bloquearon otro componente.'
    Start-Sleep -Milliseconds 200
    $snapshot = Invoke-RestMethod "$baseUrl/api/devices/stop-all?includeState=true" -Method Post
    Assert (($snapshot | Where-Object id -eq 'hydraulic-1').movement -eq 'STOPPED') 'STOP ALL no detuvo el actuador.'
    Assert (($snapshot | Where-Object id -eq 'front-lights').online -eq $false) 'STOP ALL borró condición offline.'
    Write-Output 'OK: cinco fallas HTTP reproducibles, estados sin mutación, offline, operación independiente y STOP ALL.'
}
finally { if ($process -and -not $process.HasExited) { Stop-Process -Id $process.Id; $process.WaitForExit() } }
