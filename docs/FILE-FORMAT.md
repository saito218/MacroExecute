# MacroExecute workflow files: .mexec

Starting with version 1.10, new workflows use **`.mexec`**, for example `Daily report.mexec`. The extension identifies MacroExecute workflows; the contents remain UTF-8 JSON with schema version 1, the existing action identifiers and embedded image data. This is not encryption, an executable format, or a new incompatible schema.

Use Open to select `.mexec`, `.mmacro.json` or older `.json` workflows. Saving an already opened legacy workflow keeps its current filename. **Save As** suggests `.mexec` and leaves the original file in place unless you explicitly overwrite it. There is no automatic bulk renaming of user files.

Include accepts all these file endings, including mixed-format nesting. Relative include paths resolve from the file containing them. Existing cycle checks, independent label scopes and the 16-level include limit are unchanged. When renaming a file yourself, update references to it in parent workflows; no path-rewriting or extension guessing occurs.

The Include editor provides a Browse button using the same workflow picker as Open and saved-macro bindings. CLI validation, scheduling and exported-player creation accept `.mexec` through the existing content-based loader. Image templates remain embedded. Other external files remain explicit dependencies.

```powershell
./dist/MacroExecute.exe ./examples/nested-image-if.mexec
./dist/MacroExecute.exe --validate ./examples/nested-image-if.mexec
./dist/MacroExecute.exe --export ./examples/nested-image-if.mexec ./dist/MyPlayer.exe
```

No Explorer association or Windows default-app setting is registered by this change. Open the file from MacroExecute or pass its path on the command line. The JSON format is described by `Macro` and `Step` in `src/Model.cs`; manually edited files receive the same schema and structure validation as saved ones.

Version 1.12 adds optional root RandomSeed and step InputHoldMs, TextIntervalMs, TextEntry and ToleranceMode fields. Existing workflows retain defaults; earlier readers may ignore these fields. See [behavior contracts](COMPATIBILITY.md).

## Clipboard comparisons

Schema 1 adds optional `ClipboardMatch` (0 exact, 1 any substring, 2 all substrings, 3 any complete AND group), `ClipboardIgnoreCase`, `ClipboardNegate`, and `ClipboardWaitForChange`. Missing condition settings preserve exact, case-sensitive comparison. Criteria use Text; blank lines separate Groups. CopySelection is appended as action ID 47, without renumbering existing actions, and uses InputHoldMs, TimeoutMs and PollMs. New Copy steps wait for fresh non-empty text by default. Older application binaries do not implement action 47 and should not be used to run these new workflows.

## Modified clicks

Schema 1 adds optional MouseModifiers (flags: 1 Ctrl, 2 Shift, 4 Alt), ClickCount (1–3), and ClickIntervalMs (0–60000). Defaults are no modifiers, one click and a 100 ms inter-click gap. MouseModifiers is supported only for MouseClick and ClickImage. InputHoldMs remains the duration of each press. Existing action IDs remain unchanged. Older readers can ignore these settings and therefore cannot reproduce modified or multiple clicks. See [modified clicks](MOUSE-CLICKS.md).

## Clipboard row expressions

ClipboardMatch = 4 selects the expandable row-rule language stored in Text. Whitespace separates OR alternatives; double quotes preserve phrases. Standalone lines OR, AND and GROUP OR connect rows. OR joins alternatives into the same requirement; AND starts another required clause; GROUP OR starts an independent alternative group. Each AND clause requires a distinct matching criterion string within its group. Ignore-case comparison also applies to distinctness. There is no occurrence-count requirement. Up to 128 terms are allowed. Existing modes 0–3 retain their old contracts and are preserved by an untouched editor. See [clipboard workflows](CLIPBOARD-WORKFLOWS.md). Older binaries reject mode 4.

ClipboardReuseGroups is an optional array of zero-based Rules group indexes allowed to reuse the same matching criterion across AND requirements. Missing means an empty array: all groups require distinct strings. Indexes must be unique and inside the parsed group range. Only group-local reuse changes; every AND requirement must still match and groups remain independent. Clones deep-copy this array. The editor only offers reuse for shared parsed terms across different AND requirements, respecting case settings.

The editor no longer creates standalone OR connector rows. It merges existing saved OR rows into space-separated alternatives when displaying/saving them. The parser still accepts OR lines in existing workflows, so playback remains compatible.
