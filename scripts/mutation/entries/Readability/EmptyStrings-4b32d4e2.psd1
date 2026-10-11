# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'
    Find = '&& !IsInNameof(access)'
    Replace = ''
    Tests = 'EmptyStringTests'
}
