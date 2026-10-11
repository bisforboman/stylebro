# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = '|| MultiTargetSuppressor.GetFrameworks(options).Count > 1)'
    Replace = '|| MultiTargetSuppressor.GetFrameworks(options).Count > 2)'
    Tests = 'NullForgivingTests'
}
