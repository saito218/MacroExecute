using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacroExecute
{
    internal sealed partial class StepEditor
    {
        private static string ClipboardDisplay(string text){return text.Replace("\r\n","\n").Replace("\r","\n").Replace("\n","\r\n");}
        private void ClipboardFields()
        {
            var originalMode=Value.ClipboardMatch;string originalText=Value.Text,displayText=ClipboardDisplay(Value.Text);bool forceRules=false;
            var modes=Choices("Comparison",new[]{"String rows and groups","Exact whole clipboard (legacy)"},Value.ClipboardMatch==ClipboardMatchMode.Exact?1:0,delegate(int n){});
            var builder=new ClipboardRuleBuilder(ClipboardRules.FromLegacy(Value),Value.ClipboardReuseGroups,Value.ClipboardIgnoreCase);string originalExpression=builder.Expression;Row("",builder,builder.ContentHeight+10);int builderRow=fields.RowCount-1;var blankCaption=fields.GetControlFromPosition(0,builderRow);fields.Controls.Remove(blankCaption);blankCaption.Dispose();fields.SetColumn(builder,0);fields.SetColumnSpan(builder,2);
            builder.LayoutChanged+=delegate{fields.RowStyles[builderRow].Height=builder.ContentHeight+10;fields.PerformLayout();};
            var terms=new TextBox{Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=false,Text=displayText,AcceptsReturn=true};Row("Strings to check",terms,150);int exactRow=fields.RowCount-1;
            var instructions=new Label{Text="Spaces mean OR. Quote phrases: ring \"Monthly Report\". AND needs different matching strings unless that group allows reuse. Any complete GROUP succeeds; partial matches from separate groups cannot combine.",Tag="muted"};Row("",instructions,100);
            var legacy=new FlowLayoutPanel{WrapContents=true};var legacyInfo=new Label{Text="Existing matching behavior is preserved until you edit these rows or convert them.",AutoSize=true,MaximumSize=new Size(490,0),Tag="muted"};var convert=new ThemeButton{Text="Convert to row rules",AutoSize=true,Height=32,Tag="quiet",AccessibleName="Convert legacy clipboard rules"};legacy.Controls.Add(legacyInfo);legacy.Controls.Add(convert);Row("",legacy,84);int legacyRow=fields.RowCount-1;
            Action explain=delegate{bool exact=((RadioButton)modes.Controls[1]).Checked;builder.Visible=!exact;fields.RowStyles[builderRow].Height=exact?0:builder.ContentHeight+10;terms.Parent.Visible=exact;fields.RowStyles[exactRow].Height=exact?150:0;bool old=!exact&&originalMode!=ClipboardMatchMode.Exact&&originalMode!=ClipboardMatchMode.Rules&&!forceRules;legacy.Visible=old;fields.RowStyles[legacyRow].Height=old?84:0;instructions.Text=exact?"The complete clipboard must equal this text, including line breaks. Existing exact comparisons are preserved.":"Spaces mean OR. Quote phrases: ring \"Monthly Report\". AND needs different matching strings unless that group allows reuse. Any complete GROUP succeeds; partial matches from separate groups cannot combine.";};
            builder.ReuseChanged+=delegate{forceRules=true;explain();};convert.Click+=delegate{forceRules=true;explain();};foreach(RadioButton radio in modes.Controls)radio.CheckedChanged+=delegate{explain();};explain();
            apply.Add(delegate{if(((RadioButton)modes.Controls[1]).Checked){Value.ClipboardMatch=ClipboardMatchMode.Exact;Value.ClipboardReuseGroups=new int[0];Value.Text=terms.Text==displayText?originalText:terms.Text;}else{string expression=builder.Expression;if(!forceRules&&originalMode!=ClipboardMatchMode.Exact&&originalMode!=ClipboardMatchMode.Rules&&expression==originalExpression&&builder.ReuseGroups.Length==0){Value.ClipboardMatch=originalMode;Value.Text=originalText;}else{Value.ClipboardMatch=ClipboardMatchMode.Rules;Value.ClipboardReuseGroups=builder.ReuseGroups;Value.Text=expression;}}});
            var ignoreCase=Check("Ignore letter case",Value.ClipboardIgnoreCase,delegate(bool v){Value.ClipboardIgnoreCase=v;});ignoreCase.CheckedChanged+=delegate{builder.IgnoreCase=ignoreCase.Checked;};Check("Invert result (NOT)",Value.ClipboardNegate,delegate(bool v){Value.ClipboardNegate=v;});
            Row("",new Label{Text="Matching → IF actions. Not matching → ELSE actions. Leave a branch empty to skip it. A clipboard without text never matches, even with NOT.",Tag="muted"},60);
            var toggle=new ThemeButton{Text="▸ Try with sample text",Tag="quiet",AccessibleName="Toggle clipboard tester",TextAlign=ContentAlignment.MiddleLeft};Row("",toggle,40);
            var testPanel=new TableLayoutPanel{ColumnCount=1,RowCount=3,Tag="canvas"};testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));testPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,110));testPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,38));testPanel.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var sample=new TextBox{Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,AccessibleName="Clipboard test text"};var sampleFrame=new FieldFrame{Dock=DockStyle.Fill};sampleFrame.Controls.Add(sample);testPanel.Controls.Add(sampleFrame,0,0);
            var tools=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};var load=new ThemeButton{Text="Load item example",AutoSize=true,Height=32,AccessibleName="Load clipboard item example"};var test=new ThemeButton{Text="Test condition",Tag="primary",AutoSize=true,Height=32,AccessibleName="Test clipboard condition"};tools.Controls.Add(load);tools.Controls.Add(test);testPanel.Controls.Add(tools,0,1);
            var result=new Label{Dock=DockStyle.Fill,Tag="muted",AccessibleName="Clipboard test result"};testPanel.Controls.Add(result,0,2);Row("",testPanel,1);int row=fields.RowCount-1;fields.RowStyles[row].Height=0;testPanel.Visible=false;
            bool testerOpen=false;toggle.Click+=delegate{testerOpen=!testerOpen;bool open=testerOpen;testPanel.Visible=open;fields.RowStyles[row].Height=open?220:0;toggle.Text=open?"▾ Try with sample text":"▸ Try with sample text";};
            load.Click+=delegate{sample.Text=ClipboardDisplay(ClipboardExamples.ItemText);result.Text="Example loaded. This does not read or change your clipboard.";};
            test.Click+=delegate{try{Apply();bool matches=ClipboardCondition.Matches(sample.Text,Value);result.Text=matches?"MATCH → run the IF branch.":"NO MATCH → run the ELSE branch (or skip if empty).";result.Tag=matches?"heading":"muted";validationMessage.Text="";UiTheme.Apply(result);}catch(Exception error){result.Text=error.GetBaseException().Message;result.Tag="danger";UiTheme.Apply(result);}};
        }
        private void CopyFields()
        {
            Row("Shortcut",new Label{Text="Ctrl+C · sent to the foreground application",Tag="heading"},42);
            Row("",new Label{Text="Focus the target window first. Select the text to copy in the target application. This action leaves the copied text on the clipboard.",Tag="muted"},80);
            var wait=Check("Wait for fresh clipboard text",Value.ClipboardWaitForChange,delegate(bool v){Value.ClipboardWaitForChange=v;});var timeout=TimeField("Copy timeout",Value.TimeoutMs,delegate(int n){Value.TimeoutMs=n;});var poll=TimeField("Check every",Value.PollMs,delegate(int n){Value.PollMs=n;},10,60000,false);Action update=delegate{timeout.Enabled=poll.Enabled=wait.Checked;};wait.CheckedChanged+=delegate{update();};update();
            Row("",new Label{Text="When waiting, unchanged or empty clipboard data cannot succeed. If copying fails, playback stops at the timeout instead of checking stale item text.",Tag="muted"},68);
        }
    }
}
