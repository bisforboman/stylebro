# Main CI 2026-10-26: Roslyn 4.8 FindReferences race (KeyNotFoundException), searched again
@{
    File = 'src/StyleBro.CodeFixes/References.cs'
    Find = 'when (attempt < Attempts)'
    Replace = 'when (false)'
    Tests = 'LinkedFileFixAllTests.RoslynsFindReferencesRace'
}
