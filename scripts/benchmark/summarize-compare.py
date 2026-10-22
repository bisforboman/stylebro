"""Summarizes the logs Compare-Releases.ps1 writes (one `compare` run each) into one markdown table per version and rule set.

    python scripts/benchmark/summarize-compare.py <logs folder> [top N]

Per analyzer: the base and head times of every repeat (each already the fastest of its runs), as min-max, and the
median of the head/base ratio. Totals the same way. Used for docs/proposals/performance-audit-2026-10.md.
"""

import re
import statistics
import sys
from collections import defaultdict
from pathlib import Path

ROW = re.compile(r"^\| (?P<name>[^|]+?) \| (?P<base>[\d,.]*)(?: ms)? \| (?P<head>[\d,.]*)(?: ms)? \| [^|]+ \| (?P<reports>[^|]*)\|$")


def number(text):
    return float(text.replace(",", "").strip("*")) if text.strip("*") else None


def last_table(path):
    rows = {}
    for line in path.read_text().splitlines():
        if line.startswith("| Analyzer |"):
            rows = {}
        line = line.replace("**", "")
        match = ROW.match(line)
        if match and match["name"] != "Analyzer":
            rows[match["name"].strip()] = (number(match["base"]), number(match["head"]), match["reports"].strip())
    return rows


def span(values):
    values = [v for v in values if v is not None]
    if not values:
        return ""
    low, high = min(values), max(values)
    return f"{low:,.1f}" if abs(high - low) < 0.05 else f"{low:,.1f}-{high:,.1f}"


def main():
    folder = Path(sys.argv[1])
    top = int(sys.argv[2]) if len(sys.argv) > 2 else 15
    groups = defaultdict(list)
    for log in sorted(folder.glob("*.txt")):
        key = re.sub(r"-\d+$", "", log.stem)
        if table := last_table(log):
            groups[key].append(table)

    for key, tables in groups.items():
        print(f"\n### {key} ({len(tables)} runs)\n")
        print("| Analyzer | Base (ms) | Head (ms) | Head / base (median) | Reports (base / head) |")
        print("|---|---:|---:|---:|---:|")
        names = set().union(*tables)
        def head_of(name):
            return statistics.median([t[name][1] or 0 for t in tables if name in t])
        ordered = sorted((n for n in names if n != "total"), key=lambda n: -max(head_of(n), statistics.median([t[n][0] or 0 for t in tables if n in t])))
        for name in ordered[:top] + ["total"]:
            bases = [t[name][0] for t in tables if name in t]
            heads = [t[name][1] for t in tables if name in t]
            ratios = [h / b for b, h in zip(bases, heads) if b and h]
            ratio = f"{statistics.median(ratios):.2f}x" if ratios else "new" if not any(bases) else ""
            reports = next((t[name][2] for t in tables if name in t and t[name][2]), "")
            label = f"**{name}**" if name == "total" else name
            print(f"| {label} | {span(bases)} | {span(heads)} | {ratio} | {reports} |")


if __name__ == "__main__":
    main()
