using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    public sealed partial class MainForm
    {
        private Button ShortcutTile()
        {
            var button=ActionArt.Button(ActionKind.KeyPress,delegate{Guard(AddKeyboardShortcut);});if(button.Image!=null)button.Image.Dispose();button.Image=IconArt.ControlImage("shortcut",22,UiTheme.Accent);button.Text="Keyboard shortcut";button.AccessibleName="Add Keyboard shortcut";button.Width=128;tooltips.SetToolTip(button,"Insert Ctrl/Shift/Alt combinations as explicit key-down, key-press and key-up steps, with one Undo.");return button;
        }
        private void AddKeyboardShortcut(){if(busy)return;if(headless)throw new InvalidOperationException("Interactive composer is not available during headless tests.");using(var form=new KeyboardComposer())if(ShowKeyboardComposer(form)==DialogResult.OK){CommitSequence(KeyboardChord.Create(form.Chord));SetStatus("Inserted "+new KeysConverter().ConvertToString(form.Chord)+" · "+KeyName(AppCommand.Undo)+" undoes the whole insertion.");}}
        private DialogResult ShowKeyboardComposer(Form form)
        {
            bool restore=hotkeys!=null;if(restore)hotkeys.Dispose();foreach(int id in registered)Native.UnregisterHotKey(Handle,id);registered.Clear();
            try{return InAppDialog.Show(this,form);}finally{if(restore)RegisterKeys();}
        }
        private void CommitSequence(IEnumerable<Step> steps){var added=steps.Select(s=>s.Clone()).ToList();bool enabled=EnabledInsertion();foreach(var step in added)step.Enabled=step.Enabled&&enabled;Snapshot();int index=InsertionIndex;macro.Steps.InsertRange(index,added);MarkDirty();RefreshList(index);if(!enabled)SetStatus("Added disabled steps to match the containing block.");}
        private void CommitRecording(IList<Step> recorded,int index,bool enabled){if(recorded.Count==0)return;var copy=recorded.Select(s=>s.Clone()).ToList();foreach(var step in copy)step.Enabled=step.Enabled&&enabled;Snapshot();macro.Steps.InsertRange(index,copy);MarkDirty();}
        internal void RecordingHeadless(IList<Step> steps,int index,bool enabled){if(!headless)throw new InvalidOperationException();CommitRecording(steps,index,enabled);RefreshList(steps.Count>0?index:-1);}
        private bool EnabledInsertion(){int depth=0;var stack=new Stack<bool>();for(int i=0;i<InsertionIndex;i++){var step=macro.Steps[i];if(Plan.IsIf(step.Kind)||step.Kind==ActionKind.Repeat){stack.Push(step.Enabled);if(!step.Enabled)depth++;}else if((step.Kind==ActionKind.EndIf||step.Kind==ActionKind.EndRepeat)&&stack.Count>0){if(!stack.Pop())depth--;}}return depth==0;}
        private void ReviewMacro(){if(busy)return;var items=MacroReview.Check(macro,DocumentDirectory);if(headless){SetStatus(items.Any(i=>i.Error)?"Macro needs attention.":"Macro check complete.");return;}using(var review=new MacroReviewForm(items))if(InAppDialog.Show(this,review)==DialogResult.OK&&review.SelectedStep>=0)SelectRow(review.SelectedStep);}
        private void OpenRecent(string path){if(busy)return;if(!File.Exists(path)){SetStatus("Recent workflow is no longer at that path. Choose Open to locate it.");return;}if(ConfirmDiscard())LoadFile(path);}
        internal void InsertChordHeadless(Keys keys){if(!headless)throw new InvalidOperationException();CommitSequence(KeyboardChord.Create(keys));}
    }
}
