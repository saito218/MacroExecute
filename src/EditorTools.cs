using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    public static class EditorBlocks
    {
        public static int End(IList<Step> steps,int index)
        {
            if(index<0||index>=steps.Count)return index;
            if(!Plan.IsIf(steps[index].Kind)&&steps[index].Kind!=ActionKind.Repeat)return index;
            int depth=0;
            for(int i=index;i<steps.Count;i++){var kind=steps[i].Kind;if(Plan.IsIf(kind)||kind==ActionKind.Repeat)depth++;if(kind==ActionKind.EndIf||kind==ActionKind.EndRepeat)if(--depth==0)return i;}
            throw new InvalidOperationException("This block has no matching end.");
        }
        public static bool IsBoundary(ActionKind kind){return kind==ActionKind.EndIf||kind==ActionKind.EndRepeat||kind==ActionKind.Else;}
        public static List<Step> Create(Step step){var result=new List<Step>{step};if(Plan.IsIf(step.Kind)){result.Add(new Step{Kind=ActionKind.Else});result.Add(new Step{Kind=ActionKind.EndIf});}else if(step.Kind==ActionKind.Repeat)result.Add(new Step{Kind=ActionKind.EndRepeat});return result;}
        public static List<Step> Copy(IList<Step> steps,int start)
        {
            int end=End(steps,start);var result=steps.Skip(start).Take(end-start+1).Select(s=>s.Clone()).ToList();
            var names=new HashSet<string>(steps.Where(s=>s.Kind==ActionKind.Label).Select(s=>s.Text));var renamed=new Dictionary<string,string>();
            foreach(var label in result.Where(s=>s.Kind==ActionKind.Label)){string name=label.Text;int suffix=2;while(names.Contains(name+"_copy"+suffix))suffix++;string replacement=name+"_copy"+suffix;names.Add(replacement);renamed[name]=replacement;label.Text=replacement;}
            foreach(var jump in result.Where(s=>s.Kind==ActionKind.Jump))if(renamed.ContainsKey(jump.Text))jump.Text=renamed[jump.Text];return result;
        }
        public static int Previous(IList<Step> steps,int index)
        {
            int prior=index-1;if(prior<0)return -1;
            var kind=steps[prior].Kind;if(kind==ActionKind.Else)return -1;
            if(kind==ActionKind.EndIf||kind==ActionKind.EndRepeat){for(int i=prior-1;i>=0;i--)if((Plan.IsIf(steps[i].Kind)||steps[i].Kind==ActionKind.Repeat)&&End(steps,i)==prior)return i;}
            return prior;
        }
    }
    // Original, code-drawn symbols; no third-party resource assets.
    internal static class ActionArt
    {
        public static string Name(ActionKind k)
        {
            switch(k){case ActionKind.CopySelection:return "Copy to clipboard (Ctrl+C)";case ActionKind.IfClipboard:return "IF clipboard matches";case ActionKind.Launch:return "Run program / file";case ActionKind.IfImage:return "IF image found";case ActionKind.ClickImage:return "Click image";case ActionKind.WaitImage:return "Wait for image";case ActionKind.FindImage:return "Find image";case ActionKind.MouseClick:return "Mouse click";case ActionKind.TypeText:return "Type text";case ActionKind.KeyPress:return "Press key";case ActionKind.Delay:return "Wait / delay";case ActionKind.Repeat:return "Repeat block";case ActionKind.EndIf:return "END IF";case ActionKind.EndRepeat:return "END REPEAT";case ActionKind.Else:return "ELSE";default:return System.Text.RegularExpressions.Regex.Replace(k.ToString(),"([a-z])([A-Z])","$1 $2");}
        }
        public static Bitmap Icon(ActionKind kind,int size) { return IconArt.Action(kind,size); }
        public static Button Button(ActionKind kind,EventHandler handler)
        {
            var b=new ThemeButton{Text=Name(kind),AccessibleName="Add "+Name(kind),Image=Icon(kind,22),TextImageRelation=TextImageRelation.ImageAboveText,Size=new Size(96,48),FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Color.FromArgb(36,52,75),Margin=new Padding(2),Padding=Padding.Empty,Font=new Font("Segoe UI",8.5f)};b.Width=Math.Max(94,TextRenderer.MeasureText(b.Text,b.Font).Width+12);b.FlatAppearance.BorderColor=Color.FromArgb(219,226,235);b.Click+=handler;b.Disposed+=delegate{if(b.Image!=null)b.Image.Dispose();};return b;
        }
    }
}
