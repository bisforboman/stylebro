# BRO1311 (tuple element casing)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = '&& element.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))'
    Replace = ')'
    Tests = 'TupleElementNamingTests'
}
