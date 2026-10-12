### Changed

- **`stylebro-migrate init`** follows two more of your code's conventions:
  - members left `private` by default (no access modifier): mostly without writes
    `dotnet_style_require_accessibility_modifiers = omit_if_default`, so BRO1404 no longer adds `private` everywhere;
    mixed turns BRO1404 off;
  - combined declarations (`int n = h.Length, i = 0;`), counted apart for locals and fields: mostly combined or mixed
    turns BRO1142 (locals) or BRO1114 (fields) off, so `format` doesn't split them.
