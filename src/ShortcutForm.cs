using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    internal sealed class ShortcutBox : TextBox
    {
        public Shortcut Binding {get;private set;}
        public ShortcutBox(Shortcut shortcut){Binding=shortcut.Clone();ReadOnly=true;ShortcutsEnabled=false;Text=Binding.ToString();AccessibleName=MacroExecute.Shortcuts.Name(Binding.Command)+" shortcut";}
        public void ClearBinding(){Binding.Key=Binding.Modifiers=0;Text=Binding.ToString();}
        protected override bool IsInputKey(Keys keyData){return true;}
        protected override void OnKeyDown(KeyEventArgs e){e.SuppressKeyPress=true;e.Handled=true;if(!MacroExecute.Shortcuts.IsModifier(e.KeyCode)){Binding.Key=(int)e.KeyCode;Binding.Modifiers=(int)e.Modifiers;Text=Binding.ToString();}base.OnKeyDown(e);}
    }
    internal sealed class ShortcutForm : ThemeWindow
    {
        private Shortcut[] original;
        private readonly System.Collections.Generic.List<int> bindingRows=new System.Collections.Generic.List<int>();
        private readonly System.Collections.Generic.List<int> headerRows=new System.Collections.Generic.List<int>();
        private readonly System.Collections.Generic.List<ShortcutBox> boxes=new System.Collections.Generic.List<ShortcutBox>();
        private readonly TableLayoutPanel table=new TableLayoutPanel();
        private readonly TextBox search=new HintTextBox{Hint="Search by action, key or scope",Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,AccessibleName="Search keyboard shortcuts"};
        private readonly Label feedback=new Label{Dock=DockStyle.Fill,Tag="muted",AccessibleName="Shortcut feedback"};
        public ShortcutForm(Shortcut[] bindings,string[] files,Func<Shortcut[],string> save)
        {
            original=Shortcuts.Normalize(bindings);Text="Keyboard shortcuts";Size=new Size(840,780);MinimumSize=new Size(720,560);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
            var root=DialogLayout.Root(this,"Keyboard shortcuts","Click a key field, then press a combination. Global controls work outside the app; Stop must stay assigned.");
            var content=new RoundedLayout{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=1,RowCount=3};content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Absolute,44));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Absolute,38));root.Controls.Add(content,0,1);
            content.Controls.Add(DialogLayout.SearchField(search,"Find shortcut"),0,0);
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};content.Controls.Add(scroll,0,1);content.Controls.Add(feedback,0,2);table.Dock=DockStyle.Top;table.AutoSize=true;table.ColumnCount=4;table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,52));table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,76));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,48));table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,64));scroll.Controls.Add(table);Build();search.TextChanged+=delegate{Filter();};
            var footer=DialogLayout.Footer(root);var apply=new ThemeButton{Text="Save shortcuts",Tag="primary",AutoSize=true,Height=34};var cancel=new ThemeButton{Text="Cancel",DialogResult=DialogResult.Cancel,AutoSize=true,Height=34};var reset=new ThemeButton{Text="Restore defaults",AutoSize=true,Height=34};footer.Controls.AddRange(new Control[]{apply,cancel,reset});CancelButton=cancel;
            reset.Click+=delegate{original=Shortcuts.Defaults();Build();UiTheme.Apply(this);};apply.Click+=delegate{try{var values=boxes.Select(b=>b.Binding.Clone()).ToArray();Shortcuts.Validate(values,files);string error=save(values);if(error!=null){feedback.Tag="danger";feedback.ForeColor=UiTheme.Danger;feedback.Text=error;return;}DialogResult=DialogResult.OK;Close();}catch(Exception error){feedback.Tag="danger";feedback.ForeColor=UiTheme.Danger;feedback.Text=error.GetBaseException().Message;}};UiTheme.Apply(this);
        }
        private void Build()
        {
            foreach(Control child in table.Controls.Cast<Control>().ToArray())child.Dispose();table.Controls.Clear();table.RowStyles.Clear();table.RowCount=0;boxes.Clear();bindingRows.Clear();headerRows.Clear();
            foreach(bool global in new[]{true,false}) {
                int heading=table.RowCount++;headerRows.Add(heading);table.RowStyles.Add(new RowStyle(SizeType.Absolute,34));var title=new Label{UseMnemonic=false,Text=global?"PLAYBACK & RECORDING · GLOBAL":"EDITING & NAVIGATION · IN APP",Tag="secondary",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Font=new Font(Font,FontStyle.Bold)};table.Controls.Add(title,0,heading);table.SetColumnSpan(title,4);
                foreach(var binding in original.Where(b=>Shortcuts.Global(b.Command)==global)) {
                    int row=table.RowCount++;bindingRows.Add(row);table.RowStyles.Add(new RowStyle(SizeType.Absolute,48));var box=new ShortcutBox(binding){Dock=DockStyle.Fill};boxes.Add(box);table.Controls.Add(new Label{Text=Shortcuts.Name(binding.Command),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row);table.Controls.Add(new Label{Text=global?"Global":"In app",Tag="muted",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},1,row);box.BorderStyle=BorderStyle.None;var frame=new FieldFrame{Dock=DockStyle.Fill,Margin=new Padding(3,6,3,6)};frame.Controls.Add(box);table.Controls.Add(frame,2,row);var clear=new ThemeButton{Text="Clear",Dock=DockStyle.Fill,Tag="quiet",Margin=new Padding(3,6,3,6),Enabled=binding.Command!=AppCommand.Stop,AccessibleName="Clear "+Shortcuts.Name(binding.Command)};clear.Click+=delegate{box.ClearBinding();};table.Controls.Add(clear,3,row);
                }
            }
            Filter();
        }
        private void Filter()
        {
            foreach(int header in headerRows){bool visible=string.IsNullOrWhiteSpace(search.Text);table.RowStyles[header].Height=visible?34:0;table.GetControlFromPosition(0,header).Visible=visible;}int shown=0;for(int index=0;index<boxes.Count;index++){int row=bindingRows[index];var box=boxes[index];string words=Shortcuts.Name(box.Binding.Command)+" "+box.Text+" "+(Shortcuts.Global(box.Binding.Command)?"global":"in app");bool visible=words.IndexOf(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)>=0;table.RowStyles[row].Height=visible?48:0;for(int col=0;col<4;col++)table.GetControlFromPosition(col,row).Visible=visible;if(visible)shown++;}feedback.Tag="muted";feedback.ForeColor=UiTheme.Muted;feedback.Text=shown==0?"No matching shortcuts. Try an action name or key.":shown+" actions · global controls work in other applications";
        }
    }
}
