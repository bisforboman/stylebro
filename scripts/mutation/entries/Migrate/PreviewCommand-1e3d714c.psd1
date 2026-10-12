# Trial 9, tool (2026-10-12): a project that turns central package management on itself gets no version
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (optOuts.Count > 0 || optIns.Count > 0)'
    Replace = 'if (optOuts.Count > 0)'
    Tests = 'MigrationTests.AddPackage_AProjectThatTurnsCentral'
}
