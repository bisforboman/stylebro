"""Runs `dotnet format ... -v diag` and splits its time into phases, including each diagnostic id's fix (performance audit
2026-10, docs/proposals/performance-audit-2026-10.md).

    python scripts/benchmark/time-format.py <log file> -- dotnet format analyzers X.sln --diagnostics ... -v diag
    python scripts/benchmark/time-format.py <log file>     (summarize a log written before)

Every output line is written to the log with the seconds since the start. dotnet format doesn't name the id it is
fixing; after "Fixing diagnostics..." it runs, per id with findings, the analyzers again over every project ("Running 1
analyzers on P." once per project) and then the id's Fix All. So a fix group starts where a project of the current group
comes again; its analysis is the span of its "Running" lines, its fix the time from its last "Running" line to the next
group (or the end of the fix phase). Run one id alone (--diagnostics BROxxxx) to name a slow group. Prints the phases
and every group.
"""

import subprocess
import sys
import time


def main():
    log_path = sys.argv[1]
    if "--" not in sys.argv:
        # A log written before: summarize it again.
        lines = [(float(l[:9]), l[10:].rstrip("\n")) for l in open(log_path)]
        total, returncode = lines[-1][0], "?"
    else:
        command = sys.argv[sys.argv.index("--") + 1:]
        start = time.monotonic()
        lines = []
        with open(log_path, "w") as log, subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1) as process:
            for line in process.stdout:
                stamp = time.monotonic() - start
                lines.append((stamp, line.rstrip("\n")))
                log.write(f"{stamp:9.2f} {line}")
        total, returncode = time.monotonic() - start, process.returncode

    fixing = next((t for t, l in lines if l.startswith("Fixing diagnostics")), None)
    loaded = next((t for t, l in lines if l.startswith("Running formatters")), None)
    print(f"exit {returncode}, {total:.1f} s in all")
    if loaded is not None:
        print(f"  until analysis starts (start, workspace load, files): {loaded:.1f} s")
    if fixing is None:
        return
    print(f"  first analysis: {fixing - loaded:.1f} s")
    rest = [(t, l) for t, l in lines if t >= fixing]
    end = next((t for t, l in rest if l.startswith("Analysis complete")), total)
    groups = []
    seen = set()
    for t, l in rest:
        if not l.startswith("Running ") or " on " not in l:
            continue
        project = l.split(" on ", 1)[1]
        if not groups or project in seen:
            groups.append([t, t])
            seen = set()
        seen.add(project)
        groups[-1][1] = t
    print(f"  fix phase: {end - fixing:.1f} s, {len(groups)} ids with findings")
    for i, (first, last) in enumerate(groups):
        following = groups[i + 1][0] if i + 1 < len(groups) else end
        print(f"    group {i + 1}: analysis {last - first:6.1f} s, fix {following - last:6.1f} s")
    print(f"  after the fix phase (writing files): {total - end:.1f} s")


if __name__ == "__main__":
    main()
