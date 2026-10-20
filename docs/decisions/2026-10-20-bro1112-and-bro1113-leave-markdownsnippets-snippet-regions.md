# BRO1112 and BRO1113 leave MarkdownSnippets snippet regions (2026-10-20)

## Question

Trial 37, shouldly: BRO1113 (regions inside a block, StyleCop SA1123) removed `#region ShouldBeObjects` and
`#region ShouldThrowAsync` in src/DocumentationExamples. They are MarkdownSnippets snippets (the docs say
`snippet: ShouldBeObjects`; MarkdownSnippets.MsBuild runs in the build), so the build failed with "Missing snippet" and
Shouldly.Tests didn't compile. format and check didn't notice: once the regions are gone, nothing is reported. How should
StyleBro handle such regions?

## Choices

1. **The analyzer skips snippet regions** (owner's pick): BRO1112/BRO1113 don't report a region whose name a markdown file
   of the repository names as a snippet (`snippet: Name`, MarkdownSnippets' syntax). The package targets pass the names, so
   the build and format agree. Other regions are still removed.
2. init/migrate turn the rules off there: with MarkdownSnippets (mdsnippets.json or the package), BRO1112/BRO1113 = none
   for the .cs files holding snippet regions, with a report line. Users without init still lose the snippets.
3. Off repository-wide with MarkdownSnippets.
4. Document only (turn BRO1113 off for snippet code, like Polly's Snippets project with SA1123 in NoWarn).

## Decision

The analyzer skips them. The package targets read the names (target `_StyleBroFindSnippets`, compiler-visible property
`StyleBroSnippets`) when an `mdsnippets.json` is at or above the project, from the repository's markdown files (else the
config's folder), bin/obj/node_modules left out; `snippet: Name` and the in-place `<!-- snippet: Name -->` both count.
