# BRO1101 stylebro_allow_semicolon_after_closing_brace: the ';' after a type's '}' stays (Sonar's S1116).
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStatementAnalyzer.cs'
    Find = '&& bool.TryParse(allow.Trim(), out var allowed) && allowed))'
    Replace = '&& bool.TryParse(allow.Trim(), out var allowed) && false))'
    Tests = 'EmptyStatementTests'
}
