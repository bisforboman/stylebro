# stylecop.json is named as removable once StyleCop is gone (SSH.NET).
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (setup.Sources.Where(s => Path.GetFileName(s).Equals("stylecop.json", StringComparison.OrdinalIgnoreCase)).ToList() is { Count: > 0 } styleCopJson)'
    Replace = 'if (new List<string>() is { Count: > 0 } styleCopJson)'
    Tests = 'StyleBro.Tests.MigrationTests.Migrate_SaysStyleCopJsonCanGo_OnceStyleCopIsGone'
}
