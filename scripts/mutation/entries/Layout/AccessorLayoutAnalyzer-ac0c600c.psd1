# IDE0027 is off unless configured
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayoutAnalyzer.cs'
    Find = 'c.CancellationToken, enabledByDefault: false)'
    Replace = 'c.CancellationToken, enabledByDefault: true)'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NextToALongerBlock_BecomesABlock'
}
