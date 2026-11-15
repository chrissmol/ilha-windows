using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace Ilha {
public sealed class MascotEyes : FrameworkElement {
 public static readonly DependencyProperty BlinkProperty=DependencyProperty.Register("Blink",typeof(double),typeof(MascotEyes),new FrameworkPropertyMetadata(1.0,FrameworkPropertyMetadataOptions.AffectsRender));
 public double Blink {get{return (double)GetValue(BlinkProperty);}set{SetValue(BlinkProperty,value);}}
 public static readonly DependencyProperty BlendProperty=DependencyProperty.Register("Blend",typeof(double),typeof(MascotEyes),new FrameworkPropertyMetadata(1.0,FrameworkPropertyMetadataOptions.AffectsRender));
 public double Blend {get{return (double)GetValue(BlendProperty);}set{SetValue(BlendProperty,value);}}
 public double GazeX,GazeY;
 struct Face {public double Left,Right,Width,Smile,Violet;}
 static Face Pose(string mood){return new Face{Left=mood=="sleepy"?3:mood=="focus"?7:mood=="curious"?13:mood=="surprised"?14:mood=="happy"?4:11,Right=mood=="sleepy"?3:mood=="focus"?7:mood=="curious"?9:mood=="surprised"?14:mood=="happy"?4:10.5,Width=mood=="surprised"?7:6,Smile=mood=="happy"?1:0,Violet=mood=="focus"?1:0};}
 Face from=Pose("idle"),to=Pose("idle");string mood="idle";
 static double Mix(double a,double b,double t){return a+(b-a)*t;}
 Face Current(){double t=Blend;return new Face{Left=Mix(from.Left,to.Left,t),Right=Mix(from.Right,to.Right,t),Width=Mix(from.Width,to.Width,t),Smile=Mix(from.Smile,to.Smile,t),Violet=Mix(from.Violet,to.Violet,t)};}
 public string Mood {get{return mood;}set{if(mood==value)return;from=Current();to=Pose(value);mood=value;BeginAnimation(BlendProperty,null);Blend=0;if(SystemParameters.ClientAreaAnimation)BeginAnimation(BlendProperty,new DoubleAnimation(1,TimeSpan.FromMilliseconds(230)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});else Blend=1;}}
 public void BlinkOnce(){
  if(!SystemParameters.ClientAreaAnimation)return;
  var animation=new DoubleAnimationUsingKeyFrames {Duration=TimeSpan.FromMilliseconds(170),FillBehavior=FillBehavior.Stop};
  animation.KeyFrames.Add(new LinearDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.Zero)));
  animation.KeyFrames.Add(new LinearDoubleKeyFrame(.12,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(75))));
  animation.KeyFrames.Add(new LinearDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(170))));
  BeginAnimation(BlinkProperty,animation);
 }
 protected override void OnRender(DrawingContext dc){
  base.OnRender(dc);Face f=Current();double x=ActualWidth/2+GazeX,y=ActualHeight/2+GazeY;
  var white=new SolidColorBrush(Color.FromRgb((byte)Mix(255,188,f.Violet),(byte)Mix(255,131,f.Violet),255));
  Eye(dc,white,x-7,y,f.Left,f);Eye(dc,white,x+7,y,f.Right,f);
 }
 void Eye(DrawingContext dc,Brush brush,double x,double y,double height,Face f){dc.PushOpacity(1-f.Smile);double h=Math.Max(1,height*Blink);dc.DrawRoundedRectangle(brush,null,new Rect(x-f.Width/2,y-h/2,f.Width,h),f.Width/2,f.Width/2);dc.Pop();if(f.Smile>.001){dc.PushOpacity(f.Smile);var pen=new Pen(brush,2.6){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};var curve=new StreamGeometry();using(var c=curve.Open()){c.BeginFigure(new Point(x-3,y+1),false,false);c.QuadraticBezierTo(new Point(x,y-4),new Point(x+3,y+1),true,false);}dc.DrawGeometry(null,pen,curve);dc.Pop();}}
}
}
