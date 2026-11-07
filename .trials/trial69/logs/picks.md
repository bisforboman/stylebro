# Trial 69 picks (seed 69, 128 entries, one rng, three draws)
Draws: 9, 24, 42. Entries 9..91 are all in used.tsv; 92 PolySharp (needs SDK 11), 93 Statiq.Framework (too large), 103 WeakEvent (11 files) stay skipped; 104-114 used.
- 9 -> 115 mivano/azure-cost-cli @ f60f863014487c1edb9d2addcc6dcecbf7403e12 (104 .cs, net8.0;net10.0, Directory.Build.props)
- 24 -> 116 dotliquid/dotliquid @ e1e721aa558791e96d65bbd2a7dcfd05a8668caa (148 .cs, netstandard1.3;netstandard2.0;net45;net6.0 lib, net461;net6.0;netcoreapp1.0;netcoreapp2.0 tests)
- 42 -> 117 NModbus/NModbus @ baddfca14e2852373a238aee8d234d6627983b20 (176 .cs, net46..net6.0, NuGet.config, tab indents)
Targeted (owner's pick, backlog 082 lens: white space in <TargetFrameworks>): genaray/Arch @ cbe716faa116dc61885be696a9af5f27ef9b896a (124 .cs, 'net8.0; net6.0; netstandard2.1' in the library, single-entry <TargetFrameworks> in tests/samples). Code search found no multi-line list; thomhurst/Inject.NET was the other candidate.
All four Debug builds pass (logs/smoke-*.log).
