# BRO1108: with same_line the items keep the first line-starting item's indentation.
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = '        if (!sameLine && lineStarts is not null'
    Replace = '        if (lineStarts is not null'
    Tests = 'StyleBro.Tests.ParameterLayoutTests'
}
