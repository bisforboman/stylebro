# Trial 22: NU1004 says how to update the lock files (d1b)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': error.Groups["code"].Value == "NU1004"'
    Replace = ': error.Groups["code"].Value == "NU1004x"'
    Tests = 'MigrationTests.Restore_SaysHowToUpdateLockFiles'
}
