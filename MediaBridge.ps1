param([int]$OwnerPid,[switch]$Once)
$ErrorActionPreference='Stop'
$data=Join-Path $PSScriptRoot 'Dados'
[IO.Directory]::CreateDirectory($data) | Out-Null
$statePath=Join-Path $data 'media.json'
function Publish($state) {
 $state.updated=[DateTime]::UtcNow.ToString('o')
 $json=$state | ConvertTo-Json -Compress -Depth 5
 $tmp=Join-Path $data ('media-'+$PID+'.tmp')
 [IO.File]::WriteAllText($tmp,$json,[Text.UTF8Encoding]::new($false))
 if([IO.File]::Exists($statePath)){[IO.File]::Replace($tmp,$statePath,($statePath+'.bak'))}else{[IO.File]::Move($tmp,$statePath)}
}
try {
 Add-Type -AssemblyName System.Runtime.WindowsRuntime
 $managerType=[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager,Windows.Media.Control,ContentType=WindowsRuntime]
 $propsType=[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties,Windows.Media.Control,ContentType=WindowsRuntime]
 $streamType=[Windows.Storage.Streams.IRandomAccessStreamWithContentType,Windows.Storage.Streams,ContentType=WindowsRuntime]
 $asTask=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {$_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'} | Select-Object -First 1
 $asRead=[System.IO.WindowsRuntimeStreamExtensions].GetMethods() | Where-Object {$_.Name -eq 'AsStreamForRead' -and $_.GetParameters().Count -eq 1} | Select-Object -First 1
 function Await($operation,[Type]$type) { $task=$asTask.MakeGenericMethod($type).Invoke($null,@($operation));if(-not $task.Wait(3000)){throw 'A leitura da mídia excedeu o tempo limite.'};return $task.Result }
 $manager=Await ($managerType::RequestAsync()) $managerType
 $lastKey='';$cover='';$lastCommand=''
 $oldCommand=Join-Path $data 'media-command.json'
 if(Test-Path -LiteralPath $oldCommand){try{$lastCommand=(Get-Content -LiteralPath $oldCommand -Raw|ConvertFrom-Json).id}catch{}}
 do {
  try {
   $session=$manager.GetCurrentSession()
   $sessions=@($manager.GetSessions())
   $sources=@();foreach($entry in $sessions){try{$entryProps=Await ($entry.TryGetMediaPropertiesAsync()) $propsType;$sources+=@{source=$entry.SourceAppUserModelId;title=$entryProps.Title;playing=($entry.GetPlaybackInfo().PlaybackStatus.ToString() -eq 'Playing')}}catch{}}
   $preferred='';$sourcePath=Join-Path $data 'media-source.txt';if(Test-Path -LiteralPath $sourcePath){$preferred=[IO.File]::ReadAllText($sourcePath).Trim()}
   $pinned=$false;if($preferred){foreach($entry in $sessions){if($entry.SourceAppUserModelId -eq $preferred){$session=$entry;$pinned=$true;break}}}
   # Prefer the Windows-selected source when playing; otherwise find an active
   # session from any application, including browsers and video players.
   $currentPlaying=$false
   if($null -ne $session){try{$currentPlaying=$session.GetPlaybackInfo().PlaybackStatus.ToString() -eq 'Playing'}catch{}}
   if(-not $currentPlaying -and -not $pinned){
    foreach($candidate in $sessions){
     try{if($candidate.GetPlaybackInfo().PlaybackStatus.ToString() -eq 'Playing'){$session=$candidate;break}}catch{}
    }
   }
   if($null -eq $session){Publish @{available=$false;error='';title='Nenhuma mídia ativa';artist='Reproduza áudio ou vídeo em um aplicativo ou navegador compatível.';playing=$false;position=0;duration=0;cover=''}}else{
    $commandPath=Join-Path $data 'media-command.json'
    if(Test-Path -LiteralPath $commandPath){
     try {
      $command=Get-Content -LiteralPath $commandPath -Raw | ConvertFrom-Json
      if($command.id -ne $lastCommand){$lastCommand=$command.id
       $commandSession=$session
       if($command.source){$commandSession=$null;foreach($entry in $sessions){if($entry.SourceAppUserModelId -eq $command.source){$commandSession=$entry;break}}}
       if($null -eq $commandSession){throw 'A origem de mídia foi encerrada.'}
       switch($command.action){
        'toggle' {$null=Await ($commandSession.TryTogglePlayPauseAsync()) ([bool])}
        'next' {$null=Await ($commandSession.TrySkipNextAsync()) ([bool])}
        'previous' {$null=Await ($commandSession.TrySkipPreviousAsync()) ([bool])}
        'seek' {$null=Await ($commandSession.TryChangePlaybackPositionAsync([long]([double]$command.position*10000000))) ([bool])}
       }
      }
     }catch{ }
    }
    $props=Await ($session.TryGetMediaPropertiesAsync()) $propsType
    $timeline=$session.GetTimelineProperties();$playback=$session.GetPlaybackInfo()
    $key=$session.SourceAppUserModelId+'|'+$props.Title+'|'+$props.Artist
    if($key -ne $lastKey){
     $lastKey=$key;$cover=''
     if($null -ne $props.Thumbnail){
      try{
       $stream=Await ($props.Thumbnail.OpenReadAsync()) $streamType
       $inputStream=$asRead.Invoke($null,@($stream.psobject.BaseObject))
       $sha=[Security.Cryptography.SHA256]::Create();$hash=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($key))).Replace('-','').Substring(0,20);$sha.Dispose()
       $cover=Join-Path $data ('cover-'+$hash+'.jpg')
       $outputStream=[IO.File]::Create($cover);try{$inputStream.CopyTo($outputStream)}finally{$outputStream.Dispose();$inputStream.Dispose();try{[void][Runtime.InteropServices.Marshal]::ReleaseComObject($stream.psobject.BaseObject)}catch{}}
      }catch{$cover=''}
     }
    }
    $position=$timeline.Position.TotalSeconds
    $rate=$playback.PlaybackRate;if($null -eq $rate){$rate=1}
    if($playback.PlaybackStatus.ToString() -eq 'Playing'){$position+=[Math]::Max(0,([DateTimeOffset]::Now-$timeline.LastUpdatedTime).TotalSeconds)*$rate}
    Publish @{available=$true;error='';sessions=$sources;title=$props.Title;artist=$props.Artist;album=$props.AlbumTitle;source=$session.SourceAppUserModelId;playing=($playback.PlaybackStatus.ToString() -eq 'Playing');position=$position;duration=$timeline.EndTime.TotalSeconds;cover=$cover;canSeek=$playback.Controls.IsPlaybackPositionEnabled;canNext=$playback.Controls.IsNextEnabled;canPrevious=$playback.Controls.IsPreviousEnabled;canToggle=$playback.Controls.IsPlayPauseToggleEnabled}
   }
  }catch{Publish @{available=$false;error=$_.Exception.GetBaseException().Message;title='Mídia indisponível';artist='O Windows não forneceu os dados da sessão.';playing=$false;position=0;duration=0;cover=''}}
  if($Once){break}
  Start-Sleep -Milliseconds 800
 }while($OwnerPid -gt 0 -and (Get-Process -Id $OwnerPid -ErrorAction SilentlyContinue))
}catch{Publish @{available=$false;error=$_.Exception.GetBaseException().Message;title='Mídia indisponível';artist='O serviço de mídia do Windows não está disponível.';playing=$false;position=0;duration=0;cover=''}}
