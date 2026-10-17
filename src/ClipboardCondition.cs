using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MacroExecute
{
    public enum ClipboardMatchMode { Exact, Any, All, Groups, Rules }
    public static class ClipboardCondition
    {
        public static string[][] Groups(Step step)
        {
            var groups=new List<string[]>();var lines=new List<string>();
            foreach(string raw in step.Text.Replace("\r\n","\n").Replace('\r','\n').Split('\n')){
                string line=raw.Trim();if(line.Length==0){if(step.ClipboardMatch==ClipboardMatchMode.Groups&&lines.Count>0){groups.Add(lines.ToArray());lines.Clear();}}else lines.Add(line);
            }
            if(lines.Count>0)groups.Add(lines.ToArray());return groups.ToArray();
        }
        public static void Validate(Step step)
        {
            if(!Enum.IsDefined(typeof(ClipboardMatchMode),step.ClipboardMatch))throw new InvalidDataException("Unknown clipboard comparison mode.");
            if(step.ClipboardMatch==ClipboardMatchMode.Exact)return;
            if(step.ClipboardMatch==ClipboardMatchMode.Rules){var parsed=ClipboardRules.Parse(step.Text);if(step.ClipboardReuseGroups==null||step.ClipboardReuseGroups.Any(i=>i<0||i>=parsed.Length)||step.ClipboardReuseGroups.Distinct().Count()!=step.ClipboardReuseGroups.Length)throw new InvalidDataException("Invalid clipboard group reuse settings.");return;}
            var groups=Groups(step);int count=groups.Sum(g=>g.Length);
            if(count==0)throw new InvalidDataException("Enter at least one non-empty string to check.");
            if(count>128)throw new InvalidDataException("Use at most 128 clipboard strings per condition.");
        }
        public static bool Matches(string clipboard,Step step)
        {
            Validate(step);if(string.IsNullOrEmpty(clipboard))return false;
            var comparison=step.ClipboardIgnoreCase?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal;bool match;
            if(step.ClipboardMatch==ClipboardMatchMode.Exact)match=string.Equals(clipboard,step.Text,comparison);
            else if(step.ClipboardMatch==ClipboardMatchMode.Rules)match=ClipboardRules.Matches(clipboard,ClipboardRules.Parse(step.Text),comparison,step.ClipboardReuseGroups);
            else{var groups=Groups(step);Func<string,bool> contains=term=>clipboard.IndexOf(term,comparison)>=0;match=step.ClipboardMatch==ClipboardMatchMode.Any?groups.SelectMany(g=>g).Any(contains):step.ClipboardMatch==ClipboardMatchMode.All?groups.SelectMany(g=>g).All(contains):groups.Any(g=>g.All(contains));}
            return step.ClipboardNegate?!match:match;
        }
        public static async Task<string> Read(ITextClipboard clipboard,Func<int,CancellationToken,Task> wait,CancellationToken token)
        {
            for(int elapsed=0;;elapsed+=25){token.ThrowIfCancellationRequested();try{return clipboard.ReadText();}catch(Exception error){if(!(error.GetBaseException() is System.Runtime.InteropServices.ExternalException)||elapsed>=1000)throw;}await wait(25,token);}
        }
        public static async Task Copy(Step step,IKeyboardDesktop keyboard,ITextClipboard clipboard,Func<int,CancellationToken,Task> wait,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();uint before=clipboard.Sequence;
            await TextPlayback.Copy(keyboard,step.InputHoldMs,wait,token);
            if(!step.ClipboardWaitForChange)return;
            int elapsed=0;
            while(true){
                token.ThrowIfCancellationRequested();
                try{if(clipboard.Sequence!=before&&!string.IsNullOrEmpty(clipboard.ReadText()))return;}catch(Exception error){if(!(error.GetBaseException() is System.Runtime.InteropServices.ExternalException))throw;}
                if(elapsed>=step.TimeoutMs)throw new TimeoutException("Copy timed out: no fresh clipboard text. Focus the target window and hover the item before Ctrl+C.");
                int slice=Math.Min(step.PollMs,step.TimeoutMs-elapsed);await wait(slice,token);elapsed+=slice;
            }
        }
    }
}
