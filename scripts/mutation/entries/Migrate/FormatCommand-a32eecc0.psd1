# The analyzer pass gets no IDE ids (079).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!options[i].StartsWith("IDE", StringComparison.OrdinalIgnoreCase))'
    Replace = 'if (true)'
    Tests = 'WithoutStyleIds'
}
