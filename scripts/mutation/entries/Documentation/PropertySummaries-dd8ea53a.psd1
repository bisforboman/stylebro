# First-run fixes (2026-10-06): BRO1601 conditional directives and doc generation, BRO1603 spaces before a closing
# tag, BRO1604/BRO1606 text that can't follow the standard words, BRO1134 a comment ending the header's line
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 'if (old.Length == 0 && (StartsWithAnyWord(rest, Conditions) || StartsWithAnyWord(rest, Verbs)))'
    Replace = 'if (false)'
    Tests = 'DocumentationTests'
}
