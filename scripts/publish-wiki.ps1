# 将 docs/wiki 同步到 GitHub Wiki
# 用法: ./scripts/publish-wiki.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$Source = Join-Path $Root "docs\wiki"
$Temp = Join-Path $env:TEMP "PixelBar.wiki"
$WikiUrl = "https://github.com/traceless929/PixelBar.wiki.git"

if (-not (Test-Path $Source)) {
    throw "Wiki source not found: $Source"
}

if (Test-Path $Temp) {
    Remove-Item -Recurse -Force $Temp
}

git clone $WikiUrl $Temp
if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $Temp ".git"))) {
    throw "Wiki repo not initialized. Create first page at https://github.com/traceless929/PixelBar/wiki/_new then retry."
}

Copy-Item -Path (Join-Path $Source "*") -Destination $Temp -Force
Remove-Item -Force (Join-Path $Temp "README.md") -ErrorAction SilentlyContinue

Push-Location $Temp
git add -A
$status = git status --porcelain
if ($status) {
    git -c user.name="traceless929" -c user.email="traceless0929@qq.com" commit -m "docs: sync wiki from docs/wiki"
    git push origin master
    if ($LASTEXITCODE -ne 0) {
        git push origin main
    }
    if ($LASTEXITCODE -ne 0) {
        throw "git push failed"
    }
    Write-Host "Wiki published: https://github.com/traceless929/PixelBar/wiki"
} else {
    Write-Host "Wiki already up to date."
}
Pop-Location
