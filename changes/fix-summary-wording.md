### Changed

- **`init` follows how your summaries are worded:** it counts property summaries that start with `Gets`/`Sets` and
  constructor summaries that start with `Initializes a new instance of ...` against ones in their own words. When
  most are in their own words, or the code mixes both, BRO1604/BRO1606 are turned off, so `format` doesn't reword your
  public documentation.
- **BRO1606** no longer puts the standard sentence in front of a constructor summary that already says something
  (`Opens a file or URL ...`): only empty summaries, filler (`Default constructor.`) and the standard sentence written
  differently are reported. StyleCop's SA1642 reports them all.
- **BRO1604** no longer reports a `bool` property's summary that starts with an article (`The tokenizer predicted a
  token here.`): no prefix makes a sentence of every such clause (it became `Gets or sets the tokenizer predicted ...`).
