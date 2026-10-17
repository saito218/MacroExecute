using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MacroExecute
{
    internal static class UiTheme
    {
        public static bool Dark {get;set;}
        public static string Style {get;set;}
        public static readonly string[] Styles={"Neon","Aftershock","Crimson","Graphite","Ocean","Ember"};
        private static ThemePalette Palette {get{return ThemePalette.Get(Style,Dark);}}
        public static Color Background {get{return Palette.Background;}}
        public static Color Surface {get{return Palette.Surface;}}
        public static Color Field {get{return Palette.Field;}}
        public static Color Ink {get{return Palette.Ink;}}
        public static Color Muted {get{return Palette.Muted;}}
        public static Color Accent {get{return Palette.Accent;}}
        public static Color Border {get{return Palette.Border;}}
        public static Color Stroke {get{return SoftGeometry.Blend(Surface,Border,0.38f);}}
        public static Color Primary {get{return Accent;}}
        public static Color PrimaryInk {get{return Dark?Background:Color.White;}}
        public static Color Selected {get{return Palette.Selected;}}
        public static Color Danger {get{return Dark?Color.FromArgb(255,171,183):Color.FromArgb(159,36,68);}}
        public static Color Secondary {get{return Dark?SoftGeometry.Blend(Accent,Ink,0.45f):Muted;}}
        internal static Color NestingColor(int depth){return depth%3==0?Accent:depth%3==1?Secondary:Danger;}
        internal static Color RowColor(StepRow row){return row==null?Surface:Plan.IsIf(row.Kind)||row.Kind==ActionKind.Repeat?SoftGeometry.Blend(Surface,Accent,0.07f):EditorBlocks.IsBoundary(row.Kind)?SoftGeometry.Blend(Surface,Secondary,0.05f):Surface;}
        private static void DrawStepCell(DrawListViewSubItemEventArgs e,ListView list)
        {
            var row=e.Item.Tag as StepRow;var rect=Rectangle.Inflate(e.Bounds,-6,0);Color ink=e.Item.Selected?Ink:e.Item.ForeColor;
            if(e.ColumnIndex==0){if(e.Item.Selected)using(var marker=new SolidBrush(Accent))e.Graphics.FillRectangle(marker,e.Bounds.X,e.Bounds.Y,3,e.Bounds.Height);if(list.SmallImageList!=null){int index=list.SmallImageList.Images.IndexOfKey(e.Item.ImageKey);if(index>=0){list.SmallImageList.Draw(e.Graphics,e.Bounds.X+3,e.Bounds.Y+(e.Bounds.Height-list.SmallImageList.ImageSize.Height)/2,index);rect.X+=24;rect.Width=Math.Max(0,rect.Width-24);}}}
            if(row!=null&&e.ColumnIndex==1){int depth=Math.Min(row.Depth,8),middle=e.Bounds.Top+e.Bounds.Height/2;for(int level=0;level<depth;level++)using(var guide=new Pen(NestingColor(level),1))e.Graphics.DrawLine(guide,rect.X+level*18+3,e.Bounds.Top,rect.X+level*18+3,e.Bounds.Bottom);
                bool opening=Plan.IsIf(row.Kind)||row.Kind==ActionKind.Repeat,boundary=EditorBlocks.IsBoundary(row.Kind);int x=rect.X+depth*18+3;if(opening||boundary)using(var guide=new Pen(NestingColor(row.Depth),2)){e.Graphics.DrawLine(guide,x,boundary?e.Bounds.Top:middle,x,opening||row.Kind==ActionKind.Else?e.Bounds.Bottom:middle);e.Graphics.DrawLine(guide,x,middle,x+7,middle);}rect.X+=depth*18+14;rect.Width=Math.Max(0,e.Bounds.Right-rect.X-5);
                if(opening||boundary){using(var bold=new Font(list.Font,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,e.SubItem.Text,bold,rect,ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);return;}}
            TextRenderer.DrawText(e.Graphics,e.SubItem.Text,list.Font,rect,ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        }
        private static readonly ConditionalWeakTable<Control,object> hooked=new ConditionalWeakTable<Control,object>();
        [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)] private static extern int SetWindowTheme(IntPtr window,string app,string id);
        private static void ScrollTheme(Control control){if(!control.IsHandleCreated)return;try{SetWindowTheme(control.Handle,Dark?"DarkMode_Explorer":"Explorer",null);}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}}
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
        private static void TitleBar(Form form){if(!form.IsHandleCreated)return;try{int value=Dark?1:0;DwmSetWindowAttribute(form.Handle,20,ref value,4);int color=ColorTranslator.ToWin32(Background);DwmSetWindowAttribute(form.Handle,35,ref color,4);color=ColorTranslator.ToWin32(Ink);DwmSetWindowAttribute(form.Handle,36,ref color,4);color=ColorTranslator.ToWin32(Border);DwmSetWindowAttribute(form.Handle,34,ref color,4);int corners=2;DwmSetWindowAttribute(form.Handle,33,ref corners,4);}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}}
        public static void Apply(Control control)
        {
            if(control is CaptureForm)return;
            control.BackColor=control is Form?Background:Surface;control.ForeColor=Ink;
            string role=control.Tag as string;
            if(role=="canvas"||control is SplitContainer||control is SplitterPanel)control.BackColor=Background;
            if(role=="toolbar")control.BackColor=control.Parent==null?Background:control.Parent.BackColor;
            if(role=="input")control.BackColor=Field;
            if(control is Label||control is CheckBox||control is RadioButton)control.BackColor=Color.Transparent;
            if(role=="muted")control.ForeColor=Muted;
            if(role=="heading")control.ForeColor=Accent;
            if(role=="secondary")control.ForeColor=Secondary;if(role=="danger")control.ForeColor=Danger;
            var button=control as Button;if(button!=null){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=0;button.FlatAppearance.BorderColor=Border;button.FlatAppearance.MouseOverBackColor=Field;button.FlatAppearance.MouseDownBackColor=Border;if(role=="primary"){button.BackColor=Primary;button.ForeColor=PrimaryInk;}if(role=="danger")button.ForeColor=Danger;}
            if(control is TextBoxBase||control is NumericUpDown||control is ComboBox||control is PictureBox)control.BackColor=Field;
            var page=control as TabPage;if(page!=null)page.UseVisualStyleBackColor=false;
            var grid=control as PropertyGrid;if(grid!=null){grid.ViewBackColor=Field;grid.ViewForeColor=Ink;grid.HelpBackColor=Surface;grid.HelpForeColor=Muted;grid.LineColor=Border;grid.CategoryForeColor=Accent;grid.CategorySplitterColor=Border;grid.CommandsBackColor=Surface;grid.CommandsForeColor=Ink;grid.SelectedItemWithFocusBackColor=Primary;grid.SelectedItemWithFocusForeColor=PrimaryInk;}
            var strip=control as ToolStrip;if(strip!=null){strip.Renderer=new ToolStripProfessionalRenderer(new ThemeColors());ApplyItems(strip.Items);}
            object marker;if(!hooked.TryGetValue(control,out marker)){
                hooked.Add(control,new object());
                if(control is ListView||control is ListBox||control is ScrollableControl)control.HandleCreated+=delegate{ScrollTheme(control);};
                var form=control as Form;if(form!=null)form.HandleCreated+=delegate{TitleBar(form);};
                var tabs=control as TabControl;if(tabs!=null){tabs.DrawMode=TabDrawMode.OwnerDrawFixed;tabs.DrawItem+=delegate(object sender,DrawItemEventArgs e){bool selected=e.Index==tabs.SelectedIndex;using(var bg=new SolidBrush(selected?Surface:Background))e.Graphics.FillRectangle(bg,e.Bounds);TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,tabs.Font,e.Bounds,selected?Accent:Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);};}
                var list=control as ListView;if(list!=null){list.OwnerDraw=true;list.DrawColumnHeader+=delegate(object sender,DrawListViewColumnHeaderEventArgs e){using(var bg=new SolidBrush(Field))e.Graphics.FillRectangle(bg,e.Bounds);TextRenderer.DrawText(e.Graphics,e.Header.Text,list.Font,Rectangle.Inflate(e.Bounds,-6,0),Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);};list.DrawItem+=delegate(object sender,DrawListViewItemEventArgs e){if(list.View==View.Details)return;using(var bg=new SolidBrush(e.Item.Selected?Selected:(e.Item.Tag is StepRow&&((StepRow)e.Item.Tag).InSelectedBlock?SoftGeometry.Blend(e.Item.BackColor,Accent,0.055f):e.Item.BackColor)))e.Graphics.FillRectangle(bg,e.Bounds);};list.DrawSubItem+=delegate(object sender,DrawListViewSubItemEventArgs e){using(var bg=new SolidBrush(e.Item.Selected?Selected:(e.Item.Tag is StepRow&&((StepRow)e.Item.Tag).InSelectedBlock?SoftGeometry.Blend(e.Item.BackColor,Accent,0.055f):e.Item.BackColor)))e.Graphics.FillRectangle(bg,e.Bounds);using(var line=new Pen(SoftGeometry.Blend(Surface,Border,0.16f)))e.Graphics.DrawLine(line,e.Bounds.Left,e.Bounds.Bottom-1,e.Bounds.Right,e.Bounds.Bottom-1);DrawStepCell(e,list);};}
            }
            if(control is ListView||control is ListBox||control is ScrollableControl)ScrollTheme(control);
            foreach(Control child in control.Controls)Apply(child);
            var ownedForm=control as Form;if(ownedForm!=null)TitleBar(ownedForm);control.Invalidate();
        }
        private static void ApplyItems(ToolStripItemCollection items){foreach(ToolStripItem item in items){item.ForeColor=Ink;item.BackColor=Surface;var drop=item as ToolStripDropDownItem;if(drop!=null){drop.DropDown.BackColor=Surface;drop.DropDown.ForeColor=Ink;drop.DropDown.Renderer=new ToolStripProfessionalRenderer(new ThemeColors());ApplyItems(drop.DropDownItems);}}}
        private sealed class ThemeColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground{get{return Surface;}}public override Color ImageMarginGradientBegin{get{return Surface;}}public override Color ImageMarginGradientMiddle{get{return Surface;}}public override Color ImageMarginGradientEnd{get{return Surface;}}public override Color MenuItemSelected{get{return Field;}}public override Color MenuItemBorder{get{return Border;}}public override Color MenuBorder{get{return Border;}}public override Color SeparatorDark{get{return Border;}}
            public override Color MenuItemSelectedGradientBegin{get{return Field;}}public override Color MenuItemSelectedGradientEnd{get{return Field;}}public override Color MenuItemPressedGradientBegin{get{return Border;}}public override Color MenuItemPressedGradientMiddle{get{return Border;}}public override Color MenuItemPressedGradientEnd{get{return Border;}}public override Color ToolStripGradientBegin{get{return Surface;}}public override Color ToolStripGradientMiddle{get{return Surface;}}public override Color ToolStripGradientEnd{get{return Surface;}}
        }
    }
}
