using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroExecute
{
    public static class Behavior
    {
        public static int EffectiveTolerance(Step step) { return step.ToleranceMode==ImageToleranceMode.LegacyRgbPlusOne?Math.Min(255,step.Tolerance+1):step.Tolerance; }
    }
    public interface IKeyboardDesktop { void Key(int key,bool up,bool extended); void Unicode(char value); }
    internal sealed class KeyboardDesktop : IKeyboardDesktop, IKeyboardState
    {
        public bool IsDown(int key){return (Native.GetAsyncKeyState(key)&0x8000)!=0;}
        public void Key(int key,bool up,bool extended) { if(!up)InputPermissions.EnsureForeground();Native.Key(key,up,extended); }
        public void Unicode(char value) { InputPermissions.EnsureForeground();Native.Unicode(value); }
    }
    public interface ITextClipboard
    {
        string ReadText(); object Snapshot(); uint Sequence {get;}
        void SetText(string text); void Restore(object snapshot);
    }
    internal sealed class TextClipboard : ITextClipboard
    {
        private readonly Control ui;
        public TextClipboard(Control ui) { this.ui=ui; }
        private T Invoke<T>(Func<T> action) { return ui.InvokeRequired?(T)ui.Invoke(action):action(); }
        public uint Sequence {get{return ClipboardSequence();}}
        [System.Runtime.InteropServices.DllImport("user32.dll",EntryPoint="GetClipboardSequenceNumber")] private static extern uint ClipboardSequence();
        public string ReadText(){return Invoke<string>(delegate{return Clipboard.ContainsText()?Clipboard.GetText():null;});}
        public object Snapshot() { return Invoke<object>(delegate{var source=Clipboard.GetDataObject();if(source==null)return null;var snapshot=new DataObject();foreach(string format in source.GetFormats(false))snapshot.SetData(format,false,source.GetData(format,false));return snapshot;}); }
        public void SetText(string text) { Invoke(delegate{if(text.Length==0)Clipboard.Clear();else Clipboard.SetText(text);return true;}); }
        public void Restore(object snapshot) { Invoke(delegate{if(snapshot==null)Clipboard.Clear();else Clipboard.SetDataObject(snapshot,true);return true;}); }
    }
    public static class TextPlayback
    {
        public static async Task Run(string text,Step step,IKeyboardDesktop keyboard,ITextClipboard clipboard,Func<int,CancellationToken,Task> wait,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if(text.Length==0)return;
            if(step.TextEntry==TextEntryMode.ClipboardPaste) {
                object snapshot=clipboard.Snapshot();uint written=0;bool replaced=false;
                try{token.ThrowIfCancellationRequested();clipboard.SetText(text);written=clipboard.Sequence;replaced=true;await Paste(keyboard,step.InputHoldMs,wait,token);await wait(Math.Max(100,step.TextIntervalMs),token);}
                finally{if(replaced&&written!=0&&clipboard.Sequence==written)clipboard.Restore(snapshot);}
                return;
            }
            for(int i=0;i<text.Length;i++) {
                token.ThrowIfCancellationRequested();await wait(0,token);
                char value=text[i];
                if(value=='\r'||value=='\n'){if(value=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;await KeyPress(keyboard,13,step.InputHoldMs,wait,token);}
                else if(value=='\t')await KeyPress(keyboard,9,step.InputHoldMs,wait,token);
                else {keyboard.Unicode(value);if(char.IsHighSurrogate(value)&&i+1<text.Length&&char.IsLowSurrogate(text[i+1]))keyboard.Unicode(text[++i]);}
                if(step.TextIntervalMs>0)await wait(step.TextIntervalMs,token);else if((i&31)==0)await wait(1,token);
            }
        }
        public static async Task KeyPress(IKeyboardDesktop keyboard,int key,int hold,Func<int,CancellationToken,Task> wait,CancellationToken token)
        { token.ThrowIfCancellationRequested();keyboard.Key(key,false,false);try{await wait(hold,token);}finally{keyboard.Key(key,true,false);} }
        public static async Task Copy(IKeyboardDesktop keyboard,int hold,Func<int,CancellationToken,Task> wait,CancellationToken token)
        { token.ThrowIfCancellationRequested();keyboard.Key(17,false,false);try{await KeyPress(keyboard,67,hold,wait,token);}finally{keyboard.Key(17,true,false);} }
        public static async Task Paste(IKeyboardDesktop keyboard,int hold,Func<int,CancellationToken,Task> wait,CancellationToken token)
        { token.ThrowIfCancellationRequested();keyboard.Key(17,false,false);try{await KeyPress(keyboard,86,hold,wait,token);}finally{keyboard.Key(17,true,false);} }
    }
}
