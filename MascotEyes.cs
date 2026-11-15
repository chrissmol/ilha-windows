using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace Ilha {
public sealed class MascotEyes : FrameworkElement {
 public static readonly DependencyProperty BlinkProperty=DependencyProperty.Register("Blink",typeof(double),typeof(MascotEyes),new FrameworkPropertyMetadata(1.0,FrameworkPropertyMetadataOptions.AffectsRender));
 public double Blink {get{return (double)GetValue(BlinkProperty);}set{SetValue(BlinkProperty,value);}}
 public double GazeX,GazeY;public string Mood="idle";
 public void BlinkOnce(){
  var animation=new DoubleAnimationUsingKeyFrames {Duration=TimeSpan.FromMilliseconds(170),FillBehavior=FillBehavior.Stop};
  animation.KeyFrames.Add(new LinearDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.Zero)));
  animation.KeyFrames.Add(new LinearDoubleKeyFrame(.12,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(75))));
  animation.KeyFrames.Add(new LinearDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(170))));
  BeginAnimation(BlinkProperty,animation);
 }
 protected override void OnRender(DrawingContext dc){
  base.OnRender(dc);double x=ActualWidth/2+GazeX,y=ActualHeight/2+GazeY;double h=9*Math.Max(.12,Blink);
  var white=new SolidColorBrush(Mood=="focus"?Color.FromRgb(188,131,255):Colors.White);white.Freeze();
  if(Mood=="happy"){var pen=new Pen(white,2.5);dc.DrawLine(pen,new Point(x-9,y+1),new Point(x-6.5,y-2));dc.DrawLine(pen,new Point(x-6.5,y-2),new Point(x-4,y+1));dc.DrawLine(pen,new Point(x+4,y+1),new Point(x+6.5,y-2));dc.DrawLine(pen,new Point(x+6.5,y-2),new Point(x+9,y+1));return;}
  if(Mood=="curious")h=12*Math.Max(.12,Blink);
  dc.DrawRoundedRectangle(white,null,new Rect(x-9,y-h/2,5,h),2.5,2.5);
  dc.DrawRoundedRectangle(white,null,new Rect(x+4,y-h/2,5,h),2.5,2.5);
 }
}
}
