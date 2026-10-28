# Path B where StyleCop runs in only some projects (trials 44 and 58: charls-dotnet, LightInject): migrate names the others
# as without StyleCop and format leaves them alone. Trial 58: the test project, never formatted, had an xUnit2013 (one of
# the xunit.analyzers ids format applies) that fails the untouched build with warnings as errors too, and check --build
# counted it as a finding of format's rules ("Not clean: fix them by hand"). It is compared with the untouched build now.
@{
    Description = 'StyleCop 1.1.118 in the library only, migrate; the tests it leaves alone fail TWAE untouched (xUnit2013)'
    Migrate = $true
    Reference = {
        param($repo, $context)
        $file = Join-Path $repo 'src/Lib/Lib.csproj'
        $text = (Get-Content -Raw $file).Replace('<PackageReference Include="StyleCop.Analyzers" Version="1.1.118" />', "<PackageReference Include=`"StyleBro.Analyzers`" Version=`"$($context.Version)`" />")
        Set-Text $repo 'src/Lib/Lib.csproj' $text
    }
    BuildFails = 'xUnit2013 in the tests StyleCop never ran on, untouched too'
    Assert = {
        param($repo)
        $text = Get-Content -Raw (Join-Path $repo 'tests/Lib.Tests/CounterTests.cs')
        if ($text -notmatch 'Assert\.Equal\(1, counter\.Seen\.Count\)') { 'format changed the tests StyleCop never ran on' }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj' -Packages ([ordered]@{ 'StyleCop.Analyzers' = '1.1.118' })
        Set-Text $root 'src/Lib/Counter.cs' @'
// <copyright file="Counter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Lib
{
    using System.Collections.Generic;

    /// <summary>
    /// Counts.
    /// </summary>
    public class Counter
    {
        /// <summary>
        /// Gets the values seen.
        /// </summary>
        public List<int> Seen { get; } = new List<int>();

        /// <summary>
        /// Adds a value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Add(int value)
        {
            this.Seen.Add(value);
        }
    }
}
'@
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/CounterTests.cs' @'
using Xunit;

namespace Lib.Tests
{
    public class CounterTests
    {
        [Fact]
        public void Add()
        {
            var counter = new Counter();
            counter.Add(3);
            Assert.Equal(1, counter.Seen.Count);
        }
    }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
