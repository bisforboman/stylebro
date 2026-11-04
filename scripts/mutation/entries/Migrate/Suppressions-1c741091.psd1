# Trial 67: a [SuppressMessage] of an IDE id is recognised.
@{
    File = 'src/StyleBro.Migrate/Suppressions.cs'
    Find = '|S\d{3,4}|IDE\d{4})\b'
    Replace = '|S\d{3,4})\b'
    Tests = 'Init_CarriesIde0040SuppressionsOver'
}
