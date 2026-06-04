$ffmpegDir = "$PSScriptRoot\ffmpeg"
$ffmpegExe = "$ffmpegDir\ffmpeg.exe"

if (Test-Path $ffmpegExe) {
    Write-Host "FFmpeg already installed: $ffmpegExe"
    exit 0
}

Write-Host "Downloading ffmpeg..."

$url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
$zipPath = "$env:TEMP\ffmpeg_download.zip"

try {
    $progressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing
    $progressPreference = 'Continue'

    Write-Host "Extracting..."
    Expand-Archive -Path $zipPath -DestinationPath $env:TEMP -Force

    $found = Get-ChildItem -Path $env:TEMP -Filter "ffmpeg.exe" -Recurse | Select-Object -First 1

    if ($found) {
        New-Item -ItemType Directory -Force -Path $ffmpegDir | Out-Null
        Copy-Item $found.FullName $ffmpegExe -Force
        Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
        Remove-Item "$env:TEMP\ffmpeg-*" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "FFmpeg installed: $ffmpegExe"
    }
    else {
        Write-Host "ERROR: ffmpeg.exe not found"
        exit 1
    }
}
catch {
    Write-Host "Download error: $_"
    exit 1
}
