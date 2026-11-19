using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using Forms=System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Media;
namespace Tito {
public partial class App {
 // Notes, clipboard history and a local agenda. Nothing here leaves the machine.
 static readonly List<string> clipItems=new List<string>();static string lastClip="";static TextBox notesBox;
 [System.Runtime.InteropServices.DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags);
 static bool keepAwake,batteryLow,batteryKnown,wasCharging;static DateTime curiousUntil,typingUntil,dizzyUntil,excitedUntil,loveUntil,winkUntil,agentBusyUntil,nextQuirk=DateTime.UtcNow.AddSeconds(10),quirkUntil;static Point quirkGaze;static bool agendaSoon,cpuHigh;static long cpuIdle,cpuTotal;static int cpuStreak;static DateTime cpuAt;static DateTime welcomeUntil=DateTime.UtcNow.AddSeconds(3),chatDoneUntil;static bool chatWasBusy;
 [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool GetSystemTimes(out long idle,out long kernel,out long user);
 // CPU quase cheia por ~9 s: Tito fica preocupado. Uma leitura barata a cada 3 s.
 static void CpuTick(){
  var now=DateTime.UtcNow;if(now-cpuAt<TimeSpan.FromSeconds(3))return;cpuAt=now;
  long i,k,u;if(!GetSystemTimes(out i,out k,out u))return;long total=k+u;
  if(cpuTotal>0&&total>cpuTotal){double busy=1-(double)(i-cpuIdle)/(total-cpuTotal);cpuStreak=busy>=.92?cpuStreak+1:0;cpuHigh=cpuStreak>=3;}
  cpuIdle=i;cpuTotal=total;
 }
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
  if(charging&&pct<100){color=Theme.Ok;return "⚡ "+pct+"%";}
  if(!charging&&pct<=30){color=pct<=20?Theme.Bad:"#FFB000";return pct+"%";}
  return null;
 }
 // Pomodoro: foco 25 min, pausa 5 (15 a cada 4 ciclos); cada fim vai para focus-log.txt.
 static int focusCycles;static bool onBreak;
 static void FocusEnded(){
  try{File.AppendAllText(Path.Combine(dataDir,"focus-log.txt"),DateTime.Now.ToString("yyyy-MM-dd HH:mm")+"|"+(onBreak?"pausa":"foco")+"\n");}catch{}
  if(!onBreak)focusCycles++;onBreak=!onBreak;remaining=onBreak?(focusCycles%4==0?900:300):1500;
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
 static string notesPending;static System.Windows.Threading.DispatcherTimer notesSave;
 // Grava a nota 600 ms depois da última tecla (e ao sair da aba/fechar), não a cada tecla.
 static void FlushNotes(){if(notesSave!=null)notesSave.Stop();if(notesPending==null)return;try{File.WriteAllText(NotesPath(),notesPending);notesPending=null;}catch{}}
 static void Notes(){
  FlushNotes();ScrollViewer sc;var rows=Panel("Notas",out sc);
  string saved="";try{if(File.Exists(NotesPath()))saved=File.ReadAllText(NotesPath());}catch{}
  notesBox=new TextBox{Text=saved,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Background=B("#18FFFFFF"),Foreground=B(Theme.Text),CaretBrush=Brushes.White,BorderThickness=new Thickness(0),Padding=new Thickness(10),FontSize=13,MinHeight=130,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  notesBox.TextChanged+=(s,e)=>{typingUntil=DateTime.UtcNow.AddSeconds(3);notesPending=notesBox.Text;if(notesSave==null){notesSave=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(600)};notesSave.Tick+=(a,b)=>FlushNotes();w.Closed+=(a,b)=>FlushNotes();}notesSave.Stop();notesSave.Start();};rows.Children.Add(notesBox);
 }
 // Só os itens fixados vão para o disco (clip-pinned.json); o histórico continua só na memória.
 static List<string> clipPinned;static string clipFilter="";
 static string PinPath(){return Path.Combine(dataDir,"clip-pinned.json");}
 static List<string> Pins(){if(clipPinned==null){clipPinned=new List<string>();try{if(File.Exists(PinPath()))clipPinned=json.Deserialize<List<string>>(File.ReadAllText(PinPath()));}catch{}}return clipPinned;}
 static void SavePins(){try{File.WriteAllText(PinPath(),json.Serialize(Pins()));}catch{}}
 static void ClipboardPage(){
  ScrollViewer sc;var rows=Panel("Área de transferência",out sc,Btn("Limpar",()=>{clipItems.Clear();Switch("clipboard");},"Limpa o histórico; os fixados ficam"));
  var search=new TextBox{Text=clipFilter,Background=B("#18FFFFFF"),Foreground=B(Theme.Text),CaretBrush=Brushes.White,BorderThickness=new Thickness(0),Padding=new Thickness(8,5,8,5),FontSize=12,ToolTip="Digite e aperte Enter para filtrar",Margin=new Thickness(0,0,0,6)};
  search.KeyDown+=(s,e)=>{if(e.Key!=System.Windows.Input.Key.Enter)return;clipFilter=search.Text.Trim();Switch("clipboard");};rows.Children.Add(search);
  Func<string,bool> match=t=>clipFilter==""||t.IndexOf(clipFilter,StringComparison.CurrentCultureIgnoreCase)>=0;
  var pins=Pins().Where(match).ToList();var recent=clipItems.Where(t=>match(t)&&!Pins().Contains(t)).ToList();
  if(pins.Count==0&&recent.Count==0)rows.Children.Add(T(clipFilter==""?"Copie um texto e ele aparece aqui. O histórico fica só na memória; fixe (📌) o que quiser guardar.":"Nada encontrado.",13,"#BDBDC5"));
  foreach(string item in pins.Concat(recent)){string text=item;bool pinned=Pins().Contains(text);string line=text.Replace("\r"," ").Replace("\n"," ");
   var row=new DockPanel{Margin=new Thickness(0,1,0,1)};
   var pin=Btn(pinned?"✕":"📌",()=>{if(pinned)Pins().Remove(text);else{Pins().Remove(text);Pins().Insert(0,text);if(Pins().Count>30)Pins().RemoveAt(30);}SavePins();Switch("clipboard");},pinned?"Desafixar":"Fixar (salva no disco)");DockPanel.SetDock(pin,Dock.Right);row.Children.Add(pin);
   var b=Btn((pinned?"📌 ":"")+(line.Length>64?line.Substring(0,64)+"…":line),()=>{lastClip=text;Clipboard.SetText(text);celebrateUntil=DateTime.UtcNow.AddSeconds(2);},"Copiar de novo");b.HorizontalContentAlignment=HorizontalAlignment.Left;row.Children.Add(b);rows.Children.Add(row);}
 }
 static List<string[]> AgendaEntries(){
  var list=new List<string[]>();try{if(File.Exists(AgendaPath()))foreach(string line in File.ReadAllLines(AgendaPath())){var p=line.Split(new[]{'|'},3);if(p.Length==3)list.Add(p);}}catch{}
  return list.OrderBy(p=>p[0]+p[1]).ToList();
 }
 static string agendaError="";static DateTime lastAgendaCheck;static readonly HashSet<string> agendaFired=new HashSet<string>();
 // "[hoje|amanhã|dd/MM|yyyy-MM-dd] 14:30|9h|9h30 título" -> date yyyy-MM-dd, time HH:mm.
 static bool ParseAgenda(string text,DateTime now,out string date,out string time,out string title){
  date=time=title="";
  var m=System.Text.RegularExpressions.Regex.Match((text??"").Trim(),@"^(?:(?<d>\d{4}-\d{2}-\d{2}|\d{1,2}/\d{1,2}|hoje|amanh[ãa])\s+)?(?<h>\d{1,2})(?::(?<m>\d{2})|h(?<m>\d{2})?)\s+(?<t>.+)$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
  if(!m.Success)return false;
  int h=int.Parse(m.Groups["h"].Value),min=m.Groups["m"].Success?int.Parse(m.Groups["m"].Value):0;if(h>23||min>59)return false;
  DateTime d=now.Date;string ds=m.Groups["d"].Value.ToLowerInvariant();
  if(ds.StartsWith("amanh"))d=d.AddDays(1);
  else if(ds.Contains("-")){if(!DateTime.TryParseExact(ds,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out d))return false;}
  else if(ds.Contains("/")){var p=ds.Split('/');int dd=int.Parse(p[0]),mo=int.Parse(p[1]);try{d=new DateTime(now.Year,mo,dd);if(d<now.Date)d=d.AddYears(1);}catch{return false;}}
  date=d.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);time=h.ToString("00")+":"+min.ToString("00");title=m.Groups["t"].Value;return true;
 }
 // Called from the 250 ms ticker: fires once per entry, only within 2 min after its time (so old entries don't ring when Tito opens).
 // Focus time done today ("yyyy-MM-dd|seconds" in focus.txt). Only the main island counts, so several screens do not add the same minutes twice.
 static double focusToday;static string focusDay="";static DateTime focusLast=DateTime.MinValue;static int focusSavedMin;static TextBlock focusLabel;
 static string FocusPath(){return Path.Combine(dataDir,"focus.txt");}
 static void FocusLoad(){
  string today=DateTime.Now.ToString("yyyy-MM-dd");focusDay=today;focusToday=0;
  try{if(File.Exists(FocusPath())){var p=File.ReadAllText(FocusPath()).Trim().Split('|');double sec;if(p.Length==2&&p[0]==today&&Double.TryParse(p[1],NumberStyles.Float,CultureInfo.InvariantCulture,out sec))focusToday=sec;}}catch{}
  focusSavedMin=(int)(focusToday/60);
 }
 static string FocusTodayText(){if(secondary||focusDay=="")FocusLoad();int m=(int)(focusToday/60);return "Hoje: "+(m>=60?(m/60)+" h "+(m%60)+" min":m+" min")+" de foco";}
 static void FocusTick(){
  if(secondary)return;
  if(focusDay!=DateTime.Now.ToString("yyyy-MM-dd"))FocusLoad();
  var now=DateTime.UtcNow;if(running&&focusLast!=DateTime.MinValue)focusToday+=Math.Min(2,(now-focusLast).TotalSeconds);
  bool stopped=!running&&focusLast!=DateTime.MinValue;focusLast=running?now:DateTime.MinValue;
  int min=(int)(focusToday/60);
  if(min!=focusSavedMin||stopped){focusSavedMin=min;try{File.WriteAllText(FocusPath(),focusDay+"|"+focusToday.ToString("F0",CultureInfo.InvariantCulture));}catch{}if(focusLabel!=null&&page=="focus")focusLabel.Text=FocusTodayText();}
 }
 static void AgendaReminderTick(){
  DateTime now=DateTime.Now;if(now-lastAgendaCheck<TimeSpan.FromSeconds(1))return;lastAgendaCheck=now;bool soon=false;
  foreach(var p in AgendaEntries()){DateTime at;if(!DateTime.TryParseExact(p[0]+" "+p[1],"yyyy-MM-dd HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out at))continue;
   if(at>now&&at-now<=TimeSpan.FromMinutes(10))soon=true;
   if(now<at||now-at>TimeSpan.FromMinutes(2)||!agendaFired.Add(p[0]+"|"+p[1]+"|"+p[2]))continue;
   excitedUntil=DateTime.UtcNow.AddSeconds(6);if(contextHidden||doNotDisturb)continue;
   System.Media.SystemSounds.Asterisk.Play();if(tray!=null)tray.ShowBalloonTip(5000,"Tito · "+p[1],p[2],Forms.ToolTipIcon.Info);Switch("agenda");Expand(true);}
  agendaSoon=soon;
 }
 static void Agenda(){
  ScrollViewer sc;var rows=Panel("Agenda",out sc);
  var add=new TextBox{Background=B("#18FFFFFF"),Foreground=B(Theme.Text),CaretBrush=Brushes.White,BorderThickness=new Thickness(0),Padding=new Thickness(8,5,8,5),FontSize=12,ToolTip="14:30 Reunião  ·  amanhã 9h Dentista  ·  25/11 18h30 Jantar  ·  2026-11-20 09:00 Médico",Margin=new Thickness(0,0,0,6)};
  add.KeyDown+=(s,e)=>{if(e.Key!=System.Windows.Input.Key.Enter||String.IsNullOrWhiteSpace(add.Text))return;
   string date,time,title;if(!ParseAgenda(add.Text,DateTime.Now,out date,out time,out title)){agendaError="Não entendi. Tente: 14:30 Reunião · amanhã 9h Dentista · 25/11 18h30 Jantar";Switch("agenda");return;}
   agendaError="";try{File.AppendAllText(AgendaPath(),date+"|"+time+"|"+title.Replace("|"," ")+"\n");}catch{agendaError="Não consegui salvar a agenda.";}celebrateUntil=DateTime.UtcNow.AddSeconds(2);Switch("agenda");};
  rows.Children.Add(add);
  if(agendaError!="")rows.Children.Add(T(agendaError,12,"#FF453A"));
  string today=DateTime.Now.ToString("yyyy-MM-dd");var upcoming=AgendaEntries().Where(p=>String.CompareOrdinal(p[0],today)>=0).Take(12).ToList();
  if(upcoming.Count==0)rows.Children.Add(T("Nada marcado. Digite “14:30 Reunião” e Enter.",13,"#BDBDC5"));
  foreach(var p in upcoming){var row=Stack(Orientation.Horizontal);string when=p[0]==today?p[1]:DateTime.ParseExact(p[0],"yyyy-MM-dd",CultureInfo.InvariantCulture).ToString("dd/MM",CultureInfo.InvariantCulture)+" "+p[1];
   var t=T(when,12,p[0]==today?"#FFB36B":Theme.Muted);t.Width=84;row.Children.Add(t);row.Children.Add(T(p[2],13));
   var entry=p;var done=Btn("✕",()=>{try{File.WriteAllLines(AgendaPath(),AgendaEntries().Where(x=>!(x[0]==entry[0]&&x[1]==entry[1]&&x[2]==entry[2])).Select(x=>String.Join("|",x)).ToArray());}catch{}Switch("agenda");},"Remover");done.Padding=new Thickness(6,2,6,2);row.Children.Add(done);row.Margin=new Thickness(0,3,0,3);rows.Children.Add(row);}
 }
}
}
