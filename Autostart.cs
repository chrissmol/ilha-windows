using System;
using System.IO;
using Microsoft.Win32;
namespace Ilha {
public partial class App {
 // Start with Windows: HKCU Run (no admin) pointing at the stable launcher Ilha.exe, which opens the current version.
 const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run",RunName="Tito";
 static string AutostartCommand(){return "\""+System.IO.Path.Combine(baseDir,"Ilha.exe")+"\" --boot";}
 static bool AutostartOn(){try{using(var k=Registry.CurrentUser.OpenSubKey(RunKey))return k!=null&&k.GetValue(RunName)!=null;}catch{return false;}}
 static void SetAutostart(bool on){
  try{
   using(var k=Registry.CurrentUser.CreateSubKey(RunKey)){if(on)k.SetValue(RunName,AutostartCommand());else k.DeleteValue(RunName,false);}
   File.WriteAllText(System.IO.Path.Combine(dataDir,"autostart.txt"),on?"on":"off");
  }catch(Exception ex){error="Não consegui "+(on?"ativar":"desativar")+" o início com o Windows. "+ex.Message;ShowError();}
 }
 // First run: on by default. Later runs: respect the choice and repair the path if the folder moved.
 static void AutostartInit(){
  string choice=System.IO.Path.Combine(dataDir,"autostart.txt");
  try{
   if(!File.Exists(choice)||File.ReadAllText(choice).Trim()!="off"){
    using(var k=Registry.CurrentUser.CreateSubKey(RunKey)){if(!String.Equals(k.GetValue(RunName) as string,AutostartCommand(),StringComparison.OrdinalIgnoreCase))k.SetValue(RunName,AutostartCommand());}
    if(!File.Exists(choice))File.WriteAllText(choice,"on");
   }
  }catch{}
 }
}
}
