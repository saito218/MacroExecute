using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    internal sealed class LaunchTypeBox : ComboBox
    {
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if(message.Msg!=0xF&&message.Msg!=0x317&&message.Msg!=0x318)return;
            if(message.Msg!=0xF&&message.WParam==IntPtr.Zero)return;
            using(var graphics=message.Msg==0xF?Graphics.FromHwnd(Handle):Graphics.FromHdc(message.WParam)){
                var bounds=new Rectangle(2,2,Math.Max(1,ClientSize.Width-SystemInformation.VerticalScrollBarWidth-4),Math.Max(1,ClientSize.Height-4));
                using(var fill=new SolidBrush(UiTheme.Field))graphics.FillRectangle(fill,bounds);
                TextRenderer.DrawText(graphics,SelectedItem==null?"":SelectedItem.ToString(),Font,bounds,UiTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
                if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(graphics,bounds,UiTheme.Accent,UiTheme.Field);
            }
        }
    }
    internal static class LaunchFiles
    {
        internal static readonly string[] Types={
            "All files|*.*",
            "Windows programs|*.exe;*.com",
            "Windows shortcuts|*.lnk;*.url",
            "Batch and command scripts|*.bat;*.cmd",
            "PowerShell scripts|*.ps1;*.psm1",
            "Python scripts|*.py;*.pyw",
            "JavaScript and VBScript|*.js;*.jse;*.vbs;*.vbe;*.wsf",
            "AutoHotkey and AutoIt scripts|*.ahk;*.au3",
            "Shell and other scripts|*.sh;*.bash;*.pl;*.rb;*.lua",
            "Installers and packages|*.msi;*.msp;*.msix;*.appx;*.msixbundle;*.appxbundle",
            "PDF documents|*.pdf",
            "Text and notes|*.txt;*.md;*.markdown;*.rtf;*.log",
            "Word and OpenDocument text|*.doc;*.docx;*.docm;*.dotx;*.odt;*.ott",
            "Spreadsheets and tables|*.xls;*.xlsx;*.xlsm;*.xlsb;*.ods;*.csv;*.tsv",
            "Presentations|*.ppt;*.pptx;*.pptm;*.pps;*.ppsx;*.odp",
            "Images and graphics|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.tif;*.tiff;*.svg;*.ico;*.heic;*.avif",
            "Audio|*.mp3;*.wav;*.flac;*.aac;*.m4a;*.ogg;*.opus;*.wma;*.aiff",
            "Video|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.mpeg;*.mpg;*.m4v",
            "Archives and disk images|*.zip;*.7z;*.rar;*.tar;*.gz;*.bz2;*.xz;*.iso;*.img",
            "Web pages|*.html;*.htm;*.mhtml;*.mht",
            "Data and configuration|*.json;*.xml;*.yaml;*.yml;*.toml;*.ini;*.cfg;*.sql",
            "Projects and source code|*.sln;*.slnx;*.csproj;*.vbproj;*.vcxproj;*.code-workspace;*.cs;*.cpp;*.h;*.java;*.ipynb",
            "Design and 3D files|*.psd;*.ai;*.eps;*.xcf;*.kra;*.blend;*.obj;*.stl;*.fbx;*.dwg;*.dxf",
            "Email and calendar|*.eml;*.msg;*.ics;*.vcf",
            "Java applications|*.jar;*.jnlp"
        };
        internal static string Filter {get{return string.Join("|",Types);}}
        internal static string Label(int index){return Types[index].Split('|')[0]+" ("+Types[index].Split('|')[1]+")";}
        internal static int TypeFor(string path){string extension=Path.GetExtension(path??"");if(extension.Length==0)return 0;for(int i=1;i<Types.Length;i++)if(Types[i].Split('|')[1].Split(';').Any(p=>string.Equals(p,"*"+extension,StringComparison.OrdinalIgnoreCase)))return i;return 0;}
        internal static OpenFileDialog Dialog(int type,string path,string directory)
        {
            var dialog=new OpenFileDialog{Title="Choose a program or file to open",Filter=Filter,FilterIndex=type+1,CheckFileExists=true,Multiselect=false,RestoreDirectory=true,AddExtension=false};
            if(Directory.Exists(directory))dialog.InitialDirectory=directory;
            if(!string.IsNullOrWhiteSpace(path))dialog.FileName=path;
            return dialog;
        }
    }
}
