# Format speed (2026-10-14): options that only say how to run don't change the state
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'list[i] is not ("--verify-no-changes" or "--no-restore" or CheckFirstOption or OnceOption)'
    Replace = 'list[i] is not ("--verify-no-changes" or OnceOption)'
    Tests = 'MigrationTests.Verify_KnowsTheFindingsTheLastCleanRunKept'
}
