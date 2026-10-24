# Trial 51: a run's solution filter lists the projects its projects reference
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'FilterProjects(selected, projects.ToDictionary(p => p.Key, p => p.Value.FullPath), ReferencesOf)'
    Replace = 'selected'
    Tests = 'MigrationTests.Format_ATestProjectWhoseLibrariesReferenceEachOther_Loads'
}
