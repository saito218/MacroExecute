using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MacroExecute
{
    // Desktop boundary makes image decisions testable without moving the user's mouse.
    public interface IImageDesktop
    {
        Rectangle Bounds { get; }
        Rectangle Foreground { get; }
        Point Cursor { get; }
        Bitmap Capture(Rectangle area);
        void Move(Point point);
        void Button(MouseButton button, bool up);
    }
    internal sealed class ImageDesktop : IImageDesktop
    {
        public Rectangle Bounds { get { return System.Windows.Forms.SystemInformation.VirtualScreen; } }
        public Rectangle Foreground { get { Native.Rect r; if(!Native.GetWindowRect(Native.GetForegroundWindow(),out r))throw new InvalidOperationException("No foreground window."); return Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom); } }
        public Point Cursor { get { Native.Point p; if(!Native.GetCursorPos(out p))throw new InvalidOperationException("Cannot read pointer.");return new Point(p.X,p.Y); } }
        public Bitmap Capture(Rectangle area) { return Vision.Capture(area); }
        public void Move(Point p) { InputPermissions.EnsureWindow(Native.WindowFromPoint(new Native.Point{X=p.X,Y=p.Y}));if(!Native.SetCursorPos(p.X,p.Y))throw new InvalidOperationException("Cannot move pointer."); }
        public void Button(MouseButton b,bool up) { if(!up)InputPermissions.EnsureForeground();Native.Mouse(b,up); }
    }
    public static class ImageTemplate
    {
        public static Bitmap Load(Step step)
        {
            byte[] bytes;
            if(!string.IsNullOrEmpty(step.ImageData)) { if(step.ImageData.Length > Storage.MaxBytes)throw new InvalidDataException("Image is too large."); bytes=Convert.FromBase64String(step.ImageData); }
            else { if(new FileInfo(step.Target).Length>Storage.MaxBytes)throw new InvalidDataException("Image is too large.");bytes=File.ReadAllBytes(step.Target); }
            using(var stream=new MemoryStream(bytes))using(var source=Image.FromStream(stream)) {
                if(source.Width>4096||source.Height>4096)throw new InvalidDataException("Template exceeds 4096 pixels per side.");
                var result=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb);
                using(var g=Graphics.FromImage(result)){g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;g.DrawImageUnscaled(source,0,0);} return result;
            }
        }
        public static string Encode(Bitmap image) { using(var stream=new MemoryStream()){image.Save(stream,ImageFormat.Png);return Convert.ToBase64String(stream.ToArray());} }
        public static void ValidateReferences(Step step) { using(var first=Load(step)){} foreach(string data in step.AdditionalImages)using(var image=Load(new Step{ImageData=data})){} }
        public static void Embed(Step step) { using(var image=Load(step))step.ImageData=Encode(image); }
        public static bool IsImage(ActionKind kind) { return kind==ActionKind.FindImage||kind==ActionKind.IfImage||kind==ActionKind.WaitImage||kind==ActionKind.ClickImage; }
    }
    public sealed class ImageSession
    {
        private readonly IImageDesktop desktop;
        private Random random=new Random();
        private readonly Stack<Point?> scopes=new Stack<Point?>();
        public Point? LastMatch { get; private set; }
        public Func<bool> IsPaused = delegate { return false; };
        public void SetRandomSeed(int? seed) { random=seed.HasValue?new Random(seed.Value):new Random(); }
        public ImageSession(IImageDesktop desktop) { this.desktop=desktop; }
        public void Enter() { scopes.Push(LastMatch); }
        public void Leave() { LastMatch=scopes.Pop(); }
        public void Reset() { scopes.Clear();LastMatch=null; }
        public Point Position(Step step)
        {
            Point origin=Point.Empty;
            if(step.Pointer==PointerMode.CurrentPointer)return desktop.Cursor;
            if(step.Pointer==PointerMode.LastMatch) { if(!LastMatch.HasValue)throw new InvalidOperationException("No successful image or pixel match is available in this block.");origin=LastMatch.Value; }
            else if(step.Pointer==PointerMode.CursorOffset)origin=desktop.Cursor;
            else if(step.Pointer==PointerMode.ForegroundWindow||step.Relative)origin=desktop.Foreground.Location;
            return Jitter(new Point(checked(origin.X+step.X),checked(origin.Y+step.Y)),step.PositionJitter);
        }
        public Point? SearchOnce(Step step,CancellationToken token)
        {
            LastMatch=null;token.ThrowIfCancellationRequested();
            Rectangle area=desktop.Bounds;
            if(step.SearchIn==SearchArea.ForegroundWindow)area=Rectangle.Intersect(area,desktop.Foreground);
            else if(step.SearchIn==SearchArea.Rectangle || (step.SearchIn==SearchArea.Legacy && step.Width>0&&step.Height>0))area=Rectangle.Intersect(area,new Rectangle(step.X,step.Y,step.Width,step.Height));
            if(area.Width<=0||area.Height<=0)throw new InvalidDataException("Search rectangle is empty or outside the desktop.");
            using(var scene=desktop.Capture(area)) {
                Point? p;
                if(ImageTemplate.IsImage(step.Kind)) {
                    p=null;
                    for(int index=0;index<=step.AdditionalImages.Length;index++) {
                        token.ThrowIfCancellationRequested();
                        Step reference=index==0?step:new Step{ImageData=step.AdditionalImages[index-1]};
                        Point? hit;using(var pattern=ImageTemplate.Load(reference))hit=Vision.Match(scene,pattern,Behavior.EffectiveTolerance(step),token);
                        if(hit.HasValue) { if(!p.HasValue)p=hit; if(!step.RequireAllImages)break; }
                        else if(step.RequireAllImages){p=null;break;}
                    }
                }
                else p=Vision.Pixel(scene,Storage.ParseColor(step.ColorRgb),Behavior.EffectiveTolerance(step),token);
                if(p.HasValue)LastMatch=new Point(p.Value.X+area.X,p.Value.Y+area.Y);
                return LastMatch;
            }
        }
        public async Task<Point?> Search(Step step,bool wait,CancellationToken token)
        {
            int elapsed=0;
            do {
                while(IsPaused())await Task.Delay(20,token);
                long searchStart=Stopwatch.GetTimestamp();
                Point? p=await Task.Run(delegate{return SearchOnce(step,token);},token);
                elapsed=(int)Math.Min(int.MaxValue,(long)elapsed+(Stopwatch.GetTimestamp()-searchStart)*1000/Stopwatch.Frequency);
                while(IsPaused())await Task.Delay(20,token);
                if(p.HasValue||!wait||(!step.WaitForever&&elapsed>=step.TimeoutMs))return p;
                int duration=step.WaitForever?step.PollMs:Math.Min(step.PollMs,step.TimeoutMs-elapsed);
                while(duration>0){token.ThrowIfCancellationRequested();if(IsPaused()){await Task.Delay(20,token);continue;}int slice=Math.Min(20,duration);await Task.Delay(slice,token);duration-=slice;elapsed+=slice;}
            }while(true);
        }
        private Point Jitter(Point p,int radius){return radius==0?p:new Point(checked(p.X+random.Next(-radius,radius+1)),checked(p.Y+random.Next(-radius,radius+1)));}
        public void MoveToMatch(Step step,Point p) { desktop.Move(Jitter(new Point(checked(p.X+step.MatchOffsetX),checked(p.Y+step.MatchOffsetY)),step.Kind==ActionKind.ClickImage?step.PositionJitter:0)); }
    }
}
