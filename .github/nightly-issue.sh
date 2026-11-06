#!/usr/bin/env bash
# A failed nightly run (realworld.yml, mutation.yml, drift.yml, fixtures.yml): opens the issue titled $1, or comments on it while it is still
# open (one issue per failure streak, no duplicates). The body lists the failed jobs and links the run.
# Needs GH_TOKEN with issues: write and actions: read. With a second argument, a file (vulnerable-packages.yml's
# report), the body holds that file's text instead of the failed jobs, and actions: read isn't needed.
set -euo pipefail
title=$1
run="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"
if [ $# -ge 2 ]; then
  body=$(printf '%s on %s (`%s`): %s\n\n%s\n' "$GITHUB_WORKFLOW" "$(date -u +%F)" "${GITHUB_SHA:0:7}" "$run" "$(cat "$2")")
else
  failed=$(gh api "repos/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID/jobs" --paginate \
    --jq '.jobs[] | select(.conclusion == "failure") | "- [\(.name)](\(.html_url))"')
  body=$(printf '%s on %s (`%s`): %s\n\nFailed jobs:\n%s\n' "$GITHUB_WORKFLOW" "$(date -u +%F)" "${GITHUB_SHA:0:7}" "$run" "$failed")
fi
number=$(gh issue list --repo "$GITHUB_REPOSITORY" --state open --search "\"$title\" in:title" --json number,title \
  --jq "map(select(.title == \"$title\")) | .[0].number // empty")
if [ -n "$number" ]; then
  gh issue comment "$number" --repo "$GITHUB_REPOSITORY" --body "$body"
else
  gh issue create --repo "$GITHUB_REPOSITORY" --title "$title" --body "$body"
fi
