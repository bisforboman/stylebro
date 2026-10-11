# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = '|| context.WarningsEnabled() != project.WarningsEnabled()'
    Replace = ''
    Tests = 'NullForgivingTests'
}
