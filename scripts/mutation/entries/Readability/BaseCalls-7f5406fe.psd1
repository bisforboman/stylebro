# BRO1131 (base calls). Not mutated: Speculation.SymbolAfterReplacing vs a position-based lookup of a DETACHED
# node (equivalent; the original bug passed the attached access.Name, which binds as a member-access name).
@{
    File = 'src/StyleBro.Analyzers/Readability/BaseCalls.cs'
    Find = 'var dispatchesVirtually = (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) && !symbol.IsSealed;'
    Replace = 'var dispatchesVirtually = false;'
    Tests = 'BaseCallsTests'
}
