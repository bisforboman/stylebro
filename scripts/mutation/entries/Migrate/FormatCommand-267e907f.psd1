# In a folder that turns every analyzer off, no fixes run either (optout spot).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var optOut = exclusions.Where(e => e.EndsWith(''/'') && InitCommand.OptsOut('
    Replace = 'var optOut = exclusions.Where(e => false && InitCommand.OptsOut('
    Tests = 'StyleBro.Tests.MigrationTests.Texts_SayWhatHappens_InOptOutFolders_AndWithCodeStyleInTheBuild'
}
