# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'folders = Migration.Topmost([.. folders, .. projectless]);'
    Replace = ''
    Tests = 'MigrationTests.Read_CodeWithoutAProjectOfItsOwn_'
}
