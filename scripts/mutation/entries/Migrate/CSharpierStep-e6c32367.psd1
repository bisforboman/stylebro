# Trial 24: --no-msbuild-check for the package's copy only
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = 'if (tool.Manifest is null)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.CSharpier_RunsTheRepositorysTool'
}
