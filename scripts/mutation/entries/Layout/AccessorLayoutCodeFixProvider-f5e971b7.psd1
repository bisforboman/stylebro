# IDE0027 is off unless configured
@{
    File = 'src/StyleBro.CodeFixes/Layout/AccessorLayoutCodeFixProvider.cs'
    Find = 'cancellationToken, enabledByDefault: false)'
    Replace = 'cancellationToken, enabledByDefault: true)'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NextToALongerBlock_BecomesABlock'
}
