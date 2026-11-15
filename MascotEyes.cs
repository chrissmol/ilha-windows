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
 public static readonly DependencyProperty MediaBlendProperty=DependencyProperty.Register("MediaBlend",typeof(double),typeof(MascotEyes),new FrameworkPropertyMetadata(0.0,FrameworkPropertyMetadataOptions.AffectsRender));
 public double MediaBlend {get{return (double)GetValue(MediaBlendProperty);}set{SetValue(MediaBlendProperty,value);}}
 public static readonly DependencyProperty WaveStepProperty=DependencyProperty.Register("WaveStep",typeof(double),typeof(MascotEyes),new FrameworkPropertyMetadata(1.0,FrameworkPropertyMetadataOptions.AffectsRender));
 public double WaveStep {get{return (double)GetValue(WaveStepProperty);}set{SetValue(WaveStepProperty,value);}}
 bool mediaMode;readonly double[] peaks=new double[7],waveFrom=new double[7],waveTo=new double[7];double envelope;
 public bool MediaMode {get{return mediaMode;}set{if(mediaMode==value)return;mediaMode=value;if(SystemParameters.ClientAreaAnimation)BeginAnimation(MediaBlendProperty,new DoubleAnimation(value?1:0,TimeSpan.FromMilliseconds(280)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}});else{BeginAnimation(MediaBlendProperty,null);MediaBlend=value?1:0;}}}
 public void AudioPeak(double value){if(Double.IsNaN(value)||Double.IsInfinity(value))value=0;value=Math.Max(0,Math.Min(1,value));double blend=WaveStep;for(int i=0;i<7;i++)waveFrom[i]=Mix(waveFrom[i],waveTo[i],blend);envelope+=(value-envelope)*(value>envelope?.5:.16);for(int i=0;i<6;i++)peaks[i]=peaks[i+1];peaks[6]=envelope;for(int i=0;i<7;i++)waveTo[i]=peaks[i]*.6+peaks[Math.Max(0,i-1)]*.2+peaks[Math.Min(6,i+1)]*.2;BeginAnimation(WaveStepProperty,null);WaveStep=0;if(mediaMode&&SystemParameters.ClientAreaAnimation)BeginAnimation(WaveStepProperty,new DoubleAnimation(1,TimeSpan.FromMilliseconds(95)));else WaveStep=1;InvalidateVisual();}
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
  double morph=MediaBlend;dc.PushOpacity(1-morph);Eye(dc,white,x-7,y,f.Left,f);Eye(dc,white,x+7,y,f.Right,f);dc.Pop();
  if(morph>0){dc.PushOpacity(morph);var wave=new SolidColorBrush(Color.FromRgb(83,190,255));for(int i=0;i<7;i++){double level=Mix(waveFrom[i],waveTo[i],WaveStep);double h=2+(SystemParameters.ClientAreaAnimation?Math.Sqrt(Math.Max(0,level))*14:0);double bx=ActualWidth/2+(i-3)*4.5;dc.DrawRoundedRectangle(wave,null,new Rect(bx-1.25,y-h/2,2.5,h),1.25,1.25);}dc.Pop();}
 }
 void Eye(DrawingContext dc,Brush brush,double x,double y,double height,Face f){dc.PushOpacity(1-f.Smile);double h=Math.Max(1,height*Blink);dc.DrawRoundedRectangle(brush,null,new Rect(x-f.Width/2,y-h/2,f.Width,h),f.Width/2,f.Width/2);dc.Pop();if(f.Smile>.001){dc.PushOpacity(f.Smile);var pen=new Pen(brush,2.6){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};var curve=new StreamGeometry();using(var c=curve.Open()){c.BeginFigure(new Point(x-3,y+1),false,false);c.QuadraticBezierTo(new Point(x,y-4),new Point(x+3,y+1),true,false);}dc.DrawGeometry(null,pen,curve);dc.Pop();}}
}
}
