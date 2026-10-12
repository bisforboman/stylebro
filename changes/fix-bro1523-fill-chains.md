### Changed

- **BRO1523** leaves a chain alone when its first line already holds two or more calls
  (`sb.Append(id).Append(name)` + newline + `.Append(text).Append(end)`): such a chain fills its lines, and splitting
  only the lines below the first one left a mixed layout. Its message no longer says "like the chain's other calls".
