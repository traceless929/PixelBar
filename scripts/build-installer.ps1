# 本地构建安装包（需已安装 Inno Setup 6）
# 用法: ./scripts/build-installer.ps1 [-Version 0.0.3]

param(
    [string]$Version = (Select-String -Path (Join-Path $PSScriptRoot "..\src\Directory.Build.props") -Pattern '<Version>([^<]+)</Version>' | ForEach-Object { $_.Matches[0].Groups[1].Value })
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$PublishDir = Join-Path $Root "publish\app-install"
$Iss = Join-Path $Root "installer\PixelBar.iss"

$Iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $Iscc) {
    throw "未找到 Inno Setup 6。请从 https://jrsoftware.org/isdl.php 安装。"
}

Write-Host "Publishing PixelBar.App (folder) v$Version ..."
dotnet publish (Join-Path $Root "src\PixelBar.App\PixelBar.App.csproj") `
    -c Release -r win-x64 --self-contained `
    -p:Version=$Version `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:EnableMsixTooling=true `
    -p:DebugType=None -p:DebugSymbols=false `
    -o $PublishDir

Write-Host "Compiling installer ..."
& $Iscc $Iss "/DMyAppVersion=$Version" "/DPublishDir=$PublishDir"
Write-Host "Done: artifacts\PixelBar.App-v$Version-setup.exe"
