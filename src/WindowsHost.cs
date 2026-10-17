using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroExecute
{
    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)] public struct Point { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] public struct MouseInput { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] public struct KeyInput { public ushort Key, Scan; public uint Flags, Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Explicit)] public struct InputUnion { [FieldOffset(0)] public MouseInput Mouse; [FieldOffset(0)] public KeyInput Key; }
        [StructLayout(LayoutKind.Sequential)] public struct Input { public uint Type; public InputUnion Value; }
        [StructLayout(LayoutKind.Sequential)] public struct KeyHookData { public uint Key, Scan, Flags, Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] public struct MouseHookData { public Point Position; public uint Data, Flags, Time; public UIntPtr Extra; }
        public delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
        public delegate bool EnumProc(IntPtr window, IntPtr data);
        [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint count, Input[] inputs, int size);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr window, int command);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr data);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
        [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Point point);
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr window, int id);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        public static void Send(Input input) { if (SendInput(1, new[] { input }, Marshal.SizeOf(typeof(Input))) != 1) throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows rejected input. Check the target application's privilege level."); }
        public static void Key(int key, bool up, bool extended) { Send(new Input { Type = 1, Value = new InputUnion { Key = new KeyInput { Key = (ushort)key, Flags = (up ? 2u : 0u) | (extended ? 1u : 0u) } } }); }
        public static void Unicode(char c) { var pair=new[]{new Input{Type=1,Value=new InputUnion{Key=new KeyInput{Scan=c,Flags=4}}},new Input{Type=1,Value=new InputUnion{Key=new KeyInput{Scan=c,Flags=6}}}};uint sent=SendInput(2,pair,Marshal.SizeOf(typeof(Input)));if(sent!=2){if(sent==1)Send(pair[1]);throw new Win32Exception(Marshal.GetLastWin32Error(),"Windows rejected Unicode input. Check target permissions and text-entry method.");} }
        public static void Mouse(MouseButton button, bool up) { uint flag = button == MouseButton.Left ? (up ? 4u : 2u) : button == MouseButton.Right ? (up ? 16u : 8u) : (up ? 64u : 32u); Send(new Input { Type = 0, Value = new InputUnion { Mouse = new MouseInput { Flags = flag } } }); }
        public static void Wheel(int delta) { Send(new Input { Type = 0, Value = new InputUnion { Mouse = new MouseInput { Flags = 0x800, Data = unchecked((uint)delta) } } }); }
        public static IntPtr FindWindow(string fragment)
        {
            if (string.IsNullOrWhiteSpace(fragment)) throw new InvalidDataException("Enter a window-title fragment.");
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate(IntPtr w, IntPtr unused) { if (!IsWindowVisible(w)) return true; var b = new StringBuilder(1024); GetWindowText(w, b, b.Capacity); if (b.ToString().IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0) return true; found = w; return false; }, IntPtr.Zero);
            return found;
        }
    }

    public static class Vision
    {
        public static byte[] Pixels(Bitmap bitmap)
        {
            BitmapData data = null;
            try { data = bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb); var bytes = new byte[checked(bitmap.Width * bitmap.Height * 4)]; for (int y = 0; y < bitmap.Height; y++) Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), bytes, y * bitmap.Width * 4, bitmap.Width * 4); return bytes; }
            finally { if (data != null) bitmap.UnlockBits(data); }
        }
        public static Point? Match(Bitmap scene, Bitmap template, int tolerance, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (tolerance < 0 || tolerance > 255) throw new ArgumentOutOfRangeException("tolerance");
            if (template.Width > scene.Width || template.Height > scene.Height) return null;
            byte[] source = Pixels(scene), pattern = Pixels(template);
            bool visible=false; for(int n=3;n<pattern.Length;n+=4)if(pattern[n]!=0){visible=true;break;} if(!visible)throw new InvalidDataException("An entirely transparent template cannot be matched.");
            for (int y = 0; y <= scene.Height - template.Height; y++) {
                token.ThrowIfCancellationRequested();
                for (int x = 0; x <= scene.Width - template.Width; x++) {
                    if ((x & 63) == 0) token.ThrowIfCancellationRequested();
                    bool match = true;
                    for (int ty = 0; ty < template.Height && match; ty++) {
                        if ((ty & 31) == 0) token.ThrowIfCancellationRequested();
                        for (int tx = 0; tx < template.Width && match; tx++) {
                            int a = ((y + ty) * scene.Width + x + tx) * 4, b = (ty * template.Width + tx) * 4;
                            if(pattern[b+3]==0)continue;
                            for (int c = 0; c < 3; c++) if (Math.Abs(source[a+c] - pattern[b+c]) > tolerance) { match = false; break; }
                        }
                    }
                    if (match) return new Point(x + template.Width / 2, y + template.Height / 2);
                }
            }
            return null;
        }
        public static Point? Pixel(Bitmap scene, Color color, int tolerance, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();if(tolerance<0||tolerance>255)throw new ArgumentOutOfRangeException("tolerance");byte[] pixels = Pixels(scene);
            for (int y = 0; y < scene.Height; y++) { token.ThrowIfCancellationRequested(); for (int x = 0; x < scene.Width; x++) { if((x&255)==0)token.ThrowIfCancellationRequested();int n = (y * scene.Width + x) * 4; if (Math.Abs(pixels[n]-color.B) <= tolerance && Math.Abs(pixels[n+1]-color.G) <= tolerance && Math.Abs(pixels[n+2]-color.R) <= tolerance) return new Point(x,y); } }
            return null;
        }
        public static Rectangle Region(Step step) { Rectangle desktop = SystemInformation.VirtualScreen; var area = step.Width > 0 && step.Height > 0 ? Rectangle.Intersect(desktop, new Rectangle(step.X,step.Y,step.Width,step.Height)) : desktop; if (area.IsEmpty) throw new InvalidDataException("Search rectangle is outside the desktop."); return area; }
        public static Bitmap Capture(Rectangle rect) { var bitmap = new Bitmap(rect.Width,rect.Height,PixelFormat.Format32bppArgb); try { using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(rect.Location,Point.Empty,rect.Size); return bitmap; } catch { bitmap.Dispose(); throw; } }
        public static Point? Search(Step step, bool image, CancellationToken token)
        {
            Rectangle rect = Region(step);
            using (Bitmap scene = Capture(rect)) {
                Point? result;
                if (image) {
                    if (new FileInfo(step.Target).Length > Storage.MaxBytes) throw new InvalidDataException("Image exceeds 16 MB.");
                    using (var source = Image.FromFile(step.Target)) {
                        if (source.Width > 4096 || source.Height > 4096) throw new InvalidDataException("Template exceeds 4096 pixels per side.");
                        using (var template = new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb)) { using (var g = Graphics.FromImage(template)) g.DrawImageUnscaled(source,0,0); result = Match(scene,template,Behavior.EffectiveTolerance(step),token); }
                    }
                } else result = Pixel(scene,Storage.ParseColor(step.ColorRgb),Behavior.EffectiveTolerance(step),token);
                return result.HasValue ? new Point(result.Value.X + rect.X,result.Value.Y + rect.Y) : (Point?)null;
            }
        }
    }

    public sealed partial class WindowsHost : IHost, IScopedHost
    {
        private readonly Control ui;
        private readonly IKeyboardDesktop keyboard;
        private readonly ITextClipboard textClipboard;
        public Func<int,CancellationToken,Task> Wait=delegate(int ms,CancellationToken token){return Task.Delay(ms,token);};
        private readonly HashSet<int> keys = new HashSet<int>();
        private readonly HashSet<int> extendedKeys = new HashSet<int>();
        private readonly HashSet<MouseButton> buttons = new HashSet<MouseButton>();
        private readonly IImageDesktop desktop;
        public readonly ImageSession Images;
        public WindowsHost(Control ui) : this(ui,new ImageDesktop()) { }
        public WindowsHost(Control ui,IImageDesktop desktop) : this(ui,desktop,new KeyboardDesktop(),new TextClipboard(ui)) {}
        public WindowsHost(Control ui,IImageDesktop desktop,IKeyboardDesktop keyboard,ITextClipboard clipboard) { this.ui=ui;this.desktop=desktop;this.keyboard=keyboard;textClipboard=clipboard;Images=new ImageSession(desktop); }
        public void EnterCondition(){Images.Enter();} public void LeaveCondition(){Images.Leave();} public void ResetMatches(){Images.Reset();}
        private T OnUI<T>(Func<T> f) { return ui.InvokeRequired ? (T)ui.Invoke(f) : f(); }
        private void Position(Step s) { if(s.Pointer!=PointerMode.CurrentPointer)desktop.Move(Images.Position(s)); }
        public async Task Execute(Step s, CancellationToken token)
        {
            switch (s.Kind) {
                case ActionKind.KeyDown: keyboard.Key(s.KeyCode,false,s.Extended); keys.Add(s.KeyCode); if (s.Extended) extendedKeys.Add(s.KeyCode); break;
                case ActionKind.KeyUp: keyboard.Key(s.KeyCode,true,s.Extended); keys.Remove(s.KeyCode); extendedKeys.Remove(s.KeyCode); break;
                case ActionKind.KeyPress: keyboard.Key(s.KeyCode,false,s.Extended); keys.Add(s.KeyCode); if (s.Extended) extendedKeys.Add(s.KeyCode); await Wait(s.InputHoldMs,token); keyboard.Key(s.KeyCode,true,s.Extended); keys.Remove(s.KeyCode); extendedKeys.Remove(s.KeyCode); break;
                case ActionKind.MouseMove: Position(s); break;
                case ActionKind.MouseDown: Position(s); desktop.Button(s.Button,false); buttons.Add(s.Button); break;
                case ActionKind.MouseUp: Position(s); desktop.Button(s.Button,true); buttons.Remove(s.Button); break;
                case ActionKind.MouseClick: token.ThrowIfCancellationRequested();Position(s);await Click(s,token);break;
                case ActionKind.MouseWheel: Native.Wheel(s.WheelDelta); break;
                case ActionKind.TypeText: case ActionKind.TypeDate: case ActionKind.TypeEnvironment:
                    string text = s.Kind == ActionKind.TypeText ? s.Text : s.Kind == ActionKind.TypeDate ? DateTime.Now.ToString(string.IsNullOrEmpty(s.Text) ? "yyyy-MM-dd" : s.Text) : Environment.GetEnvironmentVariable(s.Target) ?? "";
                    await TextPlayback.Run(text,s,keyboard,textClipboard,Wait,token);break;
                case ActionKind.SetClipboard: textClipboard.SetText(s.Text); break;
                case ActionKind.ClipboardClear: textClipboard.SetText("");break;
                case ActionKind.ClipboardIncrement: OnUI(delegate {long number;if(!Clipboard.ContainsText()||!long.TryParse(Clipboard.GetText(),out number))throw new InvalidDataException("Clipboard does not contain an integer.");Clipboard.SetText(checked(number+s.DelayMs).ToString(System.Globalization.CultureInfo.InvariantCulture));return true;});break;
                case ActionKind.CopySelection: await ClipboardCondition.Copy(s,keyboard,textClipboard,Wait,token);break;
                case ActionKind.Paste: await TextPlayback.Paste(keyboard,s.InputHoldMs,Wait,token);break;
                case ActionKind.Launch: Process.Start(new ProcessStartInfo(s.Target,s.Text) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(s.Target) }); break;
                case ActionKind.OpenUrl: Uri uri; if (!Uri.TryCreate(s.Target,UriKind.Absolute,out uri) || (uri.Scheme != "https" && uri.Scheme != "http")) throw new InvalidDataException("Website must be an HTTP or HTTPS URL."); Process.Start(uri.AbsoluteUri); break;
                case ActionKind.ActivateWindow: case ActionKind.MoveWindow:
                    IntPtr window = Native.FindWindow(s.Text); if (window == IntPtr.Zero) throw new InvalidOperationException("Window not found: " + s.Text);
                    if (s.Kind == ActionKind.MoveWindow) { if (s.Width < 1 || s.Height < 1 || !Native.MoveWindow(window,s.X,s.Y,s.Width,s.Height,true)) throw new InvalidOperationException("Could not move window."); }
                    else { Native.ShowWindowAsync(window,9); if (!Native.SetForegroundWindow(window)) throw new InvalidOperationException("Windows denied foreground activation."); } break;
                case ActionKind.WaitWindow:
                    int waited=0; while (Native.FindWindow(s.Text) == IntPtr.Zero) { token.ThrowIfCancellationRequested(); if (waited >= s.TimeoutMs) throw new TimeoutException("Window wait timed out.");int slice=Math.Min(100,s.TimeoutMs-waited);await Wait(slice,token);waited+=slice; } break;
                case ActionKind.WaitProcess:
                    int processWaited=0;while(!ProcessExists(s.Text)){token.ThrowIfCancellationRequested();if(processWaited>=s.TimeoutMs)throw new TimeoutException("Process wait timed out.");int slice=Math.Min(100,s.TimeoutMs-processWaited);await Wait(slice,token);processWaited+=slice;}break;
                case ActionKind.FindPixel: case ActionKind.FindImage: case ActionKind.WaitPixel: case ActionKind.WaitImage: case ActionKind.ClickImage:
                    Point? result=await Images.Search(s,s.WaitForMatch||s.WaitForever||s.Kind==ActionKind.WaitPixel||s.Kind==ActionKind.WaitImage||s.Kind==ActionKind.ClickImage,token);
                    if(!result.HasValue){if(s.ContinueIfMissing)break;throw new TimeoutException("No matching image or pixel within the configured search area and timeout.");}
                    if(s.MoveToMatch||s.Kind==ActionKind.ClickImage)Images.MoveToMatch(s,result.Value);
                    if(s.Kind==ActionKind.ClickImage)await Click(s,token);break;
                case ActionKind.Message: OnUI(delegate { MessageBox.Show(ui,s.Text,"MacroExecute message",MessageBoxButtons.OK,MessageBoxIcon.Information); return true; }); break;
                case ActionKind.CopyFile: File.Copy(s.Target,s.Text,false); break;
                case ActionKind.DeleteFile: File.Delete(s.Target); break;
                case ActionKind.RunCSharp: await Scripts.Run(s.Text,s.TimeoutMs,token);break;
                default: throw new NotSupportedException("Unhandled action: " + s.Kind);
            }
        }
        public async Task<bool> Evaluate(Step s, CancellationToken token)
        {
            switch (s.Kind) {
                case ActionKind.IfWindow: return Native.FindWindow(s.Text) != IntPtr.Zero;
                case ActionKind.IfFile: return File.Exists(s.Target);
                case ActionKind.IfProcess: return ProcessExists(s.Text);
                case ActionKind.IfClipboard: token.ThrowIfCancellationRequested();return ClipboardCondition.Matches(await ClipboardCondition.Read(textClipboard,Wait,token),s);
                case ActionKind.IfPixel: case ActionKind.IfImage: var point=await Images.Search(s,s.WaitForMatch||s.WaitForever,token); if(point.HasValue && s.MoveToMatch && !s.Negate)Images.MoveToMatch(s,point.Value); return s.Negate ? !point.HasValue : point.HasValue;
                default: throw new InvalidOperationException("Not a condition.");
            }
        }
        public void ReleaseInputs() { foreach (int key in keys.ToArray()) try { keyboard.Key(key,true,extendedKeys.Contains(key)); } catch { } foreach (MouseButton button in buttons.ToArray()) try { desktop.Button(button,true); } catch { } keys.Clear(); extendedKeys.Clear(); buttons.Clear(); }
        private static bool ProcessExists(string name){if(string.IsNullOrWhiteSpace(name))throw new InvalidDataException("Enter a process name without .exe.");var processes=Process.GetProcessesByName(Path.GetFileNameWithoutExtension(name));try{return processes.Length>0;}finally{foreach(var process in processes)process.Dispose();}}
    }

    public sealed class Recorder : IDisposable
    {
        private IntPtr keyboard, mouse;
        private Native.HookProc keyboardProc, mouseProc;
        private readonly Stopwatch clock = new Stopwatch();
        private long last, lastMove;
        private readonly uint ownPid = (uint)Process.GetCurrentProcess().Id;
        public readonly List<Step> Steps = new List<Step>();
        public bool RecordMoves = true;
        public bool RelativeCoordinates;
        public Func<int,Keys,bool> IsControlShortcut=delegate(int key,Keys modifiers){return key==119||key==120||key==121;};
        public bool Active { get; private set; }
        public bool Paused { get; private set; }
        public void TogglePause() { if(!Active)return;if(!Paused){ReleaseRecordedInputs();clock.Stop();Paused=true;}else{Paused=false;clock.Start();} }
        private void ReleaseRecordedInputs(){foreach(int key in held)Steps.Add(new Step{Kind=ActionKind.KeyUp,KeyCode=key});foreach(MouseButton button in mouseHeld)Steps.Add(new Step{Kind=ActionKind.MouseUp,Button=button,Pointer=PointerMode.CurrentPointer});held.Clear();mouseHeld.Clear();}
        public event Action<string> Failed;
        private readonly HashSet<int> held = new HashSet<int>();
        private readonly HashSet<MouseButton> mouseHeld = new HashSet<MouseButton>();
        public void Start()
        {
            if (Active) return; Steps.Clear(); held.Clear(); mouseHeld.Clear(); last = lastMove = 0; keyboardProc = Keyboard; mouseProc = Mouse;
            keyboard = Native.SetWindowsHookEx(13,keyboardProc,Native.GetModuleHandle(null),0); mouse = Native.SetWindowsHookEx(14,mouseProc,Native.GetModuleHandle(null),0);
            if (keyboard == IntPtr.Zero || mouse == IntPtr.Zero) { Dispose(); throw new Win32Exception(Marshal.GetLastWin32Error(),"Could not install input hooks."); }
            Paused=false; Active = true; clock.Restart();
        }
        private bool IsOwn(IntPtr window) { uint pid; Native.GetWindowThreadProcessId(window,out pid); return pid == ownPid; }
        private void Add(Step s)
        {
            if (Steps.Count >= Storage.MaxSteps - 512) { Fail("Recording stopped at the step limit."); return; }
            long now = clock.ElapsedMilliseconds; if (now > last) Steps.Add(new Step { Kind = ActionKind.Delay, DelayMs = (int)Math.Min(now-last,86400000) }); Steps.Add(s); last = now;
        }
        private void Fail(string message) { Dispose(); var failed = Failed; if (failed != null) failed(message); }
        private static Keys CurrentModifiers(){Keys mods=Keys.None;if((Native.GetAsyncKeyState(17)&0x8000)!=0)mods|=Keys.Control;if((Native.GetAsyncKeyState(18)&0x8000)!=0)mods|=Keys.Alt;if((Native.GetAsyncKeyState(16)&0x8000)!=0)mods|=Keys.Shift;return mods;}
        private IntPtr Keyboard(int code, IntPtr message, IntPtr data)
        {
            try { if (Active && !Paused && code >= 0 && !IsOwn(Native.GetForegroundWindow())) { var k = (Native.KeyHookData)Marshal.PtrToStructure(data,typeof(Native.KeyHookData)); int key = (int)k.Key, msg = message.ToInt32(); if ((k.Flags & 0x10) == 0 && !IsControlShortcut(key,CurrentModifiers())) { bool up = msg == 0x101 || msg == 0x105; if (up) { if (held.Remove(key)) Add(new Step { Kind = ActionKind.KeyUp, KeyCode = key, Extended = (k.Flags & 1) != 0 }); } else { held.Add(key); Add(new Step { Kind = ActionKind.KeyDown, KeyCode = key, Extended = (k.Flags & 1) != 0 }); } } } } catch (Exception e) { Fail(e.Message); }
            return Native.CallNextHookEx(keyboard,code,message,data);
        }
        private IntPtr Mouse(int code, IntPtr message, IntPtr data)
        {
            try { if (Active && !Paused && code >= 0) { var m = (Native.MouseHookData)Marshal.PtrToStructure(data,typeof(Native.MouseHookData)); if ((m.Flags & 1) == 0 && !IsOwn(Native.WindowFromPoint(m.Position))) {
                int msg = message.ToInt32(); Step s = null;
                if (msg == 0x200 && RecordMoves && clock.ElapsedMilliseconds-lastMove >= 30) { s = new Step { Kind = ActionKind.MouseMove }; lastMove = clock.ElapsedMilliseconds; }
                if (msg == 0x201 || msg == 0x204 || msg == 0x207) { MouseButton b = msg == 0x201 ? MouseButton.Left : msg == 0x204 ? MouseButton.Right : MouseButton.Middle; mouseHeld.Add(b); s = new Step { Kind = ActionKind.MouseDown, Button = b }; }
                if (msg == 0x202 || msg == 0x205 || msg == 0x208) { MouseButton b = msg == 0x202 ? MouseButton.Left : msg == 0x205 ? MouseButton.Right : MouseButton.Middle; if (mouseHeld.Remove(b)) s = new Step { Kind = ActionKind.MouseUp, Button = b }; }
                if (msg == 0x20a) s = new Step { Kind = ActionKind.MouseWheel, WheelDelta = unchecked((short)(m.Data >> 16)) };
                if (s != null) { s.X = m.Position.X; s.Y = m.Position.Y; Native.Rect rect; if (RelativeCoordinates && s.Kind != ActionKind.MouseWheel && Native.GetWindowRect(Native.GetForegroundWindow(),out rect)) { s.X -= rect.Left; s.Y -= rect.Top; s.Relative = true; } Add(s); }
            } } } catch (Exception e) { Fail(e.Message); }
            return Native.CallNextHookEx(mouse,code,message,data);
        }
        public List<Step> Stop()
        {
            Dispose(); ReleaseRecordedInputs();return Steps.Select(s => s.Clone()).ToList();
        }
        public void Dispose() { Active = false; clock.Stop(); if (keyboard != IntPtr.Zero) Native.UnhookWindowsHookEx(keyboard); if (mouse != IntPtr.Zero) Native.UnhookWindowsHookEx(mouse); keyboard = mouse = IntPtr.Zero; }
    }
}
