# Removing regions also does what BRO1001 and BRO1506 then want, only when they're on
@{
    File = 'src/StyleBro.Analyzers/Readability/Regions.cs'
    Find = 'RegionDirectiveTriviaSyntax { IsActive: true } region'
    Replace = 'RegionDirectiveTriviaSyntax region'
    Tests = 'RegionsTests'
}
