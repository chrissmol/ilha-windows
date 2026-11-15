using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Web.Script.Serialization;
namespace Ilha {
public class MediaState {
 public bool available,playing,canSeek,canNext,canPrevious,canToggle;
 public string title,artist,album,source,cover,error,updated;
 public double position,duration;
}
public partial class App {
 static string baseDir=Environment.GetEnvironmentVariable("ILHA_ROOT")??AppDomain.CurrentDomain.BaseDirectory;
 static string dataDir=System.IO.Path.Combine(baseDir,"Dados");
 static JavaScriptSerializer json=new JavaScriptSerializer();
 static MediaState media=new MediaState();
 static bool useSystem=true; static Process mediaWorker; static int poll;
 static TextBlock mediaArtist,mediaHint,agentStatus,agentDetail,agentAge;
 static Image albumImage;static Border albumBorder;static string lastCover="",lastAgent="";
 static Slider systemSeek;static Button systemPlay,systemNext,systemPrevious;static Line rulerMarker;static Canvas rulerCanvas;
 static DispatcherTimer leaveTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(420)};
 static StackPanel NavIcon(string name,string label){
  var p=new StackPanel{Orientation=Orientation.Horizontal};
  string path=name=="home"?"M1,7 L7,1 13,7 M3,6 L3,13 11,13 11,6 M6,13 L6,9 8,9 8,13":name=="music"?"M5,11 L5,3 12,1 12,9 M5,4 L12,2 M5,11 C5,14 0,14 1,11 C2,9 5,9 5,11 M12,9 C12,12 7,12 8,9 C9,7 12,7 12,9":name=="focus"?"M7,1 A6,6 0 1 1 6.99,1 M7,4 L7,7 10,8":name=="robo"?"M2,2 L12,2 Q14,2 14,4 L14,10 Q14,12 12,12 L5,12 2,14 2,12 Q0,12 0,10 L0,4 Q0,2 2,2 M4,6 L4,8 M10,6 L10,8":name=="settings"?"M7,0 L9,3 12,2 12,5 14,7 12,9 12,12 9,11 7,14 5,11 2,12 2,9 0,7 2,5 2,2 5,3 Z M7,4 A3,3 0 1 1 6.99,4":"M7,0 L7,14 M0,7 L14,7 M2,2 L12,12 M2,12 L12,2";
  p.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse(path),Stroke=B("#E6DCD5"),StrokeThickness=1,Width=14,Height=14,VerticalAlignment=VerticalAlignment.Center,Stretch=Stretch.Uniform});
  if(label!=null){var text=T(label,10,"#F7F0EC");text.Margin=new Thickness(6,0,0,0);p.Children.Add(text);}return p;
 }
 static void SendMedia(string action,double pos=0){
  if(!media.available)return;
  string path=System.IO.Path.Combine(dataDir,"media-command.json");string temp=path+".tmp";
  File.WriteAllText(temp,json.Serialize(new{ id=Guid.NewGuid().ToString("N"),action=action,position=pos}),new System.Text.UTF8Encoding(false));
  if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
 }
 static void SystemMusic(){
  if(!useSystem){LocalMusic();return;}
  var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(184)});grid.ColumnDefinitions.Add(new ColumnDefinition());view.Children.Add(grid);
  albumImage=new Image{Stretch=Stretch.UniformToFill};
  var artwork=new Grid();artwork.Children.Add(new Border{Background=new LinearGradientBrush(B("#A9AA6556").Color,B("#70433D57").Color,65)});
  var moon=new Ellipse{Width=62,Height=62,Fill=B("#60DDBDAA"),VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(20,13,0,0)};artwork.Children.Add(moon);
  artwork.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse("M0,120 L35,87 61,101 95,77 135,104 175,90 175,150 0,150 Z"),Fill=B("#43382736"),Stretch=Stretch.Fill,VerticalAlignment=VerticalAlignment.Bottom,Height=69});
  artwork.Children.Add(albumImage);albumBorder=new Border{Width=164,Height=143,HorizontalAlignment=HorizontalAlignment.Left,CornerRadius=new CornerRadius(12),ClipToBounds=true,Child=artwork};
  albumBorder.SizeChanged+=(s,e)=>albumBorder.Clip=new RectangleGeometry(new Rect(0,0,albumBorder.ActualWidth,albumBorder.ActualHeight),12,12);grid.Children.Add(albumBorder);
  var info=new Grid();Grid.SetColumn(info,1);grid.Children.Add(info);
  mediaArtist=T("",10,"#DDD2C9");mediaArtist.VerticalAlignment=VerticalAlignment.Top;mediaArtist.TextTrimming=TextTrimming.CharacterEllipsis;info.Children.Add(mediaArtist);
  musicTitle=T("",24);musicTitle.FontWeight=FontWeights.SemiBold;musicTitle.TextWrapping=TextWrapping.Wrap;musicTitle.MaxHeight=61;musicTitle.VerticalAlignment=VerticalAlignment.Top;musicTitle.Margin=new Thickness(0,19,0,0);info.Children.Add(musicTitle);
  mediaHint=T("",10,"#C3B9B4");mediaHint.VerticalAlignment=VerticalAlignment.Top;mediaHint.Margin=new Thickness(0,83,0,0);mediaHint.TextTrimming=TextTrimming.CharacterEllipsis;info.Children.Add(mediaHint);
  var timeline=new Grid{Height=17,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,31)};timeline.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(35)});timeline.ColumnDefinitions.Add(new ColumnDefinition());timeline.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(40)});
  elapsed=T("0:00",9,"#DFD3CB");timeline.Children.Add(elapsed);
  systemSeek=new Slider{Minimum=0,Maximum=100,IsMoveToPointEnabled=true,VerticalAlignment=VerticalAlignment.Center,Height=15};Grid.SetColumn(systemSeek,1);timeline.Children.Add(systemSeek);
  systemSeek.Style=(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Slider'><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Slider'><Grid Background='Transparent'><Border Height='3' Background='#35FFFFFF' CornerRadius='2'/><Track x:Name='PART_Track'><Track.DecreaseRepeatButton><RepeatButton Command='Slider.DecreaseLarge' Background='#F0DACE' BorderThickness='0' Height='3'><RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Background='{TemplateBinding Background}' Height='3' CornerRadius='2'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton><Track.Thumb><Thumb Width='6' Height='6'><Thumb.Template><ControlTemplate><Ellipse Fill='#F5E4D8'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton Command='Slider.IncreaseLarge' Opacity='0' Height='10'/></Track.IncreaseRepeatButton></Track></Grid></ControlTemplate></Setter.Value></Setter></Style>");
  systemSeek.PreviewMouseDown+=(s,e)=>seeking=true;systemSeek.PreviewMouseUp+=(s,e)=>{SendMedia("seek",media.duration*systemSeek.Value/100);seeking=false;};
  systemSeek.LostMouseCapture+=(s,e)=>seeking=false;
  mediaDuration=T("−0:00",9,"#DFD3CB");mediaDuration.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetColumn(mediaDuration,2);timeline.Children.Add(mediaDuration);info.Children.Add(timeline);
  var transport=Stack(Orientation.Horizontal);transport.HorizontalAlignment=HorizontalAlignment.Center;transport.VerticalAlignment=VerticalAlignment.Bottom;
  systemPrevious=Btn("⏮",()=>SendMedia("previous"),"Anterior");transport.Children.Add(systemPrevious);
  systemPlay=Btn("▶",()=>SendMedia("toggle"),"Reproduzir / pausar");systemPlay.Background=B("#F9F2E9");systemPlay.Foreground=B("#382D28");systemPlay.Width=31;systemPlay.Height=31;systemPlay.Padding=new Thickness(0);transport.Children.Add(systemPlay);
  systemNext=Btn("⏭",()=>SendMedia("next"),"Próxima");transport.Children.Add(systemNext);info.Children.Add(transport);
  var options=Btn("+",()=>{useSystem=false;Choose();if(tracks.Length==0){useSystem=true;Render();}},"Abrir arquivos locais");options.HorizontalAlignment=HorizontalAlignment.Right;options.VerticalAlignment=VerticalAlignment.Bottom;info.Children.Add(options);
  lastCover="";UpdateSystemMusic();
 }
 static TextBlock mediaDuration;
 static void UpdateSystemMusic(){
  if(page!="music"||!useSystem||mediaArtist==null)return;
  musicTitle.Text=String.IsNullOrWhiteSpace(media.title)?"Sua música, aqui.":media.title;
  mediaArtist.Text=String.IsNullOrWhiteSpace(media.artist)?"MUSIC":media.artist;
  mediaHint.Text=media.available?(media.album??media.source??""):media.artist??"Abra uma música no Spotify ou navegador.";
  mediaHint.ToolTip=media.error;systemPlay.Content=media.playing?"Ⅱ":"▶";systemPlay.IsEnabled=media.available&&media.canToggle;systemNext.IsEnabled=media.available&&media.canNext;systemPrevious.IsEnabled=media.available&&media.canPrevious;systemSeek.IsEnabled=media.available&&media.canSeek&&media.duration>0;
  elapsed.Text=Format(media.position);mediaDuration.Text="−"+Format(media.duration-media.position);if(!seeking)systemSeek.Value=media.duration>0?Math.Max(0,Math.Min(100,media.position/media.duration*100)):0;
  string cover=media.available?(media.cover??""):"";if(cover!=lastCover){lastCover=cover;albumImage.Source=null;try{if(File.Exists(cover)){var bmp=new BitmapImage();bmp.BeginInit();bmp.CacheOption=BitmapCacheOption.OnLoad;bmp.DecodePixelWidth=320;bmp.UriSource=new Uri(cover);bmp.EndInit();bmp.Freeze();albumImage.Source=bmp;Tint(bmp);}}catch{}}
 }
 static void Tint(BitmapSource source){
  var small=new TransformedBitmap(source,new ScaleTransform(24.0/source.PixelWidth,24.0/source.PixelHeight));var rgba=new FormatConvertedBitmap(small,PixelFormats.Bgra32,null,0);int stride=rgba.PixelWidth*4;var pixels=new byte[stride*rgba.PixelHeight];rgba.CopyPixels(pixels,stride,0);long r=0,g=0,b=0,total=0;for(int i=0;i<pixels.Length;i+=4){int weight=Math.Max(1,Math.Max(pixels[i],Math.Max(pixels[i+1],pixels[i+2]))-Math.Min(pixels[i],Math.Min(pixels[i+1],pixels[i+2])));r+=pixels[i+2]*weight;g+=pixels[i+1]*weight;b+=pixels[i]*weight;total+=weight;}Color c=Color.FromArgb(110,(byte)(r/total),(byte)(g/total),(byte)(b/total));
  glass.Background=new LinearGradientBrush(new GradientStopCollection{new GradientStop(c,0),new GradientStop(Color.FromArgb(48,c.R,c.G,c.B),.5),new GradientStop(Color.FromArgb(105,67,48,70),1)},45);
 }
 static void LiveAgent(){
  var panel=Stack();view.Children.Add(panel);var title=T("✳  Claude Code",25);title.FontWeight=FontWeights.SemiBold;panel.Children.Add(title);
  agentStatus=T("Aguardando conexão",15,"#E2D1C4");agentStatus.Margin=new Thickness(0,10,0,5);panel.Children.Add(agentStatus);
  agentDetail=T("",12,"#BEB2AC");panel.Children.Add(agentDetail);agentAge=T("",10,"#ADA099");agentAge.Margin=new Thickness(0,4,0,0);panel.Children.Add(agentAge);
  var copy=Btn("Copiar comando de conexão",()=>{Clipboard.SetText("claude --settings \""+System.IO.Path.Combine(baseDir,"claude-settings.json")+"\"");agentStatus.Text="Comando copiado";agentDetail.Text="Execute no terminal do projeto para conectar esta sessão.";});copy.HorizontalAlignment=HorizontalAlignment.Left;copy.Margin=new Thickness(-10,10,0,0);panel.Children.Add(copy);UpdateAgent();UpdateDashboard();
 }
 static void UpdateAgent(){
  if(page!="agent"||agentStatus==null)return;
  string path=System.IO.Path.Combine(dataDir,"agent.json");if(!File.Exists(path)){agentDetail.Text="Conecte uma sessão para acompanhar eventos reais.";return;}
  try{var e=json.Deserialize<Dictionary<string,string>>(File.ReadAllText(path));string ev=e.ContainsKey("event")?e["event"]:"";string tool=e.ContainsKey("tool")?e["tool"]:"";
   agentStatus.Text=ev=="Stop"?"Concluído":ev=="Notification"?"Aguardando sua atenção":ev=="SessionEnd"?"Sessão encerrada":ev=="PreToolUse"?"Executando · "+tool:ev=="PostToolUse"?"Ferramenta concluída · "+tool:ev=="UserPromptSubmit"?"Trabalhando":"Sessão conectada";
   agentDetail.Text=(e.ContainsKey("project")?e["project"]:"")+(e.ContainsKey("file")?"  /  "+e["file"]:"");DateTime stamp;if(DateTime.TryParse(e["updated"],out stamp))agentAge.Text="Último evento às "+stamp.ToLocalTime().ToString("HH:mm:ss");
   if(lastAgent!=e["updated"]){if(ev=="Stop"&&lastAgent!="")if(!contextHidden)System.Media.SystemSounds.Asterisk.Play();lastAgent=e["updated"];}
  }catch{}
 }
 static void PollIntegrations(){
  if(++poll%4!=0)return;
  try{string path=System.IO.Path.Combine(dataDir,"media.json");if(File.Exists(path)){media=json.Deserialize<MediaState>(File.ReadAllText(path));DateTime stamp;if(!DateTime.TryParse(media.updated,out stamp)||DateTime.UtcNow-stamp.ToUniversalTime()>TimeSpan.FromSeconds(12)){media.available=false;media.playing=false;media.cover="";media.artist="A conexão de mídia foi interrompida.";}UpdateSystemMusic();}}
  catch{}

  UpdateAgent();UpdateDashboard();
 }
 static bool uiTest;
 static void StartMediaBridge(){
  Directory.CreateDirectory(dataDir);WriteHookSettings();string modelFile=System.IO.Path.Combine(dataDir,"model.txt");if(File.Exists(modelFile))localModel=File.ReadAllText(modelFile).Trim();leaveTimer.Tick+=(s,e)=>{leaveTimer.Stop();if(!w.IsMouseOver&&w.IsEnabled&&Mouse.Captured==null)Expand(false);};
  try{var info=new ProcessStartInfo("powershell.exe","-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \""+System.IO.Path.Combine(baseDir,"MediaBridge.ps1")+"\" -OwnerPid "+Process.GetCurrentProcess().Id){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};mediaWorker=Process.Start(info);}catch(Exception ex){media.error=ex.Message;}
 }
 static void WriteHookSettings(){
  var hooks=new Dictionary<string,object>();string command="powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \""+System.IO.Path.Combine(baseDir,"ClaudeHook.ps1").Replace("\\","/")+"\"";
  foreach(var ev in new[]{"SessionStart","SessionEnd","UserPromptSubmit","PreToolUse","PostToolUse","Notification","Stop"})hooks[ev]=new[]{new{matcher="",hooks=new[]{new{type="command",command=command,timeout=5}}}};
  File.WriteAllText(System.IO.Path.Combine(baseDir,"claude-settings.json"),json.Serialize(new{hooks=hooks}),new System.Text.UTF8Encoding(false));
 }
 public static void Test(){
  Directory.CreateDirectory(dataDir);if(Format(1500)!="25:00"||Format(-1)!="00:00"||Format(3600)!="60:00")throw new Exception("Timer roundtrip");Build();Switch("focus");start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));if(!running)throw new Exception("Timer start failed");start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));if(running||remaining<1490||remaining>1500)throw new Exception("Timer pause failed");remaining=1500;var saved=media;media=new MediaState{available=true};SendMedia("seek",42.5);var command=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(System.IO.Path.Combine(dataDir,"media-command.json")));if(Convert.ToDouble(command["position"])!=42.5)throw new Exception("Media command failed");media=saved;
  foreach(string p in new[]{"home","music","agent","focus","robo","settings"}){Switch(p);view.BeginAnimation(UIElement.OpacityProperty,null);view.Opacity=1;glass.Measure(new Size(600,270));glass.Arrange(new Rect(0,0,600,270));glass.UpdateLayout();var bmp=new RenderTargetBitmap(600,270,96,96,PixelFormats.Pbgra32);bmp.Render(glass);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(System.IO.Path.Combine(dataDir,"preview-"+p+".png")))enc.Save(f);}
  SetPose(false,false);if(w.ShowInTaskbar||w.Width!=128||w.Height!=34||glass.Visibility!=Visibility.Collapsed)throw new Exception("Mascot rest pose");restingShell.Measure(new Size(128,34));restingShell.Arrange(new Rect(0,0,128,34));restingShell.UpdateLayout();var restBitmap=new RenderTargetBitmap(128,34,96,96,PixelFormats.Pbgra32);restBitmap.Render(restingShell);var restEncoder=new PngBitmapEncoder();restEncoder.Frames.Add(BitmapFrame.Create(restBitmap));using(var f=File.Create(System.IO.Path.Combine(dataDir,"preview-rest.png")))restEncoder.Save(f);if(!DesktopContext.CoversMonitor(new DesktopContext.Rect{Left=0,Top=0,Right=1920,Bottom=1080},new DesktopContext.Rect{Left=0,Top=0,Right=1920,Bottom=1080})||DesktopContext.CoversMonitor(new DesktopContext.Rect{Left=0,Top=0,Right=1920,Bottom=1040},new DesktopContext.Rect{Left=0,Top=0,Right=1920,Bottom=1080}))throw new Exception("Fullscreen classification");WriteHookSettings();File.WriteAllText(System.IO.Path.Combine(dataDir,"test-result.txt"),"PASS: six panels, timer start/pause, media command, hook config, mascot 128x34, switcher disabled, fullscreen rectangle classification. "+DateTime.UtcNow.ToString("o"));w.Close();
 }
 [STAThread] public static void Main(string[] args){
  try{DesktopContext.SetCurrentProcessExplicitAppUserModelID("Ilha.Desktop");Environment.SetEnvironmentVariable("ILHA_DATA",dataDir);Directory.CreateDirectory(dataDir);if(args.Contains("--test")){Test();return;}bool created;using(var mutex=new Mutex(true,"Local\\IlhaGlassDesktopFinal",out created)){if(!created){try{using(var signal=System.Threading.EventWaitHandle.OpenExisting("Local\\IlhaGlassOpen")){signal.Set();}}catch{}return;}uiTest=args.Contains("--ui-test");Run();}}
  catch(Exception ex){try{File.WriteAllText(System.IO.Path.Combine(dataDir,"startup-error.txt"),ex.ToString());}catch{}Environment.ExitCode=1;if(!args.Contains("--test"))MessageBox.Show("Não foi possível abrir a Ilha.\n"+ex.Message,"Ilha Glass");}
 }
}
}
