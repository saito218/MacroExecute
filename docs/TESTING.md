# Testing

## Local commands

```powershell
./build.ps1 -Test -Preview
npm ci --ignore-scripts --no-audit --no-fund
npm test
```

Windows 10/11 and .NET Framework 4.8 are required. The native build uses the Windows framework compiler; application runtime packages and a .NET SDK are not required. Node and the pinned npm lockfile are development dependencies for the Playwright Test runner.

If the normal executable is in use, pass `-OutputDirectory obj` to the build and set `MACROEXECUTE_TEST_EXE` to that executable's absolute path before running npm tests. Never overwrite or close a user's running instance just to test.

## Desktop isolation

All verification is headless. Playwright uses one worker and orchestrates hidden native probes; it does not request a browser or page fixture. WinForms previews use hidden control handles and DrawToBitmap, never Show/ShowDialog. Synthetic hosts replace Windows input, clipboard, process, script and window operations in execution tests. No desktop capture, global input hooks, live clicks, keystrokes, schedules or network actions are exercised by the suite.

Aftershock is the default test palette. Explicit light/dark Neon, Aftershock, Crimson, Graphite, Ocean and Ember checks cover body, secondary, accent and button text contrast (at least 4.5:1), theme persistence and compact layouts. Test settings are isolated from user preferences.

## Coverage

- Workflow schema validation and old-format compatibility; `.mexec` round trips; external paths and reusable includes, independent label scopes, cycles and include-depth limits.
- Nested IF/ELSE and Repeat structure, disabled blocks, scoped image matches, Break and Jump behavior, deterministic random cases against an independent oracle.
- Real image matching against synthetic bitmaps: tolerance interpretation, limits, search bounds, region offsets, alpha/image conversion and nested match restoration. Capture helpers are tested without capturing the desktop.
- Clipboard exact/ANY/ALL/group rules, literal item text, case/NOT, fresh Ctrl+C waits, stale or missing data, modifier cleanup, read-lock retries, nested branches and both saved item examples through injected adapters.
- Recorder commit and insertion behavior through injected model steps; playback timing, cancellation, cleanup of held input and error propagation through fake hosts.
- 100-, 500- and 1,000-action workflows, large editor searches, block drag/drop and moving a row across a 500-row list without losing steps.
- Headless settings, shortcut conflicts and migrations, workflow pickers, validation/review navigation, guided editors and first-open/repeated guide expansion.
- Read-only examples, full-text guide search, related-action navigation, scenario switching, animation pause/replay/stage scrubbing and timer shutdown while hidden. Animated GIF export additionally verifies decoded pixels and total duration.
- Script compiler diagnostics and timeouts through isolated test fixtures; portable player checksums, corrupt payloads, signature-table alignment and permission policy validation.

The native suite currently contains 268 checks; Playwright has 183 cases and includes a native-suite invocation. Detailed reports are authoritative: do not infer a pass from successful compilation alone.

## Outputs and limits

Native results are written to the selected output directory's `test-results.txt`. Unexpected hidden-mode exceptions go to `last-error.txt`; the last running check is retained. Previews and raw test fixtures are generated artifacts. Playwright produces `test-results/` and `playwright-report/`; CI retains diagnostics when a run fails.

The native build script gives each hidden check three minutes. Normal execution probes have a 45-second internal deadline and 20,000-dispatch ceiling, with 55-second child and 60-second case limits. The rendering-heavy native-suite case has a 60-second child deadline within a 75-second case limit. These are test limits, not production workflow limits. Windows timer granularity can make action-heavy scenarios take several seconds. Tests assert correctness, not a universal throughput threshold. Retries are disabled.

GitHub Actions compiles, renders and runs the headless suites on Windows with a ten-minute job limit. Signing with a real certificate, UAC/secure-desktop interaction, elevated target access and application-specific live behavior are not established by these tests. No claim of identical behavior with another macro product is made.

## Animation asset regeneration

```powershell
./build.ps1 -Preview -GuideFrames
python tools/export-guide-gifs.py dist/animation-frames docs
```

Only this explicit option produces the 400 source PNG frames. Pillow is a development-only requirement for GIF encoding; the application draws its animations without an external dependency.

## Cleanup

`./clean.ps1` removes generated build/test outputs. `./clean.ps1 -KeepRelease -Dependencies` preserves the current `dist` release while removing temporary builds, reports and reinstallable npm packages. Source, examples, documentation, license, configuration and Git history are preserved. See [project structure](PROJECT-STRUCTURE.md).
