# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (!EnforcesCodeStyleInBuild(root))'
    Replace = 'if (true)'
    Tests = 'MigrationTests'
}
