# Playback behavior, permissions and signing

## Input timing and text

Existing workflows retain the 30 ms key/click hold and inclusive RGB threshold. New optional fields are `InputHoldMs` (0–60000), `TextIntervalMs` (0–60000), `TextEntry` (0 Unicode, 1 clipboard paste), and `ToleranceMode` (0 inclusive, 1 legacy RGB plus one). Missing fields use constructor defaults. New behavior requires this application version; older readers may ignore these additive schema-v1 fields.

The default Unicode path never uses the clipboard. CRLF is one Enter; lone CR and LF are each Enter; tabs use Tab. Surrogate pairs are sent together before character pacing. Unicode typing is not physical scan-code typing and some games or custom controls may reject it. Clipboard paste explicitly sends Ctrl+V, allows at least 100 ms to settle (or TextIntervalMs if longer), then checks that its sequence is nonzero and unchanged before restoring the snapshot. This is a best-effort ownership check, not an atomic compare-and-swap with other processes. It materializes advertised native formats before replacement and aborts if snapshotting fails. External clipboard changes win. Restoration is best effort if another process has locked the clipboard; delayed-rendered/custom clipboard objects and targets that consume paste asynchronously still need target-specific checking.

Holds, character pacing and waits pause with the runner and respond to cancellation. Playback speed affects Delay actions only. Delay accounting uses elapsed monotonic time rather than assuming requested sleeps finish exactly on time; Windows scheduling is not a real-time guarantee.

## Images and random choices

Inclusive matching accepts a pixel when each RGB channel differs by at most Tolerance; zero is exact. LegacyRgbPlusOne uses min(255, Tolerance + 1), matching the threshold convention observed in the earlier installed-artifact analysis. It is not a percentage similarity score. Fully transparent template pixels are excluded. Matching remains translation-only, at the captured scale; no OCR, scale or rotation invariance is claimed. IF matching remains scoped, cancellation-aware and independently testable with synthetic desktop images.

The optional root `RandomSeed` is saved, included in selection playback and preserved in exported players. Included workflows share the root's streams; child seeds do not reset execution. Repeat counts and delay ranges include both endpoints; coordinate jitter is uniform on the integer square [-radius, radius] on each axis. Current-pointer mode intentionally does not move or jitter. Identical seeds reproduce choices for the same execution path and runtime, not necessarily across a future algorithm change or different branches.

## Scripts

C# remains a standalone executable with a normal Main entry point, compiled by CodeDOM and run in its temporary directory. It does not share MacroExecute's in-process objects. The timeout now includes compilation and execution; cancellation/timeout stops waiting for compilation without launching the result, and cleanup is deferred until compilation finishes. CodeDOM compilation itself cannot be forcibly interrupted. Script stderr/stdout diagnostics are bounded to approximately 8 KB; a nonzero exit reports collected diagnostics. The immediate child is killed on timeout/cancellation. Descendants are not guaranteed to terminate; this is not a sandbox. Only trusted scripts should run, especially in administrator mode. Automated checks use simulated compilation tasks and never compile/run workflow C#.

## Windows permissions

Microsoft documents that SendInput is limited by User Interface Privilege Isolation (UIPI); GetLastError cannot identify UIPI as the cause. MacroExecute compares the caller and target process integrity levels when readable. Known higher-integrity mismatches produce a specific error before keyboard input or native mouse movement/clicks. Inaccessible process tokens remain unknown; Windows remains the enforcement boundary. Token/UIAccess checks do not establish that a target accepts synthetic input.

Settings → Permissions / administrator shows the current mode. A user-confirmed restart uses the standard Windows runas/UAC flow after saving, and reopens the workflow without auto-play. Cancellation keeps the current application open. Administrator mode grants the same privileges to workflow actions and scripts. Secure desktops, UAC prompts and target-side input restrictions are not bypassed. The application still declares asInvoker and uiAccess=false.

UIAccess is not a general-purpose automation toggle. Microsoft reserves it for qualifying assistive technologies and requires appropriate Authenticode signing and secure installation. This project does not claim eligibility or change Windows security policy.

Sources: [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput), [UIAccess requirements](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-securityoverview).

## Exported players and release signing

Payload SHA-256 detects corruption, not publisher identity. Player reads allow a PE certificate table after the payload and up to seven alignment bytes. Re-export strips the old certificate table and clears its directory/checksum because an old signature cannot authenticate new bytes. Exported files are unsigned until separately signed. `--verify-player` checks workflow structure and decodes embedded reference images; external programs and file-operation targets remain dependencies, not automatically bundled assets.

`sign-release.ps1 -Path .\dist\MacroExecute.exe -CertificateThumbprint YOUR_CERTIFICATE_THUMBPRINT -TimestampServer YOUR_ISSUER_TIMESTAMP_URL` signs a temporary copy with SHA-256 and verifies the signature before replacing the selected file. Use an existing, trusted code-signing certificate in CurrentUser/My with its private key. No certificate is generated, purchased, trusted or installed by the script. Use the issuer's timestamp URL. Sign each final exported player separately, then run `--verify-player` again. The script does not set UIAccess.

No real signing certificate was available for this pass. Certificate-table tests use synthetic PE fixtures, not trusted signatures. Live elevated-window input, UAC restarts, real clipboard paste, and user C# execution were not exercised because testing must stay headless and nonintrusive. These remain acceptance checks before claiming interchangeability with another product.
