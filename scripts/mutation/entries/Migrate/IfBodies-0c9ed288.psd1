# The '//' lines above a protected line keep their indentation too (Newtonsoft.Json's TraceJsonWriter.cs).
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = '.TrimStart().StartsWith("//", StringComparison.Ordinal); above--)'
    Replace = '.TrimStart().StartsWith("//", StringComparison.Ordinal) && false; above--)'
    Tests = 'MigrationTests.IfBodies_AlsoAreTheCommentLinesAboveOne'
}
