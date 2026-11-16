param([Parameter(Mandatory=$true)][string]$Archive)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$stage=Join-Path $root ('dist\verify-'+[Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $Archive -DestinationPath $stage
$relative=[IO.File]::ReadAllText((Join-Path $stage 'current-version.txt')).Trim()
$files=@('Tito.exe','MediaBridge.ps1','ClaudeHook.ps1',$relative)
foreach($file in $files){
 $signature=Get-AuthenticodeSignature -LiteralPath (Join-Path $stage $file)
 if($signature.Status -ne 'Valid'){throw ('Invalid signature: '+$file+' — '+$signature.Status)}
 if($signature.SignerCertificate.Subject -eq $signature.SignerCertificate.Issuer){throw ('Self-signed certificate: '+$file)}
}
Write-Output 'All expected runtime signatures are valid.'
