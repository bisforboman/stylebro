# trial 6 (2026-10-11): framework spellings, kept reasons, the package under central package management, the preset opt-out, init's next step
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Select(g => g.OrderBy(k => k.Reason == KeptReason.UsedInProjectNotLoaded).First())'
    Replace = '.Select(g => g.First())'
    Tests = 'KeptFindingsTests.ReadKept_PrefersTheReasonOfARunThatLoadsEveryUse'
}
