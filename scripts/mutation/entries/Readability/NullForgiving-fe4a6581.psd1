# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = '|| context.AnnotationsEnabled() != project.AnnotationsEnabled()'
    Replace = ''
    Tests = 'NullForgivingTests'
}
