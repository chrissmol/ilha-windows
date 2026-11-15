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
 function Await($operation,[Type]$type) { $task=$asTask.MakeGenericMethod($type).Invoke($null,@($operation));if(-not $task.Wait(3000)){throw 'A leitura da mídia excedeu o tempo limite.'};return $task.Result }
 $manager=Await ($managerType::RequestAsync()) $managerType
 $lastKey='';$cover='';$lastCommand=''
 $oldCommand=Join-Path $data 'media-command.json'
 if(Test-Path -LiteralPath $oldCommand){try{$lastCommand=(Get-Content -LiteralPath $oldCommand -Raw|ConvertFrom-Json).id}catch{}}
 do {
  try {
   $session=$manager.GetCurrentSession()
   if($null -eq $session){Publish @{available=$false;error='';title='Nenhuma mídia ativa';artist='Abra uma música no Spotify ou no navegador.';playing=$false;position=0;duration=0;cover=''}}else{
    $commandPath=Join-Path $data 'media-command.json'
    if(Test-Path -LiteralPath $commandPath){
     try {
      $command=Get-Content -LiteralPath $commandPath -Raw | ConvertFrom-Json
      if($command.id -ne $lastCommand){$lastCommand=$command.id
       switch($command.action){
        'toggle' {$null=Await ($session.TryTogglePlayPauseAsync()) ([bool])}
        'next' {$null=Await ($session.TrySkipNextAsync()) ([bool])}
        'previous' {$null=Await ($session.TrySkipPreviousAsync()) ([bool])}
        'seek' {$null=Await ($session.TryChangePlaybackPositionAsync([long]([double]$command.position*10000000))) ([bool])}
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
       $inputStream=[System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($stream)
       $sha=[Security.Cryptography.SHA256]::Create();$hash=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($key))).Replace('-','').Substring(0,20);$sha.Dispose()
       $cover=Join-Path $data ('cover-'+$hash+'.jpg')
       $outputStream=[IO.File]::Create($cover);try{$inputStream.CopyTo($outputStream)}finally{$outputStream.Dispose();$inputStream.Dispose();$stream.Dispose()}
      }catch{$cover=''}
     }
    }
    $position=$timeline.Position.TotalSeconds
    $rate=$playback.PlaybackRate;if($null -eq $rate){$rate=1}
    if($playback.PlaybackStatus.ToString() -eq 'Playing'){$position+=[Math]::Max(0,([DateTimeOffset]::Now-$timeline.LastUpdatedTime).TotalSeconds)*$rate}
    Publish @{available=$true;error='';title=$props.Title;artist=$props.Artist;album=$props.AlbumTitle;source=$session.SourceAppUserModelId;playing=($playback.PlaybackStatus.ToString() -eq 'Playing');position=$position;duration=$timeline.EndTime.TotalSeconds;cover=$cover;canSeek=$playback.Controls.IsPlaybackPositionEnabled;canNext=$playback.Controls.IsNextEnabled;canPrevious=$playback.Controls.IsPreviousEnabled;canToggle=$playback.Controls.IsPlayPauseToggleEnabled}
   }
  }catch{Publish @{available=$false;error=$_.Exception.GetBaseException().Message;title='Mídia indisponível';artist='O Windows não forneceu os dados da sessão.';playing=$false;position=0;duration=0;cover=''}}
  if($Once){break}
  Start-Sleep -Milliseconds 800
 }while($OwnerPid -gt 0 -and (Get-Process -Id $OwnerPid -ErrorAction SilentlyContinue))
}catch{Publish @{available=$false;error=$_.Exception.GetBaseException().Message;title='Mídia indisponível';artist='O serviço de mídia do Windows não está disponível.';playing=$false;position=0;duration=0;cover=''}}
