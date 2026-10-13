# Trial 18: every folder CSharpier formats gets its section (synth1)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (!result.Any(r => (scope + "/").StartsWith(r.Folder + "/", StringComparison.OrdinalIgnoreCase)))'
    Replace = 'if (result.Count == 0)'
    Tests = 'MigrationTests.CSharpierScopes_FindsEveryFolder'
}
