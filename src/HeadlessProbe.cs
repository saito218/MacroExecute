using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroExecute
{
    // Process boundary for Playwright Test. Never constructs a real desktop host.
    internal sealed class ProbeRequest
    {
        public Macro Macro { get; set; }
        public string ClipboardText { get; set; }
        public string CopyText { get; set; }
        public string Mode { get; set; }
        public int Repetitions { get; set; }
        public double Speed { get; set; }
        public int CancelAt { get; set; }
        public int FailAt { get; set; }
        public int PauseAt { get; set; }
        public int ResumeAfterMs { get; set; }
        public int DeadlineMs { get; set; }
        public int Select { get; set; }
        public int MoveStart { get; set; }
        public int MoveGap { get; set; }
        public string Find { get; set; }
        public string View { get; set; }
        public string[] RecentPaths { get; set; }
        public ProbeRequest() { Mode="run";Repetitions=1;Speed=1;MoveStart=-1;DeadlineMs=45000; }
    }
    internal sealed class ProbeResult
    {
        public string Error { get; set; }
        public string ErrorType { get; set; }
        public bool Cancelled { get; set; }
        public bool Released { get; set; }
        public bool Hidden { get; set; }
        public bool Moved { get; set; }
        public bool StepNumbersFit { get; set; }
        public int Steps { get; set; }
        public int Selected { get; set; }
        public int Dispatches { get; set; }
        public int MaximumScope { get; set; }
        public int Scope { get; set; }
        public int Resets { get; set; }
        public int Pauses { get; set; }
        public bool PausedAtEnd { get; set; }
        public long ElapsedMs { get; set; }
        public List<string> Output { get; set; }
        public List<string> Conditions { get; set; }
        public List<string> InputEvents { get; set; }
        public List<int> Matches { get; set; }
        public ProbeResult() { InputEvents=new List<string>();Output=new List<string>();Conditions=new List<string>();Matches=new List<int>(); }
    }
    internal sealed class ProbeHost : IHost, IScopedHost
    {
        private readonly ProbeResult result;
        private readonly ProbeRequest request;
        public ProbeHost(ProbeRequest request,ProbeResult result) { this.request=request;this.result=result; }
        public Task Execute(Step step,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if(request.FailAt>0&&result.Output.Count+1==request.FailAt)throw new InvalidOperationException("Injected target failure");
            result.Output.Add(step.Text);return Task.FromResult(true);
        }
        public Task<bool> Evaluate(Step step,CancellationToken token) { token.ThrowIfCancellationRequested();result.Conditions.Add(step.Text);return Task.FromResult(step.Text.StartsWith("true:",StringComparison.Ordinal)); }
        public void ReleaseInputs() { result.Released=true; }
        public void EnterCondition() { result.Scope++;result.MaximumScope=Math.Max(result.MaximumScope,result.Scope); }
        public void LeaveCondition() { if(--result.Scope<0)throw new InvalidOperationException("Scope underflow"); }
        public void ResetMatches() { result.Scope=0;result.Resets++; }
    }
    internal sealed class ClipboardProbeHost : IHost, IScopedHost
    {
        private readonly WindowsHost host;private readonly ProbeResult result;
        internal ClipboardProbeHost(WindowsHost host,ProbeResult result){this.host=host;this.result=result;}
        public Task Execute(Step step,CancellationToken token){token.ThrowIfCancellationRequested();if(step.Kind==ActionKind.Message){result.Output.Add(step.Text);return Task.FromResult(true);}return host.Execute(step,token);}
        public Task<bool> Evaluate(Step step,CancellationToken token){return host.Evaluate(step,token);}
        public void ReleaseInputs(){host.ReleaseInputs();result.Released=true;}
        public void EnterCondition(){host.EnterCondition();result.Scope++;result.MaximumScope=Math.Max(result.MaximumScope,result.Scope);}
        public void LeaveCondition(){host.LeaveCondition();result.Scope--;}
        public void ResetMatches(){host.ResetMatches();result.Scope=0;result.Resets++;}
    }
    internal static class HeadlessProbe
    {
        private static IEnumerable<Control> Controls(Control root) { foreach(Control child in root.Controls) { yield return child;foreach(var nested in Controls(child))yield return nested; } }
        public static int Run(string requestPath,string reportPath)
        {
            var result=new ProbeResult();var watch=Stopwatch.StartNew();
            try {
                if(new FileInfo(requestPath).Length>Storage.MaxBytes)throw new InvalidDataException("Probe request exceeds 16 MB");
                var request=Storage.Serializer().Deserialize<ProbeRequest>(File.ReadAllText(requestPath));
                if(request==null)throw new InvalidDataException("Missing probe request");
                var plan=Plan.Build(request.Macro,Path.GetDirectoryName(Path.GetFullPath(requestPath)));result.Steps=plan.Steps.Count;
                if(request.Mode=="validate") { }
                else if(request.Mode=="roundtrip") {
                    string path=Path.Combine(Path.GetDirectoryName(reportPath),"roundtrip.mexec");Storage.Save(path,request.Macro);
                    string encoded=Storage.Encode(request.Macro);if(encoded!=Storage.Encode(Storage.Load(path)))throw new InvalidDataException("Roundtrip changed the workflow");
                    result.Output.Add(encoded);
                }
                else if(request.Mode=="clipboard") {
                    var allowed=new[]{ActionKind.Note,ActionKind.Delay,ActionKind.KeyDown,ActionKind.KeyUp,ActionKind.KeyPress,ActionKind.CopySelection,ActionKind.ClipboardClear,ActionKind.SetClipboard,ActionKind.MouseMove,ActionKind.IfClipboard,ActionKind.Else,ActionKind.EndIf,ActionKind.Message,ActionKind.Repeat,ActionKind.EndRepeat};
                    if(plan.Steps.Any(s=>!allowed.Contains(s.Kind)))throw new InvalidDataException("Clipboard probe permits only isolated clipboard, key and branch operations.");
                    using(var ui=new Control{Visible=false})using(var desktop=new TestDesktop())using(var cancel=new CancellationTokenSource(Math.Max(1,Math.Min(45000,request.DeadlineMs)))){var keyboard=new TestKeyboard();var clipboard=new TestClipboard{Value=request.ClipboardText};bool controlHeld=false;keyboard.OnEvent=delegate(string e){if(e=="down:17")controlHeld=true;if(e=="up:17")controlHeld=false;if(e=="up:67"&&controlHeld&&request.CopyText!=null)clipboard.SetText(request.CopyText);};var windows=new WindowsHost(ui,desktop,keyboard,clipboard);windows.Wait=delegate(int ms,CancellationToken t){t.ThrowIfCancellationRequested();return Task.FromResult(true);};var host=new ClipboardProbeHost(windows,result);var runner=new Runner(host);runner.Progress+=delegate{result.Dispatches++;if(result.Dispatches>20000||(request.CancelAt>0&&result.Dispatches==request.CancelAt))cancel.Cancel();};try{Task.Run(delegate{return runner.Run(plan,request.Repetitions,request.Speed,cancel.Token);}).GetAwaiter().GetResult();}finally{result.InputEvents=keyboard.Events;result.Conditions.Add(clipboard.Value);result.Hidden=!ui.Visible;}}
                }
                else if(request.Mode=="text") {
                    if(request.Macro.Steps.Any(s=>s.Kind!=ActionKind.TypeText&&s.Kind!=ActionKind.KeyPress&&s.Kind!=ActionKind.Paste))throw new InvalidDataException("Text probe allows only synthetic text, keypress and paste steps.");
                    using(var ui=new Control{Visible=false})using(var desktop=new TestDesktop()){var keyboard=new TestKeyboard();var clipboard=new TestClipboard();var host=new WindowsHost(ui,desktop,keyboard,clipboard);host.Wait=delegate(int ms,CancellationToken t){t.ThrowIfCancellationRequested();return Task.FromResult(true);};try{foreach(var step in request.Macro.Steps)if(step.Enabled)host.Execute(step,CancellationToken.None).GetAwaiter().GetResult();}finally{host.ReleaseInputs();}result.Output=keyboard.Events;result.Conditions.Add(clipboard.Value);result.Hidden=!ui.Visible;result.Released=true;}
                }
                else if(request.Mode=="settings") {
                    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);UiTheme.Dark=true;UiTheme.Style="Aftershock";
                    var preferences=new Preferences{DarkMode=true,AccentStyle="Aftershock"};Form dialog;
                    switch(request.View){case "preferences":dialog=new PreferencesForm(preferences);break;case "shortcuts":dialog=new ShortcutForm(preferences.ActionShortcuts,new string[0],delegate{return null;});break;case "bindings":dialog=new WorkflowBindingsForm(request.RecentPaths,preferences.ActionShortcuts,delegate{return null;});break;case "picker":dialog=new WorkflowPicker(request.RecentPaths,delegate{return null;});break;case "schedule":dialog=new ScheduleForm(delegate{return "Synthetic task";});break;default:throw new InvalidDataException("Unknown settings view");}
                    using(dialog){HeadlessUi.Render(dialog,Path.Combine(Path.GetDirectoryName(reportPath),"workbench.png"));if(request.Find!=null){var search=Controls(dialog).OfType<TextBox>().Single(t=>t.AccessibleName!=null&&t.AccessibleName.StartsWith("Search "));search.Text=request.Find;}HeadlessUi.Render(dialog,Path.Combine(Path.GetDirectoryName(reportPath),"workbench.png"));result.Hidden=!dialog.Visible;result.Output=Controls(dialog).OfType<Label>().Select(l=>l.Text).ToList();var list=Controls(dialog).OfType<ListView>().FirstOrDefault();result.Steps=list==null?Controls(dialog).OfType<ShortcutBox>().Count():list.Items.Count;}
                }
                else if(request.Mode=="ui") {
                    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                    using(var form=new MainForm(request.Macro,null,false,false,true,new Preferences{DarkMode=true,AccentStyle="Aftershock"})) {
                        HeadlessUi.Render(form,Path.Combine(Path.GetDirectoryName(reportPath),"workbench.png"));
                        form.SelectHeadlessStep(request.Select);
                        var list=Controls(form).OfType<MacroListView>().Single();
                        if(request.MoveStart>=0)result.Moved=form.DropHeadless(request.MoveStart,request.MoveGap);
                        if(request.Find!=null)form.FindHeadless(request.Find,0);
                        var doc=form.DocumentHeadless();result.Output=doc.Steps.Select(s=>s.Text).ToList();result.Steps=doc.Steps.Count;
                        result.Selected=list.SelectedIndices.Count==0?-1:list.SelectedIndices[0];
                        result.Matches=StepPresentation.Find(doc.Steps,request.Find??"").ToList();
                        HeadlessUi.Render(form,Path.Combine(Path.GetDirectoryName(reportPath),"workbench.png"),true);result.Hidden=!form.Visible;result.StepNumbersFit=list.StepNumbersFit;
                    }
                }
                else if(request.Mode=="run") {
                    if(request.DeadlineMs<1||request.DeadlineMs>45000||request.ResumeAfterMs<0||request.ResumeAfterMs>5000)throw new InvalidDataException("Invalid probe deadline or resume delay");
                    using(var cancel=new CancellationTokenSource(request.DeadlineMs)) {
                        var host=new ProbeHost(request,result);var runner=new Runner(host);var token=cancel.Token;
                        runner.PauseChanged+=delegate(bool paused) { if(!paused)return;result.Pauses++;if(request.ResumeAfterMs>0)Task.Run(async delegate {try{await Task.Delay(request.ResumeAfterMs,token);runner.Paused=false;}catch(OperationCanceledException){}}); };
                        runner.Progress+=delegate { result.Dispatches++;if(request.PauseAt>0&&result.Dispatches==request.PauseAt)runner.Paused=true;if(result.Dispatches>20000||(request.CancelAt>0&&result.Dispatches==request.CancelAt))cancel.Cancel(); };
                        try { runner.Run(plan,request.Repetitions,request.Speed,cancel.Token).GetAwaiter().GetResult(); }
                        finally { result.PausedAtEnd=runner.Paused; }
                    }
                }
                else throw new InvalidDataException("Unknown headless probe mode");
            } catch(OperationCanceledException) { result.Cancelled=true; }
            catch(Exception error) { result.Error=error.Message;result.ErrorType=error.GetType().Name; }
            result.ElapsedMs=watch.ElapsedMilliseconds;File.WriteAllText(reportPath,Storage.Serializer().Serialize(result));return 0;
        }
    }
}
