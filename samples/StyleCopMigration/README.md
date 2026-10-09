# Migration sample: StyleCop to StyleBro

A tiny project that uses StyleCop.Analyzers, migrated to StyleBro step by step. The committed files are the "before"
state; run the steps below to see the "after". The output shown here is real, trimmed where marked.

The project is StyleCop-clean (0 warnings, `TreatWarningsAsErrors`) and has changed a few of StyleCop's defaults, like
most teams do:

- `.editorconfig`: SA1101 (`this.` prefix), SA1309 (no `_` on fields) and SA1413 (trailing commas) are off. The code
  uses `_fields` and no trailing commas.
- `stylecop.json`: `usingDirectivesPlacement` is `outsideNamespace`, `documentInternalElements` is `false` (the
  internal `InMemoryOrderRepository` has no documentation), and `companyName` is `Contoso` for the file headers.

`Directory.Build.props` and `Directory.Packages.props` only keep the sample independent of the StyleBro repository's
own build settings.

## 1. Dry run

```
dotnet tool install --global StyleBro.Migrate --prerelease
stylebro-migrate samples/StyleCopMigration
```

From a clone of this repository: `dotnet run --project src/StyleBro.Migrate -- samples/StyleCopMigration`.

The report says what it read and which StyleCop rules nothing enforces after the switch:

```text
Read: .editorconfig, stylecop.json
StyleCop.Analyzers version: 1.2.0-beta.556
StyleCop rules on: 172; enforced by StyleBro or the SDK after migrating: 141
Not enforced after removing StyleCop (31):
  Dropped by design (no safe automatic fix, or not applicable):
    SA0001: StyleCop's own setup diagnostic: XML comment analysis is disabled (no GenerateDocumentationFile).
    SA1401: Making a public or protected field private changes the public API and breaks callers. ...
    SA1402: Moving a type to its own file isn't something dotnet format can do (code fixes can't add documents through it).
    ...
    SA1615: Missing <returns>; the fix would be placeholder text.
    ...
    SA1649: Renaming files isn't something dotnet format can do.
    ...
  Not covered yet:
    SA1316: Tuple element names should use correct casing
    SA1517: Code should not contain blank lines at start of file
Private fields: 3 named '_field', 0 named 'field'; SA1309 is off, so BRO1303 uses '_camelCase'.
Suppressions: no StyleCop suppressions in the code to carry over.
```

Then the settings it would write (the full block is below, under step 2).

## 2. Write the settings

```
stylebro-migrate samples/StyleCopMigration --write
```

```text
Wrote the settings to .editorconfig.
Turned the preset off in Directory.Build.props (<StyleBroPreset>none</StyleBroPreset>): the settings above replace it.
Next: add the StyleBro.Analyzers package, remove StyleCop.Analyzers, and run 'stylebro-migrate format'.
```

