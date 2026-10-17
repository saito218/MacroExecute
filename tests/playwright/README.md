# Playwright Test: headless native regression suite

MacroExecute uses native WinForms, not HTML or WebView2. Playwright Test is the orchestration/assertion/reporting layer here; it does not pretend native controls are browser DOM elements. No browser is downloaded or launched, and no desktop input is injected.

## Run on Windows

Prerequisites: .NET Framework 4.8 and Node.js 22 with npm. Application runtime requirements are unchanged; Node/Playwright are development-only tools.

```powershell
./build.ps1 -Test -Preview
npm ci --ignore-scripts --no-audit --no-fund
npm test
```

Open `playwright-report/index.html` as a static report, or explicitly run `npm run test:report` to start Playwright's report viewer. CI uploads the HTML report, JUnit XML, per-test JSON requests/results, hidden Aftershock PNGs and native assertion report even on failures. Request JSON files contain generated workflows that reproduce each case.

Use `npm test -- --grep "1000"` or `npm test -- nesting.spec.js` to select cases. Thirty fixed pseudorandom seeds are deliberately reproducible; failures identify the seed and preserve its request.

## Coverage

- Exact ordered execution of 0, 1, 100, 101, 250 and 1,000 actions.
- Validation at 1,000, 10,000, 100,000 and 100,001 steps (last one must fail). Validation is not a claim that the editor or playback performs well at 100,000 steps.
- 16, 64 and 256 IF levels, all true or false at root/middle/leaf, checking exact evaluation and action traces, scope depth and cleanup.
- 30 generated tree workflows combining IF/ELSE, finite repeats and disabled subtrees. A separate JavaScript tree interpreter produces the expected traces, independently of the native flat interpreter's indexes and stacks. Some seeds expand to over 1,000 executed actions.
- Hidden Aftershock editors with 100/500/1,000 rows: selecting/finding the final task, checking that the number column fits the full step index, moving task zero to the end, and moving a complete 64-level block. These drive real hidden controls and edit methods; they do not simulate an interactive OLE drag loop.
- Mixed-step Unicode persistence, 150-action child inclusion twice with independent labels, include cycles/depth limits, and export/verification of a 1,000-step portable player without executing that player.
- `.mexec`/legacy CLI loading, new-format image persistence/export verification, and mixed `.mexec` → `.mmacro.json` → `.mexec` nested includes. The suite contains 120 scenarios in total.
- Invalid structures/ranges, forbidden jumps, failure midway through playback, pause/resume, stop while paused/delayed, inner BREAK, and cancellation of three kinds of infinite workflows.
- The existing native suite, including image matching, capture-selection models, input cleanup, hotkey conflict models, history and synthetic native drag events. Four added native tests run the real image host on a synthetic desktop through 64 nested image IFs, with missing images at root/middle/leaf and restoration of outer image coordinates. Only the synthetic desktop receives clicks.

## Isolation and limits

Each Playwright case runs the actual compiled executable in a separate hidden process via `--headless-probe request.json result.json`. The probe always uses a fake action host. It records clicks, typing, clipboard/script/launch steps as strings rather than performing them; do not interpret these tests as acceptance of those OS integrations. Conditions in probe runs use `true:` or `false:` text to supply controlled outcomes. Native image regressions separately exercise the real image implementation against synthetic bitmaps.

The probe has a 45-second default execution deadline and a 20,000-dispatch ceiling. Explicit cancellation tests select shorter limits. These are test-harness limits, not production workflow limits. Windows may round the engine's 1 ms yields to coarser timer intervals, so action-heavy cases need seconds or tens of seconds; tests assert output correctness rather than fragile universal throughput targets. Child processes have an outer 55-second timeout, Playwright cases 60 seconds, and the CI job 10 minutes. One worker prevents concurrent hidden UI cases from competing for native resources; retries are disabled so failures remain visible.

The application protocol also reports errors as JSON instead of opening dialogs. Wrong probe argument counts return exit code 2. Reports contain error type, ordered actions/conditions, cancellation, dispatch count, scope depth/reset count, release state, elapsed time and UI selection/matches.

This suite cannot cover every possible workflow or prove real target-app compatibility, privileged input, DPI/multi-monitor behavior, native recording hooks, Task Scheduler execution, arbitrary C# child programs, or interactive drag cursor behavior. These remain separate acceptance work requiring explicit authorization. Tests never activate a form, register global hooks/hotkeys, capture the live desktop, alter the real clipboard or modify real user preferences.

Playwright Test and its runner dependencies are pinned in package-lock.json, licensed Apache-2.0 (see installed package LICENSE/NOTICE files). They are not bundled into MacroExecute.exe. The application's own license is unchanged.

Seven settings/design scenarios render preferences, shortcuts, workflow bindings, recent workflows and scheduling headlessly in Aftershock, and exercise picker and shortcut searches.

Eight behavior scenarios exercise the real WindowsHost text path through injected fake keyboard/clipboard devices, invalid options and deterministic random seeds. No actual desktop input or clipboard changes occur.

When the normal executable is in use, build an isolated copy with `./build.ps1 -Test -Preview -OutputDirectory obj`. Set `$env:MACROEXECUTE_TEST_EXE = (Resolve-Path obj/MacroExecute.exe).Path` before `npm test` to run probes and the native suite against that exact build. Remove the variable afterwards to restore the default `dist/MacroExecute.exe` target.

The native-suite case additionally covers the searchable guide workspace, action-help entry points, all six compact/wide palette combinations and deterministic stage scrubbing. It has a 60-second child deadline within a 75-second case limit to accommodate the extra hidden rendering. Normal probe limits remain unchanged.
