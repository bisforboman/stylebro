# Check coverage (2026-10-20): an error in the code means the compiler ran there (Ulid): not "compiled nothing"
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'message.Error && !message.InSource && !EmitErrors.Contains(message.Id)'
    Replace = 'message.Error && !EmitErrors.Contains(message.Id)'
    Tests = 'FormatCoverageTests.BuildCheck'
}
