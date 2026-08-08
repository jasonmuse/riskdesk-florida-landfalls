$desktopDirectory = Split-Path -Parent $PSScriptRoot
$repositoryDirectory = Split-Path -Parent $desktopDirectory
$apiProject = Join-Path $repositoryDirectory 'RiskDesk.Api\RiskDesk.Api.csproj'
$apiOutput = Join-Path $desktopDirectory 'build-resources\api'
$dataOutput = Join-Path $desktopDirectory 'build-resources\Data'
$forgeCommand = Join-Path $desktopDirectory 'node_modules\.bin\electron-forge.cmd'
$builderCommand = Join-Path $desktopDirectory 'node_modules\.bin\electron-builder.cmd'

Write-Output 'Publishing the self-contained ASP.NET Core API...'
& dotnet publish $apiProject `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $apiOutput `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw 'The API publish failed.'
}

New-Item -ItemType Directory -Path $dataOutput -Force | Out-Null
Copy-Item `
    -LiteralPath (Join-Path $repositoryDirectory 'Data\hurdat2-1851-2025-02272026.txt') `
    -Destination $dataOutput `
    -Force
Copy-Item `
    -LiteralPath (Join-Path $repositoryDirectory 'Data\florida.geojson') `
    -Destination $dataOutput `
    -Force

Push-Location $desktopDirectory

try {
    Write-Output 'Packaging the Electron application and bundled resources...'
    & $forgeCommand package --platform win32 --arch x64

    if ($LASTEXITCODE -ne 0) {
        throw 'The Electron package failed.'
    }

    $packagedApplication = Get-ChildItem `
        -Path (Join-Path $desktopDirectory 'out') `
        -Directory |
        Where-Object { $_.Name -like '*-win32-x64' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $packagedApplication) {
        throw 'The packaged Electron application was not found.'
    }

    Write-Output 'Creating the single portable Windows executable...'
    & $builderCommand `
        --win portable `
        --x64 `
        --prepackaged $packagedApplication.FullName

    if ($LASTEXITCODE -ne 0) {
        throw 'The portable executable build failed.'
    }
}
finally {
    Pop-Location
}

Write-Output 'Portable executable created in RiskDesk.Desktop\dist.'
