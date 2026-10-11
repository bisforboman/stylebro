# Rule docs as a website (2026-10-09)

## Question

The docs are Markdown files read on GitHub, and every diagnostic's help link opens a GitHub file view. Publish them as
a website? With what, which pages, and where do the help links go?

## Choices

1. Generator: MkDocs Material (Markdown as it is, search, dark mode); GitHub's built-in Jekyll (no build setup,
   plain look, no real search); DocFX (.NET's own, aimed at API docs we don't need).
2. Pages: user docs only (rules, getting started, migrating, configuration, baseline, CI, StyleCop comparison); or
   everything in `docs/`, the decision log, backlog and proposals included.
3. Help links: to the site's rule pages; or keep the GitHub file views.

## Decision

MkDocs Material; user docs only (`decisions.md`, `backlog.md`, `beyond-stylecop.md` and `proposals/` stay on GitHub;
site pages link to them with absolute GitHub URLs); help links point to the site
(`https://bisforboman.github.io/stylebro/rules/BROxxxx/`). Built with `--strict` on every pull request, deployed to
GitHub Pages from `main`.
