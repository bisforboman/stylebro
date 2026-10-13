# Trial 14: init's own blocks aren't the repository's whitespace settings
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '@"# BEGIN stylebro-.*?# END stylebro-[^\n]*"'
    Replace = '@"(?!)"'
    Tests = 'MigrationTests.FileSettings_TheNearestEditorConfigThatSetsAKeyDecides'
}
