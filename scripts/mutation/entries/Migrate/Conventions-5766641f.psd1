# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& StyleBro.Analyzers.Documentation.DocumentationComments.IsMisplaced(documentation.ParentTrivia)'
    Replace = '&& !StyleBro.Analyzers.Documentation.DocumentationComments.IsMisplaced(documentation.ParentTrivia)'
    Tests = 'MigrationTests.Conventions_SkipMisplacedSummaries'
}
