using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace MacroExecute
{
    internal static class SequenceSelection
    {
        internal static int[] Expand(IList<Step> steps,IEnumerable<int> indices)
        {
            var result=new SortedSet<int>();foreach(int index in indices){if(index<0||index>=steps.Count)throw new InvalidDataException("Selection is outside the workflow.");for(int i=index;i<=EditorBlocks.End(steps,index);i++)result.Add(i);}return result.ToArray();
        }
        internal static Macro Extract(Macro source,IEnumerable<int> indices,string directory)
        {
            int[] chosen=Expand(source.Steps,indices);if(chosen.Length==0)throw new InvalidDataException("Select one or more steps first.");
            var result=new Macro{Name="Selected steps",RandomSeed=source.RandomSeed,Steps=chosen.Select(i=>source.Steps[i].Clone()).ToList()};
            try{Plan.Build(result,directory);}catch(Exception error){throw new InvalidDataException("Select complete IF/repeat blocks and include any referenced labels. "+error.GetBaseException().Message);}
            foreach(var step in result.Steps){if(ImageTemplate.IsImage(step.Kind)){if(string.IsNullOrEmpty(step.ImageData)){step.Target=Plan.Resolve(directory,step.Target);ImageTemplate.Embed(step);}ImageTemplate.ValidateReferences(step);}else if(new[]{ActionKind.Include,ActionKind.CopyFile,ActionKind.DeleteFile,ActionKind.IfFile}.Contains(step.Kind)&&!string.IsNullOrWhiteSpace(step.Target))step.Target=Plan.Resolve(directory,step.Target);if(step.Kind==ActionKind.CopyFile&&!string.IsNullOrWhiteSpace(step.Text))step.Text=Plan.Resolve(directory,step.Text);if(step.Kind==ActionKind.Launch&&!string.IsNullOrWhiteSpace(step.Target)&&!Path.IsPathRooted(step.Target)&&(step.Target.IndexOfAny(new[]{'\\','/'})>=0||File.Exists(Path.Combine(directory,step.Target))))step.Target=Plan.Resolve(directory,step.Target);}
            return result;
        }
        internal static List<Step> Insert(Macro source,Macro payload,int at,string directory)
        {
            var added=payload.Steps.Select(s=>s.Clone()).ToList();var names=new HashSet<string>(source.Steps.Where(s=>s.Kind==ActionKind.Label).Select(s=>s.Text));var renamed=new Dictionary<string,string>();
            foreach(var label in added.Where(s=>s.Kind==ActionKind.Label)){string original=label.Text,name=original;int suffix=2;while(names.Contains(name))name=original+"_copy"+(suffix++);names.Add(name);if(name!=original)renamed[original]=name;label.Text=name;}
            foreach(var jump in added.Where(s=>s.Kind==ActionKind.Jump))if(renamed.ContainsKey(jump.Text))jump.Text=renamed[jump.Text];
            var candidate=source.Steps.Select(s=>s.Clone()).ToList();candidate.InsertRange(at,added);Plan.Build(new Macro{Steps=candidate},directory);return candidate;
        }
    }
    public sealed partial class MainForm
    {
        private static System.Drawing.Bitmap ColorModeIcon(bool dark)
        {
            return IconArt.ControlImage(dark?"sun":"moon",22,UiTheme.Ink);
        }
        private const string SequenceClipboard="MacroExecute.WorkflowSteps.v1";
        private bool selectionUpdating;
        private void SelectAllRows(){selectionUpdating=true;list.BeginUpdate();try{foreach(ListViewItem item in list.Items)item.Selected=true;}finally{list.EndUpdate();selectionUpdating=false;SelectStep();}}
        private int[] ChosenRows {get{return list.SelectedIndices.Cast<int>().OrderBy(i=>i).ToArray();}}
        private Macro SelectedWorkflow(){return SequenceSelection.Extract(macro,ChosenRows,DocumentDirectory);}
        private void CopySteps(bool cut)
        {
            if(busy)return;Guard(delegate{var selected=SelectedWorkflow();int[] indices=SequenceSelection.Expand(macro.Steps,ChosenRows);List<Step> remaining=null;if(cut){remaining=macro.Steps.Where((s,i)=>!indices.Contains(i)).ToList();Plan.Build(new Macro{Steps=remaining},DocumentDirectory);}var data=new DataObject();string text=Storage.Encode(selected);data.SetData(SequenceClipboard,text);data.SetText(text,TextDataFormat.UnicodeText);Clipboard.SetDataObject(data,true);if(cut){Snapshot();macro.Steps=remaining;MarkDirty();RefreshList(Math.Min(indices[0],macro.Steps.Count-1));}SetStatus((cut?"Cut ":"Copied ")+indices.Length+" steps. Ctrl+V inserts them at the insertion marker.");});
        }
        private void PasteSteps(){if(busy)return;Guard(delegate{string payload=Clipboard.ContainsData(SequenceClipboard)?Clipboard.GetData(SequenceClipboard) as string:Clipboard.ContainsText()?Clipboard.GetText():null;if(string.IsNullOrWhiteSpace(payload))throw new InvalidDataException("Copy workflow steps first.");PasteStepsPayload(payload);});}
        private void PasteStepsPayload(string payload){var selected=Storage.Decode(payload);if(selected.Steps.Count==0)return;if(!EnabledInsertion())foreach(var step in selected.Steps)step.Enabled=false;int at=InsertionIndex;var candidate=SequenceSelection.Insert(macro,selected,at,DocumentDirectory);Snapshot();macro.Steps=candidate;MarkDirty();RefreshList(at);foreach(int index in Enumerable.Range(at,selected.Steps.Count))list.Items[index].Selected=true;SetStatus("Inserted "+selected.Steps.Count+" steps. Undo restores the previous workflow.");}
        private void DeleteChosen(){Guard(delegate{int[] indices=SequenceSelection.Expand(macro.Steps,ChosenRows);var remaining=macro.Steps.Where((s,i)=>!indices.Contains(i)).ToList();Plan.Build(new Macro{Steps=remaining},DocumentDirectory);Snapshot();macro.Steps=remaining;MarkDirty();RefreshList(Math.Min(indices[0],remaining.Count-1));});}
        private void SaveSelection(){if(busy)return;Guard(delegate{var selected=SelectedWorkflow();using(var dialog=WorkflowFiles.SaveDialog(null)){dialog.FileName="Selected steps.mexec";if(dialog.ShowDialog(this)!=DialogResult.OK)return;selected.Name=WorkflowFiles.DisplayName(dialog.FileName);Storage.Save(dialog.FileName,selected);SetStatus("Saved "+selected.Steps.Count+" steps as a separate workflow. The current workflow is unchanged.");}});}
        private bool SelectionKey(Keys key){if(!list.ContainsFocus||InAppDialog.IsOpen(this))return false;if(key==(Keys.Control|Keys.A)){SelectAllRows();return true;}if(key==(Keys.Control|Keys.C)){CopySteps(false);return true;}if(key==(Keys.Control|Keys.X)){CopySteps(true);return true;}if(key==(Keys.Control|Keys.V)){PasteSteps();return true;}return false;}
        internal void SelectRowsHeadless(params int[] rows){if(!headless)throw new InvalidOperationException();foreach(ListViewItem item in list.Items)item.Selected=rows.Contains(item.Index);SelectStep();}
        internal string CopyRowsHeadless(){if(!headless)throw new InvalidOperationException();return Storage.Encode(SelectedWorkflow());}
        internal void PasteRowsHeadless(string payload){if(!headless)throw new InvalidOperationException();PasteStepsPayload(payload);}
    }
}
