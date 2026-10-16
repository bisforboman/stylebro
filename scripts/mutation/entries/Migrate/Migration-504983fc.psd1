# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'braces.Split('':'')[0].Trim() is not ("true" or "")'
    Replace = 'braces.Split('':'')[0].Trim() is ("true" or "")'
    Tests = 'MigrationTests.Migrate_ABraceSettingWeakerThanSA1503'
}
