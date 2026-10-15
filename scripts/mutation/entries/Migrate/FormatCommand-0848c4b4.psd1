# Trial 22: protecting a file keeps its BOM
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;'
    Replace = 'var bom = false;'
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
