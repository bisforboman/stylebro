"""Estimates what merged fix rounds (docs/proposals/format-speed.md, option (b) / phase 3) would save on a repository,
from FormatProbe logs (Measure-FixLoop.ps1) and the id groups of tests/StyleBro.Tests/FixOrderTests.cs (performance
audit 2026-10).

    python scripts/benchmark/fixloop-rounds.py <run1.probe.tsv> [<run2.probe.tsv> ...]

Model: ids that appear together in a FixOrderTests case interact (one's fix changes what another reports); every other
pair is assumed not to. A round is a set of fixed ids with no interacting pair (greedy colouring of the interaction
graph, most-connected ids first); a merged round runs ONE analysis pass with the analyzers of its ids, then the Fix All
of each of its ids. Today's loop runs, per id, the analyzers supporting that id over every project with findings, then
its Fix All. The Fix All time is the same in both. A round's analysis is costed two ways: high = its ids' analyses
summed, capped at the run's first analysis (all analyzers, every project compiled from scratch); low = its most
expensive id's analysis (the others' analyzers come almost free once the compilation is bound). Renames (BRO13xx)
interact with anything (format-speed.md): they get their own last round, whatever FixOrderTests says.
"""

import itertools
import os
import re
import statistics
import sys

sys.path.insert(0, os.path.dirname(__file__))
summarize = __import__("summarize-fixloop")


def interaction_groups(path):
    text = open(path, encoding="utf-8").read()
    groups = []
    for chunk in re.split(r"\[(?:Fact|Theory)[^\]]*\]", text)[1:]:
        # The ids are the call's trailing string arguments, after the last raw string (the source) closes.
        tail = chunk.rsplit('"""', 1)[-1]
        ids = sorted(set(re.findall(r'"(BRO\d{4})"', tail)))
        if len(ids) > 1:
            groups.append(ids)
    return groups


def rounds(ids, edges):
    neighbours = {i: {j for j in ids if j != i and frozenset((i, j)) in edges} for i in ids}
    colour = {}
    for i in sorted(ids, key=lambda x: (-len(neighbours[x]), x)):
        used = {colour[j] for j in neighbours[i] if j in colour}
        colour[i] = next(c for c in itertools.count() if c not in used)
    return colour


def main():
    root = os.path.join(os.path.dirname(__file__), "..", "..")
    groups = interaction_groups(os.path.join(root, "tests", "StyleBro.Tests", "FixOrderTests.cs"))
    edges = {frozenset(p) for g in groups for p in itertools.combinations(g, 2)}
    print(f"FixOrderTests: {len(groups)} cases with 2+ ids, {len(edges)} interacting pairs, "
          f"{len({i for g in groups for i in g})} ids involved, largest case {max(len(g) for g in groups)} ids")
    results = []
    for path in sys.argv[1:]:
        first, first_wall, steps = summarize.parse(path)
        fixed = [s for s in steps if not s["id"].startswith("(skipped")]
        ids = {s["id"] for s in fixed}
        colour = rounds({i for i in ids if not i.startswith("BRO13")}, edges)
        last = max(colour.values()) + 1 if colour else 0
        colour.update({i: last for i in ids if i.startswith("BRO13")})
        count = max(colour.values()) + 1 if colour else 0
        analysis = sum(s["analysis"] for s in steps) / 1000
        fix = sum(s["fix"] for s in steps) / 1000
        high = sum(min(first, sum(s["analysis"] / 1000 for s in fixed if colour[s["id"]] == c)) for c in range(count))
        low = sum(max(s["analysis"] / 1000 for s in fixed if colour[s["id"]] == c) for c in range(count))
        interacting = sorted(i for i in ids if any(frozenset((i, j)) in edges for j in ids if j != i))
        results.append((analysis, fix, count, low, high))
        print(f"\n{os.path.basename(path)}: {len(ids)} ids fixed, {len(interacting)} of them interact with another fixed id")
        print(f"  today: analysis before each id {analysis:.1f} s + Fix All {fix:.1f} s = {analysis + fix:.1f} s"
              f" (first analysis {first:.1f} s)")
        print(f"  merged: {count} rounds, analysis {low:.1f}-{high:.1f} s + Fix All {fix:.1f} s")
        print(f"  saving: {analysis - high:.1f}-{analysis - low:.1f} s of the fix phase"
              f" ({(analysis - high) / (analysis + fix) * 100:.0f}-{(analysis - low) / (analysis + fix) * 100:.0f} %)")
        for c in range(count):
            print(f"    round {c + 1}: {' '.join(sorted(i for i in ids if colour[i] == c))}")
    if len(results) > 1:
        low = [a - h for a, _, _, _, h in results]
        high = [a - lo for a, _, _, lo, _ in results]
        print(f"\nsaving over the runs: {statistics.median(low):.1f}-{statistics.median(high):.1f} s (medians of the low and high estimates)")


if __name__ == "__main__":
    main()
