using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
namespace Ilha {
public partial class App {
 static int labMode;
 // Lab: walks every transition. Mode 1 times render frames (no capture load); mode 2 saves PNG frames per transition.
 static void RunLab(){
  var steps=new List<KeyValuePair<string,Action>>();
  steps.Add(new KeyValuePair<string,Action>("01-rest-to-open",()=>{SetPose(false,false);Expand(true);}));
  string[] pages={"music","focus","agent","robo","agenda","notes","clipboard","painel","settings","home"};
  for(int i=0;i<pages.Length;i++){string p=pages[i];steps.Add(new KeyValuePair<string,Action>((i+2).ToString("00")+"-to-"+p,()=>Switch(p)));}
  steps.Add(new KeyValuePair<string,Action>("98-confetti",()=>{Switch("focus");Confetti();}));
  steps.Add(new KeyValuePair<string,Action>("99-open-to-rest",()=>Expand(false)));
  string dir=System.IO.Path.Combine(dataDir,"lab");Directory.CreateDirectory(dir);
  var report=new System.Text.StringBuilder("transition | render frames | avg ms | p95 ms | max ms | >24ms\n");
  var times=new List<double>();double last=-1;
  EventHandler render=(s,e)=>{double t=((System.Windows.Media.RenderingEventArgs)e).RenderingTime.TotalMilliseconds;if(t!=last){if(last>=0)times.Add(t-last);last=t;}};
  System.Windows.Media.CompositionTarget.Rendering+=render;
  int index=0,frame=0;string current=null;SetPose(false,false);
  var capture=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(33)};capture.Tick+=(s,e)=>CaptureMorphFrame(frame++);if(labMode==2)capture.Start();
  var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(750)};
  timer.Tick+=(s,e)=>{
   if(current!=null){
    if(labMode==2){string folder=System.IO.Path.Combine(dir,current);Directory.CreateDirectory(folder);for(int i=0;i<morphFrames.Count;i++)SavePng(morphFrames[i],"lab/"+current+"/f"+i.ToString("000")+".png");}
    else{times.Sort();int n=times.Count;report.AppendLine(current+" | "+n+" | "+(n==0?"-":(times.Sum()/n).ToString("0.0"))+" | "+(n==0?"-":times[(int)(n*.95)>=n?n-1:(int)(n*.95)].ToString("0.0"))+" | "+(n==0?"-":times[n-1].ToString("0.0"))+" | "+times.Count(x=>x>24));}
   }
   morphFrames.Clear();times.Clear();frame=0;
   if(index>=steps.Count){timer.Stop();capture.Stop();System.Windows.Media.CompositionTarget.Rendering-=render;if(labMode==1)File.WriteAllText(System.IO.Path.Combine(dir,"report.txt"),report.ToString());else File.WriteAllText(System.IO.Path.Combine(dir,"frames.txt"),"ok");w.Close();return;}
   current=steps[index].Key;steps[index].Value();index++;
  };
  timer.Start();
 }
 static void RunLifecycleChecks(){
  int phase=0,frame=0;SetPose(false,false);Directory.CreateDirectory(System.IO.Path.Combine(dataDir,"morph"));
  morphFrames.Clear();var frames=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(40)};
  frames.Tick+=(s,e)=>CaptureMorphFrame(frame++);frames.Start();
  var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(480)};
  timer.Tick+=(s,e)=>{
   try{
    switch(phase++){
     case 0: SetPose(false,false);if(opened||w.ShowInTaskbar)throw new Exception("Initial resting/switcher state");Expand(true);break;
     case 1: if(!opened||Math.Abs(islandClip.Rect.Width-600)>1||Math.Abs(islandClip.Rect.Height-270)>1)throw new Exception("Expansion did not finish");Expand(false);break;
     case 2: if(opened||Math.Abs(islandClip.Rect.Width-(compactWide?compactWidth:128))>1||Math.Abs(islandClip.Rect.Height-34)>1)throw new Exception("Collapse did not finish");w.Hide();break;
     case 3: if(w.IsVisible)throw new Exception("Hide did not suppress the window");w.Show();Expand(true);Expand(false);Expand(true);break;
     case 4:
      if(!w.IsVisible||!opened||Math.Abs(islandClip.Rect.Width-600)>1||restingShell.Visibility!=Visibility.Collapsed)throw new Exception("Restore or interrupted transition failed");
      if(!DesktopContext.IsToolWindow(new WindowInteropHelper(w).Handle))throw new Exception("Native tool-window style was not retained");
      timer.Stop();frames.Stop();CaptureMorphFrame(frame);SaveMorphFrames();File.WriteAllText(System.IO.Path.Combine(dataDir,"ui-test.txt"),"PASS: native tool-window style, expand/collapse, hidden resident loop, restore, interrupted transition. "+DateTime.UtcNow.ToString("o"));w.Close();break;
    }
   }catch(Exception ex){timer.Stop();frames.Stop();Environment.ExitCode=1;File.WriteAllText(System.IO.Path.Combine(dataDir,"ui-test.txt"),"FAIL: "+ex);w.Close();}
  };timer.Start();
 }
}
}
