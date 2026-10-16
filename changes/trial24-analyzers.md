### Changed

- Files from NuGet packages, git submodules and source generators are no longer analyzed at all: their findings were
  hidden anyway, but every rule still ran on them.
- **BRO1108** gives items that already start a line the same indentation as the items it moves, so a split list no
  longer ends up as a staircase.
- **BRO1149** keeps an inner condition's lines aligned under its `(` when it joins the outer condition's line.

### Fixed

- **BRO1001** with BRO1505 off (CSharpier): the sort adds the blank line BRO1519 wants after a moved member's `}`, and
  doesn't leave one between members that no longer need it, so one `dotnet format` run is enough and the result no
  longer depends on the order the fixes run in.
- Rules that need type information (such as **BRO1104**, **BRO1106**, **BRO1122**) now also report in the body of a
  partial method whose declaration a source generator wrote (CommunityToolkit.Mvvm's `OnTitleChanged`).
- **BRO1603** no longer adds a period after a line of code that ends in a word after an operator (`=> 0`, `x == null`).
