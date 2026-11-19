using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Forms=System.Windows.Forms;
namespace Tito {
public partial class App {
 // Caixa de comando (Ctrl+Alt+Espaço): digite e Enter. Aparece na tela do mouse, some ao perder o foco.
 static Window cmdWin;
 [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int cx,int cy,uint flags);
 static readonly string[][] cmdPages={new[]{"home","início"},new[]{"music","mídia"},new[]{"music","música"},new[]{"focus","foco"},new[]{"ia","ia"},new[]{"agenda","agenda"},new[]{"notes","notas"},new[]{"clipboard","colar"},new[]{"shelf","prateleira"},new[]{"audio","áudio"},new[]{"settings","configurações"}};
 static void CmdDone(){celebrateUntil=DateTime.UtcNow.AddSeconds(2);}
 static void CmdOpen(string target){try{Process.Start(new ProcessStartInfo(target){UseShellExecute=true});}catch(Exception ex){MessageBox.Show("Não consegui abrir “"+target+"”.\n"+ex.Message,"Tito",MessageBoxButton.OK,MessageBoxImage.Information);}}
 static void CmdFocus(int minutes){running=false;remaining=minutes*60;end=DateTime.UtcNow.AddSeconds(remaining);running=true;Tick();CmdDone();}
 static List<KeyValuePair<string,Action>> CommandMatches(string text){
  var list=new List<KeyValuePair<string,Action>>();string t=(text??"").Trim();if(t.Length==0)return list;
  Action<string,Action> add=(label,run)=>list.Add(new KeyValuePair<string,Action>(label,run));
  Match m;
  if((m=Regex.Match(t,@"^(?:foco|focus)(?:\s+(\d{1,3}))?$",RegexOptions.IgnoreCase)).Success){int min=m.Groups[1].Success?Math.Max(1,Math.Min(MaxFocusMin,Int32.Parse(m.Groups[1].Value))):25;add("Iniciar foco de "+min+" min",()=>CmdFocus(min));}
  if(Regex.IsMatch(t,@"^(?:parar|pausar|pause)\s+foco$",RegexOptions.IgnoreCase))add("Pausar o foco",()=>{if(running){remaining=Math.Max(0,(end-DateTime.UtcNow).TotalSeconds);running=false;Tick();}});
  if((m=Regex.Match(t,@"^(?:nota|anotar)\s+(.+)$",RegexOptions.IgnoreCase)).Success){string note=m.Groups[1].Value;add("Anotar: "+note,()=>{try{string old=File.Exists(NotesPath())?File.ReadAllText(NotesPath()):"";File.AppendAllText(NotesPath(),(old.Length>0&&!old.EndsWith("\n")?"\n":"")+note+"\n");CmdDone();}catch(Exception ex){MessageBox.Show(ex.Message,"Tito");}});}
  if((m=Regex.Match(t,@"^agenda\s+(?:(\d{4}-\d{2}-\d{2})\s+)?(\d{1,2}:\d{2})\s+(.+)$",RegexOptions.IgnoreCase)).Success){string date=m.Groups[1].Success?m.Groups[1].Value:DateTime.Now.ToString("yyyy-MM-dd");string time=m.Groups[2].Value.PadLeft(5,'0');string title=m.Groups[3].Value.Replace("|"," ");add("Agendar "+time+" "+title,()=>{try{File.AppendAllText(AgendaPath(),date+"|"+time+"|"+title+"\n");CmdDone();}catch(Exception ex){MessageBox.Show(ex.Message,"Tito");}});}
  if(Regex.IsMatch(t,@"^(?:tocar|pausar|play|pause)$",RegexOptions.IgnoreCase))add("Tocar / pausar mídia",()=>SendMedia("toggle"));
  if(Regex.IsMatch(t,@"^(?:próxima|proxima|next)$",RegexOptions.IgnoreCase))add("Próxima faixa",()=>SendMedia("next"));
  string low=t.ToLowerInvariant();
  foreach(var p in cmdPages)if(p[1].StartsWith(low)){string target=p[0];add("Abrir página: "+p[1],()=>Switch(target));}
  if((m=Regex.Match(t,@"^(?:abrir|open)\s+(.+)$",RegexOptions.IgnoreCase)).Success){string target=m.Groups[1].Value.Trim();add("Abrir "+target,()=>CmdOpen(target));}
  else if(Regex.IsMatch(t,@"^https?://",RegexOptions.IgnoreCase))add("Abrir link",()=>CmdOpen(t));
  else if(list.Count==0)add("Abrir "+t,()=>CmdOpen(t)); // sem comando conhecido: tenta abrir como programa (notepad, calc, chrome…)
  return list;
 }
 static void CommandBox(){
  if(cmdWin!=null){cmdWin.Close();return;}
  var box=new TextBox{Background=Brushes.Transparent,Foreground=B("#FFF8F6"),CaretBrush=Brushes.White,BorderThickness=new Thickness(0),FontSize=18,Padding=new Thickness(12,10,12,10),ToolTip="foco 25 · nota texto · agenda 14:30 Reunião · abrir notepad · notas"};
  var rows=Stack();var hint=T("foco 25  ·  nota …  ·  agenda 14:30 …  ·  abrir …  ·  notas",11,"#8E8892");hint.Margin=new Thickness(14,0,14,10);
  var items=new List<KeyValuePair<string,Action>>();int sel=0;
  Action refresh=()=>{items=CommandMatches(box.Text);if(sel>=items.Count)sel=0;rows.Children.Clear();
   for(int i=0;i<items.Count;i++)rows.Children.Add(new Border{CornerRadius=new CornerRadius(8),Margin=new Thickness(6,1,6,1),Padding=new Thickness(8,6,8,6),Background=i==sel?B("#33FFFFFF"):Brushes.Transparent,Child=T(items[i].Key,13)});
   hint.Visibility=box.Text.Trim().Length==0?Visibility.Visible:Visibility.Collapsed;};
  var stack=Stack();stack.Children.Add(box);stack.Children.Add(rows);stack.Children.Add(hint);
  cmdWin=new Window{WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,Topmost=true,ShowInTaskbar=false,ResizeMode=ResizeMode.NoResize,Width=560,SizeToContent=SizeToContent.Height,Opacity=0,FontFamily=w.FontFamily,
   Content=new Border{CornerRadius=new CornerRadius(18),Background=B("#F2201E24"),BorderBrush=B("#33FFFFFF"),BorderThickness=new Thickness(1),Margin=new Thickness(8),Child=stack}};
  var win=cmdWin;
  box.TextChanged+=(s,e)=>{sel=0;refresh();};
  box.PreviewKeyDown+=(s,e)=>{var k=e.Key;
   if(k==System.Windows.Input.Key.Escape){win.Close();e.Handled=true;}
   else if(k==System.Windows.Input.Key.Down&&items.Count>0){sel=(sel+1)%items.Count;refresh();e.Handled=true;}
   else if(k==System.Windows.Input.Key.Up&&items.Count>0){sel=(sel+items.Count-1)%items.Count;refresh();e.Handled=true;}
   else if(k==System.Windows.Input.Key.Enter){e.Handled=true;if(items.Count==0)return;var run=items[sel].Value;win.Close();w.Dispatcher.BeginInvoke(new Action(()=>{try{run();}catch(Exception ex){MessageBox.Show(ex.Message,"Tito");}}));}};
  win.Deactivated+=(s,e)=>win.Close();win.Closed+=(s,e)=>{if(cmdWin==win)cmdWin=null;};
  refresh();win.Show();
  var area=Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea;double scale=VisualTreeHelper.GetDpi(win).DpiScaleX;int width=(int)(win.ActualWidth*scale);
  SetWindowPos(new WindowInteropHelper(win).Handle,IntPtr.Zero,area.X+(area.Width-width)/2,area.Y+area.Height/5,0,0,0x15); // SWP_NOSIZE|NOZORDER|NOACTIVATE, em pixels da tela do mouse
  win.Opacity=1;win.Activate();box.Focus();
 }
}
}
