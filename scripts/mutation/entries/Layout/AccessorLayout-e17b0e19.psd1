# The blank line between the new block and its neighbour
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = '|| (BecomesBlock(accessors[i - 1]) && BecomesBlock(accessors[i])))'
    Replace = ')'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NextToALongerBlock_BecomesABlock'
}
