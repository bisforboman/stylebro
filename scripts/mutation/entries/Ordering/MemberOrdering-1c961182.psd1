# BRO1001 inside regions
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (createsSeparation || createsComment || createsDoc)'
    Replace = 'if (createsSeparation || createsComment)'
    Tests = 'MemberOrderingTests'
}
