# Trial 24 (Refit): a listed record gets its own place (SST1201)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'if (kindOrder?.Exists(group => Array.IndexOf(group, (int)MemberKind.Record) >= 0) != true)'
    Replace = 'if (true)'
    Tests = 'MemberOrderingTests.Records_ComeAfterClasses_WhenListed'
}
