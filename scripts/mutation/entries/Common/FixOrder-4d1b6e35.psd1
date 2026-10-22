# Format host: the ids are fixed in a fixed (ordinal) order
@{
    File = 'src/StyleBro.FormatHost/FixOrder.cs'
    Find = '.Order(StringComparer.Ordinal)'
    Replace = ''
    Tests = 'FormatHostFixTests.TheIds_AreFixedInOrdinalOrder'
}
