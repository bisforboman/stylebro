# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '(json, linked) = (null, []);'
    Replace = ''
    Tests = 'MigrationTests.StyleSharp_IsReadLikeStyleCop_'
}
