using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MacroExecute
{
    internal sealed class HintTextBox : TextBox
    {
        public string Hint {get;set;}
        [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr window,int message,IntPtr parameter,string text);
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e);SendMessage(Handle,0x1501,new IntPtr(1),Hint??""); }
    }
    internal static class SoftGeometry
    {
        public static GraphicsPath Round(RectangleF rect,float radius){var path=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(rect.Width,rect.Height));if(d<=0)return path;path.AddArc(rect.X,rect.Y,d,d,180,90);path.AddArc(rect.Right-d,rect.Y,d,d,270,90);path.AddArc(rect.Right-d,rect.Bottom-d,d,d,0,90);path.AddArc(rect.X,rect.Bottom-d,d,d,90,90);path.CloseFigure();return path;}
        public static Color Blend(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
    }
    internal sealed class RoundedLayout : TableLayoutPanel
    {
        public bool Emphasized {get;set;}
        public RoundedLayout(){DoubleBuffered=true;Tag="card";}
        protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?UiTheme.Background:Parent.BackColor);if(Width<4||Height<4)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=SoftGeometry.Round(new RectangleF(1,1,Width-3,Height-3),16))using(var fill=new SolidBrush(UiTheme.Surface)){e.Graphics.FillPath(fill,path);using(var border=new Pen(Emphasized?SoftGeometry.Blend(UiTheme.Surface,UiTheme.Accent,0.6f):UiTheme.Stroke))e.Graphics.DrawPath(border,path);}}
    }
    internal sealed class RoundedFlow : FlowLayoutPanel
    {
        public RoundedFlow(){DoubleBuffered=true;Tag="card";}
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent==null?UiTheme.Background:Parent.BackColor);
            if(Width<4||Height<4)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var path=SoftGeometry.Round(new RectangleF(1,1,Width-3,Height-3),12))using(var fill=new SolidBrush(UiTheme.Surface)){e.Graphics.FillPath(fill,path);using(var outline=new Pen(UiTheme.Stroke))e.Graphics.DrawPath(outline,path);}
        }
    }
    internal sealed class FieldFrame : Panel
    {
        public FieldFrame(){DoubleBuffered=true;Padding=new Padding(8,5,8,3);Tag="input";}
        protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?UiTheme.Surface:Parent.BackColor);if(Width<3||Height<3)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=SoftGeometry.Round(new RectangleF(0,0,Width-1,Height-1),8))using(var fill=new SolidBrush(UiTheme.Field)){e.Graphics.FillPath(fill,path);using(var outline=new Pen(ContainsFocus?UiTheme.Accent:UiTheme.Stroke,ContainsFocus?2:1))e.Graphics.DrawPath(outline,path);}}
        protected override void OnControlAdded(ControlEventArgs e){base.OnControlAdded(e);e.Control.Enter+=delegate{Invalidate();};e.Control.Leave+=delegate{Invalidate();};}

    }
    internal sealed class ThemeCheckBox : CheckBox
    {
        public ThemeCheckBox(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;}
        public override Size GetPreferredSize(Size proposedSize){var text=TextRenderer.MeasureText(Text,Font);return new Size(text.Width+36+Padding.Horizontal,Math.Max(24,text.Height+4)+Padding.Vertical);}
        protected override void OnCheckedChanged(EventArgs e){Invalidate();base.OnCheckedChanged(e);}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent==null?UiTheme.Surface:Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;int top=(Height-14)/2;using(var path=SoftGeometry.Round(new RectangleF(2,top,26,14),7))using(var fill=new SolidBrush(Enabled&&Checked?UiTheme.Accent:UiTheme.Border))g.FillPath(fill,path);using(var knob=new SolidBrush(Checked&&Enabled?UiTheme.PrimaryInk:UiTheme.Muted))g.FillEllipse(knob,Checked?16:4,top+2,10,10);TextRenderer.DrawText(g,Text,Font,new Rectangle(34,0,Math.Max(1,Width-34),Height),Enabled?UiTheme.Ink:UiTheme.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);if(Focused&&ShowFocusCues)using(var path=SoftGeometry.Round(new RectangleF(0,top-2,30,18),9))using(var pen=new Pen(UiTheme.Accent,2))g.DrawPath(pen,path);}
    }
    internal class ThemeButton : Button
    {
        private bool hover,pressed;
        internal bool CompactIconOnly {get;set;}
        public override Size GetPreferredSize(Size proposedSize){string symbol=IconArt.ControlSymbol(Text,AccessibleName);if(Image!=null||this is PaletteButton||symbol==null)return base.GetPreferredSize(proposedSize);string label=IconArt.Label(Text);bool iconOnly=CompactIconOnly||label.Length<=1;var measured=TextRenderer.MeasureText(label,Font,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);return new Size(Math.Max(MinimumSize.Width,iconOnly?36:measured.Width+42+Padding.Horizontal),Math.Max(MinimumSize.Height,Math.Max(34,measured.Height+10+Padding.Vertical)));}


        public ThemeButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;Cursor=Cursors.Hand;}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=pressed=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnMouseDown(MouseEventArgs e){pressed=e.Button==System.Windows.Forms.MouseButtons.Left;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
        protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}protected override void OnLostFocus(EventArgs e){pressed=false;Invalidate();base.OnLostFocus(e);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space){pressed=true;Invalidate();}base.OnKeyDown(e);}protected override void OnKeyUp(KeyEventArgs e){pressed=false;Invalidate();base.OnKeyUp(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent==null?UiTheme.Surface:Parent.BackColor);string role=Tag as string;bool primary=role=="primary",selected=role=="selected",quiet=role=="quiet";
            Color fill=primary?UiTheme.Primary:selected?UiTheme.Selected:quiet?(Parent==null?UiTheme.Background:Parent.BackColor):UiTheme.Field;Color ink=primary?UiTheme.PrimaryInk:selected?UiTheme.Accent:role=="danger"?UiTheme.Danger:UiTheme.Ink;
            if(Enabled&&hover)fill=SoftGeometry.Blend(fill,primary?UiTheme.Ink:UiTheme.Accent,0.10f);if(Enabled&&pressed)fill=SoftGeometry.Blend(fill,UiTheme.Accent,0.18f);if(!Enabled){fill=SoftGeometry.Blend(Parent==null?UiTheme.Surface:Parent.BackColor,UiTheme.Field,0.35f);ink=UiTheme.Muted;}
            using(var path=SoftGeometry.Round(new RectangleF(1,1,Math.Max(1,Width-3),Math.Max(1,Height-3)),primary?12:10))using(var brush=new SolidBrush(fill)){g.FillPath(brush,path);if(Enabled&&!quiet&&!primary)using(var outline=new Pen(selected||hover?UiTheme.Accent:UiTheme.Stroke,1))g.DrawPath(outline,path);if(Enabled&&Focused&&ShowFocusCues)using(var pen=new Pen(UiTheme.Accent,2))g.DrawPath(pen,path);}
            string symbol=this is PaletteButton?null:IconArt.ControlSymbol(Text,AccessibleName);string label=Image==null&&symbol!=null?IconArt.Label(Text):Text;var text=new Rectangle(6,3,Math.Max(1,Width-12),Math.Max(1,Height-6));if(Image==null&&symbol!=null){bool iconOnly=CompactIconOnly||label.Length==0||label=="?"||label=="×"||label=="□"||label=="❐"||label=="−";int iconSize=iconOnly?18:20;IconArt.DrawControl(g,new Rectangle(iconOnly?(Width-iconSize)/2:8,(Height-iconSize)/2,iconSize,iconSize),symbol,ink);if(iconOnly)label="";else{text.X=34;text.Width=Math.Max(1,Width-40);}}if(Image!=null){if(TextImageRelation==TextImageRelation.ImageAboveText){int y=5;g.DrawImage(Image,(Width-Image.Width)/2,y,Image.Width,Image.Height);text.Y=y+Image.Height+1;text.Height=Math.Max(1,Height-text.Y-3);}else{g.DrawImage(Image,8,(Height-Image.Height)/2,Image.Width,Image.Height);text.X=Image.Width+14;text.Width=Math.Max(1,Width-text.X-5);}}if(!(this is PaletteButton))TextRenderer.DrawText(g,label,Font,text,ink,(TextAlign==ContentAlignment.MiddleLeft?TextFormatFlags.Left:TextFormatFlags.HorizontalCenter)|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        }
    }

    internal sealed class PaletteButton : ThemeButton
    {
        public string Palette {get;set;}public bool DarkPreview {get;set;}
        public PaletteButton(){Height=76;Width=170;Padding=new Padding(0,18,0,0);}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var palette=ThemePalette.Get(Palette,DarkPreview);Color accent=palette.Accent,surface=palette.Background;
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var path=SoftGeometry.Round(new RectangleF(12,8,Width-24,12),6))using(var fill=new SolidBrush(surface))e.Graphics.FillPath(fill,path);
            using(var fill=new SolidBrush(accent))e.Graphics.FillEllipse(fill,17,10,8,8);
            using(var pen=new Pen(accent,2))e.Graphics.DrawLine(pen,32,14,Width-19,14);
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(8,27,Width-16,23),Tag as string=="selected"?UiTheme.Accent:UiTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
            using(var small=new Font(Font.FontFamily,8))TextRenderer.DrawText(e.Graphics,ThemePalette.Description(Palette),small,new Rectangle(6,51,Width-12,19),UiTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        }
    }
    internal sealed class WelcomeGuide : Control
    {
        public WelcomeGuide(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AccessibleName="Getting started";AccessibleDescription="Capture: record or add actions. Refine: edit and arrange steps. Run: play and stop anytime.";TabStop=false;}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.Clear(UiTheme.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;
            bool compact=Height<235;int titleY=Math.Max(4,(Height-(compact?120:225))/2);
            using(var title=new Font(Font.FontFamily,compact?15:22,FontStyle.Bold))TextRenderer.DrawText(g,"Build your first workflow",title,new Rectangle(12,titleY,Width-24,compact?26:40),UiTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g,"Record a task, or add just the actions you need.",Font,new Rectangle(12,titleY+(compact?28:44),Width-24,30),UiTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.WordBreak);
            string[] titles={"Capture","Refine","Run"},details={"Record or add actions","Edit and arrange steps","Play · Stop anytime"};
            int gap=10,w=Math.Max(1,(Width-32-gap*2)/3),y=titleY+(compact?56:85),h=compact?Math.Min(58,Math.Max(40,Height-y-4)):90;
            for(int i=0;i<3;i++){var rect=new Rectangle(16+i*(w+gap),y,w,h);using(var path=SoftGeometry.Round(rect,12))using(var fill=new SolidBrush(UiTheme.Field))g.FillPath(fill,path);using(var bold=new Font(Font,FontStyle.Bold))TextRenderer.DrawText(g,(i+1)+"  "+titles[i],bold,new Rectangle(rect.X+6,y+(compact?4:8),w-12,25),UiTheme.Accent,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);TextRenderer.DrawText(g,details[i],Font,new Rectangle(rect.X+8,y+(compact?29:35),w-16,h-(compact?31:37)),UiTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.WordBreak);}
        }
    }
    internal sealed class ThemeTabs : TabControl
    {
        public bool CategoryStrip {get;set;}
        public ThemeTabs(){Padding=new Point(14,8);SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
        protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(UiTheme.Background);}
        protected override void OnPaint(PaintEventArgs e){int top=TabCount==0?0:GetTabRect(0).Bottom+3;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=SoftGeometry.Round(new RectangleF(1,top,Math.Max(1,Width-3),Math.Max(1,Height-top-2)),12))using(var fill=new SolidBrush(CategoryStrip?UiTheme.Surface:UiTheme.Background))e.Graphics.FillPath(fill,path);}
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if((message.Msg!=0xF&&message.Msg!=0x317&&message.Msg!=0x318)||TabCount==0||Alignment!=TabAlignment.Top)return;if(message.Msg!=0xF&&message.WParam==IntPtr.Zero)return;
            using(var g=message.Msg==0xF?Graphics.FromHwnd(Handle):Graphics.FromHdc(message.WParam)){
                g.SmoothingMode=SmoothingMode.AntiAlias;int bottom=GetTabRect(0).Bottom+3;using(var bg=new SolidBrush(UiTheme.Background)){g.FillRectangle(bg,0,0,Width,bottom);g.FillRectangle(bg,0,bottom,3,Height-bottom);g.FillRectangle(bg,Width-3,bottom,3,Height-bottom);g.FillRectangle(bg,0,Height-3,Width,3);}for(int i=0;i<TabCount;i++){var rect=GetTabRect(i);rect.Inflate(-2,-2);bool selected=i==SelectedIndex;if(selected||CategoryStrip)using(var path=SoftGeometry.Round(rect,9))using(var fill=new SolidBrush(selected?UiTheme.Selected:UiTheme.Field)){g.FillPath(fill,path);if(CategoryStrip)using(var border=new Pen(selected?UiTheme.Accent:UiTheme.Stroke,selected?2:1))g.DrawPath(border,path);}if(CategoryStrip&&selected)using(var underline=new Pen(UiTheme.Accent,3))g.DrawLine(underline,rect.Left+12,rect.Bottom-2,rect.Right-12,rect.Bottom-2);TextRenderer.DrawText(g,TabPages[i].Text,Font,rect,selected?UiTheme.Accent:UiTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);}
            }
        }
    }
}
