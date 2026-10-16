# BRO1001 inside regions
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (createsSeparation || createsComment || createsDoc || createsBrace)'
    Replace = 'if (createsSeparation || createsComment || createsBrace)'
    Tests = 'MemberOrderingTests'
}
