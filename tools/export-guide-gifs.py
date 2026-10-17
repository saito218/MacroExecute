"""Encode and verify GIFs from MacroExecute --export-guide-frames output. Requires Pillow."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageChops

NAMES = ("if-image-found", "if-image-missing", "click-image", "repeat", "run-program")
FRAME_MS = 120
FRAME_COUNT = 80


def export(source, destination):
    destination.mkdir(parents=True, exist_ok=True)
    reports = []
    for name in NAMES:
        paths = sorted((source / name).glob("*.png"))
        if len(paths) != FRAME_COUNT:
            raise ValueError(f"{name}: expected {FRAME_COUNT} frames, got {len(paths)}")
        with Image.open(paths[0]) as first:
            palette = first.convert("RGB").quantize(colors=256)
        frames = []
        try:
            for path in paths:
                with Image.open(path) as image:
                    frames.append(image.convert("RGB").quantize(palette=palette, dither=Image.Dither.NONE))
            output = destination / (name + ".gif")
            frames[0].save(output, save_all=True, append_images=frames[1:],
                           duration=[FRAME_MS] * FRAME_COUNT, loop=0, optimize=False, disposal=1)
            with Image.open(output) as gif:
                if gif.info.get("loop") != 0 or gif.n_frames < 4:
                    raise ValueError(f"{name}: missing loop or animation frames")
                elapsed = 0
                for index in range(gif.n_frames):
                    gif.seek(index)
                    expected = frames[elapsed // FRAME_MS].convert("RGB")
                    actual = gif.convert("RGB")
                    try:
                        if ImageChops.difference(expected, actual).getbbox() is not None:
                            raise ValueError(f"{name}: decoded frame {index} differs from the renderer")
                    finally:
                        expected.close()
                        actual.close()
                    elapsed += gif.info["duration"]
                if elapsed != FRAME_MS * FRAME_COUNT:
                    raise ValueError(f"{name}: incorrect total duration {elapsed}")
                reports.append(dict(file=output.name, frames=gif.n_frames, duration_ms=elapsed,
                                    bytes=output.stat().st_size))
        finally:
            palette.close()
            for frame in frames:
                frame.close()
    (destination / "animation-validation.json").write_text(json.dumps(reports, indent=2), encoding="utf-8")
    return reports


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="animation-frames directory from headless previews")
    parser.add_argument("destination", type=Path, help="directory for GIF previews and validation report")
    args = parser.parse_args()
    print(json.dumps(export(args.source, args.destination), indent=2))
