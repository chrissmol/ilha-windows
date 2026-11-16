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
 [DllImport("user32.dll")] static extern bool IsZoomed(IntPtr hwnd);
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
 [DllImport("user32.dll")] static extern bool SetWindowDisplayAffinity(IntPtr hwnd,uint affinity);
 [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr handle);
 public static System.Windows.Media.Imaging.BitmapSource CaptureBehind(Window window,double dipWidth,double dipHeight,double dipTop){
  var hwnd=new WindowInteropHelper(window).Handle;Rect rect;if(hwnd==IntPtr.Zero||!GetWindowRect(hwnd,out rect))return null;
  var source=PresentationSource.FromVisual(window);double scale=source==null?1:source.CompositionTarget.TransformToDevice.M11;
  int pw=(int)(dipWidth*scale),ph=(int)(dipHeight*scale),x=(int)((rect.Left+rect.Right)/2.0-pw/2.0),y=(int)(rect.Top+dipTop*scale);
  SetWindowDisplayAffinity(hwnd,0x11);
  try{using(var bmp=new System.Drawing.Bitmap(pw,ph)){using(var g=System.Drawing.Graphics.FromImage(bmp))g.CopyFromScreen(x,y,0,0,bmp.Size);IntPtr h=bmp.GetHbitmap();try{var image=System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(h,IntPtr.Zero,Int32Rect.Empty,System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(Math.Max(1,pw/10),Math.Max(1,ph/10)));image.Freeze();return image;}finally{DeleteObject(h);}}}
  finally{SetWindowDisplayAffinity(hwnd,0);}
 }
 public static bool IsToolWindow(IntPtr hwnd){int style=GetWindowLong(hwnd,-20);return (style&0x80)!=0&&(style&0x40000)==0;}
 public static bool CoversMonitor(Rect rect,Rect monitor){return rect.Left<=monitor.Left+2&&rect.Top<=monitor.Top+2&&rect.Right>=monitor.Right-2&&rect.Bottom>=monitor.Bottom-2;}
 public static bool Suppress(IntPtr ownWindow,ISet<string> hiddenApps){
  var hwnd=GetForegroundWindow();if(hwnd==IntPtr.Zero||hwnd==ownWindow||!IsWindowVisible(hwnd)||IsIconic(hwnd))return false;
  var cls=new System.Text.StringBuilder(256);GetClassName(hwnd,cls,cls.Capacity);string name=cls.ToString();
  if(new[]{"Progman","WorkerW","Shell_TrayWnd","Shell_SecondaryTrayWnd","XamlExplorerHostIslandWindow"}.Contains(name))return false;
  uint pid;GetWindowThreadProcessId(hwnd,out pid);
  if(pid!=cachedPid){cachedPid=pid;cachedName="";cachedPath="";try{using(var p=Process.GetProcessById((int)pid)){cachedName=p.ProcessName;try{cachedPath=p.MainModule.FileName;}catch{}}}catch{}}
  if(!String.Equals(cachedName,"explorer",StringComparison.OrdinalIgnoreCase)&&!String.Equals(cachedName,"ShellExperienceHost",StringComparison.OrdinalIgnoreCase)&&!String.IsNullOrEmpty(cachedName))ForegroundApp=cachedName;
  if(MonitorFromWindow(hwnd,2)!=MonitorFromWindow(ownWindow,2))return false;
  if(hiddenApps.Contains(cachedName))return true;
  if(name=="UnrealWindow"||name=="UnityWndClass")return true;
  if(cachedPath.IndexOf("\\steamapps\\common\\",StringComparison.OrdinalIgnoreCase)>=0)return true;
  if(cachedPath.IndexOf("\\Epic Games\\",StringComparison.OrdinalIgnoreCase)>=0&&cachedName.IndexOf("Launcher",StringComparison.OrdinalIgnoreCase)<0)return true;
  string[] known={"cs2","dota2","VALORANT-Win64-Shipping","FortniteClient-Win64-Shipping","Minecraft.Windows","RobloxPlayerBeta","RocketLeague","GenshinImpact","League of Legends","Overwatch","r5apex","RainbowSix","cod"};if(known.Contains(cachedName,StringComparer.OrdinalIgnoreCase))return true;
  var monitor=new MonitorInfo{Size=Marshal.SizeOf(typeof(MonitorInfo))};if(!GetMonitorInfo(MonitorFromWindow(hwnd,2),ref monitor))return false;
  Rect rect;if(DwmGetWindowAttribute(hwnd,9,out rect,Marshal.SizeOf(typeof(Rect)))!=0&&!GetWindowRect(hwnd,out rect))return false;
  if(CoversMonitor(rect,monitor.Monitor)&&!IsZoomed(hwnd))return true;
  int state;return SHQueryUserNotificationState(out state)==0&&(state==2||state==3||state==4);
 }
 public static Point Gaze(Window window){
  CursorPoint point;if(!GetCursorPos(out point))return new Point();
  Rect rect;if(!GetWindowRect(new WindowInteropHelper(window).Handle,out rect))return new Point();
  double cx=(rect.Left+rect.Right)/2.0,cy=rect.Top+25*(rect.Bottom-rect.Top)/300.0;
  return new Point(Math.Max(-2,Math.Min(2,(point.X-cx)/180)),Math.Max(-1,Math.Min(1,(point.Y-cy)/180)));
 }
}
public partial class App {
 static Border restingShell;static MascotEyes mascot;static Grid windowHost,island;static Image backdropImage;
 static void CaptureBackdrop(){try{if(!w.IsVisible)return;var s=DesktopContext.CaptureBehind(w,600,270,8);if(s!=null)backdropImage.Source=s;}catch{}}
 static bool secondary;static int ownerPid;static Process ownerProc;static string suffix="";
 static bool contextHidden,autoHideContext=true;
 static HashSet<string> hiddenApps=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 static DateTime nextBlink=DateTime.UtcNow.AddSeconds(3);static Random random=new Random();
 static System.Threading.EventWaitHandle openSignal,restartSignal;
 static void SetPose(bool expanded,bool animated){
  if(!SystemParameters.ClientAreaAnimation)animated=false;
  if(expanded&&!opened)CaptureBackdrop();
  opened=expanded;int revision=++poseRevision;glass.Width=600;glass.Height=270;glass.HorizontalAlignment=HorizontalAlignment.Center;glass.VerticalAlignment=VerticalAlignment.Top;
  if(expanded){view.Visibility=Visibility.Visible;nav.Visibility=Visibility.Visible;date.Visibility=Visibility.Visible;time.Visibility=Visibility.Visible;compact.Visibility=Visibility.Collapsed;}
  double width=expanded?600:compactWide?176:128,height=expanded?270:34;
  var area=MonitorArea();w.Left=area.Left+(area.Width-640)/2;w.Top=area.Top;
  glass.IsHitTestVisible=expanded;restingShell.IsHitTestVisible=!expanded;
  if(animated){
   glass.Visibility=Visibility.Visible;restingShell.Visibility=Visibility.Visible;
   Spring(FrameworkElement.WidthProperty,width,expanded);Spring(FrameworkElement.HeightProperty,height,expanded);
   var panelFade=new System.Windows.Media.Animation.DoubleAnimation(expanded?1:0,TimeSpan.FromMilliseconds(expanded?260:140)){BeginTime=expanded?TimeSpan.FromMilliseconds(90):TimeSpan.Zero};
   panelFade.Completed+=(s,e)=>{if(revision==poseRevision&&!expanded)glass.Visibility=Visibility.Collapsed;};glass.BeginAnimation(UIElement.OpacityProperty,panelFade);
   var faceFade=new System.Windows.Media.Animation.DoubleAnimation(expanded?0:1,TimeSpan.FromMilliseconds(180));
   faceFade.Completed+=(s,e)=>{if(revision==poseRevision&&expanded)restingShell.Visibility=Visibility.Collapsed;};restingShell.BeginAnimation(UIElement.OpacityProperty,faceFade);
  }else{
   island.BeginAnimation(FrameworkElement.WidthProperty,null);island.BeginAnimation(FrameworkElement.HeightProperty,null);island.Width=width;island.Height=height;
   glass.BeginAnimation(UIElement.OpacityProperty,null);restingShell.BeginAnimation(UIElement.OpacityProperty,null);glass.Opacity=expanded?1:0;restingShell.Opacity=expanded?0:1;
   glass.Visibility=expanded?Visibility.Visible:Visibility.Collapsed;restingShell.Visibility=expanded?Visibility.Collapsed:Visibility.Visible;
  }
 }
 static void SetupPresence(){
  string apps=System.IO.Path.Combine(dataDir,"hidden-apps.txt");if(File.Exists(apps))foreach(var line in File.ReadAllLines(apps))if(!String.IsNullOrWhiteSpace(line))hiddenApps.Add(line.Trim());
  openSignal=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\IlhaGlassOpen"+suffix);
  restartSignal=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\IlhaGlassRestart"+suffix);
  if(!secondary)File.WriteAllLines(System.IO.Path.Combine(dataDir,"active.txt"),new[]{Process.GetCurrentProcess().Id.ToString(),Process.GetCurrentProcess().MainModule.FileName});
 }
 static void SpawnOtherIslands(){var screens=System.Windows.Forms.Screen.AllScreens;string exe=Process.GetCurrentProcess().MainModule.FileName;for(int i=0;i<screens.Length;i++)if(i!=selectedMonitor)try{Process.Start(new ProcessStartInfo(exe,"--monitor "+i+" --owner "+Process.GetCurrentProcess().Id){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=baseDir});}catch{}}
 static void PresenceTick(){
  if(secondary&&ownerProc!=null&&ownerProc.HasExited){w.Close();return;}
  if(restartSignal!=null&&restartSignal.WaitOne(0)){w.Close();return;}
  bool suppress=autoHideContext&&DesktopContext.Suppress(new WindowInteropHelper(w).Handle,hiddenApps);
  if(suppress&&!contextHidden){contextHidden=true;leaveTimer.Stop();SetPose(false,false);w.Hide();}
  else if(!suppress&&contextHidden){contextHidden=false;SetPose(false,false);w.Show();}
  if(openSignal!=null&&openSignal.WaitOne(0)&&!contextHidden){w.Show();Expand(true);}
  if(!opened&&mascot!=null&&!contextHidden){
   Point gaze=DesktopContext.Gaze(w);if((gaze-lastGaze).Length>.06){gazeUntil=DateTime.UtcNow.AddMilliseconds(1300);lastGaze=gaze;}if(DateTime.UtcNow>gazeUntil)gaze=new Point();mascot.GazeX+=(gaze.X-mascot.GazeX)*.35;mascot.GazeY+=(gaze.Y-mascot.GazeY)*.35;mascot.InvalidateVisual();
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
