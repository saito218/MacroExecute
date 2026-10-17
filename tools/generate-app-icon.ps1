param([string]$OutputPath = (Join-Path $PSScriptRoot '../assets/MacroExecute.ico'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$frames=@()
foreach($size in @(16,24,32,48,64,128,256)) {
    $bitmap=New-Object System.Drawing.Bitmap($size,$size)
    $graphics=[System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform($size/32.0,$size/32.0)
    $path=New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(1,1,12,12,180,90);$path.AddArc(19,1,12,12,270,90);$path.AddArc(19,19,12,12,0,90);$path.AddArc(1,19,12,12,90,90);$path.CloseFigure()
    $fill=New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#16131e'))
    $accent=New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#d9f777'))
    $pen=New-Object System.Drawing.Pen([System.Drawing.ColorTranslator]::FromHtml('#d9f777'),2.3)
    $pen.StartCap=$pen.EndCap=[System.Drawing.Drawing2D.LineCap]::Round
    $graphics.FillPath($fill,$path)
    $graphics.DrawArc($pen,5,5,22,22,45,280)
    $graphics.DrawLines($pen,[System.Drawing.PointF[]]@((New-Object System.Drawing.PointF(24,3)),(New-Object System.Drawing.PointF(26,9)),(New-Object System.Drawing.PointF(20,8))))
    $graphics.FillPolygon($accent,[System.Drawing.PointF[]]@((New-Object System.Drawing.PointF(13,10)),(New-Object System.Drawing.PointF(23,16)),(New-Object System.Drawing.PointF(13,22))))
    $stream=New-Object System.IO.MemoryStream
    $bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
    $frames+=,@{Size=$size;Bytes=$stream.ToArray()}
    $stream.Dispose();$pen.Dispose();$accent.Dispose();$fill.Dispose();$path.Dispose();$graphics.Dispose();$bitmap.Dispose()
}
$file=[System.IO.File]::Create([System.IO.Path]::GetFullPath($OutputPath))
$writer=New-Object System.IO.BinaryWriter($file)
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$frames.Count)
    $offset=6+16*$frames.Count
    foreach($frame in $frames){$dimension=if($frame.Size -eq 256){0}else{$frame.Size};$writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame.Bytes.Length);$writer.Write([uint32]$offset);$offset+=$frame.Bytes.Length}
    foreach($frame in $frames){$writer.Write([byte[]]$frame.Bytes)}
}finally{$writer.Dispose();$file.Dispose()}
