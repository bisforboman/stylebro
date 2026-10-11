# BRO1138 (unneeded $, @, raw string)
@{
    File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'
    Find = 'c is ''\\'' or ''"'' or'
    Replace = 'c is'
    Tests = 'StringPrefixTests'
}
