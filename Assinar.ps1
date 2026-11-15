param(
 [Parameter(Mandatory=$true)][string]$Thumbprint,
 [string]$TimestampServer='http://timestamp.digicert.com'
)
$ErrorActionPreference='Stop'
$certificate=Get-Item -LiteralPath ('Cert:\CurrentUser\My\'+$Thumbprint)
if(-not $certificate.HasPrivateKey){throw 'O certificado precisa de uma chave privada acessível.'}
if($certificate.Subject -eq $certificate.Issuer){throw 'Certificados autoassinados não atendem ao Smart App Control.'}
if($certificate.NotAfter -le (Get-Date)){throw 'O certificado expirou.'}
$runtime=Join-Path $PSScriptRoot ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'current-version.txt')).Trim())
$root=[IO.Path]::GetFullPath($PSScriptRoot)+[IO.Path]::DirectorySeparatorChar
if(-not [IO.Path]::GetFullPath($runtime).StartsWith($root,[StringComparison]::OrdinalIgnoreCase)){throw 'Caminho da versão fora da pasta da Ilha.'}
$files=@('Ilha.exe','IlhaGlass.exe','MediaBridge.ps1','ClaudeHook.ps1')|ForEach-Object {Join-Path $PSScriptRoot $_}
$files+= $runtime
foreach($file in $files){
 $signature=Set-AuthenticodeSignature -LiteralPath $file -Certificate $certificate -HashAlgorithm SHA256 -TimestampServer $TimestampServer
 if($signature.Status -ne 'Valid'){throw ('A assinatura não foi validada: '+$file+' — '+$signature.StatusMessage)}
}
Write-Output 'Assinaturas verificadas. Teste a abertura no Windows antes de distribuir.'
