# BRO1303/BRO1306: a naming rule nothing enforces leaves StyleCop's default for non-private fields.
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'IsEnforced(severity, ide1006) ? casing : '' '''
    Replace = 'severity is "none" ? '' '' : casing'
    Tests = 'FieldNamingTests'
}
