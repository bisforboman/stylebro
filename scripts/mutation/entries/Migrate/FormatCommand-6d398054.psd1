# Other projects' generated files get a note of their own (gencheck2).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'changedProjects.Contains(project) != otherProjects'
    Replace = 'changedProjects.Contains(project)'
    Tests = 'StyleBro.Tests.MigrationTests.GeneratedFileNote_LeavesDesignersOut_AndMentionsOtherProjects'
}
