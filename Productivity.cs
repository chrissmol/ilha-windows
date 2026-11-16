using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace Tito {
public partial class App {
 // Notes, clipboard history and a local agenda. Nothing here leaves the machine.
 static readonly List<string> clipItems=new List<string>();static string lastClip="";static TextBox notesBox;
 [System.Runtime.InteropServices.DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags);
 static bool keepAwake,batteryLow,batteryKnown,wasCharging;static DateTime curiousUntil,typingUntil,dizzyUntil,excitedUntil,loveUntil,winkUntil,agentBusyUntil,nextQuirk=DateTime.UtcNow.AddSeconds(10),quirkUntil;static Point quirkGaze;
 static readonly Queue<DateTime> taps=new Queue<DateTime>();
 // Tito reacts to taps: a giggle, and dizzy after five quick ones.
 static void MascotTap(){
  var now=DateTime.UtcNow;taps.Enqueue(now);while(taps.Count>0&&now-taps.Peek()>TimeSpan.FromSeconds(3))taps.Dequeue();
  if(taps.Count>=5){dizzyUntil=now.AddSeconds(3);taps.Clear();}else{celebrateUntil=now.AddMilliseconds(1400);if(headerMascot!=null)headerMascot.Hop(1);}
 }
 static void SetKeepAwake(bool on){keepAwake=on;SetThreadExecutionState(on?0x80000003u:0x80000000u);}
 // Resting-pill battery: only when charging or low; desktops without a battery return null.
 static string BatteryText(out string color){
  color=null;var ps=System.Windows.Forms.SystemInformation.PowerStatus;
  if((ps.BatteryChargeStatus&(System.Windows.Forms.BatteryChargeStatus.NoSystemBattery|System.Windows.Forms.BatteryChargeStatus.Unknown))!=0)return null;
  int pct=(int)Math.Round(ps.BatteryLifePercent*100);bool charging=ps.PowerLineStatus==System.Windows.Forms.PowerLineStatus.Online;
  batteryLow=!charging&&pct<=20;if(batteryKnown&&charging&&!wasCharging)loveUntil=DateTime.UtcNow.AddSeconds(2.5);batteryKnown=true;wasCharging=charging;
  if(charging&&pct<100){color="#30D158";return "⚡ "+pct+"%";}
  if(!charging&&pct<=30){color=pct<=20?"#FF453A":"#FFB000";return pct+"%";}
  return null;
 }
 static string NotesPath(){return Path.Combine(dataDir,"notes.txt");}
 static string AgendaPath(){return Path.Combine(dataDir,"agenda.txt");}
 [System.Runtime.InteropServices.DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();static uint clipSeq;
 static void ClipboardTick(){
  uint seq=GetClipboardSequenceNumber();if(seq==clipSeq)return;clipSeq=seq;
  try{
   if(!Clipboard.ContainsText()||Clipboard.ContainsData("ExcludeClipboardContentFromMonitorProcessing"))return;
   string text=Clipboard.GetText();if(String.IsNullOrWhiteSpace(text)||text==lastClip||text.Length>2000)return;
   curiousUntil=DateTime.UtcNow.AddMilliseconds(1500);lastClip=text;clipItems.Remove(text);clipItems.Insert(0,text);if(clipItems.Count>15)clipItems.RemoveAt(15);if(page=="clipboard")Switch("clipboard");
  }catch{}
 }
 static StackPanel Panel(string title,out ScrollViewer scroll,Button action=null){
  var root=new Grid();root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());view.Children.Add(root);
  var header=new DockPanel{Margin=new Thickness(0,0,0,6)};if(action!=null){DockPanel.SetDock(action,Dock.Right);header.Children.Add(action);}var heading=T(title,19,PageHex(page));heading.FontWeight=FontWeights.SemiBold;header.Children.Add(heading);root.Children.Add(header);
  scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetRow(scroll,1);root.Children.Add(scroll);var rows=Stack();scroll.Content=rows;return rows;
 }
 static void Notes(){
  ScrollViewer sc;var rows=Panel("Notas",out sc);
  string saved="";try{if(File.Exists(NotesPath()))saved=File.ReadAllText(NotesPath());}catch{}
  notesBox=new TextBox{Text=saved,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Background=B("#18FFFFFF"),Foreground=B("#FFF8F6"),CaretBrush=Brushes.White,BorderThickness=new Thickness(0),Padding=new Thickness(10),FontSize=13,MinHeight=130,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  notesBox.TextChanged+=(s,e)=>{typingUntil=DateTime.UtcNow.AddSeconds(3);try{File.WriteAllText(NotesPath(),notesBox.Text);}catch{}};rows.Children.Add(notesBox);
 }
 static void ClipboardPage(){
  ScrollViewer sc;var rows=Panel("Área de transferência",out sc,Btn("Limpar",()=>{clipItems.Clear();Switch("clipboard");}));
  if(clipItems.Count==0)rows.Children.Add(T("Copie um texto e ele aparece aqui. Fica só na memória.",13,"#BDBDC5"));
  foreach(string item in clipItems.ToArray()){string text=item;string line=text.Replace("\r"," ").Replace("\n"," ");var b=Btn(line.Length>70?line.Substring(0,70)+"…":line,()=>{lastClip=text;Clipboard.SetText(text);celebrateUntil=DateTime.UtcNow.AddSeconds(2);},"Copiar de novo");b.HorizontalContentAlignment=HorizontalAlignment.Left;b.Margin=new Thickness(0,1,0,1);rows.Children.Add(b);}
 }
 static List<string[]> AgendaEntries(){
  var list=new List<string[]>();try{if(File.Exists(AgendaPath()))foreach(string line in File.ReadAllLines(AgendaPath())){var p=line.Split(new[]{'|'},3);if(p.Length==3)list.Add(p);}}catch{}
  return list.OrderBy(p=>p[0]+p[1]).ToList();
 }
 static void Agenda(){
  ScrollViewer sc;var rows=Panel("Agenda",out sc);
  var add=new TextBox{Background=B("#18FFFFFF"),Foreground=B("#FFF8F6"),CaretBrush=Brushes.White,BorderThickness=new Thickness(0),Padding=new Thickness(8,5,8,5),FontSize=12,ToolTip="Hoje: 14:30 Reunião  ·  ou: 2026-11-20 09:00 Dentista",Margin=new Thickness(0,0,0,6)};
  add.KeyDown+=(s,e)=>{if(e.Key!=System.Windows.Input.Key.Enter||String.IsNullOrWhiteSpace(add.Text))return;
   var m=System.Text.RegularExpressions.Regex.Match(add.Text.Trim(),@"^(?:(\d{4}-\d{2}-\d{2})\s+)?(\d{1,2}:\d{2})\s+(.+)$");if(!m.Success){add.ToolTip="Use: HH:mm título";return;}
   string date=m.Groups[1].Success?m.Groups[1].Value:DateTime.Now.ToString("yyyy-MM-dd");string time=m.Groups[2].Value.PadLeft(5,'0');
   try{File.AppendAllText(AgendaPath(),date+"|"+time+"|"+m.Groups[3].Value.Replace("|"," ")+"\n");}catch{}celebrateUntil=DateTime.UtcNow.AddSeconds(2);Switch("agenda");};
  rows.Children.Add(add);
  string today=DateTime.Now.ToString("yyyy-MM-dd");var upcoming=AgendaEntries().Where(p=>String.CompareOrdinal(p[0],today)>=0).Take(12).ToList();
  if(upcoming.Count==0)rows.Children.Add(T("Nada marcado. Digite “14:30 Reunião” e Enter.",13,"#BDBDC5"));
  foreach(var p in upcoming){var row=Stack(Orientation.Horizontal);string when=p[0]==today?p[1]:DateTime.ParseExact(p[0],"yyyy-MM-dd",CultureInfo.InvariantCulture).ToString("dd/MM",CultureInfo.InvariantCulture)+" "+p[1];
   var t=T(when,12,p[0]==today?"#FFB36B":"#AFA9B4");t.Width=84;row.Children.Add(t);row.Children.Add(T(p[2],13));
   var entry=p;var done=Btn("✕",()=>{try{File.WriteAllLines(AgendaPath(),AgendaEntries().Where(x=>!(x[0]==entry[0]&&x[1]==entry[1]&&x[2]==entry[2])).Select(x=>String.Join("|",x)).ToArray());}catch{}Switch("agenda");},"Remover");done.Padding=new Thickness(6,2,6,2);row.Children.Add(done);row.Margin=new Thickness(0,3,0,3);rows.Children.Add(row);}
 }
}
}
