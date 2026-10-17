# Build an image macro

Choose **Images & conditions → Click image**. Capture a small, distinctive part of the target or import a PNG. Choose the search area and use **Test find**. To restrict matching to a custom area, click **Select search area on screen…** and drag a rectangle, just like reference capture. It selects Rectangle mode and fills X/Y/Width/Height automatically. Esc or right-click cancels; a zero-size drag is ignored. You can still adjust the numbers manually. The test reports screen coordinates without sending a click. Save the step, then play the macro. The action finds the target at runtime and clicks its center; Match offset changes the click point.

Use tolerance 0 for identical pixels. Increase it slightly for rendering differences. Large tolerance values increase false matches. Capture at the same display scale as playback; matching does not resize images. Cursor effects, hover states, overlays and animations can change pixels. Prefer a reference unaffected by those changes. Transparent PNG pixels form an exclusion mask.

## IF image found and nested conditions

Click **IF image found** and set the first reference. The editor inserts:

```text
IF image found: outer reference
ELSE
END IF
```

Select the IF row, then add another IF image action. It is inserted inside the first block. Add actions immediately after each IF for its successful branch, or after ELSE for its missing branch:

```text
IF image found: outer reference
    IF image found: inner reference
        Mouse click: Last image / pixel match
    ELSE
        Note: inner reference missing
    END IF
    Mouse click: Last image / pixel match  # uses outer reference again
ELSE
    Note: outer reference missing
END IF
```

All these blocks live in one `.mexec` file. More nested levels and sibling IF blocks use the same pattern. Outer false branches skip inner searches entirely. An inner failed search provides no LastMatch inside its ELSE; attempting to click it stops with a clear error instead of reusing an old match. The outer result is restored after the inner END IF. If the screen changes after a match, use a new Click image search rather than relying on old coordinates.

Select the IF or Repeat start row to move, duplicate, delete, or disable its complete block. Adding an action uses the row immediately after the selection, so selecting an IF inserts into its true branch and selecting its END IF inserts outside it. Boundary rows cannot be deleted individually through the block commands.

## Waiting and continuous playback

**Wait image** waits for appearance; **Click image** waits and clicks. Set a timeout and choose whether a missing image stops playback or skips that action. IF image checks once by default; enable waiting to let it appear before choosing a branch. Inversion chooses the missing-image branch after the configured presence check/wait.

**Forever** repeats the entire macro until F10. A Repeat block with count 0 repeats until F10 or a Break inside it. Polling and control-only loops yield so cancellation remains responsive. Paused image waits do not poll or click. The delay speed control changes Delay steps; search timeouts remain in real milliseconds.

Templates captured/imported through the editor are embedded. Saving an older file-based image macro embeds its references, and both UI and CLI export embed images after flattening includes. Executables, scripts, included source files and other file-operation targets remain dependencies.

MacroExecute has its own format and original icons/interface. It does not import proprietary `.mcr` files or promise identical vendor settings.

## Multiple reference images

Image actions support a primary reference plus up to 63 embedded alternatives. In the guided editor, use **Add images…** or **Capture +**, then **Move up**, **Move down**, or **Remove** to arrange the search order. The primary reference has the preview above the list; moving an alternative to the first position makes it the primary reference. Capture screen and Import image replace that primary reference.

Leave **Require ALL reference images** off to try images in order and stop at the first hit. Enable it to require every reference in the same screen capture, stopping at the first miss. All references share the search area and tolerance. Waiting repeats the complete check within one shared timeout; ALL requires simultaneous presence, rather than remembering images across polling attempts.

The first matching reference supplies the last-match position. Click image clicks once after the full condition succeeds. A missing ALL reference clears the result and prevents a click. Inverted IF negates the combined condition; nested IF blocks keep their normal scoped match positions. Existing single-image workflows retain their behavior.
