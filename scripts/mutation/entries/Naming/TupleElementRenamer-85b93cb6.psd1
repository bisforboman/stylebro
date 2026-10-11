# BRO1311 (tuple element casing)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'if (target is not null && target.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))'
    Replace = 'if (true)'
    Tests = 'TupleElementNamingTests'
}
