### Changed

- **`bro migrate`** knows StyleCop.Analyzers 1.3: in a repository on a 1.3 beta the rules 1.3 added (SA1143, SA1315, SA1317, SA1653, SA1654, SX1412) count, and the report lists those nothing enforces yet; in a repository on 1.2 they count as off, as before.
- The comparison with StyleCop ([StyleBro vs StyleCop](https://bisforboman.github.io/stylebro/stylebro-vs-stylecop/), [differences](https://bisforboman.github.io/stylebro/differences-from-stylecop/)) is re-measured against StyleCop.Analyzers 1.3.177-beta, with a new section on what 1.3 changed, and [migrating](https://bisforboman.github.io/stylebro/migrating/#coming-from-stylecop-13) has a section for teams coming from 1.3.
