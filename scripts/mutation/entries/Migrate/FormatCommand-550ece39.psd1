# trial 6 (2026-10-11): framework spellings, kept reasons, the package under central package management, the preset opt-out, init's next step
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (changed.Count == 0 || maxRuns == 1)'
    Replace = 'if (changed.Count == 0)'
    Tests = 'KeptFindingsTests.Format_Once'
}
