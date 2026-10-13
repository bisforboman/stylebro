# Trial 18: with CSharpier its own rules are decided before the repository's key
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (csharpier.Lines is not null && Conventions.IsCSharpiers(convention))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_WritesTheSameBlockTwice_WithCSharpierAndAKeyTheCodeContradicts'
}
