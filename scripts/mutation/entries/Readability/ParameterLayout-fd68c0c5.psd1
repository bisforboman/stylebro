# BRO1108 keeps the first item when BRO1107 is off
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'IsSameLine(options) || !isOn(DiagnosticIds.SplitParametersStartOnNewLine);'
    Replace = 'IsSameLine(options);'
    Tests = 'ParameterLayoutTests.WithBro1107Off|FullyQualifiedName~FixOrderTests.ListsWithBro1107Off'
}
