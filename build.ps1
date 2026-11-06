# Monitor Yayici derleme betigi: simgeyi uretir, sonra csc ile derler (ek kurulum gerekmez).
Add-Type -AssemblyName System.Drawing

# ---- simge (256x256 PNG, ICO icinde)
$bmp = New-Object System.Drawing.Bitmap 256, 256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.Clear([System.Drawing.Color]::Transparent)

function RoundRect($x, $y, $w, $h, $r) {
  $p = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = $r * 2
  $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
  $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
  $p.CloseFigure(); return $p
}

$bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush ([System.Drawing.Point]::new(0, 0)), ([System.Drawing.Point]::new(256, 256)), ([System.Drawing.Color]::FromArgb(255, 30, 36, 56)), ([System.Drawing.Color]::FromArgb(255, 14, 16, 24))
$g.FillPath($bgBrush, (RoundRect 4 4 248 248 52))

$accent = [System.Drawing.Color]::FromArgb(255, 82, 153, 255)
$good = [System.Drawing.Color]::FromArgb(255, 78, 201, 126)
$fillA = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 82, 153, 255))
$fillB = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 78, 201, 126))
$penA = New-Object System.Drawing.Pen $accent, 9
$penB = New-Object System.Drawing.Pen $good, 9
$g.FillPath($fillA, (RoundRect 30 78 90 84 10)); $g.DrawPath($penA, (RoundRect 30 78 90 84 10))
$g.FillPath($fillB, (RoundRect 136 78 90 84 10)); $g.DrawPath($penB, (RoundRect 136 78 90 84 10))

# ortadan gecen "yayma" oku
$arrow = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), 11
$arrow.StartCap = 'ArrowAnchor'; $arrow.EndCap = 'ArrowAnchor'
$g.DrawLine($arrow, 52, 196, 204, 196)
$g.Dispose()

$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $ms.ToArray()
$icoPath = Join-Path $PSScriptRoot 'MonitorYayici.ico'
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]1)          # ICONDIR
$bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([byte]0)   # 256x256, renk yok
$bw.Write([uint16]1); $bw.Write([uint16]32)                                 # planes, bpp
$bw.Write([uint32]$png.Length); $bw.Write([uint32]22)                       # boyut, ofset
$bw.Write($png)
$bw.Close(); $fs.Close()

# ---- derleme
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /win32icon:"$icoPath" /out:"$PSScriptRoot\MonitorYayici.exe" `
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "$PSScriptRoot\MonitorYayici.cs"
if ($LASTEXITCODE -eq 0) { "Derlendi: $PSScriptRoot\MonitorYayici.exe" } else { "DERLEME HATASI" }
