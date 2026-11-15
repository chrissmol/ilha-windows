$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$env:ILHA_ROOT=$root
$runtime=Join-Path $root ([IO.File]::ReadAllText((Join-Path $root 'current-version.txt')).Trim())
$result=Join-Path $root 'Dados\ui-test.txt'
$before=[DateTime]::UtcNow
$process=Start-Process -FilePath $runtime -ArgumentList '--ui-test' -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(15000)){throw 'Resident application lifecycle timed out.'}
if($process.ExitCode -ne 0){throw ('Lifecycle checks failed: '+$process.ExitCode)}
if(-not (Test-Path -LiteralPath $result)){throw 'No lifecycle report.'}
if((Get-Item -LiteralPath $result).LastWriteTimeUtc -lt $before){throw 'Stale lifecycle report.'}
$content=[IO.File]::ReadAllText($result)
if(-not $content.StartsWith('PASS:')){throw $content}
Write-Output $content
