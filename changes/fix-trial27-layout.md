### Changed

- **BRO1001** keeps blank lines when it sorts: members that move together keep the gap between them (fields moved to
  the top stay together), and a moved member gets a blank line where its new neighbour needs one, also with BRO1505 off
  (the Microsoft style). No more `}` glued to the next member.
- **BRO1506**: a comment right after a block's `{` with a blank line below it counts as standalone, like the section
  headers after it, and keeps its blank line.
- Findings in files a project compiles from outside its git repository (`..\..\outside\Shared.cs`) are hidden in the
  build, StyleBro's and those of the built-in rules it turns on: `bro format` leaves those files alone.
  `<StyleBroRepositoryRoot>none</StyleBroRepositoryRoot>` reports them again.

### Fixed

- **BRO1001** with `stylebro_member_order_scope = fields_first` no longer leaves a type unsorted because its first member
  has a comment above it.
- **BRO1515** with `stylebro_layout_by = csharpier` no longer wants braces around a long `return` of one string, which
  CSharpier keeps on one line.
- **BRO1514-BRO1516** leave an empty statement as the body (`foreach (var x in xs) ;`) to the compiler's warning, like
  BRO1101; wrapped in braces, it took a second `dotnet format` run.
- **BRO1405** removes nested parentheses (`F(((x * y)))`) in one run, and **BRO1523** looks through the parentheses
  BRO1405 removes, so a chain they joined is laid out in the same run.
