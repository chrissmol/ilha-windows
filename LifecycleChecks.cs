using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
namespace Ilha {
public partial class App {
 static void RunLifecycleChecks(){
  int phase=0,frame=0;SetPose(false,false);Directory.CreateDirectory(System.IO.Path.Combine(dataDir,"morph"));
  var frames=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(40)};
  frames.Tick+=(s,e)=>CaptureMorphFrame(frame++);frames.Start();
  var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(480)};
  timer.Tick+=(s,e)=>{
   try{
    switch(phase++){
     case 0: SetPose(false,false);if(opened||w.ShowInTaskbar)throw new Exception("Initial resting/switcher state");Expand(true);break;
     case 1: if(!opened||Math.Abs(w.ActualWidth-600)>1||Math.Abs(w.ActualHeight-270)>1)throw new Exception("Expansion did not finish");Expand(false);break;
     case 2: if(opened||Math.Abs(w.ActualWidth-128)>1||Math.Abs(w.ActualHeight-34)>1)throw new Exception("Collapse did not finish");w.Hide();break;
     case 3: if(w.IsVisible)throw new Exception("Hide did not suppress the window");w.Show();Expand(true);Expand(false);Expand(true);break;
     case 4:
      if(!w.IsVisible||!opened||Math.Abs(w.ActualWidth-600)>1||restingShell.Visibility!=Visibility.Collapsed)throw new Exception("Restore or interrupted transition failed");
      if(!DesktopContext.IsToolWindow(new WindowInteropHelper(w).Handle))throw new Exception("Native tool-window style was not retained");
      timer.Stop();frames.Stop();CaptureMorphFrame(frame);File.WriteAllText(System.IO.Path.Combine(dataDir,"ui-test.txt"),"PASS: native tool-window style, expand/collapse, hidden resident loop, restore, interrupted transition. "+DateTime.UtcNow.ToString("o"));w.Close();break;
    }
   }catch(Exception ex){timer.Stop();frames.Stop();Environment.ExitCode=1;File.WriteAllText(System.IO.Path.Combine(dataDir,"ui-test.txt"),"FAIL: "+ex);w.Close();}
  };timer.Start();
 }
}
}
