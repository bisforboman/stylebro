# In a folder that turns every analyzer off, no fixes run either (optout spot; trial 23: the opting-out folder may be above).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (InitCommand.OptsOut(Migration.OwnSettings(Path.Combine(root, folder))))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Texts_SayWhatHappens_InOptOutFolders_AndWithCodeStyleInTheBuild'
}
