# Parallel Fix All: results in the items order
@{
    File = 'src/StyleBro.CodeFixes/LinkedFileFixAllProvider.cs'
    Find = 'results[i] = await run(items[i])'
    Replace = 'results[items.Count - 1 - i] = await run(items[i])'
    Tests = 'LinkedFileFixAllTests'
}
