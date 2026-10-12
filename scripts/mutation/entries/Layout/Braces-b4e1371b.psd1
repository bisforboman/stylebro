# A chain that braces on one clause make inconsistent gets all its braces in one run (sourcegit)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = '|| (afterFix && isOn(DiagnosticIds.BracesConsistent) && GetFindings(node, text, isOn, options).Any())'
    Replace = ''
    Tests = 'FixOrderTests'
}
