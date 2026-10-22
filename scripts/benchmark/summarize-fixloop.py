"""Summarizes FormatProbe logs (Measure-FixLoop.ps1) per diagnostic id: the analysis dotnet format runs before the id's
Fix All, the Fix All itself, the diagnostics fixed and the documents and files changed (performance audit 2026-10).

    python scripts/benchmark/summarize-fixloop.py <run1.probe.tsv> [<run2.probe.tsv> ...] [--markdown] [--top N]

With several runs (the same repository), every number is the median over the runs, with the range of the time columns.
"""

import statistics
import sys


def parse(path):
    """One run: (first analysis seconds, first analysis wall, [steps]); a step is one id: analysis ms, fix ms, ..."""
    first_ms, first_start, fixing_at = 0.0, None, None
    steps, pending, pending_ids = [], 0.0, []
    for line in open(path):
        parts = line.rstrip("\n").split("\t")
        stamp, kind = float(parts[0]), parts[1]
        if kind == "phase":
            fixing_at = stamp
        elif kind == "analyze":
            ms = float(parts[3])
            if fixing_at is None:
                first_ms += ms
                first_start = stamp - ms / 1000 if first_start is None else first_start
            else:
                pending += ms
                pending_ids = parts[5].split(",")
        elif kind == "fix":
            diag_id, ms, diagnostics, documents, files = parts[2], float(parts[3]), int(parts[4]), int(parts[5]), int(parts[6])
            if steps and steps[-1]["id"] == diag_id and pending == 0:
                # The same id's fixer from another project's analyzer reference (dotnet format doesn't deduplicate).
                step = steps[-1]
                step["fix"] += ms
                step["calls"] += 1
                step["documents"] += documents
                step["files"] += files
                continue
            if pending_ids and diag_id not in pending_ids and pending > 0:
                # An analysis that found nothing: dotnet format skipped that id.
                steps.append({"id": "(skipped: " + ",".join(pending_ids) + ")", "analysis": pending, "fix": 0.0,
                              "diagnostics": 0, "documents": 0, "files": 0, "calls": 0})
                pending = 0.0
            steps.append({"id": diag_id, "analysis": pending, "fix": ms, "diagnostics": diagnostics,
                          "documents": documents, "files": files, "calls": 1})
            pending, pending_ids = 0.0, []
    if pending > 0:
        steps.append({"id": "(skipped: " + ",".join(pending_ids) + ")", "analysis": pending, "fix": 0.0,
                      "diagnostics": 0, "documents": 0, "files": 0, "calls": 0})
    first_wall = (fixing_at - first_start) if fixing_at is not None and first_start is not None else 0.0
    return first_ms / 1000, first_wall, steps


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    markdown = "--markdown" in sys.argv
    top = int(sys.argv[sys.argv.index("--top") + 1]) if "--top" in sys.argv else None
    if top is not None:
        args.remove(str(top))
    runs = [parse(p) for p in args]
    by_id = {}
    for _, _, steps in runs:
        for step in steps:
            if step["id"].startswith("(skipped"):
                by_id.setdefault("(skipped ids)", []).append(step)
                continue
            by_id.setdefault(step["id"], []).append(step)

    def med(values):
        return statistics.median(values) if values else 0.0

    rows = []
    for diag_id, steps in by_id.items():
        if diag_id == "(skipped ids)":
            continue
        analysis = [s["analysis"] / 1000 for s in steps]
        fix = [s["fix"] / 1000 for s in steps]
        rows.append({
            "id": diag_id,
            "analysis": med(analysis), "analysis_range": (min(analysis), max(analysis)),
            "fix": med(fix), "fix_range": (min(fix), max(fix)),
            "total": med([a + f for a, f in zip(analysis, fix)]),
            "diagnostics": med([s["diagnostics"] for s in steps]),
            "files": med([s["files"] for s in steps]),
            "documents": med([s["documents"] for s in steps]),
            "calls": med([s["calls"] for s in steps]),
            "runs": len(steps),
        })
    rows.sort(key=lambda r: -r["total"])

    totals_analysis = [sum(s["analysis"] for s in steps) / 1000 for _, _, steps in runs]
    totals_fix = [sum(s["fix"] for s in steps) / 1000 for _, _, steps in runs]
    skipped = [sum(s["analysis"] for s in steps if s["id"].startswith("(skipped")) / 1000 for _, _, steps in runs]
    ids_fixed = [len([s for s in steps if not s["id"].startswith("(skipped")]) for _, _, steps in runs]
    print(f"runs: {len(runs)}")
    print(f"first analysis (sum of per-project calls): {', '.join(f'{r[0]:.1f}' for r in runs)} s")
    print(f"ids fixed: {', '.join(map(str, ids_fixed))}")
    print(f"fix phase, analysis before each id: {', '.join(f'{v:.1f}' for v in totals_analysis)} s "
          f"(of it ids that found nothing: {', '.join(f'{v:.1f}' for v in skipped)} s)")
    print(f"fix phase, Fix All: {', '.join(f'{v:.1f}' for v in totals_fix)} s")

    def rng(r):
        return f"{r[0]:.1f}-{r[1]:.1f}" if len(runs) > 1 else f"{r[0]:.1f}"

    shown = rows[:top] if top else rows
    if markdown:
        print("\n| Id | Analysis before it (s) | Fix All (s) | Diagnostics | Files changed | Fix All per file (ms) |")
        print("|---|---:|---:|---:|---:|---:|")
        for r in shown:
            per_file = r["fix"] * 1000 / r["files"] if r["files"] else float("nan")
            print(f"| {r['id']} | {rng(r['analysis_range'])} | {rng(r['fix_range'])} | {r['diagnostics']:.0f} | "
                  f"{r['files']:.0f} | {per_file:.0f} |")
    else:
        print(f"\n{'id':8} {'analysis s':>14} {'fix s':>14} {'diag':>6} {'files':>6} {'docs':>6} {'calls':>5} {'ms/file':>8}")
        for r in shown:
            per_file = r["fix"] * 1000 / r["files"] if r["files"] else float("nan")
            print(f"{r['id']:8} {rng(r['analysis_range']):>14} {rng(r['fix_range']):>14} {r['diagnostics']:6.0f} "
                  f"{r['files']:6.0f} {r['documents']:6.0f} {r['calls']:5.0f} {per_file:8.0f}")


if __name__ == "__main__":
    main()
