# Trial 9: lower-case unsigned suffixes with BRO1135 off
@{
    File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'
    Find = 'if (suffix.Contains(''U'') && !Severities.IsOn('
    Replace = 'if (false && !Severities.IsOn('
    Tests = 'LiteralSuffixTests'
}
