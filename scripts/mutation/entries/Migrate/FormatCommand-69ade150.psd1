# A WinForms form's Designer.cs.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| (File.Exists(stem + ".cs") && text.Contains("InitializeComponent", StringComparison.Ordinal))'
    Replace = '|| false'
    Tests = 'StyleBro.Tests.MigrationTests.GeneratedFileNote_LeavesDesignersOut_AndMentionsOtherProjects'
}
