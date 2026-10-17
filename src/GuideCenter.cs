using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    // Reference-only workspace: never constructs, executes or changes a workflow.
    internal sealed class GuideCenter : ThemeWindow
    {
        private readonly TextBox search=new TextBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,AccessibleName="Search guides"};
        private readonly ListBox topics=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,IntegralHeight=false,AccessibleName="Guide topics",Font=new Font("Segoe UI",10)};
        private readonly Label title=new Label{Dock=DockStyle.Fill,Font=new Font("Segoe UI",18,FontStyle.Bold),Tag="heading"};
        private readonly Label subtitle=new Label{Dock=DockStyle.Fill,Tag="muted"};
        private readonly Panel viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Tag="canvas",AccessibleName="Guide reading area"};
        private readonly Panel content=new Panel{Dock=DockStyle.Top,Tag="canvas"};
        private readonly Label explanation=new Label{AccessibleName="Guide instructions",Font=new Font("Segoe UI",11),UseMnemonic=false,Tag="muted"};
        private readonly FlowLayoutPanel controls=new FlowLayoutPanel{Height=40,Tag="canvas",WrapContents=true,AccessibleName="Guide playback controls"};
        private ActionShowcase illustration;
        private RecipePage recipePage;
        private bool layingOut;
        private ThemeButton playbackButton;
        private readonly Label resultCount=new Label{Dock=DockStyle.Fill,Tag="muted",AccessibleName="Guide result count"};
        private readonly ThemeButton clearSearch=new ThemeButton{Text="Clear",Dock=DockStyle.Fill,Tag="quiet",AccessibleName="Clear guide search"};
        private readonly FlowLayoutPanel related=new FlowLayoutPanel{Tag="canvas",WrapContents=true,AccessibleName="Related action guides"};
        private readonly ToolTip guideTips=new ToolTip{AutoPopDelay=12000};
        private sealed class Topic
        {
            internal string Name;internal ActionKind? Kind;internal string Text;
            public override string ToString(){return Name;}
        }
        private static readonly Topic[] recipes={
            new Topic{Name="Recipe · Alt-click and other mouse combinations",Text="Hold a modifier in one step\r\nOpen Mouse click and tick Alt under Hold during click. Ctrl and Shift can be selected separately or combined. Choose a left, right or middle button and the usual pointer location.\r\n\r\nDouble or triple click\r\nChoose the Click pattern, then set Between clicks if needed. Modifiers stay held across all clicks. The target app determines what these gestures do.\r\n\r\nPress and hold\r\nLong press presets a single 750 ms hold without changing modifiers or position. Normal press returns the hold to 30 ms. You can enter your own hold duration.\r\n\r\nImage clicks\r\nThe same options are available on Click image. Modifiers are pressed after the reference is found, for the click itself. Use explicit key-down/up steps if a modifier must already be held during the image search or across a drag."},
            new Topic{Name="Recipe · Selected report check",Text="Clear, hover, copy\r\nSelect report text in a text editor. Add Clear clipboard, then Copy to clipboard (Ctrl+C). Keep Wait for fresh clipboard text enabled.\r\n\r\nCheck the strings\r\nAdd IF clipboard matches. Enter \"Monthly Report\" in one string row. Use + → AND for \"Status: Approved\". Spaces mean OR alternatives, while quotes keep phrases together. Each AND row requires a different matching string. Use + → GROUP OR for an alternative complete combination.\r\n\r\nRun or skip\r\nPlace actions after IF to run on a match. Leave ELSE empty to skip. Add actions after ELSE if you want different behavior when the item does not match. Copy sends Ctrl+C; Paste sends Ctrl+V.\r\n\r\nTry it safely\r\nThe condition editor has a local sample-text tester with this item example. It does not touch the system clipboard or run actions. Open examples/clipboard-item-check.mexec; select the sample report text before running."},
            new Topic{Name="Start here · Your first workflow",Text="1   Capture or build\r\nPress Record, perform a short task, then press your Stop shortcut. Or choose an action icon to add a step manually.\r\n\r\n2   Refine\r\nDouble-click a row to edit it. Drag a row to reorder it. Select an IF or Repeat header to move the entire block. Before / After controls where the next action is inserted.\r\n\r\n3   Review, save, run\r\nChoose Check macro to find missing files or invalid blocks. Save as .mexec, then try one run. Increase Runs or choose Forever once it behaves as expected.\r\n\r\nSearch the topics on the left for setup-and-outcome examples. These illustrations never send input or run a macro."},
            new Topic{Name="Recipe · Click a changing button",Text="Use Click image when a button moves around.\r\n\r\n1   Add Click image. Capture a small, distinctive part of the button.\r\n2   Select a search area. A specific window or rectangle avoids matching similar buttons elsewhere.\r\n3   Use Test find to check the reference before running. Match the display scale used when capturing. Start with exact matching and raise tolerance only as needed.\r\n4   Save the step and run once. Click image finds and clicks the match automatically.\r\n\r\nUse IF image found instead when you need a decision. That condition does not click anything on its own."},
            new Topic{Name="Recipe · Nested image decisions",Text="Build this structure using action icons and the Before / After controls:\r\n\r\nIF image found: Login\r\n    IF image found: Warning\r\n        Add your recovery actions here\r\n    ELSE\r\n        Add your normal actions here\r\n    END IF\r\nELSE\r\n    Add actions for the missing Login button\r\nEND IF\r\n\r\nSelect the outer IF before adding the inner IF with After selected. Select the correct IF or ELSE row before adding its actions. The hint above the list names the current branch.\r\n\r\nColored guide lines show nesting. Go to ELSE, Go to end and Parent block navigate a large workflow. Inner image results are scoped; the outer match is restored afterwards."},
            new Topic{Name="Recipe · Repeat and reuse workflows",Text="Whole workflow\r\nRuns repeats the entire file. Forever keeps repeating until Stop. Start with one run while setting up your task.\r\n\r\nOnly part of a workflow\r\nAdd Repeat, then insert actions between its header and END REPEAT. Count 0 repeats until Stop or Break. Break exits the nearest Repeat.\r\n\r\nReuse another file\r\nAdd Include / run macro and select a .mexec file. Included workflows can include other workflows. Keep related files together so relative paths remain portable. Cyclic includes are rejected.\r\n\r\nUse Check macro before playback to catch missing references. Export player creates a separate runnable player; saving a workflow alone does not export one."},
            new Topic{Name="Troubleshooting · Images and timing",Text="Image not found?\r\nRecapture at the target display scale. Avoid changing text, animations and large blank backgrounds in the reference. Confirm that the target is visible inside the chosen search area. Test find checks your current settings.\r\n\r\nClick happens too early?\r\nUse Wait image or Wait window for a readiness signal instead of guessing a long delay. A Delay is useful when the app has no stable signal.\r\n\r\nText goes into the wrong place?\r\nActivate the intended window, then click its input field before typing. Keyboard actions go to the focused application.\r\n\r\nNeed a different shortcut?\r\nSettings → Keyboard shortcuts remaps app actions. Workflow shortcuts launch saved files. The current toolbar labels show your assigned playback keys.\r\n\r\nSearch this guide for a specific action to see its example and field explanations."}
        };
        private void DrawTopic(object sender,DrawItemEventArgs e)
        {
            if(e.Index<0)return;
            bool selected=(e.State&DrawItemState.Selected)!=0;
            using(var fill=new SolidBrush(UiTheme.Surface))e.Graphics.FillRectangle(fill,e.Bounds);
            var row=Rectangle.Inflate(e.Bounds,-2,-2);
            using(var path=SoftGeometry.Round(row,8))using(var fill=new SolidBrush(selected?UiTheme.Selected:UiTheme.Surface))e.Graphics.FillPath(fill,path);
            var topic=(Topic)topics.Items[e.Index];string name=topic.Name.Replace("Recipe · ","").Replace("Troubleshooting · ","");
            TextRenderer.DrawText(e.Graphics,topic.Kind.HasValue?"◇":"▤",topics.Font,new Rectangle(row.X+8,row.Y+12,22,24),UiTheme.Accent,TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics,name,topics.Font,new Rectangle(row.X+34,row.Y+5,row.Width-42,23),selected?UiTheme.Accent:UiTheme.Ink,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            using(var small=new Font("Segoe UI",8.5f))TextRenderer.DrawText(e.Graphics,topic.Kind.HasValue?"Action example":topic.Name.StartsWith("Troubleshooting")?"Troubleshooting":"Practical recipe",small,new Rectangle(row.X+34,row.Y+27,row.Width-42,20),UiTheme.Muted,TextFormatFlags.NoPrefix);
            if((e.State&DrawItemState.Focus)!=0)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(row,-2,-2),UiTheme.Ink,selected?UiTheme.Selected:UiTheme.Surface);
        }
        internal GuideCenter(ActionKind? initial=null)
        {
            topics.DrawMode=DrawMode.OwnerDrawFixed;topics.ItemHeight=52;topics.DrawItem+=DrawTopic;Text="MacroExecute — Guides";Size=new Size(1180,850);MinimumSize=new Size(940,650);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20,14,20,16),ColumnCount=2,RowCount=2,Tag="canvas"};root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,260));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,44));Controls.Add(root);root.BringToFront();
            var sidebar=new RoundedLayout{Dock=DockStyle.Fill,Padding=new Padding(12),RowCount=5,ColumnCount=1};sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,32));sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,24));sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,40));sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,28));sidebar.RowStyles.Add(new RowStyle(SizeType.Percent,100));sidebar.Controls.Add(new Label{Text="Learn MacroExecute",Dock=DockStyle.Fill,Font=new Font("Segoe UI",12,FontStyle.Bold),Tag="heading"},0,0);sidebar.Controls.Add(new Label{Text="Search guides",Dock=DockStyle.Fill,Tag="muted"},0,1);var frame=new FieldFrame{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,6)};frame.Controls.Add(search);var searchRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,60));searchRow.Controls.Add(frame,0,0);searchRow.Controls.Add(clearSearch,1,0);sidebar.Controls.Add(searchRow,0,2);sidebar.Controls.Add(resultCount,0,3);clearSearch.Click+=delegate{search.Clear();search.Focus();};sidebar.Controls.Add(topics,0,4);root.Controls.Add(sidebar,0,0);
            var reader=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(16,4,0,0),Tag="canvas"};reader.RowStyles.Add(new RowStyle(SizeType.Absolute,42));reader.RowStyles.Add(new RowStyle(SizeType.Absolute,52));reader.RowStyles.Add(new RowStyle(SizeType.Percent,100));reader.Controls.Add(title,0,0);reader.Controls.Add(subtitle,0,1);reader.Controls.Add(viewport,0,2);viewport.Controls.Add(content);root.Controls.Add(reader,1,0);
            var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Tag="canvas"};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));footer.Controls.Add(new Label{Text="Reference examples only · Your workflow stays unchanged",Dock=DockStyle.Fill,Tag="muted",TextAlign=ContentAlignment.MiddleLeft},0,0);var close=new ThemeButton{Text="Close guide",Dock=DockStyle.Fill,DialogResult=DialogResult.Cancel,Tag="primary"};footer.Controls.Add(close,1,0);root.Controls.Add(footer,0,1);root.SetColumnSpan(footer,2);CancelButton=close;
            topics.SelectedIndexChanged+=delegate{ReadTopic();};topics.MouseMove+=delegate(object sender,MouseEventArgs e){int index=topics.IndexFromPoint(e.Location);string text=index<0?null:((Topic)topics.Items[index]).Name;if(guideTips.GetToolTip(topics)!=text)guideTips.SetToolTip(topics,text);};guideTips.SetToolTip(search,"Search action names, field hints and example values. Ctrl+F focuses search; Escape clears it.");search.TextChanged+=delegate{Filter();};search.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Down&&topics.Items.Count>0){topics.Focus();topics.SelectedIndex=0;e.SuppressKeyPress=true;}if(e.KeyCode==Keys.Escape&&search.Text.Length>0){search.Clear();e.SuppressKeyPress=true;}};viewport.SizeChanged+=delegate{LayoutReading();};Resize+=delegate{if(illustration!=null)illustration.RefreshPlayback();};Filter();if(initial.HasValue)for(int i=0;i<topics.Items.Count;i++)if(((Topic)topics.Items[i]).Kind==initial){topics.SelectedIndex=i;break;}UiTheme.Apply(this);
        }
        private static bool Matches(string text,string[] words){return words.All(word=>text.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0);}
        private void Filter()
        {
            var previous=topics.SelectedItem as Topic;
            string[] words=search.Text.Split(new[]{' ','\t','\r','\n'},StringSplitOptions.RemoveEmptyEntries);
            topics.BeginUpdate();topics.Items.Clear();
            foreach(var recipe in recipes)if(Matches(recipe.Name+" "+recipe.Text,words))topics.Items.Add(recipe);
            foreach(ActionKind kind in Enum.GetValues(typeof(ActionKind)).Cast<ActionKind>().OrderBy(k=>ActionArt.Name(k))){string name=ActionArt.Name(kind);var scene=ShowcaseScene.For(kind,0);if(Matches(name+" "+kind+" "+ActionCatalog.Describe(kind)+" "+ActionExamples.For(kind)+" "+string.Join(" ",scene.Fields),words))topics.Items.Add(new Topic{Name=name,Kind=kind});}
            topics.EndUpdate();clearSearch.Enabled=search.Text.Length>0;resultCount.Text=topics.Items.Count+" guides"+(words.Length>0?" found":" · Recipes & actions");
            int selected=-1;if(previous!=null)for(int i=0;i<topics.Items.Count;i++){var candidate=(Topic)topics.Items[i];if(candidate.Name==previous.Name&&candidate.Kind==previous.Kind){selected=i;break;}}
            if(topics.Items.Count>0)topics.SelectedIndex=selected>=0?selected:0;
            else{ReadTopic();title.Text="No matching guides";subtitle.Text="Try image, tolerance, arguments, repeat, or text. Clear returns to all guides.";viewport.AutoScrollMinSize=Size.Empty;content.Height=0;}
        }
        private void SelectTopic(ActionKind kind)
        {
            search.Clear();for(int i=0;i<topics.Items.Count;i++)if(((Topic)topics.Items[i]).Kind==kind){topics.SelectedIndex=i;topics.Focus();break;}
        }
        private static ActionKind[] Related(ActionKind kind)
        {
            if(kind==ActionKind.IfClipboard||kind==ActionKind.CopySelection||kind==ActionKind.ClipboardClear)return new[]{ActionKind.ClipboardClear,ActionKind.CopySelection,ActionKind.IfClipboard}.Where(k=>k!=kind).ToArray();
            if(kind==ActionKind.IfImage)return new[]{ActionKind.MouseClick,ActionKind.ClickImage,ActionKind.WaitImage};
            if(kind==ActionKind.ClickImage||kind==ActionKind.FindImage||kind==ActionKind.WaitImage)return new[]{ActionKind.IfImage,ActionKind.MouseClick,ActionKind.ActivateWindow};
            if(kind==ActionKind.Repeat||kind==ActionKind.Break||kind==ActionKind.Include)return new[]{ActionKind.Repeat,ActionKind.Break,ActionKind.Include}.Where(k=>k!=kind).ToArray();
            if(kind==ActionKind.TypeText||kind==ActionKind.KeyPress||kind==ActionKind.Paste)return new[]{ActionKind.ActivateWindow,ActionKind.MouseClick,ActionKind.SetClipboard};
            if(kind==ActionKind.Launch||kind==ActionKind.ActivateWindow)return new[]{ActionKind.WaitWindow,ActionKind.TypeText,ActionKind.KeyPress};
            return new ActionKind[0];
        }
        private void ReadTopic()
        {
            foreach(Control child in content.Controls.Cast<Control>().ToArray()){if(child!=explanation&&child!=controls&&child!=related)child.Dispose();}content.Controls.Clear();foreach(Control child in controls.Controls.Cast<Control>().ToArray())child.Dispose();foreach(Control child in related.Controls.Cast<Control>().ToArray())child.Dispose();illustration=null;recipePage=null;playbackButton=null;var topic=topics.SelectedItem as Topic;if(topic==null)return;
            title.Text=topic.Name;subtitle.Text=topic.Kind.HasValue?ActionCatalog.Describe(topic.Kind.Value):"A practical walkthrough · Read first, then try it in your workflow";
            if(topic.Kind.HasValue){var kind=topic.Kind.Value;illustration=new ActionShowcase(kind);content.Controls.Add(controls);content.Controls.Add(illustration);string[] scenarios=kind==ActionKind.IfImage?new[]{"Image found","Image missing"}:kind==ActionKind.Launch?new[]{"Program","Document","Script"}:new string[0];for(int i=0;i<scenarios.Length;i++){int variant=i;var choice=new ThemeButton{Text=scenarios[i],AutoSize=true,Height=30,Tag=i==0?"selected":"quiet"};choice.Click+=delegate{illustration.Variant=variant;if(playbackButton!=null)playbackButton.Text="Pause";foreach(Control sibling in controls.Controls)if(sibling.AccessibleName=="Guide scenario")sibling.Tag=sibling==choice?"selected":"quiet";controls.Invalidate(true);};choice.AccessibleName="Guide scenario";controls.Controls.Add(choice);}if(illustration.CanAnimate){var pause=new ThemeButton{Text="Pause",Width=80,Height=30,Tag="quiet",AccessibleName="Pause guide"};playbackButton=pause;pause.Click+=delegate{illustration.Paused=!illustration.Paused;pause.Text=illustration.Paused?"Play":"Pause";};illustration.MouseUp+=delegate{pause.Text=illustration.Paused?"Play":"Pause";};illustration.KeyUp+=delegate{pause.Text=illustration.Paused?"Play":"Pause";};controls.Controls.Add(pause);var replay=new ThemeButton{Text="Replay",Width=80,Height=30,Tag="quiet",AccessibleName="Replay guide"};replay.Click+=delegate{illustration.Replay();pause.Text="Pause";};controls.Controls.Add(replay);}var suggestions=Related(kind);if(suggestions.Length>0){related.Controls.Add(new Label{Text="Related guides",AutoSize=true,Tag="muted",Padding=new Padding(0,8,8,0)});foreach(var suggestion in suggestions){ActionKind destination=suggestion;var link=new ThemeButton{Text=ActionArt.Name(destination),AutoSize=true,Height=32,Tag="quiet",AccessibleName="Related guide: "+ActionArt.Name(destination)};link.Click+=delegate{SelectTopic(destination);};related.Controls.Add(link);}content.Controls.Add(related);}explanation.Text=ActionExamples.For(kind).Replace("\r\n","\n").Replace("\n","\r\n");}
            else {recipePage=new RecipePage(topic.Text);content.Controls.Add(recipePage);}
            if(topic.Kind.HasValue)content.Controls.Add(explanation);UiTheme.Apply(content);LayoutReading();viewport.AutoScrollPosition=Point.Empty;
        }
        private void LayoutReading()
        {
            if(layingOut)return;layingOut=true;try{int width=Math.Max(200,viewport.ClientSize.Width-24),y=0;if(recipePage!=null){recipePage.Width=width;recipePage.Location=Point.Empty;recipePage.Height=recipePage.RequiredHeight;content.Height=recipePage.Height+12;viewport.AutoScrollMinSize=new Size(0,content.Height);return;}if(illustration!=null){controls.SetBounds(0,0,width,controls.Controls.Count==0?0:width<570?76:40);illustration.SetBounds(0,controls.Bottom+4,width,illustration.IllustrationHeight);y=illustration.Bottom+16;}explanation.SetBounds(10,y,Math.Max(1,width-20),Math.Max(illustration==null?420:150,TextRenderer.MeasureText(explanation.Text,explanation.Font,new Size(Math.Max(1,width-20),10000),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height+20));int end=explanation.Bottom;if(related.Controls.Count>0&&illustration!=null){int rows=1,line=0;foreach(Control item in related.Controls){int itemWidth=item.GetPreferredSize(Size.Empty).Width+item.Margin.Horizontal;if(line>0&&line+itemWidth>width){rows++;line=0;}line+=itemWidth;}related.SetBounds(0,end+12,width,rows*40);end=related.Bottom;}content.Height=end+12;viewport.AutoScrollMinSize=new Size(0,content.Height);}finally{layingOut=false;}
        }
        private sealed class RecipePage : Control
        {
            private readonly string[] sections;
            internal RecipePage(string text){sections=text.Replace("\r\n","\n").Split(new[]{"\n\n"},StringSplitOptions.RemoveEmptyEntries);DoubleBuffered=true;AccessibleName="Recipe walkthrough";AccessibleDescription=text;Font=new Font("Segoe UI",10.5f);SetStyle(ControlStyles.ResizeRedraw,true);}
            private int SectionHeight(string section)
            {
                bool tree=section.StartsWith("IF image found:");using(var body=new Font(tree?"Consolas":"Segoe UI",tree?10:10.5f)){int split=section.IndexOf('\n');string text=tree?section:split<0?(section.Length>52?section:""):section.Substring(split+1);return 64+(text.Length==0?0:TextRenderer.MeasureText(text,body,new Size(Math.Max(100,Width-44),10000),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height);}
            }
            internal int RequiredHeight {get{return sections.Sum(s=>SectionHeight(s)+12);}}
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);e.Graphics.Clear(UiTheme.Background);int y=0;foreach(string section in sections){int height=SectionHeight(section);var card=new Rectangle(1,y+1,Math.Max(1,Width-3),height);using(var path=SoftGeometry.Round(card,12))using(var fill=new SolidBrush(UiTheme.Surface))using(var edge=new Pen(UiTheme.Stroke)){e.Graphics.FillPath(fill,path);e.Graphics.DrawPath(edge,path);}bool tree=section.StartsWith("IF image found:");int split=section.IndexOf('\n');string heading=tree?"Example · Nested branches":split<0?(section.Length>52?"Usage tip":section):section.Substring(0,split),body=tree?section:split<0?(section.Length>52?section:""):section.Substring(split+1);using(var titleFont=new Font("Segoe UI",11,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,heading,titleFont,new Rectangle(18,y+15,Width-36,30),UiTheme.Accent,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);using(var bodyFont=new Font(tree?"Consolas":"Segoe UI",tree?10:10.5f))TextRenderer.DrawText(e.Graphics,body,bodyFont,new Rectangle(18,y+48,Width-36,height-48),UiTheme.Ink,TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);y+=height+12;}
            }
        }
        protected override bool ProcessCmdKey(ref Message message,Keys keyData)
        {
            if(keyData==(Keys.Control|Keys.F)){search.Focus();search.SelectAll();return true;}if(keyData==Keys.Escape&&search.ContainsFocus&&search.Text.Length>0){search.Clear();return true;}return base.ProcessCmdKey(ref message,keyData);
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing){guideTips.Dispose();if(related.Parent==null)related.Dispose();if(explanation.Parent==null)explanation.Dispose();if(controls.Parent==null)controls.Dispose();}base.Dispose(disposing);
        }
        internal void SearchHeadless(string value){search.Text=value;}
        internal int TopicCount {get{return topics.Items.Count;}}
    }
}
