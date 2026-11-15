$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$env:ILHA_ROOT=$root
$runtime=Join-Path $root ([IO.File]::ReadAllText((Join-Path $root 'current-version.txt')).Trim())
$result=Join-Path $root 'Dados\test-result.txt'
$before=[DateTime]::UtcNow
$process=Start-Process -FilePath $runtime -ArgumentList '--test' -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(15000)){throw 'Application checks timed out.'}
if($process.ExitCode -ne 0){throw ('Application checks failed with code '+$process.ExitCode)}
if(-not (Test-Path -LiteralPath $result)){throw 'No check report was produced.'}
if((Get-Item -LiteralPath $result).LastWriteTimeUtc -lt $before){throw 'The check report is stale.'}
$content=[IO.File]::ReadAllText($result)
if(-not $content.StartsWith('PASS:')){throw $content}
Write-Output $content
