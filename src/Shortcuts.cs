using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    public enum AppCommand { Record=1, PlayPause=2, Stop=3, Pause=4, New=5, Open=6, Save=7, SaveAs=8, Undo=9, Redo=10, EditStep=11, Delete=12, Duplicate=13, MoveUp=14, MoveDown=15, PlayFrom=16, PlayBlock=17, ToggleTheme=18, FindAction=19, FindStep=20, NextMatch=21, PreviousMatch=22 }
    public sealed class Shortcut
    {
        public AppCommand Command {get;set;}
        public int Key {get;set;}
        public int Modifiers {get;set;}
        public Keys Chord {get{return (Keys)(Key|Modifiers);}}
        public Shortcut Clone(){return (Shortcut)MemberwiseClone();}
        public override string ToString(){return Key==0?"Not assigned":new KeysConverter().ConvertToString(Chord);}
        public uint NativeModifiers {get{return 0x4000u|((Modifiers&(int)Keys.Alt)!=0?1u:0u)|((Modifiers&(int)Keys.Control)!=0?2u:0u)|((Modifiers&(int)Keys.Shift)!=0?4u:0u);}}
    }
    public static class Shortcuts
    {
        public static Shortcut[] Active=Defaults();
        public static string Caption(AppCommand command){return Active.First(s=>s.Command==command).ToString();}
        public static bool Global(AppCommand command){return (int)command<=4;}
        public static string Name(AppCommand c){switch(c){case AppCommand.Record:return "Start / finish recording";case AppCommand.PlayPause:return "Play / pause / resume";case AppCommand.Stop:return "Stop recording / playback";case AppCommand.Pause:return "Pause / resume only";case AppCommand.EditStep:return "Edit selected step";case AppCommand.PlayFrom:return "Play from selected row";case AppCommand.PlayBlock:return "Play selected block";case AppCommand.FindStep:return "Find in this macro";case AppCommand.NextMatch:return "Next matching step";case AppCommand.PreviousMatch:return "Previous matching step";case AppCommand.FindAction:return "Find an action";case AppCommand.ToggleTheme:return "Toggle dark mode";default:return System.Text.RegularExpressions.Regex.Replace(c.ToString(),"([a-z])([A-Z])","$1 $2");}}
        private static Shortcut Bind(AppCommand c,Keys keys){return new Shortcut{Command=c,Key=(int)(keys&Keys.KeyCode),Modifiers=(int)(keys&Keys.Modifiers)};}
        public static Shortcut[] Defaults(){return new[]{Bind(AppCommand.Record,Keys.F8),Bind(AppCommand.PlayPause,Keys.F9),Bind(AppCommand.Stop,Keys.F10),Bind(AppCommand.Pause,Keys.F7),Bind(AppCommand.New,Keys.Control|Keys.N),Bind(AppCommand.Open,Keys.Control|Keys.O),Bind(AppCommand.Save,Keys.Control|Keys.S),Bind(AppCommand.SaveAs,Keys.Control|Keys.Shift|Keys.S),Bind(AppCommand.Undo,Keys.Control|Keys.Z),Bind(AppCommand.Redo,Keys.Control|Keys.Y),Bind(AppCommand.EditStep,Keys.Enter),Bind(AppCommand.Delete,Keys.Delete),Bind(AppCommand.Duplicate,Keys.Control|Keys.D),Bind(AppCommand.MoveUp,Keys.Alt|Keys.Up),Bind(AppCommand.MoveDown,Keys.Alt|Keys.Down),Bind(AppCommand.PlayFrom,Keys.Control|Keys.F9),Bind(AppCommand.PlayBlock,Keys.Shift|Keys.F9),Bind(AppCommand.ToggleTheme,Keys.Control|Keys.Shift|Keys.T),Bind(AppCommand.FindAction,Keys.Control|Keys.F),Bind(AppCommand.FindStep,Keys.Control|Keys.Shift|Keys.F),Bind(AppCommand.NextMatch,Keys.F3),Bind(AppCommand.PreviousMatch,Keys.Shift|Keys.F3)};}
        public static Shortcut[] Normalize(Shortcut[] source){var defaults=Defaults();if(source==null)return defaults;foreach(var item in source){if(item==null||!Enum.IsDefined(typeof(AppCommand),item.Command))throw new InvalidDataException("Unknown shortcut action.");}if(source.Select(x=>x.Command).Distinct().Count()!=source.Length)throw new InvalidDataException("An action has multiple shortcut entries.");return defaults.Select(d=>source.FirstOrDefault(s=>s.Command==d.Command)??((int)d.Command>=(int)AppCommand.FindAction&&source.Any(s=>s.Chord==d.Chord)?new Shortcut{Command=d.Command}:d)).Select(s=>s.Clone()).ToArray();}
        public static void Validate(Shortcut[] bindings,string[] files)
        {
            var set=Normalize(bindings);var seen=new HashSet<Keys>();
            foreach(var s in set){if(s.Key<0||s.Key>255||(s.Modifiers&~(int)(Keys.Control|Keys.Shift|Keys.Alt))!=0)throw new InvalidDataException("Invalid shortcut key or modifiers.");if(s.Key==0){if(s.Command==AppCommand.Stop)throw new InvalidDataException("Stop must have a shortcut.");if(s.Modifiers!=0)throw new InvalidDataException("An unassigned shortcut cannot have modifiers.");continue;}if(IsModifier((Keys)s.Key)||s.Key==(int)Keys.LWin||s.Key==(int)Keys.RWin)throw new InvalidDataException("Choose a key in addition to modifiers; Windows keys are not supported.");if(Global(s.Command)&&s.Key==(int)Keys.F12)throw new InvalidDataException("Windows reserves F12 for debugging; choose another global shortcut.");if(!seen.Add(s.Chord))throw new InvalidDataException("Two actions use "+s+". Assign different shortcuts.");if(files!=null)for(int i=0;i<Math.Min(9,files.Length);i++)if(!string.IsNullOrWhiteSpace(files[i])&&s.Chord==(Keys.Control|Keys.Alt|(Keys)(49+i)))throw new InvalidDataException(s+" is assigned to a saved macro. Change that macro binding first.");}
        }
        public static bool IsModifier(Keys key){return key==Keys.ControlKey||key==Keys.LControlKey||key==Keys.RControlKey||key==Keys.ShiftKey||key==Keys.LShiftKey||key==Keys.RShiftKey||key==Keys.Menu||key==Keys.LMenu||key==Keys.RMenu;}
        public static bool TextEditorOwns(Keys chord){Keys key=chord&Keys.KeyCode;Keys mods=chord&Keys.Modifiers;if((mods&(Keys.Control|Keys.Alt))==0&&!(key>=Keys.F1&&key<=Keys.F24))return true;return chord==(Keys.Control|Keys.A)||chord==(Keys.Control|Keys.C)||chord==(Keys.Control|Keys.X)||chord==(Keys.Control|Keys.V)||chord==(Keys.Control|Keys.Z)||chord==(Keys.Control|Keys.Y)||chord==(Keys.Control|Keys.Insert)||chord==(Keys.Shift|Keys.Insert)||chord==(Keys.Shift|Keys.Delete);}
        public static AppCommand? Find(Shortcut[] bindings,Keys chord){var item=bindings.FirstOrDefault(x=>x.Key!=0&&x.Chord==chord);return item==null?(AppCommand?)null:item.Command;}
        public static bool RecordingControl(Shortcut[] bindings,int key,Keys modifiers){return bindings.Any(s=>Global(s.Command)&&s.Key!=0&&s.Key==key&&(Keys)s.Modifiers==modifiers);}
    }
    internal interface IHotkeyRegistrar { bool Register(int id,Shortcut binding);void Unregister(int id); }
    internal sealed class WindowsHotkeyRegistrar : IHotkeyRegistrar
    {
        private readonly IntPtr window;public WindowsHotkeyRegistrar(IntPtr window){this.window=window;}
        public bool Register(int id,Shortcut binding){return Native.RegisterHotKey(window,id,binding.NativeModifiers,(uint)binding.Key);}
        public void Unregister(int id){Native.UnregisterHotKey(window,id);}
    }
    internal sealed class HotkeyManager : IDisposable
    {
        private readonly IHotkeyRegistrar registrar;
        private readonly Dictionary<int,Shortcut> active=new Dictionary<int,Shortcut>();
        public HotkeyManager(IHotkeyRegistrar registrar){this.registrar=registrar;}
        public bool Registered(AppCommand command){return active.ContainsKey((int)command);}
        public bool TryApply(Shortcut[] requested,out string error)
        {
            requested=Shortcuts.Normalize(requested);
            Shortcuts.Validate(requested,null);var previous=active.Values.Select(s=>s.Clone()).ToArray();Dispose();
            foreach(var item in requested.Where(s=>Shortcuts.Global(s.Command)&&s.Key!=0)){
                if(registrar.Register((int)item.Command,item)){active[(int)item.Command]=item.Clone();continue;}
                Dispose();var failedRestore=new List<string>();foreach(var old in previous)if(registrar.Register((int)old.Command,old))active[(int)old.Command]=old;else failedRestore.Add(Shortcuts.Name(old.Command));
                error=item+" is already in use by another application ("+Shortcuts.Name(item.Command)+"). Choose another shortcut.";if(failedRestore.Count>0)error+=" Could not restore: "+string.Join(", ",failedRestore)+". Playback stays disabled until Stop is registered.";return false;
            }
            error=null;return true;
        }
        public void Dispose(){foreach(var id in active.Keys.ToArray())registrar.Unregister(id);active.Clear();}
    }
}
