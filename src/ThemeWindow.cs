using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MacroExecute
{
    // Keep native resizing/system commands, but paint the caption with the app palette.
    public class ThemeWindow : Form
    {
        private readonly Label caption=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,Padding=new Padding(18,0,0,0),Tag="muted"};
        private readonly Button maximize=new ThemeButton{Text="□",AccessibleName="Maximize window",Dock=DockStyle.Fill,Tag="quiet"};
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
        public ThemeWindow()
        {
            Icon=AppBrand.ApplicationIcon;FormBorderStyle=FormBorderStyle.None;Padding=new Padding(6);SetStyle(ControlStyles.ResizeRedraw,true);
            var bar=new TableLayoutPanel{Dock=DockStyle.Top,Height=40,ColumnCount=4,RowCount=1,Tag="canvas",Margin=Padding.Empty,Padding=new Padding(5,3,8,0)};
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=0;i<3;i++)bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,42));
            var minimize=new ThemeButton{Text="−",AccessibleName="Minimize window",Dock=DockStyle.Fill,Tag="quiet"};
            var close=new ThemeButton{Text="×",AccessibleName="Close window",Dock=DockStyle.Fill,Tag="danger"};
            foreach(var button in new[]{minimize,maximize,close}){button.Font=new Font("Segoe UI",12);button.Margin=new Padding(2,0,2,2);button.AccessibleDescription=button.AccessibleName;}bar.Controls.Add(caption,0,0);bar.Controls.Add(minimize,1,0);bar.Controls.Add(maximize,2,0);bar.Controls.Add(close,3,0);Controls.Add(bar);
            minimize.Click+=delegate{WindowState=FormWindowState.Minimized;};maximize.Click+=delegate{ToggleMaximize();};close.Click+=delegate{Close();};
            caption.MouseDown+=delegate(object sender,MouseEventArgs e){if(!TopLevel||e.Button!=MouseButtons.Left)return;if(e.Clicks==2){ToggleMaximize();return;}ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);};
            TextChanged+=delegate{caption.Text=Text;};Resize+=delegate{maximize.Text=WindowState==FormWindowState.Maximized?"❐":"□";maximize.AccessibleName=WindowState==FormWindowState.Maximized?"Restore window":"Maximize window";};
        }
        protected override CreateParams CreateParams {get{var cp=base.CreateParams;if(TopLevel)cp.Style|=0x40000|0x80000|0x20000|0x10000;return cp;}}
        protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);MaximizedBounds=Screen.FromHandle(Handle).WorkingArea;}
        protected override void OnLocationChanged(EventArgs e){base.OnLocationChanged(e);if(IsHandleCreated&&WindowState==FormWindowState.Normal)MaximizedBounds=Screen.FromHandle(Handle).WorkingArea;}
        private void ToggleMaximize(){MaximizedBounds=Screen.FromHandle(Handle).WorkingArea;WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;}
        internal static int ResizeHit(Point p,Size size,int edge)
        {
            bool l=p.X<edge,r=p.X>=size.Width-edge,t=p.Y<edge,b=p.Y>=size.Height-edge;
            return t?(l?13:r?14:12):b?(l?16:r?17:15):l?10:r?11:1;
        }
        protected override void WndProc(ref Message message)
        {
            if(TopLevel&&message.Msg==0x83){message.Result=IntPtr.Zero;return;}
            base.WndProc(ref message);
            if(TopLevel&&message.Msg==0x84&&WindowState==FormWindowState.Normal){long raw=message.LParam.ToInt64();var p=PointToClient(new Point((short)(raw&0xffff),(short)((raw>>16)&0xffff)));int hit=ResizeHit(p,ClientSize,Math.Max(6,(int)(6*DeviceDpi/96f)));if(hit!=1)message.Result=new IntPtr(hit);}
        }
        protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<20||Height<20)return;var previous=Region;if(WindowState==FormWindowState.Normal){using(var path=SoftGeometry.Round(new RectangleF(0,0,ClientSize.Width,ClientSize.Height),12))Region=new Region(path);}else Region=null;if(previous!=null)previous.Dispose();}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(WindowState==FormWindowState.Normal){e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var path=SoftGeometry.Round(new RectangleF(0.5f,0.5f,Math.Max(1,ClientSize.Width-1),Math.Max(1,ClientSize.Height-1)),12))using(var pen=new Pen(UiTheme.Stroke))e.Graphics.DrawPath(pen,path);}}
    }
}
