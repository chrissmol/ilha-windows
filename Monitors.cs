using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
namespace Ilha {
public partial class App {
 // Multi-monitor control: one island per screen (a process each). Turn screens on/off, follow plug/unplug, open the island on the screen under the mouse with Ctrl+Alt+T.
 static Dictionary<int,Process> spawned=new Dictionary<int,Process>();static DispatcherTimer displayTimer;
 [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hwnd,int id,uint modifiers,uint key);
 [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hwnd,int id);
 static string MonitorsFile(){return Path.Combine(dataDir,"monitors.txt");}
 static HashSet<int> OffMonitors(){var off=new HashSet<int>();try{if(File.Exists(MonitorsFile()))foreach(var line in File.ReadAllLines(MonitorsFile())){int n;if(Int32.TryParse(line.Trim(),out n))off.Add(n);}}catch{}return off;}
 static string MonitorLabel(int i){var screens=Forms.Screen.AllScreens;var b=screens[i].Bounds;return "Tela "+(i+1)+" · "+b.Width+"×"+b.Height+(screens[i].Primary?" · principal":"");}
 static void SignalIsland(string name,string suffix){try{using(var signal=System.Threading.EventWaitHandle.OpenExisting("Local\\"+name+suffix))signal.Set();}catch{}}
 // Primary only: starts islands for enabled screens that have none, closes the ones switched off.
 static void SyncIslands(){
  var off=OffMonitors();var screens=Forms.Screen.AllScreens;string exe=Process.GetCurrentProcess().MainModule.FileName;
  for(int i=0;i<screens.Length;i++){
   if(i==selectedMonitor)continue;
   Process p;spawned.TryGetValue(i,out p);bool running=p!=null&&!p.HasExited;
   if(off.Contains(i)){if(running)SignalIsland("IlhaGlassRestart","m"+i);}
   else if(!running)try{spawned[i]=Process.Start(new ProcessStartInfo(exe,"--monitor "+i+" --owner "+Process.GetCurrentProcess().Id){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=baseDir});}catch{}
  }
 }
 static void SetMonitor(int i,bool on){
  var off=OffMonitors();if(on)off.Remove(i);else off.Add(i);
  try{File.WriteAllLines(MonitorsFile(),off.Select(x=>x.ToString()).ToArray());}catch{}
  if(secondary){if(!on&&i==selectedMonitor)w.Close();}else SyncIslands();
 }
 static void SetupMonitors(){
  if(uiTest)return;
  Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=(s,e)=>w.Dispatcher.BeginInvoke(new Action(()=>{if(displayTimer!=null){displayTimer.Stop();displayTimer.Start();}}));
  // Debounced: Windows raises several display events per plug/unplug.
  displayTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(700)};
  displayTimer.Tick+=(s,e)=>{displayTimer.Stop();
   if(secondary&&selectedMonitor>=Forms.Screen.AllScreens.Length){w.Close();return;}
   SetPose(opened,false);if(!secondary)SyncIslands();};
  if(secondary)return;
  var helper=new WindowInteropHelper(w);IntPtr hwnd=helper.EnsureHandle();
  HwndSource.FromHwnd(hwnd).AddHook((IntPtr h,int msg,IntPtr wp,IntPtr lp,ref bool handled)=>{
   if(msg==0x312&&wp.ToInt32()==0x7157){handled=true;var screens=Forms.Screen.AllScreens;int i=Array.IndexOf(screens,Forms.Screen.FromPoint(Forms.Cursor.Position));SignalIsland("IlhaGlassOpen",i<0||i==selectedMonitor?"":"m"+i);}
   return IntPtr.Zero;});
  RegisterHotKey(hwnd,0x7157,0x3|0x4000,0x54); // Ctrl+Alt+T
  w.Closed+=(s,e)=>UnregisterHotKey(hwnd,0x7157);
 }
 static Forms.ToolStripMenuItem MonitorsMenu(){
  var root=new Forms.ToolStripMenuItem("Telas (Ctrl+Alt+T abre onde está o mouse)");
  root.DropDownOpening+=(s,e)=>{
   root.DropDownItems.Clear();var off=OffMonitors();var screens=Forms.Screen.AllScreens;
   for(int i=0;i<screens.Length;i++){int n=i;var item=new Forms.ToolStripMenuItem(MonitorLabel(i)){Checked=!off.Contains(i),Enabled=i!=selectedMonitor};item.Click+=(a,b)=>SetMonitor(n,off.Contains(n));root.DropDownItems.Add(item);}
  };
  root.DropDownItems.Add("…");return root;
 }
 static UIElement MonitorsCard(){
  var box=Stack();var title=T("Telas",12,"#E3D9E8");title.Margin=new Thickness(8,4,0,4);box.Children.Add(title);
  var off=OffMonitors();var screens=Forms.Screen.AllScreens;
  if(secondary)box.Children.Add(LinkRow("Desligar Tito nesta tela",()=>SetMonitor(selectedMonitor,false)));
  else for(int i=0;i<screens.Length;i++){int n=i;if(i==selectedMonitor){var own=T(MonitorLabel(i)+" · sempre ligada",10.5,"#B9ADB9");own.Margin=new Thickness(8,4,8,4);box.Children.Add(own);}else box.Children.Add(ToggleRow(MonitorLabel(i),!off.Contains(i),v=>SetMonitor(n,v)));}
  var hint=T("Ctrl+Alt+T abre o Tito na tela onde está o mouse.",10.5,"#B9ADB9");hint.TextWrapping=TextWrapping.Wrap;hint.Margin=new Thickness(8,4,8,4);box.Children.Add(hint);
  return CardBox(box);
 }
}
}
