# BRO1138 (unneeded $, @, raw string)
@{
    File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'
    Find = '.ConvertedType?.SpecialType is SpecialType.System_String or SpecialType.System_Object'
    Replace = '.ConvertedType is not null'
    Tests = 'StringPrefixTests'
}
