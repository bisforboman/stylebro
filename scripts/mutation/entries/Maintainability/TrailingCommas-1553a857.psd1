# BRO1401 (trailing commas): stylebro_trailing_comma = omit
@{
    File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'
    Find = 'while (start > 0 && text[start - 1] is'
    Replace = 'while (false && text[start - 1] is'
    Tests = 'TrailingCommaTests'
}
