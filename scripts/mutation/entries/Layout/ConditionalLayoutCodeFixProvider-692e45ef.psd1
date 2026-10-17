# BRO1524 fix mirrors when BRO1520 is off
@{
    File = 'src/StyleBro.CodeFixes/Layout/ConditionalLayoutCodeFixProvider.cs'
    Find = 'var mirror = !Severities.IsOn('
    Replace = 'var mirror = Severities.IsOn('
    Tests = 'ConditionalLayoutTests'
}
