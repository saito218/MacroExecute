using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroExecute
{
    public sealed partial class MainForm
    {
        private static string ButtonHint(string text)
        {
            if(text.Contains("Undo"))return "Undo the last macro edit.";
            if(text.Contains("Redo"))return "Restore an undone edit.";
            if(text=="▶ From selected")return "Play from the selected action to the end; select a complete IF or Repeat block.";
            if(text=="▶ Selected block")return "Try only the selected action or its entire IF / Repeat block.";
            if(text=="Duplicate block")return "Copy the selected action, including everything inside its IF or Repeat block.";
            if(text=="Delete")return "Remove the selected action and its whole block. Undo can restore it.";
            if(text=="Edit step…")return "Open guided settings. You can also double-click a row.";
            if(text=="✓ Check macro")return "Check macro structure, included files and reference images. Jump straight to a step that needs attention.";
            return text.Replace("…","");
        }
        private void UpdateSelectionHints()
        {
            LayoutStepToolbar();var step=Selected;bool actionable=!busy&&step!=null&&(ChosenRows.Length>1||!EditorBlocks.IsBoundary(step.Kind));
            foreach(var button in selectionButtons)button.Enabled=actionable;
            foreach(var button in selectionButtons){if(button.AccessibleName=="▶ From selected"){button.Enabled=actionable&&StepPresentation.BalancedTail(macro.Steps,SelectedIndex);tooltips.SetToolTip(button,button.Enabled?"Run from this row through the end.":"This position leaves an IF or Repeat unfinished. Use Selected block to try a single action, or select its block header.");}if(button.AccessibleName=="↑ Up")button.Enabled=actionable&&CanMove(-1);if(button.AccessibleName=="↓ Down")button.Enabled=actionable&&CanMove(1);}
            int selectedCount=ChosenRows.Length;if(selectedCount>1){properties.SelectedObject=null;foreach(var button in selectionButtons)if(new[]{"Edit step…","Edit settings…","↑ Up","↓ Down","▶ From selected","▶ Selected block"}.Contains(button.AccessibleName))button.Enabled=false;}int count=actionable?EditorBlocks.End(macro.Steps,SelectedIndex)-SelectedIndex+1:0;inspectorTitle.Text=selectedCount>1?selectedCount+" selected steps":step==null?"Step details":"Step "+(SelectedIndex+1)+" of "+macro.Steps.Count+(count>1?" · "+count+"-row block":"");
            foreach(ListViewItem row in list.Items){if(row==null)continue;var nesting=row.Tag as StepRow;if(nesting!=null)nesting.InSelectedBlock=count>1&&row.Index>SelectedIndex&&row.Index<SelectedIndex+count;}list.Invalidate();
            foreach(var button in selectionButtons)if(button.AccessibleName=="Duplicate block"){button.Text=count>1?"Duplicate block":"Duplicate step";tooltips.SetToolTip(button,count>1?"Duplicate all "+count+" rows, including nested blocks.":"Duplicate the selected step.");}
            foreach(var button in selectionButtons){if(button.AccessibleName=="Delete"){button.Text=count>1?"Delete block":"Delete";tooltips.SetToolTip(button,count>1?"Remove all "+count+" rows in this block. Undo restores them.":"Remove this step. Undo restores it.");}if(button.AccessibleName=="Enable / disable"){button.Text=(step!=null&&!step.Enabled?"Enable ":"Disable ")+(count>1?"block":"step");tooltips.SetToolTip(button,count>1?"Switch the whole selected block on or off together.":"Skip this step during playback without removing it.");}}
            if(selectedCount>1)foreach(var button in selectionButtons){if(button.AccessibleName=="Duplicate block")button.Text="Duplicate selection";if(button.AccessibleName=="Delete")button.Text="Delete selection";if(button.AccessibleName=="Enable / disable")button.Text=step!=null&&!step.Enabled?"Enable selection":"Disable selection";}
            if(undoButton!=null)undoButton.Enabled=!busy&&undo.Count>0;if(redoButton!=null)redoButton.Enabled=!busy&&redo.Count>0;
            if(beforeButton!=null){beforeButton.Enabled=afterButton.Enabled=!busy&&step!=null;beforeButton.Tag=insertBefore?"selected":"quiet";afterButton.Tag=insertBefore?"quiet":"selected";beforeButton.Invalidate();afterButton.Invalidate();}
            insertionHint.AutoEllipsis=true;insertionHint.Text=(step==null?"Append at end":(insertBefore?"Insert before step ":"Insert after step ")+(SelectedIndex+1))+"  ·  "+InsertionContext.Describe(macro.Steps,InsertionIndex);
            documentState.Text=dirty?"● Unsaved changes":filename==null?"Not saved yet":"✓ Saved";tooltips.SetToolTip(documentState,filename??"Use Save to keep this workflow and its embedded images.");
            if(!busy&&playButton!=null)playButton.Enabled=macro.Steps.Count>0;foreach(var entry in commandMenus)entry.Value.Enabled=CommandAvailable(entry.Key);

        }
        private string KeyName(AppCommand command){return preferences.ActionShortcuts.First(s=>s.Command==command).ToString();}
        private string ShortcutText(string text){return System.Text.RegularExpressions.Regex.Replace(text,@"\bF(8|9|10)\b",delegate(System.Text.RegularExpressions.Match match){return KeyName(match.Value=="F8"?AppCommand.Record:match.Value=="F9"?AppCommand.PlayPause:AppCommand.Stop);});}
        private void Guard(Action action){try{action();}catch(Exception error){ShowError(error.GetBaseException().Message);}}
        private void MenuCommand(ToolStripMenuItem parent,AppCommand command){var item=new ToolStripMenuItem(Shortcuts.Name(command));item.Click+=async delegate{await Dispatch(command);};parent.DropDownItems.Add(item);commandMenus[command]=item;}
        private async Task Dispatch(AppCommand command)
        {
            try{
                if((OwnedForms.Length>0||InAppDialog.IsOpen(this)||draggingSteps)&&command!=AppCommand.Stop)return;
                if(busy&&command!=AppCommand.Stop&&command!=AppCommand.Record&&command!=AppCommand.PlayPause&&command!=AppCommand.Pause)return;
                switch(command){case AppCommand.Record:ToggleRecord();break;case AppCommand.PlayPause:await Play(false);break;case AppCommand.Stop:Stop();break;case AppCommand.Pause:TogglePause();break;case AppCommand.New:NewDocument();break;case AppCommand.Open:OpenDocument();break;case AppCommand.Save:Save(false);break;case AppCommand.SaveAs:Save(true);break;case AppCommand.Undo:History(false);break;case AppCommand.Redo:History(true);break;case AppCommand.EditStep:EditSelected();break;case AppCommand.Delete:Delete();break;case AppCommand.Duplicate:Duplicate();break;case AppCommand.MoveUp:Move(-1);break;case AppCommand.MoveDown:Move(1);break;case AppCommand.PlayFrom:await PlayRange(false);break;case AppCommand.PlayBlock:await PlayRange(true);break;case AppCommand.ToggleTheme:ToggleTheme();break;case AppCommand.FindAction:actionSearch.Focus();actionSearch.SelectAll();break;case AppCommand.FindStep:OpenStepFinder();break;case AppCommand.NextMatch:FindStep(1);break;case AppCommand.PreviousMatch:FindStep(-1);break;}
            }catch(Exception error){ShowError(error.GetBaseException().Message);}
        }
        private void RefreshCommandHints()
        {
            Shortcuts.Active=preferences.ActionShortcuts;if(string.IsNullOrWhiteSpace(actionSearch.Text))searchSummary.Text=SearchShortcutHint();
            RefreshTransport();stopButton.Text="■ Stop  "+KeyName(AppCommand.Stop);
            foreach(var pair in commandMenus)pair.Value.ShortcutKeyDisplayString=KeyName(pair.Key);
            empty.Text="1   Record a task, or choose an action above\n\n2   Double-click a step to adjust its settings\n\n3   Review the macro, then press Play\n\n"+KeyName(AppCommand.Stop)+" stops recording and playback.\nCustomize shortcuts in Settings → Keyboard shortcuts.";
            tooltips.SetToolTip(forever,"Repeat the whole macro until Stop ("+KeyName(AppCommand.Stop)+").");
            RefreshIdleStatus();
        }
        internal void SelectHeadlessStep(int index){if(!headless)throw new InvalidOperationException("Hidden test mode required.");RefreshList(index);}
        internal void SelectHeadlessPreview(){RefreshList(macro.Steps.Count>0?0:-1);}
        private void ApplyTheme()
        {
            UiTheme.Dark=preferences.DarkMode;UiTheme.Style=preferences.AccentStyle;recordButton.Tag="danger";stopButton.Tag="danger";playButton.Tag="primary";status.Tag=empty.Tag=inspectorHelp.Tag="muted";stepGuide.Tag="muted";UiTheme.Apply(this);if(navigation!=null)UiTheme.Apply(navigation);RecolorIcons();if(darkMenu!=null)darkMenu.Checked=preferences.DarkMode;foreach(var item in accentMenus)item.Value.Checked=item.Key==preferences.AccentStyle;if(themeButton!=null){themeButton.Text="";if(themeButton.Image!=null)themeButton.Image.Dispose();themeButton.Image=ColorModeIcon(preferences.DarkMode);themeButton.AccessibleName=preferences.DarkMode?"Switch to light mode":"Switch to dark mode";tooltips.SetToolTip(themeButton,themeButton.AccessibleName);}undoButton.Tag="primary";redoButton.Tag="selected";UiTheme.Apply(undoButton);UiTheme.Apply(redoButton);
            foreach(var item in list.Items.Cast<ListViewItem>()){item.BackColor=UiTheme.RowColor(item.Tag as StepRow);item.ForeColor=item.Index<macro.Steps.Count&&macro.Steps[item.Index].Enabled?UiTheme.Ink:UiTheme.Muted;}
        }
        private void RebuildListIcons(){actionImages.Images.Clear();foreach(var old in ownedIcons)old.Dispose();ownedIcons.Clear();foreach(ActionKind kind in Enum.GetValues(typeof(ActionKind))){var icon=new Bitmap(22,28);using(var art=ActionArt.Icon(kind,22))using(var graphics=Graphics.FromImage(icon))graphics.DrawImageUnscaled(art,0,3);ownedIcons.Add(icon);actionImages.Images.Add(kind.ToString(),icon);}}
        private void RecolorIcons(){RebuildListIcons();RecolorActionButtons(this);SelectStep();}
        private void RecolorActionButtons(Control parent){foreach(Control child in parent.Controls){var button=child as Button;if(button!=null&&button.Image!=null&&button.AccessibleName!=null&&button.AccessibleName.StartsWith("Add ")){var kind=button.AccessibleName=="Add Keyboard shortcut"?ActionKind.KeyPress:Enum.GetValues(typeof(ActionKind)).Cast<ActionKind>().FirstOrDefault(k=>"Add "+ActionArt.Name(k)==button.AccessibleName);var old=button.Image;button.Image=button.AccessibleName=="Add Keyboard shortcut"?IconArt.ControlImage("shortcut",22,UiTheme.Accent):ActionArt.Icon(kind,22);if(old!=null)old.Dispose();}RecolorActionButtons(child);}}
        private void SetAccent(string style){string previous=preferences.AccentStyle;preferences.AccentStyle=style;ApplyTheme();if(!headless)try{preferences.Save();}catch(Exception error){preferences.AccentStyle=previous;ApplyTheme();ShowError(error.Message);}}
        private void ToggleTheme(){bool previous=preferences.DarkMode;preferences.DarkMode=!previous;ApplyTheme();if(!headless)try{preferences.Save();}catch(Exception error){preferences.DarkMode=previous;ApplyTheme();ShowError("Could not save appearance: "+error.Message);}}
        private void ActionBindings()
        {
            if(busy)return;
            if(hotkeys!=null)hotkeys.Dispose();foreach(int id in registered.ToArray()){Native.UnregisterHotKey(Handle,id);registered.Remove(id);}
            try{using(var form=new ShortcutForm(preferences.ActionShortcuts,preferences.HotkeyFiles,delegate(Shortcut[] values){
                var previous=preferences.ActionShortcuts;string error;
                if(hotkeys!=null&&!hotkeys.TryApply(values,out error))return error;
                preferences.ActionShortcuts=values;
                try{if(!headless)preferences.Save();}catch(Exception failure){preferences.ActionShortcuts=previous;string restoreError;if(hotkeys!=null&&!hotkeys.TryApply(previous,out restoreError))return failure.Message+" "+restoreError;return "Could not save shortcuts: "+failure.Message;}
                RefreshCommandHints();ConfigureRecorderShortcuts();return null;
            }))InAppDialog.Show(this,form);}finally{if(hotkeys!=null){string restoreError;if(!hotkeys.TryApply(preferences.ActionShortcuts,out restoreError))SetStatus(restoreError);}if(!headless)RegisterBindings();}
        }
        private void ConfigureRecorderShortcuts(){recorder.IsControlShortcut=delegate(int key,Keys modifiers){if(Shortcuts.RecordingControl(preferences.ActionShortcuts,key,modifiers))return true;return modifiers==(Keys.Control|Keys.Alt)&&key>=49&&key<=57&&preferences.HotkeyFiles!=null&&key-49<preferences.HotkeyFiles.Length&&!string.IsNullOrWhiteSpace(preferences.HotkeyFiles[key-49]);};}
        private void InstallStepMenu()
        {
            var menu=new ContextMenuStrip();menu.Items.Add("Copy steps   Ctrl+C",null,delegate{CopySteps(false);});menu.Items.Add("Cut steps   Ctrl+X",null,delegate{CopySteps(true);});menu.Items.Add("Paste steps   Ctrl+V",null,delegate{PasteSteps();});menu.Items.Add("Save selection as workflow…",null,delegate{SaveSelection();});menu.Items.Add(new ToolStripSeparator());foreach(var command in new[]{AppCommand.EditStep,AppCommand.Duplicate,AppCommand.Delete,AppCommand.MoveUp,AppCommand.MoveDown,AppCommand.PlayFrom,AppCommand.PlayBlock}){AppCommand action=command;var item=new ToolStripMenuItem(Shortcuts.Name(command));item.Tag=action;item.Click+=async delegate{await Dispatch(action);};menu.Items.Add(item);}menu.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e){e.Cancel=true;UiTheme.Apply(menu);foreach(ToolStripItem item in menu.Items)if(item.Tag is AppCommand)item.Enabled=CommandAvailable((AppCommand)item.Tag);InWindowMenu.Show(this,list,menu.Items,headless?Point.Empty:list.PointToClient(Cursor.Position));};list.ContextMenuStrip=menu;
            list.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==System.Windows.Forms.MouseButtons.Right){var hit=list.HitTest(e.Location);if(hit.Item!=null&&!hit.Item.Selected){foreach(ListViewItem row in list.Items)row.Selected=false;hit.Item.Selected=true;}}};
            list.Resize+=delegate{if(list.ClientSize.Width>340)list.Columns[2].Width=Math.Max(120,list.Width-SystemInformation.VerticalScrollBarWidth-list.Columns[0].Width-list.Columns[1].Width-4);};
        }
    }
}
