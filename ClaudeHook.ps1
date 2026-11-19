$ErrorActionPreference='SilentlyContinue'
try {
 $eventData=[Console]::In.ReadToEnd() | ConvertFrom-Json
 $folder=Join-Path $PSScriptRoot 'Dados'
 [IO.Directory]::CreateDirectory($folder) | Out-Null
 $record=@{event=$eventData.hook_event_name;session=$eventData.session_id;tool=$eventData.tool_name;project=[IO.Path]::GetFileName($eventData.cwd);updated=[DateTime]::UtcNow.ToString('o')}
 $filePath=$eventData.tool_input.file_path
 if($filePath){$record.file=[IO.Path]::GetFileName($filePath)}
 $sid=[string]$eventData.session_id
 if($sid -match '^[A-Za-z0-9_-]+$'){$sdir=Join-Path $folder 'sessions';[IO.Directory]::CreateDirectory($sdir) | Out-Null;$sfile=Join-Path $sdir ($sid+'.json');if($eventData.hook_event_name -eq 'SessionEnd'){[IO.File]::Delete($sfile)}else{[IO.File]::WriteAllText($sfile,($record|ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))}}
 $dest=Join-Path $folder 'agent.json';$tmp=Join-Path $folder ('agent-'+$PID+'.tmp')
 [IO.File]::WriteAllText($tmp,($record|ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
 if([IO.File]::Exists($dest)){[IO.File]::Replace($tmp,$dest,($dest+'.bak'))}else{[IO.File]::Move($tmp,$dest)}
}catch{}
exit 0