The block added to `.editorconfig` (the team's own lines above it stay). The team's changes show up as settings:
`_camelCase` for SA1309 off, BRO1401 off for SA1413 off, `outside_namespace`, `stylebro_document_internal_elements =
false`, the company name. Every other rule follows StyleCop's defaults.

```ini
# BEGIN stylebro-migrate
# Generated from this repository's StyleCop settings by stylebro-migrate. Run it again to update; edits
# between these markers are overwritten.
[*.cs]
# StyleBro rules: on when StyleCop enforced every rule they replace.
dotnet_diagnostic.BRO1001.severity = warning
dotnet_diagnostic.BRO1002.severity = warning
# ... (every BRO rule; all at warning except these two)
dotnet_diagnostic.BRO1310.severity = none
dotnet_diagnostic.BRO1401.severity = none
# ...
dotnet_diagnostic.BRO1615.severity = warning
stylebro_member_order = field, constructor, finalizer, delegate, event, enum, interface, property, indexer, conversion, operator, extension, method, struct, class
stylebro_member_access_order = public, internal, protected_internal, protected, private_protected, private
stylebro_member_constants_first = true
stylebro_member_static_first = true
stylebro_member_readonly_first = true
stylebro_private_field_naming = _camelCase
stylebro_private_static_field_naming = PascalCase
stylebro_document_exposed_elements = true
stylebro_document_internal_elements = false
stylebro_document_private_elements = false
stylebro_allow_consecutive_usings = true
stylebro_file_header_company = Contoso
stylebro_file_header_copyright = Copyright (c) {companyName}. All rights reserved.

# Built-in .NET rules, with the severity of the StyleCop rules they cover.
dotnet_diagnostic.IDE0055.severity = warning
csharp_new_line_before_open_brace = all
csharp_preserve_single_line_blocks = true
csharp_preserve_single_line_statements = false
dotnet_diagnostic.IDE0011.severity = none
csharp_prefer_braces = true
dotnet_diagnostic.IDE0040.severity = none
dotnet_style_require_accessibility_modifiers = for_non_interface_members
dotnet_diagnostic.IDE0036.severity = warning
csharp_preferred_modifier_order = public,private,protected,internal,file,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,required,volatile,async
dotnet_diagnostic.IDE0049.severity = warning
dotnet_style_predefined_type_for_locals_parameters_members = true
dotnet_style_predefined_type_for_member_access = true
dotnet_diagnostic.IDE0047.severity = none
dotnet_diagnostic.IDE0048.severity = none
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity
dotnet_style_parentheses_in_other_operators = never_if_unnecessary
dotnet_diagnostic.IDE0065.severity = warning
csharp_using_directive_placement = outside_namespace
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
dotnet_diagnostic.IDE2000.severity = none
dotnet_style_allow_multiple_blank_lines_experimental = false
dotnet_diagnostic.IDE2002.severity = none
csharp_style_allow_blank_lines_between_consecutive_braces_experimental = false
dotnet_diagnostic.IDE2003.severity = none
dotnet_style_allow_statement_immediately_after_block_experimental = false
dotnet_diagnostic.IDE0009.severity = none
dotnet_diagnostic.IDE0003.severity = none
dotnet_style_qualification_for_field = false
dotnet_style_qualification_for_property = false
dotnet_style_qualification_for_method = false
dotnet_style_qualification_for_event = false
# END stylebro-migrate
```

`Directory.Build.props` gets `<StyleBroPreset>none</StyleBroPreset>`: the block above replaces the preset.

## 3. Swap the package

In `Orders.csproj`, replace StyleCop.Analyzers with StyleBro.Analyzers (use the latest version):

```diff
-    <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" PrivateAssets="all" />
+    <PackageReference Include="StyleBro.Analyzers" Version="0.3.0-alpha.1" PrivateAssets="all" />
```

`stylecop.json` can stay until StyleCop is gone everywhere; StyleBro doesn't read it.

## 4. Run dotnet format

```
dotnet format samples/StyleCopMigration/Orders.csproj
dotnet format samples/StyleCopMigration/Orders.csproj --verify-no-changes --severity warn
```

The first run changes no code file, and the check passes (exit code 0). The build has 0 warnings too. StyleBro now
enforces what StyleCop did: a new file with `class Tmp { int x; }` gets BRO1615 (no header), BRO1404 (access
modifiers), BRO1303 (`Rename 'x' to '_x'`), BRO1509 (one line) and BRO1505 (blank line), all fixable by
`dotnet format`.

That's the point of migrating: a StyleCop-clean repository stays as it is.

## Without the migration tool

For comparison: `stylebro-migrate init --write` refuses here (it finds `stylecop.json` and points to the migration).
With StyleBro's preset anyway (the package without the migration's settings, then `dotnet format`), 3 of the 4 files
change, because the preset follows StyleCop's defaults, not this team's changes:

```diff
-    private readonly List<string> _items = new List<string>();
-    private OrderStatus _status;
+    private readonly List<string> items = new List<string>();
+    private OrderStatus status;
...
-            _ => throw new InvalidOperationException("The order has already shipped.")
+            _ => throw new InvalidOperationException("The order has already shipped."),
...
+    /// <inheritdoc/>
     public void Add(Order order)
```

Fields lose their `_` (SA1309 is on by default), multi-line initializers get trailing commas (SA1413), and the
internal repository's members get `<inheritdoc/>` (`documentInternalElements` is `true` by default).
