using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    internal sealed class ReviewItem {public int Step=-1;public bool Error;public string Message;}
    internal static class MacroReview
    {
        public static List<ReviewItem> Check(Macro macro,string directory)
        {
            var result=new List<ReviewItem>();if(macro.Steps.Count==0)result.Add(new ReviewItem{Message="No actions yet. Record a task or add an action from the library."});
            for(int i=0;i<macro.Steps.Count;i++){var step=macro.Steps[i];if(!step.Enabled)continue;
                if(ImageTemplate.IsImage(step.Kind))try{var copy=step.Clone();if(string.IsNullOrWhiteSpace(copy.ImageData)&&string.IsNullOrWhiteSpace(copy.Target))throw new InvalidDataException("Capture or import a reference image in this step.");if(!string.IsNullOrWhiteSpace(copy.Target)&&!Path.IsPathRooted(copy.Target))copy.Target=Plan.Resolve(directory,copy.Target);ImageTemplate.ValidateReferences(copy); }catch(Exception error){result.Add(new ReviewItem{Step=i,Error=true,Message="Reference image: "+error.GetBaseException().Message});}
                if(step.Kind==ActionKind.Include)try{if(!File.Exists(Plan.Resolve(directory,step.Target)))result.Add(new ReviewItem{Step=i,Error=true,Message="Included macro file is missing. Choose an existing MacroExecute workflow."});}catch(Exception error){result.Add(new ReviewItem{Step=i,Error=true,Message=error.Message});}
                if(Plan.IsIf(step.Kind)&&i+1<macro.Steps.Count&&(macro.Steps[i+1].Kind==ActionKind.Else||macro.Steps[i+1].Kind==ActionKind.EndIf))result.Add(new ReviewItem{Step=i,Message="The true branch is empty. Insert actions after this IF if you want it to do something."});
            }
            try{Plan.Build(macro,directory);}catch(Exception error){if(!result.Any(r=>r.Error)){string message=error.GetBaseException().Message;var match=System.Text.RegularExpressions.Regex.Match(message,@"at step (\d+)");int row=-1;if(match.Success&&!macro.Steps.Any(s=>s.Kind==ActionKind.Include)){int number;if(int.TryParse(match.Groups[1].Value,out number)&&number>0&&number<=macro.Steps.Count)row=number-1;}result.Add(new ReviewItem{Step=row,Error=true,Message=message});}}
            if(result.Count==0)result.Add(new ReviewItem{Message="No issues found in the macro structure or checked references. No actions were run."});return result;
        }
    }
    internal sealed class MacroReviewForm : ThemeWindow
    {
        public int SelectedStep {get;private set;}
        public MacroReviewForm(IList<ReviewItem> items)
        {
            SelectedStep=-1;Text="Check macro";Size=new Size(780,430);MinimumSize=new Size(620,350);Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterParent;var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=3};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,56));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,44));Controls.Add(root);root.BringToFront();root.Controls.Add(new Label{Text=(items.Any(i=>i.Error)?"Needs attention":"Macro check complete")+"\nSelect an item and choose Go to step. This check does not run the macro or click anything.",Dock=DockStyle.Fill,Tag="muted"},0,0);
            var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,HideSelection=false,MultiSelect=false,BorderStyle=BorderStyle.None};list.Columns.Add("Step",65);list.Columns.Add("Result",80);list.Columns.Add("Details",570);foreach(var issue in items){var row=new ListViewItem(issue.Step<0?"—":(issue.Step+1).ToString()){Tag=issue};row.SubItems.Add(issue.Error?"Fix":"Info");row.SubItems.Add(issue.Message);row.ToolTipText=issue.Message;list.Items.Add(row);}list.ShowItemToolTips=true;root.Controls.Add(list,0,1);
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};var close=new ThemeButton{Text="Close",AutoSize=true,Height=34,DialogResult=DialogResult.Cancel};var jump=new ThemeButton{Text="Go to step",AutoSize=true,Height=34,Tag="primary",Enabled=false};footer.Controls.Add(close);footer.Controls.Add(jump);root.Controls.Add(footer,0,2);CancelButton=close;list.ItemSelectionChanged+=delegate{jump.Enabled=list.SelectedItems.Count>0&&((ReviewItem)list.SelectedItems[0].Tag).Step>=0;};Action go=delegate{if(!jump.Enabled)return;SelectedStep=((ReviewItem)list.SelectedItems[0].Tag).Step;DialogResult=DialogResult.OK;Close();};jump.Click+=delegate{go();};list.DoubleClick+=delegate{go();};UiTheme.Apply(this);
        }
    }
}
