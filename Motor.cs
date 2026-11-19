using System;
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace Tito {
// Embedded local AI: Robo downloads llama.cpp (llama-server, CPU) and a small GGUF model into Dados/ on first use,
// verifies SHA-256, and talks to it on 127.0.0.1 only. Nothing is committed to the repo; nothing leaves the PC except the two downloads.
public partial class App {
 class MotorModel{public string Id,Name,File,Url,Sha;public long Size;public double FreeGB;}
 class MotorException:Exception{public MotorException(string m):base(m){}}
 static readonly MotorModel[] motorModels={
  new MotorModel{Id="qwen15",Name="Qwen2.5 1.5B (leve)",File="qwen2.5-1.5b-instruct-q4_k_m.gguf",Url="https://huggingface.co/Qwen/Qwen2.5-1.5B-Instruct-GGUF/resolve/main/qwen2.5-1.5b-instruct-q4_k_m.gguf",Sha="6a1a2eb6d15622bf3c96857206351ba97e1af16c30d7a74ee38970e434e9407e",Size=1117320736,FreeGB=2.5},
  new MotorModel{Id="qwen3b",Name="Qwen2.5 3B (melhor)",File="qwen2.5-3b-instruct-q4_k_m.gguf",Url="https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf",Sha="626b4a6678b86442240e33df819e00132d3ba7dddfe1cdc4fbb18e0a9615c62d",Size=2104932768,FreeGB=4.0}};
 // Pinned engine build (CPU, x64). Update tag/url/hash together.
 const string EngineTag="b12435",EngineUrl="https://github.com/ggml-org/llama.cpp/releases/download/b12435/llama-b12435-bin-win-cpu-x64.zip",EngineSha="c186d52379d7b1c269667ff61c6a8677bcc4e1866c460d3fbbf236674fbc8203";const long EngineSize=25212529;
 static event Action<string,double> MotorProgress;
 static readonly SemaphoreSlim motorGate=new SemaphoreSlim(1,1);
 static Process motorProc;static string motorBase,motorLoaded;static readonly StringBuilder motorLog=new StringBuilder();
 static string MotorDir(){return System.IO.Path.Combine(dataDir,"motor");}
 static bool IsOllama(){return localModel.Length>0&&!localModel.StartsWith("tito:");}
 static string OllamaName(){return localModel.StartsWith("ollama:")?localModel.Substring(7):localModel;}
 static MotorModel MotorChoice(){string id=localModel.StartsWith("tito:")?localModel.Substring(5):"";foreach(var m in motorModels)if(m.Id==id)return m;return motorModels[0];}
 static string ModelPath(MotorModel m){return System.IO.Path.Combine(MotorDir(),"modelos",m.File);}
 static string MotorLabel(){return IsOllama()?"Ollama · "+OllamaName():"Tito · "+MotorChoice().Name;}
 static string GB(long b){return (b/1073741824.0).ToString("0.0")+" GB";}
 static string MB(long b){return (b/1048576.0).ToString("0")+" MB";}
 static void MotorReport(string text,double p){var h=MotorProgress;if(h!=null)h(text,p);}

 [StructLayout(LayoutKind.Sequential)] class MemStatus{public uint dwLength=(uint)Marshal.SizeOf(typeof(MemStatus));public uint dwMemoryLoad;public ulong TotalPhys,AvailPhys,TotalPage,AvailPage,TotalVirt,AvailVirt,AvailExt;}
 [DllImport("kernel32.dll")] static extern bool GlobalMemoryStatusEx([In,Out] MemStatus s);
 static double FreeRamGB(){var s=new MemStatus();return GlobalMemoryStatusEx(s)?s.AvailPhys/1073741824.0:99;}

 static string MotorExe(){string d=System.IO.Path.Combine(MotorDir(),"engine-"+EngineTag);if(!Directory.Exists(d))return null;var f=Directory.GetFiles(d,"llama-server.exe",SearchOption.AllDirectories);return f.Length>0?f[0]:null;}
 static bool MotorReady(){return MotorExe()!=null&&File.Exists(ModelPath(MotorChoice()));}
 static string MotorStatusText(){
  if(IsOllama())return "Usando o Ollama deste PC ("+OllamaName()+").";
  var m=MotorChoice();long need=(MotorExe()==null?EngineSize:0)+(File.Exists(ModelPath(m))?0:m.Size);
  return need==0?"IA embutida pronta. Tudo roda neste PC, sem enviar dados.":"Na primeira conversa o Tito baixa a IA sozinho (~"+(need>=1073741824?GB(need):MB(need))+", uma vez só) e roda tudo neste PC.";
 }

 static Task<string> MotorEnsure(MotorModel m){return Task.Run(()=>{motorGate.Wait();try{return MotorPrepare(m);}finally{motorGate.Release();}});}

 static string MotorPrepare(MotorModel m){
  if(motorProc!=null&&!motorProc.HasExited&&motorLoaded==m.Id&&motorBase!=null)return motorBase;
  MotorStop();Directory.CreateDirectory(MotorDir());
  string exe=MotorExe();bool haveModel=File.Exists(ModelPath(m));
  long need=(exe==null?EngineSize:0)+(haveModel?0:m.Size);
  if(need>0){var drive=new DriveInfo(System.IO.Path.GetPathRoot(System.IO.Path.GetFullPath(MotorDir())));if(drive.AvailableFreeSpace<need+536870912)throw new MotorException("Falta espaço em disco: preciso de ~"+GB(need+536870912)+" livres em "+drive.Name+" para baixar a IA.");}
  double free=FreeRamGB();if(free<m.FreeGB)throw new MotorException(String.Format("Pouca memória livre: o modelo {0} precisa de ~{1:0.#} GB livres e há {2:0.#} GB agora. Feche programas ou escolha o modelo menor em Settings.",m.Name,m.FreeGB,free));
  if(exe==null){
   string zip=System.IO.Path.Combine(MotorDir(),"engine-"+EngineTag+".zip");
   MotorDownload("Baixando o motor de IA",EngineUrl,zip,EngineSha);
   MotorReport("Instalando o motor…",-1);
   string tmp=System.IO.Path.Combine(MotorDir(),"engine-tmp");if(Directory.Exists(tmp))Directory.Delete(tmp,true);Directory.CreateDirectory(tmp);
   string root=System.IO.Path.GetFullPath(tmp)+System.IO.Path.DirectorySeparatorChar;
   using(var za=System.IO.Compression.ZipFile.OpenRead(zip))foreach(var e in za.Entries){
    if(e.Name.Length==0)continue;string dest=System.IO.Path.GetFullPath(System.IO.Path.Combine(tmp,e.FullName));
    if(!dest.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new MotorException("Pacote do motor inválido.");
    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));System.IO.Compression.ZipFileExtensions.ExtractToFile(e,dest,true);}
   Directory.Move(tmp,System.IO.Path.Combine(MotorDir(),"engine-"+EngineTag));File.Delete(zip);exe=MotorExe();
   if(exe==null)throw new MotorException("O pacote do motor não contém llama-server.exe.");
  }
  if(!haveModel)MotorDownload("Baixando "+m.Name,m.Url,ModelPath(m),m.Sha);
  MotorReport("Iniciando a IA…",-1);
  int port;var l=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);l.Start();port=((IPEndPoint)l.LocalEndpoint).Port;l.Stop();
  var psi=new ProcessStartInfo(exe,"-m \""+ModelPath(m)+"\" --host 127.0.0.1 --port "+port+" -c 4096 -t "+Math.Max(2,Environment.ProcessorCount/2)){WorkingDirectory=System.IO.Path.GetDirectoryName(exe),UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
  motorLog.Length=0;Process p;
  try{p=Process.Start(psi);}catch(System.ComponentModel.Win32Exception ex){throw new MotorException("O Windows impediu a execução do motor de IA ("+ex.Message.Trim()+"). Smart App Control, SmartScreen ou antivírus costumam bloquear programas sem assinatura. Veja docs/MOTOR_IA.md; enquanto isso, o Robo também funciona com o Ollama, se instalado.");}
  p.ErrorDataReceived+=(s,e)=>{lock(motorLog){if(e.Data!=null){motorLog.AppendLine(e.Data);if(motorLog.Length>6000)motorLog.Remove(0,motorLog.Length-4000);}}};p.OutputDataReceived+=(s,e)=>{};p.BeginErrorReadLine();p.BeginOutputReadLine();
  try{File.WriteAllText(System.IO.Path.Combine(MotorDir(),"pid.txt"),p.Id.ToString());}catch{}
  string b="http://127.0.0.1:"+port;var until=DateTime.UtcNow.AddMinutes(4);
  while(true){
   if(p.HasExited){string tail;lock(motorLog)tail=motorLog.ToString().Trim();if(tail.Length>400)tail=tail.Substring(tail.Length-400);throw new MotorException("O motor de IA fechou ao iniciar (código "+p.ExitCode+"). Pode ser falta de memória, bloqueio do Windows ou arquivo corrompido (apague a pasta Dados\\motor para baixar de novo). "+tail);}
   if(DateTime.UtcNow>until){MotorStop();throw new MotorException("O motor de IA demorou demais para iniciar.");}
   try{if(HttpGet(b+"/health",2000).Length>=0)break;}catch{Thread.Sleep(500);}
  }
  motorProc=p;motorBase=b;motorLoaded=m.Id;return b;
 }

 static void MotorStop(){try{if(motorProc!=null&&!motorProc.HasExited)motorProc.Kill();}catch{}motorProc=null;motorBase=null;motorLoaded=null;}
 // A previous Tito that was killed can leave llama-server running; stop it by the pid we saved.
 static void MotorKillStale(){try{string f=System.IO.Path.Combine(MotorDir(),"pid.txt");if(!File.Exists(f))return;var p=Process.GetProcessById(int.Parse(File.ReadAllText(f).Trim()));if(p.ProcessName.StartsWith("llama-server",StringComparison.OrdinalIgnoreCase))p.Kill();File.Delete(f);}catch{}}

 static string HttpGet(string url,int timeout){var r=(HttpWebRequest)WebRequest.Create(url);r.Timeout=timeout;r.Proxy=null;using(var res=r.GetResponse())using(var rd=new StreamReader(res.GetResponseStream()))return rd.ReadToEnd();}

 // Resumable download (.part + Range), then SHA-256 check before the file gets its final name.
 static void MotorDownload(string label,string url,string dest,string sha){
  Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));string part=dest+".part";
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
  for(int attempt=0;;attempt++){
   try{
    long have=File.Exists(part)?new FileInfo(part).Length:0;
    var req=(HttpWebRequest)WebRequest.Create(url);req.UserAgent="Tito";req.Timeout=30000;req.ReadWriteTimeout=30000;if(have>0)req.AddRange(have);
    using(var res=(HttpWebResponse)req.GetResponse()){
     bool resumed=res.StatusCode==HttpStatusCode.PartialContent;if(!resumed)have=0;long total=res.ContentLength>0?have+res.ContentLength:-1;
     using(var inp=res.GetResponseStream())using(var fs=new FileStream(part,resumed?FileMode.Append:FileMode.Create,FileAccess.Write)){
      var buf=new byte[81920];int n;long got=have;var last=DateTime.MinValue;
      while((n=inp.Read(buf,0,buf.Length))>0){fs.Write(buf,0,n);got+=n;if((DateTime.UtcNow-last).TotalMilliseconds>250){last=DateTime.UtcNow;MotorReport(label+" · "+MB(got)+(total>0?" / "+MB(total):""),total>0?(double)got/total:-1);}}}}
    break;
   }catch(WebException ex){
    var hr=ex.Response as HttpWebResponse;if(hr!=null&&(int)hr.StatusCode==416)break; // .part is already complete
    if(attempt>=3)throw new MotorException("Falha no download ("+ex.Message+"). Confira a internet e tente de novo: ele continua de onde parou.");Thread.Sleep(2000*(attempt+1));
   }catch(IOException ex){if(attempt>=3)throw new MotorException("Falha ao gravar o download: "+ex.Message);Thread.Sleep(2000);}
  }
  MotorReport("Verificando o arquivo…",-1);string h;
  using(var s=File.OpenRead(part))h=BitConverter.ToString(SHA256.Create().ComputeHash(s)).Replace("-","").ToLowerInvariant();
  if(!h.Equals(sha,StringComparison.OrdinalIgnoreCase)){File.Delete(part);throw new MotorException("O arquivo baixado não confere com o hash esperado e foi descartado. Tente de novo.");}
  if(File.Exists(dest))File.Delete(dest);File.Move(part,dest);
 }

 static async Task<string> MotorChat(List<Dictionary<string,string>> messages,Action<string> onText=null){
  string b=await MotorEnsure(MotorChoice());
  string payload=json.Serialize(new{messages=messages,stream=true,max_tokens=600,temperature=0.7});
  return await Task.Run(()=>PostStream(b+"/v1/chat/completions",payload,300000,line=>{
   if(!line.StartsWith("data:"))return null;string d=line.Substring(5).Trim();if(d=="[DONE]")return null;
   var delta=(Dictionary<string,object>)((Dictionary<string,object>)((System.Collections.IList)json.Deserialize<Dictionary<string,object>>(d)["choices"])[0])["delta"];
   return delta.ContainsKey("content")?Convert.ToString(delta["content"]):null;},onText));
 }
 // POSTs and reads the reply line by line; deltaOf turns one line into new text (or null). Returns the full text.
 static string PostStream(string url,string payload,int timeout,Func<string,string> deltaOf,Action<string> onText){
  var r=(HttpWebRequest)WebRequest.Create(url);r.Method="POST";r.ContentType="application/json";r.Timeout=timeout;r.ReadWriteTimeout=timeout;r.Proxy=null;byte[] d=Encoding.UTF8.GetBytes(payload);r.ContentLength=d.Length;using(var s=r.GetRequestStream())s.Write(d,0,d.Length);
  var sb=new StringBuilder();using(var res=r.GetResponse())using(var rd=new StreamReader(res.GetResponseStream())){string line;while((line=rd.ReadLine())!=null){string t=deltaOf(line);if(String.IsNullOrEmpty(t))continue;sb.Append(t);if(onText!=null)onText(sb.ToString());}}
  return sb.ToString().Trim();
 }

 // `Tito.exe --motor-test` (CI): full real path, download -> hash -> extract -> start -> one chat turn.
 static int MotorSelfTest(){
  string report=System.IO.Path.Combine(dataDir,"motor-test.txt");
  try{MotorProgress+=(t,p)=>Console.WriteLine(t);localModel="tito:qwen15";string a=Task.Run(()=>MotorChat(new List<Dictionary<string,string>>{new Dictionary<string,string>{{"role","user"},{"content","Diga olá em português, em uma frase."}}})).Result;MotorStop();File.WriteAllText(report,(a.Length>0?"PASS: ":"FAIL: ")+a);return a.Length>0?0:1;}
  catch(Exception ex){MotorStop();var e=ex is AggregateException?ex.InnerException:ex;File.WriteAllText(report,"FAIL: "+e.Message);return 1;}
 }

 // Progress UI: status line + thin bar inside the Robo chat, mirrored in Settings.
 static StackPanel chatProgress;static TextBlock chatProgressText;static System.Windows.Controls.ProgressBar chatProgressBar;
 static void MotorShowProgress(bool on){
  if(chatMessages==null)return;
  if(!on){if(chatProgress!=null)chatMessages.Children.Remove(chatProgress);chatProgress=null;return;}
  chatProgressText=T("Preparando a IA…",11,"#BDB4C7");chatProgressBar=new System.Windows.Controls.ProgressBar{Height=4,Minimum=0,Maximum=100,IsIndeterminate=true,Margin=new Thickness(0,4,0,4),MaxWidth=300,HorizontalAlignment=HorizontalAlignment.Left};
  chatProgress=Stack();chatProgress.Children.Add(chatProgressText);chatProgress.Children.Add(chatProgressBar);chatMessages.Children.Add(chatProgress);if(chatScroll!=null)chatScroll.ScrollToEnd();
 }
 static void MotorUi(string text,double p){
  w.Dispatcher.BeginInvoke(new Action(()=>{
   if(chatProgress!=null){chatProgressText.Text=text;chatProgressBar.IsIndeterminate=p<0;if(p>=0)chatProgressBar.Value=p*100;}
   if(modelStatus!=null)modelStatus.Text=p>=0?text+" ("+(int)(p*100)+"%)":text;}));
 }
 static async void PrepareMotor(){
  if(IsOllama()){modelStatus.Text=MotorStatusText();return;}
  MotorProgress+=MotorUi;
  try{await MotorEnsure(MotorChoice());modelStatus.Text="IA embutida pronta. Abra Robo para conversar.";}
  catch(Exception ex){if(modelStatus!=null)modelStatus.Text=ex.Message;}
  finally{MotorProgress-=MotorUi;}
 }
}
}
