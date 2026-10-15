# The empty blocks' statements line is written last on every run
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'kept.RemoveRange(keptAt, 2);'
    Replace = ''
    Tests = 'MigrationTests.Init_RunAgain_KeepsTheMemberOrderDecision'
}
