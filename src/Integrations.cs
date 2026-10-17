using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MacroExecute
{
    public static class Scripts
    {
        internal static async Task AwaitCompilation(Task compilation,int timeout,CancellationToken token)
        {
            using(var timer=new CancellationTokenSource())using(var combined=CancellationTokenSource.CreateLinkedTokenSource(timer.Token,token)){
                var deadline=Task.Delay(timeout,combined.Token);Task winner=await Task.WhenAny(compilation,deadline);token.ThrowIfCancellationRequested();if(winner!=compilation)throw new TimeoutException("C# compilation exceeded the script time limit.");timer.Cancel();await compilation;
            }
        }
        public static async Task Run(string source,int timeout,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();if(timeout<1)throw new ArgumentOutOfRangeException("timeout");
            string directory=Path.Combine(Path.GetTempPath(),"MacroExecute-script-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);Process child=null;Task compilation=null;var clock=Stopwatch.StartNew();
            try {
                string executable=Path.Combine(directory,"FlowScript.exe");
                compilation=Task.Run(delegate {using(var compiler=CodeDomProvider.CreateProvider("CSharp")){var options=new CompilerParameters(new[]{"System.dll","System.Core.dll","System.Drawing.dll","System.Windows.Forms.dll"},executable){GenerateExecutable=true,GenerateInMemory=false,CompilerOptions="/optimize+"};CompilerResults result=compiler.CompileAssemblyFromSource(options,source);if(result.Errors.HasErrors){var errors=new StringBuilder();foreach(CompilerError error in result.Errors)if(!error.IsWarning)errors.AppendLine("Line "+error.Line+": "+error.ErrorText);throw new InvalidDataException(errors.ToString());}}},token);
                await AwaitCompilation(compilation,timeout,token);token.ThrowIfCancellationRequested();if(clock.ElapsedMilliseconds>=timeout)throw new TimeoutException("C# script exceeded its time limit during compilation.");
                var output=new StringBuilder();Action<string> capture=delegate(string line){if(line==null)return;lock(output){if(output.Length<8192)output.AppendLine(line.Substring(0,Math.Min(line.Length,8192-output.Length)));}};
                child=new Process{StartInfo=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=directory,RedirectStandardError=true,RedirectStandardOutput=true}};child.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){capture(e.Data);};child.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){capture(e.Data);};child.Start();child.BeginErrorReadLine();child.BeginOutputReadLine();
                while(!child.HasExited){token.ThrowIfCancellationRequested();if(clock.ElapsedMilliseconds>=timeout)throw new TimeoutException("C# script exceeded its time limit (including compilation).");await Task.Delay(25,token);}if(child.ExitCode!=0){string detail;lock(output)detail=output.ToString();throw new InvalidOperationException("C# script exited with code "+child.ExitCode+". "+detail);}
            }finally{if(child!=null){try{if(!child.HasExited){child.Kill();child.WaitForExit(2000);}}finally{child.Dispose();}}if(compilation!=null&&!compilation.IsCompleted){DeferCleanup(compilation,directory);}else Cleanup(directory);}
        }
        private static void DeferCleanup(Task compilation,string directory) { compilation.ContinueWith(delegate(Task finished){var observed=finished.Exception;Cleanup(directory);},TaskScheduler.Default); }
        private static void Cleanup(string directory) { try{Directory.Delete(directory,true);}catch(IOException){}catch(UnauthorizedAccessException){} }
    }
    public static class Portable
    {
        // Retain the v1 wire marker so existing exported players remain readable.
        private static readonly byte[] marker = new byte[]{77,65,67,82,79,77,65,71,73,67,118,49,69,78,68,33};
        public static void Validate(Macro macro,string directory) { var plan=Plan.Build(macro,directory);foreach(var step in plan.Steps)if(ImageTemplate.IsImage(step.Kind))ImageTemplate.ValidateReferences(step); }
        // Authenticode appends a certificate table after the overlay. Its file offset,
        // unlike other PE directories, is not an RVA. Never interpret certificate bytes as payload.
        private static long CertificateDirectory(Stream stream)
        {
            if(stream.Length<64)return -1;var reader=new BinaryReader(stream,Encoding.UTF8,true);stream.Position=0;if(reader.ReadUInt16()!=0x5a4d)return -1;stream.Position=0x3c;long pe=reader.ReadUInt32();if(pe<64||pe>stream.Length-24)return -1;stream.Position=pe;if(reader.ReadUInt32()!=0x4550)return -1;stream.Position=pe+20;int size=reader.ReadUInt16();long optional=pe+24;if(optional+size>stream.Length)return -1;stream.Position=optional;int magic=reader.ReadUInt16();int offset=magic==0x10b?128:magic==0x20b?144:-1;if(offset<0||size<offset+8)return -1;return optional+offset;
        }
        private static long ContentEnd(Stream stream)
        {
            long directory=CertificateDirectory(stream);if(directory<0)return stream.Length;stream.Position=directory;var reader=new BinaryReader(stream,Encoding.UTF8,true);long offset=reader.ReadUInt32(),size=reader.ReadUInt32();if(offset==0&&size==0)return stream.Length;if(offset<directory+8||size<8||offset>stream.Length-size||offset+size!=stream.Length)throw new InvalidDataException("Invalid executable certificate table.");return offset;
        }
        private static long FooterEnd(Stream stream)
        {
            long end=ContentEnd(stream);int padding=end==stream.Length?0:7;for(int n=0;n<=padding;n++){if(end-n<marker.Length)break;stream.Position=end-n-marker.Length;bool match=true;for(int i=0;i<marker.Length;i++)if(stream.ReadByte()!=marker[i]){match=false;break;}if(match)return end-n;if(n<padding){stream.Position=end-n-1;if(stream.ReadByte()!=0)break;}}return end;
        }
        public static Macro Read(string path)
        {
            using (var stream = File.OpenRead(path)) {
                long length = PayloadLength(stream); if (length < 0) return null;
                stream.Position = FooterEnd(stream) - marker.Length - 8 - 32 - length;
                byte[] bytes = new byte[(int)length]; ReadExact(stream,bytes); byte[] digest = new byte[32]; ReadExact(stream,digest);
                using (var sha = SHA256.Create()) { byte[] actual = sha.ComputeHash(bytes); for (int i = 0; i < 32; i++) if (digest[i] != actual[i]) throw new InvalidDataException("Portable macro checksum mismatch."); }
                return Storage.Decode(Encoding.UTF8.GetString(bytes));
            }
        }
        private static void ReadExact(Stream stream, byte[] bytes) { int n = 0; while (n < bytes.Length) { int r = stream.Read(bytes,n,bytes.Length-n); if (r == 0) throw new EndOfStreamException(); n += r; } }
        private static long PayloadLength(Stream stream)
        {
            long footer=FooterEnd(stream);if (footer < marker.Length + 40) return -1;
            stream.Position = footer-marker.Length; byte[] tail = new byte[marker.Length]; ReadExact(stream,tail); for (int i=0;i<tail.Length;i++) if(tail[i]!=marker[i]) return -1;
            stream.Position = footer-marker.Length-8; byte[] size = new byte[8]; ReadExact(stream,size); long length = BitConverter.ToInt64(size,0);
            if (length < 0 || length > Storage.MaxBytes || length > footer-marker.Length-40) throw new InvalidDataException("Invalid portable macro length."); return length;
        }
        public static void Export(string sourceExe, string targetExe, Macro macro)
        {
            if (Path.GetFullPath(sourceExe).Equals(Path.GetFullPath(targetExe),StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Choose a different executable filename.");
            // Includes and external assets remain explicit dependencies; no third-party binary is read.
            byte[] payload = Encoding.UTF8.GetBytes(Storage.Encode(macro));
            string temp = targetExe+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                using (var source = File.OpenRead(sourceExe)) using (var dest = File.Create(temp)) {
                    long previous = PayloadLength(source); long remaining = previous < 0 ? ContentEnd(source) : FooterEnd(source)-marker.Length-40-previous; source.Position=0;
                    byte[] buffer = new byte[65536]; while (remaining > 0) { int count=source.Read(buffer,0,(int)Math.Min(remaining,buffer.Length)); if(count==0) throw new EndOfStreamException(); dest.Write(buffer,0,count); remaining-=count; }
                    // Export is a new unsigned artifact; the source signature cannot cover changed bytes.
                    long certificate=CertificateDirectory(source);if(certificate>=0){long end=dest.Position;dest.Position=certificate;dest.Write(new byte[8],0,8);source.Position=0x3c;var peReader=new BinaryReader(source,Encoding.UTF8,true);dest.Position=peReader.ReadUInt32()+24+64;dest.Write(new byte[4],0,4);dest.Position=end;}
                    dest.Write(payload,0,payload.Length); using(var sha=SHA256.Create()) { byte[] digest=sha.ComputeHash(payload); dest.Write(digest,0,digest.Length); }
                    byte[] size=BitConverter.GetBytes((long)payload.Length); dest.Write(size,0,size.Length); dest.Write(marker,0,marker.Length);
                }
                if(File.Exists(targetExe)) File.Replace(temp,targetExe,null); else File.Move(temp,targetExe);
            } finally { if(File.Exists(temp)) File.Delete(temp); }
        }
    }
    public static class Scheduling
    {
        public static string Quote(string value)
        {
            var b = new StringBuilder("\""); int slashes=0;
            foreach(char c in value) { if(c=='\\') { slashes++; continue; } if(c=='"') { b.Append('\\',slashes*2+1); b.Append(c); } else { b.Append('\\',slashes); b.Append(c); } slashes=0; }
            b.Append('\\',slashes*2); return b.Append('"').ToString();
        }
        public static string Create(string executable, string macro, DateTime when, bool daily)
        {
            if (!File.Exists(macro)) throw new FileNotFoundException("Save the macro before scheduling.",macro);
            if(when <= DateTime.Now) throw new InvalidOperationException("Choose a future start time.");
            object serviceObject=null,folderObject=null,definitionObject=null,triggerObject=null,actionObject=null,registered=null;
            try {
                Type type=Type.GetTypeFromProgID("Schedule.Service"); if(type==null) throw new NotSupportedException("Windows Task Scheduler is unavailable.");
                serviceObject=Activator.CreateInstance(type); dynamic service=serviceObject; service.Connect();
                folderObject=service.GetFolder("\\"); dynamic folder=folderObject;
                definitionObject=service.NewTask(0); dynamic definition=definitionObject;
                definition.RegistrationInfo.Description="MacroExecute: "+Path.GetFileName(macro);
                definition.Principal.LogonType=3; definition.Principal.RunLevel=0;
                definition.Settings.Enabled=true; definition.Settings.StartWhenAvailable=true; definition.Settings.ExecutionTimeLimit="PT2H";
                triggerObject=definition.Triggers.Create(daily?2:1); dynamic trigger=triggerObject; trigger.StartBoundary=when.ToString("yyyy-MM-dd'T'HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture); if(daily) trigger.DaysInterval=1;
                actionObject=definition.Actions.Create(0); dynamic action=actionObject; action.Path=Path.GetFullPath(executable); action.Arguments="--run "+Quote(Path.GetFullPath(macro))+" --exit"; action.WorkingDirectory=Path.GetDirectoryName(Path.GetFullPath(macro));
                string name="MacroExecute-"+Guid.NewGuid().ToString("N"); registered=folder.RegisterTaskDefinition(name,definition,2,null,null,3,null); return name;
            } finally { foreach(object o in new[]{registered,actionObject,triggerObject,definitionObject,folderObject,serviceObject}) if(o!=null && Marshal.IsComObject(o)) Marshal.FinalReleaseComObject(o); }
        }
    }
    public sealed class Preferences
    {
        public bool RecordMoves {get;set;}
        public bool RelativeCoordinates {get;set;}
        public int Repetitions {get;set;}
        public double Speed {get;set;}
        public string[] RecentFiles {get;set;}
        public string[] HotkeyFiles {get;set;}
        public bool DarkMode {get;set;}
        public string AccentStyle {get;set;}
        public Shortcut[] ActionShortcuts {get;set;}
        public Preferences() { RecordMoves=true; Repetitions=1; Speed=1; RecentFiles=new string[0]; HotkeyFiles=new string[9];ActionShortcuts=Shortcuts.Defaults();AccentStyle="Neon"; }
        private static string PathName {get {return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MacroExecute","preferences.json");}}
        public static Preferences Load() { return LoadFrom(PathName); }
        internal static Preferences LoadFrom(string currentPath,params string[] legacyPaths) { try { string path=currentPath;if(!File.Exists(path))foreach(string legacyPath in legacyPaths)if(File.Exists(legacyPath)){path=legacyPath;break;}if(!File.Exists(path)) return new Preferences(); var p=Storage.Serializer().Deserialize<Preferences>(File.ReadAllText(path)); if(p==null||p.Speed<0.1||p.Speed>20||p.Repetitions<0||p.Repetitions>1000000||double.IsNaN(p.Speed)) return new Preferences();try{p.ActionShortcuts=Shortcuts.Normalize(p.ActionShortcuts);Shortcuts.Validate(p.ActionShortcuts,p.HotkeyFiles);}catch{p.ActionShortcuts=Shortcuts.Defaults();}return p; } catch {return new Preferences();} }
        public void Save() { SaveTo(PathName); }
        internal void SaveTo(string path) { Shortcuts.Validate(ActionShortcuts,HotkeyFiles);string full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full));string temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,Storage.Serializer().Serialize(this),new UTF8Encoding(false));if(File.Exists(full))File.Replace(temp,full,null);else File.Move(temp,full);}finally{if(File.Exists(temp))File.Delete(temp);} }
    }
}
