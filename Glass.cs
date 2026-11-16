using System;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Markup;
using Forms=System.Windows.Forms;
namespace Ilha {
public partial class App {
 static Window w; static Border glass; static Grid view; static StackPanel nav; static TextBlock date,time,compact,timerText,finish,musicTitle,musicSub,elapsed; static Button start,play; static Slider seek; static DispatcherTimer ticker; static Forms.NotifyIcon tray;
 static string page="home"; static bool opened=true,running,playing,seeking; static double remaining=1500; static DateTime end; static MediaPlayer player=new MediaPlayer(); static string[] tracks=new string[0]; static int track; static string error="";
 static SolidColorBrush B(string s){return (SolidColorBrush)new BrushConverter().ConvertFromString(s);}
 static TextBlock T(string s,double size,string c="#FFF8F6"){return new TextBlock{Text=s,FontSize=size,Foreground=B(c),VerticalAlignment=VerticalAlignment.Center};}
 static Button Btn(string s,Action a,string tip=null){
  var b=new Button{Content=s,Style=(Style)w.FindResource("Quiet"),ToolTip=tip??s};
  b.Click+=(o,e)=>{try{a();}catch(Exception ex){error=ex.Message;ShowError();}};return b;
 }
 static void ShowError(){MessageBox.Show(w,error,"Ilha",MessageBoxButton.OK,MessageBoxImage.Information);}
 static StackPanel Stack(Orientation o=Orientation.Vertical){return new StackPanel{Orientation=o};}
 static string Format(double n){n=Math.Max(0,Math.Floor(n));return ((int)n/60).ToString("00")+":"+((int)n%60).ToString("00");}
 static void Spring(DependencyProperty property,double value,bool expanding){island.BeginAnimation(property,new DoubleAnimation(value,TimeSpan.FromMilliseconds(expanding?520:320)){EasingFunction=expanding?(IEasingFunction)new BackEase{Amplitude=.22,EasingMode=EasingMode.EaseOut}:new CubicEase{EasingMode=EasingMode.EaseInOut}});}
 static void Animate(DependencyObject target,DependencyProperty prop,double value){var a=new DoubleAnimation(value,TimeSpan.FromMilliseconds(360)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}}; ((IAnimatable)target).BeginAnimation(prop,a);}
 static void Expand(bool yes){SetPose(yes,true);}

