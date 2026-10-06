param([string]$FrontendUrl = 'http://127.0.0.1:5173', [string]$BackendUrl = 'http://127.0.0.1:5080')
$ErrorActionPreference = 'Stop'
foreach ($base in @($BackendUrl, $FrontendUrl)) {
    foreach ($route in @('/api/devices', '/api/sequences', '/api/sequences/SHOW_FNE', '/api/sequences/execution', '/api/sequences/events')) {
        $response = Invoke-WebRequest "$base$route"
        if ($response.StatusCode -ne 200 -or $response.Headers['Content-Type'] -notmatch 'application/json') {
            throw "$base$route no devolvió HTTP 200 application/json"
        }
        $null = $response.Content | ConvertFrom-Json
        Write-Output "OK: $base$route JSON"
    }
}
