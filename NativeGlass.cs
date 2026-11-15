using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
namespace Ilha {
public static class NativeGlass {
 [StructLayout(LayoutKind.Sequential)] struct Margins {public int L,R,T,B;}
 [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
 [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd,ref Margins margins);
 [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left,Top,Right,Bottom; }
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
 [DllImport("user32.dll")] static extern int SetWindowRgn(IntPtr hwnd,IntPtr region,bool redraw);
 [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int x1,int y1,int x2,int y2,int ellipseWidth,int ellipseHeight);
 [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr region);
 public static void Round(Window window){
  IntPtr hwnd=new WindowInteropHelper(window).Handle;if(hwnd==IntPtr.Zero)return;
  Rect rect;if(!GetWindowRect(hwnd,out rect))return;
  var source=PresentationSource.FromVisual(window);double scale=source==null?1:source.CompositionTarget.TransformToDevice.M11;
  int diameter=(int)(48*scale);IntPtr region=CreateRoundRectRgn(0,0,rect.Right-rect.Left+1,rect.Bottom-rect.Top+1,diameter,diameter);
  if(SetWindowRgn(hwnd,region,true)==0)DeleteObject(region);
 }
 public static bool Apply(Window window) {
  var hwnd=new WindowInteropHelper(window).Handle;
  var source=HwndSource.FromHwnd(hwnd);source.CompositionTarget.BackgroundColor=Colors.Transparent;
  int dark=1;DwmSetWindowAttribute(hwnd,20,ref dark,4);
  int corner=2;DwmSetWindowAttribute(hwnd,33,ref corner,4);
  int border=-2;DwmSetWindowAttribute(hwnd,34,ref border,4);
  var margins=new Margins {L=-1,R=-1,T=-1,B=-1};DwmExtendFrameIntoClientArea(hwnd,ref margins);
  int acrylic=3;return DwmSetWindowAttribute(hwnd,38,ref acrylic,4)==0;
 }
 public static void Prepare(Window window) {
  WindowChrome.SetWindowChrome(window,new WindowChrome {CaptionHeight=0,ResizeBorderThickness=new Thickness(0),GlassFrameThickness=new Thickness(-1),UseAeroCaptionButtons=false,CornerRadius=new CornerRadius(24)});
  window.SizeChanged+=(s,e)=>Round(window);
 }
}
}
