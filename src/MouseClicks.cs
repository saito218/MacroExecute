using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
namespace MacroExecute
{
    [Flags] public enum MouseModifiers { None=0, Ctrl=1, Shift=2, Alt=4 }
    public interface IKeyboardState { bool IsDown(int key); }
    public sealed partial class WindowsHost
    {
        private bool ModifierHeld(int key)
        {
            int left=key==17?162:key==16?160:164;var state=keyboard as IKeyboardState;
            return keys.Contains(key)||keys.Contains(left)||keys.Contains(left+1)||(state!=null&&(state.IsDown(key)||state.IsDown(left)||state.IsDown(left+1)));
        }
        private async Task Click(Step step,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();var acquired=new List<int>();Exception failure=null;bool buttonOwned=false;
            try{
                foreach(int key in new[]{17,16,18}){var flag=key==17?MouseModifiers.Ctrl:key==16?MouseModifiers.Shift:MouseModifiers.Alt;if((step.MouseModifiers&flag)==0||ModifierHeld(key))continue;acquired.Add(key);keys.Add(key);keyboard.Key(key,false,false);}
                for(int i=0;i<step.ClickCount;i++){
                    token.ThrowIfCancellationRequested();buttonOwned=true;buttons.Add(step.Button);desktop.Button(step.Button,false);
                    await Wait(step.InputHoldMs,token);desktop.Button(step.Button,true);buttons.Remove(step.Button);buttonOwned=false;
                    if(i+1<step.ClickCount)await Wait(step.ClickIntervalMs,token);
                }
            }catch(Exception error){failure=error;}
            finally{
                if(buttonOwned)try{desktop.Button(step.Button,true);buttons.Remove(step.Button);}catch(Exception error){if(failure==null)failure=error;}
                for(int i=acquired.Count-1;i>=0;i--)try{keyboard.Key(acquired[i],true,false);keys.Remove(acquired[i]);}catch(Exception error){if(failure==null)failure=error;}
            }
            if(failure!=null)ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
    internal static class ClickPresentation
    {
        internal static string Keys(MouseModifiers value){return value==MouseModifiers.None?"":((value&MouseModifiers.Ctrl)!=0?"Ctrl+":"")+((value&MouseModifiers.Shift)!=0?"Shift+":"")+((value&MouseModifiers.Alt)!=0?"Alt+":"");}
        internal static string Detail(Step step){return Keys(step.MouseModifiers)+step.Button+" · "+(step.ClickCount==1?"single":step.ClickCount==2?"double":"triple")+" click · hold "+StepPresentation.Duration(step.InputHoldMs)+(step.ClickCount>1?" · gap "+StepPresentation.Duration(step.ClickIntervalMs):"");}
    }
}
