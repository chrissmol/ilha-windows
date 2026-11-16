using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
namespace Tito {
public partial class App {
 // Files dragged out of browsers, mail and zip archives are "virtual files" (FileGroupDescriptorW + FileContents).
 // They are saved as real files so the shelf hands back the same file, not a link.
 const long MaxVirtualFile=500L*1024*1024;
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern uint RegisterClipboardFormat(string name);
 [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr handle);
 [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr handle);
 [DllImport("kernel32.dll")] static extern UIntPtr GlobalSize(IntPtr handle);
 [DllImport("ole32.dll")] static extern void ReleaseStgMedium(ref STGMEDIUM medium);
 static string[] DescriptorNames(byte[] raw){
  var names=new List<string>();if(raw==null||raw.Length<4)return new string[0];int count=BitConverter.ToInt32(raw,0);
  for(int i=0;i<count;i++){int start=4+i*592;if(start+592>raw.Length)break;names.Add(Encoding.Unicode.GetString(raw,start+72,520).Split('\0')[0]);}
  return names.ToArray();
 }
 static string SafeName(string name){foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');return String.IsNullOrWhiteSpace(name)?"arquivo":name;}
 static bool HasVirtualFiles(System.Windows.IDataObject data){return data.GetDataPresent("FileGroupDescriptorW");}
 static byte[] VirtualFileBytes(System.Runtime.InteropServices.ComTypes.IDataObject com,int index){
  var format=new FORMATETC{cfFormat=(short)RegisterClipboardFormat("FileContents"),ptd=IntPtr.Zero,dwAspect=DVASPECT.DVASPECT_CONTENT,lindex=index,tymed=TYMED.TYMED_ISTREAM|TYMED.TYMED_HGLOBAL};
  STGMEDIUM medium;com.GetData(ref format,out medium);
  try{
   if(medium.tymed==TYMED.TYMED_ISTREAM){
    var stream=(IStream)Marshal.GetObjectForIUnknown(medium.unionmember);var result=new MemoryStream();var buffer=new byte[65536];IntPtr read=Marshal.AllocHGlobal(4);
    try{stream.Seek(0,0,IntPtr.Zero);while(true){stream.Read(buffer,buffer.Length,read);int n=Marshal.ReadInt32(read);if(n<=0)break;result.Write(buffer,0,n);if(result.Length>MaxVirtualFile)throw new Exception("Arquivo grande demais para a prateleira.");}}
    finally{Marshal.FreeHGlobal(read);}
    return result.ToArray();
   }
   if(medium.tymed==TYMED.TYMED_HGLOBAL){
    IntPtr pointer=GlobalLock(medium.unionmember);
    try{long size=(long)GlobalSize(medium.unionmember).ToUInt64();if(size>MaxVirtualFile)throw new Exception("Arquivo grande demais para a prateleira.");var bytes=new byte[size];Marshal.Copy(pointer,bytes,0,(int)size);return bytes;}finally{GlobalUnlock(medium.unionmember);}
   }
  }finally{ReleaseStgMedium(ref medium);}
  return null;
 }
 static bool ReceiveVirtualFiles(System.Windows.IDataObject data,ShelfItem item){
  var raw=data.GetData("FileGroupDescriptorW") as MemoryStream;var com=data as System.Runtime.InteropServices.ComTypes.IDataObject;if(raw==null||com==null)return false;
  string[] names=DescriptorNames(raw.ToArray());if(names.Length==0)return false;
  string folder=Path.Combine(dataDir,"shelf",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);var paths=new List<string>();
  try{
   for(int i=0;i<names.Length;i++){byte[] bytes=VirtualFileBytes(com,i);if(bytes==null)continue;string path=Path.Combine(folder,SafeName(names[i]));if(File.Exists(path))path=Path.Combine(folder,i+"-"+SafeName(names[i]));File.WriteAllBytes(path,bytes);paths.Add(path);}
  }catch(Exception ex){error=ex.Message;ShowError();}
  if(paths.Count==0){try{Directory.Delete(folder,true);}catch{}return false;}
  item.Files=paths.ToArray();item.Folder=folder;item.Name=names.Length==1?names[0]:paths.Count+" arquivos";return true;
 }
 static void Forget(ShelfItem item){shelf.Remove(item);if(item.Folder!=null)try{Directory.Delete(item.Folder,true);}catch{}}
}
}
