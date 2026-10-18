# Backlog 073 (2026-10-18): xUnit fixes in format
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '{(xunit ? XunitPart : string.Empty)} and the built-in rules init and migrate turn on ({ids} ids);'
    Replace = ' and the built-in rules init and migrate turn on ({ids} ids);'
    Tests = 'MigrationTests.Format_SaysItFixesXunitRules_OnlyWhereItDoes'
}
