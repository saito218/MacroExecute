using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MacroExecute
{
    public sealed partial class MainForm
    {
        private readonly TextBox actionSearch=new TextBox{BorderStyle=BorderStyle.None,AccessibleName="Find an action"};
        private readonly FlowLayoutPanel searchResults=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true,AccessibleName="Action search results",Tag="canvas",Visible=false};
        private readonly Label searchSummary=new Label{AutoSize=true,Tag="muted",Padding=new Padding(0,9,0,0)};
        private readonly Label documentState=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleRight,Tag="secondary",AutoEllipsis=true};
        private readonly FlowLayoutPanel blockNavigation=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false,AutoScroll=true,AccessibleName="Block navigation",Tag="toolbar"};
        private readonly Panel welcome=new Panel{Dock=DockStyle.Fill};
        private TableLayoutPanel welcomeContent;
        private FlowLayoutPanel recentFlows;
        private readonly Label categoryHint=new Label{Dock=DockStyle.Fill,Tag="muted",AutoEllipsis=true,AccessibleName="Action category guidance",Font=new Font("Segoe UI",9)};
        private ThemeTabs actionTabs;
        private TableLayoutPanel timelinePanel;
        private FlowLayoutPanel stepToolbar;
        private Button beforeButton,afterButton,undoButton,redoButton;
        private bool insertBefore;
        private int InsertionIndex {get{return SelectedIndex<0?macro.Steps.Count:SelectedIndex+(insertBefore?0:1);}}
        private Control ActionLibrary(ThemeTabs tabs)
        {
            actionTabs=tabs;var host=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Tag="canvas",Margin=Padding.Empty};host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));host.RowStyles.Add(new RowStyle(SizeType.Absolute,38));host.RowStyles.Add(new RowStyle(SizeType.Absolute,24));host.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var tools=Bar();host.Controls.Add(tools,0,0);tools.Controls.Add(new Label{Text="Find an action",AutoSize=true,Padding=new Padding(0,9,8,0)});
            var frame=new FieldFrame{Width=265,Height=32,Margin=new Padding(0,2,4,0)};actionSearch.Dock=DockStyle.Fill;frame.Controls.Add(actionSearch);tools.Controls.Add(frame);editing.Add(frame);
            AddButton(tools,"Clear",delegate{actionSearch.Clear();actionSearch.Focus();},true);tools.Controls.Add(new Label{Text="Insert",AutoSize=true,Padding=new Padding(14,9,4,0)});
            beforeButton=AddButton(tools,"Before",delegate{SetInsertion(true);},true);afterButton=AddButton(tools,"After",delegate{SetInsertion(false);},true);AddButton(tools,"Append at end",delegate{list.SelectedItems.Clear();UpdateSelectionHints();},true);tools.Controls.Add(searchSummary);tools.SizeChanged+=delegate{frame.Width=tools.ClientSize.Width<1080?210:265;searchSummary.Visible=tools.ClientSize.Width>=1080;};
            var deck=new Panel{Dock=DockStyle.Fill,Tag="canvas",Margin=Padding.Empty};deck.Controls.Add(tabs);deck.Controls.Add(searchResults);host.Controls.Add(categoryHint,0,1);host.Controls.Add(deck,0,2);tabs.SelectedIndexChanged+=delegate{RefreshCategoryHint();};editing.Add(searchResults);
            actionSearch.TextChanged+=delegate{FilterActions();};actionSearch.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Escape){actionSearch.Clear();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Enter){var first=searchResults.Controls.OfType<Button>().FirstOrDefault();if(!busy&&first!=null){first.PerformClick();e.SuppressKeyPress=true;}}};
            tooltips.SetToolTip(actionSearch,"Search every action: try image, loop, hotkey, clipboard, or wait. Enter opens the first result; Escape clears.");return host;
        }
        private void InstallActionHelp(Button button,ActionKind kind)
        {
            var menu=new ContextMenuStrip();menu.Items.Add("How to use "+ActionArt.Name(kind),null,delegate{ShowGuide(kind);});button.ContextMenuStrip=menu;menu.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e){e.Cancel=true;InWindowMenu.Show(this,button,menu.Items);};button.MouseEnter+=delegate{categoryHint.Text=ActionArt.Name(kind)+" · "+ActionCatalog.Describe(kind);};button.MouseLeave+=delegate{RefreshCategoryHint();};button.Disposed+=delegate{menu.Dispose();};tooltips.SetToolTip(button,ActionCatalog.Describe(kind)+"\nClick to add · Right-click for a visual example.");
        }
        private void RefreshCategoryHint(){if(actionTabs==null)return;if(!string.IsNullOrWhiteSpace(actionSearch.Text)){categoryHint.Text="Search results · All categories · Clear to return to the category tabs";return;}var page=actionTabs.SelectedTab;int count=page==null||page.Controls.Count==0?0:page.Controls[0].Controls.Count;categoryHint.Text="Action categories · "+(page==null?"Choose a tab":page.Text+" ("+count+")")+" · Choose a tab · Click to add · Right-click for examples";}
        private void LayoutStepToolbar(){if(stepToolbar==null||timelinePanel==null)return;bool any=macro.Steps.Count>0;bool wrap=stepToolbar.ClientSize.Width<720;stepToolbar.Visible=any;stepToolbar.WrapContents=wrap;stepToolbar.AutoScroll=false;timelinePanel.RowStyles[timelinePanel.RowStyles.Count-1].Height=any?(wrap?78:42):0;}
        private void SetInsertion(bool before){insertBefore=before;UpdateSelectionHints();}
        private void FilterActions()
        {
            searchResults.SuspendLayout();while(searchResults.Controls.Count>0)searchResults.Controls[0].Dispose();bool filtering=!string.IsNullOrWhiteSpace(actionSearch.Text);actionTabs.Visible=!filtering;searchResults.Visible=filtering;
            if(filtering){var matches=ActionCatalog.Search(actionSearch.Text);foreach(var kind in matches){ActionKind choice=kind;var button=ActionArt.Button(kind,delegate{Guard(delegate{AddStep(choice);});});tooltips.SetToolTip(button,ActionCatalog.Describe(kind));InstallActionHelp(button,kind);searchResults.Controls.Add(button);}bool shortcut=KeyboardChord.Matches(actionSearch.Text);if(matches.Length==0&&!shortcut)searchResults.Controls.Add(new Label{Text="No matching actions. Try image, click, wait, text, or loop.",AutoSize=true,Padding=new Padding(8,15,0,0),Tag="muted"});if(shortcut)searchResults.Controls.Add(ShortcutTile());searchSummary.Text=searchResults.Controls.OfType<Button>().Count()+" found";UiTheme.Apply(searchResults);}else searchSummary.Text=SearchShortcutHint();searchResults.ResumeLayout();RefreshCategoryHint();
        }
        private void BuildWelcome()
        {
            var content=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1};content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Absolute,44));content.RowStyles.Add(new RowStyle(SizeType.Absolute,preferences.RecentFiles!=null&&preferences.RecentFiles.Length>0?72:0));empty.Dock=DockStyle.Fill;content.Controls.Add(new WelcomeGuide{Dock=DockStyle.Fill,Font=Font},0,0);
            var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true,Tag="toolbar"};content.Controls.Add(actions,0,1);
            var record=AddButton(actions,"● Record a task",delegate{ToggleRecord();},false);record.Tag="primary";editing.Add(record);AddButton(actions,"Add mouse click",delegate{AddStep(ActionKind.MouseClick);},true);AddButton(actions,"Add image condition",delegate{AddStep(ActionKind.IfImage);},true);AddButton(actions,"Explore guides",delegate{ShowGuide(null);},false);recentFlows=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true,Tag="toolbar",AccessibleName="Recent workflows"};welcomeContent=content;content.Controls.Add(recentFlows,0,2);RefreshRecentFiles();welcome.Controls.Add(content);
        }
        private void RefreshRecentFiles()
        {
            if(recentFlows==null)return;foreach(Control control in recentFlows.Controls.Cast<Control>().ToArray()){editing.Remove(control);tooltips.SetToolTip(control,null);control.Dispose();}
            var paths=(preferences.RecentFiles??new string[0]).Where(p=>!string.IsNullOrWhiteSpace(p)).Take(3).ToArray();welcomeContent.RowStyles[2].Height=paths.Length>0?72:0;
            recentFlows.Controls.Add(new Label{Text="Recent workflows",AutoSize=true,Padding=new Padding(0,8,8,0),Tag="muted"});foreach(string path in paths){string chosen=path;var button=AddButton(recentFlows,Path.GetFileName(path),delegate{OpenRecent(chosen);},true);button.AccessibleName="Open recent "+Path.GetFileName(path);button.AutoSize=false;button.Width=180;tooltips.SetToolTip(button,path);}UiTheme.Apply(recentFlows);
        }
        private void UpdateNavigation()
        {
            while(blockNavigation.Controls.Count>0)blockNavigation.Controls[0].Dispose();var step=Selected;if(step==null)return;int index=SelectedIndex;
            Action<string,int> link=delegate(string label,int target){var button=new ThemeButton{Text=label,AccessibleName=label,AutoSize=true,Height=30,Tag="quiet",Enabled=!busy,Margin=new Padding(0,0,3,0)};button.Click+=delegate{RefreshList(target);};blockNavigation.Controls.Add(button);};
            if(Plan.IsIf(step.Kind)||step.Kind==ActionKind.Repeat){int end=EditorBlocks.End(macro.Steps,index);if(Plan.IsIf(step.Kind)){int depth=0;for(int i=index+1;i<end;i++){var k=macro.Steps[i].Kind;if(Plan.IsIf(k)||k==ActionKind.Repeat)depth++;else if(k==ActionKind.EndIf||k==ActionKind.EndRepeat)depth--;else if(k==ActionKind.Else&&depth==0){link("Go to ELSE",i);break;}}}link("Go to end",end);}
            int parent=InsertionContext.Parent(macro.Steps,index);if(parent>=0)link("Parent block",parent);UiTheme.Apply(blockNavigation);
        }
        private bool CanMove(int direction)
        {
            if(ChosenRows.Length>1||Selected==null||EditorBlocks.IsBoundary(Selected.Kind))return false;int n=SelectedIndex,end=EditorBlocks.End(macro.Steps,n);if(direction>0)return end+1<macro.Steps.Count&&!EditorBlocks.IsBoundary(macro.Steps[end+1].Kind);int prior=EditorBlocks.Previous(macro.Steps,n);return prior>=0&&!((Plan.IsIf(macro.Steps[prior].Kind)||macro.Steps[prior].Kind==ActionKind.Repeat)&&EditorBlocks.End(macro.Steps,prior)>=n);
        }
        private string SearchShortcutHint(){var binding=preferences.ActionShortcuts.First(s=>s.Command==AppCommand.FindAction);return binding.Key==0?"All actions":binding+" to search";}
        private string IdleStatus(){int off=macro.Steps.Count(s=>!s.Enabled);return macro.Steps.Count+" steps"+(off>0?" ("+off+" off)":"")+"  ·  "+(forever.Checked?"Repeat until Stop":repetitions.Value==1?"Run once":"Run "+repetitions.Value+" times")+"  ·  Speed "+speed.Value.ToString("0.#")+"×  ·  "+KeyName(AppCommand.Stop)+" stops";}
        private void RefreshIdleStatus(){if(!busy)SetStatus(IdleStatus());}
        private bool CommandAvailable(AppCommand command)
        {
            if(busy)return Shortcuts.Global(command);if(ChosenRows.Length>1&&new[]{AppCommand.EditStep,AppCommand.MoveUp,AppCommand.MoveDown,AppCommand.PlayFrom,AppCommand.PlayBlock}.Contains(command))return false;if(command==AppCommand.Undo)return undo.Count>0;if(command==AppCommand.Redo)return redo.Count>0;if(command==AppCommand.MoveUp)return CanMove(-1);if(command==AppCommand.MoveDown)return CanMove(1);
            if(command==AppCommand.PlayFrom)return Selected!=null&&!EditorBlocks.IsBoundary(Selected.Kind)&&StepPresentation.BalancedTail(macro.Steps,SelectedIndex);
            if(new[]{AppCommand.EditStep,AppCommand.Duplicate,AppCommand.Delete,AppCommand.PlayBlock}.Contains(command))return Selected!=null&&!EditorBlocks.IsBoundary(Selected.Kind);return true;
        }
        private void CommitAddedStep(Step step){CommitSequence(EditorBlocks.Create(step));}
        internal Macro DocumentHeadless(){if(!headless)throw new InvalidOperationException();return Storage.Decode(Storage.Encode(macro));}
        internal void AddHeadless(ActionKind kind){if(!headless)throw new InvalidOperationException();CommitAddedStep(new Step{Kind=kind,Text="Headless inserted step"});}
        internal void SearchHeadless(string query){if(!headless)throw new InvalidOperationException();actionSearch.Text=query;}
        internal void InsertBeforeHeadless(bool value){if(!headless)throw new InvalidOperationException();SetInsertion(value);}
    }
}
