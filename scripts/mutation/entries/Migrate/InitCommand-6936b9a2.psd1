# Trial 9, tool (2026-10-12): init's dry run on a migrated repository says what --write says
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'Console.Write(hasBlock ? null : block);'
    Replace = 'Console.Write(block);'
    Tests = 'MigrationTests.Init_DryRunOnAMigratedRepository'
}
