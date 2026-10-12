# Trial 9, tool (2026-10-12): init's dry run on a migrated repository says what --write says
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'Console.Write(target.HasBlock ? null : target.Block);'
    Replace = 'Console.Write(target.Block);'
    Tests = 'MigrationTests.Init_DryRunOnAMigratedRepository'
}
