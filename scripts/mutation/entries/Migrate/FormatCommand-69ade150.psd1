# A WinForms form's Designer.cs.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| (File.Exists(stem + ".cs") && File.ReadAllText(file).Contains("InitializeComponent", StringComparison.Ordinal));'
    Replace = ';'
    Tests = 'StyleBro.Tests.MigrationTests.GeneratedFileNote_LeavesDesignersOut_AndMentionsOtherProjects'
}
