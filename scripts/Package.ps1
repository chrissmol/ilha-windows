param([switch]$SkipBuild)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(-not $SkipBuild){& (Join-Path $root 'Compilar.ps1')}
$dist=Join-Path $root 'dist'
$stage=Join-Path $dist ('stage-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($stage) | Out-Null
$relative=[IO.File]::ReadAllText((Join-Path $root 'current-version.txt')).Trim()
$files=@('Tito.exe','Tito.ico','Iniciar.cmd','current-version.txt','MediaBridge.ps1','ClaudeHook.ps1','README.md','LICENSE','PRIVACY.md','CODE_SIGNING_POLICY.md',$relative,($relative+'.config'))
foreach($file in $files){
 $target=Join-Path $stage $file
 [IO.Directory]::CreateDirectory((Split-Path $target -Parent)) | Out-Null
 Copy-Item -LiteralPath (Join-Path $root $file) -Destination $target
}
$zip=Join-Path $dist 'Tito-Windows-unsigned.zip'
Copy-Item -LiteralPath (Join-Path $root 'Fonts\LICENSE.txt') -Destination (Join-Path $stage 'Inter-LICENSE.txt')
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
Write-Output $zip
