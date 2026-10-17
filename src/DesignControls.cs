using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace MacroExecute
{
    internal sealed class ThemeRadioButton : RadioButton
    {
        public ThemeRadioButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;UseMnemonic=false;}
        public override Size GetPreferredSize(Size proposedSize){return new Size(TextRenderer.MeasureText(Text,Font).Width+46,32);}
        protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent==null?UiTheme.Surface:Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(1,1,Math.Max(1,Width-3),Math.Max(1,Height-3));using(var path=SoftGeometry.Round(r,9))using(var fill=new SolidBrush(Checked?UiTheme.Selected:UiTheme.Surface))using(var edge=new Pen(Checked?UiTheme.Accent:UiTheme.Stroke)){g.FillPath(fill,path);g.DrawPath(edge,path);}int y=(Height-14)/2;using(var pen=new Pen(Enabled?UiTheme.Accent:UiTheme.Muted,1.5f))g.DrawEllipse(pen,11,y,14,14);if(Checked)using(var fill=new SolidBrush(Enabled?UiTheme.Accent:UiTheme.Muted))g.FillEllipse(fill,15,y+4,6,6);TextRenderer.DrawText(g,Text,Font,new Rectangle(35,0,Math.Max(1,Width-43),Height),Enabled?UiTheme.Ink:UiTheme.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(5,4,Math.Max(1,Width-10),Math.Max(1,Height-8)),UiTheme.Accent,UiTheme.Surface);}
    }
    internal sealed class ThemePreview : Control
    {
        public string Palette {get;set;}public bool DarkPreview {get;set;}
        public ThemePreview(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AccessibleName="Appearance preview";TabStop=false;}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var p=ThemePalette.Get(Palette,DarkPreview);g.Clear(Parent==null?UiTheme.Surface:Parent.BackColor);using(var frame=SoftGeometry.Round(new RectangleF(1,1,Width-3,Height-3),12))using(var fill=new SolidBrush(p.Background))g.FillPath(fill,frame);using(var accent=new SolidBrush(p.Accent))g.FillEllipse(accent,16,14,8,8);using(var bold=new Font(Font,FontStyle.Bold))TextRenderer.DrawText(g,Palette+" · "+(DarkPreview?"Dark":"Light"),bold,new Rectangle(32,7,Width-47,24),p.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);for(int i=0;i<2;i++){var rect=new Rectangle(14,38+i*30,Math.Max(1,Width-28),25);using(var path=SoftGeometry.Round(rect,7))using(var fill=new SolidBrush(i==0?p.Selected:p.Surface))g.FillPath(fill,path);TextRenderer.DrawText(g,i==0?"01     Mouse click     ·     Alt + Left":"02     IF clipboard matches     ·     2 groups",Font,Rectangle.Inflate(rect,-10,0),p.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);}}
    }
}
