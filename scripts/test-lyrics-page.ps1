# 对完整发布目录检查歌词时间轴选择，以及实际 WinUI 页面构造、加载和 Frame.Navigate。
# 不启动托盘或歌词推送，不发送 HID 数据，不操作鼠标或键盘。
param(
    [Parameter(Mandatory)]
    [string]$PublishDirectory,
    [string]$LogPath = (Join-Path $env:TEMP ('PixelBar-LyricsPage-' + [guid]::NewGuid() + '.log'))
)

$ErrorActionPreference = 'Stop'
$publishRoot = (Resolve-Path -LiteralPath $PublishDirectory).Path
$testExe = Join-Path $publishRoot 'PixelBar.App.exe'
if (-not (Test-Path -LiteralPath $testExe)) { throw '找不到发布程序 PixelBar.App.exe。' }
$testLogPath = [IO.Path]::GetFullPath($LogPath)
if (Test-Path -LiteralPath $testLogPath) { throw '请指定新的日志路径，避免覆盖已有测试记录。' }
$previousLogPath = $env:PIXELBAR_SELF_TEST_LOG
$testProcess = $null
try {
    $env:PIXELBAR_SELF_TEST_LOG = $testLogPath
    $testProcess = Start-Process -FilePath $testExe -ArgumentList '--self-test-lyrics' -PassThru -WindowStyle Hidden
    if (-not $testProcess.WaitForExit(30000)) {
        throw '歌词页面检查超过 30 秒未完成。'
    }
    $testLog = if (Test-Path -LiteralPath $testLogPath) { Get-Content -LiteralPath $testLogPath -Raw } else { '' }
    Write-Host $testLog
    if ($testProcess.ExitCode -ne 0 -or
        $testLog -notmatch '(?m)^\S+ PASS \d+ timeline checks:' -or
        $testLog -notmatch '(?m)^\S+ PASS \d+ NetEase API response checks:' -or
        $testLog -notmatch '(?m)^\S+ PASS \d+ clock sync checks:' -or
        $testLog -notmatch '(?m)^\S+ PASS ClockPage navigation and Loaded, twice; preferences unchanged' -or
        $testLog -notmatch '(?m)^\S+ PASS direct construction and Frame.Navigate, twice each; preferences unchanged') {
        throw "歌词页面检查失败，退出码：$($testProcess.ExitCode)，日志：$testLogPath"
    }
} finally {
    $env:PIXELBAR_SELF_TEST_LOG = $previousLogPath
    if ($testProcess -and -not $testProcess.HasExited) {
        $testProcess.Kill()
        $testProcess.WaitForExit()
    }
    if ($testProcess) { $testProcess.Dispose() }
}
