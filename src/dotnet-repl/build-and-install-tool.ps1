Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

$thisDir = Split-Path -Parent $PSCommandPath
$toolLocation = ""
$toolVersion = ""

dotnet pack -c debug (Join-Path $thisDir "dotnet-repl.csproj") /p:Version=1.0.0
$script:toolLocation = Join-Path $thisDir "bin" "debug"
$script:toolVersion = "1.0.0"

if (Get-Command dotnet-repl -ErrorAction SilentlyContinue) {
    dotnet tool uninstall -g dotnet-repl
}
dotnet tool install -g --add-source "$toolLocation" --version $toolVersion dotnet-repl