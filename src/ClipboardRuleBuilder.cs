using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace MacroExecute
{
    internal sealed partial class ClipboardRuleBuilder : UserControl
    {
        private sealed class Entry { public string Join;public bool AllowReuse;public TextBox Input;public EventHandler Changed; }
        private readonly List<Entry> entries=new List<Entry>();
        private readonly TableLayoutPanel layout=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=false,ColumnCount=1,Margin=Padding.Empty,Tag="canvas"};
        private ContextMenuStrip addMenu;private int insertionIndex;
        public event Action LayoutChanged;
        public int ContentHeight {get{return entries.Count*94+54+entries.Skip(1).Count(entry=>entry.Join=="GROUP OR")*10+reuseOptions.Sum(option=>(int)option.Style.Height);}}
        public string Expression {get{return string.Join("\n",entries.Select((entry,i)=>(i==0?"":entry.Join+"\n")+entry.Input.Text.Trim()));}}
        public ClipboardRuleBuilder(string expression,int[] reuseGroups=null,bool ignoreCase=false)
        {
            Tag="canvas";AccessibleName="Clipboard rule builder";layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(layout);string join="";
            foreach(string raw in expression.Replace("\r\n","\n").Replace('\r','\n').Split('\n')){string text=raw.Trim();if(text=="AND"||text=="OR"||text=="GROUP OR"){join=text;continue;}if(text.Length==0)continue;if(join=="OR"&&entries.Count>0)entries[entries.Count-1].Input.Text+=" "+text;else entries.Add(MakeEntry(join,text));join="OR";}
            if(entries.Count==0)entries.Add(MakeEntry("",""));ignoreLetterCase=ignoreCase;int g=-1;for(int i=0;i<entries.Count;i++)if(i==0||entries[i].Join=="GROUP OR"){g++;entries[i].AllowReuse=reuseGroups!=null&&reuseGroups.Contains(g);}Rebuild();
        }
        private Entry MakeEntry(string join,string text){return new Entry{Join=join,Input=new TextBox{Text=text,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,AccessibleName="Clipboard string row",Margin=Padding.Empty}};}
        internal void Insert(int after,string connector)
        {
            if(entries.Count>=128)return;int index=Math.Min(entries.Count,after+1);entries.Insert(index,MakeEntry(index==0?"":connector,""));Rebuild();entries[index].Input.Focus();
        }
        internal void Remove(int index)
        {
            var old=entries[index];if(index+1<entries.Count&&(index==0||old.Join=="GROUP OR")&&entries[index+1].Join!="GROUP OR")entries[index+1].AllowReuse=old.AllowReuse;if(index+1<entries.Count&&(entries[index+1].Join=="OR"||old.Join=="GROUP OR"&&entries[index+1].Join!="GROUP OR"))entries[index+1].Join=old.Join;entries.RemoveAt(index);old.Input.Parent.Controls.Remove(old.Input);old.Input.Dispose();if(entries.Count==0)entries.Add(MakeEntry("",""));entries[0].Join="";Rebuild();entries[Math.Min(index,entries.Count-1)].Input.Focus();
        }
        internal ContextMenuStrip GetAddMenu()
        {
            if(addMenu==null){addMenu=new ContextMenuStrip();addMenu.Items.Add("Required string row · AND",null,delegate{Insert(insertionIndex,"AND");});addMenu.Items.Add("Alternative group · GROUP OR",null,delegate{Insert(insertionIndex,"GROUP OR");});}return addMenu;
        }
        private void Menu(Button button,int index){insertionIndex=index;var menu=GetAddMenu();UiTheme.Apply(menu);menu.Show(button,new Point(0,button.Height));}
        public override Size GetPreferredSize(Size proposedSize){return new Size(300,ContentHeight);}
        protected override void Dispose(bool disposing){if(disposing&&addMenu!=null)addMenu.Dispose();base.Dispose(disposing);}
        private void Rebuild()
        {
            SuspendLayout();reuseOptions.Clear();foreach(var entry in entries)if(entry.Input.Parent!=null)entry.Input.Parent.Controls.Remove(entry.Input);foreach(Control c in layout.Controls.Cast<Control>().ToArray())c.Dispose();layout.Controls.Clear();layout.RowStyles.Clear();layout.RowCount=0;
            int group=1;
            for(int i=0;i<entries.Count;i++){
                var entry=entries[i];int index=i;if(entry.Join=="GROUP OR")group++;
                var card=new RoundedLayout{Emphasized=i==0||entry.Join=="GROUP OR",Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,Margin=new Padding(0,entry.Join=="GROUP OR"?13:3,0,3),Padding=new Padding(12,3,10,3),AccessibleName="Clipboard rule row "+(i+1)};card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,34));card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,34));card.RowStyles.Add(new RowStyle(SizeType.Absolute,23));card.RowStyles.Add(new RowStyle(SizeType.Absolute,29));card.RowStyles.Add(new RowStyle(SizeType.Percent,100));
                var heading=new Label{Text=i==0?"GROUP 1 · match each AND row":entry.Join=="GROUP OR"?"GROUP OR · GROUP "+group:entry.Join=="AND"?"AND · also match this row":"OR · saved alternatives",Tag=entry.Join=="GROUP OR"||i==0?"heading":"muted",Dock=DockStyle.Fill,AutoEllipsis=true,AccessibleName="Clipboard row relationship"};card.Controls.Add(heading,0,0);card.SetColumnSpan(heading,3);
                var frame=new FieldFrame{Dock=DockStyle.Fill,Margin=new Padding(0,0,4,0)};frame.Controls.Add(entry.Input);card.Controls.Add(frame,0,1);
                var add=new ThemeButton{Text="+",Dock=DockStyle.Fill,Tag="quiet",Margin=Padding.Empty,AccessibleName="Add after clipboard row "+(i+1)};add.Click+=delegate{Menu(add,index);};var remove=new ThemeButton{Text="×",Dock=DockStyle.Fill,Tag="quiet",Margin=Padding.Empty,AccessibleName="Remove clipboard row "+(i+1)};remove.Click+=delegate{Remove(index);};card.Controls.Add(add,1,1);card.Controls.Add(remove,2,1);
                var status=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,Tag="muted",AccessibleName="Clipboard row hint"};card.Controls.Add(status,0,2);card.SetColumnSpan(status,3);Action explain=delegate{if(entry.Input.Text.Trim().Length==0){status.Text="Enter a word or a quoted phrase, e.g. ring \"Monthly Report\"";status.Tag="muted";}else try{var terms=ClipboardRules.Terms(entry.Input.Text);status.Text=terms.Length==1?"1 string · must occur in the clipboard":terms.Length+" alternatives · any one may match";status.Tag="muted";}catch(Exception error){status.Text=error.Message;status.Tag="danger";}UiTheme.Apply(status);};if(entry.Changed!=null)entry.Input.TextChanged-=entry.Changed;entry.Changed=delegate{explain();UpdateReuseOptions();};entry.Input.TextChanged+=entry.Changed;explain();
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute,entry.Join=="GROUP OR"?104:94));layout.Controls.Add(card,0,layout.RowCount++);if(i+1==entries.Count||entries[i+1].Join=="GROUP OR")AddReuseOption(group-1,i);
            }
            var tools=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true,Margin=Padding.Empty,Tag="canvas"};var plus=new ThemeButton{Text="+ Add row ▾",AutoSize=true,Height=32,Tag="primary",AccessibleName="Add clipboard rule"};plus.Click+=delegate{Menu(plus,entries.Count-1);};tools.Controls.Add(plus);layout.RowStyles.Add(new RowStyle(SizeType.Absolute,54));layout.Controls.Add(tools,0,layout.RowCount++);UpdateReuseOptions(false);Height=ContentHeight;UiTheme.Apply(this);ResumeLayout(true);if(LayoutChanged!=null)LayoutChanged();
        }
    }
}
