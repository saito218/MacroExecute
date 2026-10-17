using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
namespace MacroExecute
{
    internal static class IconTests
    {
        private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        private static string Fingerprint(Bitmap image){using(var stream=new MemoryStream()){image.Save(stream,ImageFormat.Png);using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(stream.ToArray()));}}
        internal static void Sheet(string path,string style,bool dark)
        {
            bool previousDark=UiTheme.Dark;string previousStyle=UiTheme.Style;try{UiTheme.Style=style;UiTheme.Dark=dark;var kinds=Enum.GetValues(typeof(ActionKind)).Cast<ActionKind>().ToArray();int rows=(kinds.Length+5)/6;
                using(var bitmap=new Bitmap(1200,54+rows*102))using(var g=Graphics.FromImage(bitmap))using(var title=new Font("Segoe UI",15,FontStyle.Bold))using(var font=new Font("Segoe UI",9)){
                    g.Clear(UiTheme.Background);System.Windows.Forms.TextRenderer.DrawText(g,"MacroExecute icon reference - "+style+" / "+(dark?"dark":"light"),title,new Rectangle(20,8,1160,36),UiTheme.Ink);
                    for(int i=0;i<kinds.Length;i++){int x=(i%6)*200,y=54+(i/6)*102;using(var fill=new SolidBrush(UiTheme.Surface))g.FillRectangle(fill,x+6,y+2,188,94);using(var icon=ActionArt.Icon(kinds[i],40))g.DrawImageUnscaled(icon,x+22,y+10);using(var small=ActionArt.Icon(kinds[i],22))g.DrawImageUnscaled(small,x+82,y+20);using(var tiny=ActionArt.Icon(kinds[i],16))g.DrawImageUnscaled(tiny,x+130,y+23);System.Windows.Forms.TextRenderer.DrawText(g,ActionArt.Name(kinds[i]),font,new Rectangle(x+10,y+60,180,30),UiTheme.Ink,System.Windows.Forms.TextFormatFlags.HorizontalCenter|System.Windows.Forms.TextFormatFlags.WordBreak|System.Windows.Forms.TextFormatFlags.NoPrefix);}
                    bitmap.Save(path,ImageFormat.Png);
                }
            }finally{UiTheme.Dark=previousDark;UiTheme.Style=previousStyle;}
        }
        public static void Run(Action<string,Action> test,string directory)
        {
            var context=System.Threading.SynchronizationContext.Current;try{
            test("Every action has distinct original artwork at list and toolbar sizes in all palettes",delegate{bool oldDark=UiTheme.Dark;string oldStyle=UiTheme.Style;try{foreach(string style in UiTheme.Styles)foreach(bool dark in new[]{false,true})foreach(int size in new[]{16,22,32}){UiTheme.Style=style;UiTheme.Dark=dark;var fingerprints=new HashSet<string>();foreach(ActionKind kind in Enum.GetValues(typeof(ActionKind)))using(var image=ActionArt.Icon(kind,size)){Check(image.Width==size&&image.Height==size,"Incorrect icon dimensions");Check(fingerprints.Add(Fingerprint(image)),"Duplicate icon: "+kind+" at "+size+" / "+style);int opaque=0;for(int y=0;y<size;y++)for(int x=0;x<size;x++)if(image.GetPixel(x,y).A>100)opaque++;Check(opaque>12&&opaque<size*size*0.85,"Empty or illegible icon: "+kind);}}}finally{UiTheme.Dark=oldDark;UiTheme.Style=oldStyle;}});
            test("Application icon is embedded and decodes without opening a window",delegate{using(var bitmap=AppBrand.ApplicationIcon.ToBitmap()){Check(bitmap.Width==32&&bitmap.Height==32,"Application icon resource missing or wrong size");Check(bitmap.GetPixel(16,16).A>0,"Application mark is empty");}});
            test("Action icon bounds reject invalid render sizes",delegate{foreach(int size in new[]{0,11,513}){bool rejected=false;try{using(var image=ActionArt.Icon(ActionKind.Note,size)){} }catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"Invalid icon size accepted");}});
            test("Common UI commands map to recognizable pictograms without replacing accessible labels",delegate{var pairs=new Dictionary<string,string>{{"Undo","undo"},{"Redo","redo"},{"File","folder"},{"Settings","settings"},{"Save step","save"},{"Copy steps","copy"},{"Delete selection","delete"},{"Add images...","import"},{"Capture screen","capture"},{"Test find","find"},{"Keyboard shortcuts","keyboard"},{"Cancel","close"}};foreach(var pair in pairs)Check(IconArt.ControlSymbol(pair.Key,pair.Key)==pair.Value,"Wrong control icon for "+pair.Key);Check(IconArt.ControlSymbol("x","Maximize window")=="maximize","Window controls use font glyphs");Check(IconArt.Label("\u25b6 Play  F9")=="Play  F9","Duplicate transport glyph not removed");});
            test("Icon button measurement stays stable through repeated compact layout passes",delegate{using(var button=new ThemeButton{Text="Save",AccessibleName="Save",AutoSize=true,Font=new Font("Segoe UI",10)}){var initial=button.GetPreferredSize(Size.Empty);for(int i=0;i<20;i++){button.Size=button.GetPreferredSize(new Size(1200,40));Check(button.GetPreferredSize(Size.Empty)==initial&&button.Width<140,"Icon measurement grows with available width");}button.CompactIconOnly=true;Check(button.GetPreferredSize(Size.Empty).Width==36&&button.AccessibleName=="Save","Compact icons lost semantics or stayed oversized");}});
            test("Color mode icons remain distinct and readable in every light and dark palette",delegate{bool oldDark=UiTheme.Dark;string oldStyle=UiTheme.Style;try{foreach(string style in UiTheme.Styles)foreach(bool dark in new[]{false,true}){UiTheme.Style=style;UiTheme.Dark=dark;using(var sun=IconArt.ControlImage("sun",22,UiTheme.Ink))using(var moon=IconArt.ControlImage("moon",22,UiTheme.Ink)){Check(Fingerprint(sun)!=Fingerprint(moon),"Sun and moon look identical");Check(sun.GetPixel(11,11).A>200,"Sun center is missing");int count=0;for(int y=0;y<22;y++)for(int x=0;x<22;x++)if(moon.GetPixel(x,y).A>128)count++;Check(count>50&&count<250,"Moon silhouette is missing or filled solid");}}}finally{UiTheme.Dark=oldDark;UiTheme.Style=oldStyle;}});
            test("Icon reference sheets render for light and dark Aftershock without a window",delegate{Sheet(Path.Combine(directory,"icons-aftershock-dark.png"),"Aftershock",true);Sheet(Path.Combine(directory,"icons-aftershock-light.png"),"Aftershock",false);});
            }finally{System.Threading.SynchronizationContext.SetSynchronizationContext(context);}
        }
    }
}
