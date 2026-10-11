# BRO1138 (unneeded $, @, raw string)
@{
    File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'
    Find = 'if (content.Length == 0)'
    Replace = 'if (false)'
    Tests = 'StringPrefixTests'
}
