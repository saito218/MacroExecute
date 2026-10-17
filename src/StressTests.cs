using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace MacroExecute
{
    internal static class StressTests
    {
        private static void Check(bool value,string message) { if(!value)throw new Exception(message); }
        public static void Run(Action<string,Action> test,string directory)
        {
            foreach(int missing in new[]{-1,0,32,63}) {
                int failure=missing;
                test("64 actual image IF scopes restore match after missing level "+failure,delegate {
                    using(var desktop=new TestDesktop())using(var pattern=new Bitmap(2,2)) {
                        using(var graphics=Graphics.FromImage(pattern))graphics.Clear(Color.Black);
                        pattern.SetPixel(0,0,Color.Red);pattern.SetPixel(1,1,Color.Lime);
                        string image=ImageTemplate.Encode(pattern);var steps=new List<Step>();
                        for(int i=0;i<64;i++) {
                            var condition=new Step{Kind=ActionKind.IfImage,ImageData=image,MoveToMatch=false};
                            if(i==failure) { condition.SearchIn=SearchArea.Rectangle;condition.X=-20;condition.Y=10;condition.Width=2;condition.Height=2; }
                            steps.Add(condition);
                        }
                        steps.Add(new Step{Kind=ActionKind.MouseClick,Pointer=PointerMode.LastMatch});
                        for(int i=63;i>=0;i--) {
                            steps.Add(new Step{Kind=ActionKind.Else});steps.Add(new Step{Kind=ActionKind.Note,Text="Missing image at level "+i});steps.Add(new Step{Kind=ActionKind.EndIf});
                            if(i>0)steps.Add(new Step{Kind=ActionKind.MouseClick,Pointer=PointerMode.LastMatch});
                        }
                        var host=new WindowsHost(null,desktop);
                        new Runner(host).Run(Plan.Build(new Macro{Steps=steps},directory),1,1,CancellationToken.None).GetAwaiter().GetResult();
                        int expected=failure<0?64:failure;
                        Check(desktop.Clicks.Count==expected,"Wrong branch or scoped image result: "+desktop.Clicks.Count+" expected "+expected);
                        Check(desktop.Clicks.All(p=>p==new Point(-9,17)),"A click used a stale/wrong coordinate");
                        Check(desktop.Captures==(failure<0?64:failure+1),"False ancestor did not short-circuit image capture");
                        Check(!host.Images.LastMatch.HasValue,"Last match leaked after playback");
                    }
                });
            }
        }
    }
}
