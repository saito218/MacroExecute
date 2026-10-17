using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacroExecute
{
    internal sealed partial class StepEditor : ThemeWindow
    {
        public Step Value { get; private set; }
        private readonly TableLayoutPanel fields=new TableLayoutPanel();
        private readonly PictureBox preview=new PictureBox();
        private readonly Label imageInfo=new Label();
        private readonly Label validationMessage=new Label{Dock=DockStyle.Fill,Tag="danger",AutoEllipsis=true,AccessibleName="Step validation",AccessibleRole=AccessibleRole.Alert,Padding=new Padding(0,5,10,0)};
        private readonly TableLayoutPanel examplePanel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Tag="canvas",Margin=Padding.Empty};
        private readonly ThemeButton exampleToggle=new ThemeButton{Text="▸ How to use this action · Example",AccessibleName="Toggle action example",Dock=DockStyle.Fill,Tag="quiet",TextAlign=ContentAlignment.MiddleLeft,Margin=Padding.Empty};
        private readonly TextBox exampleText=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,AccessibleName="Action usage example",Visible=false};
        private readonly Panel exampleHost=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Tag="canvas",Visible=false,AccessibleName="Visual example guide"};
        private readonly Panel exampleContent=new Panel{Dock=DockStyle.Top,Tag="canvas",Margin=Padding.Empty,AccessibleName="Visual example content"};
        private readonly FlowLayoutPanel exampleScenarios=new FlowLayoutPanel{WrapContents=true,Tag="canvas",AccessibleName="Example scenarios",Margin=Padding.Empty};
        private readonly Label exampleCaption=new Label{Text="Illustration · Sample values, independent of your settings",Tag="muted",AccessibleName="Example illustration caption",Margin=Padding.Empty};
        private bool exampleLayoutActive;
        private readonly ThemeButton detailsToggle=new ThemeButton{Text="▸ Explanation & field hints",AccessibleName="Toggle example explanation",Height=30,Tag="quiet",TextAlign=ContentAlignment.MiddleLeft};
        private ActionShowcase showcase;
        private ThemeTabs editorTabs;
        private readonly ThemeButton pauseExample=new ThemeButton{Text="Pause",AccessibleName="Pause or play example animation",Width=85,Height=30,Tag="quiet",Margin=new Padding(5,0,0,0)};
        private readonly ThemeButton replayExample=new ThemeButton{Text="Replay",AccessibleName="Replay example animation",Width=80,Height=30,Tag="quiet",Margin=new Padding(5,0,0,0)};
        private Button saveStepButton;
        private readonly ThemeButton guideBack=new ThemeButton{Text="Back to settings",AccessibleName="Back from visual guide",Tag="primary",Width=145,Height=35,Visible=false};
        private readonly ThemeButton outcomeJump=new ThemeButton{Text="View outcome ↓",AccessibleName="Jump to example outcome",Width=150,Height=30,Tag="quiet",Margin=new Padding(5,0,0,0)};
        private bool detailsOpen;
        private RowStyle exampleRow;
        private bool exampleOpen;
        private readonly PropertyGrid advanced=new PropertyGrid();
        private readonly System.Collections.Generic.List<Action> apply=new System.Collections.Generic.List<Action>();
        private readonly ToolTip tips=new ToolTip();
        private CancellationTokenSource testCancellation;
        private CancellationTokenSource captureCancellation;
        private bool captureActive;
        private readonly string directory;
        private readonly Func<OpenFileDialog,string> chooseLaunchFile;
        public StepEditor(Step step,string directory,Func<OpenFileDialog,string> launchChooser=null)
        {
            chooseLaunchFile=launchChooser??(dialog=>dialog.ShowDialog(this)==DialogResult.OK?dialog.FileName:null);
            Value=step.Clone();if(Value.Relative&&Value.Pointer==PointerMode.Absolute){Value.Pointer=PointerMode.ForegroundWindow;Value.Relative=false;}if(Value.SearchIn==SearchArea.Legacy)Value.SearchIn=Value.Width>0&&Value.Height>0?SearchArea.Rectangle:SearchArea.Desktop;this.directory=directory;Text=ActionArt.Name(step.Kind)+" — step settings";Size=new Size(920,860);MinimumSize=new Size(650,600);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(244,247,251);
            var root=new TableLayoutPanel{Tag="canvas",Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(22,12,22,16)};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,66));exampleRow=new RowStyle(SizeType.Absolute,40);root.RowStyles.Add(exampleRow);root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,66));Controls.Add(root);root.BringToFront();
            var heading=new TableLayoutPanel{Tag="canvas",Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};heading.RowStyles.Add(new RowStyle(SizeType.Absolute,30));heading.RowStyles.Add(new RowStyle(SizeType.Percent,100));heading.Controls.Add(new Label{Text=ActionArt.Name(step.Kind),Dock=DockStyle.Fill,Font=new Font("Segoe UI",16,FontStyle.Bold),Tag="heading"},0,0);heading.Controls.Add(new Label{Text=Hint(step.Kind),Dock=DockStyle.Fill,Tag="muted"},0,1);root.Controls.Add(heading,0,0);
            examplePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));examplePanel.RowStyles.Add(new RowStyle(SizeType.Absolute,36));examplePanel.RowStyles.Add(new RowStyle(SizeType.Percent,100));examplePanel.Controls.Add(exampleToggle,0,0);examplePanel.Controls.Add(exampleHost,0,1);BuildVisualExample();root.Controls.Add(examplePanel,0,1);
            exampleText.Text=ActionExamples.For(Value.Kind);exampleToggle.Click+=delegate{ToggleExample(!exampleOpen);};
            var tabs=new ThemeTabs{Dock=DockStyle.Fill};editorTabs=tabs;root.Controls.Add(tabs,0,2);var guided=new TabPage("Settings");var extra=new TabPage("Advanced fields");tabs.TabPages.Add(guided);tabs.TabPages.Add(extra);
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};guided.Controls.Add(scroll);fields.Dock=DockStyle.Top;fields.AutoSize=true;fields.ColumnCount=2;fields.Padding=new Padding(12);fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroll.Controls.Add(fields);
            advanced.Dock=DockStyle.Fill;advanced.SelectedObject=Value;var advancedLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Tag="canvas"};advancedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));advancedLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,36));advancedLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));var advancedExample=new ThemeButton{Text="Example for selected property",AccessibleName="Example for selected property",Dock=DockStyle.Fill,Tag="quiet",TextAlign=ContentAlignment.MiddleLeft};advancedExample.Click+=delegate{ShowExample(advanced.SelectedGridItem==null?null:advanced.SelectedGridItem.Label);};advancedLayout.Controls.Add(advancedExample,0,0);advancedLayout.Controls.Add(advanced,0,1);extra.Controls.Add(advancedLayout);
            tabs.Selecting+=delegate(object sender,TabControlCancelEventArgs e){if(e.TabPage==extra){try{Apply();advanced.Refresh();}catch(Exception error){e.Cancel=true;validationMessage.Text=error.GetBaseException().Message;tips.SetToolTip(validationMessage,validationMessage.Text);}}};
            // Once advanced fields are opened they become the source of truth until returning to Settings.
            tabs.SelectedIndexChanged+=delegate{if(tabs.SelectedTab==guided)Rebuild();};
            Build();UiTheme.Apply(this);
            var bottom=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,280));bottom.Controls.Add(validationMessage,0,0);var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};bottom.Controls.Add(footer,1,0);root.Controls.Add(bottom,0,3);var ok=new ThemeButton{Text="Save step",Tag="primary",Width=120,Height=35,BackColor=Color.FromArgb(38,101,183),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};var cancel=new ThemeButton{Text="Cancel",Width=100,Height=35,DialogResult=DialogResult.Cancel};saveStepButton=ok;guideBack.Click+=delegate{ToggleExample(false);};footer.Controls.Add(guideBack);footer.Controls.Add(ok);footer.Controls.Add(cancel);CancelButton=cancel;
            ok.Click+=delegate{try{if(tabs.SelectedTab==guided)Apply();Storage.Validate(new Macro{Steps=new System.Collections.Generic.List<Step>{Value}});if(ImageTemplate.IsImage(Value.Kind)){if(!string.IsNullOrEmpty(Value.Target)&&!Path.IsPathRooted(Value.Target))Value.Target=Path.Combine(directory,Value.Target);if(string.IsNullOrEmpty(Value.ImageData)&&string.IsNullOrWhiteSpace(Value.Target))throw new InvalidDataException("Capture or import a reference image first.");ImageTemplate.Embed(Value);ImageTemplate.ValidateReferences(Value);}DialogResult=DialogResult.OK;Close();}catch(Exception error){validationMessage.Text=error.GetBaseException().Message;tips.SetToolTip(validationMessage,validationMessage.Text);}};
            UiTheme.Apply(this);
            FormClosing+=delegate{if(testCancellation!=null)testCancellation.Cancel();if(captureCancellation!=null)captureCancellation.Cancel();};
        }
        private void BuildVisualExample()
        {
            exampleHost.Controls.Add(exampleContent);
            var scenarios=exampleScenarios;
            string[] names=Value.Kind==ActionKind.Launch?new[]{"Program","Document","Script"}:Value.Kind==ActionKind.IfImage?new[]{"Image found","Image missing"}:new string[0];
            showcase=new ActionShowcase(Value.Kind){Margin=new Padding(0,3,0,5)};showcase.MouseUp+=delegate{pauseExample.Text=showcase.Paused?"Play":"Pause";};showcase.KeyUp+=delegate{pauseExample.Text=showcase.Paused?"Play":"Pause";};if(showcase.CanAnimate)exampleCaption.Text="Sample walkthrough · Click a numbered stage to inspect it · No real input";
            for(int i=0;i<names.Length;i++){int variant=i;var button=new ThemeButton{Text=names[i],AccessibleName="Example scenario: "+names[i],Height=30,Width=110,Tag=i==0?"selected":"quiet",Margin=new Padding(0,0,5,0)};button.Click+=delegate{showcase.Variant=variant;pauseExample.Text="Pause";foreach(Control option in scenarios.Controls){if(option==detailsToggle||option==outcomeJump||option==pauseExample||option==replayExample)continue;option.Tag=option==button?"selected":"quiet";option.Invalidate();}};scenarios.Controls.Add(button);}
            detailsToggle.Width=230;detailsToggle.Margin=new Padding(8,0,0,0);scenarios.Controls.Add(detailsToggle);scenarios.Controls.Add(pauseExample);scenarios.Controls.Add(replayExample);pauseExample.Visible=replayExample.Visible=showcase.CanAnimate;pauseExample.Click+=delegate{showcase.Paused=!showcase.Paused;pauseExample.Text=showcase.Paused?"Play":"Pause";};replayExample.Click+=delegate{showcase.Replay();pauseExample.Text="Pause";};scenarios.Controls.Add(outcomeJump);outcomeJump.Click+=delegate{exampleHost.AutoScrollPosition=new Point(0,showcase.Top+showcase.OutcomeStart);};scenarios.Visible=true;exampleContent.Controls.Add(scenarios);exampleContent.Controls.Add(showcase);
            exampleContent.Controls.Add(exampleCaption);
            exampleText.Dock=DockStyle.None;exampleText.Margin=Padding.Empty;exampleContent.Controls.Add(exampleText);
            detailsToggle.Click+=delegate{SetExampleDetails(!detailsOpen);};
            exampleHost.SizeChanged+=delegate{LayoutExampleContent();};exampleHost.VisibleChanged+=delegate{LayoutExampleContent();};exampleHost.HandleCreated+=delegate{LayoutExampleContent();};Resize+=delegate{showcase.RefreshPlayback();LayoutExampleContent();};LayoutExampleContent();
        }
        private void LayoutExampleContent()
        {
            if(exampleLayoutActive||showcase==null)return;exampleLayoutActive=true;
            try{
                // Do not depend on Visible/GetPreferredSize: this subtree starts collapsed.
                int width=Math.Max(180,exampleHost.ClientSize.Width-24);
                bool stacked=width<620;outcomeJump.Visible=stacked;
                int rows=1,lineWidth=0;foreach(Control button in exampleScenarios.Controls){if(button==outcomeJump&&!stacked||(button==pauseExample||button==replayExample)&&!showcase.CanAnimate)continue;int itemWidth=button.Width+button.Margin.Horizontal;if(lineWidth>0&&lineWidth+itemWidth>width){rows++;lineWidth=0;}lineWidth+=itemWidth;}
                int toolbarHeight=rows*36;
                exampleScenarios.SetBounds(0,0,width,toolbarHeight);
                showcase.Width=width;int artHeight=showcase.IllustrationHeight;
                showcase.SetBounds(0,toolbarHeight+5,width,artHeight);
                exampleCaption.SetBounds(0,showcase.Bottom+5,width,showcase.CanAnimate&&width<750?44:26);
                exampleText.SetBounds(4,exampleCaption.Bottom+5,Math.Max(1,width-8),150);
                exampleContent.Height=(detailsOpen?exampleText.Bottom:exampleCaption.Bottom)+8;
                exampleHost.AutoScrollMinSize=new Size(0,exampleContent.Height);
                exampleContent.PerformLayout();exampleScenarios.PerformLayout();showcase.Invalidate();
            }finally{exampleLayoutActive=false;}
        }
        private void SetExampleDetails(bool open){detailsOpen=open;exampleText.Visible=open;detailsToggle.Text=open?"▾ Explanation & field hints":"▸ Explanation & field hints";LayoutExampleContent();}
        private void ToggleExample(bool open)
        {
            exampleOpen=open;exampleHost.Visible=open;exampleHost.AutoScrollPosition=Point.Empty;
            var root=(TableLayoutPanel)examplePanel.Parent;root.SuspendLayout();exampleRow.SizeType=open?SizeType.Percent:SizeType.Absolute;exampleRow.Height=open?100:40;root.RowStyles[2].SizeType=open?SizeType.Absolute:SizeType.Percent;root.RowStyles[2].Height=open?0:100;if(editorTabs!=null)editorTabs.Visible=!open;if(saveStepButton!=null)saveStepButton.Visible=!open;guideBack.Visible=open;root.ResumeLayout(true);
            exampleToggle.Text=open?"← Back to settings   ·   Visual guide":"▸ How to use this action · Example";exampleToggle.AccessibleDescription=open?"Expanded. Press to hide the example.":"Collapsed. Press to show a usage example.";examplePanel.PerformLayout();exampleHost.PerformLayout();LayoutExampleContent();if(open){showcase.Replay();pauseExample.Text="Pause";exampleHost.AutoScrollPosition=Point.Empty;exampleHost.ScrollControlIntoView(exampleScenarios);}
        }
        private void ShowExample(string field)
        {
            exampleText.Text=string.IsNullOrWhiteSpace(field)?ActionExamples.For(Value.Kind):field+"\r\n\r\n"+ActionExamples.Field(Value.Kind,field)+"\r\n\r\nACTION EXAMPLE\r\n"+ActionExamples.For(Value.Kind);exampleText.Text=exampleText.Text.Replace("\r\n","\n").Replace("\n","\r\n");SetExampleDetails(true);ToggleExample(true);exampleText.SelectionStart=0;exampleText.ScrollToCaret();exampleContent.PerformLayout();exampleHost.ScrollControlIntoView(exampleText);
        }
        protected override bool ProcessCmdKey(ref Message message,Keys keyData){if(exampleOpen&&keyData==Keys.Escape){ToggleExample(false);return true;}return base.ProcessCmdKey(ref message,keyData);}
        private static string Hint(ActionKind kind)
        {
            if(kind==ActionKind.IfImage)return "Run the steps in this IF branch only when the reference is found. Nest more IF blocks inside it.";
            if(kind==ActionKind.ClickImage)return "Find the reference on screen, then click its center. No separate mouse click is needed.";
            if(kind==ActionKind.WaitImage)return "Wait until the reference appears before continuing.";
            if(kind==ActionKind.Repeat)return "All steps inside this block repeat together. Stop always cancels an infinite repeat.";
            if(kind==ActionKind.MouseClick)return "Choose where to click. Use Last image match inside an image IF branch.";
            return ActionCatalog.Describe(kind);
        }
        private void Row(string label,Control control,int height)
        {
            int row=fields.RowCount++;fields.RowStyles.Add(new RowStyle(SizeType.Absolute,height));if(!string.IsNullOrWhiteSpace(label))control.AccessibleName=label;var caption=new Label{UseMnemonic=false,Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
            if(!string.IsNullOrWhiteSpace(label)){var captionRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};captionRow.RowStyles.Add(new RowStyle(SizeType.Percent,100));captionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));captionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));captionRow.Controls.Add(caption,0,0);var info=new ThemeButton{Text="?",AccessibleName="Help: "+label,Dock=DockStyle.Fill,Tag="quiet",Margin=Padding.Empty};info.Click+=delegate{ShowExample(label);};tips.SetToolTip(info,ActionExamples.Field(Value.Kind,label));tips.SetToolTip(caption,ActionExamples.Field(Value.Kind,label));captionRow.Controls.Add(info,1,0);fields.Controls.Add(captionRow,0,row);}else fields.Controls.Add(caption,0,row);control.Dock=DockStyle.Fill;control.Margin=new Padding(3,5,3,5);Control surface=control;if(control is TextBoxBase||control is NumericUpDown){var box=control as TextBoxBase;if(box!=null)box.BorderStyle=BorderStyle.None;var numeric=control as NumericUpDown;if(numeric!=null)numeric.BorderStyle=BorderStyle.None;var frame=new FieldFrame{Dock=DockStyle.Fill,Margin=control.Margin};control.Margin=Padding.Empty;frame.Controls.Add(control);surface=frame;}fields.Controls.Add(surface,1,row);
        }
        private void TextField(string label,string value,Action<string> save,bool multiline)
        {
            var box=new TextBox{Text=value,Multiline=multiline,ScrollBars=multiline?ScrollBars.Vertical:ScrollBars.None};Row(label,box,multiline?115:40);apply.Add(delegate{save(box.Text);});
        }
        private void Number(string label,int value,int min,int max,Action<int> save)
        {
            var n=new NumericUpDown{Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),ThousandsSeparator=true};Row(label,n,40);apply.Add(delegate{save((int)n.Value);});
        }
        private DurationInput TimeField(string label,int value,Action<int> save,int min=0,int max=86400000,bool seconds=true)
        {
            var input=new DurationInput(value,min,max,seconds);Row(label,input,42);apply.Add(delegate{save(input.Milliseconds);});tips.SetToolTip(input,"Choose seconds or milliseconds. Values are stored exactly to the nearest millisecond.");return input;
        }
        private ThemeCheckBox Check(string label,bool value,Action<bool> save)
        {
            var check=new ThemeCheckBox{Text=label,Checked=value,AutoSize=true};Row("",check,36);var help=new ThemeButton{Text="?",AccessibleName="Help: "+label,Tag="quiet",Dock=DockStyle.Right,Width=30,Margin=Padding.Empty};help.Click+=delegate{ShowExample(label);};tips.SetToolTip(help,ActionExamples.Field(Value.Kind,label));var caption=fields.GetControlFromPosition(0,fields.RowCount-1);fields.Controls.Remove(caption);caption.Dispose();var helpHost=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};helpHost.Controls.Add(help);fields.Controls.Add(helpHost,0,fields.RowCount-1);apply.Add(delegate{save(check.Checked);});return check;
        }
        private FlowLayoutPanel Choices(string label,string[] labels,int value,Action<int> save)
        {
            var panel=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false};var radios=new RadioButton[labels.Length];for(int i=0;i<labels.Length;i++){radios[i]=new ThemeRadioButton{Text=labels[i],AutoSize=false,Height=32,Width=400,Checked=i==value,Margin=new Padding(0,1,0,1)};panel.Controls.Add(radios[i]);}panel.SizeChanged+=delegate{foreach(Control radio in panel.Controls)radio.Width=Math.Max(120,panel.ClientSize.Width-4);};Row(label,panel,labels.Length*34+12);apply.Add(delegate{for(int i=0;i<radios.Length;i++)if(radios[i].Checked){save(i);break;}});return panel;
        }
        private void Build()
        {
            var k=Value.Kind;
            if(k==ActionKind.MouseClick||k==ActionKind.ClickImage){Section("Click behavior");ClickModeFields();}
            if(ImageTemplate.IsImage(k)){Section("Reference image and search area");
                var panel=new TableLayoutPanel{ColumnCount=1,RowCount=3};panel.RowStyles.Add(new RowStyle(SizeType.Absolute,112));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,28));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,38));preview.Dock=DockStyle.Fill;preview.SizeMode=PictureBoxSizeMode.Zoom;preview.BackColor=Color.FromArgb(232,237,245);panel.Controls.Add(preview,0,0);imageInfo.Dock=DockStyle.Fill;panel.Controls.Add(imageInfo,0,1);
                var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};panel.Controls.Add(buttons,0,2);Button(buttons,"Capture screen",async delegate{await CaptureReference();});Button(buttons,"Import image",delegate{Import();});Button(buttons,"Test find",async delegate{await TestImage();});Row("Reference image",panel,190);UpdatePreview();MultiImageFields();
                var areaChoices=Choices("Search area",new[]{"Entire desktop","Foreground window","Selected rectangle"},Math.Max(0,(int)Value.SearchIn-1),delegate(int n){Value.SearchIn=(SearchArea)(n+1);});
                var selectArea=new ThemeButton{Text="Select search area on screen…"};selectArea.Click+=async delegate{await CaptureRegion(true);};Row("",selectArea,42);
                int rectangleStart=fields.RowCount;Number("Rectangle X",Value.X,-100000,100000,delegate(int n){Value.X=n;});Number("Rectangle Y",Value.Y,-100000,100000,delegate(int n){Value.Y=n;});Number("Width (pixels)",Value.Width,0,32768,delegate(int n){Value.Width=n;});Number("Height (pixels)",Value.Height,0,32768,delegate(int n){Value.Height=n;});
                int rectangleEnd=fields.RowCount;Action showRectangle=delegate{bool show=((RadioButton)areaChoices.Controls[2]).Checked;for(int row=rectangleStart;row<rectangleEnd;row++){fields.RowStyles[row].Height=show?40:0;for(int column=0;column<2;column++)fields.GetControlFromPosition(column,row).Visible=show;}};foreach(RadioButton radio in areaChoices.Controls)radio.CheckedChanged+=delegate{showRectangle();};showRectangle();
                Section("Match accuracy");Choices("Tolerance interpretation",new[]{"Exact RGB threshold (recommended)","Legacy RGB threshold + 1"},(int)Value.ToleranceMode,delegate(int n){Value.ToleranceMode=(ImageToleranceMode)n;});Number("Color tolerance",Value.Tolerance,0,255,delegate(int n){Value.Tolerance=n;});Row("",new Label{Text="0 = exact RGB. Transparent pixels are ignored. Match at the same display scale.",ForeColor=Color.DimGray},48);
                Section("Waiting and result");ThemeCheckBox waitBefore=null;if(k==ActionKind.IfImage){waitBefore=Check("Wait before deciding the IF branch",Value.WaitForMatch,delegate(bool v){Value.WaitForMatch=v;});Check("Invert: IF image is NOT found",Value.Negate,delegate(bool v){Value.Negate=v;});}
                var timeout=TimeField("Timeout",Value.TimeoutMs,delegate(int n){Value.TimeoutMs=n;});var indefinitely=Check("Wait forever until Stop",Value.WaitForever,delegate(bool v){Value.WaitForever=v;});var polling=TimeField("Check every",Value.PollMs,delegate(int n){Value.PollMs=n;},10,60000,false);Action updateWait=delegate{bool waits=k==ActionKind.WaitImage||k==ActionKind.ClickImage||(waitBefore!=null?waitBefore.Checked:Value.WaitForMatch)||indefinitely.Checked;timeout.Enabled=waits&&!indefinitely.Checked;polling.Enabled=waits;};if(waitBefore!=null)waitBefore.CheckedChanged+=delegate{updateWait();};indefinitely.CheckedChanged+=delegate{updateWait();};updateWait();
                if(k!=ActionKind.IfImage)Check("If missing: skip action and continue",Value.ContinueIfMissing,delegate(bool v){Value.ContinueIfMissing=v;});
                Check("Move pointer to successful match",Value.MoveToMatch,delegate(bool v){Value.MoveToMatch=v;});Number("Match offset X",Value.MatchOffsetX,-32768,32768,delegate(int n){Value.MatchOffsetX=n;});Number("Match offset Y",Value.MatchOffsetY,-32768,32768,delegate(int n){Value.MatchOffsetY=n;});
                if(k==ActionKind.ClickImage)ChooseMouseButton();
            }else if(k==ActionKind.MouseWheel){Section("Scroll at the current pointer");Number("Wheel delta",Value.WheelDelta,-12000,12000,delegate(int n){Value.WheelDelta=n;});Row("",new Label{Text="120 scrolls up one notch; -120 scrolls down. Move the pointer first if the target area is elsewhere.",Tag="muted"},60);}else if(k.ToString().StartsWith("Mouse")){Section("Pointer location");
                var pointerChoices=Choices("Click / move at",new[]{"Absolute screen coordinates","Current pointer (do not move)","Last image / pixel match + offset","Foreground window + coordinates","Current pointer + offset"},(int)Value.Pointer,delegate(int n){Value.Pointer=(PointerMode)n;Value.Relative=false;});
                int coordinatesFirst=fields.RowCount;Number("X / horizontal offset",Value.X,-100000,100000,delegate(int n){Value.X=n;});Number("Y / vertical offset",Value.Y,-100000,100000,delegate(int n){Value.Y=n;});
                int coordinatesLast=fields.RowCount;Action coordinateState=delegate{bool useCoordinates=!((RadioButton)pointerChoices.Controls[1]).Checked;for(int row=coordinatesFirst;row<coordinatesLast;row++)fields.GetControlFromPosition(1,row).Enabled=useCoordinates;};foreach(RadioButton radio in pointerChoices.Controls)radio.CheckedChanged+=delegate{coordinateState();};coordinateState();
                var pick=new ThemeButton{Text="Pick pointer position in 3 seconds"};pick.Click+=async delegate{if(captureActive)return;captureActive=true;pick.Enabled=false;try{ApplyDraft();using(new CaptureVisibility(this)){await Task.Delay(3000);Native.Point p;if(!Native.GetCursorPos(out p))throw new InvalidOperationException("Cannot read the pointer position.");Value.X=p.X;Value.Y=p.Y;Value.Pointer=PointerMode.Absolute;Value.Relative=false;}Rebuild();}catch(Exception error){InAppDialog.Message(this,error.GetBaseException().Message);}finally{captureActive=false;if(!pick.IsDisposed)pick.Enabled=true;}};Row("",pick,42);
                if(k==ActionKind.MouseWheel)Number("Wheel delta",Value.WheelDelta,-12000,12000,delegate(int n){Value.WheelDelta=n;});else if(k!=ActionKind.MouseMove)ChooseMouseButton();
            }else if(k==ActionKind.Delay){TimeField("Wait duration",Value.DelayMs,delegate(int n){Value.DelayMs=n;});TimeField("Random maximum (0=off)",Value.DelayMaxMs,delegate(int n){Value.DelayMaxMs=n;});}
            else if(k==ActionKind.Repeat){ExtraFields(k);}
            else if(k==ActionKind.KeyPress||k==ActionKind.KeyDown||k==ActionKind.KeyUp){var key=new TextBox{ReadOnly=true,Text=((Keys)Value.KeyCode).ToString()};key.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Modifiers!=Keys.None&&!Shortcuts.IsModifier(e.KeyCode)){validationMessage.Text="Use Keyboard shortcut in the action library for key combinations.";e.SuppressKeyPress=true;return;}validationMessage.Text="";Value.KeyCode=(int)e.KeyCode;Value.Extended=KeyboardChord.Extended(e.KeyCode);key.Text=e.KeyCode.ToString();e.SuppressKeyPress=true;};Row("Press a key here",key,42);Row("",new Label{Text="For Ctrl/Shift/Alt combinations, use Keyboard shortcut in the action library."},60);}
            else if(k==ActionKind.TypeText||k==ActionKind.Note||k==ActionKind.Message||k==ActionKind.SetClipboard||k==ActionKind.RunCSharp)TextField("Text",Value.Text,delegate(string text){Value.Text=text;},true);
            else if(k==ActionKind.IfClipboard)ClipboardFields();
            else if(k==ActionKind.CopySelection)CopyFields();
            else if(k==ActionKind.ActivateWindow||k==ActionKind.IfWindow||k==ActionKind.WaitWindow||k==ActionKind.Label||k==ActionKind.Jump||k==ActionKind.IfProcess||k==ActionKind.WaitProcess||k==ActionKind.TypeDate)TextField(k==ActionKind.ActivateWindow||k==ActionKind.IfWindow||k==ActionKind.WaitWindow?"Window title contains":k==ActionKind.Label||k==ActionKind.Jump?"Label name":k==ActionKind.IfProcess||k==ActionKind.WaitProcess?"Process name":k==ActionKind.TypeDate?"Date format":"Text / name",Value.Text,delegate(string text){Value.Text=text;},false);
            else if(k==ActionKind.Include){
                var path=new TextBox{Text=Value.Target,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,AccessibleName="Included workflow path"};
                var frame=new FieldFrame{Dock=DockStyle.Fill};frame.Controls.Add(path);
                var panel=new TableLayoutPanel{ColumnCount=2,RowCount=1};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.Controls.Add(frame,0,0);
                var browse=new ThemeButton{Text="Browse…",AccessibleName="Browse included workflow",Dock=DockStyle.Fill};panel.Controls.Add(browse,1,0);
                browse.Click+=delegate{using(var file=WorkflowFiles.OpenDialog()){file.InitialDirectory=directory;if(file.ShowDialog(this)==DialogResult.OK)path.Text=file.FileName;}};
                Row("MacroExecute file",panel,42);apply.Add(delegate{Value.Target=path.Text;});
                Row("",new Label{Text="Choose a .mexec workflow. Older .mmacro.json and JSON workflows also work, including nested includes.",Tag="muted"},55);
            }
            else if(k==ActionKind.Launch){LaunchFields();}
            else if(k==ActionKind.OpenUrl||k==ActionKind.IfFile||k==ActionKind.DeleteFile||k==ActionKind.TypeEnvironment){TextField(k==ActionKind.OpenUrl?"Website URL":k==ActionKind.TypeEnvironment?"Environment variable":"File path",Value.Target,delegate(string text){Value.Target=text;},false);}
            else if(ExtraFields(k)){}else Row("",new Label{Text="Use Advanced fields to configure this action.",Dock=DockStyle.Fill},55);
            if(k==ActionKind.TypeText||k==ActionKind.TypeDate||k==ActionKind.TypeEnvironment){Choices("Text entry method",new[]{"Unicode typing (keeps clipboard untouched)","Paste text, then restore clipboard"},(int)Value.TextEntry,delegate(int n){Value.TextEntry=(TextEntryMode)n;});TimeField("Between characters / paste settle",Value.TextIntervalMs,delegate(int n){Value.TextIntervalMs=n;},0,60000,false);Row("",new Label{Text="Paste restores the clipboard only if it is unchanged. Some targets need a longer settle time. Unicode sends Enter for line breaks and Tab for tabs."},68);}
            if(k==ActionKind.MouseClick||k==ActionKind.ClickImage){Section("Click timing");ClickTimingFields();}
            if(k==ActionKind.KeyPress||k==ActionKind.Paste||k==ActionKind.CopySelection)TimeField("Button / key hold",Value.InputHoldMs,delegate(int n){Value.InputHoldMs=n;},0,60000,false);
            if(k==ActionKind.WaitWindow||k==ActionKind.WaitProcess||k==ActionKind.RunCSharp)TimeField("Timeout",Value.TimeoutMs,delegate(int n){Value.TimeoutMs=n;});
            Section("Step status");Check("Enable this action",Value.Enabled,delegate(bool v){Value.Enabled=v;});
        }
        private void ChooseMouseButton(){Number("Random offset radius",Value.PositionJitter,0,32768,delegate(int n){Value.PositionJitter=n;});Choices("Mouse button",new[]{"Left","Right","Middle"},(int)Value.Button,delegate(int n){Value.Button=(MouseButton)n;});}
        private void Button(Control panel,string text,EventHandler action){var b=new ThemeButton{Text=text,AutoSize=true,Height=30};b.Click+=action;panel.Controls.Add(b);}
        private void Apply(){foreach(var action in apply)action();if(Value.SearchIn==SearchArea.Rectangle&&(Value.Width==0||Value.Height==0))throw new InvalidDataException("A rectangle search needs a positive width and height.");}
        private void LaunchFields()
        {
            var types=new LaunchTypeBox{DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Launch file type",DropDownWidth=650,FlatStyle=FlatStyle.Flat,DrawMode=DrawMode.OwnerDrawFixed};
            types.DrawItem+=delegate(object sender,DrawItemEventArgs e){if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;using(var fill=new SolidBrush(selected?UiTheme.Selected:UiTheme.Field))e.Graphics.FillRectangle(fill,e.Bounds);TextRenderer.DrawText(e.Graphics,types.Items[e.Index].ToString(),types.Font,e.Bounds,UiTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);e.DrawFocusRectangle();};
            for(int i=0;i<LaunchFiles.Types.Length;i++)types.Items.Add(LaunchFiles.Label(i));types.SelectedIndex=LaunchFiles.TypeFor(Value.Target);
            var path=new TextBox{Text=Value.Target,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,AccessibleName="Program or file path"};
            var line=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};line.RowStyles.Add(new RowStyle(SizeType.Percent,100));line.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));line.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,96));
            var frame=new FieldFrame{Dock=DockStyle.Fill,Margin=Padding.Empty};frame.Controls.Add(path);line.Controls.Add(frame,0,0);
            var browse=new ThemeButton{Text="Browse…",AccessibleName="Browse program or file",Dock=DockStyle.Fill,Tag="primary",Margin=new Padding(5,0,0,0)};line.Controls.Add(browse,1,0);Row("Program or file",line,48);Row("File type",types,42);
            browse.Click+=delegate{using(var dialog=LaunchFiles.Dialog(types.SelectedIndex,path.Text,directory)){string chosen=chooseLaunchFile(dialog);if(chosen!=null){path.Text=chosen;types.SelectedIndex=LaunchFiles.TypeFor(chosen);}}};
            Row("",new Label{Text="Choose a program, document or script. Documents open with their Windows default app.",Tag="muted"},50);
            var extras=new ThemeButton{Text="▸ Extra options · Command-line arguments",AccessibleName="Toggle launch arguments",Tag="quiet",TextAlign=ContentAlignment.MiddleLeft};Row("",extras,40);
            int argumentRow=fields.RowCount;TextField("Arguments",Value.Text,delegate(string text){Value.Text=text;},false);
            bool expanded=!string.IsNullOrEmpty(Value.Text);Action showArguments=delegate{fields.RowStyles[argumentRow].Height=expanded?40:0;for(int column=0;column<2;column++)fields.GetControlFromPosition(column,argumentRow).Visible=expanded;extras.Text=expanded?"▾ Extra options · Command-line arguments":"▸ Extra options · Command-line arguments";};extras.Click+=delegate{expanded=!expanded;showArguments();};showArguments();
            apply.Add(delegate{if(string.IsNullOrWhiteSpace(path.Text))throw new InvalidDataException("Choose a program or file, or enter a program name.");Value.Target=path.Text.Trim();});
        }
        private void UpdatePreview(){if(preview.Image!=null){preview.Image.Dispose();preview.Image=null;}try{var copy=Value.Clone();if(!string.IsNullOrEmpty(copy.Target)&&!Path.IsPathRooted(copy.Target))copy.Target=Path.Combine(directory,copy.Target);preview.Image=ImageTemplate.Load(copy);imageInfo.Text=preview.Image.Width+" × "+preview.Image.Height+" px • stored inside this macro";}catch{imageInfo.Text="No reference yet — capture or import an image.";}}
        private void Import(){using(var file=new OpenFileDialog{Filter="Image files|*.png;*.bmp;*.jpg;*.jpeg;*.gif|All files|*.*"})if(file.ShowDialog(this)==DialogResult.OK){try{Value.Target=file.FileName;Value.ImageData="";ImageTemplate.Embed(Value);UpdatePreview();}catch(Exception e){InAppDialog.Message(this,e.Message);}}}
        private void Rebuild(){fields.SuspendLayout();try{if(preview.Parent!=null)preview.Parent.Controls.Remove(preview);if(imageInfo.Parent!=null)imageInfo.Parent.Controls.Remove(imageInfo);var retired=new Control[fields.Controls.Count];fields.Controls.CopyTo(retired,0);foreach(Control control in retired)control.Dispose();fields.Controls.Clear();fields.RowStyles.Clear();fields.RowCount=0;apply.Clear();Build();}finally{fields.ResumeLayout(true);UiTheme.Apply(this);}}
        // Preserve edits even when Rectangle has been selected but not drawn yet.
        private void ApplyDraft(){foreach(var action in apply)action();}
        private Task CaptureReference(){return CaptureRegion(false);}
        private async Task CaptureRegion(bool searchArea)
        {
            if(captureActive||testCancellation!=null)return;captureActive=true;captureCancellation=new CancellationTokenSource();
            try{
                ApplyDraft();
                using(new CaptureVisibility(this)){
                    await Task.Delay(300,captureCancellation.Token);
                    using(var capture=new CaptureForm(searchArea)){
                        if(capture.ShowDialog(this)!=DialogResult.OK)return;
                        if(searchArea)capture.ApplySearchArea(Value);
                        else using(var bitmap=capture.SelectedBitmap()){Value.ImageData=ImageTemplate.Encode(bitmap);Value.Target="";}
                    }
                }
                Rebuild();
            }catch(OperationCanceledException){}catch(Exception error){if(!IsDisposed)InAppDialog.Message(this,error.GetBaseException().Message,"Screen selection",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            finally{captureCancellation.Dispose();captureCancellation=null;captureActive=false;}
        }
        private async Task TestImage()
        {
            if(captureActive||testCancellation!=null)return;
            try{Apply();testCancellation=new CancellationTokenSource();Point? point;
                using(new CaptureVisibility(this)){await Task.Delay(350,testCancellation.Token);var session=new ImageSession(new ImageDesktop());point=await Task.Run(delegate{return session.SearchOnce(Value,testCancellation.Token);});}
                InAppDialog.Message(this,point.HasValue?"Found at screen position "+point.Value.X+", "+point.Value.Y+". No click was sent.":"Image not found. Try a smaller reference, the correct search area, or a slightly higher tolerance.","Test find");
            }catch(OperationCanceledException){}catch(Exception error){InAppDialog.Message(this,error.GetBaseException().Message);}finally{if(testCancellation!=null)testCancellation.Dispose();testCancellation=null;}
        }
        protected override void Dispose(bool disposing){if(disposing){tips.Dispose();if(preview.Image!=null)preview.Image.Dispose();}base.Dispose(disposing);}
    }
}
