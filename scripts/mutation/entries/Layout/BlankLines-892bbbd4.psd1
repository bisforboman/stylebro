# BRO1617-BRO1619 (documentation style) and BRO1504's exempt prefixes
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = 'if (exemptPrefixes.Any(p => commentText.StartsWith(p, StringComparison.Ordinal)))'
    Replace = 'if (false)'
    Tests = 'BlankLineAfterTests'
}
