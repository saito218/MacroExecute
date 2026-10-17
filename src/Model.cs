using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace MacroExecute
{
    public enum ActionKind { Note, Delay, KeyDown, KeyUp, KeyPress, MouseMove, MouseDown, MouseUp, MouseClick, MouseWheel, TypeText, SetClipboard, TypeDate, TypeEnvironment, Launch, OpenUrl, ActivateWindow, MoveWindow, WaitWindow, FindPixel, WaitPixel, FindImage, IfPixel, IfImage, IfClipboard, IfWindow, Else, EndIf, Repeat, EndRepeat, Break, Label, Jump, Include, Message, Pause, CopyFile, DeleteFile, IfFile, IfProcess, WaitProcess, ClipboardIncrement, ClipboardClear, Paste, RunCSharp, WaitImage, ClickImage, CopySelection }
    public enum PointerMode { Absolute, CurrentPointer, LastMatch, ForegroundWindow, CursorOffset }
    public enum ImageToleranceMode { InclusiveRgb, LegacyRgbPlusOne }
    public enum TextEntryMode { Unicode, ClipboardPaste }
    public enum SearchArea { Legacy, Desktop, ForegroundWindow, Rectangle }
    public enum MouseButton { Left, Right, Middle }

    public sealed class Step : ICustomTypeDescriptor
    {
        [Category("Action")] public ActionKind Kind { get; set; }
        [Category("Action")] public bool Enabled { get; set; }
        [Category("Content"), Description("Text, label name, window-title fragment or command argument, depending on action.")] public string Text { get; set; }
        [Category("Content"), Description("Executable, image, included macro, environment variable, or source file path.")] public string Target { get; set; }
        [Category("Position")] public int X { get; set; }
        [Category("Position")] public int Y { get; set; }
        [Category("Position")] public int Width { get; set; }
        [Category("Position")] public int Height { get; set; }
        [Category("Position"), Description("For mouse coordinates: relative to the foreground window.")] public bool Relative { get; set; }
        [Category("Timing")] public int DelayMs { get; set; }
        [Category("Timing"), Description("Zero checks once for WaitWindow/WaitPixel; otherwise maximum wait.")] public int TimeoutMs { get; set; }
        [Category("Input"), Description("Windows virtual-key value, for example 65 = A and 13 = Enter.")] public int KeyCode { get; set; }
        [Category("Input")] public bool Extended { get; set; }
        [Category("Input")] public MouseButton Button { get; set; }
        [Category("Input"), Description("Mouse-wheel delta, normally 120 or -120.")] public int WheelDelta { get; set; }
        [Category("Control flow"), Description("Repeat count; zero repeats until Stop or Break.")] public int Count { get; set; }
        [Category("Timing"), Description("Optional maximum randomized delay. Zero uses DelayMs exactly.")] public int DelayMaxMs { get; set; }
        [Category("Control flow"), Description("Optional maximum randomized repeat count. Zero uses Count exactly.")] public int CountMax { get; set; }
        [Category("Position"), Description("Random offset in each direction, in pixels; zero is exact.")] public int PositionJitter { get; set; }
        [Category("Image"), Description("RGB hex color, for example #42C6AE.")] public string ColorRgb { get; set; }
        [Category("Image"), Description("Maximum per-channel difference, 0 = exact, 255 = any.")] public int Tolerance { get; set; }
        [Category("Image"), Description("Move cursor to the first image or pixel match.")] public bool MoveToMatch { get; set; }
        [Category("Position"), Description("Choose absolute coordinates, the current pointer, or the last successful image/pixel result.")] public PointerMode Pointer { get; set; }
        [Category("Image")] public SearchArea SearchIn { get; set; }
        [Browsable(false)] public string ImageData { get; set; }
        [Browsable(false)] public string[] AdditionalImages { get; set; }
        [Browsable(false)] public bool RequireAllImages { get; set; }
        [Category("Timing"), Description("Key or mouse button hold time in real milliseconds, independent of playback speed.")] public int InputHoldMs { get; set; }
        [Category("Timing")] public int TextIntervalMs { get; set; }
        [Category("Content")] public TextEntryMode TextEntry { get; set; }
        [Category("Image"), Description("Inclusive RGB uses the exact threshold. Legacy RGB adds one, capped at 255.")] public ImageToleranceMode ToleranceMode { get; set; }
        [Category("Image")] public bool ContinueIfMissing { get; set; }
        [Category("Image")] public bool WaitForMatch { get; set; }
        [Category("Image")] public bool WaitForever { get; set; }
        [Category("Image")] public bool Negate { get; set; }
        [Category("Image")] public int PollMs { get; set; }
        [Category("Image")] public int MatchOffsetX { get; set; }
        [Category("Image")] public int MatchOffsetY { get; set; }
        [Category("Clipboard"), Description("Exact, legacy Any/All/Groups, or expandable Rules with quoted alternatives, distinct AND matches and GROUP OR.")] public ClipboardMatchMode ClipboardMatch { get; set; }
        [Category("Clipboard"), Description("Zero-based rule groups allowed to reuse a matching string across AND clauses.")] public int[] ClipboardReuseGroups {get;set;}
        [Category("Clipboard")] public bool ClipboardIgnoreCase { get; set; }
        [Category("Clipboard"), Description("Invert a valid text comparison. A clipboard without text never matches.")] public bool ClipboardNegate { get; set; }
        [Category("Clipboard"), Description("After Ctrl+C, wait for a changed clipboard containing non-empty text; timeout stops playback.")] public bool ClipboardWaitForChange { get; set; }
        [Category("Input"), Description("Hold Ctrl, Shift and/or Alt during a mouse or image click. Previously held modifiers are preserved.")] public MouseModifiers MouseModifiers { get; set; }
        [Category("Input"), Description("1 single, 2 double, 3 triple click. Modifiers remain down for the whole sequence.")] public int ClickCount { get; set; }
        [Category("Timing"), Description("Real milliseconds between button release and the next click in a double/triple sequence.")] public int ClickIntervalMs { get; set; }
        public Step() { AdditionalImages=new string[0];ClipboardReuseGroups=new int[0];ClickCount=1;ClickIntervalMs=100;ClipboardWaitForChange=true;InputHoldMs=30; ImageData = ""; PollMs = 100; Enabled = true; Text = ""; Target = ""; DelayMs = 500; TimeoutMs = 5000; Count = 2; KeyCode = 65; WheelDelta = 120; ColorRgb = "#42C6AE"; MoveToMatch = true; }
        public Step Clone() { var copy=(Step)MemberwiseClone();copy.AdditionalImages=AdditionalImages==null?null:(string[])AdditionalImages.Clone();copy.ClipboardReuseGroups=ClipboardReuseGroups==null?null:(int[])ClipboardReuseGroups.Clone();return copy; }
        public override string ToString()
        {
            switch (Kind) {
                case ActionKind.Delay: return DelayMs + " ms";
                case ActionKind.MouseMove: case ActionKind.MouseClick: case ActionKind.MouseDown: case ActionKind.MouseUp: return Pointer==PointerMode.CurrentPointer?Button+" at current pointer":Button + " / " + Pointer + " at " + X + ", " + Y + (Relative ? " (window)" : "");
                case ActionKind.KeyDown: case ActionKind.KeyUp: case ActionKind.KeyPress: return ((System.Windows.Forms.Keys)KeyCode).ToString();
                case ActionKind.Repeat: return Count == 0 ? "Forever (until Stop)" : Count + " iterations";
                case ActionKind.FindPixel: case ActionKind.WaitPixel: case ActionKind.IfPixel: return ColorRgb + " / tolerance " + Tolerance;
                case ActionKind.FindImage: case ActionKind.IfImage: case ActionKind.WaitImage: case ActionKind.ClickImage: return (!string.IsNullOrEmpty(ImageData)?"Embedded reference":Target)+" / "+SearchIn+" / tolerance "+Tolerance;
                default: return string.IsNullOrEmpty(Text) ? Target : Text.Replace("\r", " ").Replace("\n", " ");
            }
        }
        private bool Show(string name)
        {
            if(name=="MouseModifiers"||name=="ClickCount"||name=="ClickIntervalMs")return Kind==ActionKind.MouseClick||Kind==ActionKind.ClickImage;
            if (name=="ClipboardReuseGroups"||name=="ClipboardMatch"||name=="ClipboardIgnoreCase"||name=="ClipboardNegate")return Kind==ActionKind.IfClipboard;
            if(name=="ClipboardWaitForChange")return Kind==ActionKind.CopySelection;
            if (name == "Kind") return false;
            if (name == "Enabled") return true;
            if (name == "DelayMaxMs") return Kind==ActionKind.Delay;
            if (name == "CountMax") return Kind==ActionKind.Repeat;
            if (name == "PositionJitter") return Kind.ToString().StartsWith("Mouse") || Kind==ActionKind.ClickImage;
            if (name == "InputHoldMs") return Kind==ActionKind.KeyPress||Kind==ActionKind.MouseClick||Kind==ActionKind.ClickImage||Kind==ActionKind.Paste||Kind==ActionKind.CopySelection;
            if (name == "TextIntervalMs" || name == "TextEntry") return Kind==ActionKind.TypeText||Kind==ActionKind.TypeDate||Kind==ActionKind.TypeEnvironment;
            if (name == "ToleranceMode") return ImageTemplate.IsImage(Kind);
            if (name == "ImageData" || name == "AdditionalImages" || name == "RequireAllImages") return false;
            if (new[]{"SearchIn","ContinueIfMissing","WaitForMatch","WaitForever","Negate","PollMs","MatchOffsetX","MatchOffsetY"}.Contains(name)) return Kind.ToString().EndsWith("Image") || Kind.ToString().EndsWith("Pixel") || (name=="PollMs"&&Kind==ActionKind.CopySelection);
            if (name == "Pointer") return Kind.ToString().StartsWith("Mouse");
            string k = Kind.ToString();
            if (name == "Text") return new[] { "Note", "TypeText", "TypeDate", "Launch", "ActivateWindow", "MoveWindow", "WaitWindow", "IfWindow", "IfClipboard", "Label", "Jump", "Message", "SetClipboard", "CopyFile", "IfProcess", "WaitProcess", "RunCSharp" }.Contains(k);
            if (name == "Target") return new[] { "Launch", "OpenUrl", "FindImage", "IfImage", "WaitImage", "ClickImage", "Include", "TypeEnvironment", "CopyFile", "DeleteFile", "IfFile" }.Contains(k);
            if (name == "X" || name == "Y") return k.StartsWith("Mouse") || k == "MoveWindow" || k.EndsWith("Pixel") || k.EndsWith("Image");
            if (name == "Width" || name == "Height") return k == "MoveWindow" || k.EndsWith("Pixel") || k.EndsWith("Image");
            if (name == "Relative") return k.StartsWith("Mouse") && k != "MouseWheel";
            if (name == "DelayMs") return k == "Delay" || k == "ClipboardIncrement";
            if (name == "TimeoutMs") return k == "WaitWindow" || k == "WaitPixel" || k == "WaitProcess" || k == "RunCSharp" || k=="CopySelection" || k.EndsWith("Image");
            if (name == "KeyCode" || name == "Extended") return k.StartsWith("Key");
            if (name == "Button") return (k.StartsWith("Mouse") && k != "MouseWheel" && k != "MouseMove") || Kind==ActionKind.ClickImage;
            if (name == "WheelDelta") return k == "MouseWheel";
            if (name == "Count") return k == "Repeat";
            if (name == "ColorRgb") return k.EndsWith("Pixel");
            if (name == "Tolerance" || name == "MoveToMatch") return k.EndsWith("Pixel") || k.EndsWith("Image");
            return false;
        }
        public PropertyDescriptorCollection GetProperties() { return new PropertyDescriptorCollection(TypeDescriptor.GetProperties(this, true).Cast<PropertyDescriptor>().Where(p => Show(p.Name)).ToArray()); }
        public PropertyDescriptorCollection GetProperties(Attribute[] a) { return GetProperties(); }
        public AttributeCollection GetAttributes() { return TypeDescriptor.GetAttributes(this, true); }
        public string GetClassName() { return "Macro step"; } public string GetComponentName() { return Kind.ToString(); }
        public TypeConverter GetConverter() { return TypeDescriptor.GetConverter(this, true); }
        public EventDescriptor GetDefaultEvent() { return null; } public PropertyDescriptor GetDefaultProperty() { return null; }
        public object GetEditor(Type t) { return null; } public EventDescriptorCollection GetEvents() { return EventDescriptorCollection.Empty; }
        public EventDescriptorCollection GetEvents(Attribute[] a) { return EventDescriptorCollection.Empty; } public object GetPropertyOwner(PropertyDescriptor p) { return this; }
    }

    public sealed class Macro
    {
        public int? RandomSeed { get; set; }
        public int SchemaVersion { get; set; }
        public string Name { get; set; }
        public List<Step> Steps { get; set; }
        public Macro() { SchemaVersion = 1; Name = "Untitled flow"; Steps = new List<Step>(); }
    }
    public static class Storage
    {
        public const int MaxSteps = 100000;
        public const int MaxBytes = 16 * 1024 * 1024;
        public static JavaScriptSerializer Serializer() { return new JavaScriptSerializer { MaxJsonLength = MaxBytes, RecursionLimit = 128 }; }
        public static string Encode(Macro macro) { Validate(macro); string text=Serializer().Serialize(macro);if(Encoding.UTF8.GetByteCount(text)>MaxBytes)throw new InvalidDataException("Macro exceeds the 16 MB limit.");return text; }
        public static Macro Decode(string json)
        {
            if (json == null || Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new InvalidDataException("Macro exceeds the 16 MB limit.");
            Macro m = Serializer().Deserialize<Macro>(json); Validate(m); return m;
        }
        public static Macro Load(string path) { if (new FileInfo(path).Length > MaxBytes) throw new InvalidDataException("Macro is too large."); return Decode(File.ReadAllText(path, Encoding.UTF8)); }
        public static void Save(string path, Macro macro)
        {
            string text = Encode(macro); string full = Path.GetFullPath(path); string temp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllText(temp, text, new UTF8Encoding(false)); if (File.Exists(full)) File.Replace(temp, full, null); else File.Move(temp, full); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        public static void Validate(Macro macro)
        {
            if (macro == null || macro.SchemaVersion != 1 || macro.Steps == null) throw new InvalidDataException("Expected a MacroExecute schema version 1 document.");
            if (macro.Steps.Count > MaxSteps) throw new InvalidDataException("Too many steps.");
            foreach (Step s in macro.Steps) {
                if (s != null && (s.AdditionalImages == null || s.AdditionalImages.Length > 63 || s.AdditionalImages.Any(string.IsNullOrWhiteSpace))) throw new InvalidDataException("Use at most 64 nonempty reference images.");
                if (s == null || !Enum.IsDefined(typeof(ActionKind), s.Kind) || !Enum.IsDefined(typeof(MouseButton), s.Button)) throw new InvalidDataException("Unknown or null action.");
                if (s.DelayMs < 0 || s.DelayMs > 86400000 || s.TimeoutMs < 0 || s.TimeoutMs > 86400000) throw new InvalidDataException("Durations must be between zero and one day.");
                if (s.Count < 0 || s.Count > 1000000 || s.KeyCode < 1 || s.KeyCode > 255) throw new InvalidDataException("Repeat count or virtual key is out of range.");
                if(((int)s.MouseModifiers&~7)!=0||s.ClickCount<1||s.ClickCount>3||s.ClickIntervalMs<0||s.ClickIntervalMs>60000)throw new InvalidDataException("Invalid mouse modifiers, click count or click interval.");
                if(s.MouseModifiers!=MouseModifiers.None&&s.Kind!=ActionKind.MouseClick&&s.Kind!=ActionKind.ClickImage)throw new InvalidDataException("Held click modifiers apply only to Mouse click or Click image. Use explicit key steps for drags or other actions.");
                if(s.InputHoldMs<0||s.InputHoldMs>60000||s.TextIntervalMs<0||s.TextIntervalMs>60000||!Enum.IsDefined(typeof(TextEntryMode),s.TextEntry)||!Enum.IsDefined(typeof(ImageToleranceMode),s.ToleranceMode))throw new InvalidDataException("Invalid input timing, text method or tolerance mode.");
                if(s.DelayMaxMs<0 || s.DelayMaxMs>86400000 || (s.DelayMaxMs>0 && s.DelayMaxMs<s.DelayMs) || s.CountMax<0 || s.CountMax>1000000 || (s.CountMax>0 && (s.Count==0 || s.CountMax<s.Count)) || s.PositionJitter<0 || s.PositionJitter>32768)throw new InvalidDataException("Randomized ranges must have a maximum at least as large as the minimum; infinite repeats cannot be randomized.");
                if (s.Tolerance < 0 || s.Tolerance > 255 || s.Width < 0 || s.Height < 0 || s.Width > 32768 || s.Height > 32768) throw new InvalidDataException("Search dimensions or tolerance are out of range.");
                if (s.Kind.ToString().EndsWith("Pixel")) ParseColor(s.ColorRgb);
                if (!Enum.IsDefined(typeof(PointerMode),s.Pointer) || !Enum.IsDefined(typeof(SearchArea),s.SearchIn) || s.PollMs < 10 || s.PollMs > 60000) throw new InvalidDataException("Invalid pointer, search area, or polling interval.");
                if (s.Text == null || s.Target == null) throw new InvalidDataException("Text and Target must be strings.");
                if(s.Kind==ActionKind.IfClipboard)ClipboardCondition.Validate(s);
            }
        }
        public static System.Drawing.Color ParseColor(string value)
        {
            int number; if (value == null || value.Length != 7 || value[0] != '#' || !int.TryParse(value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number)) throw new InvalidDataException("Use an RGB color such as #42C6AE.");
            return System.Drawing.Color.FromArgb((number >> 16) & 255, (number >> 8) & 255, number & 255);
        }
    }
}
