# Explica por que a Tito nao abriu: Smart App Control, modo restrito do PowerShell ou erro de compilacao.
$sac=(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -ErrorAction SilentlyContinue).VerifiedAndReputablePolicyState
Write-Output ('Smart App Control: '+@{0='desligado';1='LIGADO (bloqueia exe sem assinatura)';2='em avaliacao'}[[int]$sac])
Write-Output ('Modo do PowerShell: '+$ExecutionContext.SessionState.LanguageMode+' (ConstrainedLanguage = scripts sem assinatura limitados pelo Windows)')
$events=Get-WinEvent -FilterHashtable @{LogName='Microsoft-Windows-CodeIntegrity/Operational';Id=3076,3077} -MaxEvents 5 -ErrorAction SilentlyContinue
if($events){Write-Output 'Bloqueios recentes do Windows (Code Integrity):';$events|ForEach-Object{Write-Output (' '+$_.TimeCreated+' '+($_.Message -split "`n")[0])}}
else{Write-Output 'Nenhum bloqueio recente de Code Integrity encontrado.'}
Write-Output 'Envie esta saida ao Claude para decidirmos o proximo passo.'
