# BRO1618's <c>keyword</c> places are counted
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Count(f => f.Id == DiagnosticIds.LangwordElement)'
    Replace = 'Count(f => f.Id != DiagnosticIds.LangwordElement)'
    Tests = 'MigrationTests.Conventions_CountDocPeriodsCommentsLangwords'
}
