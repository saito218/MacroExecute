using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace MacroExecute
{
    internal sealed partial class ClipboardRuleBuilder
    {
        private sealed class ReuseOption { public int Group,Start,End;public Entry Head;public ThemeCheckBox Check;public Label Detail;public Panel Panel;public RowStyle Style; }
        private readonly List<ReuseOption> reuseOptions=new List<ReuseOption>();
        public event Action ReuseChanged;
        private bool ignoreLetterCase;
        public bool IgnoreCase {get{return ignoreLetterCase;}set{ignoreLetterCase=value;UpdateReuseOptions();}}
        public int[] ReuseGroups {get{return reuseOptions.Where(option=>option.Style.Height>0&&option.Head.AllowReuse).Select(option=>option.Group).ToArray();}}
        internal int AvailableReuseOptions {get{return reuseOptions.Count(option=>option.Style.Height>0);}}
        private void AddReuseOption(int group,int end)
        {
            int start=end;while(start>0&&entries[start].Join!="GROUP OR")start--;
            var option=new ReuseOption{Group=group,Start=start,End=end,Head=entries[start],Panel=new Panel{Dock=DockStyle.Fill,Margin=new Padding(8,0,8,4),Tag="canvas"},Style=new RowStyle(SizeType.Absolute,0)};
            option.Check=new ThemeCheckBox{Text="Allow same string for multiple AND rows",Checked=option.Head.AllowReuse,Dock=DockStyle.Top,Height=30,AccessibleName="Allow shared strings in clipboard group "+(group+1)};
            option.Detail=new Label{Dock=DockStyle.Fill,Tag="muted",AutoEllipsis=true,AccessibleName="Shared strings in clipboard group "+(group+1)};option.Panel.Controls.Add(option.Detail);option.Panel.Controls.Add(option.Check);option.Check.CheckedChanged+=delegate{option.Head.AllowReuse=option.Check.Checked;if(ReuseChanged!=null)ReuseChanged();};layout.RowStyles.Add(option.Style);layout.Controls.Add(option.Panel,0,layout.RowCount++);reuseOptions.Add(option);
        }
        private void UpdateReuseOptions(bool notify=true)
        {
            foreach(var option in reuseOptions){var clauses=new List<string[]>();var terms=new List<string>();bool valid=true;for(int i=option.Start;i<=option.End;i++){if(i>option.Start&&entries[i].Join=="AND"){clauses.Add(terms.ToArray());terms.Clear();}try{terms.AddRange(ClipboardRules.Terms(entries[i].Input.Text));}catch(System.IO.InvalidDataException){valid=false;}}clauses.Add(terms.ToArray());var shared=ClipboardRules.SharedTerms(clauses.ToArray(),IgnoreCase);bool show=shared.Length>0;if(!show&&valid)option.Head.AllowReuse=false;option.Check.Checked=option.Head.AllowReuse;option.Check.Enabled=show;option.Panel.Visible=show;option.Style.Height=show?58:0;option.Detail.Text="Shared: "+string.Join(", ",shared.Select(ClipboardRules.Quote));}
            Height=ContentHeight;layout.PerformLayout();if(notify&&LayoutChanged!=null)LayoutChanged();
        }
    }
}
