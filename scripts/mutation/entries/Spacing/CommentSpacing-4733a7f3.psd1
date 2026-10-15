# BRO1002 keeps tabs after // (trial 23)
@{
    File = 'src/StyleBro.Analyzers/Spacing/CommentSpacing.cs'
    Find = 'return rest.TrimStart('' '', ''\t'').Length == 0 ? "//" : "// " + rest;'
    Replace = 'return rest.TrimStart('' '', ''\t'').Length == 0 ? "//" : "// " + rest.TrimStart(''\t'');'
    Tests = 'CommentSpacingTests.TabsAfterSlashes_AreKept'
}
