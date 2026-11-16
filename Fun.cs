using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace Ilha {
public partial class App {
 // Color and play: every drawer has its own color (glow, buttons, headings), the active tab bounces, and finished work throws confetti.
 static string PageHex(string p){string a=Accent(p);return a=="#FFFFFF"?"#9C9CD8":a;}
 static Color HexColor(string hex){return (Color)ColorConverter.ConvertFromString(hex);}
 static GradientStop glowCenter,glowEdge;static Button activeNav;static Canvas confettiLayer;static string lastMood="idle";
 static Border MakeGlow(){
  glowCenter=new GradientStop(Color.FromArgb(0x4A,0x9C,0x9C,0xD8),0);glowEdge=new GradientStop(Color.FromArgb(0,0x9C,0x9C,0xD8),1);
  return new Border{IsHitTestVisible=false,Background=new RadialGradientBrush(new GradientStopCollection{glowCenter,glowEdge}){Center=new Point(.12,0),GradientOrigin=new Point(.12,0),RadiusX=.95,RadiusY=1.25}};
 }
 static void SetGlow(string page){
  if(glowCenter==null)return;Color c=HexColor(PageHex(page)),edge=Color.FromArgb(0,c.R,c.G,c.B),center=Color.FromArgb(0x4A,c.R,c.G,c.B);
  if(!SystemParameters.ClientAreaAnimation){glowCenter.Color=center;glowEdge.Color=edge;return;}
  var time=TimeSpan.FromMilliseconds(420);glowCenter.BeginAnimation(GradientStop.ColorProperty,new ColorAnimation(center,time));glowEdge.BeginAnimation(GradientStop.ColorProperty,new ColorAnimation(edge,time));
 }
 static void NavBounce(){
  if(activeNav==null||!SystemParameters.ClientAreaAnimation)return;var icon=activeNav.Content as UIElement;if(icon==null)return;
  var scale=new ScaleTransform(1,1);icon.RenderTransformOrigin=new Point(.5,.5);icon.RenderTransform=scale;
  var bounce=new DoubleAnimationUsingKeyFrames{Duration=TimeSpan.FromMilliseconds(380)};
  bounce.KeyFrames.Add(new LinearDoubleKeyFrame(.8,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));bounce.KeyFrames.Add(new LinearDoubleKeyFrame(1.22,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(230))));bounce.KeyFrames.Add(new LinearDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(380))));
  scale.BeginAnimation(ScaleTransform.ScaleXProperty,bounce);scale.BeginAnimation(ScaleTransform.ScaleYProperty,bounce);
 }
 static readonly string[] confettiColors={"#FF375F","#FF9F0A","#FFD60A","#30D158","#64D2FF","#BF5AF2"};
 static void Confetti(){
  if(confettiLayer==null||!SystemParameters.ClientAreaAnimation)return;var random=new Random();
  for(int i=0;i<30;i++){
   var piece=new System.Windows.Shapes.Rectangle{Width=random.Next(5,9),Height=random.Next(4,8),RadiusX=1.5,RadiusY=1.5,Fill=new SolidColorBrush(HexColor(confettiColors[random.Next(confettiColors.Length)])),RenderTransformOrigin=new Point(.5,.5)};
   var spin=new RotateTransform();piece.RenderTransform=spin;double x0=320+random.Next(-24,25),y0=25,dx=random.Next(-270,271);double up=random.Next(40,90),down=random.Next(110,200);
   confettiLayer.Children.Add(piece);Canvas.SetLeft(piece,x0);Canvas.SetTop(piece,y0);
   var time=TimeSpan.FromMilliseconds(random.Next(800,1300));
   piece.BeginAnimation(Canvas.LeftProperty,new DoubleAnimation(x0,x0+dx,time){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});
   var fall=new DoubleAnimationUsingKeyFrames{Duration=time};fall.KeyFrames.Add(new EasingDoubleKeyFrame(y0-up,KeyTime.FromPercent(.35),new CubicEase{EasingMode=EasingMode.EaseOut}));fall.KeyFrames.Add(new EasingDoubleKeyFrame(y0+down,KeyTime.FromPercent(1),new QuadraticEase{EasingMode=EasingMode.EaseIn}));
   piece.BeginAnimation(Canvas.TopProperty,fall);
   spin.BeginAnimation(RotateTransform.AngleProperty,new DoubleAnimation(0,random.Next(-540,540),time));
   var fade=new DoubleAnimation(1,0,time){BeginTime=TimeSpan.FromMilliseconds(time.TotalMilliseconds*.6)};var target=piece;fade.Completed+=(s,e)=>confettiLayer.Children.Remove(target);
   piece.BeginAnimation(UIElement.OpacityProperty,fade);
  }
 }
}
}
