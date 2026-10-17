using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroExecute
{
    // Opt-in acceptance test. It can send input only while its own test window
    // is foreground and only to points strictly inside that window's client area.
    internal sealed class GuardedDesktop : IImageDesktop
    {
        private readonly Form window;
        private readonly IntPtr handle;
        private readonly Rectangle bounds;
        public GuardedDesktop(Form window){this.window=window;handle=window.Handle;bounds=window.RectangleToScreen(window.ClientRectangle);}
        private void Guard(){if(Native.GetForegroundWindow()!=handle)throw new OperationCanceledException("Desktop test lost foreground focus; stopped before sending input.");}
        public Rectangle Bounds{get{return bounds;}}
        public Rectangle Foreground{get{return bounds;}}
        public Point Cursor{get{Native.Point p;Native.GetCursorPos(out p);return new Point(p.X,p.Y);}}
        public Bitmap Capture(Rectangle area){Guard();return Vision.Capture(area);}
        public void Move(Point point){Guard();if(!bounds.Contains(point))throw new InvalidOperationException("Test pointer escaped its window.");if(!Native.SetCursorPos(point.X,point.Y))throw new InvalidOperationException("Pointer move failed.");}
        public void Button(MouseButton button,bool up){if(up){Native.Mouse(button,true);return;}Guard();if(!bounds.Contains(Cursor))throw new InvalidOperationException("Test click escaped its window.");Native.Mouse(button,false);}
    }
    internal static class DesktopAcceptance
    {
        public static int Run(string report)
        {
            int result=1;string text="Desktop test did not complete.";
            using(var form=new Form{Text="MacroExecute image acceptance test",Width=620,Height=380,StartPosition=FormStartPosition.CenterScreen,BackColor=Color.White})
            using(var image=new Bitmap(28,28,PixelFormat.Format32bppArgb)){
                using(var g=Graphics.FromImage(image)){g.Clear(Color.FromArgb(31,86,163));g.FillRectangle(Brushes.Orange,3,5,8,17);g.FillEllipse(Brushes.Lime,14,9,10,10);}
                var target=new PictureBox{Image=image,Size=image.Size,Location=new Point(70,90)};form.Controls.Add(target);
                var status=new Label{Text="Checking image search and clicks in this test window…",Dock=DockStyle.Top,Height=45};form.Controls.Add(status);int clicks=0;target.Click+=delegate{clicks++;target.Location=clicks%2==0?new Point(70,90):new Point(440,240);};
                form.Shown+=async delegate{
                    using(var cancellation=new CancellationTokenSource(15000)){
                        WindowsHost host=null;
                        try{
                            Native.ShowWindowAsync(form.Handle,9);form.Activate();await Task.Delay(1000,cancellation.Token);host=new WindowsHost(form,new GuardedDesktop(form));
                            using(var captured=Vision.Capture(form.RectangleToScreen(form.ClientRectangle)))captured.Save(Path.Combine(Path.GetDirectoryName(report),"desktop-test-scene.png"),ImageFormat.Png);
                            image.Save(Path.Combine(Path.GetDirectoryName(report),"desktop-test-template.png"),ImageFormat.Png);
                            var match=new Step{Kind=ActionKind.IfImage,ImageData=ImageTemplate.Encode(image),SearchIn=SearchArea.ForegroundWindow,MoveToMatch=false,Tolerance=50};
                            var missing=match.Clone();using(var absent=new Bitmap(5,5,PixelFormat.Format32bppArgb)){using(var g=Graphics.FromImage(absent))g.Clear(Color.Magenta);missing.ImageData=ImageTemplate.Encode(absent);}
                            var macro=new Macro{Steps=new List<Step>{match,missing,new Step{Kind=ActionKind.MouseClick,X=-999,Y=-999},new Step{Kind=ActionKind.Else},new Step{Kind=ActionKind.Note},new Step{Kind=ActionKind.EndIf},new Step{Kind=ActionKind.MouseClick,Pointer=PointerMode.LastMatch},new Step{Kind=ActionKind.EndIf}}};
                            for(int i=0;i<4;i++){await new Runner(host).Run(Plan.Build(macro,AppDomain.CurrentDomain.BaseDirectory),1,1,cancellation.Token);new GuardedDesktop(form).Move(form.PointToScreen(new Point(25,65)));form.Refresh();await Task.Delay(500,cancellation.Token);using(var captured=Vision.Capture(form.RectangleToScreen(form.ClientRectangle)))captured.Save(Path.Combine(Path.GetDirectoryName(report),"desktop-stage-"+i+".png"),ImageFormat.Png);if(clicks!=i+1)throw new InvalidOperationException("Expected one click on moving target per iteration; got "+clicks);}
                            // An atomic ClickImage must follow the target to its new location too.
                            var click=match.Clone();click.Kind=ActionKind.ClickImage;await host.Execute(click,cancellation.Token);await Task.Delay(100,cancellation.Token);if(clicks!=5)throw new InvalidOperationException("Atomic image click missed.");
                            text="PASS: real screen capture and Windows input reached the moving target five times. Four nested image IF / ELSE runs restored the outer match after an inner miss; the fifth used ClickImage. Tolerance 50 accommodated the desktop pointer highlight. All input was confined to the foreground test window.";result=0;
                        }catch(Exception e){text="FAIL: "+e.ToString();}finally{if(host!=null)host.ReleaseInputs();File.WriteAllText(report,text);form.Close();}
                    }
                };Application.Run(form);
            }
            return result;
        }
    }
}
