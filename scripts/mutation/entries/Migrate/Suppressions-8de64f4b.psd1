# Trial 67: IDE0040 is replaced by BRO1007 and BRO1404.
@{
    File = 'src/StyleBro.Migrate/Suppressions.cs'
    Find = '["IDE0040"] = ["BRO1007", "BRO1404"],'
    Replace = '["IDE0040"] = ["BRO1404"],'
    Tests = 'Init_CarriesIde0040SuppressionsOver'
}
