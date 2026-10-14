# IDE0005 where enforced
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        if (EnforcesUnusedUsings(root))'
    Replace = '        if (false)'
    Tests = 'FullyQualifiedName~MigrationTests.Format_FixesUnusedUsings'
}
