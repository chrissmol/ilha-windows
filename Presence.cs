using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace Ilha {
public static class DesktopContext {
 [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)] struct MonitorInfo {public int Size;public Rect Monitor,Work;public uint Flags;}
 [StructLayout(LayoutKind.Sequential)] struct CursorPoint {public int X,Y;}
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
 [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd,System.Text.StringBuilder name,int length);
 [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd,uint flags);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
 [DllImport("user32.dll")] static extern bool GetCursorPos(out CursorPoint point);
 [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd,int attr,out Rect rect,int size);
 [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int GetWindowLong(IntPtr hwnd,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongW")] static extern int SetWindowLong(IntPtr hwnd,int index,int value);
 [DllImport("shell32.dll")] static extern int SHQueryUserNotificationState(out int state);
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] public static extern int SetCurrentProcessExplicitAppUserModelID(string id);
 static uint cachedPid;static string cachedName="",cachedPath="";
 public static string ForegroundApp="";
 public static void HideFromSwitcher(Window window){var hwnd=new WindowInteropHelper(window).Handle;int style=GetWindowLong(hwnd,-20);SetWindowLong(hwnd,-20,(style|0x80)&~0x40000);}
 public static bool CoversMonitor(Rect rect,Rect monitor){return rect.Left<=monitor.Left+2&&rect.Top<=monitor.Top+2&&rect.Right>=monitor.Right-2&&rect.Bottom>=monitor.Bottom-2;}
 public static bool Suppress(IntPtr ownWindow,ISet<string> hiddenApps){
  var hwnd=GetForegroundWindow();if(hwnd==IntPtr.Zero||hwnd==ownWindow||!IsWindowVisible(hwnd)||IsIconic(hwnd))return false;
  var cls=new System.Text.StringBuilder(256);GetClassName(hwnd,cls,cls.Capacity);string name=cls.ToString();
  if(new[]{"Progman","WorkerW","Shell_TrayWnd","Shell_SecondaryTrayWnd","XamlExplorerHostIslandWindow"}.Contains(name))return false;
  uint pid;GetWindowThreadProcessId(hwnd,out pid);
  if(pid!=cachedPid){cachedPid=pid;cachedName="";cachedPath="";try{using(var p=Process.GetProcessById((int)pid)){cachedName=p.ProcessName;try{cachedPath=p.MainModule.FileName;}catch{}}}catch{}}
  if(!String.Equals(cachedName,"explorer",StringComparison.OrdinalIgnoreCase)&&!String.Equals(cachedName,"ShellExperienceHost",StringComparison.OrdinalIgnoreCase)&&!String.IsNullOrEmpty(cachedName))ForegroundApp=cachedName;
  if(hiddenApps.Contains(cachedName))return true;
  if(name=="UnrealWindow"||name=="UnityWndClass")return true;
  if(cachedPath.IndexOf("\\steamapps\\common\\",StringComparison.OrdinalIgnoreCase)>=0)return true;
  if(cachedPath.IndexOf("\\Epic Games\\",StringComparison.OrdinalIgnoreCase)>=0&&cachedName.IndexOf("Launcher",StringComparison.OrdinalIgnoreCase)<0)return true;
  string[] known={"cs2","dota2","VALORANT-Win64-Shipping","FortniteClient-Win64-Shipping","Minecraft.Windows","RobloxPlayerBeta","RocketLeague","GenshinImpact","League of Legends","Overwatch","r5apex","RainbowSix","cod"};if(known.Contains(cachedName,StringComparer.OrdinalIgnoreCase))return true;
  var monitor=new MonitorInfo{Size=Marshal.SizeOf(typeof(MonitorInfo))};if(!GetMonitorInfo(MonitorFromWindow(hwnd,2),ref monitor))return false;
  Rect rect;if(DwmGetWindowAttribute(hwnd,9,out rect,Marshal.SizeOf(typeof(Rect)))!=0&&!GetWindowRect(hwnd,out rect))return false;
  if(CoversMonitor(rect,monitor.Monitor))return true;
  int state;return SHQueryUserNotificationState(out state)==0&&(state==2||state==3||state==4);
 }
 public static Point Gaze(Window window){
  CursorPoint point;if(!GetCursorPos(out point))return new Point();
  Rect rect;if(!GetWindowRect(new WindowInteropHelper(window).Handle,out rect))return new Point();
  double cx=(rect.Left+rect.Right)/2.0,cy=(rect.Top+rect.Bottom)/2.0;
  return new Point(Math.Max(-2,Math.Min(2,(point.X-cx)/180)),Math.Max(-1,Math.Min(1,(point.Y-cy)/180)));
 }
}
public partial class App {
 static Border restingShell;static MascotEyes mascot;static Grid windowHost;
 static bool contextHidden,autoHideContext=true;
 static HashSet<string> hiddenApps=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 static DateTime nextBlink=DateTime.UtcNow.AddSeconds(3);static Random random=new Random();
 static System.Threading.EventWaitHandle openSignal,restartSignal;
 static void SetPose(bool expanded,bool animated){
  opened=expanded;glass.Visibility=expanded?Visibility.Visible:Visibility.Collapsed;restingShell.Visibility=expanded?Visibility.Collapsed:Visibility.Visible;
  if(expanded){view.Visibility=Visibility.Visible;nav.Visibility=Visibility.Visible;date.Visibility=Visibility.Visible;time.Visibility=Visibility.Visible;compact.Visibility=Visibility.Collapsed;}
  double width=expanded?600:128,height=expanded?270:34;
  double left=SystemParameters.WorkArea.Left+(SystemParameters.WorkArea.Width-width)/2;
  if(animated){Animate(w,Window.WidthProperty,width);Animate(w,Window.HeightProperty,height);Animate(w,Window.LeftProperty,left);}else{
   w.BeginAnimation(Window.WidthProperty,null);w.BeginAnimation(Window.HeightProperty,null);w.BeginAnimation(Window.LeftProperty,null);w.Width=width;w.Height=height;w.Left=left;
  }
  if(expanded){view.Opacity=0;Animate(view,UIElement.OpacityProperty,1);}
 }
 static void SetupPresence(){
  string apps=System.IO.Path.Combine(dataDir,"hidden-apps.txt");if(File.Exists(apps))foreach(var line in File.ReadAllLines(apps))if(!String.IsNullOrWhiteSpace(line))hiddenApps.Add(line.Trim());
  openSignal=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\IlhaGlassOpen");
  restartSignal=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\IlhaGlassRestart");
  File.WriteAllLines(System.IO.Path.Combine(dataDir,"active.txt"),new[]{Process.GetCurrentProcess().Id.ToString(),Process.GetCurrentProcess().MainModule.FileName});
 }
 static void PresenceTick(){
  if(restartSignal!=null&&restartSignal.WaitOne(0)){w.Close();return;}
  bool suppress=autoHideContext&&DesktopContext.Suppress(new WindowInteropHelper(w).Handle,hiddenApps);
  if(suppress&&!contextHidden){contextHidden=true;leaveTimer.Stop();SetPose(false,false);w.Hide();}
  else if(!suppress&&contextHidden){contextHidden=false;SetPose(false,false);w.Show();}
  if(openSignal!=null&&openSignal.WaitOne(0)&&!contextHidden){w.Show();Expand(true);}
  if(!opened&&mascot!=null&&!contextHidden){
   Point gaze=DesktopContext.Gaze(w);mascot.GazeX=gaze.X;mascot.GazeY=gaze.Y;mascot.InvalidateVisual();
   if(DateTime.UtcNow>=nextBlink){mascot.BlinkOnce();nextBlink=DateTime.UtcNow.AddSeconds(3+random.NextDouble()*4);}
  }
 }
 static void Restart(){
  string launcher=System.IO.Path.Combine(baseDir,"Ilha.exe");
  Process.Start(new ProcessStartInfo(launcher,"--wait "+Process.GetCurrentProcess().Id){UseShellExecute=false,CreateNoWindow=true});w.Close();
 }
 static void HideCurrentApp(){
  string app=DesktopContext.ForegroundApp;if(String.IsNullOrEmpty(app))return;
  hiddenApps.Add(app);File.WriteAllLines(System.IO.Path.Combine(dataDir,"hidden-apps.txt"),hiddenApps);PresenceTick();
 }
}
}
