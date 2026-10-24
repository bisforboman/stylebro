# Trial 51: FilterProjects adds the referenced projects, transitively
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'listed.Add(key);'
    Replace = ''
    Tests = 'MigrationTests.Format_ATestProjectWhoseLibrariesReferenceEachOther_Loads'
}
