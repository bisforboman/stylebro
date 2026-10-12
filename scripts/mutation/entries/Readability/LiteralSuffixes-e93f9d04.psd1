# BRO1135 lone_l: only a suffix that is just 'l' (Sonar's S818), not '1ul' or '1lu'.
@{
    File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'
    Find = 'return suffix == "l" ?'
    Replace = 'return suffix.IndexOf(''l'') >= 0 ?'
    Tests = 'LiteralSuffixCaseTests'
}
