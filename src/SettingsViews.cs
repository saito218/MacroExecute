using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    internal static class DialogLayout
    {
        public static TableLayoutPanel Root(Form form,string title,string description)
        {
            form.Font=new Font("Segoe UI",10);form.StartPosition=FormStartPosition.CenterParent;
            var root=new TableLayoutPanel{Tag="canvas",Dock=DockStyle.Fill,Padding=new Padding(22,12,22,16),ColumnCount=1,RowCount=3};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,88));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,54));form.Controls.Add(root);root.BringToFront();
            var header=new TableLayoutPanel{Tag="canvas",Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};header.RowStyles.Add(new RowStyle(SizeType.Absolute,36));header.RowStyles.Add(new RowStyle(SizeType.Percent,100));header.Controls.Add(new Label{Text=title,Font=new Font(form.Font.FontFamily,18,FontStyle.Bold),Tag="heading",Dock=DockStyle.Fill},0,0);header.Controls.Add(new Label{Text=description,Tag="muted",Dock=DockStyle.Fill},0,1);root.Controls.Add(header,0,0);return root;
        }
        public static Control SearchField(TextBox input,string caption)
        {
            var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=new Padding(0,0,0,10)};row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.RowStyles.Add(new RowStyle(SizeType.Percent,100));row.Controls.Add(new Label{Text=caption,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,UseMnemonic=false},0,0);var frame=new FieldFrame{Dock=DockStyle.Fill,Margin=Padding.Empty};frame.Controls.Add(input);row.Controls.Add(frame,1,0);return row;
        }
        public static FlowLayoutPanel Footer(TableLayoutPanel root) { var footer=new FlowLayoutPanel{Tag="canvas",Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Padding=new Padding(0,10,0,0)};root.Controls.Add(footer,0,2);return footer; }
        public static Button Button(Control parent,string text,Action action,string role="quiet") { var button=new ThemeButton{Text=text,AccessibleName=text,Width=140,Height=36,Tag=role,Margin=new Padding(6,0,0,0)};button.Click+=delegate{action();};parent.Controls.Add(button);return button; }

        public static RoundedLayout Card(Control parent,string title,string description,int height)
        {
            var card=new RoundedLayout{Width=660,Height=height,Margin=new Padding(0,0,0,12),Padding=new Padding(18),ColumnCount=1,RowCount=3};card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));card.RowStyles.Add(new RowStyle(SizeType.Absolute,28));card.RowStyles.Add(new RowStyle(SizeType.Absolute,38));card.RowStyles.Add(new RowStyle(SizeType.Percent,100));card.Controls.Add(new Label{Text=title,Dock=DockStyle.Fill,Font=new Font("Segoe UI",11,FontStyle.Bold)},0,0);card.Controls.Add(new Label{Text=description,Dock=DockStyle.Fill,Tag="muted"},0,1);parent.Controls.Add(card);return card;
        }
        public static FlowLayoutPanel Stack(TableLayoutPanel root)
        {
            var stack=new FlowLayoutPanel{Tag="canvas",Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(0,0,12,0)};root.Controls.Add(stack,0,1);stack.SizeChanged+=delegate{foreach(Control child in stack.Controls)child.Width=Math.Max(200,stack.ClientSize.Width-24);};return stack;
        }
    }
    internal sealed class RandomizationForm : ThemeWindow
    {
        public int? Seed {get;private set;}
        public RandomizationForm(int? seed)
        {
            Text="Workflow randomization";Size=new Size(720,410);MinimumSize=Size;
            var root=DialogLayout.Root(this,"Workflow randomization","A fixed seed makes random repeat counts, delays and pointer offsets repeatable for this workflow.");
            var card=new RoundedLayout{Dock=DockStyle.Fill,Padding=new Padding(18)};var enabled=new ThemeCheckBox{Text="Use a fixed random seed",Checked=seed.HasValue,Dock=DockStyle.Top,Height=40};var number=new NumericUpDown{Minimum=int.MinValue,Maximum=int.MaxValue,Value=seed??0,Dock=DockStyle.Top,Enabled=seed.HasValue,AccessibleName="Random seed"};enabled.CheckedChanged+=delegate{number.Enabled=enabled.Checked;};card.Controls.Add(number);card.Controls.Add(enabled);root.Controls.Add(card,0,1);var footer=DialogLayout.Footer(root);DialogLayout.Button(footer,"Save seed",delegate{Seed=enabled.Checked?(int?)number.Value:null;DialogResult=DialogResult.OK;Close();},"primary");CancelButton=DialogLayout.Button(footer,"Cancel",delegate{DialogResult=DialogResult.Cancel;Close();});UiTheme.Apply(this);
        }
    }
    internal sealed class PreferencesForm : ThemeWindow
    {
        public Preferences Value {get;private set;}
        private readonly ThemeCheckBox dark=new ThemeCheckBox{Text="Dark appearance",AutoSize=true};
        private readonly ThemeCheckBox moves=new ThemeCheckBox{Text="Include pointer movements",AutoSize=true};
        private readonly ThemeCheckBox relative=new ThemeCheckBox{Text="Use coordinates relative to the active window",AutoSize=true};
        private readonly ThemeCheckBox forever=new ThemeCheckBox{Text="Repeat until stopped",AutoSize=true};
        private readonly NumericUpDown runs=new NumericUpDown{Minimum=1,Maximum=1000000,Width=95};
        private readonly NumericUpDown speed=new NumericUpDown{Minimum=0.1m,Maximum=20,DecimalPlaces=1,Increment=0.1m,Width=80};
        public PreferencesForm(Preferences preferences)
        {
            runs.AccessibleName="Default run count";speed.AccessibleName="Default playback speed";Value=Storage.Serializer().Deserialize<Preferences>(Storage.Serializer().Serialize(preferences));Text="Preferences";Size=new Size(820,760);MinimumSize=new Size(720,620);
            var root=DialogLayout.Root(this,"Preferences","Set your appearance and defaults. Changes apply only when you save.");var stack=DialogLayout.Stack(root);
            var appearance=DialogLayout.Card(stack,"Appearance","Six palettes, each with a balanced light and dark appearance.",432);
            var appearanceChoices=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty};appearanceChoices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));appearanceChoices.RowStyles.Add(new RowStyle(SizeType.Absolute,32));appearanceChoices.RowStyles.Add(new RowStyle(SizeType.Absolute,174));appearanceChoices.RowStyles.Add(new RowStyle(SizeType.Percent,100));appearance.Controls.Add(appearanceChoices,0,2);dark.Checked=Value.DarkMode;appearanceChoices.Controls.Add(dark,0,0);
            var paletteChoices=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true,Margin=Padding.Empty,AccessibleName="Theme gallery"};appearanceChoices.Controls.Add(paletteChoices,0,1);var preview=new ThemePreview{Palette=Value.AccentStyle,DarkPreview=dark.Checked,Dock=DockStyle.Fill,Margin=new Padding(0,4,0,0)};appearanceChoices.Controls.Add(preview,0,2);
            foreach(string name in UiTheme.Styles){string style=name;var button=new PaletteButton{Text=name,AccessibleName=name,Palette=name,DarkPreview=dark.Checked,Margin=new Padding(0,2,6,6)};button.Click+=delegate{Value.AccentStyle=style;preview.Palette=style;preview.Invalidate();foreach(Control c in paletteChoices.Controls){c.Tag=c.Text==style?"selected":"quiet";c.Invalidate();}};paletteChoices.Controls.Add(button);button.Tag=name==Value.AccentStyle?"selected":"quiet";}
            paletteChoices.SizeChanged+=delegate{int columns=paletteChoices.ClientSize.Width>=510?3:2;appearanceChoices.RowStyles[1].Height=columns==3?174:258;appearance.Height=columns==3?432:516;foreach(Control c in paletteChoices.Controls)c.Width=Math.Max(120,paletteChoices.ClientSize.Width/columns-7);};dark.CheckedChanged+=delegate{preview.DarkPreview=dark.Checked;preview.Invalidate();foreach(PaletteButton b in paletteChoices.Controls){b.DarkPreview=dark.Checked;b.Invalidate();}};
            var recording=DialogLayout.Card(stack,"Recording","Keep only the input you need. These options also appear in the workbench.",160);var recordingChoices=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};moves.Checked=Value.RecordMoves;relative.Checked=Value.RelativeCoordinates;recordingChoices.Controls.Add(moves);recordingChoices.Controls.Add(relative);recording.Controls.Add(recordingChoices,0,2);
            var playback=DialogLayout.Card(stack,"Playback defaults","Set the run count and speed. Stop remains available during playback.",160);var playbackChoices=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true};runs.Value=Math.Max(1,Value.Repetitions);speed.Value=(decimal)Value.Speed;forever.Checked=Value.Repetitions==0;runs.Enabled=!forever.Checked;forever.CheckedChanged+=delegate{runs.Enabled=!forever.Checked;};playbackChoices.Controls.Add(new Label{Text="Runs",AutoSize=true,Padding=new Padding(0,5,0,0)});playbackChoices.Controls.Add(NumberFrame(runs,105));playbackChoices.Controls.Add(forever);playbackChoices.Controls.Add(new Label{Text="Speed ×",AutoSize=true,Padding=new Padding(10,5,0,0)});playbackChoices.Controls.Add(NumberFrame(speed,90));playback.Controls.Add(playbackChoices,0,2);
            var footer=DialogLayout.Footer(root);DialogLayout.Button(footer,"Save preferences",delegate{Value.DarkMode=dark.Checked;Value.RecordMoves=moves.Checked;Value.RelativeCoordinates=relative.Checked;Value.Repetitions=forever.Checked?0:(int)runs.Value;Value.Speed=(double)speed.Value;DialogResult=DialogResult.OK;Close();},"primary");CancelButton=DialogLayout.Button(footer,"Cancel",delegate{DialogResult=DialogResult.Cancel;Close();});UiTheme.Apply(this);
        }
        private static Control NumberFrame(NumericUpDown number,int width){number.BorderStyle=BorderStyle.None;number.Dock=DockStyle.Fill;var frame=new FieldFrame{Width=width,Height=34,Margin=new Padding(4,0,8,0)};frame.Controls.Add(number);return frame;}
    }
    internal sealed class ScheduleForm : ThemeWindow
    {
        public string CreatedTask {get;private set;}
        public ScheduleForm(Func<DateTime,bool,string> create)
        {
            Text="Schedule workflow";Size=new Size(700,470);MinimumSize=new Size(640,440);var root=DialogLayout.Root(this,"Schedule workflow","Choose when this saved workflow should run. You must be signed in with the desktop unlocked.");
            var card=new RoundedLayout{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=4};card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));card.RowStyles.Add(new RowStyle(SizeType.Absolute,30));card.RowStyles.Add(new RowStyle(SizeType.Absolute,44));card.RowStyles.Add(new RowStyle(SizeType.Absolute,42));card.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.Controls.Add(card,0,1);card.Controls.Add(new Label{Text="Start date and time",Dock=DockStyle.Fill,Font=new Font(Font,FontStyle.Bold)},0,0);
            var date=new DateTimePicker{Value=DateTime.Now.AddMinutes(10),Format=DateTimePickerFormat.Custom,CustomFormat="ddd, dd MMM yyyy  HH:mm",Dock=DockStyle.Fill,AccessibleName="Schedule start time"};card.Controls.Add(date,0,1);var daily=new ThemeCheckBox{Text="Repeat every day at this time",Dock=DockStyle.Fill,AccessibleName="Repeat schedule daily"};card.Controls.Add(daily,0,2);var error=new Label{Dock=DockStyle.Fill,Tag="muted",AccessibleName="Schedule feedback",Text="Leave daily repetition off to run once. Manage existing schedules in Windows Task Scheduler."};card.Controls.Add(error,0,3);
            var footer=DialogLayout.Footer(root);DialogLayout.Button(footer,"Create schedule",delegate{try{if(date.Value<=DateTime.Now)throw new InvalidOperationException("Choose a future start time.");CreatedTask=create(date.Value,daily.Checked);DialogResult=DialogResult.OK;Close();}catch(Exception failure){error.Text=failure.Message;error.ForeColor=UiTheme.Danger;}},"primary");CancelButton=DialogLayout.Button(footer,"Cancel",delegate{DialogResult=DialogResult.Cancel;Close();});UiTheme.Apply(this);
        }
    }
    internal sealed class WorkflowPicker : ThemeWindow
    {
        public string SelectedPath {get;private set;}
        private readonly string[] paths;
        private readonly TextBox search=new HintTextBox{Hint="Search workflow names or locations",Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,AccessibleName="Search recent workflows"};
        private readonly ListView list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false,BorderStyle=BorderStyle.None,AccessibleName="Recent workflow selection",ShowItemToolTips=true};
        private readonly Label state=new Label{Dock=DockStyle.Fill,Tag="muted",AutoEllipsis=true};
        private readonly Button open;
        public WorkflowPicker(string[] recent,Func<string> browse)
        {
            paths=(recent??new string[0]).Where(p=>!string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();Text="Open workflow";Size=new Size(900,620);MinimumSize=new Size(720,480);
            var root=DialogLayout.Root(this,"Open a workflow","Choose recent work or browse for a .mexec file. Opening does not start playback.");var content=new RoundedLayout{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=1,RowCount=3};content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Absolute,44));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Absolute,38));root.Controls.Add(content,0,1);content.Controls.Add(DialogLayout.SearchField(search,"Find workflow"),0,0);
            list.Columns.Add("Workflow",230);list.Columns.Add("Location",390);list.Columns.Add("Status",105);list.ClientSizeChanged+=delegate{list.Columns[1].Width=Math.Max(180,list.ClientSize.Width-335);};content.Controls.Add(list,0,1);content.Controls.Add(state,0,2);
            var footer=DialogLayout.Footer(root);open=DialogLayout.Button(footer,"Open selected",delegate{Choose();},"primary");CancelButton=DialogLayout.Button(footer,"Cancel",delegate{DialogResult=DialogResult.Cancel;Close();});DialogLayout.Button(footer,"Browse files…",delegate{string path=browse();if(!string.IsNullOrEmpty(path))Accept(path);});AcceptButton=open;
            search.TextChanged+=delegate{RefreshRows();};list.SelectedIndexChanged+=delegate{UpdateState();};list.DoubleClick+=delegate{Choose();};RefreshRows();UiTheme.Apply(this);
        }
        private void RefreshRows() { list.BeginUpdate();list.Items.Clear();foreach(string path in paths.Where(p=>p.IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0)){var item=new ListViewItem(WorkflowFiles.DisplayName(path)){Tag=path,ToolTipText=path};item.SubItems.Add(Path.GetDirectoryName(path));item.SubItems.Add(File.Exists(path)?"Available":"Unavailable");list.Items.Add(item);}list.EndUpdate();if(list.Items.Count>0)list.Items[0].Selected=true;UpdateState(); }
        private void UpdateState() { string path=list.SelectedItems.Count>0?list.SelectedItems[0].Tag as string:null;open.Enabled=path!=null&&File.Exists(path);state.Text=path==null?(paths.Length==0?"No recent workflows yet. Browse to open your first file.":list.Items.Count==0?"No workflows match your search.":"Select a workflow to see its location."):File.Exists(path)?path:"This file has moved or is unavailable. Use Browse files to locate it."; }
        private void Choose() { if(list.SelectedItems.Count>0)Accept((string)list.SelectedItems[0].Tag); }
        private void Accept(string path) { if(!File.Exists(path)){state.Text="This workflow is unavailable. Choose another file.";return;}SelectedPath=path;DialogResult=DialogResult.OK;Close(); }
    }
    internal sealed class WorkflowBindingsForm : ThemeWindow
    {
        public string[] Value {get;private set;}
        public WorkflowBindingsForm(string[] values,Shortcut[] shortcuts,Func<string> browse)
        {
            Value=new string[9];Array.Copy(values??new string[0],Value,Math.Min(9,(values??new string[0]).Length));Text="Workflow shortcuts";Size=new Size(900,760);MinimumSize=new Size(760,550);var root=DialogLayout.Root(this,"Workflow shortcuts","Choose a saved workflow for each key. MacroExecute must remain open; each shortcut starts the normal countdown.");var stack=DialogLayout.Stack(root);
            for(int i=0;i<9;i++) {
                int index=i;var row=new RoundedLayout{Height=96,Width=790,Padding=new Padding(14),Margin=new Padding(0,0,0,10),ColumnCount=4,RowCount=2};row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,125));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,72));row.RowStyles.Add(new RowStyle(SizeType.Absolute,30));row.RowStyles.Add(new RowStyle(SizeType.Percent,100));
                var key=new Label{Text="Ctrl + Alt + "+(i+1),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Tag="secondary"};row.Controls.Add(key,0,0);row.SetRowSpan(key,2);var name=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,Font=new Font(Font,FontStyle.Bold)};var location=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,Tag="muted"};row.Controls.Add(name,1,0);row.Controls.Add(location,1,1);
                var choose=new ThemeButton{Text="Choose…",AccessibleName="Choose workflow "+(i+1),Size=new Size(94,34),Anchor=AnchorStyles.Right};var clear=new ThemeButton{Text="Clear",AccessibleName="Clear workflow "+(i+1),Size=new Size(66,34),Anchor=AnchorStyles.Right,Tag="quiet"};row.Controls.Add(choose,2,0);row.SetRowSpan(choose,2);row.Controls.Add(clear,3,0);row.SetRowSpan(clear,2);
                Action update=delegate{string path=Value[index];name.Text=string.IsNullOrWhiteSpace(path)?"No workflow assigned":WorkflowFiles.DisplayName(path);location.Text=string.IsNullOrWhiteSpace(path)?"Choose a .mexec workflow to use this shortcut.":File.Exists(path)?path:"Unavailable · "+path;clear.Enabled=!string.IsNullOrWhiteSpace(path);};choose.Click+=delegate{string path=browse();if(!string.IsNullOrEmpty(path)){Value[index]=path;update();}};clear.Click+=delegate{Value[index]="";update();};update();stack.Controls.Add(row);
            }
            var footer=DialogLayout.Footer(root);var error=new Label{AutoSize=true,MaximumSize=new Size(440,0),Tag="danger",AccessibleName="Binding validation"};DialogLayout.Button(footer,"Save bindings",delegate{try{Shortcuts.Validate(shortcuts,Value);DialogResult=DialogResult.OK;Close();}catch(Exception failure){error.Text=failure.Message;}},"primary");CancelButton=DialogLayout.Button(footer,"Cancel",delegate{DialogResult=DialogResult.Cancel;Close();});footer.Controls.Add(error);UiTheme.Apply(this);
        }
    }
}
