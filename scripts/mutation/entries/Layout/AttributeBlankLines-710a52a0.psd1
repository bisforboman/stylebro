# BRO1525 (no blank line after attributes)
@{
    File = 'src/StyleBro.Analyzers/Layout/AttributeBlankLines.cs'
    Find = 'if (!close.TrailingTrivia.Concat('
    Replace = 'if (false && !close.TrailingTrivia.Concat('
    Tests = 'AttributeBlankLinesTests'
}
