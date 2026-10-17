# BRO1524 mirrors when BRO1520 is off
@{
    File = 'src/StyleBro.Analyzers/Layout/ConditionalLayoutAnalyzer.cs'
    Find = 'var mirror = !Severities.IsOn('
    Replace = 'var mirror = Severities.IsOn('
    Tests = 'ConditionalLayoutTests'
}
