# Quick probe: list SMTC media sessions (run while QQ Music is playing)
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media, ContentType=WindowsRuntime]
$asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]

function Await($asyncOp, $type) {
    $m = $asTask.MakeGenericMethod($type)
    $task = $m.Invoke($null, @($asyncOp))
    $task.GetAwaiter().GetResult()
    return $task.Result
}

$manager = Await ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]::RequestAsync()) ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager])
foreach ($session in $manager.GetSessions()) {
    $appId = $session.SourceAppUserModelId
    $props = Await ($session.TryGetMediaPropertiesAsync()) ([Windows.Media.MediaProperties.MusicDisplayProperties])
    $timeline = $session.GetTimelineProperties()
    $playback = $session.GetPlaybackInfo()
    [PSCustomObject]@{
        AppId = $appId
        Title = $props.Title
        Artist = $props.Artist
        Position = $timeline.Position
        Status = $playback.PlaybackStatus
    }
}
