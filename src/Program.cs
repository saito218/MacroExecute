using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MacroExecute
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try {
                bool hidden=Array.IndexOf(args,"--export-guide-frames")>=0||Array.IndexOf(args,"--self-test")>=0||Array.IndexOf(args,"--smoke")>=0||Array.IndexOf(args,"--headless-preview")>=0||Array.IndexOf(args,"--headless-probe")>=0;
                Application.SetUnhandledExceptionMode(hidden?UnhandledExceptionMode.CatchException:UnhandledExceptionMode.ThrowException);
                if(hidden)Application.ThreadException+=delegate(object sender,System.Threading.ThreadExceptionEventArgs error){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"last-error.txt"),error.Exception.ToString());Environment.Exit(1);};
                if(Array.IndexOf(args,"--headless-probe")>=0){if(args.Length!=3||args[0]!="--headless-probe")return 2;return HeadlessProbe.Run(Path.GetFullPath(args[1]),Path.GetFullPath(args[2]));}
                if(args.Length>0&&args[0]=="--self-test")return SelfTests.Run(args.Length>1?args[1]:null);
                if(args.Length>1&&args[0]=="--validate"){Plan.Build(Storage.Load(args[1]),Path.GetDirectoryName(Path.GetFullPath(args[1])));Console.WriteLine("Valid MacroExecute workflow.");return 0;}
                if(args.Length==3&&args[0]=="--export"){Macro original=Storage.Load(args[1]);var flat=new Macro{RandomSeed=original.RandomSeed,Name=original.Name,Steps=Plan.Build(original,Path.GetDirectoryName(Path.GetFullPath(args[1]))).Steps};foreach(var step in flat.Steps)if(ImageTemplate.IsImage(step.Kind))ImageTemplate.Embed(step);Portable.Export(Assembly.GetExecutingAssembly().Location,Path.GetFullPath(args[2]),flat);return 0;}
                if(args.Length==2&&args[0]=="--verify-player"){Macro embedded=Portable.Read(args[1]);if(embedded==null)throw new InvalidDataException("No MacroExecute payload found.");Portable.Validate(embedded,Path.GetDirectoryName(Path.GetFullPath(args[1])));return 0;}
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                if(args.Length==2&&args[0]=="--export-guide-frames")return HeadlessUi.ExportAnimationFrames(Path.GetFullPath(args[1]));
                if(args.Length==2&&args[0]=="--headless-preview")return HeadlessUi.Previews(Path.GetFullPath(args[1]));
                if(args.Length==2&&args[0]=="--desktop-test")return DesktopAcceptance.Run(Path.GetFullPath(args[1]));
                Macro macro=null;string path=null;bool run=false,exit=false;
                if(args.Length>1&&args[0]=="--run"){path=Path.GetFullPath(args[1]);macro=Storage.Load(path);run=true;exit=Array.IndexOf(args,"--exit")>=0;}
                else if(args.Length>0&&args[0]!="--smoke"){if(args.Length!=1)throw new ArgumentException("Usage: MacroExecute.exe [flow.mexec] | --run flow.mexec [--exit] | --validate flow.mexec | --self-test [report.txt]");path=Path.GetFullPath(args[0]);macro=Storage.Load(path);}
                else macro=Portable.Read(Application.ExecutablePath);
                if(args.Length>0&&args[0]=="--smoke"){
                    string output=Path.GetFullPath(args.Length>1?args[1]:"MacroExecute-preview.png");string directory=Path.GetDirectoryName(output);HeadlessUi.Previews(directory);File.Copy(Path.Combine(directory,"workbench-light.png"),output,true);return 0;
                }

                Application.Run(new MainForm(macro,path,run,exit));return 0;
            }catch(Exception error){if(Array.IndexOf(args,"--export-guide-frames")>=0||Array.IndexOf(args,"--headless-probe")>=0||Array.IndexOf(args,"--self-test")>=0||Array.IndexOf(args,"--validate")>=0||Array.IndexOf(args,"--smoke")>=0||Array.IndexOf(args,"--export")>=0||Array.IndexOf(args,"--verify-player")>=0||Array.IndexOf(args,"--desktop-test")>=0||Array.IndexOf(args,"--headless-preview")>=0){Console.Error.WriteLine(error.ToString());File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"last-error.txt"),error.ToString());return 1;}MessageBox.Show(error.Message,"MacroExecute",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
        }

    }
    internal static class Samples
    {
        public static Macro Nested(){string image;using(var b=new Bitmap(28,28)){using(var g=Graphics.FromImage(b)){g.Clear(Color.FromArgb(31,86,163));g.FillRectangle(Brushes.Orange,3,5,8,17);g.FillEllipse(Brushes.Lime,14,9,10,10);}image=ImageTemplate.Encode(b);}return new Macro{Name="Nested image IF example",Steps=new System.Collections.Generic.List<Step>{new Step{Kind=ActionKind.IfImage,ImageData=image,SearchIn=SearchArea.ForegroundWindow,MoveToMatch=false},new Step{Kind=ActionKind.Note,Text="Outer image found — add actions here"},new Step{Kind=ActionKind.IfImage,ImageData=image,SearchIn=SearchArea.Desktop,MoveToMatch=false},new Step{Kind=ActionKind.Note,Text="Both images found — add another IF here if needed"},new Step{Kind=ActionKind.Else},new Step{Kind=ActionKind.Note,Text="Inner image missing"},new Step{Kind=ActionKind.EndIf},new Step{Kind=ActionKind.Note,Text="Outer image result is available again here"},new Step{Kind=ActionKind.Else},new Step{Kind=ActionKind.Note,Text="Outer image missing"},new Step{Kind=ActionKind.EndIf}}};}
        public static Macro Welcome(){return new Macro{Name="Welcome flow",Steps=new System.Collections.Generic.List<Step>{new Step{Kind=ActionKind.Note,Text="A small flow to make your own"},new Step{Kind=ActionKind.ActivateWindow,Text="Notepad"},new Step{Kind=ActionKind.Delay,DelayMs=500},new Step{Kind=ActionKind.Repeat,Count=3},new Step{Kind=ActionKind.TypeText,Text="Hello from MacroExecute!\n"},new Step{Kind=ActionKind.Delay,DelayMs=250},new Step{Kind=ActionKind.EndRepeat}}};}
    }
}
