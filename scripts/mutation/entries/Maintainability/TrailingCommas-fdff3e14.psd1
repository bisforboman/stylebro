# BRO1401 (trailing commas): stylebro_trailing_comma = omit
@{
    File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'
    Find = '|| Omits(getOptions()) != hasComma'
    Replace = '|| !hasComma'
    Tests = 'TrailingCommaTests'
}
