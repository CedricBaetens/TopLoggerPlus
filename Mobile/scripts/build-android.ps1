param([switch]$DebugBuild, [string]$SigningFile)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    if (!$DebugBuild -and !$SigningFile -and !$env:TLP_KEYSTORE) {
        $SigningFile = Join-Path $PSScriptRoot '../signing/signing.json'
    }
    if ($SigningFile) {
        $SigningFile = (Resolve-Path -LiteralPath $SigningFile).Path
        $signing = Get-Content -LiteralPath $SigningFile -Raw | ConvertFrom-Json
        $env:TLP_KEYSTORE = if ([IO.Path]::IsPathRooted($signing.keystore)) { $signing.keystore } else { Join-Path (Split-Path $SigningFile -Parent) $signing.keystore }
        $env:TLP_STORE_PASSWORD = $signing.password
        $env:TLP_KEY_ALIAS = $signing.alias
    }
    if (!$DebugBuild -and !$env:TLP_KEYSTORE) { throw 'Supply signing/signing.json, TLP_KEYSTORE and TLP_STORE_PASSWORD, or -SigningFile.' }
    if (!$env:JAVA_HOME -or !(Test-Path "$env:JAVA_HOME/bin/javac.exe")) { throw 'JAVA_HOME must point to a JDK 21 installation.' }
    if (!$env:ANDROID_HOME) { throw 'Set ANDROID_HOME to the Android SDK directory (platform 36 and build tools 36.0.0).' }
    # Short socket paths avoid Windows AF_UNIX failures with long temporary paths.
    $socketDirectory = Join-Path $env:ANDROID_HOME 'socket-temp'
    New-Item -ItemType Directory -Force $socketDirectory | Out-Null
    $env:JAVA_TOOL_OPTIONS = "$env:JAVA_TOOL_OPTIONS -Djdk.net.unixdomain.tmpdir=$socketDirectory".Trim()
    npm ci
    if ($LASTEXITCODE) { throw 'npm ci failed' }
    npm test
    if ($LASTEXITCODE) { throw 'Tests failed' }
    npm run typecheck
    if ($LASTEXITCODE) { throw 'Type check failed' }
    npm run android:sync
    if ($LASTEXITCODE) { throw 'Web build / Android sync failed' }
    Push-Location android
    try {
        $task = if ($DebugBuild) { ':app:assembleDebug' } else { ':app:assembleRelease' }
        & ./gradlew.bat $task --console=plain
        if ($LASTEXITCODE) { throw 'Android build failed' }
    } finally { Pop-Location }
} finally { Pop-Location }
