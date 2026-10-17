using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MacroExecute
{
    internal sealed class FakeHost : IHost
    {
        public readonly List<string> Calls=new List<string>(); public bool Condition=true,Released,Fail;
        public Task Execute(Step s,CancellationToken t){t.ThrowIfCancellationRequested();if(Fail)throw new InvalidOperationException("Synthetic host failure.");Calls.Add(s.Text);return Task.FromResult(true);}
        public Task<bool> Evaluate(Step s,CancellationToken t){return Task.FromResult(Condition);}
        public void ReleaseInputs(){Released=true;}
    }
    internal static class SelfTests
    {
        private static readonly List<string> results=new List<string>();
        private static int failed;
        private static string progressReport;
        private static void Test(string name,Action action){if(progressReport!=null)File.WriteAllText(progressReport,string.Join(Environment.NewLine,results)+Environment.NewLine+"RUNNING "+name);try{action();results.Add("PASS "+name);}catch(Exception e){failed++;results.Add("FAIL "+name+": "+e.Message);}}
        private static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
        private static void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("Expected validation failure.");}
        private static Macro M(params Step[] steps){return new Macro{Steps=steps.ToList()};}
        private static Step S(ActionKind k,string text){return new Step{Kind=k,Text=text};}
        private static FakeHost Execute(Macro m,bool condition){var host=new FakeHost{Condition=condition};new Runner(host).Run(Plan.Build(m,Path.GetTempPath()),1,1,CancellationToken.None).GetAwaiter().GetResult();return host;}
        public static int Run(string report)
        {
            progressReport=report;string oldStyle=UiTheme.Style;bool oldDark=UiTheme.Dark;UiTheme.Style="Aftershock";UiTheme.Dark=true;
            string temp=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"MacroExecute-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
            try {
                Test("MacroExecute prefers the most recent legacy settings and saves only its own path",delegate{string current=Path.Combine(temp,"macroexecute.json"),recent=Path.Combine(temp,"previous-settings.json"),old=Path.Combine(temp,"older-settings.json");new Preferences{Repetitions=4}.SaveTo(old);Assert(Preferences.LoadFrom(current,recent,old).Repetitions==4,"Original settings not found.");new Preferences{Repetitions=6}.SaveTo(recent);Assert(Preferences.LoadFrom(current,recent,old).Repetitions==6,"Recent legacy settings not preferred.");var prefs=Preferences.LoadFrom(current,recent,old);prefs.Repetitions=8;prefs.SaveTo(current);Assert(Preferences.LoadFrom(current,recent,old).Repetitions==8&&Preferences.LoadFrom(recent).Repetitions==6&&Preferences.LoadFrom(old).Repetitions==4,"Settings precedence or legacy isolation failed.");File.WriteAllText(recent,"broken");File.Delete(current);Assert(Preferences.LoadFrom(current,recent,old).Repetitions==1,"Invalid recent settings revived stale originals.");});
                Test("Rebrand retains legacy preferences without modifying the old file",delegate{string legacy=Path.Combine(temp,"legacy-preferences.json"),current=Path.Combine(temp,"new-preferences.json");var prefs=new Preferences{DarkMode=true,AccentStyle="Aftershock",Repetitions=7};prefs.SaveTo(legacy);string original=File.ReadAllText(legacy);var loaded=Preferences.LoadFrom(current,legacy);Assert(loaded.DarkMode&&loaded.AccentStyle=="Aftershock"&&loaded.Repetitions==7,"Legacy settings were lost.");loaded.SaveTo(current);Assert(File.ReadAllText(legacy)==original,"Legacy settings were modified.");loaded.Repetitions=3;loaded.SaveTo(current);Assert(Preferences.LoadFrom(current,legacy).Repetitions==3,"New preferences did not take priority.");File.WriteAllText(current,"broken");Assert(Preferences.LoadFrom(current,legacy).Repetitions==1,"Corrupt new preferences silently revived old settings.");});
                Test("Rebrand preserves v1 player marker and original workflow schema",delegate{string source=Path.Combine(temp,"legacy-base.exe"),player=Path.Combine(temp,"legacy-player.exe");File.WriteAllBytes(source,new byte[]{77,90,1,2});var macro=Storage.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","examples","nested-image-if.mexec"));Portable.Export(source,player,macro);byte[] bytes=File.ReadAllBytes(player);Assert(bytes.Skip(bytes.Length-16).SequenceEqual(new byte[]{77,65,67,82,79,77,65,71,73,67,118,49,69,78,68,33}),"Legacy player marker changed.");Assert(Storage.Encode(Portable.Read(player))==Storage.Encode(macro),"Workflow changed during rebranded export.");Assert(typeof(Macro).Namespace=="MacroExecute"&&System.Reflection.Assembly.GetExecutingAssembly().GetName().Name=="MacroExecute","Assembly still uses the old identity.");});
                Test("Unicode/newline/delimiter JSON roundtrip",delegate{var m=M(S(ActionKind.TypeText,"日本語 😀\r\n : {braces} \"quoted\""));Assert(Storage.Decode(Storage.Encode(m)).Steps[0].Text==m.Steps[0].Text,"Text changed.");});
                Test("Schema version rejected",delegate{Reject(delegate{Storage.Decode("{\"SchemaVersion\":99,\"Steps\":[]}");});});
                Test("Null action rejected",delegate{Reject(delegate{Storage.Validate(M((Step)null));});});
                Test("Unknown action rejected",delegate{Reject(delegate{Storage.Validate(M(new Step{Kind=(ActionKind)999}));});});
                Test("Invalid RGB rejected",delegate{Reject(delegate{Storage.Validate(M(new Step{Kind=ActionKind.IfPixel,ColorRgb="red"}));});});
                Test("Negative delay rejected",delegate{Reject(delegate{Storage.Validate(M(new Step{Kind=ActionKind.Delay,DelayMs=-1}));});});
                Test("Zero repeat means forever",delegate{Storage.Validate(M(new Step{Kind=ActionKind.Repeat,Count=0}));});
                Test("Atomic save and replace",delegate{string file=Path.Combine(temp,"roundtrip.json");Storage.Save(file,M(S(ActionKind.Note,"one")));Storage.Save(file,M(S(ActionKind.Note,"two")));Assert(Storage.Load(file).Steps[0].Text=="two","Replace failed.");Assert(Directory.GetFiles(temp,"*.tmp").Length==0,"Temporary file leaked.");});
                Test("Crossed structural blocks rejected",delegate{Reject(delegate{Plan.Build(M(S(ActionKind.Repeat,""),S(ActionKind.IfWindow,"x"),S(ActionKind.EndRepeat,""),S(ActionKind.EndIf,"")),temp);});});
                Test("Duplicate labels rejected",delegate{Reject(delegate{Plan.Build(M(S(ActionKind.Label,"x"),S(ActionKind.Label,"x")),temp);});});
                Test("Missing labels rejected",delegate{Reject(delegate{Plan.Build(M(S(ActionKind.Jump,"missing")),temp);});});
                Test("Double ELSE rejected",delegate{Reject(delegate{Plan.Build(M(S(ActionKind.IfWindow,"x"),S(ActionKind.Else,""),S(ActionKind.Else,""),S(ActionKind.EndIf,"")),temp);});});
                Test("Unclosed block rejected",delegate{Reject(delegate{Plan.Build(M(S(ActionKind.Repeat,"")),temp);});});
                Test("BREAK outside repeat rejected",delegate{Reject(delegate{Plan.Build(M(S(ActionKind.Break,"")),temp);});});
                Test("Nested repeats execute exact counts",delegate{var m=M(new Step{Kind=ActionKind.Repeat,Count=2},new Step{Kind=ActionKind.Repeat,Count=3},S(ActionKind.Message,"body"),S(ActionKind.EndRepeat,""),S(ActionKind.EndRepeat,""));var h=Execute(m,true);Assert(h.Calls.Count==6&&h.Released,"Nested count/cleanup wrong.");});
                Test("IF true and false choose one branch",delegate{var m=M(S(ActionKind.IfWindow,"x"),S(ActionKind.Message,"yes"),S(ActionKind.Else,""),S(ActionKind.Message,"no"),S(ActionKind.EndIf,""));Assert(Execute(m,true).Calls.SequenceEqual(new[]{"yes"}),"True path wrong.");Assert(Execute(m,false).Calls.SequenceEqual(new[]{"no"}),"False path wrong.");});
                Test("Nested IF false skips nested ELSE",delegate{var m=M(S(ActionKind.IfWindow,"x"),S(ActionKind.IfWindow,"x"),S(ActionKind.Message,"bad"),S(ActionKind.Else,""),S(ActionKind.Message,"bad2"),S(ActionKind.EndIf,""),S(ActionKind.Else,""),S(ActionKind.Message,"outer"),S(ActionKind.EndIf,""));Assert(Execute(m,false).Calls.SequenceEqual(new[]{"outer"}),"Wrong nesting.");});
                Test("BREAK exits nearest loop",delegate{var m=M(new Step{Kind=ActionKind.Repeat,Count=3},S(ActionKind.Message,"once"),S(ActionKind.Break,""),S(ActionKind.Message,"bad"),S(ActionKind.EndRepeat,""));Assert(Execute(m,true).Calls.SequenceEqual(new[]{"once"}),"Break failed.");});
                Test("Jump skips intervening action",delegate{Assert(Execute(M(S(ActionKind.Jump,"end"),S(ActionKind.Message,"bad"),S(ActionKind.Label,"end"),S(ActionKind.Message,"good")),true).Calls.SequenceEqual(new[]{"good"}),"Jump failed.");});
                Test("Jump into inactive loop rejected and inputs released",delegate{var host=new FakeHost();Reject(delegate{new Runner(host).Run(Plan.Build(M(S(ActionKind.Jump,"inside"),S(ActionKind.Repeat,""),S(ActionKind.Label,"inside"),S(ActionKind.EndRepeat,"")),temp),1,1,CancellationToken.None).GetAwaiter().GetResult();});Assert(host.Released,"Cleanup not called.");});
                Test("Disabled actions are skipped",delegate{var s=S(ActionKind.Message,"bad");s.Enabled=false;Assert(Execute(M(s,S(ActionKind.Message,"good")),true).Calls.SequenceEqual(new[]{"good"}),"Disabled ran.");});
                Test("Include cycle rejected",delegate{string file=Path.Combine(temp,"cycle.json");Storage.Save(file,M(new Step{Kind=ActionKind.Include,Target="cycle.json"}));Reject(delegate{Plan.Build(Storage.Load(file),temp);});});
                Test("Relative asset paths follow macro directory",delegate{var plan=Plan.Build(M(new Step{Kind=ActionKind.IfFile,Target="asset.txt"},S(ActionKind.EndIf,""),new Step{Kind=ActionKind.Launch,Target="notepad.exe"}),temp);Assert(plan.Steps[0].Target==Path.Combine(temp,"asset.txt"),"Relative asset escaped base.");Assert(plan.Steps[2].Target=="notepad.exe","PATH program changed.");});
                Test("Includes have independent label scope",delegate{Storage.Save(Path.Combine(temp,"child.json"),M(S(ActionKind.Jump,"finish"),S(ActionKind.Message,"bad"),S(ActionKind.Label,"finish"),S(ActionKind.Message,"good")));var m=M(new Step{Kind=ActionKind.Include,Target="child.json"},new Step{Kind=ActionKind.Include,Target="child.json"});var h=new FakeHost();new Runner(h).Run(Plan.Build(m,temp),1,1,CancellationToken.None).GetAwaiter().GetResult();Assert(h.Calls.SequenceEqual(new[]{"good","good"}),"Include scope leaked.");});
                Test("Cancellation releases inputs",delegate{var h=new FakeHost();var c=new CancellationTokenSource();c.CancelAfter(40);try{new Runner(h).Run(Plan.Build(M(new Step{Kind=ActionKind.Delay,DelayMs=5000}),temp),1,1,c.Token).GetAwaiter().GetResult();throw new Exception("Did not cancel.");}catch(OperationCanceledException){Assert(h.Released,"Cleanup missing.");}finally{c.Dispose();}});
                Test("Host exception releases inputs",delegate{var h=new FakeHost{Fail=true};try{new Runner(h).Run(Plan.Build(M(S(ActionKind.Message,"fail")),temp),1,1,CancellationToken.None).GetAwaiter().GetResult();throw new Exception("Host failure lost.");}catch(InvalidOperationException){Assert(h.Released,"Exception cleanup missing.");}});
                Test("File/process conditions are structural IF blocks",delegate{Assert(Execute(M(S(ActionKind.IfFile,""),S(ActionKind.Message,"bad"),S(ActionKind.Else,""),S(ActionKind.Message,"fallback"),S(ActionKind.EndIf,"")),false).Calls.SequenceEqual(new[]{"fallback"}),"New IF not paired.");Reject(delegate{Plan.Build(M(S(ActionKind.IfProcess,"x")),temp);});});
                Test("Pause can be resumed without UI input",delegate{var h=new FakeHost();var r=new Runner(h);var run=r.Run(Plan.Build(M(S(ActionKind.Pause,""),S(ActionKind.Message,"resumed")),temp),1,1,CancellationToken.None);Task.Delay(40).ContinueWith(delegate{r.Paused=false;});run.GetAwaiter().GetResult();Assert(h.Calls.SequenceEqual(new[]{"resumed"}),"Resume failed.");});
                Test("Image right-edge positive match and bounds",delegate{using(var scene=new Bitmap(5,3,PixelFormat.Format32bppArgb))using(var template=new Bitmap(2,2,PixelFormat.Format32bppArgb)){using(var g=Graphics.FromImage(scene))g.Clear(Color.Black);using(var g=Graphics.FromImage(template))g.Clear(Color.Red);for(int y=1;y<3;y++)for(int x=3;x<5;x++)scene.SetPixel(x,y,Color.Red);Point? p=Vision.Match(scene,template,0,CancellationToken.None);Assert(p.HasValue&&p.Value==new Point(4,2),"Edge match wrong.");scene.SetPixel(3,1,Color.Black);Assert(!Vision.Match(scene,template,0,CancellationToken.None).HasValue,"False match.");}});
                Test("Oversized template returns no match",delegate{using(var scene=new Bitmap(1,1,PixelFormat.Format32bppArgb))using(var pattern=new Bitmap(2,2,PixelFormat.Format32bppArgb))Assert(!Vision.Match(scene,pattern,0,CancellationToken.None).HasValue,"Oversize accepted.");});
                Test("Image cancellation is observed",delegate{using(var scene=new Bitmap(5,5,PixelFormat.Format32bppArgb))using(var pattern=new Bitmap(1,1,PixelFormat.Format32bppArgb)){var source=new CancellationTokenSource();source.Cancel();try{Vision.Match(scene,pattern,0,source.Token);throw new Exception("Cancellation ignored.");}catch(OperationCanceledException){}finally{source.Dispose();}}});
                Test("Tolerance zero is exact",delegate{using(var scene=new Bitmap(1,1,PixelFormat.Format32bppArgb))using(var pattern=new Bitmap(1,1,PixelFormat.Format32bppArgb)){scene.SetPixel(0,0,Color.FromArgb(20,30,40));pattern.SetPixel(0,0,Color.FromArgb(21,30,40));Assert(!Vision.Match(scene,pattern,0,CancellationToken.None).HasValue,"Zero tolerance not exact.");Assert(Vision.Match(scene,pattern,1,CancellationToken.None).HasValue,"Tolerance boundary failed.");}});
                Test("Odd-width pixel buffer preserves rows",delegate{using(var scene=new Bitmap(3,2,PixelFormat.Format32bppArgb)){using(var g=Graphics.FromImage(scene))g.Clear(Color.Black);scene.SetPixel(2,1,Color.Lime);Assert(Vision.Pixel(scene,Color.Lime,0,CancellationToken.None)==new Point(2,1),"Pixel row index wrong.");}});
                Test("Native INPUT ABI size",delegate{Assert(Marshal.SizeOf(typeof(Native.Input))==(IntPtr.Size==8?40:28),"INPUT layout wrong.");});
                Test("Portable roundtrip and re-export replace payload",delegate{string source=Path.Combine(temp,"base.exe"),one=Path.Combine(temp,"one.exe"),two=Path.Combine(temp,"two.exe");File.WriteAllBytes(source,new byte[]{77,90,1,2,3,4});Portable.Export(source,one,M(S(ActionKind.Note,"one")));Portable.Export(one,two,M(S(ActionKind.Note,"two")));Assert(Portable.Read(two).Steps[0].Text=="two","Overlay not replaced.");Assert(new FileInfo(one).Length==new FileInfo(two).Length,"Overlay accumulated.");});
                Test("Portable corruption rejected",delegate{string file=Path.Combine(temp,"one.exe");byte[] bytes=File.ReadAllBytes(file);bytes[6]^=1;File.WriteAllBytes(file,bytes);Reject(delegate{Portable.Read(file);});});
                Test("Windows argument quoting",delegate{Assert(Scheduling.Quote("C:\\folder name\\flow.json")=="\"C:\\folder name\\flow.json\"","Space quoting failed.");Assert(Scheduling.Quote("ends\\")=="\"ends\\\\\"","Trailing slash quoting failed.");Assert(Scheduling.Quote("a\"b")=="\"a\\\"b\"","Quote escaping failed.");});
            IconTests.Run(Test,temp);
            WorkbenchInteractionTests.Run(Test,temp);
            FeatureTests.Run(Test,temp);
            CaptureTests.Run(Test);
            SettingsTests.Run(Test,temp);
            UsabilityTests.Run(Test,temp);
            BuildingTests.Run(Test,temp);
            StressTests.Run(Test,temp);
            FileTypeTests.Run(Test,temp);
            DesignTests.Run(Test,temp);
            BehaviorTests.Run(Test,temp);
            ClipboardTests.Run(Test,temp);MouseClickTests.Run(Test,temp);ClipboardRuleTests.Run(Test,temp);DesignUpgradeTests.Run(Test,temp);
            }finally{UiTheme.Style=oldStyle;UiTheme.Dark=oldDark;Directory.Delete(temp,true);}
            results.Add((results.Count-failed)+" passed; "+failed+" failed. No global hooks, input injection, clipboard changes, schedules or network calls were exercised.");
            string text=string.Join(Environment.NewLine,results);Console.WriteLine(text);if(report!=null)File.WriteAllText(report,text);return failed==0?0:1;
        }
    }
}
