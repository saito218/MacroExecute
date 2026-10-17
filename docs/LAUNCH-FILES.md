# Run program / file

Choose **Windows & files → Run program / file**, or search for `launch`, `program`, `document`, `PDF`, `Python` or `PowerShell`.

Start with **Program or file**: press **Browse…**, or type a program name/path directly. The **File type** dropdown below it limits the Browse results. Open **Extra options → Command-line arguments** only when needed; existing arguments remain visible when editing an older step. The group filters the chooser; it does not convert files, install a handler, or force an interpreter. **All files** accepts other extensions too. Documents open through their Windows file association. Unknown types need a suitable installed application.

Expand **How to use this action · Example**, always below the action heading, for paired setup/outcome illustrations. Switch Program / Document / Script to compare the examples without changing your settings. The **?** buttons beside settings show field-specific help in that same panel. Help only explains settings; it does not execute or replace them.

Use **Arguments** under Extra options for command-line options. For predictable script execution, choose the interpreter executable and pass the quoted script path as an argument. For example, select `python.exe` with arguments `"D:\Tasks\daily report.py"`, or `powershell.exe` with `-File "D:\Tasks\daily report.ps1"`. Existing interpreter policies and permissions still apply. MacroExecute does not bypass them.

This action starts the program or opens the file and continues; it does not wait for completion. Add Wait window, Wait process, or another appropriate condition when the next step depends on the launched app. Use **Include** to execute another MacroExecute workflow inside the current sequence.

Files are external dependencies, including in exported players. Relative paths containing a directory resolve against the workflow's directory. Bare filenames also resolve there when a matching local file exists; otherwise Windows program-name lookup is preserved. Arguments are passed as entered and need their own quoting.

## File picker groups

| Group | Extensions |
|---|---|
| All files | *.* |
| Windows programs | *.exe, *.com |
| Windows shortcuts | *.lnk, *.url |
| Batch and command scripts | *.bat, *.cmd |
| PowerShell scripts | *.ps1, *.psm1 |
| Python scripts | *.py, *.pyw |
| JavaScript and VBScript | *.js, *.jse, *.vbs, *.vbe, *.wsf |
| AutoHotkey and AutoIt scripts | *.ahk, *.au3 |
| Shell and other scripts | *.sh, *.bash, *.pl, *.rb, *.lua |
| Installers and packages | *.msi, *.msp, *.msix, *.appx, *.msixbundle, *.appxbundle |
| PDF documents | *.pdf |
| Text and notes | *.txt, *.md, *.markdown, *.rtf, *.log |
| Word and OpenDocument text | *.doc, *.docx, *.docm, *.dotx, *.odt, *.ott |
| Spreadsheets and tables | *.xls, *.xlsx, *.xlsm, *.xlsb, *.ods, *.csv, *.tsv |
| Presentations | *.ppt, *.pptx, *.pptm, *.pps, *.ppsx, *.odp |
| Images and graphics | *.png, *.jpg, *.jpeg, *.bmp, *.gif, *.webp, *.tif, *.tiff, *.svg, *.ico, *.heic, *.avif |
| Audio | *.mp3, *.wav, *.flac, *.aac, *.m4a, *.ogg, *.opus, *.wma, *.aiff |
| Video | *.mp4, *.mkv, *.avi, *.mov, *.wmv, *.webm, *.mpeg, *.mpg, *.m4v |
| Archives and disk images | *.zip, *.7z, *.rar, *.tar, *.gz, *.bz2, *.xz, *.iso, *.img |
| Web pages | *.html, *.htm, *.mhtml, *.mht |
| Data and configuration | *.json, *.xml, *.yaml, *.yml, *.toml, *.ini, *.cfg, *.sql |
| Projects and source code | *.sln, *.slnx, *.csproj, *.vbproj, *.vcxproj, *.code-workspace, *.cs, *.cpp, *.h, *.java, *.ipynb |
| Design and 3D files | *.psd, *.ai, *.eps, *.xcf, *.kra, *.blend, *.obj, *.stl, *.fbx, *.dwg, *.dxf |
| Email and calendar | *.eml, *.msg, *.ics, *.vcf |
| Java applications | *.jar, *.jnlp |

Headless tests exercise filters, case-insensitive detection, unknown extensions, picker cancellation, argument/path preservation, inline empty-target validation and local-file lookup. They never launch a real process or open a real file association.
