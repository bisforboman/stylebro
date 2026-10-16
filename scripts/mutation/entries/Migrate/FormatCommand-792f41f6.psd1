# Trial 25: Visual Studio writes Settings.Designer.cs
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| File.Exists(stem + ".settings")'
    Replace = ''
    Tests = 'MigrationTests.GeneratedFileNote_LeavesOutDesignersAndToolOutput_NoBuildWrites'
}
