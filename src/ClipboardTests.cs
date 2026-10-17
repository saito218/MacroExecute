using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace MacroExecute
{
    internal static class ClipboardTests
    {
        private static void Check(bool value,string message){if(!value)throw new Exception(message);}
        private static Task NoWait(int ms,CancellationToken token){token.ThrowIfCancellationRequested();return Task.FromResult(true);}
        private static IEnumerable<Control> All(Control root){foreach(Control c in root.Controls){yield return c;foreach(var nested in All(c))yield return nested;}}
        private static void Click(Control c){typeof(Control).GetMethod("OnClick",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(c,new object[]{EventArgs.Empty});}
        public static void Run(Action<string,Action> test,string directory)
        {
            test("Clipboard substring modes preserve literal punctuation and combine any all and groups",delegate{
                var step=new Step{Kind=ActionKind.IfClipboard,ClipboardMatch=ClipboardMatchMode.All,Text="Monthly Report\nStatus: Approved\nVariance: 36(38-33)%"};Check(ClipboardCondition.Matches(ClipboardExamples.ItemText,step),"Literal item modifiers did not match");step.Text+="\nMissing field";Check(!ClipboardCondition.Matches(ClipboardExamples.ItemText,step),"All accepted missing text");step.ClipboardMatch=ClipboardMatchMode.Any;Check(ClipboardCondition.Matches(ClipboardExamples.ItemText,step),"Any required every term");step.ClipboardMatch=ClipboardMatchMode.Groups;step.Text="Monthly Report\nMissing field\n\nRecord Number: 85\nStatus: Approved";Check(ClipboardCondition.Matches(ClipboardExamples.ItemText,step),"Alternative group missed");step.Text="Monthly Report\nMissing field\n\nRecord Number: 100\nStatus: Approved";Check(!ClipboardCondition.Matches(ClipboardExamples.ItemText,step),"Partial groups were combined incorrectly");
            });
            test("Clipboard cases negation line endings and empty input have explicit contracts",delegate{
                var step=new Step{Kind=ActionKind.IfClipboard,ClipboardMatch=ClipboardMatchMode.All,Text="  monthly report  \r\n\r\nStatus: Approved\r"};Check(!ClipboardCondition.Matches(ClipboardExamples.ItemText,step),"Case-insensitive by default");step.ClipboardIgnoreCase=true;Check(ClipboardCondition.Matches(ClipboardExamples.ItemText,step),"Ignore case failed");step.ClipboardNegate=true;Check(!ClipboardCondition.Matches(ClipboardExamples.ItemText,step)&&!ClipboardCondition.Matches(null,step)&&!ClipboardCondition.Matches("",step),"NOT accepted matching or absent text");step.Text="not present";Check(ClipboardCondition.Matches(ClipboardExamples.ItemText,step),"NOT did not invert a nonmatch");
            });
            test("Legacy clipboard equality remains exact and case sensitive after JSON migration",delegate{
                var macro=Storage.Decode("{\"SchemaVersion\":1,\"Steps\":[{\"Kind\":24,\"Text\":\"READY\"}]}");var step=macro.Steps[0];Check(step.ClipboardMatch==ClipboardMatchMode.Exact&&ClipboardCondition.Matches("READY",step)&&!ClipboardCondition.Matches("ready",step)&&!ClipboardCondition.Matches("READY\n",step)&&!ClipboardCondition.Matches("xREADY",step),"Legacy equality changed");
            });
            test("Clipboard condition validation rejects empty rules invalid modes and excessive terms",delegate{
                foreach(var step in new[]{new Step{Kind=ActionKind.IfClipboard,ClipboardMatch=ClipboardMatchMode.All,Text=" \n"},new Step{Kind=ActionKind.IfClipboard,ClipboardMatch=(ClipboardMatchMode)99},new Step{Kind=ActionKind.IfClipboard,ClipboardMatch=ClipboardMatchMode.Groups,Text=string.Join("\n",Enumerable.Repeat("x",129))}}){bool rejected=false;try{Storage.Validate(new Macro{Steps=new List<Step>{step}});}catch(InvalidDataException){rejected=true;}Check(rejected,"Malformed clipboard rule accepted");}
            });
            test("Clipboard rules survive clone portable payload and file roundtrip",delegate{
                var macro=ClipboardExamples.Workflow(true);macro.Steps[5].ClipboardIgnoreCase=true;macro.Steps[5].ClipboardNegate=true;var loaded=Storage.Decode(Storage.Encode(macro));Check(loaded.Steps[5].ClipboardMatch==ClipboardMatchMode.Groups&&loaded.Steps[5].ClipboardIgnoreCase&&loaded.Steps[5].ClipboardNegate&&loaded.Steps[4].ClipboardWaitForChange,"Clipboard settings lost");Check(loaded.Steps[5].Clone().Text==macro.Steps[5].Text,"Clone lost groups");string file=Path.Combine(directory,"clipboard-player.exe");Portable.Export(System.Reflection.Assembly.GetExecutingAssembly().Location,file,loaded);Check(Portable.Read(file).Steps[5].ClipboardMatch==ClipboardMatchMode.Groups,"Exported player lost mode");
            });
            test("Clipboard lock retries unwrap UI invocation errors without swallowing unrelated failures",delegate{
                var clipboard=new TestClipboard();int attempts=0;clipboard.ReadOverride=delegate{if(++attempts<3)throw new System.Reflection.TargetInvocationException(new System.Runtime.InteropServices.ExternalException("busy"));return "ready";};Check(ClipboardCondition.Read(clipboard,NoWait,CancellationToken.None).GetAwaiter().GetResult()=="ready"&&attempts==3,"Temporary lock was not retried");clipboard.ReadOverride=delegate{throw new InvalidOperationException("real error");};bool preserved=false;try{ClipboardCondition.Read(clipboard,NoWait,CancellationToken.None).GetAwaiter().GetResult();}catch(InvalidOperationException){preserved=true;}Check(preserved,"Unrelated clipboard error swallowed");
            });
            test("Copy sends exactly Ctrl C and leaves fresh copied data available",delegate{
                var keyboard=new TestKeyboard();var clipboard=new TestClipboard{Value="old"};keyboard.OnEvent=delegate(string e){if(e=="up:67")clipboard.SetText(ClipboardExamples.ItemText);};ClipboardCondition.Copy(new Step{Kind=ActionKind.CopySelection},keyboard,clipboard,NoWait,CancellationToken.None).GetAwaiter().GetResult();Check(keyboard.Events.SequenceEqual(new[]{"down:17","down:67","up:67","up:17"})&&clipboard.Value==ClipboardExamples.ItemText&&clipboard.Restores==0,"Copy shortcut or data lifetime incorrect");
            });
            test("Copy waits for delayed fresh text and rejects stale or empty results",delegate{
                var clipboard=new TestClipboard{Value="old"};int polls=0;ClipboardCondition.Copy(new Step{Kind=ActionKind.CopySelection,TimeoutMs=200,PollMs=50},new TestKeyboard(),clipboard,delegate(int ms,CancellationToken t){if(ms==50&&++polls==2)clipboard.SetText("fresh");return NoWait(ms,t);},CancellationToken.None).GetAwaiter().GetResult();Check(polls==2&&clipboard.Value=="fresh","Did not wait for target update");foreach(bool empty in new[]{false,true}){clipboard=new TestClipboard{Value="stale"};var keyboard=new TestKeyboard();if(empty)keyboard.OnEvent=delegate(string e){if(e=="up:67")clipboard.SetText("");};bool timed=false;try{ClipboardCondition.Copy(new Step{TimeoutMs=100,PollMs=50},keyboard,clipboard,NoWait,CancellationToken.None).GetAwaiter().GetResult();}catch(TimeoutException){timed=true;}Check(timed,"Copy accepted stale or empty data");}
            });
            test("Copy cancellation releases C and Control before returning",delegate{
                var keyboard=new TestKeyboard();using(var cancel=new CancellationTokenSource()){bool stopped=false;try{ClipboardCondition.Copy(new Step(),keyboard,new TestClipboard(),delegate(int ms,CancellationToken t){cancel.Cancel();return NoWait(ms,t);},cancel.Token).GetAwaiter().GetResult();}catch(OperationCanceledException){stopped=true;}Check(stopped&&keyboard.Events.SequenceEqual(new[]{"down:17","down:67","up:67","up:17"}),"Cancellation left modifiers held");}
            });
            test("Full clear hover copy and conditional item workflow runs IF or skips ELSE headlessly",delegate{
                foreach(bool match in new[]{true,false})using(var ui=new Control{Visible=false})using(var desktop=new TestDesktop()){var macro=ClipboardExamples.Workflow();macro.Steps[3].DelayMs=0;var keyboard=new TestKeyboard();var clipboard=new TestClipboard{Value="stale"};keyboard.OnEvent=delegate(string e){if(e=="up:67")clipboard.SetText(match?ClipboardExamples.ItemText:"Document Type: Invoice\nStatus: Pending");};var result=new ProbeResult();var windows=new WindowsHost(ui,desktop,keyboard,clipboard);windows.Wait=NoWait;new Runner(new ClipboardProbeHost(windows,result)).Run(Plan.Build(macro,directory),1,1,CancellationToken.None).GetAwaiter().GetResult();Check(result.Output.Count==(match?1:0)&&result.Scope==0&&result.Released&&!ui.Visible,"Wrong branch, scope leak or desktop window shown");}
            });
            test("Clipboard editor displays LF rules as separate lines without rewriting legacy exact text",delegate{
                string original="READY\nNEXT";using(var editor=new StepEditor(new Step{Kind=ActionKind.IfClipboard,Text=original},directory)){var box=All(editor).OfType<TextBox>().Single(c=>c.AccessibleName=="Strings to check");Check(box.Text=="READY\r\nNEXT","Editor visually concatenates LF-only rules");typeof(StepEditor).GetMethod("Apply",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(editor,null);Check(editor.Value.Text==original,"Merely opening the editor rewrote exact text");}
            });
            test("Clipboard editor sample tester and compact layouts do not access the clipboard",delegate{
                using(var editor=new StepEditor(new Step{Kind=ActionKind.IfClipboard,ClipboardMatch=ClipboardMatchMode.All,Text="Monthly Report\nStatus: Approved"},directory)){HeadlessUi.Render(editor,Path.Combine(directory,"clipboard-fields.png"));var toggle=All(editor).Single(c=>c.AccessibleName=="Toggle clipboard tester");Click(toggle);Click(All(editor).Single(c=>c.AccessibleName=="Load clipboard item example"));Click(All(editor).Single(c=>c.AccessibleName=="Test clipboard condition"));Check(All(editor).OfType<Label>().Single(l=>l.AccessibleName=="Clipboard test result").Text.StartsWith("MATCH"),"Tester did not match provided item");editor.Size=editor.MinimumSize;HeadlessUi.Render(editor,Path.Combine(directory,"clipboard-fields-compact.png"));Check(!editor.Visible,"Tester showed a window");Click(toggle);}
                foreach(string style in UiTheme.Styles)foreach(bool dark in new[]{true,false}){UiTheme.Style=style;UiTheme.Dark=dark;using(var editor=new StepEditor(new Step{Kind=ActionKind.CopySelection},directory)){editor.Size=editor.MinimumSize;HeadlessUi.Render(editor,Path.Combine(directory,"copy-"+style+"-"+dark+".png"));Check(!editor.Visible,"Copy editor shown");}}UiTheme.Style="Aftershock";UiTheme.Dark=true;
            });
        }
    }
}
