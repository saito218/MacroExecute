using System;
using System.Collections.Generic;
using System.Drawing;
namespace MacroExecute
{
    internal sealed class ThemePalette
    {
        public Color Background,Surface,Field,Selected,Accent,Ink,Muted,Border;
        private static readonly Dictionary<string,ThemePalette> palettes=new Dictionary<string,ThemePalette>();
        static ThemePalette(){
            Add("Neon",true,"101923","1a2836","243747","2f4859","81e6f3","f0f5fa","c2d3e0","6d899e");
            Add("Neon",false,"f2f6fa","ffffff","e9f0f7","dce9f2","145a78","253447","43576b","7991a6");
            Add("Aftershock",true,"16131e","22202c","302c3c","373244","d9f777","f0eff8","c5c4d2","797084");
            Add("Aftershock",false,"f4f3f7","ffffff","eeebf2","e5e0ee","623d7e","242331","4b495c","82778e");
            Add("Crimson",true,"20171c","30222a","42303a","4a343f","ffb4c8","fff0f5","e0c5d0","a27689");
            Add("Crimson",false,"faf3f5","ffffff","f5e7ec","efdae3","922a4b","3b2430","694352","a77e90");
            Add("Graphite",true,"14171b","20252c","2c333c","36404c","bad4f4","f0f4fa","c5ced9","727d8c");
            Add("Graphite",false,"f3f5f7","ffffff","eaf0f5","dde5ee","294e78","202f40","43576d","76899b");
            Add("Ocean",true,"0d1a21","142b34","203b47","294957","7ce9df","edf9fa","c1d7de","527e89");
            Add("Ocean",false,"f0f7f7","ffffff","e2f0ef","d5e7e5","00635f","203639","3c555c","6f9692");
            Add("Ember",true,"1d1714","2b231e","3b3028","4a3b30","ffc58f","fff4e8","e0cdbf","8f7765");
            Add("Ember",false,"faf5ef","ffffff","f4e9df","edddcf","864019","3c2c24","684e3e","a48b76");
        }
        private static Color Hex(string value){return ColorTranslator.FromHtml("#"+value);}
        private static void Add(string name,bool dark,params string[] c){palettes.Add(name+dark,new ThemePalette{Background=Hex(c[0]),Surface=Hex(c[1]),Field=Hex(c[2]),Selected=Hex(c[3]),Accent=Hex(c[4]),Ink=Hex(c[5]),Muted=Hex(c[6]),Border=Hex(c[7])});}
        public static ThemePalette Get(string name,bool dark){ThemePalette result;return palettes.TryGetValue((name??"")+dark,out result)?result:palettes["Neon"+dark];}
        public static string Description(string name){switch(name){case "Aftershock":return "Soft violet · electric lime";case "Crimson":return "Rose tones · berry accent";case "Graphite":return "Quiet slate · cool silver";case "Ocean":return "Deep teal · sea glass";case "Ember":return "Warm charcoal · amber";default:return "Midnight blue · cyan";}}
    }
}
