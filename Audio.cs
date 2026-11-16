using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace Tito {
// Core Audio COM contracts. Objects are scoped to each operation so changing
// the default endpoint or unplugging a device never leaves a cached endpoint.
public static class DesktopAudio {
 [ComImport,Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IPeak {void GetPeakValue(out float peak);}
 // Only normalized output peak values are read. No audio buffers or input
 // devices are opened. All COM objects live on the worker that created them.
 public sealed class PeakMonitor : IDisposable {
  public volatile float Level;public volatile bool Available,Enabled;public string Error="";
  readonly System.Threading.ManualResetEvent stop=new System.Threading.ManualResetEvent(false);readonly System.Threading.Thread worker;
  public PeakMonitor(){worker=new System.Threading.Thread(Read){IsBackground=true,Name="Tito output meter"};worker.SetApartmentState(System.Threading.ApartmentState.MTA);worker.Start();}
  void Read(){IDevices e=null;IDevice d=null;object meter=null;string id="";DateTime refresh=DateTime.MinValue;
   try{while(!stop.WaitOne(50)){
    if(!Enabled){Level=0;continue;}
    try{if(e==null)e=(IDevices)new Enumerator();if(DateTime.UtcNow>=refresh){refresh=DateTime.UtcNow.AddSeconds(1);IDevice next=null;try{e.GetDefaultAudioEndpoint(0,1,out next);string nextId;next.GetId(out nextId);if(nextId!=id||meter==null){Release(meter);meter=null;Release(d);d=next;next=null;var iid=typeof(IPeak).GUID;d.Activate(ref iid,23,IntPtr.Zero,out meter);id=nextId;}}finally{Release(next);}}
     float value;((IPeak)meter).GetPeakValue(out value);Level=Single.IsNaN(value)||Single.IsInfinity(value)?0:Math.Max(0,Math.Min(1,value));Available=true;Error="";
    }catch(Exception ex){Level=0;Available=false;Error=ex.Message;Release(meter);meter=null;Release(d);d=null;Release(e);e=null;id="";refresh=DateTime.MinValue;if(stop.WaitOne(1000))break;}
   }}finally{Level=0;Available=false;Release(meter);Release(d);Release(e);stop.Dispose();}
  }
  public void Dispose(){try{stop.Set();}catch(ObjectDisposedException){} }
 }
 [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class Enumerator {}
 [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDevices {
  void EnumAudioEndpoints(int flow,uint mask,out ICollection devices);void GetDefaultAudioEndpoint(int flow,int role,out IDevice device);void GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IDevice device);
 }
 [ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface ICollection {void GetCount(out uint count);void Item(uint index,out IDevice device);}
 [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDevice {void Activate(ref Guid iid,uint context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)]out object value);void OpenPropertyStore(uint access,out IProperties store);void GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);void GetState(out uint state);}
 [StructLayout(LayoutKind.Sequential)] struct Key {public Guid Format;public uint Id;}
 [StructLayout(LayoutKind.Explicit,Size=24)] struct Variant {[FieldOffset(0)]public ushort Type;[FieldOffset(8)]public IntPtr Value;}
 [DllImport("ole32.dll")] static extern int PropVariantClear(ref Variant value);
 [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IProperties {void GetCount(out uint count);void GetAt(uint index,out Key key);void GetValue(ref Key key,out Variant value);}
 [ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IVolume {
  void Register(IntPtr callback);void Unregister(IntPtr callback);void Channels(out uint count);void SetDb(float level,ref Guid context);void SetScalar(float level,ref Guid context);void GetDb(out float level);void GetScalar(out float level);void SetChannelDb(uint channel,float level,ref Guid context);void SetChannelScalar(uint channel,float level,ref Guid context);void GetChannelDb(uint channel,out float level);void GetChannelScalar(uint channel,out float level);void SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid context);void GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
 }
 // Windows has no public setter for the default playback device. This optional
 // PolicyConfig contract is isolated; failure is surfaced and never concealed.
 [ComImport,Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")] class Policy {}
 [ComImport,Guid("F8679F50-850A-41CF-9C72-430F290290C8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IPolicy {
  void Mix();void Format();void Reset();void SetFormat();void GetPeriod();void SetPeriod();void GetMode();void SetMode();void GetProperty();void SetProperty();void SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)]string id,int role);
 }
 public sealed class Output {public string Id,Name;public bool Current;public override string ToString(){return Name+(Current?" · atual":"");}}
 static void Release(object value){if(value!=null&&Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
 public static List<Output> Outputs(){
  var result=new List<Output>();IDevices e=null;ICollection c=null;IDevice current=null;
  try{e=(IDevices)new Enumerator();string active="";try{e.GetDefaultAudioEndpoint(0,1,out current);current.GetId(out active);}catch{}e.EnumAudioEndpoints(0,1,out c);uint n;c.GetCount(out n);
   for(uint i=0;i<n;i++){IDevice d=null;IProperties props=null;try{c.Item(i,out d);string id;d.GetId(out id);d.OpenPropertyStore(0,out props);var key=new Key{Format=new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),Id=14};Variant v;props.GetValue(ref key,out v);string name;try{name=v.Type==31?Marshal.PtrToStringUni(v.Value):"Saída de áudio";}finally{PropVariantClear(ref v);}result.Add(new Output{Id=id,Name=name,Current=id==active});}finally{Release(props);Release(d);}}
  }finally{Release(current);Release(c);Release(e);}return result;
 }
 public static float Volume(float? level){IDevices e=null;IDevice d=null;object o=null;try{e=(IDevices)new Enumerator();e.GetDefaultAudioEndpoint(0,1,out d);var iid=typeof(IVolume).GUID;d.Activate(ref iid,23,IntPtr.Zero,out o);var v=(IVolume)o;Guid context=Guid.Empty;if(level.HasValue)v.SetScalar(Math.Max(0,Math.Min(1,level.Value)),ref context);float actual;v.GetScalar(out actual);return actual;}finally{Release(o);Release(d);Release(e);}}
 public static void Select(string id){object p=null;try{p=new Policy();var policy=(IPolicy)p;policy.SetDefaultEndpoint(id,0);policy.SetDefaultEndpoint(id,1);}finally{Release(p);}}
}
}
