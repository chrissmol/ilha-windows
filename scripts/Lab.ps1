# Tito lab: drives the real app through every transition without needing the screen.
# Pass 1 (--lab) measures render-frame timing; pass 2 (--lab-frames) saves PNG frames per transition.
# Output: Dados\lab\report.txt and Dados\lab\sheet-*.png (contact sheets, one per transition).
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$env:TITO_ROOT=$root
$runtime=Join-Path $root ([IO.File]::ReadAllText((Join-Path $root 'current-version.txt')).Trim())
$lab=Join-Path $root 'Dados\lab'
Get-Process TitoGlass -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 1
foreach($mode in '--lab','--lab-frames'){
  $p=Start-Process -FilePath $runtime -ArgumentList $mode -PassThru
  if(-not $p.WaitForExit(90000)){$p.Kill();throw "Lab $mode timed out."}
  if($p.ExitCode -ne 0){throw "Lab $mode failed: $($p.ExitCode)"}
  if($mode -eq '--lab'){Copy-Item (Join-Path $lab 'report.txt') (Join-Path $lab 'timing.txt') -Force}
}
Add-Type -AssemblyName System.Drawing
Get-ChildItem $lab -Directory | ForEach-Object {
  $files=Get-ChildItem $_.FullName -Filter *.png | Sort-Object Name
  if($files.Count -eq 0){return}
  $pick=@(); $n=[Math]::Min(10,$files.Count); for($i=0;$i -lt $n;$i++){$pick+=$files[[int]($i*($files.Count-1)/[Math]::Max(1,$n-1))]}
  $w=256;$h=121;$sheet=New-Object Drawing.Bitmap ($w*5),($h*[Math]::Ceiling($n/5)); $g=[Drawing.Graphics]::FromImage($sheet); $g.Clear([Drawing.Color]::FromArgb(32,36,46))
  for($i=0;$i -lt $n;$i++){$img=[Drawing.Image]::FromFile($pick[$i].FullName);$g.DrawImage($img,($i%5)*$w,[Math]::Floor($i/5)*$h,$w,$h);$img.Dispose()}
  $sheet.Save((Join-Path $lab ("sheet-"+$_.Name+".png")));$g.Dispose();$sheet.Dispose()
}
Get-Content (Join-Path $lab 'timing.txt')
Write-Output "Sheets: $lab\sheet-*.png"
