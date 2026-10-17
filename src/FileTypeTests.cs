using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    internal static class FileTypeTests
    {
        private static void Check(bool value,string message) { if(!value)throw new Exception(message); }
        private static IEnumerable<Control> All(Control root) { foreach(Control child in root.Controls){yield return child;foreach(var nested in All(child))yield return nested;} }
        public static void Run(Action<string,Action> test,string directory)
        {
            test("Workflow dialogs default to mexec and retain legacy opening",delegate{
                using(var open=WorkflowFiles.OpenDialog())using(var save=WorkflowFiles.SaveDialog(null))using(var legacy=WorkflowFiles.SaveDialog("old.flow.MMACRO.JSON")) {
                    Check(open.Filter.Contains("*.mexec;*.mmacro.json")&&open.Filter.Contains("*.json")&&open.CheckFileExists,"Legacy picker compatibility lost");
                    Check(save.DefaultExt=="mexec"&&save.AddExtension&&save.OverwritePrompt&&save.FileName=="My workflow.mexec","New save defaults incorrect");
                    Check(legacy.FileName=="old.flow.mexec","Save As did not suggest new extension");
                }
            });
            test("Workflow titles strip only complete recognized suffixes case insensitively",delegate{
                foreach(string path in new[]{"flow.mexec","flow.MEXEC","flow.mmacro.json","flow.MMACRO.JSON","flow.json"})Check(WorkflowFiles.DisplayName(path)=="flow","Incorrect title for "+path);
                Check(WorkflowFiles.DisplayName("flow.mexec.backup")=="flow.mexec.backup"&&WorkflowFiles.DisplayName("my.flow.mexec")=="my.flow","Dotted names were altered");
            });
            test("New and legacy files nest and resolve relative paths across folders",delegate{
                string folder=Path.Combine(directory,"mixed-formats");Directory.CreateDirectory(folder);string child=Path.Combine(folder,"child.mmacro.json"),leaf=Path.Combine(folder,"leaf.mexec"),root=Path.Combine(directory,"root.mexec");
                Storage.Save(leaf,new Macro{Steps=new List<Step>{new Step{Kind=ActionKind.Message,Text="leaf 日本語"}}});
                Storage.Save(child,new Macro{Steps=new List<Step>{new Step{Kind=ActionKind.Include,Target="leaf.mexec"}}});
                Storage.Save(root,new Macro{Steps=new List<Step>{new Step{Kind=ActionKind.IfWindow,Text="x"},new Step{Kind=ActionKind.Repeat,Count=2},new Step{Kind=ActionKind.Include,Target=Path.Combine("mixed-formats","child.mmacro.json")},new Step{Kind=ActionKind.EndRepeat},new Step{Kind=ActionKind.EndIf}}});
                var host=new FakeHost();new Runner(host).Run(Plan.Build(Storage.Load(root),directory),1,1,System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                Check(host.Calls.SequenceEqual(new[]{"leaf 日本語","leaf 日本語"})&&host.Released,"Mixed-extension nested includes changed behavior");
            });
            test("Include editor exposes new workflow picker and preserves selected path",delegate{
                using(var editor=new StepEditor(new Step{Kind=ActionKind.Include,Target="child.mexec"},directory)) {
                    var controls=All(editor).ToArray();Check(controls.OfType<Button>().Any(b=>b.AccessibleName=="Browse included workflow"),"No workflow browse action");
                    var path=controls.OfType<TextBox>().Single(t=>t.AccessibleName=="Included workflow path");path.Text="nested.mmacro.json";
                    var save=controls.OfType<Button>().Single(b=>b.Text=="Save step");typeof(Control).GetMethod("OnClick",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(save,new object[]{EventArgs.Empty});
                    Check(editor.Value.Target=="nested.mmacro.json"&&!editor.Visible,"Include path changed or editor displayed");
                }
            });
        }
    }
}
