# Trial 24 audit: SST1201 orders only a type members
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'var orderNamespaces = ReadBool(options, NamespacesKey, true);'
    Replace = 'var orderNamespaces = true;'
    Tests = 'MemberOrderingTests.NamespaceMembers_AreLeftAlone_WhenTurnedOff'
}
