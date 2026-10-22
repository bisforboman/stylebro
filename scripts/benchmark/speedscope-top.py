"""Lists the frames with the most inclusive time in a `dotnet-trace --format speedscope` file.

    python scripts/benchmark/speedscope-top.py <trace.speedscope.json> [filter, default StyleBro] [top N, default 40]

Sums the sample weights (or, for dotnet-trace's evented profiles, the time on the stack) per frame name over every thread, counting a frame once per sample however deep it recurses,
and prints the frames whose name contains the filter ("" for all), largest first, with their share of all samples on
threads that ran the filter at all. A sampling profile lands on safepoints, so read it for where time goes, not exact
costs (scripts/benchmark/README.md). Used for docs/proposals/performance-audit-2026-10.md.
"""

import json
import sys
from collections import defaultdict


def main():
    path = sys.argv[1]
    needle = sys.argv[2] if len(sys.argv) > 2 else "StyleBro"
    top = int(sys.argv[3]) if len(sys.argv) > 3 else 40
    data = json.load(open(path))
    names = [f["name"] for f in data["shared"]["frames"]]
    inclusive = defaultdict(float)
    total = 0.0
    for profile in data["profiles"]:
        if profile.get("type") == "evented":
            # dotnet-trace writes open/close events per frame: a frame's time is the time it was on the stack, counted
            # once while it recurses.
            events = profile["events"]
            if not any(needle in names[e["frame"]] for e in events):
                continue
            total += profile["endValue"] - profile["startValue"]
            depth, opened = defaultdict(int), {}
            for event in events:
                name = names[event["frame"]]
                if needle not in name:
                    continue
                if event["type"] == "O":
                    if depth[name] == 0:
                        opened[name] = event["at"]
                    depth[name] += 1
                else:
                    depth[name] -= 1
                    if depth[name] == 0:
                        inclusive[name] += event["at"] - opened[name]
            continue
        if profile.get("type") != "sampled":
            continue
        samples, weights = profile["samples"], profile["weights"]
        if not any(needle in names[i] for stack in samples for i in stack):
            continue
        for stack, weight in zip(samples, weights):
            total += weight
            for name in {names[i] for i in stack}:
                if needle in name:
                    inclusive[name] += weight
    print(f"{total / 1000:,.1f} s of samples on threads that ran '{needle}'")
    for name, weight in sorted(inclusive.items(), key=lambda p: -p[1])[:top]:
        print(f"{weight / 1000:9.1f} s {100 * weight / total:5.1f} %  {name[:160]}")


if __name__ == "__main__":
    main()
