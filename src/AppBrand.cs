using System.Drawing;
using System.Reflection;
namespace MacroExecute
{
    internal static class AppBrand
    {
        internal static readonly Icon ApplicationIcon=Load();
        private static Icon Load(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("MacroExecute.AppIcon"))using(var icon=new Icon(stream,32,32))return (Icon)icon.Clone();}
    }
}
