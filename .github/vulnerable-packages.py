#!/usr/bin/env python3
"""Lists the vulnerable NuGet packages, direct or transitive, of everything StyleBro builds or ships.

vulnerable-packages.yml runs it weekly (docs/decisions/2026-11-06-vulnerable-packages-fail-every-build.md). Each target
is restored with the audit's errors turned off (Directory.Build.props makes NU1901-NU1904 errors, which would stop the
restore before anything is listed), then `dotnet list package --vulnerable --include-transitive --format json` reads
the result. Writes a Markdown report to the file given as the first argument (and to $GITHUB_STEP_SUMMARY when set) and
exits 1 when a package is vulnerable or a target could not be listed.

  python3 .github/vulnerable-packages.py report.md [--json listing.json ...]

`--json` parses saved listings instead of running dotnet (for testing the parsing).
"""
import json
import os
import subprocess
import sys

# Every solution and project StyleBro builds or ships, in CI or by its scripts. samples/StyleCopMigration and .trials/
# are left out on purpose: they keep StyleBro's props out and are not built.
TARGETS = [
    "StyleBro.slnx",
    "samples/MultiTarget/MultiTarget.csproj",
    "scripts/benchmark/Benchmark.csproj",
    "scripts/benchmark/FormatProbe/FormatProbe.csproj",
    "scripts/stylecop-survey/Inventory/Inventory.csproj",
    "scripts/fixtures/packages/Fixture.Trimmer/Fixture.Trimmer.csproj",
]


def run(args):
    return subprocess.run(args, capture_output=True, text=True)


def listing(target):
    """Returns (parsed listing, None) or (None, the failure's text)."""
    restore = run(["dotnet", "restore", target, "-nologo", "-p:WarningsAsErrors="])
    if restore.returncode != 0:
        return None, f"`dotnet restore {target}` failed:\n\n```\n{restore.stdout[-3000:]}{restore.stderr[-2000:]}\n```"
    result = run(["dotnet", "list", target, "package", "--vulnerable", "--include-transitive", "--format", "json",
                  "--no-restore"])
    try:
        data = json.loads(result.stdout)
    except json.JSONDecodeError:
        return None, f"`dotnet list {target} package` gave no JSON:\n\n```\n{result.stdout[-3000:]}{result.stderr[-2000:]}\n```"
    return data, None


def findings(data, root):
    """Yields (package, version, severity, advisory url, project, framework) from one listing."""
    for project in data.get("projects", []):
        path = os.path.relpath(project["path"], root).replace(os.sep, "/")
        for framework in project.get("frameworks", []):
            for kind in ("topLevelPackages", "transitivePackages"):
                for package in framework.get(kind, []):
                    for vulnerability in package.get("vulnerabilities", []):
                        yield (package["id"], package["resolvedVersion"], vulnerability["severity"],
                               vulnerability["advisoryurl"], path, framework["framework"])


def problems(data, target):
    return [f"`{target}`: {p['text']}" for p in data.get("problems", []) if p.get("level") == "error"]


def main():
    out = sys.argv[1]
    root = os.getcwd()
    rows = {}
    errors = []
    if len(sys.argv) > 2 and sys.argv[2] == "--json":
        listings = [(path, (json.load(open(path)), None)) for path in sys.argv[3:]]
    else:
        listings = [(target, listing(target)) for target in TARGETS]
    for target, (data, error) in listings:
        if error:
            errors.append(error)
            continue
        errors.extend(problems(data, target))
        for package, version, severity, url, project, framework in findings(data, root):
            rows.setdefault((package, version, severity, url), set()).add(f"`{project}` ({framework})")

    lines = []
    if rows:
        lines += ["Vulnerable packages (`dotnet list package --vulnerable --include-transitive`):", "",
                  "| Package | Version | Severity | Advisory | Projects |", "|---|---|---|---|---|"]
        for (package, version, severity, url), projects in sorted(rows.items()):
            lines.append(f"| {package} | {version} | {severity} | {url} | {', '.join(sorted(projects))} |")
        lines += ["", "Fix each with a bump or a pin in Directory.Packages.props that names the advisory, never with a "
                  "suppression (docs/decisions/2026-11-06-vulnerable-packages-fail-every-build.md)."]
    if errors:
        lines += ["", "Could not list:", ""] + [f"- {e}" for e in errors]
    if not lines:
        lines = [f"No vulnerable packages in {len(listings)} solutions and projects."]
    text = "\n".join(lines).strip() + "\n"
    with open(out, "w") as f:
        f.write(text)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a") as f:
            f.write(text)
    print(text)
    return 1 if rows or errors else 0


if __name__ == "__main__":
    sys.exit(main())
