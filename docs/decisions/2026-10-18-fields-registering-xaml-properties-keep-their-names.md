# Fields registering XAML properties keep their names (2026-10-18)

## Question

Trial 31 (Ursa.Avalonia): BRO1303 renamed 8 internal and private Avalonia property fields to the repository's naming rule
(`internal static readonly DirectProperty<AnchorItem, int> LevelProperty` -> `_levelProperty`). It compiled and the tests
passed, but Avalonia's analyzer then reported AVP1022 "Bad name" (176 more build errors with TreatWarningsAsErrors), and
XAML (Avalonia, WPF, MAUI) finds a property's registration by its `<Name>Property` field (attached properties, dependency
property lookups), so such a rename can break XAML at build or run time. How should the renamers treat these fields?

## Choices

1. Keep `<Name>Property` fields: never rename a static field of a type deriving from AvaloniaProperty, DependencyProperty
   or BindableProperty (or a RoutedEvent named `<Name>Event`), whatever its accessibility; the finding is reported as kept
   with a reason, like the other guards.
2. Keep any field of those types, whatever its name.
3. Rename as now: the repository's naming rule asks for `_levelProperty`.

## Decision

**Choice 1: Keep `<Name>Property` fields** (owner's choice, 2026-10-18). The shared renamer (BRO1303, BRO1306-BRO1308)
keeps a static field named `...Property` of a type deriving from `AvaloniaProperty`, `DependencyProperty` or
`BindableProperty`, `...PropertyKey` of `DependencyPropertyKey`/`BindablePropertyKey`, or `...Event` of a `RoutedEvent`
type, matched by type name (no framework reference needed). The kept finding says it registers a XAML framework's property
or routed event; `format` lists and baselines it.
