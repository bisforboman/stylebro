# After a wrapped condition the next one starts its own line (Flow.Launcher layout)
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '            var ownLine = current != top && builder.ToString().IndexOf(''\n'') >= 0;'
    Replace = '            var ownLine = false;'
    Tests = 'NestedIfTests'
}
