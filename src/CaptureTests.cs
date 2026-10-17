using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;

namespace MacroExecute
{
    internal static class CaptureTests
    {
        private static void Check(bool condition,string message) { if(!condition)throw new Exception(message); }
        public static void Run(Action<string,Action> test)
        {
            var previousContext=SynchronizationContext.Current;
            try{
                test("Drag selection normalizes reverse direction and negative desktop origin",delegate{
                    var selection=new ScreenSelection(new Rectangle(-1920,-200,3840,1200));selection.Drag(new Point(300,250),new Point(100,50));
                    Check(selection.Pixels==new Rectangle(100,50,200,200),"Reversed drag is wrong.");Check(selection.ScreenBounds==new Rectangle(-1820,-150,200,200),"Desktop origin was lost.");
                });
                test("Drag selection clips to captured desktop",delegate{
                    var selection=new ScreenSelection(new Rectangle(-100,20,200,100));selection.Drag(new Point(-20,-30),new Point(300,130));Check(selection.Pixels==new Rectangle(0,0,200,100),"Drag escaped screenshot bounds.");
                });
                test("Zero-size drag cannot capture or overwrite search area",delegate{
                    var selection=new ScreenSelection(new Rectangle(0,0,20,20));selection.Drag(new Point(3,3),new Point(3,10));var step=new Step{X=77};
                    try{selection.ApplySearchArea(step);throw new Exception("Empty area accepted.");}catch(InvalidOperationException){}Check(step.X==77,"Rejected selection changed the step.");
                    using(var image=new Bitmap(20,20))try{using(var crop=selection.Crop(image)){}throw new Exception("Empty image accepted.");}catch(InvalidOperationException){}
                });
                test("Headless reference capture survives completed dialog selection and embeds exact crop",delegate{
                    using(var source=new Bitmap(20,12,PixelFormat.Format32bppArgb)){
                        source.SetPixel(4,3,Color.Red);source.SetPixel(9,7,Color.Lime);
                        using(var capture=new CaptureForm(new Rectangle(-300,40,20,12),source,false)){
                            capture.CompleteSelection(new Point(10,8),new Point(4,3));Check(capture.DialogResult==DialogResult.OK&&!capture.IsDisposed,"Selection ended the overlay's bitmap lifetime.");
                            using(var cropped=capture.SelectedBitmap())using(var decoded=ImageTemplate.Load(new Step{ImageData=ImageTemplate.Encode(cropped)})){
                                Check(decoded.Size==new Size(6,5)&&decoded.GetPixel(0,0).ToArgb()==Color.Red.ToArgb()&&decoded.GetPixel(5,4).ToArgb()==Color.Lime.ToArgb(),"Reference crop pixels changed.");
                            }
                            Check(!capture.Visible&&!capture.IsHandleCreated,"Headless test opened a window.");
                        }
                    }
                });
                test("Headless search-area capture stores physical coordinates without replacing reference",delegate{
                    using(var source=new Bitmap(20,12))using(var capture=new CaptureForm(new Rectangle(-300,40,20,12),source,true)){
                        var step=new Step{Kind=ActionKind.IfImage,ImageData="existing reference",Tolerance=17};capture.CompleteSelection(new Point(2,3),new Point(15,10));capture.ApplySearchArea(step);
                        Check(step.SearchIn==SearchArea.Rectangle&&step.X==-298&&step.Y==43&&step.Width==13&&step.Height==7,"Search area mapping failed.");Check(step.ImageData=="existing reference"&&step.Tolerance==17,"Area capture replaced other settings.");
                        var roundtrip=Storage.Decode(Storage.Encode(new Macro{Steps=new System.Collections.Generic.List<Step>{step}})).Steps[0];Check(roundtrip.X==-298&&roundtrip.Height==7,"Saved rectangle changed.");
                    }
                });
                test("Headless overlay cancel leaves editor values untouched",delegate{
                    using(var image=new Bitmap(20,12))using(var capture=new CaptureForm(new Rectangle(0,0,20,12),image,true)){
                        capture.CompleteSelection(new Point(4,4),new Point(4,4));Check(capture.DialogResult==DialogResult.None,"Zero drag accepted.");capture.DialogResult=DialogResult.Cancel;Check(!capture.IsDisposed&&!capture.Visible,"Cancel disposed or showed overlay unexpectedly.");
                    }
                });
                test("Capture transparency preserves editor session and restores owner opacity",delegate{
                    using(var owner=new Form{Opacity=0.8})using(var editor=new StepEditor(new Step{Kind=ActionKind.IfImage},AppDomain.CurrentDomain.BaseDirectory)){
                        editor.Owner=owner;editor.Opacity=0.9;int changes=0;editor.VisibleChanged+=delegate{changes++;};
                        using(new CaptureVisibility(editor)){Check(editor.Opacity==0&&owner.Opacity==0,"Editor covered the capture.");Check(editor.DialogResult==DialogResult.None&&!editor.IsDisposed,"Modal editor session ended.");}
                        Check(Math.Abs(editor.Opacity-0.9)<0.001&&Math.Abs(owner.Opacity-0.8)<0.001&&changes==0,"Visibility changed or opacity not restored.");Check(!editor.Visible&&!owner.Visible&&!editor.IsHandleCreated&&!owner.IsHandleCreated,"Test showed a window.");
                    }
                });
                test("Capture failure restores transparency without disposing editor",delegate{
                    using(var editor=new StepEditor(new Step{Kind=ActionKind.IfImage},AppDomain.CurrentDomain.BaseDirectory)){
                        try{using(new CaptureVisibility(editor)){throw new InvalidOperationException("Synthetic screenshot failure.");}}catch(InvalidOperationException){}
                        Check(editor.Opacity==1&&editor.DialogResult==DialogResult.None&&!editor.IsDisposed&&!editor.Visible,"Failure damaged editor lifetime.");
                    }
                });
            }finally{SynchronizationContext.SetSynchronizationContext(previousContext);}
        }
    }
}
