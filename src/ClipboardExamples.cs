using System;
using System.Collections.Generic;
namespace MacroExecute
{
    internal static class ClipboardExamples
    {
        internal const string ItemText=@"Document Type: Status Report
Title: Monthly Report
Team: Paper Lantern
--------
Status: Approved
Processing time: 3,80 seconds
Record Number: 85
(Priority: 3)
Variance: 36(38-33)%
--------
Summary: Warehouse stock review completed.";
        internal static Macro Workflow(bool groups=false)
        {
            return new Macro{Name="Copied item check"+(groups?" · Alternative combinations":" · All strings"),Steps=new List<Step>{
                new Step{Kind=ActionKind.Note,Text="Select report text in a text editor before playback. Mouse move uses the current pointer by default; edit it to pick a fixed position. No window is activated automatically."},
                new Step{Kind=ActionKind.ClipboardClear},new Step{Kind=ActionKind.MouseMove,Pointer=PointerMode.CurrentPointer},new Step{Kind=ActionKind.Delay,DelayMs=250},
                new Step{Kind=ActionKind.CopySelection,ClipboardWaitForChange=true,TimeoutMs=5000},
                new Step{Kind=ActionKind.IfClipboard,ClipboardMatch=groups?ClipboardMatchMode.Groups:ClipboardMatchMode.All,Text=groups?"Document Type: Status Report\nMonthly Report\nStatus: Approved\n\nDocument Type: Status Report\nMonthly Report\n(Priority: 1)":"Document Type: Status Report\nMonthly Report\nStatus: Approved"},
                new Step{Kind=ActionKind.Message,Text="Item matches. Replace this message with your desired action(s)."},new Step{Kind=ActionKind.Else},new Step{Kind=ActionKind.Note,Text="No match: skip the action."},new Step{Kind=ActionKind.EndIf}
            }};
        }
    }
}
