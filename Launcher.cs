using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
class Launcher {
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern int SetCurrentProcessExplicitAppUserModelID(string id);
 [STAThread] static void Main(string[] args){try{
  string root=AppDomain.CurrentDomain.BaseDirectory;bool boot=args.Length>0&&args[0]=="--boot";if(boot)Thread.Sleep(10000); // wait for desktop/monitors after logon
  SetCurrentProcessExplicitAppUserModelID("Tito.Desktop");
  if(args.Length==2&&args[0]=="--wait"){int pid;if(Int32.TryParse(args[1],out pid))try{using(var old=Process.GetProcessById(pid)){old.WaitForExit(8000);}}catch{}}
  string current=File.ReadAllText(Path.Combine(root,"current-version.txt")).Trim();string executable=Path.GetFullPath(Path.Combine(root,current));
  if(!executable.StartsWith(Path.GetFullPath(root),StringComparison.OrdinalIgnoreCase)||!executable.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))throw new Exception("Caminho da versão inválido.");
  string active=Path.Combine(root,"Dados","active.txt");bool wait=false;int oldPid=0;
  if(File.Exists(active)){var lines=File.ReadAllLines(active);if(lines.Length>=2&&Int32.TryParse(lines[0],out oldPid)){
   try{using(var p=Process.GetProcessById(oldPid)){if(p.ProcessName.StartsWith("TitoGlass",StringComparison.OrdinalIgnoreCase)&&!p.HasExited){
    if(String.Equals(Path.GetFullPath(lines[1]),executable,StringComparison.OrdinalIgnoreCase)){
     try{using(var signal=EventWaitHandle.OpenExisting("Local\\TitoGlassOpen")){if(!boot)signal.Set();return;}}catch{}
    }else{try{using(var signal=EventWaitHandle.OpenExisting("Local\\TitoGlassRestart")){signal.Set();wait=true;}}catch{}}
    if(wait)p.WaitForExit(8000);
   }}}catch{}
  }}
  var start=new ProcessStartInfo(executable){WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true};start.EnvironmentVariables["TITO_ROOT"]=root;Process.Start(start);
 }catch(Exception ex){MessageBox.Show("Não foi possível abrir o Tito.\n"+ex.Message,"Tito",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
}
