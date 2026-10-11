# BRO1514-BRO1516 (braces)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'preference == BracePreference.WhenMultiline ? IsMultiLineForSdk(child, text) : IsMultiLine(child, text)'
    Replace = 'IsMultiLine(child, text)'
    Tests = 'BracesTests'
}
