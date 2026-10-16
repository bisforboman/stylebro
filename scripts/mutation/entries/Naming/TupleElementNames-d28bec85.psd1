# Trial 26: names a public interface in the solution forces
@{
    File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'
    Find = '(!b.Locations.Any(l => l.IsInSource) || !PublicApi.CanRename(b, options))'
    Replace = '!b.Locations.Any(l => l.IsInSource)'
    Tests = 'TupleElementNamingTests.NamesForcedByAPublicInterface_AreNotReportedByDefault'
}
