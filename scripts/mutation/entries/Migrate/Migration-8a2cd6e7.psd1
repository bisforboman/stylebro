# Issue #339: SX1412 is charset = utf-8
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var withMark = setup.IsOn("SA1412");'
    Replace = 'var withMark = true;'
    Tests = 'MigrationTests.NoByteOrderMark_IsCharsetUtf8'
}
