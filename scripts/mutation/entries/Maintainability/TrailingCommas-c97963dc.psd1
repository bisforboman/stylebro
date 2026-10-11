# BRO1401 (trailing commas): stylebro_trailing_comma = omit
@{
    File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'
    Find = '|| HasDirectiveBetween(node, l.OpenBrace, l.CloseBrace)'
    Replace = ''
    Tests = 'TrailingCommaTests'
}
