using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacroExecute
{
    internal sealed class DurationInput : UserControl
    {
        private readonly NumericUpDown number=new NumericUpDown{BorderStyle=BorderStyle.None,Dock=DockStyle.Fill};
        private readonly ThemeButton ms=new ThemeButton{Text="ms",Dock=DockStyle.Fill,AccessibleName="Use milliseconds",Margin=new Padding(1,0,1,0)},seconds=new ThemeButton{Text="s",Dock=DockStyle.Fill,AccessibleName="Use seconds",Margin=new Padding(1,0,1,0)};
        private readonly int minimum,maximum;private bool inSeconds;
        public int Milliseconds {get{return Decimal.ToInt32(Decimal.Round(number.Value*(inSeconds?1000:1),0,MidpointRounding.AwayFromZero));}set{number.Value=value/(inSeconds?1000m:1m);}}
        public DurationInput(int value,int min,int max,bool showSeconds)
        {
            minimum=min;maximum=max;AutoScaleMode=AutoScaleMode.None;Height=30;var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,42));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,36));var frame=new FieldFrame{Dock=DockStyle.Fill,Margin=Padding.Empty};frame.Controls.Add(number);layout.Controls.Add(frame,0,0);layout.Controls.Add(ms,1,0);layout.Controls.Add(seconds,2,0);Controls.Add(layout);number.Minimum=min;number.Maximum=max;number.Value=value;number.ThousandsSeparator=true;ms.Click+=delegate{SetSeconds(false);};seconds.Click+=delegate{SetSeconds(true);};SetSeconds(showSeconds);
        }
        internal void SetSeconds(bool value){int keep=Milliseconds;inSeconds=value;number.Minimum=0;number.Maximum=86400000;number.DecimalPlaces=value?3:0;number.Increment=value?0.1m:100;number.Value=keep/(value?1000m:1m);number.Minimum=minimum/(value?1000m:1m);number.Maximum=maximum/(value?1000m:1m);ms.Tag=value?"quiet":"selected";seconds.Tag=value?"selected":"quiet";number.AccessibleName=AccessibleName+(value?" in seconds":" in milliseconds");UiTheme.Apply(this);}
        protected override void OnParentChanged(EventArgs e){base.OnParentChanged(e);number.AccessibleName=AccessibleName+(inSeconds?" in seconds":" in milliseconds");}
    }
}
