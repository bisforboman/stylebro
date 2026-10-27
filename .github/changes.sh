#!/usr/bin/env bash
# What a pull request changes, for the `changes` job of each workflow. Writes to $GITHUB_OUTPUT:
#   relevant  false when every changed file is documentation: the real-world, mutation and parity jobs then report
#             their checks without doing the work. Any file not listed as documentation below counts as relevant.
#   perf      true when analyzer speed can change (the performance job).
#   fixtures  true when the fixture matrix can change (the tool, the analyzers, the fixtures, the build settings).
#   files     the changed files, one per line (mutation testing runs only the entries for these).
# Anything but a pull request (the nightly run on main, a manual run) changes everything.
set -euo pipefail
export LC_ALL=C.UTF-8 # grep -P
out=${GITHUB_OUTPUT:-/dev/stdout}
if [ "${GITHUB_EVENT_NAME:-}" != pull_request ]; then
  printf 'relevant=true\nperf=true\nfixtures=true\n' >> "$out"
  exit 0
fi

files=$(git diff --name-only "origin/$GITHUB_BASE_REF...HEAD")
# Docs, markdown outside src/tests/samples, the docs site's setup, the backlog generator and its items, New-Decision, the
# trial containers (scripts/trials, run by hand only), the changelog fold script (the build job checks it), and the
# StyleCop survey except what the parity check and StyleBro.Migrate (embedded CSVs) read.
docs='^(docs/|(?!(src|tests|samples)/).*\.md$|mkdocs\.yml$|\.github/docs-requirements\.txt$|\.github/workflows/docs\.yml$|scripts/New-Backlog\.py$|scripts/backlog/|scripts/New-Decision\.ps1$|scripts/trials/|scripts/changelog\.py$|scripts/stylecop-survey/(?!Compare-WithStyleCop\.ps1$|parity/|data/inventory-|data/mapping\.csv$))'
fixtures='^(src/|scripts/fixtures/|Directory\.|global\.json$|nuget\.config$|[^/]*\.slnx$|\.github/workflows/fixtures\.yml$|\.github/changes\.sh$)'
perf='^(src/|scripts/benchmark/|scripts/realworld/repos\.psd1$|Directory\.|global\.json$|[^/]*\.slnx$|\.github/workflows/ci\.yml$)'

code=$(grep -vP "$docs" <<< "$files" || true)
relevant=false; [ -n "$code" ] && relevant=true
speed=false; grep -qP "$perf" <<< "$code" && speed=true
fixture=false; grep -qP "$fixtures" <<< "$code" && fixture=true
{
  echo "relevant=$relevant"
  echo "perf=$speed"
  echo "fixtures=$fixture"
  echo 'files<<EOF_FILES'
  echo "$files"
  echo 'EOF_FILES'
} >> "$out"
echo "relevant=$relevant perf=$speed fixtures=$fixture"
echo "$files"