 static void Switch(string p){if(page=="robo"&&chatInput!=null)chatDraft=chatInput.Text;page=p;Render();if(!opened)Expand(true);}
 static void Render(){
  ConfigureArtwork();
  view.Children.Clear();nav.Children.Clear();
  if(page=="focus")Focus();else if(page=="music")SystemMusic();else if(page=="agent")LiveAgent();else if(page=="robo"){Robo();if(chatInput!=null)chatInput.Text=chatDraft;}else if(page=="shelf")Shelf();else if(page=="audio")AudioPanel();else if(page=="settings")Settings();else if(page=="notes")Notes();else if(page=="clipboard")ClipboardPage();else if(page=="agenda")Agenda();else Dashboard();
  var links=new[]{new[]{"home","home","HOME"},new[]{"music","music","MÍDIA"},new[]{"focus","focus","FOCUS"},new[]{"agent","agent","CLAUDE"},new[]{"robo","robo","ROBO"},new[]{"agenda","agenda","AGENDA"},new[]{"notes","notes","NOTAS"},new[]{"clipboard","clipboard","COLAR"}};
  foreach(var link in links){string target=link[1];var b=Btn("",()=>{if(target=="music"){useSystem=true;player.Pause();playing=false;}Switch(target);},link[2]);b.Content=NavIcon(link[0],page==target?link[2]:null);if(page==target){b.Background=B("#22FFFFFF");b.Foreground=B("#FFFFFF");}nav.Children.Add(b);}

  view.Opacity=0;Animate(view,UIElement.OpacityProperty,1);
 }
 static void Focus(){
  var grid=new Grid();view.Children.Add(grid);
  timerText=T(Format(remaining),61);timerText.FontWeight=FontWeights.Light;System.Windows.Documents.Typography.SetNumeralAlignment(timerText,FontNumeralAlignment.Tabular);timerText.VerticalAlignment=VerticalAlignment.Top;timerText.Margin=new Thickness(0,-3,0,0);grid.Children.Add(timerText);
  finish=T("",11,"#CFC6C4");finish.VerticalAlignment=VerticalAlignment.Top;finish.Margin=new Thickness(2,70,0,0);grid.Children.Add(finish);
  var actions=Stack(Orientation.Horizontal);actions.HorizontalAlignment=HorizontalAlignment.Right;actions.VerticalAlignment=VerticalAlignment.Top;actions.Margin=new Thickness(0,15,0,0);
  start=Btn(running?"Pause":"Start",()=>{if(running){remaining=Math.Max(0,(end-DateTime.UtcNow).TotalSeconds);running=false;}else{if(remaining<=0)remaining=1500;end=DateTime.UtcNow.AddSeconds(remaining);running=true;}Tick();start.Content=running?"Pause":"Start";});start.Background=B("#FFF5F0");start.Foreground=B("#302725");start.Padding=new Thickness(20,10,20,10);actions.Children.Add(start);actions.Children.Add(Btn("↺",()=>{running=false;remaining=1500;Tick();start.Content="Start";},"Reiniciar"));grid.Children.Add(actions);
  rulerCanvas=new Canvas{Height=45,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(2,0,2,5)};
  var ruler=rulerCanvas;
  for(int i=0;i<=60;i++){var l=new Line{X1=i*9.0,X2=i*9.0,Y1=i%5==0?0:8,Y2=17,Stroke=B("#60FFFFFF"),StrokeThickness=1};ruler.Children.Add(l);if(i%10==0){var label=T(i.ToString(),9,"#AEAAA8");Canvas.SetLeft(label,i*9.0-4);Canvas.SetTop(label,25);ruler.Children.Add(label);} }
  ruler.ToolTip="Clique na régua para escolher de 1 a 60 minutos";ruler.Background=Brushes.Transparent;ruler.Cursor=Cursors.Hand;ruler.MouseLeftButtonDown+=(s,e)=>{running=false;remaining=Math.Max(1,Math.Min(60,Math.Round(e.GetPosition(ruler).X/9.0)))*60;Tick();start.Content="Start";};rulerMarker=new Line{X1=225,X2=225,Y1=-6,Y2=19,Stroke=B("#FFF2E5"),StrokeThickness=2};ruler.Children.Add(rulerMarker);grid.Children.Add(ruler);Tick();
 }
 static void LocalMusic(){
  var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(155)});grid.ColumnDefinitions.Add(new ColumnDefinition());view.Children.Add(grid);
  var art=new Grid{Width=132,Height=148,HorizontalAlignment=HorizontalAlignment.Left};
  var artBg=new Border{CornerRadius=new CornerRadius(16),Background=new LinearGradientBrush(B("#A9BF7768").Color,B("#B34E4B65").Color,55)};art.Children.Add(artBg);
  art.Children.Add(new Ellipse{Width=66,Height=66,Fill=B("#50F4CFBC"),VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,19,0,0)});
  var glyph=T("♫",32,"#CEFFF5EB");glyph.HorizontalAlignment=HorizontalAlignment.Center;glyph.VerticalAlignment=VerticalAlignment.Bottom;glyph.Margin=new Thickness(0,0,0,15);art.Children.Add(glyph);grid.Children.Add(art);
  var info=Stack();Grid.SetColumn(info,1);grid.Children.Add(info);
  musicSub=T(tracks.Length==0?"SUA BIBLIOTECA":"ARQUIVO LOCAL",10,"#D6CAC4");info.Children.Add(musicSub);
  musicTitle=T(tracks.Length==0?"Sua música,\nno topo da tela.":System.IO.Path.GetFileNameWithoutExtension(tracks[track]),23);musicTitle.TextWrapping=TextWrapping.Wrap;musicTitle.MaxHeight=62;musicTitle.FontWeight=FontWeights.SemiBold;musicTitle.Margin=new Thickness(0,6,0,6);info.Children.Add(musicTitle);
  elapsed=T("00:00 / 00:00",10,"#CDC4BF");info.Children.Add(elapsed);
  seek=new Slider{Minimum=0,Maximum=100,Height=15,Margin=new Thickness(0,3,0,2)};seek.PreviewMouseDown+=(s,e)=>seeking=true;seek.PreviewMouseUp+=(s,e)=>{if(player.NaturalDuration.HasTimeSpan)player.Position=TimeSpan.FromSeconds(player.NaturalDuration.TimeSpan.TotalSeconds*seek.Value/100);seeking=false;};info.Children.Add(seek);
  var controls=Stack(Orientation.Horizontal);controls.HorizontalAlignment=HorizontalAlignment.Center;controls.Children.Add(Btn("⏮",()=>Next(-1),"Anterior"));play=Btn(playing?"Ⅱ":"▶",()=>{if(tracks.Length==0){Choose();return;}if(playing)player.Pause();else player.Play();playing=!playing;play.Content=playing?"Ⅱ":"▶";},"Reproduzir / pausar");controls.Children.Add(play);controls.Children.Add(Btn("⏭",()=>Next(1),"Próxima"));controls.Children.Add(Btn("+",Choose,"Abrir músicas"));info.Children.Add(controls);
 }
 static void Choose(){var d=new Microsoft.Win32.OpenFileDialog{Title="Escolher músicas",Multiselect=true,Filter="Áudio|*.mp3;*.wav;*.wma;*.m4a;*.aac|Todos os arquivos|*.*"};if(d.ShowDialog(w)==true){tracks=d.FileNames;track=0;Load();}}
 static void Load(){useSystem=false;player.Open(new Uri(tracks[track]));player.Play();playing=true;Switch("music");}
 static void Next(int delta){if(tracks.Length==0){Choose();return;}track=(track+delta+tracks.Length)%tracks.Length;Load();}
 static void OldAgent(){
  var panel=Stack();view.Children.Add(panel);panel.Children.Add(T("✳  Claude Code",25));
  var sub=T("Integração ainda não conectada",12,"#D5C8C1");sub.Margin=new Thickness(0,9,0,12);panel.Children.Add(sub);
  var desc=T("O vídeo mostra o acompanhamento do agente.\nPara exibir tarefas reais, precisamos conectar os eventos\ndo Claude Code instalado neste computador.",13,"#C9BDB7");desc.LineHeight=21;panel.Children.Add(desc);
 }
 static void Home(){var panel=Stack();view.Children.Add(panel);var clock=T(DateTime.Now.ToString("HH:mm"),57);clock.FontWeight=FontWeights.Light;panel.Children.Add(clock);var p=Forms.SystemInformation.PowerStatus;string power=p.BatteryChargeStatus==Forms.BatteryChargeStatus.NoSystemBattery?"Computador conectado":String.Format("Bateria {0:0}%",p.BatteryLifePercent*100);panel.Children.Add(T(DateTime.Now.ToString("dddd, dd 'de' MMMM",new CultureInfo("pt-BR"))+"  ·  "+power,13,"#D0C5BF"));var actions=Stack(Orientation.Horizontal);actions.Margin=new Thickness(-8,12,0,0);actions.Children.Add(Btn("♫  Mídia",()=>Switch("music")));actions.Children.Add(Btn("◉  Começar foco",()=>Switch("focus")));panel.Children.Add(actions);}
 static void Tick(){if(date!=null)date.Text=DateTime.Now.ToString("ddd  dd  MMM",CultureInfo.InvariantCulture).ToUpper();if(time!=null)time.Text=DateTime.Now.ToString("h:mm tt",CultureInfo.InvariantCulture);if(running){remaining=Math.Max(0,(end-DateTime.UtcNow).TotalSeconds);if(remaining==0){running=false;if(!contextHidden&&!doNotDisturb){System.Media.SystemSounds.Asterisk.Play();Switch("focus");Expand(true);}if(start!=null)start.Content="Start";}}
  if(rulerMarker!=null){double x=Math.Max(0,Math.Min(60,remaining/60))*9;rulerMarker.X1=x;rulerMarker.X2=x;}if(timerText!=null)timerText.Text=Format(Math.Ceiling(remaining));if(finish!=null)finish.Text=running?"Done at "+DateTime.Now.AddSeconds(remaining).ToString("HH:mm"):remaining<=0?"Focus complete":"Done at "+DateTime.Now.AddSeconds(remaining).ToString("h:mm tt",CultureInfo.InvariantCulture);
  if(page=="music"&&useSystem){UpdateSystemMusic();UpdateLyrics();}if(page=="home")UpdateDashboard();
  if(compact!=null)compact.Text=running?"◉  "+Format(Math.Ceiling(remaining)):media.available?"♫  "+(media.playing?"Playing":"Paused"):playing?"♫  Reproduzindo":"●  ●";
  if(page=="music"&&!useSystem&&elapsed!=null&&player.NaturalDuration.HasTimeSpan){elapsed.Text=Format(player.Position.TotalSeconds)+" / "+Format(player.NaturalDuration.TimeSpan.TotalSeconds);if(!seeking&&player.NaturalDuration.TimeSpan.TotalSeconds>0)seek.Value=player.Position.TotalSeconds/player.NaturalDuration.TimeSpan.TotalSeconds*100;}
 }
 public static void LegacyTest(){if(Format(1500)!="25:00"||Format(-1)!="00:00")throw new Exception("Timer format");Build();Switch("music");Switch("agent");Switch("home");Switch("focus");w.Measure(new Size(560,284));w.Arrange(new Rect(0,0,560,284));var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(560,284,96,96,PixelFormats.Pbgra32);view.BeginAnimation(UIElement.OpacityProperty,null);view.Opacity=1;glass.Measure(new Size(560,284));glass.Arrange(new Rect(0,0,560,284));glass.UpdateLayout();bitmap.Render(glass);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));using(var f=File.Create(System.IO.Path.Combine(Environment.GetEnvironmentVariable("ILHA_DATA"),"preview.png")))encoder.Save(f);Console.WriteLine("Compilação, relógio e quatro painéis: OK");}
 static void Build(){
  w=new Window{Title="Ilha Glass",Width=640,Height=300,Top=0,Left=SystemParameters.WorkArea.Left+(SystemParameters.WorkArea.Width-640)/2,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=null,Topmost=true,ShowInTaskbar=false,ShowActivated=false,FontFamily=new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Inter")};
  w.Resources.Add("Quiet",(Style)XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Setter Property='Foreground' Value='#BBD7CDC7'/><Setter Property='Background' Value='Transparent'/><Setter Property='BorderThickness' Value='0'/><Setter Property='Padding' Value='11,7'/><Setter Property='Margin' Value='2,0'/><Setter Property='Cursor' Value='Hand'/><Setter Property='FontSize' Value='12'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border x:Name='b' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' CornerRadius='10' Background='{TemplateBinding Background}' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='b' Property='Background' Value='#35FFFFFF'/><Setter Property='Foreground' Value='White'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='b' Property='BorderBrush' Value='White'/><Setter TargetName='b' Property='BorderThickness' Value='1'/></Trigger><Trigger Property='IsPressed' Value='True'><Setter TargetName='b' Property='Opacity' Value='.65'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>"));
  glass=new Border{Margin=new Thickness(0),CornerRadius=new CornerRadius(24),Background=NeutralGlass(),Padding=new Thickness(0),Effect=null};windowHost=new Grid();island=new Grid{Width=128,Height=34,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,8,0,0),Effect=new System.Windows.Media.Effects.DropShadowEffect{BlurRadius=26,ShadowDepth=7,Direction=270,Opacity=.42,Color=Colors.Black}};island.SizeChanged+=(s,e)=>{double r=Math.Min(24,island.ActualHeight/2);island.Clip=new RectangleGeometry(new Rect(0,0,island.ActualWidth,island.ActualHeight),r,r);};backdropImage=new Image{Width=600,Height=270,Stretch=Stretch.Fill,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top,Effect=new System.Windows.Media.Effects.BlurEffect{Radius=26,KernelType=System.Windows.Media.Effects.KernelType.Gaussian}};island.Children.Add(backdropImage);island.Children.Add(glass);mascot=new MascotEyes{Width=36,Height=20,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};restingShell=new Border{Background=B("#FF050506"),Child=mascot,Visibility=Visibility.Collapsed};island.Children.Add(restingShell);windowHost.Children.Add(island);w.Content=windowHost;TextOptions.SetTextFormattingMode(w,TextFormattingMode.Ideal);TextOptions.SetTextRenderingMode(w,TextRenderingMode.Grayscale);RenderOptions.SetBitmapScalingMode(w,BitmapScalingMode.HighQuality);w.SourceInitialized+=(s,e)=>DesktopContext.HideFromSwitcher(w);
  panelSurface=new Grid{ClipToBounds=true};panelSurface.SizeChanged+=(s,e)=>panelSurface.Clip=new RectangleGeometry(new Rect(0,0,panelSurface.ActualWidth,panelSurface.ActualHeight),23,23);glass.Child=panelSurface;backgroundArtwork=new Image{Source=Landscape(),Stretch=Stretch.UniformToFill,Width=600,HorizontalAlignment=HorizontalAlignment.Left,Opacity=.85,RenderTransform=new TranslateTransform(-250,0)};panelSurface.Children.Add(backgroundArtwork);artworkShade=new Border{Background=new LinearGradientBrush(new GradientStopCollection{new GradientStop(Color.FromArgb(0,5,6,9),0),new GradientStop(Color.FromArgb(70,5,6,9),.30),new GradientStop(Color.FromArgb(245,7,7,10),.53),new GradientStop(Color.FromArgb(245,7,7,10),1)},0)};panelSurface.Children.Add(artworkShade);
  var root=new Grid{Margin=new Thickness(20,9,20,9)};root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});panelSurface.Children.Add(root);
  var header=new Grid{Height=23,Background=Brushes.Transparent,Cursor=Cursors.Hand};root.Children.Add(header);date=T(DateTime.Now.ToString("ddd  dd  MMM",CultureInfo.InvariantCulture).ToUpper(),9,"#CBBEB7");header.Children.Add(date);time=T("",10,"#DAD1CB");time.HorizontalAlignment=HorizontalAlignment.Right;time.Margin=new Thickness(0,0,32,0);header.Children.Add(time);headerMascot=new MascotEyes{Width=36,Height=20,HorizontalAlignment=HorizontalAlignment.Center,LayoutTransform=new ScaleTransform(.55,.55)};header.Children.Add(headerMascot);compact=T("●  ●",10,"#D3B086");compact.HorizontalAlignment=HorizontalAlignment.Center;compact.Visibility=Visibility.Collapsed;header.Children.Add(compact);var more=Btn("···",()=>Switch("settings"),"Settings");more.HorizontalAlignment=HorizontalAlignment.Right;more.Width=24;more.Padding=new Thickness(0);header.Children.Add(more);header.MouseLeftButtonUp+=(s,e)=>{if(!IsButtonSource(e.OriginalSource as DependencyObject))Expand(!opened);};
  view=new Grid{Margin=new Thickness(0,7,0,12)};Grid.SetRow(view,1);root.Children.Add(view);
  var footer=new Grid{Height=28};Grid.SetRow(footer,2);root.Children.Add(footer);nav=Stack(Orientation.Horizontal);nav.HorizontalAlignment=HorizontalAlignment.Center;footer.Children.Add(nav);Button add=null;add=Btn("+",()=>QuickMenu(add),"Ações rápidas");add.HorizontalAlignment=HorizontalAlignment.Left;add.Padding=new Thickness(3,2,3,2);footer.Children.Add(add);var settings=Btn("",()=>Switch("settings"),"Settings");settings.Content=NavIcon("settings",null);settings.HorizontalAlignment=HorizontalAlignment.Right;settings.Padding=new Thickness(3,2,3,2);footer.Children.Add(settings);
  w.MouseEnter+=(s,e)=>{leaveTimer.Stop();if(!opened&&!doNotDisturb)Expand(true);};w.MouseLeave+=(s,e)=>leaveTimer.Start();w.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape)Expand(false);};SetupExperience();Render();Tick();
 }
 static void Log(string text){try{File.AppendAllText(System.IO.Path.Combine(dataDir,"lifecycle.log"),text);}catch{}}
 public static void Run(){
  var application=new System.Windows.Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  Log("Build start\n");Build();Log("Build done\n");if(!secondary)StartMediaBridge();SetupPresence();if(!secondary&&!uiTest)SpawnOtherIslands();Log("Bridge started\n");player.MediaEnded+=(s,e)=>{playing=false;if(tracks.Length>1)Next(1);else if(play!=null)play.Content="▶";};player.MediaFailed+=(s,e)=>{playing=false;error="Não foi possível reproduzir este arquivo. "+e.ErrorException.Message;ShowError();};
  ticker=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};ticker.Tick+=(s,e)=>{if(!uiTest)PresenceTick();Tick();PollIntegrations();ExperienceTick();};ticker.Start();
  if(!secondary){tray=new Forms.NotifyIcon{Icon=new System.Drawing.Icon(System.IO.Path.Combine(baseDir,"Ilha.ico")),Text="Ilha · Robo",Visible=true};tray.MouseClick+=(s,e)=>{if(e.Button==Forms.MouseButtons.Left&&!contextHidden){Expand(true);w.Activate();}};
  var menu=new Forms.ContextMenuStrip();menu.Items.Add("Abrir Ilha",null,(s,e)=>{if(!contextHidden)Expand(true);});menu.Items.Add("Reiniciar / atualizar",null,(s,e)=>Restart());
  var quiet=new Forms.ToolStripMenuItem("Ocultar em jogos / tela cheia"){Checked=true,CheckOnClick=true};quiet.CheckedChanged+=(s,e)=>{autoHideContext=quiet.Checked;PresenceTick();};menu.Items.Add(quiet);
  var hideApp=new Forms.ToolStripMenuItem("Ocultar neste aplicativo");menu.Opening+=(s,e)=>hideApp.Text="Ocultar em "+(String.IsNullOrEmpty(DesktopContext.ForegroundApp)?"aplicativo atual":DesktopContext.ForegroundApp);hideApp.Click+=(s,e)=>HideCurrentApp();menu.Items.Add(hideApp);menu.Items.Add("Prateleira",null,(s,e)=>{Switch("shelf");Expand(true);});menu.Items.Add("Áudio",null,(s,e)=>{Switch("audio");Expand(true);});var dnd=new Forms.ToolStripMenuItem("Não incomodar"){CheckOnClick=true,Checked=doNotDisturb};dnd.CheckedChanged+=(s,e)=>doNotDisturb=dnd.Checked;menu.Items.Add(dnd);menu.Items.Add("Desligar Ilha",null,(s,e)=>w.Close());tray.ContextMenuStrip=menu;}
  w.Closed+=(s,e)=>{ticker.Stop();leaveTimer.Stop();player.Close();if(tray!=null)tray.Dispose();if(openSignal!=null)openSignal.Dispose();if(restartSignal!=null)restartSignal.Dispose();if(mediaWorker!=null&&!mediaWorker.HasExited)try{mediaWorker.Kill();}catch{}application.Shutdown();};Log("Show start\n");w.Loaded+=(s,e)=>{if(uiTest){RunLifecycleChecks();}else SetPose(false,false);};w.Show();application.Run();
 }
}
}
