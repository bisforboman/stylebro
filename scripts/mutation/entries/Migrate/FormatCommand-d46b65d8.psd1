# trial 49: kept lines are matched by file, rule and message, not by line
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'IsFindingLine(line) && AsKept(line, kept) is not null'
    Replace = 'IsFindingLine(line) && kept.Any(k => line.Contains($"({k.Line},", StringComparison.Ordinal))'
    Tests = 'MigrationTests.MarkKept_AKeptFindingAnotherFixMoved_IsStillMarked'
}
