using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace MacroExecute
{
    // Physical screen coordinates and bitmap-local coordinates stay separate,
    // including when the virtual desktop starts left of or above the primary monitor.
    internal sealed class ScreenSelection
    {
        public Rectangle Desktop { get; private set; }
        public Rectangle Pixels { get; private set; }
        public Rectangle ScreenBounds { get { return new Rectangle(Desktop.X+Pixels.X,Desktop.Y+Pixels.Y,Pixels.Width,Pixels.Height); } }
        public ScreenSelection(Rectangle desktop) { if(desktop.Width<=0||desktop.Height<=0)throw new ArgumentException("The desktop must have a positive size.");Desktop=desktop; }
        public void Drag(Point start,Point end) { Pixels=Rectangle.Intersect(new Rectangle(Point.Empty,Desktop.Size),Rectangle.FromLTRB(Math.Min(start.X,end.X),Math.Min(start.Y,end.Y),Math.Max(start.X,end.X),Math.Max(start.Y,end.Y))); }
        public Bitmap Crop(Bitmap background) { if(Pixels.Width<=0||Pixels.Height<=0)throw new InvalidOperationException("Drag a non-empty rectangle first.");if(background.Size!=Desktop.Size)throw new ArgumentException("Screenshot dimensions do not match the desktop.");return background.Clone(Pixels,PixelFormat.Format32bppArgb); }
        public void ApplySearchArea(Step step) { if(Pixels.Width<=0||Pixels.Height<=0)throw new InvalidOperationException("Drag a non-empty rectangle first.");var area=ScreenBounds;step.SearchIn=SearchArea.Rectangle;step.X=area.X;step.Y=area.Y;step.Width=area.Width;step.Height=area.Height; }
    }

    // Hiding a modal form ends ShowDialog. Make the editor and its owner
    // transparent instead, preserving visibility, ownership and dialog lifetime.
    internal sealed class CaptureVisibility : IDisposable
    {
        private readonly List<Form> forms=new List<Form>();
        private readonly List<double> opacity=new List<double>();
        public CaptureVisibility(Form editor) { try { for(Form form=editor;form!=null;form=form.TopLevel?form.Owner:form.Parent==null?null:form.Parent.FindForm()){if(!form.TopLevel)continue;forms.Add(form);opacity.Add(form.Opacity);form.Opacity=0;} } catch { Dispose();throw; } }
        public void Dispose() { for(int i=forms.Count-1;i>=0;i--)if(!forms[i].IsDisposed)forms[i].Opacity=opacity[i];forms.Clear();opacity.Clear(); }
    }

    internal sealed class CaptureForm : Form
    {
        private readonly Bitmap background;
        private readonly ScreenSelection selection;
        private readonly string instruction;
        private Point start;
        private bool dragging;
        public Rectangle SelectedBounds { get { return selection.ScreenBounds; } }
        public CaptureForm(bool searchArea) : this(SystemInformation.VirtualScreen,null,searchArea) { }
        internal CaptureForm(Rectangle desktop,Bitmap screenshot,bool searchArea)
        {
            selection=new ScreenSelection(desktop);background=screenshot==null?Vision.Capture(desktop):(Bitmap)screenshot.Clone();
            instruction=searchArea?"Drag the image search area  •  Esc or right-click cancels":"Drag a reference image  •  Esc or right-click cancels";
            AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=desktop;TopMost=true;DoubleBuffered=true;Cursor=Cursors.Cross;KeyPreview=true;ShowInTaskbar=false;
        }
        protected override void OnKeyDown(KeyEventArgs e) { if(e.KeyCode==Keys.Escape){DialogResult=DialogResult.Cancel;e.Handled=true;}base.OnKeyDown(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if(e.Button==System.Windows.Forms.MouseButtons.Right){DialogResult=DialogResult.Cancel;return;}if(e.Button==System.Windows.Forms.MouseButtons.Left){start=e.Location;dragging=true;Capture=true;}base.OnMouseDown(e); }
        protected override void OnMouseMove(MouseEventArgs e) { if(dragging){selection.Drag(start,e.Location);Invalidate();}base.OnMouseMove(e); }
        protected override void OnMouseUp(MouseEventArgs e) { if(dragging&&e.Button==System.Windows.Forms.MouseButtons.Left){dragging=false;Capture=false;CompleteSelection(start,e.Location);Invalidate();}base.OnMouseUp(e); }
        internal void CompleteSelection(Point from,Point to) { selection.Drag(from,to);if(selection.Pixels.Width>0&&selection.Pixels.Height>0)DialogResult=DialogResult.OK; }
        internal void ApplySearchArea(Step step) { selection.ApplySearchArea(step); }
        protected override void OnPaint(PaintEventArgs e) { e.Graphics.DrawImageUnscaled(background,0,0);using(var shade=new SolidBrush(Color.FromArgb(95,0,0,0)))e.Graphics.FillRectangle(shade,ClientRectangle);var rect=selection.Pixels;if(rect.Width>0&&rect.Height>0){e.Graphics.DrawImage(background,rect,rect,GraphicsUnit.Pixel);using(var pen=new Pen(Color.FromArgb(83,222,174),2))e.Graphics.DrawRectangle(pen,rect);}e.Graphics.DrawString(instruction,SystemFonts.CaptionFont,Brushes.White,new PointF(24,24)); }
        public Bitmap SelectedBitmap() { return selection.Crop(background); }
        protected override void Dispose(bool disposing) { if(disposing)background.Dispose();base.Dispose(disposing); }
    }
}
