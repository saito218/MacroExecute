using System;
using System.Collections.Generic;

namespace MacroExecute
{
    internal static class ActionExamples
    {
        private static readonly Dictionary<ActionKind,string> examples=new Dictionary<ActionKind,string>{
            {ActionKind.CopySelection,"Focus the target application, hover the item, then copy with Ctrl+C.\nEnable Wait for fresh clipboard text (5 seconds) so a failed copy stops instead of checking stale data. Clear clipboard before the hover/copy chain. The copied text is kept for IF clipboard matches."},
            {ActionKind.Note,"Text: Sign-in steps start here.\nThis documents the workflow; it performs no input."},
            {ActionKind.Delay,"Wait duration: 2 seconds.\nThe next action starts after the delay. Set Random maximum to 3 seconds to vary between 2 and 3."},
            {ActionKind.KeyDown,"Key: ControlKey.\nFollow with Press key C and Key up ControlKey for Ctrl+C. The Keyboard shortcut tile builds the whole combination for you."},
            {ActionKind.KeyUp,"Key: ControlKey.\nReleases a modifier held by an earlier Key down. Pair held keys with releases."},
            {ActionKind.KeyPress,"Key: Enter.\nFocus the target app first. Use the Keyboard shortcut tile for Ctrl/Alt/Shift combinations."},
            {ActionKind.MouseMove,"Click / move at: Absolute screen coordinates; X: 500; Y: 300.\nMoves the pointer there without clicking. Pick pointer position can capture coordinates."},
            {ActionKind.MouseDown,"Button: Left; location: Current pointer.\nStart a drag, add Mouse move, then Mouse up to finish it."},
            {ActionKind.MouseUp,"Button: Left; location: Current pointer.\nCompletes the drag started by Mouse down."},
            {ActionKind.MouseClick,"Location: Current pointer; Button: Left.\nTick Alt under Hold during click for an Alt-click in one step. Ctrl and Shift can be combined too. Choose Single/Double/Triple and a hold duration; Long press presets 750 ms. Inside an image IF, Last image / pixel match uses the found location."},
            {ActionKind.MouseWheel,"Wheel delta: -120; location: Current pointer.\nScrolls down one standard wheel increment in the target app. Positive 120 scrolls up."},
            {ActionKind.TypeText,"Text: Hello world!\nActivate the intended window and click its text field before this step. Unicode typing keeps the clipboard untouched."},
            {ActionKind.SetClipboard,"Text: Order 123.\nStores this text on the clipboard. Add Paste to send it to the focused field."},
            {ActionKind.TypeDate,"Date format: yyyy-MM-dd.\nTypes a date such as 2026-10-16 into the focused field."},
            {ActionKind.TypeEnvironment,"Environment variable: USERNAME.\nTypes the current Windows user name. Enter the name without percent signs."},
            {ActionKind.Launch,"OPEN A DOCUMENT\nChoose a file: D:\\Reports\\report.pdf. Windows opens it in the default PDF app.\n\nRUN A PROGRAM\nChoose notepad.exe. Under Extra options, Arguments can contain \"D:\\Notes\\todo.txt\".\n\nRUN A SCRIPT\nChoose python.exe and use Arguments: \"D:\\Tasks\\daily report.py\". A script needs a suitable installed interpreter or file association.\n\nThe file-type menu only filters Browse. This step continues after starting the app; add Wait window when later steps need it ready."},
            {ActionKind.OpenUrl,"Website URL: https://example.com\nOpens the site in the default browser. Use an HTTP or HTTPS URL."},
            {ActionKind.ActivateWindow,"Window title contains: Notepad.\nBrings a matching window to the foreground before typing or clicking. The app must already be running."},
            {ActionKind.MoveWindow,"Advanced fields: Text = Notepad; X = 100; Y = 100; Width = 900; Height = 600.\nMoves and sizes the matching window."},
            {ActionKind.WaitWindow,"Window title contains: Notepad; Timeout: 5000 ms in Advanced fields.\nWaits for the matching window before continuing."},
            {ActionKind.FindPixel,"Advanced fields: ColorRgb = #42C6AE; Tolerance = 0.\nFinds that exact color in the selected search area and remembers its position for Last image / pixel match."},
            {ActionKind.WaitPixel,"Advanced fields: ColorRgb = #42C6AE; TimeoutMs = 5000.\nWaits for that color to appear before continuing."},
            {ActionKind.FindImage,"Capture or import a small, distinctive reference, such as a button.\nUse Test find to check it without clicking. A later mouse step can use Last image / pixel match."},
            {ActionKind.IfPixel,"Advanced fields: ColorRgb = #42C6AE; Tolerance = 0.\nIF runs its true branch when that color is found; ELSE handles absence. END IF closes the block."},
            {ActionKind.IfImage,"Capture a Login button, then use Test find.\nIF image found → add a click using Last image / pixel match.\nELSE → add a note or recovery action.\nEND IF\n\nAdd another IF inside either branch to nest conditions. Enable Wait before deciding when the image may appear later."},
            {ActionKind.IfClipboard,"Enter \"Monthly Report\" in the first row. Use + then AND to require \"Status: Approved\" in another row.\nSpaces separate OR alternatives; quotes keep a phrase together. Each AND row needs a different matching string. + then GROUP OR starts an alternative complete combination. Try the sample tester before saving. Existing exact/legacy conditions retain their behavior until converted."},
            {ActionKind.IfWindow,"Window title contains: Notepad.\nIF runs its true branch when a matching window exists. ELSE runs when none matches."},
            {ActionKind.Else,"Actions after ELSE run when the matching IF condition is false.\nSelect the IF header to edit the condition or move the complete block."},
            {ActionKind.EndIf,"END IF closes its matching IF / ELSE block.\nAdd steps after END IF to continue regardless of the chosen branch."},
            {ActionKind.Repeat,"Repetitions: 3.\nPut Click and Wait steps inside the block. END REPEAT closes it. A count of 0 repeats until Stop or Break."},
            {ActionKind.EndRepeat,"END REPEAT closes its matching Repeat block.\nUse the Repeat header to change the count, duplicate or move the whole block."},
            {ActionKind.Break,"Place Break inside Repeat, often in an IF branch.\nFor example, IF image found → Break exits the nearest repeat when the success image appears."},
            {ActionKind.Label,"Label name: retry.\nAdd Jump with the same name to return here. Labels must be unique within their workflow scope."},
            {ActionKind.Jump,"Label name: retry.\nContinues at the matching Label. Do not jump into an inactive IF or Repeat block; Stop remains available for loops."},
            {ActionKind.Include,"MacroExecute file: .\\parts\\sign-in.mexec.\nRuns those steps here, then continues with this file. Includes may contain other includes; cycles are rejected."},
            {ActionKind.Message,"Text: Task finished.\nDisplays a message during playback. Use Note instead for comments that should not interrupt playback."},
            {ActionKind.Pause,"Insert before a task requiring manual input.\nPlayback waits until you resume it with your configured Play/Pause shortcut. Stop cancels it."},
            {ActionKind.CopyFile,"Advanced fields: Target = D:\\Input\\report.csv; Text = D:\\Output\\report.csv.\nCopies the source to the destination. An existing destination causes an error."},
            {ActionKind.DeleteFile,"File path: D:\\Temp\\old-report.txt.\nDeletes that file during playback. Use a disposable test file when checking this step."},
            {ActionKind.IfFile,"File path: D:\\Reports\\ready.csv.\nThe true branch runs when that file exists; ELSE handles a missing file."},
            {ActionKind.IfProcess,"Process name: notepad.\nThe true branch runs while that process is running. Use the process name rather than its window title."},
            {ActionKind.WaitProcess,"Process name: notepad.\nWaits for the process to exist, not for it to finish. Configure TimeoutMs in Advanced fields."},
            {ActionKind.ClipboardIncrement,"Advanced fields: DelayMs = 1.\nIf clipboard text is 41, it becomes 42. Clipboard text must contain an integer."},
            {ActionKind.ClipboardClear,"Clears the current clipboard contents during playback.\nUse Set clipboard or Paste when you need to provide specific text instead."},
            {ActionKind.Paste,"Activate the target field, then add Paste.\nSends Ctrl+V using the current clipboard contents. Set clipboard can supply the text first."},
            {ActionKind.RunCSharp,"Text: System.Console.WriteLine(\"Hello from a macro\");\nRuns a C# script in a child process. Console output is not typed into the target application. Only run code you trust."},
            {ActionKind.WaitImage,"Capture a Ready indicator; Timeout: 5 seconds.\nPlayback waits for it to appear. Wait forever until Stop removes the time limit."},
            {ActionKind.ClickImage,"Capture a small button reference and use Test find.\nThis action finds and clicks its center automatically. Choose a search area, a timeout and optional match offsets. Hold during click supports Alt/Ctrl/Shift; double/triple and long presses work here too. Modifiers are pressed only after the image search succeeds."},
        };
        internal static string For(ActionKind kind){return examples[kind].Replace("\n","\r\n");}
        internal static string Field(ActionKind kind,string name)
        {
            switch(name){
                case "Reference image":return "Example: capture just a button and its distinctive edges. Use Test find before playback; keep the display scale the same as when captured.";
                case "Tolerance interpretation":return "Example: Inclusive RGB with tolerance 0 is exact. Legacy mode adds one to the threshold for older workflows.";
                case "Wait before deciding the IF branch":return "Example: enable this and set Timeout to 5 seconds to allow a loading indicator to appear before choosing the IF branch.";
                case "Invert: IF image is NOT found":return "Example: enable this for a recovery branch that should run when the reference cannot be found. ELSE handles the image being present.";
                case "Wait forever until Stop":return "Example: keep checking for a completion image until it appears or you press Stop. This ignores the finite timeout.";
                case "If missing: skip action and continue":return "Example: an optional popup may not appear. Enable this to continue when its image is missing instead of failing playback.";
                case "Move pointer to successful match":return "Example: enable this to move to the found image center. Disable it when you only need to test the condition. Click image still clicks its result.";
                case "Rectangle X":case "Rectangle Y":case "Width (pixels)":case "Height (pixels)":return "Example: X=100, Y=100, Width=800, Height=600 searches that screen rectangle. Use Select search area on screen to draw it instead of entering numbers.";
                case "Wheel delta":return "Example: -120 scrolls down one standard increment; 120 scrolls up. The app may interpret wheel increments differently.";
                case "Mouse button":case "Button":return "Example: Left activates a button; Right opens a context menu in most applications.";
                case "Press a key here":return "Example: focus this field and press Enter to select the Enter key. For Ctrl+C use the Keyboard shortcut tile.";
                case "Between characters / paste settle":return "Example: 50 ms slows Unicode typing between characters. In paste mode it gives the target time to consume the clipboard before restoration.";
                case "File path":return "Example: D:\\Reports\\ready.csv. IF file checks existence; Delete file removes it. Review the action before playback.";
                case "MacroExecute file":return "Example: .\\parts\\sign-in.mexec runs an included workflow. Its own includes can nest; recursive cycles are rejected.";
                case "Website URL":return "Example: https://example.com opens that website in the default browser. Include the http:// or https:// prefix.";
                case "Environment variable":return "Example: USERNAME types your current Windows user name. Enter the name without percent signs.";
                case "Enable this action":return "Example: turn this off to keep a step in the workflow while skipping it during playback. Disabling a block header disables its contained steps.";
                case "File type":return "Example: PDF documents (*.pdf) limits the Browse list to PDFs. It does not convert a file or change its default app.";
                case "Program or file":return "Example: notepad.exe, or D:\\Reports\\report.pdf. Browse chooses a file; you can also type a program name.";
                case "Arguments":return "Example: \"D:\\Notes\\todo.txt\" opens that document when the chosen program is notepad.exe. Keep the program path in its own field.";
                case "Search area":return "Example: choose Selected rectangle and use Select search area on screen to draw around the target app. Foreground window searches the active window.";
                case "Color tolerance":return "Example: 0 requires exact RGB values; 10 allows a difference up to 10 in each channel with the recommended interpretation. Raise it cautiously and use Test find.";
                case "Timeout":return "Example: 5 seconds waits up to five seconds. Wait forever ignores this limit. For an image IF, enable Wait before deciding to use a timed search.";
                case "Hold during click":return "Tick Alt for Alt-click. Ctrl, Shift and Alt may be combined; they stay down through the whole multi-click and are released afterwards. Previously held keys are preserved.";
                case "Click pattern":return "Single sends one click, Double two, Triple three at the same location. The app decides whether it recognizes a double/triple click. Use Between clicks to adjust the gap.";
                case "Between clicks":return "Example: 100 ms between releases and the next press. Used only for double/triple clicks. Modifiers remain held across the gap.";
                case "Press presets":return "Long press selects Single and a 750 ms button hold. Normal press restores a 30 ms hold. Both preserve selected modifiers, button and position.";
                case "Strings to check":return "Exact comparison uses the entire text unchanged, including line breaks. For expandable substring rules, choose String rows & groups.";
                case "Comparison":return "String rows: spaces mean OR, quotes enclose one phrase. AND requires a different matching string per row. GROUP OR starts an independent alternative group. Exact compares the complete clipboard.";
                case "Copy timeout":return "Example: 5 seconds allows the focused app to publish fresh non-empty text after Ctrl+C. If it does not, playback stops.";
                case "Check every":return "Example: 100 ms checks about ten times per second while waiting. Smaller intervals do more work.";
                case "Repetitions":return "Example: 3 runs the block three times; 0 repeats until Stop or Break.";
                case "Click / move at":return "Example: Current pointer leaves the pointer in place. Last image / pixel match uses the most recent successful match plus the offsets below.";
                case "X / horizontal offset":case "Y / vertical offset":return "Example: absolute X=500 and Y=300 selects a screen position. With Last match, X=10 and Y=0 shifts ten pixels right of that match.";
                case "Match offset X":case "Match offset Y":return "Example: X=10 and Y=0 targets ten pixels right of the found image center.";
                case "Wait duration":return "Example: 2 seconds pauses before the next step; 250 ms is a quarter of a second.";
                case "Random maximum (0=off)":return kind==ActionKind.Repeat?"Example: Repetitions=2 and Random maximum=5 chooses from 2 through 5. Zero disables randomization.":"Example: Wait duration=1 second and Random maximum=2 seconds varies the delay between them. Zero disables randomization.";
                case "Window title contains":return "Example: Notepad matches a window whose title contains Notepad. This is a window title, not an executable path.";
                case "Process name":return "Example: notepad identifies the process; a document title such as todo.txt is not the process name.";
                case "Label name":return "Example: retry. A Jump and its destination Label must use the same name.";
                case "Date format":return "Example: yyyy-MM-dd produces 2026-10-16; HH:mm produces a 24-hour time such as 14:30.";
                case "Button / key hold":return "Example: 30 ms holds the button briefly before release; increase it if the target misses fast input.";
                case "Text entry method":return "Example: Unicode types without replacing the clipboard. Paste text uses the clipboard temporarily and attempts to restore it.";
                default:return "Example for "+ActionArt.Name(kind)+":\n"+For(kind);
            }
        }
    }
}
