using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MacroExecute
{
    public sealed class Plan
    {
        public int? RandomSeed;
        public List<Step> Steps = new List<Step>();
        public Dictionary<int,int> Pair = new Dictionary<int,int>();
        public Dictionary<int,int> Alternative = new Dictionary<int,int>();
        public Dictionary<string,int> Labels = new Dictionary<string,int>(StringComparer.Ordinal);
        public static Plan Build(Macro macro, string directory) { var p = new Plan{RandomSeed=macro.RandomSeed}; Expand(macro, directory, new HashSet<string>(StringComparer.OrdinalIgnoreCase), p.Steps, 0); p.Index(); return p; }
        private static void Expand(Macro macro, string directory, HashSet<string> stack, List<Step> output, int depth)
        {
            Storage.Validate(macro); if (depth > 16) throw new InvalidDataException("Include nesting exceeds 16 levels.");
            foreach (Step s in macro.Steps.Where(x => x.Enabled)) {
                if (s.Kind == ActionKind.Include) {
                    string path = Resolve(directory, s.Target);
                    if (!stack.Add(path)) throw new InvalidDataException("Include cycle at " + Path.GetFileName(path));
                    try {
                        var children = new List<Step>(); Expand(Storage.Load(path), Path.GetDirectoryName(path), stack, children, depth + 1);
                        string scope = "include_" + Guid.NewGuid().ToString("N") + "_";
                        var labels = new HashSet<string>(children.Where(x => x.Kind == ActionKind.Label).Select(x => x.Text));
                        foreach (Step c in children) { if ((c.Kind == ActionKind.Label || c.Kind == ActionKind.Jump) && labels.Contains(c.Text)) c.Text = scope + c.Text; output.Add(c); }
                    } finally { stack.Remove(path); }
                } else { Step copy = s.Clone(); if (new[] { ActionKind.Launch, ActionKind.FindImage, ActionKind.IfImage, ActionKind.WaitImage, ActionKind.ClickImage, ActionKind.CopyFile, ActionKind.DeleteFile, ActionKind.IfFile }.Contains(copy.Kind) && !string.IsNullOrWhiteSpace(copy.Target)) {copy.Target=Environment.ExpandEnvironmentVariables(copy.Target);if(copy.Kind!=ActionKind.Launch||Path.IsPathRooted(copy.Target)||copy.Target.IndexOfAny(new[]{'\\','/'})>=0||File.Exists(Path.Combine(directory,copy.Target))||Directory.Exists(Path.Combine(directory,copy.Target)))copy.Target = Resolve(directory, copy.Target);} if (copy.Kind == ActionKind.CopyFile && !string.IsNullOrWhiteSpace(copy.Text)) copy.Text = Resolve(directory, copy.Text); output.Add(copy); }
                if (output.Count > Storage.MaxSteps) throw new InvalidDataException("Expanded macro exceeds the step limit.");
            }
        }
        public static string Resolve(string directory, string path) { return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(directory, path)); }
        private void Index()
        {
            var stack = new Stack<int>();
            for (int i = 0; i < Steps.Count; i++) {
                ActionKind k = Steps[i].Kind;
                if (k == ActionKind.Label) { if (string.IsNullOrWhiteSpace(Steps[i].Text) || Labels.ContainsKey(Steps[i].Text)) throw new InvalidDataException("Labels must be nonempty and unique: " + Steps[i].Text); Labels.Add(Steps[i].Text, i); }
                if (k == ActionKind.Repeat || IsIf(k)) stack.Push(i);
                if (k == ActionKind.Else) { if (stack.Count == 0 || !IsIf(Steps[stack.Peek()].Kind) || Alternative.ContainsKey(stack.Peek())) throw new InvalidDataException("ELSE must belong to one IF."); Alternative[stack.Peek()] = i; }
                if (k == ActionKind.EndIf || k == ActionKind.EndRepeat) {
                    if (stack.Count == 0) throw new InvalidDataException("Unmatched block end at step " + (i + 1));
                    int start = stack.Pop(); if ((k == ActionKind.EndIf) != IsIf(Steps[start].Kind)) throw new InvalidDataException("Crossed IF/REPEAT blocks.");
                    Pair[start] = i; Pair[i] = start; if (Alternative.ContainsKey(start)) Pair[Alternative[start]] = i;
                }
            }
            if (stack.Count != 0) throw new InvalidDataException("Unclosed block at step " + (stack.Peek() + 1));
            for (int i = 0; i < Steps.Count; i++) {
                if (Steps[i].Kind == ActionKind.Jump && !Labels.ContainsKey(Steps[i].Text)) throw new InvalidDataException("Unknown label: " + Steps[i].Text);
                if (Steps[i].Kind == ActionKind.Break && !Pair.Any(x => x.Key < i && x.Value > i && Steps[x.Key].Kind == ActionKind.Repeat)) throw new InvalidDataException("BREAK must be inside REPEAT.");
            }
        }
        public static bool IsIf(ActionKind k) { return k == ActionKind.IfPixel || k == ActionKind.IfImage || k == ActionKind.IfWindow || k == ActionKind.IfClipboard || k == ActionKind.IfFile || k == ActionKind.IfProcess; }
    }
    public interface IHost
    {
        Task Execute(Step step, CancellationToken token);
        Task<bool> Evaluate(Step step, CancellationToken token);
        void ReleaseInputs();
    }
    public interface IScopedHost { void EnterCondition(); void LeaveCondition(); void ResetMatches(); }
    public sealed class Runner
    {
        private readonly IHost host;
        private Random random=new Random();
        private volatile bool paused;
        public event Action<bool> PauseChanged;
        public bool Paused { get { return paused; } set { if(paused==value)return;paused=value;var changed=PauseChanged;if(changed!=null)changed(value); } }
        public event Action<int,Step> Progress;
        public Runner(IHost host) { this.host = host; var windows=host as WindowsHost; if(windows!=null){windows.Images.IsPaused=delegate{return paused;};windows.Wait=Delay;} }
        public async Task Run(Plan plan, int repetitions, double speed, CancellationToken token)
        {
            if (repetitions < 0 || repetitions > 1000000 || speed < 0.1 || speed > 20 || double.IsNaN(speed)) throw new ArgumentOutOfRangeException("Playback settings");
            random=plan.RandomSeed.HasValue?new Random(plan.RandomSeed.Value):new Random();var inputHost=host as WindowsHost;if(inputHost!=null)inputHost.Images.SetRandomSeed(plan.RandomSeed);
            long budget = 0;
            bool unlimited=repetitions==0 || plan.Steps.Any(x=>x.Kind==ActionKind.Repeat && x.Count==0);
            if(plan.Steps.Count==0)return;
            try {
                for (int repetition = 0; repetitions == 0 || repetition < repetitions; repetition++) {
                    var loops = new Dictionary<int,int>(); var conditions = new Stack<int>(); var scoped = host as IScopedHost; if(scoped != null) scoped.ResetMatches();
                    for (int pc = 0; pc < plan.Steps.Count; pc++) {
                        token.ThrowIfCancellationRequested(); while (paused) await Task.Delay(40, token);
                        if ((++budget & 31) == 0) await Task.Delay(1, token);
                        while(conditions.Count > 0 && (pc <= conditions.Peek() || pc >= plan.Pair[conditions.Peek()])) { conditions.Pop(); if(scoped != null) scoped.LeaveCondition(); }
                        if (!unlimited && budget > 10000000) throw new InvalidOperationException("Playback exceeded the ten-million-step budget. Use Forever for intentional continuous playback.");
                        Step s = plan.Steps[pc]; var handler = Progress; if (handler != null) handler(pc, s);
                        if (s.Kind == ActionKind.Delay) { int ms=s.DelayMaxMs>0?random.Next(s.DelayMs,s.DelayMaxMs+1):s.DelayMs;await Delay((int)Math.Round(ms / speed), token); continue; }
                        if (s.Kind == ActionKind.Pause) { Paused = true; continue; }
                        if (s.Kind == ActionKind.Note || s.Kind == ActionKind.Label || s.Kind == ActionKind.EndIf) continue;
                        if (Plan.IsIf(s.Kind)) { conditions.Push(pc); if(scoped != null) scoped.EnterCondition(); if (!await host.Evaluate(s, token)) pc = plan.Alternative.ContainsKey(pc) ? plan.Alternative[pc] : plan.Pair[pc]; continue; }
                        if (s.Kind == ActionKind.Else) { pc = plan.Pair[pc]; continue; }
                        if (s.Kind == ActionKind.Repeat) { loops[pc] = s.CountMax>0?random.Next(s.Count,s.CountMax+1):s.Count; continue; }
                        if (s.Kind == ActionKind.EndRepeat) { int start = plan.Pair[pc]; if (!loops.ContainsKey(start)) throw new InvalidDataException("Jump entered a repeat block without its start."); if (loops[start] == 0 || --loops[start] > 0) pc = start; else loops.Remove(start); continue; }
                        if (s.Kind == ActionKind.Break) { int start = plan.Pair.Where(x => x.Key < pc && x.Value > pc && plan.Steps[x.Key].Kind == ActionKind.Repeat).Max(x => x.Key); pc = plan.Pair[start]; loops.Remove(start); continue; }
                        if (s.Kind == ActionKind.Jump) {
                            int dest = plan.Labels[s.Text];
                            foreach (int start in loops.Keys.ToArray()) if (!(dest > start && dest < plan.Pair[start])) loops.Remove(start);
                            foreach (var pair in plan.Pair.Where(x => x.Key < dest && x.Value > dest && plan.Steps[x.Key].Kind == ActionKind.Repeat)) if (!loops.ContainsKey(pair.Key)) throw new InvalidDataException("Jump cannot enter an inactive repeat block.");
                            foreach (var pair in plan.Pair.Where(x => x.Key < dest && x.Value > dest && Plan.IsIf(plan.Steps[x.Key].Kind))) if(!conditions.Contains(pair.Key)) throw new InvalidDataException("Jump cannot enter an inactive IF block.");
                            pc = dest; continue;
                        }
                        await host.Execute(s, token);
                        // Yield during action-only loops so stop and hotkey messages are serviced.
                        await Task.Delay(1, token);
                    }
                }
            } finally { var scoped = host as IScopedHost; if(scoped != null) scoped.ResetMatches(); host.ReleaseInputs(); Paused = false; }
        }
        private async Task Delay(int duration, CancellationToken token) { token.ThrowIfCancellationRequested();while(paused)await Task.Delay(40,token);int remaining = duration; while (remaining > 0) { token.ThrowIfCancellationRequested(); if (paused) { await Task.Delay(40, token); continue; } int slice = Math.Min(remaining, 20); var clock=System.Diagnostics.Stopwatch.StartNew();await Task.Delay(slice, token);remaining-=(int)Math.Min(remaining,Math.Max(1,clock.ElapsedMilliseconds)); } }
    }
}
