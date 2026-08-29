param(
    [string]$ApiExe = ""
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ApiExe)) {
    $candidates = Get-ChildItem -Path $PSScriptRoot -Filter "*.exe" -File |
        Where-Object { $_.Name -notmatch 'setup|unins' -and $_.Name -match 'API' }

    if ($candidates.Count -eq 1) {
        $ApiExe = $candidates[0].Name
    }
    elseif (Test-Path (Join-Path $PSScriptRoot 'HardwareStorePortal.API.exe')) {
        $ApiExe = 'HardwareStorePortal.API.exe'
    }
    else {
        throw 'Could not find the Hardware Store Portal API executable.'
    }
}

$app = Join-Path $PSScriptRoot $ApiExe
$url = 'http://localhost:5000'

if (-not (Test-Path $app)) {
    throw "API executable not found: $app"
}

# If the portal is already running, just open the browser.
try {
    $existing = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2
    if ($existing.StatusCode -ge 200 -and $existing.StatusCode -lt 500) {
        Start-Process $url
        exit 0
    }
} catch {}

$proc = Start-Process -FilePath $app -WorkingDirectory $PSScriptRoot -PassThru

try {
    $ready = $false

    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Milliseconds 500

        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                $ready = $true
                break
            }
        } catch {}

        if ($proc.HasExited) {
            throw 'The Hardware Store Portal server stopped before it became ready.'
        }
    }

    if (-not $ready) {
        throw 'The Hardware Store Portal did not become ready at http://localhost:5000 within 30 seconds.'
    }

    Start-Process $url
}
catch {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show(
        $_.Exception.Message,
        'Hardware Store Portal'
    ) | Out-Null
}
