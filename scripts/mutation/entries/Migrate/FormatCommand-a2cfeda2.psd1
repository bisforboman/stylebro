# Usings the fixes left unused are removed in the same run (ide5: 3 runs).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (code == 0 && !args.Contains("--diagnostics") && !args.Contains("--all") && !MissingCompileRoots.ContainsKey(root) && EnforcesUnusedUsings(root)'
    Replace = 'if (false && !args.Contains("--diagnostics") && !args.Contains("--all") && !MissingCompileRoots.ContainsKey(root) && EnforcesUnusedUsings(root)'
    Tests = 'StyleBro.Tests.MigrationTests.Format_RemovesUsingsItsFixesLeftUnused_InTheSameRun'
}
