# Generuje Assets\app.ico: zaokraglony kwadrat z linia pulsu (styl "activity").
Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size) {
	$bmp = New-Object System.Drawing.Bitmap $size, $size
	$g = [System.Drawing.Graphics]::FromImage($bmp)
	$g.SmoothingMode = 'AntiAlias'
	$g.Clear([System.Drawing.Color]::Transparent)

	$pad = [single]($size * 0.06)
	$w = [single]($size - 2 * $pad)
	$d = [single]($size * 0.44)
	$path = New-Object System.Drawing.Drawing2D.GraphicsPath
	$path.AddArc($pad, $pad, $d, $d, 180, 90)
	$path.AddArc($pad + $w - $d, $pad, $d, $d, 270, 90)
	$path.AddArc($pad + $w - $d, $pad + $w - $d, $d, $d, 0, 90)
	$path.AddArc($pad, $pad + $w - $d, $d, $d, 90, 90)
	$path.CloseFigure()

	$brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(52, 152, 219))
	$g.FillPath($brush, $path)

	$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ([single]($size * 0.075))
	$pen.LineJoin = 'Round'
	$pen.StartCap = 'Round'
	$pen.EndCap = 'Round'
	$pts = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
	foreach ($p in @(@(0.20, 0.55), @(0.36, 0.55), @(0.45, 0.30), @(0.57, 0.72), @(0.65, 0.50), @(0.80, 0.50))) {
		$pts.Add((New-Object System.Drawing.PointF ([single]($p[0] * $size)), ([single]($p[1] * $size))))
	}
	$g.DrawLines($pen, $pts.ToArray())
	$g.Dispose()
	return ,$bmp
}

$sizes = 16, 32, 48, 256
$pngs = New-Object System.Collections.ArrayList
foreach ($s in $sizes) {
	$b = New-IconBitmap $s
	$ms = New-Object System.IO.MemoryStream
	$b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
	[void]$pngs.Add($ms.ToArray())
	$b.Dispose()
}

$outDir = Join-Path $PSScriptRoot '..\Assets'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$out = Join-Path $outDir 'app.ico'
$fs = [System.IO.File]::Create($out)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
	$s = $sizes[$i]
	$dim = if ($s -ge 256) { 0 } else { $s }
	$bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
	$bw.Write([uint16]1); $bw.Write([uint16]32)
	$bw.Write([uint32]$pngs[$i].Length); $bw.Write([uint32]$offset)
	$offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Dispose(); $fs.Dispose()
Write-Output "OK $out"
