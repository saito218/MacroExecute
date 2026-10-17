using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacroExecute
{
    internal sealed class MacroListView : ListView
    {
        internal bool StepNumbersFit { get { return Columns.Count>0&&Columns[0].Width-36>=TextRenderer.MeasureText(Math.Max(1,Items.Count).ToString(),Font).Width; } }
        internal void FitStepNumbers()
        {
            if(Columns.Count==0)return;
            // DrawStepCell reserves 12 px of cell padding and 24 px for its icon.
            Columns[0].Width=Math.Max(Columns[0].Width,TextRenderer.MeasureText(Math.Max(1,Items.Count).ToString(),Font).Width+44);
        }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e);FitStepNumbers(); }
        protected override void OnItemSelectionChanged(ListViewItemSelectionChangedEventArgs e){base.OnItemSelectionChanged(e);Invalidate();}
        protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
        protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
        private int gap=-1;
        public int DropGap {get{return gap;}set{if(gap==value)return;gap=value;Invalidate();}}
        internal int GapAt(Point point)
        {
            if(point.X<0||point.X>=ClientSize.Width||point.Y<0||point.Y>=ClientSize.Height)return -1;if(Items.Count==0)return 0;
            var hit=GetItemAt(5,point.Y);if(hit!=null)return hit.Index+(point.Y>=hit.Bounds.Top+hit.Bounds.Height/2?1:0);
            if(point.Y>=Items[Items.Count-1].Bounds.Bottom)return Items.Count;return -1;
        }
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);if(gap<0||gap>Items.Count||(message.Msg!=0xF&&message.Msg!=0x317&&message.Msg!=0x318))return;if(message.Msg!=0xF&&message.WParam==IntPtr.Zero)return;
            int y=Items.Count==0?28:gap==Items.Count?Items[Items.Count-1].Bounds.Bottom:Items[gap].Bounds.Top;if(y<0||y>ClientSize.Height)return;
            using(var g=message.Msg==0xF?Graphics.FromHwnd(Handle):Graphics.FromHdc(message.WParam))using(var pen=new Pen(UiTheme.Accent,3))using(var fill=new SolidBrush(UiTheme.Accent)){g.DrawLine(pen,4,y,ClientSize.Width-4,y);g.FillPolygon(fill,new[]{new Point(3,y-5),new Point(10,y),new Point(3,y+5)});}
        }
    }
}
