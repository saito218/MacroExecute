using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MacroExecute
{
    public sealed partial class MainForm
    {
        private sealed class StepDrag {public MainForm Owner;public int Start;public string Snapshot;}
        private bool draggingSteps;
        private StepDrag activeDrag;
        private Point dragPoint;
        private readonly System.Windows.Forms.Timer dragScroll=new System.Windows.Forms.Timer{Interval=120};
        private void InstallDragDrop()
        {
            list.AllowDrop=true;list.AccessibleDescription="Drag an action to move it. Drag an IF or Repeat header to move its entire block. Colored guides show nesting.";
            list.ItemDrag+=delegate(object sender,ItemDragEventArgs e){if(busy||headless||e.Button!=MouseButtons.Left)return;if(ChosenRows.Length>1){SetStatus("Use Cut/Paste to move multiple selected sections together.");return;}int start=((ListViewItem)e.Item).Index;if(EditorBlocks.IsBoundary(macro.Steps[start].Kind)){SetStatus("Drag the IF or Repeat header to move its whole block.");return;}
                activeDrag=new StepDrag{Owner=this,Start=start,Snapshot=Storage.Encode(macro)};var data=new DataObject();data.SetData(typeof(StepDrag),activeDrag);draggingSteps=true;
                try{list.DoDragDrop(data,DragDropEffects.Move);}finally{draggingSteps=false;activeDrag=null;ClearDropCue();if(status.Text.StartsWith("Drop "))SetStatus("Move cancelled. The macro is unchanged.");}};
            list.DragEnter+=DragStepsOver;list.DragOver+=DragStepsOver;list.DragLeave+=delegate{ClearDropCue();};list.QueryContinueDrag+=delegate(object sender,QueryContinueDragEventArgs e){if(e.EscapePressed){e.Action=DragAction.Cancel;ClearDropCue();SetStatus("Move cancelled. The macro is unchanged.");}};
            list.DragDrop+=delegate(object sender,DragEventArgs e){var token=ReadDrag(e.Data);int gap=list.GapAt(list.PointToClient(new Point(e.X,e.Y)));ClearDropCue();e.Effect=DragDropEffects.None;if(token==null||busy||token.Snapshot!=Storage.Encode(macro)){SetStatus("The macro changed; start the move again.");return;}if(CommitDrop(token.Start,gap))e.Effect=DragDropEffects.Move;};
            dragScroll.Tick+=delegate{ScrollDrag();};
        }
        private void ScrollDrag()
        {
            if(activeDrag==null||busy){ClearDropCue();return;}if(list.Items.Count==0)return;var top=list.TopItem;if(top==null)return;
            if(dragPoint.Y<45&&top.Index>0)list.EnsureVisible(top.Index-1);else if(dragPoint.Y>list.ClientSize.Height-30){var bottom=list.GetItemAt(5,Math.Max(0,list.ClientSize.Height-20));if(bottom!=null&&bottom.Index+1<list.Items.Count)list.EnsureVisible(bottom.Index+1);}UpdateDropCue(activeDrag,dragPoint);
        }

        private StepDrag ReadDrag(IDataObject data){if(data==null||!data.GetDataPresent(typeof(StepDrag)))return null;var token=data.GetData(typeof(StepDrag)) as StepDrag;return token!=null&&ReferenceEquals(token.Owner,this)&&ReferenceEquals(token,activeDrag)?token:null;}
        private void DragStepsOver(object sender,DragEventArgs e)
        {
            e.Effect=DragDropEffects.None;var token=ReadDrag(e.Data);if(token==null||busy){ClearDropCue();return;}dragPoint=list.PointToClient(new Point(e.X,e.Y));if(UpdateDropCue(token,dragPoint))e.Effect=DragDropEffects.Move;if(!headless)dragScroll.Start();
        }
        private bool UpdateDropCue(StepDrag token,Point point)
        {
            int gap=list.GapAt(point),selected;List<Step> candidate;string reason;if(!BlockMove.Propose(macro.Steps,token.Start,gap,out candidate,out selected,out reason)){list.DropGap=-1;SetStatus(reason);return false;}
            list.DropGap=gap;SetStatus("Drop "+(EditorBlocks.End(macro.Steps,token.Start)-token.Start+1)+" step(s) "+(gap==macro.Steps.Count?"at the end":"before step "+(gap+1))+"  ·  "+InsertionContext.Describe(macro.Steps,gap)+"  ·  Esc cancels");return true;
        }
        private void ClearDropCue(){dragScroll.Stop();list.DropGap=-1;}
        private bool CommitDrop(int start,int gap)
        {
            if(busy)return false;List<Step> candidate;int selected;string reason;if(!BlockMove.Propose(macro.Steps,start,gap,out candidate,out selected,out reason)){SetStatus(reason);return false;}
            try{Plan.Build(new Macro{Name=macro.Name,Steps=candidate},DocumentDirectory);}catch(Exception error){SetStatus("Move not applied: "+error.GetBaseException().Message);return false;}
            Snapshot();macro.Steps=candidate;MarkDirty();RefreshList(selected);SetStatus("Moved to step "+(selected+1)+"  ·  "+InsertionContext.Describe(macro.Steps,selected)+"  ·  Undo is available.");return true;
        }
        internal IDataObject BeginDragHeadless(int start){if(!headless)throw new InvalidOperationException();activeDrag=new StepDrag{Owner=this,Start=start,Snapshot=Storage.Encode(macro)};draggingSteps=true;var data=new DataObject();data.SetData(typeof(StepDrag),activeDrag);return data;}
        internal void EndDragHeadless(){if(!headless)throw new InvalidOperationException();draggingSteps=false;activeDrag=null;ClearDropCue();}
        internal void ScrollHeadless(Point point){if(!headless)throw new InvalidOperationException();dragPoint=point;ScrollDrag();}
        internal bool DropHeadless(int start,int gap){if(!headless)throw new InvalidOperationException();return CommitDrop(start,gap);}
        internal void CategoryHeadless(int index){if(!headless)throw new InvalidOperationException();actionTabs.SelectedIndex=index;}
    }
}
