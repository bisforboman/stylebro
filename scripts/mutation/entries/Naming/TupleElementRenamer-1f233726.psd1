# BRO1311: names of one tuple type are renamed all or none (trial 20)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'parents[Find(name)] = Find(inGroup[0]);'
    Replace = '_ = Find(inGroup[0]);'
    Tests = 'TupleElementNamingTests.ElementsOfOneTupleType'
}
