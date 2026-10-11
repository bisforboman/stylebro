# The migration applies only where StyleCop ran (2026-10-10)

## Question

A trial of 0.5.0-alpha.1 on LiteBus: StyleCop.Analyzers is referenced only by the `src/` projects (through
`src/Directory.Build.props`, one analyzer project removes it again), and `src/.editorconfig` turns every category but
documentation off. `stylebro-migrate --write` read no StyleCop severities at the root, so it wrote StyleCop's defaults
for the whole repository, and `stylebro-migrate format` changed 226 files: `this.` added and usings sorted in `tests`,
`samples` and `benchmarks`, which never ran StyleCop. Which part of the repository should the settings cover?

## Choices

1. Only where StyleCop ran: the folders of the projects that reference it, with the rules it had on there; the rest
   gets no rule changes, and the report says `init` can set it up later.
2. The whole repository, with StyleCop's settings from where it ran.
3. The whole repository, as before (StyleCop's defaults where no config says otherwise).

## Decision

1 (owner). When some projects run StyleCop and others don't, the block's section names the folders of the projects
that do (`[src/{A,B}/**.cs]`, the topmost folders without a project that doesn't), a sub-directory `.editorconfig` or
ruleset folder covering all of them is folded into the base settings, one covering some moves to a root section for
those, and one covering none is dropped. The private field style is judged in those folders too. When every project
or none references StyleCop, nothing changes: the settings cover the repository.
