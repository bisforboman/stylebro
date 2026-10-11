# BRO1525 (no blank line after attributes)
@{
    File = 'src/StyleBro.Analyzers/Layout/AttributeBlankLines.cs'
    Find = 'if (list.Parent is null or CompilationUnitSyntax || '
    Replace = 'if ('
    Tests = 'AttributeBlankLinesTests'
}
