using System;
using System.Drawing;
using System.Windows.Forms;
namespace MacroExecute
{
    internal sealed partial class StepEditor
    {
        private RadioButton[] clickModes;
        private void ClickModeFields()
        {
            var modifiers=new FlowLayoutPanel{WrapContents=false};var ctrl=new ThemeCheckBox{Text="Ctrl",AutoSize=true,Checked=(Value.MouseModifiers&MouseModifiers.Ctrl)!=0,AccessibleName="Click modifier Ctrl"};var shift=new ThemeCheckBox{Text="Shift",AutoSize=true,Checked=(Value.MouseModifiers&MouseModifiers.Shift)!=0,AccessibleName="Click modifier Shift"};var alt=new ThemeCheckBox{Text="Alt",AutoSize=true,Checked=(Value.MouseModifiers&MouseModifiers.Alt)!=0,AccessibleName="Click modifier Alt"};modifiers.Controls.Add(ctrl);modifiers.Controls.Add(shift);modifiers.Controls.Add(alt);Row("Hold during click",modifiers,42);apply.Add(delegate{Value.MouseModifiers=(ctrl.Checked?MouseModifiers.Ctrl:0)|(shift.Checked?MouseModifiers.Shift:0)|(alt.Checked?MouseModifiers.Alt:0);});
            var patterns=new FlowLayoutPanel{WrapContents=false};clickModes=new RadioButton[3];for(int i=0;i<3;i++){clickModes[i]=new ThemeRadioButton{Text=new[]{"Single","Double","Triple"}[i],Checked=Value.ClickCount==i+1,AutoSize=true,AccessibleName=new[]{"Single click","Double click","Triple click"}[i]};patterns.Controls.Add(clickModes[i]);}Row("Click pattern",patterns,52);apply.Add(delegate{for(int i=0;i<3;i++)if(clickModes[i].Checked)Value.ClickCount=i+1;});
            tips.SetToolTip(alt,"Alt-click in one step. Combine with Ctrl or Shift as needed. The target app decides what a modified click does.");tips.SetToolTip(modifiers,"Modifiers stay down through the entire click sequence. Keys already held by you or the macro are preserved.");
        }
        private void ClickTimingFields()
        {
            var hold=TimeField("Button / key hold",Value.InputHoldMs,delegate(int n){Value.InputHoldMs=n;},0,60000,false);var interval=TimeField("Between clicks",Value.ClickIntervalMs,delegate(int n){Value.ClickIntervalMs=n;},0,60000,false);
            Action update=delegate{interval.Enabled=clickModes[1].Checked||clickModes[2].Checked;};foreach(var radio in clickModes)radio.CheckedChanged+=delegate{update();};update();
            var presets=new FlowLayoutPanel{WrapContents=false};var normal=new ThemeButton{Text="Normal press · 30 ms",AutoSize=true,Height=32,Tag="quiet",AccessibleName="Normal mouse press preset"};var longPress=new ThemeButton{Text="Long press · 750 ms",AutoSize=true,Height=32,Tag="quiet",AccessibleName="Long mouse press preset"};normal.Click+=delegate{hold.Milliseconds=30;};longPress.Click+=delegate{clickModes[0].Checked=true;hold.Milliseconds=750;};presets.Controls.Add(normal);presets.Controls.Add(longPress);Row("Press presets",presets,42);
            Row("",new Label{Text="Ctrl, Shift and Alt can be combined with left, right or middle clicks. Double/triple click interpretation depends on the target app and the gap. Image-click modifiers are held after finding the image.",Tag="muted"},74);
        }
    }
}
