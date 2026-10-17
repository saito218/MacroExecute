using System;
using System.IO;
using System.Windows.Forms;

namespace MacroExecute
{
    internal static class WorkflowFiles
    {
        public const string Extension=".mexec";
        public const string OpenFilter="MacroExecute workflows|*.mexec;*.mmacro.json|Legacy JSON workflows|*.json|All files|*.*";
        public static string DisplayName(string path)
        {
            string name=Path.GetFileName(path);
            foreach(string ending in new[]{".mmacro.json",Extension,".json"})if(name.EndsWith(ending,StringComparison.OrdinalIgnoreCase))return name.Substring(0,name.Length-ending.Length);
            return name;
        }
        public static OpenFileDialog OpenDialog() { return new OpenFileDialog{Filter=OpenFilter,DefaultExt="mexec",CheckFileExists=true,Multiselect=false}; }
        public static SaveFileDialog SaveDialog(string current) { return new SaveFileDialog{Filter="MacroExecute workflow (*.mexec)|*.mexec",DefaultExt="mexec",AddExtension=true,OverwritePrompt=true,FileName=string.IsNullOrEmpty(current)?"My workflow.mexec":DisplayName(current)+Extension}; }
    }
}
