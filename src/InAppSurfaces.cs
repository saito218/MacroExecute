using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
namespace MacroExecute
{
    internal static class InAppCoordinates
    {
        internal static Point RelativeTo(Control control,Control root){Point point=Point.Empty;for(Control current=control;current!=null&&current!=root;current=current.Parent)point.Offset(current.Location);return point;}
    }
    internal static class InAppDialog
    {
        private static readonly Dictionary<Form,int> active=new Dictionary<Form,int>();
        internal static bool IsOpen(Form owner){return active.ContainsKey(owner);}
        internal sealed class Surface : IDisposable
        {
            private readonly Form owner,dialog;private readonly Panel host;private readonly Dictionary<Control,bool> enabled=new Dictionary<Control,bool>();
            internal Surface(Form owner,Form dialog)
            {
                this.owner=owner;this.dialog=dialog;InWindowMenu.Close(owner);active[owner]=active.ContainsKey(owner)?active[owner]+1:1;
                foreach(Control child in owner.Controls){enabled[child]=child.Enabled;child.Enabled=false;}
                host=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=UiTheme.Background,Padding=new Padding(12),AccessibleName="In-app dialog surface"};owner.Controls.Add(host);host.BringToFront();
                dialog.TopLevel=false;dialog.ShowInTaskbar=false;dialog.FormBorderStyle=FormBorderStyle.None;dialog.Region=null;dialog.StartPosition=FormStartPosition.Manual;host.Controls.Add(dialog);
                foreach(var control in Descendants(dialog).Where(c=>c.AccessibleName=="Minimize window"||c.AccessibleName=="Maximize window"))control.Visible=false;
                host.Resize+=Resize;Resize(null,EventArgs.Empty);dialog.Visible=true;UiTheme.Apply(host);
            }
            private void Resize(object sender,EventArgs e){if(dialog.IsDisposed)return;int w=Math.Max(dialog.MinimumSize.Width,Math.Min(dialog.Width,Math.Max(300,host.ClientSize.Width-24)));int h=Math.Max(dialog.MinimumSize.Height,Math.Min(dialog.Height,Math.Max(200,host.ClientSize.Height-24)));dialog.Bounds=new Rectangle(Math.Max(12,(host.ClientSize.Width-w)/2),12,w,h);host.AutoScrollMinSize=new Size(w+24,h+24);}
            public void Dispose(){if(!dialog.IsDisposed){dialog.Visible=false;host.Controls.Remove(dialog);}host.Dispose();foreach(var entry in enabled)if(!entry.Key.IsDisposed)entry.Key.Enabled=entry.Value;if(active.ContainsKey(owner)&&--active[owner]==0)active.Remove(owner);if(!owner.IsDisposed)owner.Invalidate(true);}
        }
        private static IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
        internal static DialogResult Message(Form owner,string text,string title="MacroExecute",MessageBoxButtons buttons=MessageBoxButtons.OK,MessageBoxIcon icon=MessageBoxIcon.None)
        {
            using(var dialog=new ThemeWindow{Text=title,Size=new Size(660,370),MinimumSize=new Size(440,280),Font=owner.Font}){
                var body=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24,48,24,20),ColumnCount=1,RowCount=2};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Absolute,48));dialog.Controls.Add(body);body.BringToFront();body.Controls.Add(new TextBox{Text=text,Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill},0,0);
                var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};body.Controls.Add(footer,0,1);
                DialogResult[] results=buttons==MessageBoxButtons.YesNoCancel?new[]{DialogResult.Cancel,DialogResult.No,DialogResult.Yes}:buttons==MessageBoxButtons.OKCancel?new[]{DialogResult.Cancel,DialogResult.OK}:new[]{DialogResult.OK};
                foreach(var result in results){var value=result;var button=new ThemeButton{Text=value.ToString(),Width=100,Height=36,Tag=value==DialogResult.OK||value==DialogResult.Yes?"primary":"quiet"};button.Click+=delegate{dialog.DialogResult=value;};footer.Controls.Add(button);if(value==DialogResult.Cancel)dialog.CancelButton=button;if(value==DialogResult.OK||value==DialogResult.Yes)dialog.AcceptButton=button;}
                return Show(owner,dialog);
            }
        }
        internal static Surface Attach(Form owner,Form dialog){return new Surface(owner,dialog);}
        internal static DialogResult Show(Form owner,Form dialog)
        {
            dialog.DialogResult=DialogResult.None;using(Attach(owner,dialog)){
                dialog.Select();while(!owner.IsDisposed&&!dialog.IsDisposed&&dialog.DialogResult==DialogResult.None&&dialog.Visible){Application.DoEvents();Thread.Sleep(10);}
                DialogResult result=dialog.DialogResult==DialogResult.None?DialogResult.Cancel:dialog.DialogResult;if(!dialog.IsDisposed)dialog.Close();return result;
            }
        }
    }
    internal sealed class InWindowMenu : IMessageFilter,IDisposable
    {
        private static readonly Dictionary<Form,InWindowMenu> open=new Dictionary<Form,InWindowMenu>();
        private readonly Form owner;private readonly FlowLayoutPanel panel;private readonly Control anchor;private readonly Point? offset;
        private InWindowMenu(Form owner,Control anchor,ToolStripItemCollection items,Point? offset)
        {
            this.owner=owner;this.anchor=anchor;this.offset=offset;panel=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Width=310,BackColor=UiTheme.Surface,Padding=new Padding(6),AccessibleName="In-app navigation menu",BorderStyle=BorderStyle.FixedSingle};owner.Controls.Add(panel);panel.BringToFront();Populate(items,null);Application.AddMessageFilter(this);owner.Disposed+=OwnerClosed;
        }
        private void OwnerClosed(object sender,EventArgs e){Dispose();}
        private void Populate(ToolStripItemCollection items,ToolStripItemCollection back)
        {
            foreach(Control child in panel.Controls.Cast<Control>().ToArray())child.Dispose();if(back!=null){var up=Item("Back",true);up.Click+=delegate{Populate(back,null);};panel.Controls.Add(up);}
            foreach(ToolStripItem item in items){if(item is ToolStripSeparator){panel.Controls.Add(new Panel{Height=1,Width=280,BackColor=UiTheme.Border});continue;}var submenu=item as ToolStripDropDownItem;bool children=submenu!=null&&submenu.DropDownItems.Count>0;var button=Item(item.Text.Replace("&","")+(children?"  >":""),item.Enabled);var check=item as ToolStripMenuItem;if(check!=null&&check.Checked)button.Text="✓ "+button.Text;button.Click+=delegate{if(children){Populate(submenu.DropDownItems,items);return;}Dispose();item.PerformClick();};panel.Controls.Add(button);}
            panel.Height=Math.Min(owner.ClientSize.Height-35,panel.Controls.Cast<Control>().Sum(c=>c.Height+c.Margin.Vertical)+16);Point location=Point.Add(InAppCoordinates.RelativeTo(anchor,owner),new Size(offset??new Point(0,anchor.Height)));panel.Location=new Point(Math.Max(8,Math.Min(location.X,owner.ClientSize.Width-panel.Width-8)),Math.Max(8,Math.Min(location.Y,owner.ClientSize.Height-panel.Height-8)));UiTheme.Apply(panel);panel.BringToFront();if(panel.Controls.Count>0)panel.Controls[0].Select();
        }
        private Button Item(string text,bool enabled){return new ThemeButton{Text=text,Enabled=enabled,Width=284,Height=36,TextAlign=ContentAlignment.MiddleLeft,Tag="quiet",AccessibleName=text,Margin=new Padding(0,1,0,1)};}
        internal static void Show(Form owner,Control anchor,ToolStripItemCollection items,Point? offset=null){Close(owner);var menu=new InWindowMenu(owner,anchor,items,offset);open[owner]=menu;}
        internal static void Close(Form owner){InWindowMenu menu;if(open.TryGetValue(owner,out menu))menu.Dispose();}
        public bool PreFilterMessage(ref Message message){if(message.Msg==0x100&&((Keys)(int)message.WParam==Keys.Up||(Keys)(int)message.WParam==Keys.Down)){var buttons=panel.Controls.OfType<Button>().Where(b=>b.Enabled).ToArray();if(buttons.Length>0){int current=Array.FindIndex(buttons,b=>b.ContainsFocus);int direction=(Keys)(int)message.WParam==Keys.Down?1:-1;buttons[(current+direction+buttons.Length)%buttons.Length].Select();}return true;}if(message.Msg==0x100&&((Keys)(int)message.WParam==Keys.Enter||(Keys)(int)message.WParam==Keys.Right)){var selected=panel.Controls.OfType<Button>().FirstOrDefault(b=>b.ContainsFocus);if(selected!=null&&((Keys)(int)message.WParam==Keys.Enter||selected.Text.EndsWith("  >"))){selected.PerformClick();return true;}}if(message.Msg==0x100&&(Keys)(int)message.WParam==Keys.Escape){Dispose();return true;}if(message.Msg==0x201||message.Msg==0x204){Control target=Control.FromHandle(message.HWnd);if(target!=panel&&(target==null||!panel.Contains(target)))Dispose();}return false;}
        public void Dispose(){Application.RemoveMessageFilter(this);owner.Disposed-=OwnerClosed;open.Remove(owner);if(!panel.IsDisposed)panel.Dispose();}
    }
}
