using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace Ilha {
public partial class App {
 // Settings: grouped cards with switches and link rows.
 static Border CardBox(UIElement child){return new Border{Background=B("#16FFFFFF"),CornerRadius=new CornerRadius(14),Padding=new Thickness(4),Margin=new Thickness(0,0,0,6),Child=child};}
 static Border RowBox(UIElement child,Action click){
  var row=new Border{Height=26,CornerRadius=new CornerRadius(9),Background=Brushes.Transparent,Cursor=System.Windows.Input.Cursors.Hand,Child=child};
  row.MouseEnter+=(s,e)=>row.Background=B("#14FFFFFF");row.MouseLeave+=(s,e)=>row.Background=Brushes.Transparent;
  row.MouseLeftButtonUp+=(s,e)=>{try{click();}catch(Exception ex){error=ex.Message;ShowError();}};return row;
 }
 static UIElement ToggleRow(string label,bool on,Action<bool> set){
  var grid=new Grid();grid.Children.Add(new TextBlock{Text=label,Foreground=B("#FFF8F6"),FontSize=12,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,0,0)});
  var knob=new System.Windows.Shapes.Ellipse{Width=16,Height=16,Fill=Brushes.White,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(2),RenderTransform=new TranslateTransform(on?14:0,0)};
  var track=new Border{Width=34,Height=20,CornerRadius=new CornerRadius(10),HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,0,6,0),Background=B(on?"#30D158":"#4A4A50"),Child=knob};grid.Children.Add(track);
  bool state=on;
  return RowBox(grid,()=>{state=!state;set(state);track.Background=B(state?"#30D158":"#4A4A50");((TranslateTransform)knob.RenderTransform).BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(state?14:0,TimeSpan.FromMilliseconds(180)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});});
 }
 static UIElement LinkRow(string label,Action go){
  var grid=new Grid();grid.Children.Add(new TextBlock{Text=label,Foreground=B("#FFF8F6"),FontSize=12,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,0,0)});
  grid.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse("M0,0 L4,4 0,8"),Stroke=B("#80FFFFFF"),StrokeThickness=1.5,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,12,0)});
  return RowBox(grid,go);
 }
 static Border PillButton(string label,Action go){
  var pill=new Border{Background=B("#0A84FF"),CornerRadius=new CornerRadius(13),Padding=new Thickness(14,5,14,5),Margin=new Thickness(4,0,4,0),HorizontalAlignment=HorizontalAlignment.Left,Cursor=System.Windows.Input.Cursors.Hand,Child=new TextBlock{Text=label,Foreground=Brushes.White,FontSize=12,FontWeight=FontWeights.SemiBold}};
  pill.MouseLeftButtonUp+=(s,e)=>{try{go();}catch(Exception ex){error=ex.Message;ShowError();}};return pill;
 }
 static void Settings(){
  var grid=new Grid{Margin=new Thickness(0,0,0,-8)};grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  var scroll=new ScrollViewer{Content=grid,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};view.Children.Add(scroll);
  var left=Stack();left.Margin=new Thickness(0,0,5,0);
  var switches=Stack();switches.Children.Add(ToggleRow("Não incomodar",doNotDisturb,v=>doNotDisturb=v));switches.Children.Add(ToggleRow("Manter acordado",keepAwake,SetKeepAwake));switches.Children.Add(ToggleRow("Ocultar em tela cheia",autoHideContext,v=>{autoHideContext=v;PresenceTick();}));left.Children.Add(CardBox(switches));
  var links=Stack();links.Children.Add(LinkRow("Prateleira",()=>Switch("shelf")));links.Children.Add(LinkRow("Áudio",()=>Switch("audio")));links.Children.Add(LinkRow("Conectar Claude",()=>Switch("agent")));left.Children.Add(CardBox(links));left.Children.Add(MonitorsCard());
  var right=Stack();right.Margin=new Thickness(5,0,0,0);Grid.SetColumn(right,1);
  var robo=Stack();var title=T("Robo · IA local",12,"#E3D9E8");title.Margin=new Thickness(8,4,0,4);robo.Children.Add(title);
  models=new ComboBox{Height=28,Margin=new Thickness(4,0,4,6)};if(!String.IsNullOrEmpty(localModel)){models.Items.Add(localModel);models.SelectedIndex=0;}
  models.SelectionChanged+=(s,e)=>{if(models.SelectedItem!=null){localModel=models.SelectedItem.ToString();File.WriteAllText(System.IO.Path.Combine(dataDir,"model.txt"),localModel);}};robo.Children.Add(models);
  robo.Children.Add(PillButton("Detectar modelos",DetectModels));
  modelStatus=T("Requer Ollama em execução neste PC.",10.5,"#B9ADB9");modelStatus.TextWrapping=TextWrapping.Wrap;modelStatus.Margin=new Thickness(8,7,8,4);robo.Children.Add(modelStatus);right.Children.Add(CardBox(robo));
  var media_=Stack();var status=T("Mídia · "+(media.available?"conectada ao Windows":String.IsNullOrEmpty(media.error)?"aguardando player":media.error.Trim()),10.5,"#B9ADB9");status.TextWrapping=TextWrapping.Wrap;status.Margin=new Thickness(8,5,8,3);media_.Children.Add(status);
  media_.Children.Add(LinkRow("Voltar ao player Windows",()=>{useSystem=true;player.Pause();playing=false;Switch("music");}));right.Children.Add(CardBox(media_));
  grid.Children.Add(left);grid.Children.Add(right);
 }
}
}
