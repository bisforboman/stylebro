# BRO1138 (unneeded $, @, raw string)
@{
    File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'
    Find = '? Plain(content, string.Empty) ?? raw : raw;'
    Replace = '? raw : raw;'
    Tests = 'StringPrefixTests'
}
