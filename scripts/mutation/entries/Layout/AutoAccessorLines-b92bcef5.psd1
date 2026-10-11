# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = 'if (!gaps.All(gap => SingleLineBlocks.Gap(gap.Before, gap.After, " ", text, changes)))'
    Replace = 'if (!gaps.All(gap => SingleLineBlocks.Gap(gap.Before, gap.After, " ", text, changes) || true))'
    Tests = 'AutoAccessorLinesTests'
}
