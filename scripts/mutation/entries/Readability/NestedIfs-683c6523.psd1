# BRO1149: an outer condition of type dynamic isn't merged either.
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'rendered.Merged.Any(m => IsDynamic(m.Condition, model) || IsDynamic(GetOuter(m)!.Condition, model))'
    Replace = 'rendered.Merged.Any(m => IsDynamic(m.Condition, model))'
    Tests = 'NestedIfTests'
}
