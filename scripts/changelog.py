"""Fold the changelog fragments (changes/*.md, one per pull request) into CHANGELOG.md as a new version's section,
then delete them. Each PR writes its own file, so parallel PRs never edit the same lines (changes/README.md).
Ported from ShotDetector's tools/changelog.py.

Usage: python scripts/changelog.py X.Y.Z[-pre] "One line on what the version is." [--date YYYY-MM-DD]
       python scripts/changelog.py --check   (CI: every fragment parses)
       python scripts/changelog.py --test    (self-check)"""
import argparse
import datetime
import re
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
# The release notes' order (release.yml publishes the section as it is); a heading may go on after the word,
# e.g. "Breaking changes (stylebro-migrate only)". "Upgrading" becomes "Upgrading from <previous version>" and starts
# with the bump line every release needs.
ORDER = ["Breaking changes", "New", "Changed", "Fixed", "Upgrading"]


def parse(path: Path) -> list[tuple[str, str]]:
    """The fragment's (heading, text) pairs."""
    sections, heading, lines = [], None, []
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("### "):
            if heading:
                sections.append((heading, "\n".join(lines).strip("\n")))
            heading, lines = line[4:].strip(), []
        elif heading:
            lines.append(line)
        elif line.strip():
            sys.exit(f"{path}: text before the first '### <section>' heading")
    if heading:
        sections.append((heading, "\n".join(lines).strip("\n")))
    if not sections:
        sys.exit(f"{path}: no '### <section>' heading")
    for h, text in sections:
        if not any(h == o or h.startswith(o + " ") for o in ORDER):
            sys.exit(f"{path}: section '{h}' isn't one of {', '.join(ORDER)}")
        if not text.strip():
            sys.exit(f"{path}: section '{h}' is empty")
    return sections


def fragments(root: Path) -> list[Path]:
    return sorted(p for p in (root / "changes").glob("*.md") if p.name != "README.md")


def fold(root: Path, version: str, summary: str, date: str) -> list[Path]:
    found = fragments(root)
    if not found:
        sys.exit("no fragments in changes/")
    merged: dict[str, list[str]] = {}
    for f in found:
        for heading, text in parse(f):
            merged.setdefault(heading, []).append(text)

    log = root / "CHANGELOG.md"
    raw = log.read_bytes().decode("utf-8")
    nl = "\r\n" if "\r\n" in raw else "\n"
    text = raw.replace("\r\n", "\n")
    if f"\n## {version} " in text:
        sys.exit(f"CHANGELOG.md already has {version}")
    previous = re.search(r"^## (\S+) ", text, re.M)
    tool = "dotnet tool update -g StyleBro.Migrate" + (" --prerelease" if "-" in version else "")
    upgrading = [f"Bump the package, then update the tool: `{tool}`."] + merged.pop("Upgrading", [])
    if previous:
        merged[f"Upgrading from {previous.group(1)}"] = ["\n\n".join(upgrading)]  # paragraphs, not bullets

    rank = lambda h: next(i for i, o in enumerate(ORDER) if h == o or h.startswith(o + " "))
    body = "".join(f"### {h}\n\n" + "\n".join(merged[h]) + "\n\n" for h in sorted(merged, key=rank))
    section = f"## {version} – {date}\n\n{summary}\n\n{body}"
    at = text.find("\n## ") + 1
    if at == 0:
        text, at = text.rstrip("\n") + "\n\n", len(text.rstrip("\n")) + 2
    log.write_bytes((text[:at] + section + text[at:]).replace("\n", nl).encode("utf-8"))
    for f in found:
        f.unlink()
    return found


def test():
    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        (root / "changes").mkdir()
        (root / "CHANGELOG.md").write_bytes(
            b"# Changelog\r\n\r\nIntro.\r\n\r\n## 0.5.0-alpha.1 \xe2\x80\x93 2026-01-01\r\n\r\nOld.\r\n")
        (root / "changes" / "README.md").write_text("not a fragment")
        (root / "changes" / "a.md").write_text("### Fixed\n\n- A fix.\n\n### New\n\n- A thing,\n  two lines.\n")
        (root / "changes" / "b.md").write_text(
            "### Upgrading\n\nRun `init` again.\n\n### Fixed\n\n- B fix.\n\n### Breaking changes (tool only)\n\n- Gone.\n")
        fold(root, "0.6.0-alpha.1", "Summary.", "2026-02-02")
        got = (root / "CHANGELOG.md").read_bytes().decode()
        want = ("# Changelog\r\n\r\nIntro.\r\n\r\n## 0.6.0-alpha.1 – 2026-02-02\r\n\r\nSummary.\r\n\r\n"
                "### Breaking changes (tool only)\r\n\r\n- Gone.\r\n\r\n### New\r\n\r\n- A thing,\r\n  two lines.\r\n\r\n"
                "### Fixed\r\n\r\n- A fix.\r\n- B fix.\r\n\r\n### Upgrading from 0.5.0-alpha.1\r\n\r\n"
                "Bump the package, then update the tool: `dotnet tool update -g StyleBro.Migrate --prerelease`.\r\n\r\n"
                "Run `init` again.\r\n\r\n## 0.5.0-alpha.1 – 2026-01-01\r\n\r\nOld.\r\n")
        assert got == want, got
        assert [p.name for p in (root / "changes").iterdir()] == ["README.md"]

        # A malformed fragment stops the fold (and --check) with its name.
        for bad in ["Text first.\n### Fixed\n\n- x\n", "### Faster\n\n- x\n", "### New\n\n", "- no heading\n"]:
            (root / "changes" / "bad.md").write_text(bad)
            try:
                parse(root / "changes" / "bad.md")
            except SystemExit as e:
                assert "bad.md" in str(e), e
            else:
                raise AssertionError(f"accepted: {bad!r}")
    print("ok")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("version", nargs="?")
    ap.add_argument("summary", nargs="?")
    ap.add_argument("--date", default=datetime.date.today().isoformat())
    ap.add_argument("--test", action="store_true")
    ap.add_argument("--check", action="store_true")
    args = ap.parse_args()
    if args.test:
        return test()
    if args.check:
        for f in fragments(ROOT):
            parse(f)
        return print(f"ok ({len(fragments(ROOT))} fragments)")
    if not args.version or not args.summary:
        ap.error("version and summary are required")
    if not re.fullmatch(r"\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?", args.version):
        ap.error(f"'{args.version}' isn't X.Y.Z or X.Y.Z-pre")
    for f in fold(ROOT, args.version, args.summary, args.date):
        print(f"folded {f.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
