# No diagnostics: no language filter (Newtonsoft.Json real-world: No associated code fix found).
@{
    File = 'src/StyleBro.FormatHost/FixOrder.cs'
    Find = 'languages.ToHashSet(StringComparer.Ordinal) is { Count: > 0 } set ? set : null;'
    Replace = 'languages.ToHashSet(StringComparer.Ordinal);'
    Tests = 'StyleBro.Tests.FormatHostFixTests.AFixer_ForAnotherLanguage_IsLeftOut'
}
