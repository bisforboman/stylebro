"""Writes scaled copies of a C# file for the per-file benchmark's scaling check (README.md, "Scaling").

    python scripts/benchmark/make-scaled.py <file.cs> <out folder> [factors, default 1,2,4,8]

Copy N keeps everything before the file's first block-scoped `namespace X` line and repeats the namespace block N times,
the copies renamed `X.CopyK` so the types don't collide. Time grows linearly when `file`'s total for copy 8 is ~8x copy 1.
"""

import re
import sys
from pathlib import Path


def main():
    source = Path(sys.argv[1])
    out = Path(sys.argv[2])
    factors = [int(f) for f in (sys.argv[3] if len(sys.argv) > 3 else "1,2,4,8").split(",")]
    text = source.read_text(encoding="utf-8-sig")
    match = re.search(r"^namespace ([\w.]+)\s*$", text, re.MULTILINE)
    if not match:
        sys.exit("No block-scoped namespace line found.")
    head, block = text[: match.start()], text[match.start():]
    out.mkdir(parents=True, exist_ok=True)
    for factor in factors:
        copies = [block.replace(f"namespace {match[1]}", f"namespace {match[1]}.Copy{k}", 1) for k in range(factor)]
        target = out / f"{source.stem}.x{factor}.cs"
        target.write_text(head + "\n".join(copies), encoding="utf-8")
        print(f"{target}: {target.stat().st_size / 1024:.0f} KB")


if __name__ == "__main__":
    main()
