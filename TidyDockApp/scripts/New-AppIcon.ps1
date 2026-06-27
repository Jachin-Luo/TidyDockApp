$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$assetRoot = Join-Path $projectRoot "assets"
$iconPath = Join-Path $assetRoot "TidyDock.ico"
New-Item -ItemType Directory -Force -Path $assetRoot | Out-Null

function New-IconPngBytes {
    param([int]$Size)

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $rect = New-Object System.Drawing.RectangleF 0, 0, $Size, $Size
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = [Math]::Max(4, [int]($Size * 0.22))
    $diameter = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 46, 128, 232)), ([System.Drawing.Color]::FromArgb(255, 40, 186, 146)), 135
    $graphics.FillPath($bg, $path)

    $dockRect = New-Object System.Drawing.RectangleF ($Size * 0.17), ($Size * 0.59), ($Size * 0.66), ($Size * 0.18)
    $dockPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $dockRadius = [Math]::Max(3, [int]($Size * 0.06))
    $dockDiameter = $dockRadius * 2
    $dockPath.AddArc($dockRect.X, $dockRect.Y, $dockDiameter, $dockDiameter, 180, 90)
    $dockPath.AddArc($dockRect.Right - $dockDiameter, $dockRect.Y, $dockDiameter, $dockDiameter, 270, 90)
    $dockPath.AddArc($dockRect.Right - $dockDiameter, $dockRect.Bottom - $dockDiameter, $dockDiameter, $dockDiameter, 0, 90)
    $dockPath.AddArc($dockRect.X, $dockRect.Bottom - $dockDiameter, $dockDiameter, $dockDiameter, 90, 90)
    $dockPath.CloseFigure()

    $dockBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(205, 255, 255, 255))
    $graphics.FillPath($dockBrush, $dockPath)

    $dotBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 255, 255, 255))
    foreach ($x in @(0.30, 0.43, 0.56, 0.69)) {
        $diam = $Size * 0.085
        $graphics.FillEllipse($dotBrush, ($Size * $x) - ($diam / 2), ($Size * 0.68) - ($diam / 2), $diam, $diam)
    }

    $highlight = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(100, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(0, 255, 255, 255)), 90
    $graphics.FillPath($highlight, $path)

    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $stream.ToArray()

    $stream.Dispose()
    $highlight.Dispose()
    $dotBrush.Dispose()
    $dockBrush.Dispose()
    $dockPath.Dispose()
    $bg.Dispose()
    $path.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()

    return $bytes
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = @()
foreach ($size in $sizes) {
    $images += ,@{
        Size = $size
        Bytes = New-IconPngBytes -Size $size
    }
}

$file = [System.IO.File]::Create($iconPath)
$writer = New-Object System.IO.BinaryWriter $file

$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$images.Count)

$offset = 6 + ($images.Count * 16)
foreach ($image in $images) {
    $size = [int]$image.Size
    $bytes = [byte[]]$image.Bytes
    $encodedSize = $size
    if ($size -eq 256) {
        $encodedSize = 0
    }
    $writer.Write([byte]$encodedSize)
    $writer.Write([byte]$encodedSize)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$bytes.Length)
    $writer.Write([UInt32]$offset)
    $offset += $bytes.Length
}

foreach ($image in $images) {
    $writer.Write([byte[]]$image.Bytes)
}

$writer.Dispose()
$file.Dispose()

Write-Host $iconPath
