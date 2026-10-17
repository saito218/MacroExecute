using System;
using System.Collections.Generic;
using System.Linq;

namespace MacroExecute
{
    internal static class BlockMove
    {
        // Destination is a gap in the ORIGINAL list: 0 before the first row, Count after the last.
        public static bool Propose(IList<Step> source,int start,int gap,out List<Step> result,out int selected,out string reason)
        {
            result=null;selected=-1;reason="";
            if(start<0||start>=source.Count||gap<0||gap>source.Count){reason="Choose a row and a position inside this macro.";return false;}
            if(EditorBlocks.IsBoundary(source[start].Kind)){reason="Drag the IF or Repeat header to move its whole block.";return false;}
            int end;try{end=EditorBlocks.End(source,start);}catch(Exception e){reason=e.Message;return false;}
            if(gap>=start&&gap<=end+1){reason="Choose a position outside the steps being moved.";return false;}
            var moved=source.Skip(start).Take(end-start+1).ToList();var remaining=source.Take(start).Concat(source.Skip(end+1)).ToList();int target=gap>end?gap-moved.Count:gap;
            if(moved.Any(s=>s.Enabled)&&DisabledScope(remaining,target)){reason="Enable the destination block before moving active steps into it.";return false;}
            remaining.InsertRange(target,moved);
            if(!Shape(remaining,out reason)||!Shape(remaining.Where(s=>s.Enabled),out reason))return false;
            result=remaining;selected=target;return true;
        }
        private static bool DisabledScope(IList<Step> steps,int gap)
        {
            var stack=new Stack<bool>();int disabled=0;for(int i=0;i<gap;i++){var step=steps[i];if(Plan.IsIf(step.Kind)||step.Kind==ActionKind.Repeat){stack.Push(step.Enabled);if(!step.Enabled)disabled++;}else if((step.Kind==ActionKind.EndIf||step.Kind==ActionKind.EndRepeat)&&stack.Count>0){if(!stack.Pop())disabled--;}}return disabled>0;
        }
        private static bool Shape(IEnumerable<Step> steps,out string reason)
        {
            var stack=new List<ActionKind>();var alternatives=new List<bool>();reason="";
            foreach(var step in steps){var k=step.Kind;if(Plan.IsIf(k)||k==ActionKind.Repeat){stack.Add(k);alternatives.Add(false);}else if(k==ActionKind.Else){if(stack.Count==0||!Plan.IsIf(stack.Last())||alternatives.Last()){reason="This would detach ELSE from its IF.";return false;}alternatives[alternatives.Count-1]=true;}else if(k==ActionKind.EndIf||k==ActionKind.EndRepeat){if(stack.Count==0||(k==ActionKind.EndIf)!=Plan.IsIf(stack.Last())){reason="This would split an IF or Repeat block.";return false;}stack.RemoveAt(stack.Count-1);alternatives.RemoveAt(alternatives.Count-1);}else if(k==ActionKind.Break&&!stack.Contains(ActionKind.Repeat)){reason="BREAK must stay inside a Repeat block.";return false;}}
            if(stack.Count>0){reason="This would leave a block unclosed.";return false;}return true;
        }
    }
    internal sealed class StepRow
    {
        public int Depth;public ActionKind Kind;public bool InSelectedBlock;
        public static StepRow[] Describe(IList<Step> steps){int depth=0;var rows=new List<StepRow>();foreach(var step in steps){if(EditorBlocks.IsBoundary(step.Kind))depth=Math.Max(0,depth-1);rows.Add(new StepRow{Depth=depth,Kind=step.Kind});if(Plan.IsIf(step.Kind)||step.Kind==ActionKind.Repeat||step.Kind==ActionKind.Else)depth++;}return rows.ToArray();}
    }
}
