using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    public sealed partial class MainForm
    {
        private readonly TextBox stepSearch=new TextBox{BorderStyle=BorderStyle.None,AccessibleName="Find in this macro"};
        private readonly Label stepMatches=new Label{AutoSize=true,Tag="muted",Padding=new Padding(3,8,0,0),AccessibleName="Step search results"};
        private readonly FlowLayoutPanel stepFinder=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true,Tag="toolbar",Margin=Padding.Empty,Visible=false};
        private bool finderOpen;
        private readonly Label inspectorTitle=new Label{Text="Step details",Tag="secondary",Dock=DockStyle.Fill,Font=new Font("Segoe UI",10,FontStyle.Bold),AutoEllipsis=true};
        private void BuildStepFinder(TableLayoutPanel timeline)
        {
            timeline.RowStyles[0].Height=54;timeline.RowCount=4;timeline.RowStyles.Insert(1,new RowStyle(SizeType.Absolute,0));
            var body=timeline.GetControlFromPosition(0,1);var tools=timeline.GetControlFromPosition(0,2);timeline.SetRow(tools,3);timeline.SetRow(body,2);
            var heading=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Tag="toolbar",Margin=Padding.Empty};heading.RowStyles.Add(new RowStyle(SizeType.Absolute,24));heading.RowStyles.Add(new RowStyle(SizeType.Percent,100));heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,102));timeline.Controls.Remove(insertionHint);heading.Controls.Add(new Label{Text="Workflow",AccessibleName="Workflow heading",Font=new Font(Font.FontFamily,11,FontStyle.Bold),Dock=DockStyle.Fill},0,0);heading.Controls.Add(insertionHint,0,1);heading.SetColumnSpan(insertionHint,2);var find=AddButton(heading,"Find step…",delegate{OpenStepFinder();},true);heading.SetCellPosition(find,new TableLayoutPanelCellPosition(1,0));find.Dock=DockStyle.Fill;find.Height=26;find.MinimumSize=new Size(30,24);find.Margin=Padding.Empty;timeline.Controls.Add(heading,0,0);
            var frame=new FieldFrame{Width=190,Height=30,Margin=new Padding(0,1,3,1)};stepSearch.Dock=DockStyle.Fill;frame.Controls.Add(stepSearch);stepFinder.Controls.Add(frame);editing.Add(frame);
            var previous=AddButton(stepFinder,"↑",delegate{FindStep(-1);},true);previous.AccessibleName="Previous matching step";var next=AddButton(stepFinder,"↓",delegate{FindStep(1);},true);next.AccessibleName="Next matching step";var close=AddButton(stepFinder,"×",delegate{CloseStepFinder();},true);close.AccessibleName="Close step search";foreach(var button in new[]{previous,next,close}){button.AutoSize=false;button.Width=32;button.Height=28;button.MinimumSize=new Size(32,28);button.Margin=new Padding(2,1,2,1);}stepFinder.Controls.Add(stepMatches);timeline.Controls.Add(stepFinder,0,1);
            stepSearch.TextChanged+=delegate{FindStep(0);};stepSearch.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Escape){CloseStepFinder();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Enter){FindStep(e.Shift?-1:1);e.SuppressKeyPress=true;}};
            tooltips.SetToolTip(stepSearch,"Find existing steps by action, text, image path or settings. Enter: next; Shift+Enter: previous; Escape: close. Rows stay in place.");
        }
        private void OpenStepFinder(){if(busy)return;finderOpen=true;stepFinder.Visible=true;timelinePanel.RowStyles[1].Height=38;stepSearch.Focus();stepSearch.SelectAll();RefreshStepMatches();}
        private void CloseStepFinder(){finderOpen=false;stepFinder.Visible=false;timelinePanel.RowStyles[1].Height=0;stepSearch.Clear();list.Focus();}
        private void FindStep(int direction)
        {
            if(busy)return;if(!finderOpen){OpenStepFinder();return;}var matches=StepPresentation.Find(macro.Steps,stepSearch.Text);if(matches.Length==0){RefreshStepMatches();return;}
            int index=direction==0?(matches.Contains(SelectedIndex)?SelectedIndex:matches[0]):direction>0?matches.FirstOrDefault(i=>i>SelectedIndex):matches.LastOrDefault(i=>i<SelectedIndex);
            if(direction>0&&!matches.Any(i=>i>SelectedIndex))index=matches[0];if(direction<0&&!matches.Any(i=>i<SelectedIndex))index=matches.Last();
            SelectRow(index);RefreshStepMatches();
        }
        private void SelectRow(int index){list.SelectedItems.Clear();if(index>=0&&index<list.Items.Count){list.Items[index].Selected=true;list.Items[index].Focused=true;list.EnsureVisible(index);}SelectStep();}
        private void RefreshStepMatches(){if(!finderOpen)return;var matches=StepPresentation.Find(macro.Steps,stepSearch.Text);int position=Array.IndexOf(matches,SelectedIndex);stepMatches.Text=string.IsNullOrWhiteSpace(stepSearch.Text)?"Type to find a step":matches.Length==0?"No matches":(position>=0?(position+1)+" / ":"")+matches.Length+" matches";}
        private void RefreshTransport()
        {
            if(playButton==null)return;bool paused=recordingSession?recorder.Paused:runner!=null&&runner.Paused;
            recordButton.Text=TransportLabels.Record(recordingSession)+"  "+KeyName(AppCommand.Record);playButton.Text=TransportLabels.Play(busy,recordingSession,paused)+"  "+KeyName(AppCommand.PlayPause);pauseButton.Text=TransportLabels.Pause(paused)+"  "+KeyName(AppCommand.Pause);
        }
        internal void FindHeadless(string query,int direction){if(!headless)throw new InvalidOperationException();OpenStepFinder();if(stepSearch.Text!=query)stepSearch.Text=query;if(direction!=0)FindStep(direction);}
    }
}
