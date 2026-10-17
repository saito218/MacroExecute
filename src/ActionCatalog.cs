using System;
using System.Collections.Generic;
using System.Linq;

namespace MacroExecute
{
    internal static class ActionCatalog
    {
        public static string Describe(ActionKind kind)
        {
            switch(kind){
                case ActionKind.CopySelection:return "Send Ctrl+C to the focused app, then optionally wait for fresh copied text.";
                case ActionKind.Launch:return "Run a program or open a file in its Windows default app, with optional arguments.";
                case ActionKind.MouseClick:return "Click a fixed point, current pointer or last match; supports Alt/Ctrl/Shift, double/triple click and long press.";
                case ActionKind.TypeText:return "Type a message or text into the active application.";
                case ActionKind.KeyPress:return "Press a key or a keyboard shortcut.";
                case ActionKind.Delay:return "Wait for a duration before the next action.";
                case ActionKind.ClickImage:return "Find a reference image and click its center automatically.";
                case ActionKind.FindImage:return "Find an image and remember its position without clicking.";
                case ActionKind.WaitImage:return "Wait until a reference image appears on screen.";
                case ActionKind.IfImage:return "If an image is found, run the true branch; otherwise run ELSE.";
                case ActionKind.Repeat:return "Loop the steps inside a block a set number of times or forever.";
                case ActionKind.Include:return "Run another MacroExecute file as part of this workflow.";
                case ActionKind.Note:return "Add a comment to explain this part of your workflow.";
                case ActionKind.Pause:return "Pause playback until you choose to resume.";
                case ActionKind.MouseMove:return "Move the pointer without clicking. Choose fixed coordinates or a remembered image match.";
                case ActionKind.MouseDown:return "Hold a mouse button down. Pair with Mouse up to release it after a drag.";
                case ActionKind.MouseUp:return "Release a held mouse button at the chosen position.";
                case ActionKind.MouseWheel:return "Scroll the focused window by the configured wheel amount.";
                case ActionKind.KeyDown:return "Hold a keyboard key down until a matching Key up releases it.";
                case ActionKind.KeyUp:return "Release a held keyboard key.";
                case ActionKind.SetClipboard:return "Replace the clipboard contents with your text.";
                case ActionKind.Paste:return "Send Ctrl+V to paste the current clipboard contents into the focused field.";
                case ActionKind.ClipboardClear:return "Empty the clipboard contents.";
                case ActionKind.ClipboardIncrement:return "Increase a numeric value stored in the clipboard.";
                case ActionKind.TypeDate:return "Insert the current date or time using your chosen format.";
                case ActionKind.TypeEnvironment:return "Type the value of a Windows environment variable.";
                case ActionKind.ActivateWindow:return "Bring a matching window to the foreground before sending input.";
                case ActionKind.WaitWindow:return "Wait for a matching window to exist before continuing.";
                case ActionKind.MoveWindow:return "Move or resize a matching window.";
                case ActionKind.OpenUrl:return "Open a web address in your default browser.";
                case ActionKind.CopyFile:return "Copy a file to a new destination path.";
                case ActionKind.DeleteFile:return "Delete the specified file during playback.";
                case ActionKind.RunCSharp:return "Run trusted C# code in a separate process.";
                case ActionKind.WaitProcess:return "Wait for a process to start before continuing.";
                case ActionKind.FindPixel:return "Find a matching screen color and remember its position.";
                case ActionKind.WaitPixel:return "Wait for a matching screen color before continuing.";
                case ActionKind.IfPixel:return "Run the IF branch when a matching pixel is found; otherwise run ELSE.";
                case ActionKind.IfWindow:return "Run the IF branch when a matching window exists; otherwise run ELSE.";
                case ActionKind.IfClipboard:return "Check clipboard text using string rows, AND requirements and alternative groups.";
                case ActionKind.IfFile:return "Run the IF branch when the specified file exists; otherwise run ELSE.";
                case ActionKind.IfProcess:return "Run the IF branch when the process is running; otherwise run ELSE.";
                case ActionKind.Label:return "Name a destination that a Jump action can return to.";
                case ActionKind.Jump:return "Continue playback from a named Label in an active scope.";
                case ActionKind.Break:return "Leave the nearest Repeat block and continue after its END REPEAT.";
                case ActionKind.Message:return "Show a message dialog during playback.";
                case ActionKind.Else:return "Start the branch used when the matching IF condition is false.";
                case ActionKind.EndIf:return "Rejoin the IF and ELSE branches, then continue with the next action.";
                case ActionKind.EndRepeat:return "Finish this repeat pass; loop again or continue after the block.";
                default:if(Plan.IsIf(kind))return "Choose a true or false branch based on this condition.";return "Add "+ActionArt.Name(kind).ToLowerInvariant()+" to your workflow.";
            }
        }
        private static string Keywords(ActionKind kind)
        {
            switch(kind){case ActionKind.Launch:return "launch execute executable application document script pdf excel word python powershell batch open file";case ActionKind.Delay:return "sleep timer milliseconds seconds";case ActionKind.Repeat:return "loop infinite forever count";case ActionKind.TypeText:return "write typing input message";case ActionKind.KeyPress:return "hotkey keyboard combination shortcut";case ActionKind.Include:return "macro subroutine call file";case ActionKind.Note:return "comment annotation description";default:return kind.ToString().Contains("Image")?"picture screenshot template visual":"";}
        }
        public static ActionKind[] Search(string query)
        {
            var words=(query??"").Trim().Split(new[]{' ','\t','\r','\n'},StringSplitOptions.RemoveEmptyEntries);
            return Enum.GetValues(typeof(ActionKind)).Cast<ActionKind>().Where(k=>!EditorBlocks.IsBoundary(k)).Where(k=>words.All(w=>(ActionArt.Name(k)+" "+Describe(k)+" "+Keywords(k)).IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0)).OrderBy(k=>ActionArt.Name(k)).ToArray();
        }
    }
    internal static class InsertionContext
    {
        private sealed class Scope {public int Start;public ActionKind Kind;public bool FalseBranch;}
        public static string Describe(IList<Step> steps,int insertion)
        {
            var scopes=new List<Scope>();for(int i=0;i<Math.Min(insertion,steps.Count);i++){var kind=steps[i].Kind;if(Plan.IsIf(kind)||kind==ActionKind.Repeat)scopes.Add(new Scope{Start=i,Kind=kind});else if(kind==ActionKind.Else&&scopes.Count>0)scopes[scopes.Count-1].FalseBranch=true;else if((kind==ActionKind.EndIf||kind==ActionKind.EndRepeat)&&scopes.Count>0)scopes.RemoveAt(scopes.Count-1);}
            if(scopes.Count==0)return "Main workflow";var scope=scopes[scopes.Count-1];return scope.Kind==ActionKind.Repeat?"Inside Repeat at step "+(scope.Start+1):"Inside the "+(scope.FalseBranch?"false":"true")+" branch of IF step "+(scope.Start+1);
        }
        public static int Parent(IList<Step> steps,int index){for(int i=index-1;i>=0;i--)if((Plan.IsIf(steps[i].Kind)||steps[i].Kind==ActionKind.Repeat)&&EditorBlocks.End(steps,i)>=index)return i;return -1;}
    }
}
