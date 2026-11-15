using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
namespace Ilha {
public partial class App {
 static bool doNotDisturb,dropHover,compactWide;static int selectedMonitor;static TextBlock compactStatus;static bool focusWasRunning;static DateTime celebrateUntil;
 static MascotEyes headerMascot;static DateTime lastMascotInteraction=DateTime.UtcNow,gazeUntil,surprisedUntil;static Point lastGaze;static bool mediaWasPlaying;
 sealed class ShelfItem {public string Name,Text;public string[] Files;public BitmapSource Image;public DateTime Added=DateTime.UtcNow;}
 static List<ShelfItem> shelf=new List<ShelfItem>();const int ShelfLimit=12;
 static Rect MonitorArea(){var screens=System.Windows.Forms.Screen.AllScreens;var s=screens[Math.Max(0,Math.Min(selectedMonitor,screens.Length-1))];var a=s.WorkingArea;var source=PresentationSource.FromVisual(w);var transform=source==null?Matrix.Identity:source.CompositionTarget.TransformFromDevice;var origin=transform.Transform(new Point(a.Left,a.Top));var size=transform.Transform(new Vector(a.Width,a.Height));return new Rect(origin.X,origin.Y,size.X,size.Y);}
 static void SetupExperience(){
  w.PreviewMouseMove+=(s,e)=>lastMascotInteraction=DateTime.UtcNow;w.PreviewKeyDown+=(s,e)=>lastMascotInteraction=DateTime.UtcNow;
  try{string path=Path.Combine(dataDir,"experience.json");if(File.Exists(path)){var prefs=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(path));if(prefs.ContainsKey("monitor"))selectedMonitor=Convert.ToInt32(prefs["monitor"]);if(prefs.ContainsKey("quiet"))doNotDisturb=Convert.ToBoolean(prefs["quiet"]);}string source=Path.Combine(dataDir,"media-source.txt");if(File.Exists(source))selectedSource=File.ReadAllText(source).Trim();}catch{}
  w.Closed+=(s,e)=>File.WriteAllText(Path.Combine(dataDir,"experience.json"),json.Serialize(new{monitor=selectedMonitor,quiet=doNotDisturb}));
  var face=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
  mascot.HorizontalAlignment=HorizontalAlignment.Center;restingShell.Child=null;face.Children.Add(mascot);compactStatus=T("",12);compactStatus.Margin=new Thickness(8,0,0,0);compactStatus.Visibility=Visibility.Collapsed;face.Children.Add(compactStatus);restingShell.Child=face;
  w.AllowDrop=true;w.PreviewDragOver+=(s,e)=>{bool valid=e.Data.GetDataPresent(DataFormats.FileDrop)||e.Data.GetDataPresent(DataFormats.UnicodeText)||e.Data.GetDataPresent(DataFormats.Bitmap);e.Effects=valid?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;if(valid){dropHover=true;leaveTimer.Stop();if(page!="shelf")Switch("shelf");if(!opened)Expand(true);}};
  w.DragLeave+=(s,e)=>{dropHover=false;leaveTimer.Start();};w.Drop+=(s,e)=>{dropHover=false;ReceiveDrop(e.Data);Switch("shelf");e.Handled=true;};
  restingShell.MouseLeftButtonUp+=(s,e)=>Expand(true);
 }
 static void ReceiveDrop(IDataObject data){
  var item=new ShelfItem();
  if(data.GetDataPresent(DataFormats.FileDrop)){item.Files=(string[])data.GetData(DataFormats.FileDrop);if(item.Files==null||item.Files.Length==0)return;item.Name=item.Files.Length==1?Path.GetFileName(item.Files[0]):item.Files.Length+" arquivos";}
  else if(data.GetDataPresent(DataFormats.Bitmap)){var bitmap=data.GetData(DataFormats.Bitmap) as BitmapSource;if(bitmap==null)return;if((long)bitmap.PixelWidth*bitmap.PixelHeight>32000000){error="Imagem muito grande. Arraste o arquivo da imagem.";ShowError();return;}item.Image=bitmap.Clone();item.Image.Freeze();item.Name="Imagem";}
  else if(data.GetDataPresent(DataFormats.UnicodeText)){item.Text=Convert.ToString(data.GetData(DataFormats.UnicodeText));if(String.IsNullOrWhiteSpace(item.Text))return;if(item.Text.Length>1000000){error="Texto muito grande para a prateleira.";ShowError();return;}item.Name=item.Text.Replace("\r"," ").Replace("\n"," ");if(item.Name.Length>48)item.Name=item.Name.Substring(0,48)+"…";}else return;
  shelf.Insert(0,item);if(shelf.Count>ShelfLimit)shelf.RemoveAt(shelf.Count-1);lastMascotInteraction=DateTime.UtcNow;surprisedUntil=DateTime.UtcNow.AddMilliseconds(450);celebrateUntil=DateTime.UtcNow.AddSeconds(2.5);
 }
 static DataObject ShelfData(ShelfItem item){var d=new DataObject();if(item.Files!=null){d.SetData(DataFormats.FileDrop,item.Files);if(item.Files.Length==1&&new[]{".png",".jpg",".jpeg",".bmp",".gif"}.Contains(Path.GetExtension(item.Files[0]).ToLowerInvariant()))try{var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;using(var metadata=System.Drawing.Image.FromFile(item.Files[0])){if(metadata.Width>4096)image.DecodePixelWidth=4096;}image.UriSource=new Uri(item.Files[0]);image.EndInit();image.Freeze();d.SetData(DataFormats.Bitmap,image);}catch{}}else if(item.Image!=null)d.SetData(DataFormats.Bitmap,item.Image);else d.SetText(item.Text);return d;}
 static void Shelf(){
  var root=new Grid();root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());view.Children.Add(root);
  var header=Stack(Orientation.Horizontal);header.Children.Add(T("Prateleira temporária",18));header.Children.Add(Btn("Limpar",()=>{shelf.Clear();Switch("shelf");}));root.Children.Add(header);
  var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(0,8,0,0)};Grid.SetRow(scroll,1);root.Children.Add(scroll);var rows=Stack();scroll.Content=rows;
  if(shelf.Count==0){rows.Children.Add(T("Arraste imagens, arquivos ou textos para cá.",14));rows.Children.Add(T("Até 12 itens · expiram em 1 hora · nada é enviado",11,"#BDBDC5"));}
  foreach(var item in shelf.ToArray()){var captured=item;var row=Stack(Orientation.Horizontal);var copy=Btn(item.Name,()=>{Clipboard.SetDataObject(ShelfData(captured),true);celebrateUntil=DateTime.UtcNow.AddSeconds(2);},"Copiar para colar em outro aplicativo");copy.MaxWidth=440;copy.PreviewMouseMove+=(s,e)=>{if(e.LeftButton==MouseButtonState.Pressed&&e.GetPosition(copy).Y>copy.ActualHeight)DragDrop.DoDragDrop(copy,ShelfData(captured),DragDropEffects.Copy);};row.Children.Add(copy);row.Children.Add(Btn("×",()=>{shelf.Remove(captured);Switch("shelf");},"Remover"));rows.Children.Add(row);}
 }
 static void AudioPanel(){
  var root=Stack();view.Children.Add(root);root.Children.Add(T("Áudio",23));
  try{var outputs=DesktopAudio.Outputs();var picker=new ComboBox{ItemsSource=outputs,SelectedItem=outputs.FirstOrDefault(x=>x.Current),Margin=new Thickness(0,10,0,8),MaxWidth=500,HorizontalAlignment=HorizontalAlignment.Left};picker.SelectionChanged+=(s,e)=>{var output=picker.SelectedItem as DesktopAudio.Output;if(output!=null)try{DesktopAudio.Select(output.Id);}catch(Exception ex){error="Não foi possível trocar a saída. "+ex.Message;ShowError();}};root.Children.Add(picker);float volume=DesktopAudio.Volume(null);var label=T("Volume · "+Math.Round(volume*100)+"%",13);root.Children.Add(label);var slider=new Slider{Minimum=0,Maximum=100,Value=volume*100,Margin=new Thickness(0,10,0,0),Width=470,HorizontalAlignment=HorizontalAlignment.Left};slider.ValueChanged+=(s,e)=>{try{float actual=DesktopAudio.Volume((float)(slider.Value/100));label.Text="Volume · "+Math.Round(actual*100)+"%";}catch(Exception ex){label.Text="Áudio indisponível: "+ex.Message;}};root.Children.Add(slider);}
  catch(Exception ex){root.Children.Add(T("Áudio indisponível: "+ex.Message,12));}
 }
 static void SourceMenu(){var menu=new ContextMenu();var automatic=new MenuItem{Header="Automático",IsCheckable=true,IsChecked=String.IsNullOrEmpty(selectedSource)};automatic.Click+=(s,e)=>SelectSource("");menu.Items.Add(automatic);foreach(var source in media.sessions??new MediaSource[0]){var entry=source;var item=new MenuItem{Header=(entry.playing?"▶ ":"")+(entry.title??"Mídia")+" · "+entry.source,IsCheckable=true,IsChecked=selectedSource==entry.source};item.Click+=(s,e)=>SelectSource(entry.source);menu.Items.Add(item);}menu.IsOpen=true;}
 static string selectedSource="";static string chatDraft="";
 static void SelectSource(string source){selectedSource=source;File.WriteAllText(Path.Combine(dataDir,"media-source.txt"),source);}
 static void ExperienceTick(){
  if(focusWasRunning&&!running&&remaining<=0)celebrateUntil=DateTime.UtcNow.AddSeconds(8);focusWasRunning=running;
  bool removed=shelf.RemoveAll(x=>DateTime.UtcNow-x.Added>TimeSpan.FromHours(1))>0;if(removed&&page=="shelf")Switch("shelf");
  if(mediaWasPlaying&&!media.playing)celebrateUntil=DateTime.UtcNow.AddSeconds(1);mediaWasPlaying=media.playing;
  string mood=dropHover?"curious":DateTime.UtcNow<surprisedUntil?"surprised":DateTime.UtcNow<celebrateUntil?"happy":running?"focus":media.playing?"music":DateTime.UtcNow-lastMascotInteraction>TimeSpan.FromSeconds(60)?"sleepy":"idle";
  if(mascot!=null){mascot.Mood=mood;mascot.InvalidateVisual();}if(headerMascot!=null)headerMascot.Mood=mood;
  if(compactStatus!=null){string text=running?Format(remaining):media.playing?"▂ ▅ ▃":"";compactStatus.Text=text;compactStatus.Foreground=running?B("#BC83FF"):Brushes.White;compactStatus.Visibility=text.Length>0?Visibility.Visible:Visibility.Collapsed;bool wide=text.Length>0;if(compactWide!=wide){compactWide=wide;if(!opened)SetPose(false,true);}}
 }
 static void CheckExperience(){
  var expressions=new HashSet<string>();foreach(string mood in new[]{"idle","curious","happy","focus","sleepy","surprised"}){var eyes=new MascotEyes{Width=36,Height=20,Mood=mood,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};eyes.BeginAnimation(MascotEyes.BlendProperty,null);eyes.Blend=1;var shell=new Border{Width=128,Height=34,Background=Brushes.Black,CornerRadius=new CornerRadius(17),Child=eyes};shell.Measure(new Size(128,34));shell.Arrange(new Rect(0,0,128,34));shell.UpdateLayout();var bitmap=new RenderTargetBitmap(128,34,96,96,PixelFormats.Pbgra32);bitmap.Render(shell);var pixels=new byte[128*34*4];bitmap.CopyPixels(pixels,128*4,0);using(var sha=System.Security.Cryptography.SHA256.Create())expressions.Add(Convert.ToBase64String(sha.ComputeHash(pixels)));SavePng(bitmap,"preview-mascot-"+mood+".png");}if(expressions.Count!=6)throw new Exception("Mascot expressions must be visually distinct");
  shelf.Clear();var text=new DataObject();text.SetText("Um texto temporário");ReceiveDrop(text);if(shelf.Count!=1||shelf[0].Text!="Um texto temporário")throw new Exception("Shelf text drop");
  var files=new DataObject(DataFormats.FileDrop,new[]{"C:\\sample.png"});ReceiveDrop(files);if(shelf[0].Files[0]!="C:\\sample.png")throw new Exception("Shelf file reference");
  for(int i=0;i<15;i++){var d=new DataObject();d.SetText("Item "+i);ReceiveDrop(d);}if(shelf.Count!=ShelfLimit||shelf[0].Text!="Item 14")throw new Exception("Shelf capacity/order");
  if(ShelfData(shelf[0]).GetText()!="Item 14")throw new Exception("Shelf clipboard payload");shelf.Clear();celebrateUntil=DateTime.MinValue;
  bool inter=false;foreach(var face in w.FontFamily.GetTypefaces()){GlyphTypeface glyph;if(face.TryGetGlyphTypeface(out glyph)&&glyph.FamilyNames.Values.Any(x=>x.IndexOf("Inter",StringComparison.OrdinalIgnoreCase)>=0)){inter=true;break;}}if(!inter)throw new Exception("Embedded Inter font did not resolve");
  foreach(string panel in new[]{"shelf","audio"}){Switch(panel);Capture(panel);}Switch("home");
 }
}
}
