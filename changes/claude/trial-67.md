### Fixed

- `bro init --write` now carries suppressions of the built-in rules it turns off for StyleBro's over to the rules that replace them (`#pragma warning disable IDE0040`, `[SuppressMessage]` and `<NoWarn>` get BRO1007 and BRO1404 next to it; IDE0011, IDE0047, IDE0048 and IDE0065 likewise), the way `bro migrate` does for StyleCop ids. Before, code you had exempted from IDE0040 (an interface left implicitly private on purpose) still got `private` added.
- `bro check` straight after a clean `bro format` answers from format's record again in repositories that also get xunit.analyzers' "no Fix All" note (xunit.v3 test projects): it took the full 18 s instead of 1 s there.
