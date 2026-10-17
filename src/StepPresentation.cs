using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MacroExecute
{
    internal static class StepPresentation
    {
        public static string Name(Step step){if((step.Kind==ActionKind.MouseClick||step.Kind==ActionKind.ClickImage)&&(step.MouseModifiers!=MouseModifiers.None||step.ClickCount>1))return ClickPresentation.Keys(step.MouseModifiers)+(step.ClickCount>1?(step.ClickCount==2?"Double ":"Triple "):"")+ActionArt.Name(step.Kind);if(step.Negate&&step.Kind==ActionKind.IfImage)return "IF image NOT found";if(step.Negate&&step.Kind==ActionKind.IfPixel)return "IF pixel NOT found";return ActionArt.Name(step.Kind);}
        public static string Duration(int milliseconds){return milliseconds<1000?milliseconds+" ms":(milliseconds/1000m).ToString("0.###",CultureInfo.InvariantCulture)+" s";}
        public static string Summary(Step step)
        {
            if(step.Kind==ActionKind.MouseClick)return ClickPresentation.Detail(step)+" · "+step.ToString();
            if(step.Kind==ActionKind.CopySelection)return "Ctrl+C"+(step.ClipboardWaitForChange?" · fresh text within "+Duration(step.TimeoutMs):" · no clipboard wait");
            if(step.Kind==ActionKind.IfClipboard)return (step.ClipboardNegate?"NOT ":"")+step.ClipboardMatch+(step.ClipboardIgnoreCase?" · ignore case":" · case-sensitive")+" · "+step.Text.Replace("\r","").Replace("\n"," | ");
            if(step.Kind==ActionKind.Launch)return step.Target+(string.IsNullOrWhiteSpace(step.Text)?"":" · Arguments: "+step.Text);
            if(step.Kind==ActionKind.Delay)return step.DelayMaxMs>0?Duration(step.DelayMs)+" – "+Duration(step.DelayMaxMs)+" (random)":Duration(step.DelayMs);
            if(step.Kind==ActionKind.Repeat)return step.Count==0?"Until Stop or Break":step.CountMax>0?step.Count+" – "+step.CountMax+" repetitions (random)":step.Count+" repetitions";
            if(ImageTemplate.IsImage(step.Kind)){
                bool rectangle=step.SearchIn==SearchArea.Rectangle||(step.SearchIn==SearchArea.Legacy&&step.Width>0&&step.Height>0);
                string area=rectangle?"Selected area "+step.Width+"×"+step.Height:step.SearchIn==SearchArea.ForegroundWindow?"Active window":"Entire desktop";
                bool waits=step.WaitForever||step.WaitForMatch||step.Kind==ActionKind.WaitImage||step.Kind==ActionKind.ClickImage;
                string timing=step.WaitForever?"wait until Stop":waits?"wait up to "+Duration(step.TimeoutMs):"check once";
                return (string.IsNullOrEmpty(step.ImageData)?string.IsNullOrWhiteSpace(step.Target)?"No reference image":step.Target:"Embedded image")+(step.AdditionalImages.Length>0?" · "+(step.AdditionalImages.Length+1)+" images · "+(step.RequireAllImages?"all required":"first hit"):"")+" · "+area+" · "+timing+" · tolerance "+step.Tolerance+(step.ToleranceMode==ImageToleranceMode.LegacyRgbPlusOne?" (+1 legacy)":"")+(step.ContinueIfMissing?" · continue if missing":"")+(step.Kind==ActionKind.ClickImage?" · "+ClickPresentation.Detail(step):"");
            }
            if(step.Kind==ActionKind.TypeText)return step.Text.Replace("\r\n"," ↵ ").Replace("\n"," ↵ ").Replace("\r"," ↵ ");
            return step.ToString();
        }
        public static int[] Find(IList<Step> steps,string query){var words=(query??"").Split(new[]{' ','\t','\r','\n'},StringSplitOptions.RemoveEmptyEntries);if(words.Length==0)return new int[0];return Enumerable.Range(0,steps.Count).Where(i=>words.All(w=>(Name(steps[i])+" "+Summary(steps[i])+" "+steps[i].Text+" "+steps[i].Target+(!steps[i].Enabled?" disabled off":"")).IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();}
        public static bool BalancedTail(IList<Step> steps,int start){int depth=0;for(int i=start;i<steps.Count;i++){var k=steps[i].Kind;if(Plan.IsIf(k)||k==ActionKind.Repeat)depth++;else if(k==ActionKind.Else&&depth==0)return false;else if(k==ActionKind.EndIf||k==ActionKind.EndRepeat){if(--depth<0)return false;}}return depth==0;}
    }
    internal static class TransportLabels
    {
        public static string Play(bool busy,bool recording,bool paused){return recording?"▶ Play":!busy?"▶ Play":paused?"▶ Resume":"Ⅱ Pause";}
        public static string Pause(bool paused){return paused?"▶ Resume":"Ⅱ Pause";}
        public static string Record(bool recording){return recording?"■ Finish":"● Record";}
    }
}
