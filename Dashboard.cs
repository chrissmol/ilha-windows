using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace Ilha {
public partial class App {
 static TextBlock dashTrack,dashArtist,dashTimer,dashReady,dashAgent,dashCount;
 static Image dashImage;static string dashCover="";
 static Button dashPlay,dashStart;static ScrollViewer chatScroll;static StackPanel chatMessages;static TextBox chatInput;static bool chatBusy;
 static List<Dictionary<string,string>> conversation=new List<Dictionary<string,string>>();
 static string localModel="";static TextBlock modelStatus;static ComboBox models;
 static Border Card(string top,string bottom,double width){return new Border{CornerRadius=new CornerRadius(12),Background=new LinearGradientBrush(B(top).Color,B(bottom).Color,90),Width=width,Padding=new Thickness(12),Cursor=Cursors.Hand};}
 static void Dashboard(){
  var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(110)});grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(110)});view.Children.Add(grid);
  var agent=Card("#FFFF6B1A","#FFFFA63D",104);agent.HorizontalAlignment=HorizontalAlignment.Left;agent.MouseLeftButtonUp+=(s,e)=>Switch("agent");
  var a=new Grid();agent.Child=a;a.Children.Add(new TextBlock{Text="CLAUDE",FontSize=9,Foreground=B("#E6E0D7"),VerticalAlignment=VerticalAlignment.Top});
  var info=Stack();info.VerticalAlignment=VerticalAlignment.Bottom;dashCount=T("—",29);info.Children.Add(dashCount);dashAgent=T("Sem sessão",9,"#F0D2BE");info.Children.Add(dashAgent);a.Children.Add(info);grid.Children.Add(agent);
  var music=Card("#FF2BB0FF","#FF2146E0",Double.NaN);music.Margin=new Thickness(4,0,4,0);music.Padding=new Thickness(0);music.ClipToBounds=true;music.SizeChanged+=(s,e)=>music.Clip=new RectangleGeometry(new Rect(0,0,music.ActualWidth,music.ActualHeight),12,12);Grid.SetColumn(music,1);grid.Children.Add(music);
  var m=new Grid();music.Child=m;var moon=new Ellipse{Width=80,Height=80,Fill=B("#BEE7E4D8"),VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,-10,32,0)};m.Children.Add(moon);
  m.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse("M0,90 L65,43 120,80 185,20 270,73 330,43 340,160 0,160 Z"),Fill=B("#70202B50"),Stretch=Stretch.Fill,Height=85,VerticalAlignment=VerticalAlignment.Bottom});
  dashImage=new Image{Stretch=Stretch.UniformToFill};m.Children.Add(dashImage);m.Children.Add(new Border{Background=new LinearGradientBrush(new GradientStopCollection{new GradientStop(Colors.Transparent,0),new GradientStop(Color.FromArgb(215,7,12,24),1)},90)});
  var text=Stack();text.VerticalAlignment=VerticalAlignment.Bottom;text.Margin=new Thickness(14,0,48,13);dashTrack=T("Nenhuma mídia",20);dashTrack.FontWeight=FontWeights.SemiBold;dashTrack.TextTrimming=TextTrimming.CharacterEllipsis;text.Children.Add(dashTrack);dashArtist=T("Abra seu player",10,"#E7E4E1");dashArtist.Margin=new Thickness(0,3,0,0);text.Children.Add(dashArtist);m.Children.Add(text);
  dashPlay=Btn("▶",()=>{if(media.available)SendMedia("toggle");else Switch("music");},"Reproduzir / pausar");RoundTransport(dashPlay);dashPlay.Width=30;dashPlay.Height=30;dashPlay.Padding=new Thickness(0);dashPlay.Background=B("#F5F1EB");dashPlay.Foreground=B("#25212B");dashPlay.HorizontalAlignment=HorizontalAlignment.Right;dashPlay.VerticalAlignment=VerticalAlignment.Bottom;dashPlay.Margin=new Thickness(0,0,13,14);m.Children.Add(dashPlay);
  music.MouseLeftButtonUp+=(s,e)=>{if(!(e.OriginalSource is Button)&&!IsButtonSource(e.OriginalSource as DependencyObject))Switch("music");};
  var focus=Card("#FF7B3DFF","#FFC05CFF",104);focus.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetColumn(focus,2);grid.Children.Add(focus);var f=new Grid();focus.Child=f;var label=T("FOCUS",9,"#E8DFF8");label.VerticalAlignment=VerticalAlignment.Top;f.Children.Add(label);
  var timer=Stack();timer.VerticalAlignment=VerticalAlignment.Bottom;timer.Margin=new Thickness(0,0,0,2);dashTimer=T("25:00",24);System.Windows.Documents.Typography.SetNumeralAlignment(dashTimer,FontNumeralAlignment.Tabular);timer.Children.Add(dashTimer);dashReady=T("Ready",9,"#D7CCF1");dashReady.Margin=new Thickness(0,4,0,0);timer.Children.Add(dashReady);f.Children.Add(timer);
  dashStart=Btn("▶",()=>{if(running){remaining=Math.Max(0,(end-DateTime.UtcNow).TotalSeconds);running=false;}else{if(remaining<=0)remaining=1500;end=DateTime.UtcNow.AddSeconds(remaining);running=true;}UpdateDashboard();},"Iniciar / pausar foco");RoundTransport(dashStart);dashStart.Width=23;dashStart.Height=23;dashStart.Padding=new Thickness(0);dashStart.HorizontalAlignment=HorizontalAlignment.Right;dashStart.VerticalAlignment=VerticalAlignment.Bottom;dashStart.Margin=new Thickness(0,-2,-4,0);dashStart.Foreground=B("#51467B");dashStart.Background=B("#F5F1EB");f.Children.Add(dashStart);
  focus.MouseLeftButtonUp+=(s,e)=>{if(!IsButtonSource(e.OriginalSource as DependencyObject))Switch("focus");};dashCover="";UpdateDashboard();
 }
 static bool IsButtonSource(DependencyObject o){while(o!=null){if(o is Button)return true;try{o=VisualTreeHelper.GetParent(o);}catch{return false;}}return false;}
 static void UpdateDashboard(){
  if(page!="home"||dashTrack==null)return;
  dashTrack.Text=media.available&&!String.IsNullOrWhiteSpace(media.title)?media.title:"Nenhuma mídia";dashArtist.Text=media.available?(media.artist??""):"Abra seu player";dashPlay.Content=TransportIcon(media.playing?"pause":"play",true);dashTimer.Text=Format(Math.Ceiling(remaining));dashReady.Text=running?"Running":"Ready";dashStart.Content=TransportIcon(running?"pause":"play",true);
  string cover=media.available?(media.cover??""):"";if(cover!=dashCover){dashCover=cover;dashImage.Source=null;try{if(File.Exists(cover)){var bmp=new BitmapImage();bmp.BeginInit();bmp.CacheOption=BitmapCacheOption.OnLoad;bmp.DecodePixelWidth=1200;bmp.UriSource=new Uri(cover);bmp.EndInit();bmp.Freeze();dashImage.Source=bmp;Tint(bmp);}}catch{}}
  try{string path=System.IO.Path.Combine(dataDir,"agent.json");if(File.Exists(path)){var e=json.Deserialize<Dictionary<string,string>>(File.ReadAllText(path));dashAgent.Text=e["event"]=="Stop"?"Concluído":e["event"]=="SessionEnd"?"Encerrado":"Conectado";dashCount.Text=e["event"]=="SessionEnd"?"0":"1";}else{dashCount.Text="0";dashAgent.Text="Sem sessão";}}catch{}
 }
 static void Robo(){
  var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});view.Children.Add(layout);
  var chatHeader=new DockPanel{Margin=new Thickness(0,0,0,4)};chatHeader.Children.Add(T(String.IsNullOrEmpty(localModel)?"ROBO":"via "+localModel,9,"#AFA9B4"));var reset=Btn("New chat",()=>{if(!chatBusy){conversation.Clear();Switch("robo");}},"Nova conversa");reset.HorizontalAlignment=HorizontalAlignment.Right;reset.Padding=new Thickness(6,1,6,1);chatHeader.Children.Add(reset);layout.Children.Add(chatHeader);
  chatMessages=Stack();chatScroll=new ScrollViewer{Content=chatMessages,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetRow(chatScroll,1);layout.Children.Add(chatScroll);
  if(conversation.Count==0){chatMessages.Children.Add(T("ROBO",9,"#C1B5CC"));var welcome=T("O que falta para hoje?",21);welcome.FontWeight=FontWeights.SemiBold;welcome.Margin=new Thickness(0,13,0,7);chatMessages.Children.Add(welcome);chatMessages.Children.Add(T(String.IsNullOrEmpty(localModel)?"Conecte uma IA local em Settings para conversar.":"IA local conectada · "+localModel,11,"#BDB4C7"));}else foreach(var message in conversation)AddChatBubble(message["content"],message["role"]=="user");
  var entry=new Grid{Margin=new Thickness(0,9,0,0)};entry.ColumnDefinitions.Add(new ColumnDefinition());entry.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});Grid.SetRow(entry,2);layout.Children.Add(entry);
  chatInput=new TextBox{Background=Brushes.Transparent,Foreground=B("#F2ECE7"),BorderBrush=B("#25FFFFFF"),BorderThickness=new Thickness(0,0,0,1),Padding=new Thickness(0,5,8,5),FontSize=12,ToolTip="Mensagem para o Robo"};chatInput.KeyDown+=(s,e)=>{if(e.Key==Key.Enter){SendChat();e.Handled=true;}};entry.Children.Add(chatInput);
  var send=Btn("↑",SendChat,"Enviar");send.Width=29;send.Height=29;send.Padding=new Thickness(0);send.Background=B("#F5ECEA");send.Foreground=B("#28202E");Grid.SetColumn(send,1);entry.Children.Add(send);
 }
 static void AddChatBubble(string message,bool user){
  if(chatMessages==null)return;var text=T(message,12,user?"#EEE8E6":"#D9D0DF");text.TextWrapping=TextWrapping.Wrap;
  var border=new Border{Child=text,CornerRadius=new CornerRadius(10),Padding=new Thickness(10,6,10,6),Margin=new Thickness(0,3,0,3),MaxWidth=450,Background=user?B("#25FFFFFF"):Brushes.Transparent,HorizontalAlignment=user?HorizontalAlignment.Right:HorizontalAlignment.Left};chatMessages.Children.Add(border);if(chatScroll!=null)chatScroll.ScrollToEnd();
 }
 static string LocalRequest(string route,string payload=null){
  var req=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:11434/api/"+route);req.Timeout=payload==null?2000:60000;req.ReadWriteTimeout=req.Timeout;req.Proxy=null;
  if(payload!=null){req.Method="POST";req.ContentType="application/json";byte[] b=System.Text.Encoding.UTF8.GetBytes(payload);req.ContentLength=b.Length;using(var stream=req.GetRequestStream())stream.Write(b,0,b.Length);}
  using(var response=req.GetResponse())using(var reader=new StreamReader(response.GetResponseStream()))return reader.ReadToEnd();
 }
 static async void SendChat(){
  if(chatBusy||chatInput==null||String.IsNullOrWhiteSpace(chatInput.Text))return;
  if(String.IsNullOrEmpty(localModel)){Switch("settings");return;}
  string message=chatInput.Text;chatInput.Clear();conversation.Add(new Dictionary<string,string>{{"role","user"},{"content",message}});AddChatBubble(message,true);chatBusy=true;
  try{var context=new List<Dictionary<string,string>>{new Dictionary<string,string>{{"role","system"},{"content","Você é Robo, um assistente de desktop. Responda em português de forma breve. Não invente acesso a tarefas, calendário ou arquivos. O timer está "+(running?"em andamento":"parado")+" e mostra "+Format(remaining)+". Você só conhece o que o usuário escreveu nesta conversa."}}};context.AddRange(conversation);
   string payload=json.Serialize(new{model=localModel,messages=context,stream=false});string raw=await Task.Run(()=>LocalRequest("chat",payload));var response=json.Deserialize<Dictionary<string,object>>(raw);var answer=response["message"] as Dictionary<string,object>;string content=Convert.ToString(answer["content"]);conversation.Add(new Dictionary<string,string>{{"role","assistant"},{"content",content}});if(page=="robo")AddChatBubble(content,false);
  }catch{if(page=="robo")AddChatBubble("Não consegui conectar à IA local. Confira o Ollama em Settings.",false);}finally{chatBusy=false;}
 }
 static void Settings(){
  var settings=Stack();var scroll=new ScrollViewer{Content=settings,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};view.Children.Add(scroll);settings.Children.Add(T("Settings",23));
  var controls=Stack(Orientation.Horizontal);controls.Children.Add(Btn(doNotDisturb?"Não incomodar · ativo":"Não incomodar",()=>{doNotDisturb=!doNotDisturb;Switch("settings");}));controls.Children.Add(Btn("Prateleira",()=>Switch("shelf")));controls.Children.Add(Btn("Áudio",()=>Switch("audio")));settings.Children.Add(controls);
  var monitors=T("Uma Ilha em cada monitor · some só onde há tela cheia",10,"#AFA9B4");monitors.Margin=new Thickness(0,7,0,7);settings.Children.Add(monitors);
  var desc=T("Robo · IA local",12,"#E3D9E8");desc.Margin=new Thickness(0,8,0,5);settings.Children.Add(desc);
  var row=Stack(Orientation.Horizontal);models=new ComboBox{Width=240,Height=27,Margin=new Thickness(0,0,6,0)};if(!String.IsNullOrEmpty(localModel)){models.Items.Add(localModel);models.SelectedIndex=0;}models.SelectionChanged+=(s,e)=>{if(models.SelectedItem!=null){localModel=models.SelectedItem.ToString();File.WriteAllText(System.IO.Path.Combine(dataDir,"model.txt"),localModel);}};row.Children.Add(models);row.Children.Add(Btn("Detectar modelos",DetectModels));settings.Children.Add(row);
  modelStatus=T("Requer Ollama em execução neste PC. Conexão com localhost.",10,"#B9ADB9");modelStatus.TextWrapping=TextWrapping.Wrap;modelStatus.Margin=new Thickness(0,6,0,7);settings.Children.Add(modelStatus);
  var connection=T("Mídia · "+(media.available?"conectada ao Windows":String.IsNullOrEmpty(media.error)?"aguardando player":media.error.Trim()),10,"#B9ADB9");connection.TextWrapping=TextWrapping.Wrap;settings.Children.Add(connection);
  var actions=Stack(Orientation.Horizontal);actions.Margin=new Thickness(-10,6,0,0);actions.Children.Add(Btn("Conectar Claude",()=>Switch("agent")));actions.Children.Add(Btn("Voltar ao player Windows",()=>{useSystem=true;player.Pause();playing=false;Switch("music");}));settings.Children.Add(actions);
 }
 static async void DetectModels(){
  modelStatus.Text="Procurando modelos locais…";
  try{string raw=await Task.Run(()=>LocalRequest("tags"));var data=json.Deserialize<Dictionary<string,object>>(raw);var entries=(System.Collections.IEnumerable)data["models"];models.Items.Clear();foreach(Dictionary<string,object> entry in entries)models.Items.Add(Convert.ToString(entry["name"]));if(models.Items.Count>0){models.SelectedIndex=0;modelStatus.Text="IA local conectada. Abra Robo para conversar.";}else modelStatus.Text="Ollama está ativo, mas não há modelos instalados.";}
  catch{if(modelStatus!=null)modelStatus.Text="Ollama não encontrado. Instale e abra o Ollama para conectar uma IA local.";}
 }
}
}
