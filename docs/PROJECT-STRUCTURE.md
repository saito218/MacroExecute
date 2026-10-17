# Project structure and generated files

The Git repository is `MacroExecute/`. The animated guides are the standard implementation in `src/ActionShowcase.cs`, used by the step editor and searchable guide workspace. There is one current executable at `dist/MacroExecute.exe`; GIFs in `docs/` are illustrations, not application dependencies.

| Path | Purpose |
| --- | --- |
| `src/` | Application, engine, Windows adapters, original UI, hidden probes and native checks |
| `tests/playwright/` | Native regression and stress orchestration; no browser tests |
| `examples/` | Small original `.mexec` workflows for learning and regression fixtures |
| `docs/` | Current usage, architecture, format, compatibility and testing references; curated screenshots/GIFs |
| `tools/export-guide-gifs.py` | Optional original GIF encoder and pixel/duration validator |
| `.github/workflows/` | Windows build and headless regression CI |
| `build.ps1`, `clean.ps1`, `sign-release.ps1` | Build, generated-file cleanup and optional certificate signing |
| `MacroExecute.csproj`, `app.manifest` | Windows project and application manifest |
| `package.json`, `package-lock.json`, `playwright.config.js` | Pinned development runner and configuration |
| `LICENSE`, `README.md` | Rights and entry-point documentation |

`dist/`, `bin/`, `obj/`, npm dependencies and test reports are generated and ignored by Git. `./clean.ps1 -KeepRelease -Dependencies` retains the current release while removing disposable builds, reports and reinstallable packages. `npm ci --ignore-scripts --no-audit --no-fund` restores the development dependencies.

Normal preview builds render screenshots only. `-GuideFrames` explicitly exports source frames when GIFs need regeneration. The application always retains its live animated examples.

Local agent instructions, personal notes, historical review records and business planning are kept outside this repository. Build outputs and local tooling state are excluded from Git.
