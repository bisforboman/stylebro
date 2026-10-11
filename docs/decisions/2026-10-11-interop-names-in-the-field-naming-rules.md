# Interop names in the field naming rules (2026-10-11)

## Question

A trial on Flow.Launcher: the field naming rules renamed Win32 and COM names: `S_OK` -> `SOk`, `MAX_PATH` -> `MaxPath`,
`DWM_E_COMPOSITIONDISABLED` -> `DwmECompositiondisabled`, `IID_IContextMenu` -> `_iidIContextMenu`, and the fields of
interop structs, `cbSize` -> `CbSize`, `hSubMenu` -> `HSubMenu`. StyleCop (and StyleBro) only skip classes named
`*NativeMethods`. Rename these, or leave them?

## Choices

- Rename them, like StyleCop outside `*NativeMethods` classes.
- Leave interop names alone, recognized by their shape.

## Decision

The owner: "Leave interop names alone." BRO1303, BRO1306, BRO1307, BRO1308 and BRO1310 skip SCREAMING_CASE constants
and `static readonly` fields (no lower-case letter, at least one underscore), COM GUID fields (`IID_`, `CLSID_`,
`LIBID_`, `DIID_`, `CATID_`, `GUID_`), and every field of a type with `[StructLayout]`, `[ComImport]` or
`[UnmanagedFunctionPointer]`, or with `[DllImport]`/`[LibraryImport]`/`extern` methods, and of types nested in one
(`FieldNames.IsInteropName`). Flow's public static `Chinese_TW` in `AvailableLanguages` is still renamed: the class is
`internal`, so it isn't public API, and `Chinese_TW` isn't SCREAMING_CASE.
