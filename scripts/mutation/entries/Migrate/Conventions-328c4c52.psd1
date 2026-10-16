# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'JudgesGapAfter(brace, next, text, AfterBlockOn, gapIsReplaced: true)'
    Replace = 'JudgesGapAfter(brace, next, text, AfterBlockOn, gapIsReplaced: false)'
    Tests = 'MigrationTests.Conventions_CountStatementsAfterBlocksAndNestedIfs'
}
