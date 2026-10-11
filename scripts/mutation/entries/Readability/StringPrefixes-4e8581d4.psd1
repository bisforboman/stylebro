# BRO1138 (unneeded $, @, raw string)
@{
    File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'
    Find = 'content.Length == 0 || content.Any('
    Replace = 'content.Any('
    Tests = 'StringPrefixTests'
}
