# With CSharpier, init writes no using sort key (2026-10-13)

## Question

tusdotnet formats with CSharpier, which sorts using directives itself. `init` wrote `dotnet_sort_system_directives_first
= true` (CSharpier's System-first order, #168), so `dotnet format` sorted them too, with its own comparer: two files
swapped `using AspNetCore_netcoreapp3._0_TestApp.Endpoints;` and `using AspNetCore_netcoreapp3_0_TestApp.Authentication;`
back and forth between the two tools. What should `init` (and `--write`) write for the using order when CSharpier formats
the code?

## Choices

1. **No sort key unless the repository sets one** (agent's proposal). `dotnet format` sorts usings only when
   `dotnet_sort_system_directives_first` or `dotnet_separate_import_directive_groups` is set, so without them CSharpier
   alone orders them. The using convention is reported as `set` ("CSharpier sorts them"), a sort key from an earlier
   init isn't kept, and `--write` drops the keys SA1208/SA1210 would bring. Where the repository's .editorconfig sets a
   key itself, init still writes the value closer to CSharpier's order (an .editorconfig can't unset a key, #168).
2. Keep writing `true` (CSharpier's order with `dotnet format`'s comparer): the two disagree on names like `App3._0` and
   `App3_0`, and every run flips them.

## Decision

**Choice 1** (agent's proposal; the owner can revisit). A repository that sets a sort key keeps the edge case; the
init report says which key it is.
