# Trial 23: VB names count as code the run doesn't load, also where a C# project references the VB project
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'f + " (Visual Basic, left alone)"'
    Replace = 'f'
    Tests = 'MigrationTests.Format_LeavesVisualBasicAlone'
}
