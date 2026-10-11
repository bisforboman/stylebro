# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (isSet?.Invoke(SortUsingsKey) == true || isSet?.Invoke("dotnet_separate_import_directive_groups") == true)'
    Replace = 'if (isSet?.Invoke(SortUsingsKey) == true)'
    Tests = 'MigrationTests.Conventions_DecideTheUsingOrder'
}
