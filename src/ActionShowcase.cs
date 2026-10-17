using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MacroExecute
{
    internal sealed class ShowcaseScene
    {
        internal string[] Fields;internal string Title,Body,Outcome,Art;
        internal ShowcaseScene(string[] fields,string title,string body,string outcome,string art){Fields=fields;Title=title;Body=body;Outcome=outcome;Art=art;}
        private static readonly Dictionary<ActionKind,ShowcaseScene> scenes=new Dictionary<ActionKind,ShowcaseScene>{
            {ActionKind.CopySelection,new ShowcaseScene(new[]{"Shortcut|Ctrl+C","Wait for fresh clipboard text|On","Copy timeout|5 seconds"},"Clipboard","Document Type: Status Report\nMonthly Report","Fresh copied text stays available for the next condition.","text")},
            {ActionKind.Launch,new ShowcaseScene(new[]{"Program or file|notepad.exe","Arguments|\"D:\\Notes\\todo.txt\"","Browse filter|Windows programs"},"todo.txt — Notepad","Review the report\nSend the weekly update","The program opens the supplied document.","window")},
            {ActionKind.IfImage,new ShowcaseScene(new[]{"Reference|Login button","Search area|Foreground window","Wait before deciding|Off"},"Example target app","Login","Image found → IF branch (add actions here)","image")},
            {ActionKind.ClickImage,new ShowcaseScene(new[]{"Reference|Login button","Search area|Foreground window","Mouse button|Left"},"Example target app","Login","Finds the button and clicks its center.","image")},
            {ActionKind.FindImage,new ShowcaseScene(new[]{"Reference|Login button","Search area|Foreground window","Move pointer|Off"},"Example target app","Login","Remembers the match for a later mouse step.","image")},
            {ActionKind.WaitImage,new ShowcaseScene(new[]{"Reference|Ready indicator","Timeout|5 seconds","Check every|100 ms"},"Example target app","Ready","Continues when Ready appears; times out if absent.","image")},
            {ActionKind.Repeat,new ShowcaseScene(new[]{"Repetitions|3","Random maximum|0 (off)","Inside block|Click → Wait"},"Workflow execution","Click → Wait\nClick → Wait\nClick → Wait","Three passes, then continue after END REPEAT.","flow")},
            {ActionKind.Delay,new ShowcaseScene(new[]{"Wait duration|2 seconds","Random maximum|0 (off)"},"Playback timeline","Previous step\nWait 2 seconds\nNext step","A pause between the surrounding steps.","flow")},
            {ActionKind.TypeText,new ShowcaseScene(new[]{"Text|Hello world!","Entry method|Unicode typing","Target|Already focused field"},"Example text field","Hello world!","Types into the field that has keyboard focus.","text")},
            {ActionKind.Include,new ShowcaseScene(new[]{"Workflow file|.\\parts\\sign-in.mexec","Next step|Continues in this file"},"Expanded workflow","Current workflow\nIncluded sign-in steps\nBack to current workflow","Included files can themselves include workflows.","flow")},
            {ActionKind.MouseClick,new ShowcaseScene(new[]{"Click / move at|Absolute screen coordinates","X / Y|500 / 300","Button|Left"},"Example target app","Submit","The pointer moves to (500, 300) and clicks.","pointer")},
            {ActionKind.MouseMove,new ShowcaseScene(new[]{"Click / move at|Absolute screen coordinates","X / Y|500 / 300"},"Example target app","Target position","Moves the pointer without clicking.","pointer")},
            {ActionKind.MouseDown,new ShowcaseScene(new[]{"Location|Current pointer","Button|Left"},"Example target app","Drag starts here","Holds Left until a Mouse up step releases it.","pointer")},
            {ActionKind.MouseUp,new ShowcaseScene(new[]{"Location|Current pointer","Button|Left"},"Example target app","Drop position","Releases the held mouse button.","pointer")},
            {ActionKind.MouseWheel,new ShowcaseScene(new[]{"Location|Current pointer","Wheel delta|-120"},"Example scrollable list","Item 2\nItem 3\nItem 4","Scrolls down one standard wheel increment.","window")},
            {ActionKind.KeyPress,new ShowcaseScene(new[]{"Press a key|Enter","Button / key hold|30 ms"},"Focused application","Enter","The target receives Enter; its app decides the effect.","key")},
            {ActionKind.KeyDown,new ShowcaseScene(new[]{"Press a key|ControlKey","Follow with|Press C → Key up ControlKey"},"Keyboard state","Ctrl held","Keeps the modifier held for following input.","key")},
            {ActionKind.KeyUp,new ShowcaseScene(new[]{"Press a key|ControlKey"},"Keyboard state","Ctrl released","Ends the matching Key down.","key")},
            {ActionKind.SetClipboard,new ShowcaseScene(new[]{"Text|Order 123"},"Clipboard text","Order 123","Replaces clipboard text; add Paste to enter it.","text")},
            {ActionKind.Paste,new ShowcaseScene(new[]{"Before this|Focus a text field","Clipboard text|Order 123"},"Example text field","Order 123","Sends Ctrl+V with the current clipboard.","text")},
            {ActionKind.ClipboardClear,new ShowcaseScene(new[]{"Action|Clear clipboard"},"Clipboard","(empty)","Removes the existing clipboard contents.","text")},
            {ActionKind.ClipboardIncrement,new ShowcaseScene(new[]{"Advanced: DelayMs|1","Existing clipboard|41"},"Clipboard text","42","Adds one; clipboard must contain an integer.","text")},
            {ActionKind.TypeDate,new ShowcaseScene(new[]{"Date format|yyyy-MM-dd","Target|Focused text field"},"Example text field","2026-10-16","Illustrative date; playback uses its actual date.","text")},
            {ActionKind.TypeEnvironment,new ShowcaseScene(new[]{"Environment variable|USERNAME"},"Example text field","Saito","Types the value for the current Windows account.","text")},
            {ActionKind.OpenUrl,new ShowcaseScene(new[]{"Website URL|https://example.com"},"Default browser","example.com","Opens the address using the default browser.","window")},
            {ActionKind.ActivateWindow,new ShowcaseScene(new[]{"Window title contains|Notepad"},"Foreground window","Notepad","Brings an existing matching window forward.","window")},
            {ActionKind.MoveWindow,new ShowcaseScene(new[]{"Advanced: Text|Notepad","X / Y|100 / 100","Width / Height|900 / 600"},"Desktop layout","Notepad → (100, 100)","Moves and resizes the existing matching window.","window")},
            {ActionKind.WaitWindow,new ShowcaseScene(new[]{"Window title contains|Notepad","Advanced: TimeoutMs|5000"},"Workflow execution","Wait for Notepad\nContinue when found","Fails after the timeout if no matching window exists.","flow")},
            {ActionKind.IfWindow,new ShowcaseScene(new[]{"Window title contains|Notepad"},"Branch selection","Window exists → IF\nNo match → ELSE","Only the matching branch runs.","flow")},
            {ActionKind.IfClipboard,new ShowcaseScene(new[]{"Comparison|Contains all strings","Strings to check|Monthly Report / Status: Approved","Ignore letter case|Off"},"Clipboard item text","All strings present → IF\nOne missing → ELSE","Use blank-line groups for alternative AND combinations.","flow")},
            {ActionKind.IfFile,new ShowcaseScene(new[]{"File path|D:\\Reports\\ready.csv"},"Branch selection","File exists → IF\nMissing → ELSE","Tests file existence without opening it.","flow")},
            {ActionKind.IfProcess,new ShowcaseScene(new[]{"Process name|notepad"},"Branch selection","Running → IF\nNot running → ELSE","Process names differ from window titles.","flow")},
            {ActionKind.WaitProcess,new ShowcaseScene(new[]{"Process name|notepad","Advanced: TimeoutMs|5000"},"Workflow execution","Wait for notepad\nContinue when running","Waits for a process to start, not to finish.","flow")},
            {ActionKind.FindPixel,new ShowcaseScene(new[]{"Advanced: ColorRgb|#42C6AE","Tolerance|0 (exact)"},"Color search","Color match","Remembers a matching pixel for later input.","pixel")},
            {ActionKind.WaitPixel,new ShowcaseScene(new[]{"Advanced: ColorRgb|#42C6AE","Advanced: TimeoutMs|5000"},"Color search","Color appears","Continues when that color is found.","pixel")},
            {ActionKind.IfPixel,new ShowcaseScene(new[]{"Advanced: ColorRgb|#42C6AE","Tolerance|0 (exact)"},"Branch selection","Match → IF\nNo match → ELSE","Runs the branch for the pixel search result.","pixel")},
            {ActionKind.Label,new ShowcaseScene(new[]{"Label name|retry"},"Workflow destination","retry:\nNext action","Provides a named destination for Jump.","flow")},
            {ActionKind.Jump,new ShowcaseScene(new[]{"Label name|retry"},"Workflow navigation","Jump → retry\nContinue from Label retry","Cannot jump into an inactive block.","flow")},
            {ActionKind.Break,new ShowcaseScene(new[]{"Place inside|Repeat block","Example condition|IF success image found"},"Workflow execution","Repeat\nBreak → After END REPEAT","Exits only the nearest enclosing repeat.","flow")},
            {ActionKind.Pause,new ShowcaseScene(new[]{"Action|Pause playback"},"Workflow execution","Paused\nResume → next step","Waits for your configured resume command.","flow")},
            {ActionKind.Note,new ShowcaseScene(new[]{"Text|Sign-in steps start here"},"Workflow timeline","Note: Sign-in steps start here\nNext action","A comment; sends no input to the target app.","flow")},
            {ActionKind.Message,new ShowcaseScene(new[]{"Text|Task finished"},"Message dialog","Task finished","Shows a message during playback.","window")},
            {ActionKind.CopyFile,new ShowcaseScene(new[]{"Advanced: Target|D:\\Input\\report.csv","Advanced: Text|D:\\Output\\report.csv"},"Files","Input/report.csv\n→ Output/report.csv","Creates the destination; fails if it already exists.","flow")},
            {ActionKind.DeleteFile,new ShowcaseScene(new[]{"File path|D:\\Temp\\old-report.txt"},"Files","old-report.txt → removed","Deletes the specified file during playback.","flow")},
            {ActionKind.RunCSharp,new ShowcaseScene(new[]{"Text|System.Console.WriteLine(\"Hello\");","Advanced: TimeoutMs|5000"},"Script process","Console output: Hello","Runs trusted C# code in a child process.","window")},
            {ActionKind.Else,new ShowcaseScene(new[]{"Matching block|IF condition","Branch|False"},"Workflow execution","IF false → ELSE steps\nThen continue after END IF","Select the IF header to edit or move the block.","flow")},
            {ActionKind.EndIf,new ShowcaseScene(new[]{"Matching block|IF / ELSE"},"Workflow execution","True or false branch\nEND IF → next step","Both branches rejoin after this boundary.","flow")},
            {ActionKind.EndRepeat,new ShowcaseScene(new[]{"Matching block|Repeat 3 times"},"Workflow execution","More passes → Repeat\nFinished → next step","Closes the repeat block.","flow")},
        };
        internal static ShowcaseScene For(ActionKind kind,int variant)
        {
            if(kind==ActionKind.Launch&&variant==1)return new ShowcaseScene(new[]{"Program or file|D:\\Reports\\report.pdf","Arguments|(none)","Browse filter|PDF documents"},"report.pdf — Default PDF app","WEEKLY REPORT\nRevenue and activity","Windows opens the file in its associated app.","window");
            if(kind==ActionKind.Launch&&variant==2)return new ShowcaseScene(new[]{"Program or file|python.exe","Arguments|\"D:\\Tasks\\daily report.py\"","Prerequisite|Python installed"},"Script process","daily report.py\nScript starts","The interpreter runs the script; this step does not wait for it to finish.","window");
            if(kind==ActionKind.IfImage&&variant==1)return new ShowcaseScene(new[]{"Reference|Login button","Search area|Foreground window","Wait before deciding|Off"},"Example target app","No Login button","Image missing → ELSE branch (add recovery here)","missing");
            return scenes[kind];
        }
    }
    // Original, scalable UI illustrations. No desktop capture or playback occurs here.
    internal sealed class ActionShowcase : Control
    {
        internal ActionKind Kind;private int variant;
        internal int Variant {get{return variant;}set{variant=value;Replay();UpdateDescription();Invalidate();}}
        internal bool Stacked {get{return Width<620;}}
        internal int OutcomeStart {get{return (Stacked?432:0)+TimelineHeight;}}
        internal int IllustrationHeight {get{return (Stacked?840:438)+TimelineHeight;}}
        internal const int CycleMs=9600;
        private readonly Timer animationTimer=new Timer{Interval=60};
        private readonly System.Diagnostics.Stopwatch animationClock=new System.Diagnostics.Stopwatch();
        private int playhead;private bool paused;
        internal bool CanAnimate {get{return Kind==ActionKind.IfImage||Kind==ActionKind.ClickImage||Kind==ActionKind.Repeat||Kind==ActionKind.Launch;}}
        internal int TimelineHeight {get{return CanAnimate?54:0;}}
        internal int Playhead {get{return (int)((playhead+animationClock.ElapsedMilliseconds)%CycleMs);}}
        internal int Phase {get{return CanAnimate?Math.Min(3,Playhead/2400):3;}}
        internal bool Paused {get{return paused;}set{CommitClock();paused=value;UpdatePlayback();Invalidate();}}
        internal bool AnimationRunning {get{return animationTimer.Enabled;}}
        internal void Seek(int milliseconds){if(milliseconds<0||milliseconds>=CycleMs)throw new ArgumentOutOfRangeException("milliseconds");animationClock.Reset();playhead=milliseconds;UpdatePlayback();Invalidate();}
        internal void Replay(){Seek(0);Paused=false;}
        internal void RefreshPlayback(){UpdatePlayback();}
        private void CommitClock(){playhead=Playhead;animationClock.Reset();}
        private void UpdatePlayback(){if(CanAnimate&&Visible&&FindForm()!=null&&FindForm().Visible&&FindForm().WindowState!=FormWindowState.Minimized&&!paused&&!IsDisposed){animationClock.Start();animationTimer.Start();}else{CommitClock();animationTimer.Stop();}}
        protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);UpdatePlayback();}
        protected override void Dispose(bool disposing){if(disposing){animationTimer.Stop();animationTimer.Dispose();animationClock.Stop();}base.Dispose(disposing);}
        internal Rectangle SetupBounds {get;private set;}
        internal Rectangle OutcomeBounds {get;private set;}
        internal ShowcaseScene Scene {get{return ShowcaseScene.For(Kind,Variant);}}
        internal ActionShowcase(ActionKind kind){Kind=kind;DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);AccessibleName="Setup and outcome illustration";AccessibleRole=AccessibleRole.Graphic;TabStop=CanAnimate;AccessibleDescription="Click a stage to pause there. Left and Right inspect stages; Space plays or pauses.";Font=new Font("Segoe UI",10);animationTimer.Tick+=delegate{if(!Visible||FindForm()==null||FindForm().WindowState==FormWindowState.Minimized){UpdatePlayback();return;}Invalidate();};UpdateDescription();}
        private void UpdateDescription(){var scene=Scene;AccessibleDescription="Illustrative example. Setup: "+string.Join("; ",scene.Fields)+". Outcome: "+scene.Outcome+(CanAnimate?". Click a numbered stage to pause there. Arrow keys change stages; Space plays or pauses.":"");}
        protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Height!=IllustrationHeight)Height=IllustrationHeight;}
        private void Write(Graphics g,string text,Rectangle rect,Color color,float size=10,bool bold=false)
        {
            if(rect.Width<=0||rect.Height<=0)return;
            using(var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular))TextRenderer.DrawText(g,text,font,rect,color,TextFormatFlags.Left|TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        }
        private void Round(Graphics g,Rectangle rect,Color fill,int radius=12,bool border=false)
        {
            if(rect.Width<1||rect.Height<1)return;using(var path=SoftGeometry.Round(rect,radius))using(var brush=new SolidBrush(fill)){g.FillPath(brush,path);if(border)using(var pen=new Pen(UiTheme.Stroke))g.DrawPath(pen,path);}
        }
        private void Pill(Graphics g,Rectangle rect,string text,bool active)
        {
            Round(g,rect,active?UiTheme.Selected:UiTheme.Field,8);Write(g,text,new Rectangle(rect.X+10,rect.Y+8,rect.Width-20,rect.Height-10),active?UiTheme.Accent:UiTheme.Muted,9,true);
        }
        private void Chrome(Graphics g,Rectangle app,string title)
        {
            Round(g,app,UiTheme.Field,10,true);using(var dot=new SolidBrush(UiTheme.Muted))for(int i=0;i<3;i++)g.FillEllipse(dot,app.X+12+i*11,app.Y+14,4,4);
            Write(g,title,new Rectangle(app.X+54,app.Y+7,app.Width-65,25),UiTheme.Muted,9,true);
            using(var pen=new Pen(UiTheme.Stroke))g.DrawLine(pen,app.Left+1,app.Top+34,app.Right-1,app.Top+34);
        }
        private void CursorAt(Graphics g,Point p)
        {
            using(var fill=new SolidBrush(UiTheme.Ink))using(var edge=new Pen(UiTheme.Background,1.5f)){var polygon=new[]{p,new Point(p.X+5,p.Y+26),new Point(p.X+11,p.Y+18),new Point(p.X+22,p.Y+16)};g.FillPolygon(fill,polygon);g.DrawPolygon(edge,polygon);}
        }
        private bool Image {get{return Kind==ActionKind.IfImage||Kind==ActionKind.ClickImage||Kind==ActionKind.FindImage||Kind==ActionKind.WaitImage;}}
        private void DrawSetup(Graphics g,Rectangle card,ShowcaseScene scene)
        {
            Pill(g,new Rectangle(card.X+18,card.Y+16,90,29),"01  SETUP",true);
            Write(g,Image?"Choose what to look for":"Configure the action",new Rectangle(card.X+18,card.Y+57,card.Width-36,32),UiTheme.Ink,14,true);
            Write(g,Image?"A small, distinctive reference works best.":"Use these sample values as a starting point.",new Rectangle(card.X+18,card.Y+94,card.Width-36,39),UiTheme.Muted,9);
            int y=card.Y+143;
            foreach(string field in scene.Fields){var pair=field.Split(new[]{'|'},2);bool reference=pair[0]=="Reference";
                Write(g,pair[0],new Rectangle(card.X+18,y,card.Width-36,20),UiTheme.Muted,9,true);y+=24;
                int h=reference?64:37;var box=new Rectangle(card.X+18,y,card.Width-36,h);Round(g,box,UiTheme.Field,8,true);
                if(reference){for(int gx=box.Left+4;gx<box.Right-4;gx+=12)for(int gy=box.Top+4;gy<box.Bottom-4;gy+=12)if(((gx-box.Left)/12+(gy-box.Top)/12)%2==0)using(var tile=new SolidBrush(SoftGeometry.Blend(UiTheme.Field,UiTheme.Border,0.12f)))g.FillRectangle(tile,gx,gy,10,10);
                    var button=new Rectangle(box.X+(box.Width-100)/2,box.Y+15,100,34);Round(g,button,UiTheme.Selected,7);Write(g,Kind==ActionKind.WaitImage?"Ready":"Login",new Rectangle(button.X+27,button.Y+8,70,24),UiTheme.Accent,10,true);
                }else Write(g,pair.Length>1?pair[1]:"",new Rectangle(box.X+10,box.Y+9,box.Width-20,h-9),UiTheme.Ink,10);
                y+=h+15;
            }
        }
        private void DrawTarget(Graphics g,Rectangle app,ShowcaseScene scene)
        {
            Chrome(g,app,scene.Title);var body=new Rectangle(app.X+15,app.Y+47,app.Width-30,app.Height-60);
            if(scene.Art=="pointer"){
                using(var pen=new Pen(UiTheme.Stroke))for(int x=body.Left+8;x<body.Right;x+=27)g.DrawLine(pen,x,body.Top+5,x,body.Bottom-5);
                var target=new Rectangle(body.X+Math.Max(8,(body.Width-115)/2),body.Y+54,115,39);Round(g,target,UiTheme.Selected,8);
                string label=Kind==ActionKind.MouseClick?"Submit":Kind==ActionKind.MouseDown?"Drag me":Kind==ActionKind.MouseUp?"Drop here":"Target";
                Write(g,label,new Rectangle(target.X+20,target.Y+10,target.Width-25,25),UiTheme.Accent,10,true);
                if(Kind==ActionKind.MouseClick)using(var pen=new Pen(UiTheme.Accent,2))g.DrawEllipse(pen,target.Right-16,target.Bottom-11,25,25);
                CursorAt(g,new Point(target.Right-6,target.Bottom-3));
                Write(g,Kind==ActionKind.MouseMove||Kind==ActionKind.MouseClick?"Position: 500, 300":"Button: Left",new Rectangle(body.X,body.Y+122,body.Width,35),UiTheme.Muted,9);
            }else if(Image||scene.Art=="missing"){
                Write(g,Kind==ActionKind.WaitImage?"Your task is processing":"Welcome back",new Rectangle(body.X,body.Y,body.Width,28),UiTheme.Ink,12,true);
                if(scene.Art!="missing"){
                    Write(g,"Account",new Rectangle(body.X,body.Y+34,body.Width,20),UiTheme.Muted,9);
                    Round(g,new Rectangle(body.X,body.Y+58,body.Width,30),UiTheme.Surface,6,true);
                    Write(g,"name@example.com",new Rectangle(body.X+8,body.Y+65,body.Width-16,20),UiTheme.Muted,9);
                    var target=new Rectangle(body.X+8,body.Y+109,Math.Min(122,body.Width-16),37);Round(g,target,UiTheme.Selected,7);
                    Write(g,Kind==ActionKind.WaitImage?"Ready":scene.Body,new Rectangle(target.X+24,target.Y+9,target.Width-28,25),UiTheme.Accent,10,true);
                    if(Phase>=2)using(var pen=new Pen(UiTheme.Accent,2)){pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,Rectangle.Inflate(target,6,6));}
                    if(Kind==ActionKind.ClickImage&&Phase>=2){float progress=Math.Min(1,(Playhead-4800)/2800f);var cursor=new Point((int)(body.Right-10+(target.Right-7-(body.Right-10))*progress),(int)(body.Top+20+(target.Bottom-4-(body.Top+20))*progress));CursorAt(g,cursor);if(Phase==3){int radius=12+(Playhead%600)/30;using(var pen=new Pen(UiTheme.Accent,2))g.DrawEllipse(pen,target.Right-7-radius,target.Bottom-4-radius,radius*2,radius*2);}}

                }else{
                    Write(g,"This screen has no Login button.",new Rectangle(body.X,body.Y+42,body.Width,45),UiTheme.Muted,10);
                    Round(g,new Rectangle(body.X,body.Y+105,body.Width,37),UiTheme.Surface,6,true);
                    Write(g,"Continue as guest",new Rectangle(body.X+12,body.Y+115,body.Width-20,22),UiTheme.Muted,10);
                }
            }else if(scene.Art=="flow"){
                string[] lines=scene.Body.Split('\n');int y=body.Y+6;for(int i=0;i<lines.Length;i++){var row=new Rectangle(body.X,y,body.Width,43);Round(g,row,(Kind==ActionKind.Repeat?Phase>0&&i==Math.Min(2,(Playhead-2400)/1600):i==lines.Length-1)?UiTheme.Selected:UiTheme.Surface,8);Write(g,Kind==ActionKind.Repeat&&Playhead>=2400+(i+1)*1600?"✓":(i+1).ToString("00"),new Rectangle(row.X+10,row.Y+12,29,25),UiTheme.Accent,10,true);Write(g,lines[i],new Rectangle(row.X+43,row.Y+12,row.Width-51,29),UiTheme.Ink,9,true);if(i<lines.Length-1)using(var pen=new Pen(UiTheme.Accent,1.5f)){pen.EndCap=LineCap.ArrowAnchor;g.DrawLine(pen,row.X+22,row.Bottom+2,row.X+22,row.Bottom+11);}y+=57;}
            }else if(scene.Art=="pixel"){
                for(int x=0;x<7;x++)for(int y=0;y<4;y++)using(var brush=new SolidBrush(x==3&&y==2?Color.FromArgb(66,198,174):SoftGeometry.Blend(UiTheme.Surface,UiTheme.Border,(x+y)%3*0.07f)))g.FillRectangle(brush,body.X+x*27,body.Y+y*27,24,24);
                using(var pen=new Pen(UiTheme.Accent,2))g.DrawRectangle(pen,body.X+3*27-3,body.Y+2*27-3,30,30);
                Write(g,"#42C6AE  ·  matching pixel",new Rectangle(body.X,body.Y+125,body.Width,40),UiTheme.Ink,10,true);
            }else if(scene.Art=="key"){
                var key=new Rectangle(body.X+12,body.Y+22,Math.Min(170,body.Width-24),60);Round(g,new Rectangle(key.X,key.Y+5,key.Width,key.Height),UiTheme.Stroke,10);Round(g,key,UiTheme.Surface,10,true);Write(g,scene.Body,new Rectangle(key.X+20,key.Y+17,key.Width-30,35),UiTheme.Accent,14,true);
                Write(g,"Sent to the focused application",new Rectangle(body.X,body.Y+109,body.Width,45),UiTheme.Muted,10);
            }else{
                var paper=new Rectangle(body.X,body.Y,body.Width,body.Height);Round(g,paper,UiTheme.Surface,6);
                Write(g,Kind==ActionKind.Launch&&Phase<2?(Phase==0?"Application preview":"Opening application…"):scene.Body,new Rectangle(paper.X+12,paper.Y+14,paper.Width-24,88),UiTheme.Ink,11,true);
                using(var pen=new Pen(UiTheme.Stroke))for(int i=0;i<3;i++)g.DrawLine(pen,paper.X+12,paper.Y+116+i*17,paper.Right-20-i*15,paper.Y+116+i*17);
                if(Kind==ActionKind.Launch&&Phase==1)using(var spinner=new Pen(UiTheme.Accent,3))g.DrawArc(spinner,paper.Right-35,paper.Top+13,17,17,(Playhead%900)*360f/900,260);
                if(Kind==ActionKind.TypeText||Kind==ActionKind.Paste)using(var pen=new Pen(UiTheme.Accent,2))g.DrawLine(pen,paper.X+121,paper.Y+14,paper.X+121,paper.Y+33);
            }
        }
        // The stage strip doubles as a scrubber: inspecting a frame pauses the example.
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(CanAnimate&&e.Button==MouseButtons.Left&&e.Y<50){Focus();Paused=true;Seek(Math.Min(3,Math.Max(0,e.X*4/Math.Max(1,Width)))*2400+600);}
        }
        protected override bool IsInputKey(Keys keyData){return keyData==Keys.Left||keyData==Keys.Right||base.IsInputKey(keyData);}
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);if(!CanAnimate)return;if(e.KeyCode==Keys.Space){Paused=!Paused;e.Handled=true;}else if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right){int next=Math.Max(0,Math.Min(3,Phase+(e.KeyCode==Keys.Right?1:-1)));Paused=true;Seek(next*2400+600);e.Handled=true;}
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);if(Width<80)return;var g=e.Graphics;g.Clear(Parent==null?UiTheme.Background:Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;var scene=Scene;
            if(Stacked){SetupBounds=new Rectangle(2,2+TimelineHeight,Width-4,406);OutcomeBounds=new Rectangle(2,432+TimelineHeight,Width-4,404);}else{int width=(Width-32)/2;SetupBounds=new Rectangle(2,2+TimelineHeight,width,432);OutcomeBounds=new Rectangle(width+30,2+TimelineHeight,width,432);}
            if(CanAnimate){string[] stages={"1  Configure","2  "+(Kind==ActionKind.Launch?"Launch":Kind==ActionKind.Repeat?"Run block":"Search"),"3  "+(Kind==ActionKind.Repeat?"Repeat":"Result"),"4  "+(Kind==ActionKind.ClickImage?"Click":Kind==ActionKind.IfImage?"Choose branch":"Continue")};int segment=(Width-12)/4;for(int i=0;i<4;i++){Pill(g,new Rectangle(2+i*(segment+3),2,segment,34),stages[i],i==Phase);using(var pen=new Pen(i<=Phase?UiTheme.Accent:UiTheme.Stroke,3))g.DrawLine(pen,6+i*(segment+3),43,segment-2+i*(segment+3),43);}}
            Round(g,SetupBounds,UiTheme.Surface,16,true);Round(g,OutcomeBounds,UiTheme.Surface,16,true);DrawSetup(g,SetupBounds,scene);
            var right=OutcomeBounds;Pill(g,new Rectangle(right.X+18,right.Y+16,105,29),"02  OUTCOME",true);
            Write(g,Kind==ActionKind.IfImage?(Phase<3?(Phase==1?"Searching the window":Phase==2?(variant==1?"No match found":"Reference matched"):"Start with a reference"):(variant==1?"Take the ELSE branch":"Take the IF branch")):"See the result",new Rectangle(right.X+18,right.Y+57,right.Width-36,32),UiTheme.Ink,14,true);
            var app=new Rectangle(right.X+18,right.Y+103,right.Width-36,Kind==ActionKind.IfImage?218:232);DrawTarget(g,app,scene);
            if(CanAnimate&&Phase==1&&Image){int scanY=app.Y+47+(int)((Playhead%2400)/2400f*Math.Max(1,app.Height-60));using(var scan=new Pen(UiTheme.Accent,2))g.DrawLine(scan,app.Left+15,scanY,app.Right-15,scanY);}
            int y=app.Bottom+13;
            if(Kind==ActionKind.IfImage){int half=(right.Width-46)/2;Pill(g,new Rectangle(right.X+18,y,half,30),"IF · image found",Phase==3&&variant==0);Pill(g,new Rectangle(right.X+28+half,y,half,30),"ELSE · missing",Phase==3&&variant==1);y+=39;}
            Write(g,CanAnimate&&Phase<3?(Phase==0?"Start with the sample settings on the left.":Phase==1?(Kind==ActionKind.Repeat?"The steps inside the block begin running.":Kind==ActionKind.Launch?"Windows starts the chosen program or file.":"Search stays inside the chosen area."):Kind==ActionKind.IfImage?(variant==0?"A matching button was found.":"The reference is not present in this window."):"Watch the example progress to the next step."):scene.Outcome,new Rectangle(right.X+18,y,right.Width-36,right.Bottom-y-12),UiTheme.Ink,10,true);
            using(var arrow=new Pen(UiTheme.Accent,2)){arrow.EndCap=LineCap.ArrowAnchor;if(Stacked)g.DrawLine(arrow,Width/2,415+TimelineHeight,Width/2,426+TimelineHeight);else g.DrawLine(arrow,SetupBounds.Right+4,Height/2,OutcomeBounds.Left-4,Height/2);}
        }
    }
}
