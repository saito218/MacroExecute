using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace MacroExecute
{
    // Original geometric artwork. A shared 32-unit grid keeps the family consistent.
    internal static class IconArt
    {
        internal static Bitmap Action(ActionKind kind,int size)
        {
            if(size<12||size>512)throw new ArgumentOutOfRangeException("size");
            using(var large=new Bitmap(size*3,size*3)){
                using(var g=Graphics.FromImage(large)){g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(large.Width/32f,large.Height/32f);DrawAction(g,kind,UiTheme.Accent,UiTheme.Ink);}
                var result=new Bitmap(size,size);using(var g=Graphics.FromImage(result)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(large,new Rectangle(0,0,size,size));}return result;
            }
        }
        internal static Bitmap ControlImage(string symbol,int size,Color color){using(var large=new Bitmap(size*3,size*3)){using(var g=Graphics.FromImage(large)){g.SmoothingMode=SmoothingMode.AntiAlias;DrawControl(g,new Rectangle(0,0,large.Width,large.Height),symbol,color);}var result=new Bitmap(size,size);using(var g=Graphics.FromImage(result)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(large,new Rectangle(0,0,size,size));}return result;}}
        private static Pen Pen(Color color){return new Pen(color,2.1f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};}
        private static void Line(Graphics g,Pen p,params float[] xy){var points=new PointF[xy.Length/2];for(int i=0;i<points.Length;i++)points[i]=new PointF(xy[i*2],xy[i*2+1]);g.DrawLines(p,points);}
        private static void Arrow(Graphics g,Pen p,float x,float y,float dx,float dy){g.DrawLine(p,x,y,x+dx,y+dy);float length=(float)Math.Sqrt(dx*dx+dy*dy);float ux=dx/length,uy=dy/length;Line(g,p,x+dx-ux*4-uy*3,y+dy-uy*4+ux*3,x+dx,y+dy,x+dx-ux*4+uy*3,y+dy-uy*4-ux*3);}
        private static void Document(Graphics g,Pen p){Line(g,p,7,28,7,4,20,4,26,10,26,28,7,28);Line(g,p,20,4,20,10,26,10);}
        private static void Clipboard(Graphics g,Pen p){g.DrawRectangle(p,7,6,19,23);g.DrawRectangle(p,12,3,9,5);}
        private static void Keyboard(Graphics g,Pen p){g.DrawRectangle(p,3,7,26,19);for(int y=12;y<=17;y+=5)for(int x=7;x<=25;x+=6)g.DrawLine(p,x,y,x+1,y);g.DrawLine(p,10,22,22,22);}
        private static void Picture(Graphics g,Pen p){g.DrawRectangle(p,3,5,25,22);g.DrawEllipse(p,19,9,4,4);Line(g,p,5,23,12,15,18,21,22,17,27,23);}
        private static void Window(Graphics g,Pen p){g.DrawRectangle(p,3,5,26,22);g.DrawLine(p,3,11,29,11);g.DrawLine(p,7,8,8,8);g.DrawLine(p,12,8,13,8);}
        private static void Clock(Graphics g,Pen p,float x=6,float y=5,float diameter=21){g.DrawEllipse(p,x,y,diameter,diameter);float c=x+diameter/2,d=y+diameter/2;Line(g,p,c,d-6,c,d,c+4,d+3);}
        private static void Cursor(Graphics g,Pen p){Line(g,p,7,3,7,25,13,20,18,29,23,26,18,18,26,17,7,3);}
        private static void Mouse(Graphics g,Pen p){using(var shape=SoftGeometry.Round(new RectangleF(7,3,18,26),8))g.DrawPath(p,shape);g.DrawLine(p,7,13,25,13);g.DrawLine(p,16,3,16,13);}
        private static void Magnify(Graphics g,Pen p){g.DrawEllipse(p,3,3,17,17);g.DrawLine(p,18,18,29,29);}
        private static void Trash(Graphics g,Pen p){Line(g,p,7,8,9,28,24,28,26,8);g.DrawLine(p,5,8,28,8);Line(g,p,12,8,12,4,21,4,21,8);g.DrawLine(p,14,13,14,23);g.DrawLine(p,20,13,20,23);}
        private static void Gear(Graphics g,Pen p){g.DrawEllipse(p,8,8,16,16);g.DrawEllipse(p,13,13,6,6);for(int i=0;i<8;i++){double a=i*Math.PI/4;g.DrawLine(p,(float)(16+9*Math.Cos(a)),(float)(16+9*Math.Sin(a)),(float)(16+13*Math.Cos(a)),(float)(16+13*Math.Sin(a)));}}
        private static void Badge(Graphics g,string symbol,Color color)
        {
            using(var fill=new SolidBrush(UiTheme.Surface))g.FillEllipse(fill,16,16,16,16);using(var p=Pen(color)){
                if(symbol=="if")g.DrawPolygon(p,new[]{new Point(24,17),new Point(31,24),new Point(24,31),new Point(17,24)});
                else if(symbol=="wait"){g.DrawEllipse(p,18,18,12,12);Line(g,p,24,20,24,24,27,26);}
                else if(symbol=="find"){g.DrawEllipse(p,18,18,8,8);g.DrawLine(p,25,25,30,30);}
                else if(symbol=="click"){Line(g,p,20,18,20,30,24,27,28,30,30,27,26,24,30,23,20,18);}
                else if(symbol=="down")Arrow(g,p,24,18,0,12);
                else if(symbol=="up")Arrow(g,p,24,30,0,-12);
                else if(symbol=="plus"){g.DrawLine(p,18,24,30,24);g.DrawLine(p,24,18,24,30);}
                else if(symbol=="clear"){g.DrawLine(p,19,19,29,29);g.DrawLine(p,29,19,19,29);}
                else if(symbol=="paste")Arrow(g,p,18,24,12,0);
                else if(symbol=="copy"){g.DrawRectangle(p,18,18,8,9);Line(g,p,27,21,30,21,30,30,22,30,22,28);}
                else if(symbol=="check")Line(g,p,18,24,22,28,30,19);
            }
        }
        internal static void DrawAction(Graphics g,ActionKind kind,Color color,Color detail)
        {
            using(var p=Pen(color)){
                switch(kind){
                    case ActionKind.Note:Document(g,p);g.DrawLine(p,11,14,21,14);g.DrawLine(p,11,19,21,19);g.DrawLine(p,11,24,17,24);break;
                    case ActionKind.Delay:Clock(g,p);break;
                    case ActionKind.KeyDown:Keyboard(g,p);Badge(g,"down",detail);break;
                    case ActionKind.KeyUp:Keyboard(g,p);Badge(g,"up",detail);break;
                    case ActionKind.KeyPress:Keyboard(g,p);Badge(g,"check",detail);break;
                    case ActionKind.MouseMove:Cursor(g,p);Arrow(g,p,20,7,9,0);g.DrawLine(p,26,4,29,7);break;
                    case ActionKind.MouseDown:Mouse(g,p);Badge(g,"down",detail);break;
                    case ActionKind.MouseUp:Mouse(g,p);Badge(g,"up",detail);break;
                    case ActionKind.MouseClick:Mouse(g,p);using(var brush=new SolidBrush(color))g.FillRectangle(brush,9,5,5,6);Line(g,p,2,6,4,8);Line(g,p,3,1,5,4);break;
                    case ActionKind.MouseWheel:Mouse(g,p);g.DrawLine(p,16,6,16,10);Arrow(g,p,29,22,0,-14);break;
                    case ActionKind.TypeText:Keyboard(g,p);using(var f=new Font("Segoe UI",12,FontStyle.Bold,GraphicsUnit.Pixel))using(var b=new SolidBrush(detail))g.DrawString("A",f,b,18,17);break;
                    case ActionKind.SetClipboard:Clipboard(g,p);g.DrawLine(p,11,13,21,13);g.DrawLine(p,11,18,19,18);Badge(g,"down",detail);break;
                    case ActionKind.TypeDate:g.DrawRectangle(p,4,7,24,22);g.DrawLine(p,4,13,28,13);g.DrawLine(p,10,3,10,10);g.DrawLine(p,22,3,22,10);g.DrawRectangle(p,10,18,4,4);g.DrawRectangle(p,19,18,4,4);break;
                    case ActionKind.TypeEnvironment:g.DrawRectangle(p,3,5,26,22);Line(g,p,10,10,6,16,10,22);Line(g,p,22,10,26,16,22,22);g.DrawLine(p,18,10,14,22);break;
                    case ActionKind.Launch:Window(g,p);g.DrawPolygon(p,new[]{new Point(12,15),new Point(12,24),new Point(22,19)});break;
                    case ActionKind.OpenUrl:g.DrawEllipse(p,3,3,26,26);g.DrawEllipse(p,10,3,12,26);g.DrawLine(p,3,16,29,16);g.DrawArc(p,4,5,24,12,0,180);break;
                    case ActionKind.ActivateWindow:Window(g,p);Line(g,p,9,17,14,22,23,14);break;
                    case ActionKind.MoveWindow:Window(g,p);Arrow(g,p,10,19,14,0);Arrow(g,p,17,25,0,-12);break;
                    case ActionKind.WaitWindow:Window(g,p);Badge(g,"wait",detail);break;
                    case ActionKind.FindPixel:Pixel(g,p);Badge(g,"find",detail);break;
                    case ActionKind.WaitPixel:Pixel(g,p);Badge(g,"wait",detail);break;
                    case ActionKind.FindImage:Picture(g,p);Badge(g,"find",detail);break;
                    case ActionKind.IfPixel:Pixel(g,p);Badge(g,"if",detail);break;
                    case ActionKind.IfImage:Picture(g,p);Badge(g,"if",detail);break;
                    case ActionKind.IfClipboard:Clipboard(g,p);Badge(g,"if",detail);break;
                    case ActionKind.IfWindow:Window(g,p);Badge(g,"if",detail);break;
                    case ActionKind.Else:Line(g,p,8,3,8,15,25,15,25,27);Arrow(g,p,25,20,0,7);g.DrawLine(p,8,15,8,28);break;
                    case ActionKind.EndIf:Line(g,p,5,4,5,14,16,22,27,14,27,4);g.DrawLine(p,16,22,16,29);g.DrawLine(p,10,29,22,29);break;
                    case ActionKind.Repeat:Loop(g,p);break;
                    case ActionKind.EndRepeat:Loop(g,p);Badge(g,"check",detail);break;
                    case ActionKind.Break:Line(g,p,5,6,5,25,16,25);Arrow(g,p,13,12,15,0);g.DrawLine(p,27,5,27,20);break;
                    case ActionKind.Label:Line(g,p,6,29,6,3,26,3,21,10,26,17,6,17);break;
                    case ActionKind.Jump:Line(g,p,4,27,4,17,14,17,14,8,27,8);Arrow(g,p,20,8,7,0);g.DrawLine(p,28,3,28,13);break;
                    case ActionKind.Include:Document(g,p);Arrow(g,p,2,18,15,0);break;
                    case ActionKind.Message:Line(g,p,3,4,29,4,29,22,15,22,7,29,7,22,3,22,3,4);g.DrawLine(p,8,10,24,10);g.DrawLine(p,8,16,20,16);break;
                    case ActionKind.Pause:g.DrawRectangle(p,7,5,6,23);g.DrawRectangle(p,20,5,6,23);break;
                    case ActionKind.CopyFile:Document(g,p);Badge(g,"copy",detail);break;
                    case ActionKind.DeleteFile:Document(g,p);Badge(g,"clear",detail);break;
                    case ActionKind.IfFile:Document(g,p);Badge(g,"if",detail);break;
                    case ActionKind.IfProcess:Gear(g,p);Badge(g,"if",detail);break;
                    case ActionKind.WaitProcess:Gear(g,p);Badge(g,"wait",detail);break;
                    case ActionKind.ClipboardIncrement:Clipboard(g,p);Badge(g,"plus",detail);break;
                    case ActionKind.ClipboardClear:Clipboard(g,p);Badge(g,"clear",detail);break;
                    case ActionKind.Paste:Clipboard(g,p);Badge(g,"paste",detail);break;
                    case ActionKind.RunCSharp:Line(g,p,11,6,3,16,11,26);Line(g,p,21,6,29,16,21,26);g.DrawLine(p,18,5,14,27);break;
                    case ActionKind.WaitImage:Picture(g,p);Badge(g,"wait",detail);break;
                    case ActionKind.ClickImage:Picture(g,p);Badge(g,"click",detail);break;
                    case ActionKind.CopySelection:Line(g,p,3,3,3,19,19,19,19,3,3,3);g.DrawLine(p,7,8,15,8);g.DrawLine(p,7,13,13,13);Badge(g,"copy",detail);break;
                    default:throw new ArgumentOutOfRangeException("kind");
                }
            }
        }
        private static void Pixel(Graphics g,Pen p){g.DrawRectangle(p,3,4,9,9);g.DrawRectangle(p,15,4,9,9);g.DrawRectangle(p,3,16,9,9);using(var b=new SolidBrush(p.Color))g.FillRectangle(b,16,17,8,8);}
        private static void Loop(Graphics g,Pen p){Line(g,p,4,13,4,7,26,7);Arrow(g,p,21,7,5,0);Line(g,p,28,19,28,25,6,25);Arrow(g,p,11,25,-5,0);}
        internal static string ControlSymbol(string text,string name)
        {
            string key=(name??text??"").ToLowerInvariant();string label=Label(text).ToLowerInvariant();
            if(key.Contains("minimize window"))return "minimize";if(key.Contains("maximize window"))return "maximize";if(key.Contains("restore window"))return "restore";if(key.Contains("close window")||label=="×")return "close";
            if(key.Contains("example scenario"))return null;
            if(key.Contains("undo"))return "undo";if(key.Contains("redo"))return "redo";
            if(label.Contains("record"))return "record";if(label.Contains("resume")||label.Contains("play")||label.Contains("from selected")||label.Contains("selected block"))return "play";if(label.Contains("pause"))return "pause";if(label.Contains("stop"))return "stop";
            if(key.StartsWith("help:")||label=="?"||label.Contains("how to use")||label.Contains("guide"))return "help";
            if(label.StartsWith("file"))return "folder";if(label.StartsWith("edit"))return "edit";if(label.StartsWith("view"))return "view";if(label.StartsWith("settings")||label.Contains("preferences"))return "settings";if(label.StartsWith("help"))return "help";
            if(label.Contains("shortcut"))return "keyboard";if(label.Contains("randomization"))return "shuffle";if(label.Contains("permissions"))return "shield";if(label.Contains("schedule"))return "calendar";if(label.Contains("new workflow")||label=="new")return "new";if(label.Contains("select all"))return "select";if(label.Contains("save")||label.Contains("export"))return "save";if(label.StartsWith("open")||label.StartsWith("browse"))return "folder";
            if(label.Contains("capture"))return "capture";if(label.Contains("import")||label.Contains("add images"))return "import";if(label.Contains("test find")||label.Contains("find step")||label.Contains("search"))return "find";
            if(label.Contains("copy")||label.Contains("duplicate"))return "copy";if(label.Contains("cut steps"))return "cut";if(label.Contains("paste steps"))return "paste";
            if(label.Contains("delete")||label=="remove")return "delete";if(label.StartsWith("enable")||label.Contains("check macro")||label=="ok")return "check";if(label.StartsWith("disable"))return "disable";
            if(label=="up"||label.Contains("↑ up")||key.Contains("previous matching"))return "up";if(label=="down"||label.Contains("↓ down")||key.Contains("next matching"))return "down";
            if(label=="cancel"||label.StartsWith("clear"))return "close";if(label.StartsWith("back")||label.Contains("parent block"))return "back";
            if(label.Contains("select search area"))return "capture";return null;
        }
        internal static string Label(string text){return (text??"").TrimStart('●','▶','Ⅱ','■','✓','↶','↷','↑','↓','▸','▾','←',' ');}
        internal static void DrawControl(Graphics g,Rectangle bounds,string symbol,Color color)
        {
            var state=g.Save();g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/32f,bounds.Height/32f);using(var p=Pen(color)){
                switch(symbol){
                    case "undo":Line(g,p,12,4,4,11,12,18);g.DrawArc(p,6,9,22,20,200,245);break;
                    case "redo":Line(g,p,20,4,28,11,20,18);g.DrawArc(p,4,9,22,20,95,245);break;
                    case "record":using(var b=new SolidBrush(color))g.FillEllipse(b,6,6,20,20);break;
                    case "play":using(var b=new SolidBrush(color))g.FillPolygon(b,new[]{new Point(9,4),new Point(27,16),new Point(9,28)});break;
                    case "pause":g.DrawRectangle(p,6,5,6,23);g.DrawRectangle(p,20,5,6,23);break;
                    case "stop":using(var b=new SolidBrush(color))g.FillRectangle(b,6,6,20,20);break;
                    case "folder":Line(g,p,3,27,3,7,13,7,16,11,29,11,29,27,3,27);g.DrawLine(p,4,15,28,15);break;
                    case "save":Line(g,p,5,3,24,3,29,8,29,29,4,29,4,3);g.DrawRectangle(p,10,3,12,9);g.DrawRectangle(p,10,19,13,10);break;
                    case "edit":Line(g,p,5,21,22,4,28,10,11,27,4,29,5,21);g.DrawLine(p,18,8,24,14);break;
                    case "view":g.DrawEllipse(p,3,8,26,17);g.DrawEllipse(p,12,12,8,8);break;
                    case "settings":Gear(g,p);break;
                    case "help":g.DrawEllipse(p,3,3,26,26);g.DrawArc(p,11,8,10,9,190,270);Line(g,p,16,17,16,21);g.DrawEllipse(p,15.5f,25,1,1);break;
                    case "find":Magnify(g,p);break;
                    case "copy":g.DrawRectangle(p,4,4,17,20);Line(g,p,23,9,28,9,28,29,11,29,11,26);break;
                    case "cut":g.DrawEllipse(p,3,21,8,8);g.DrawEllipse(p,21,21,8,8);g.DrawLine(p,9,23,27,3);g.DrawLine(p,23,23,5,3);break;
                    case "paste":Clipboard(g,p);Badge(g,"paste",color);break;
                    case "delete":Trash(g,p);break;
                    case "check":Line(g,p,4,17,12,25,28,7);break;
                    case "disable":g.DrawEllipse(p,4,4,24,24);g.DrawLine(p,7,25,25,7);break;
                    case "up":Arrow(g,p,16,28,0,-24);break;
                    case "down":Arrow(g,p,16,4,0,24);break;
                    case "back":Arrow(g,p,28,16,-24,0);break;
                    case "close":g.DrawLine(p,7,7,25,25);g.DrawLine(p,25,7,7,25);break;
                    case "minimize":g.DrawLine(p,6,23,26,23);break;
                    case "maximize":g.DrawRectangle(p,5,5,22,22);break;
                    case "restore":g.DrawRectangle(p,4,10,18,18);Line(g,p,10,7,10,4,28,4,28,22,25,22);break;
                    case "capture":Line(g,p,3,11,3,3,11,3);Line(g,p,21,3,29,3,29,11);Line(g,p,3,21,3,29,11,29);Line(g,p,21,29,29,29,29,21);g.DrawLine(p,16,9,16,23);g.DrawLine(p,9,16,23,16);break;
                    case "import":Picture(g,p);Badge(g,"plus",color);break;
                    case "keyboard":Keyboard(g,p);break;case "shortcut":g.DrawRectangle(p,3,4,12,12);g.DrawRectangle(p,17,17,12,12);g.DrawLine(p,20,7,28,7);g.DrawLine(p,24,3,24,11);Line(g,p,7,20,7,25,12,25);break;
                    case "shuffle":Line(g,p,3,6,9,6,22,26,29,26);Line(g,p,3,26,9,26,22,6,29,6);Arrow(g,p,23,6,6,0);Arrow(g,p,23,26,6,0);break;
                    case "shield":Line(g,p,16,3,28,8,26,23,16,30,6,23,4,8,16,3);Line(g,p,10,16,14,20,22,11);break;
                    case "calendar":g.DrawRectangle(p,4,7,24,22);g.DrawLine(p,4,13,28,13);g.DrawLine(p,10,3,10,10);g.DrawLine(p,22,3,22,10);g.DrawRectangle(p,11,19,5,5);break;
                    case "new":Document(g,p);Badge(g,"plus",color);break;
                    case "select":g.DrawRectangle(p,3,3,26,26);Line(g,p,8,16,14,22,24,9);break;
                    case "sun":using(var fill=new SolidBrush(color))g.FillEllipse(fill,11,11,10,10);for(int i=0;i<8;i++){double a=i*Math.PI/4;g.DrawLine(p,(float)(16+10*Math.Cos(a)),(float)(16+10*Math.Sin(a)),(float)(16+14*Math.Cos(a)),(float)(16+14*Math.Sin(a)));}break;
                    case "moon":using(var crescent=new GraphicsPath()){crescent.AddBezier(25,4,10,-1,0,13,7,25);crescent.AddBezier(7,25,14,34,27,30,29,21);crescent.AddBezier(29,21,17,25,10,12,25,4);crescent.CloseFigure();using(var fill=new SolidBrush(color))g.FillPath(fill,crescent);}break;
                }
            }g.Restore(state);
        }
    }
}
