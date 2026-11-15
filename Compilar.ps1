$ErrorActionPreference='Stop'
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$version=Get-Date -Format 'yyyyMMdd-HHmmssfff'
$versionDir=Join-Path $PSScriptRoot ('Versions\'+$version)
[IO.Directory]::CreateDirectory($versionDir) | Out-Null
$references=@('WPF\PresentationFramework.dll','WPF\PresentationCore.dll','WPF\WindowsBase.dll','System.Xaml.dll','System.Windows.Forms.dll','System.Drawing.dll','System.Web.Extensions.dll') | ForEach-Object {'/reference:'+(Join-Path $framework $_)}
$sources=@('Glass.cs','NativeGlass.cs','Integrations.cs','Dashboard.cs','MascotEyes.cs','Presence.cs','Presentation.cs','LifecycleChecks.cs','AssemblyInfo.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:anycpu ('/win32icon:'+(Join-Path $PSScriptRoot 'Ilha.ico')) ('/win32manifest:'+(Join-Path $PSScriptRoot 'app.manifest')) ('/out:'+(Join-Path $versionDir 'IlhaGlass.exe')) @references @sources
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar a Ilha.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'IlhaGlass.exe.config') -Destination (Join-Path $versionDir 'IlhaGlass.exe.config')
& (Join-Path $framework 'csc.exe') /nologo /target:winexe ('/win32icon:'+(Join-Path $PSScriptRoot 'Ilha.ico')) ('/win32manifest:'+(Join-Path $PSScriptRoot 'app.manifest')) ('/reference:'+(Join-Path $framework 'System.Windows.Forms.dll')) ('/out:'+(Join-Path $PSScriptRoot 'Ilha.exe')) (Join-Path $PSScriptRoot 'Launcher.cs') (Join-Path $PSScriptRoot 'AssemblyInfo.cs')
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar o inicializador.'}
$manifest=Join-Path $PSScriptRoot 'current-version.txt'
$temp=$manifest+'.tmp'
[IO.File]::WriteAllText($temp,('Versions\'+$version+'\IlhaGlass.exe'),[Text.UTF8Encoding]::new($false))
if([IO.File]::Exists($manifest)){[IO.File]::Replace($temp,$manifest,($manifest+'.bak'))}else{[IO.File]::Move($temp,$manifest)}
Write-Output ('Versao compilada: '+$version)
