# Format host: each fixer type is called once per id
@{
    File = 'src/StyleBro.FormatHost/FixOrder.cs'
    Find = '.GroupBy(f => f.GetType())'
    Replace = '.GroupBy(f => f)'
    Tests = 'FormatHostFixTests.EachFixerType_IsCalledOncePerId'
}
