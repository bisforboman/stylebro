# Trial 28: Compile items picked inside a When
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '<(?<tag>ItemGroup|When)\b[^>]*\bCondition'
    Replace = '<(?<tag>ItemGroup)\b[^>]*\bCondition'
    Tests = 'MigrationTests.Format_RunsTheConfigurationsWhoseCompileItemsDiffer'
}
