param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$templatePath = Join-Path $PSScriptRoot "..\RELEASE_NOTES.template.md"
$content = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8
$content = $content.Replace("{VERSION}", $Version)

$dir = Split-Path -Parent $OutputPath
if ($dir) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($OutputPath, $content, $utf8)
