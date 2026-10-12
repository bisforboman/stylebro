# format (trial 11, Autofac): only projects that reference a forced project ask for all frameworks
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& p.Value.References.Any(keep.Contains)'
    Replace = ''
    Tests = 'MigrationTests.Format_AProjectWithoutTheRunsFramework'
}
