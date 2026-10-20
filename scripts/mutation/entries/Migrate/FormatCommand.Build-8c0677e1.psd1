# Check coverage (2026-10-20): an error in the code means the compiler ran there (Ulid): not "compiled nothing"
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '(current.Any(m => m.Error && !m.InSource) ||'
    Replace = '(current.Any(m => m.Error) ||'
    Tests = 'FormatCoverageTests.BuildCheck'
}
