using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MacroExecute
{
    internal static class KeyboardChord
    {
        public static List<Step> Create(Keys chord)
        {
            Keys key=chord&Keys.KeyCode;if((int)key<1||(int)key>255||Shortcuts.IsModifier(key)||key==Keys.LWin||key==Keys.RWin||(chord&~(Keys.KeyCode|Keys.Control|Keys.Alt|Keys.Shift))!=Keys.None)throw new ArgumentException("Choose a normal key, optionally with Ctrl, Shift or Alt.");
            var modifiers=new List<int>();if((chord&Keys.Control)!=0)modifiers.Add(17);if((chord&Keys.Shift)!=0)modifiers.Add(16);if((chord&Keys.Alt)!=0)modifiers.Add(18);var steps=new List<Step>();foreach(int code in modifiers)steps.Add(new Step{Kind=ActionKind.KeyDown,KeyCode=code});steps.Add(new Step{Kind=ActionKind.KeyPress,KeyCode=(int)key,Extended=Extended(key)});modifiers.Reverse();foreach(int code in modifiers)steps.Add(new Step{Kind=ActionKind.KeyUp,KeyCode=code});return steps;
        }
        public static bool Extended(Keys key){return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||key==Keys.Insert||key==Keys.Delete||key==Keys.Home||key==Keys.End||key==Keys.PageUp||key==Keys.PageDown||key==Keys.Divide||key==Keys.NumLock||key==Keys.RControlKey||key==Keys.RMenu;}
        public static bool Matches(string query){var words=(query??"").Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);if(words.Length==0)return false;foreach(string word in words)if("keyboard shortcut hotkey key combination ctrl control shift alt".IndexOf(word,StringComparison.OrdinalIgnoreCase)<0)return false;return true;}
    }
    internal sealed class KeyboardComposer : ThemeWindow
    {
        private readonly ShortcutBox capture=new ShortcutBox(new Shortcut{Command=AppCommand.EditStep});
        public Keys Chord {get{return capture.Binding.Chord;}}
        public KeyboardComposer()
        {
            Text="Insert keyboard shortcut";Size=new Size(600,330);MinimumSize=new Size(540,330);Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterParent;var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=4};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,74));root.RowStyles.Add(new RowStyle(SizeType.Absolute,42));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,44));Controls.Add(root);root.BringToFront();
            root.Controls.Add(new Label{Text="Click the field, then press a combination such as Ctrl+C.\nThis adds ordinary key-down, key-press and key-up steps. Modifiers are released in reverse order.",Dock=DockStyle.Fill,Tag="muted"},0,0);var frame=new FieldFrame{Dock=DockStyle.Fill};capture.AccessibleName="Keyboard shortcut to insert";capture.BorderStyle=BorderStyle.None;capture.Dock=DockStyle.Fill;frame.Controls.Add(capture);root.Controls.Add(frame,0,1);
            var presets=new FlowLayoutPanel{Dock=DockStyle.Fill};foreach(var preset in new[]{Keys.Control|Keys.C,Keys.Control|Keys.V,Keys.Control|Keys.A}){Keys value=preset;var button=new ThemeButton{Text=new KeysConverter().ConvertToString(value),AutoSize=true,Height=32};button.Click+=delegate{capture.Binding.Key=(int)(value&Keys.KeyCode);capture.Binding.Modifiers=(int)(value&Keys.Modifiers);capture.Text=capture.Binding.ToString();};presets.Controls.Add(button);}root.Controls.Add(presets,0,2);
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};var insert=new ThemeButton{Text="Insert steps",AutoSize=true,Height=34,Tag="primary"};var cancel=new ThemeButton{Text="Cancel",AutoSize=true,Height=34,DialogResult=DialogResult.Cancel};footer.Controls.Add(insert);footer.Controls.Add(cancel);CancelButton=cancel;var message=new Label{AutoSize=true,Tag="danger"};presets.Controls.Add(message);insert.Click+=delegate{try{KeyboardChord.Create(Chord);DialogResult=DialogResult.OK;Close();}catch(Exception error){message.Text=error.Message;}};root.Controls.Add(footer,0,3);UiTheme.Apply(this);
        }
    }
}
