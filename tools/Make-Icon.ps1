# Draws src\EasyPixel\Assets\EasyPixel.ico: a green pixel-art pencil (crisp squares) on the dark rounded tile of
# EasyEngine's icon, in the same green, at 16-256 px.
# Run from anywhere:  powershell -ExecutionPolicy Bypass -File tools\Make-Icon.ps1
# -PngDir <folder>: writes the same drawing as EasyPixel-<size>.png there instead; the .ico is left alone.
param([string]$PngDir)
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root 'src\EasyPixel\Assets'
New-Item -ItemType Directory -Force $dir | Out-Null
$out = Join-Path $dir 'EasyPixel.ico'
$sizes = 256, 64, 48, 32, 24, 16

# The pencil as an 8x8 grid: W body, P eraser, D tip, K lead, . empty
$pencil = @(
    '......PP',
    '.....PPP',
    '....WWP.',
    '...WWW..',
    '..WWW...',
    '.DWW....',
    '.DD.....',
    'K.......'
)
# all greens: EasyEngine's (24,185,138) for the body, lighter for the eraser and the lead, darker for the wood
$colors = @{
    'W' = [System.Drawing.Color]::FromArgb(255, 24, 185, 138)
    'P' = [System.Drawing.Color]::FromArgb(255, 110, 231, 183)
    'D' = [System.Drawing.Color]::FromArgb(255, 15, 118, 88)
    'K' = [System.Drawing.Color]::FromArgb(255, 110, 231, 183)
}

function Rounded([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = [single]([Math]::Min($r * 2, [Math]::Min($w, $h)))
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return ,$p
}

function Draw-Png([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    # dark rounded tile, the same as EasyEngine's icon
    $g.SmoothingMode = 'AntiAlias'
    $m = [single]([Math]::Max(0, $s * 0.02)); $w = [single]($s - 2 * $m)
    $tile = Rounded $m $m $w $w ([single]($s * 0.22))
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 28, 28, 34))), $tile)
    # the pencil, whole pixels so it stays crisp at every size
    $g.SmoothingMode = 'None'; $g.PixelOffsetMode = 'Half'
    $cell = [Math]::Max(1, [Math]::Floor($s * 0.68 / 8))
    $origin = [Math]::Floor(($s - $cell * 8) / 2)
    for ($row = 0; $row -lt 8; $row++) {
        for ($col = 0; $col -lt 8; $col++) {
            $c = $pencil[$row][$col]
            if ($c -eq '.') { continue }
            $b = New-Object System.Drawing.SolidBrush $colors[[string]$c]
            $g.FillRectangle($b, [int]($origin + $col * $cell), [int]($origin + $row * $cell), [int]$cell, [int]$cell)
            $b.Dispose()
        }
    }
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    return ,$ms.ToArray()
}

if ($PngDir) {
    New-Item -ItemType Directory -Force $PngDir | Out-Null
    foreach ($s in 16, 32, 64, 128, 256, 512, 1024) {
        [System.IO.File]::WriteAllBytes((Join-Path $PngDir ("EasyPixel-{0:D4}.png" -f $s)), (Draw-Png $s))
    }
    "wrote the PNGs in $PngDir"
    return
}

# ICO = header + one directory entry per size + the PNGs
$pngs = $sizes | ForEach-Object { ,(Draw-Png $_) }
$fs = [System.IO.File]::Create($out); $bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $pngs[$i].Length
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s }))); $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$len); $bw.Write([uint32]$offset); $offset += $len
}
foreach ($p in $pngs) { $bw.Write([byte[]]$p) }
$bw.Close()
"wrote $out"
