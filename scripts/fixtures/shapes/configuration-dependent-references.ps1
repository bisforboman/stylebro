# Trial 65 (stefanprodan/AspNetCoreRateLimit): its Demo project takes project references in Debug and the published packages of
# the same names in Release (AspNetCoreRateLimit and AspNetCoreRateLimit.Redis, which depends on the first, and the Redis project
# itself switches the same way). 'check --build'
# builds both configurations, Release last, and the restore output it left made every later full 'check' fail to load the
# projects ("The target CoreCompile does not exist in the project", Not verified) until a 'dotnet restore'. The check after
# check --build (run without the clean record, `Remove-KeptRecord`) says what the first check said.
@{
    Description = 'a project whose references depend on the configuration (projects in Debug, packages of the same names in Release)'
    Create = {
        param($root)
        New-Project $root 'src/AspNetCoreRateLimit/AspNetCoreRateLimit.csproj' -Frameworks 'net6.0'
        Set-Messy $root 'src/AspNetCoreRateLimit/Limiter.cs' -Namespace 'AspNetCoreRateLimit'
        New-Project $root 'src/AspNetCoreRateLimit.Redis/AspNetCoreRateLimit.Redis.csproj' -Frameworks 'net6.0' -Extra @'
  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <ProjectReference Include="../AspNetCoreRateLimit/AspNetCoreRateLimit.csproj" />
  </ItemGroup>
  <ItemGroup Condition="'$(Configuration)' == 'Release'">
    <PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
  </ItemGroup>
'@
        Set-Messy $root 'src/AspNetCoreRateLimit.Redis/RedisLimiter.cs' -Namespace 'AspNetCoreRateLimit.Redis'
        New-Project $root 'src/Demo/Demo.csproj' -Frameworks 'net6.0' -Extra @'
  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <ProjectReference Include="../AspNetCoreRateLimit.Redis/AspNetCoreRateLimit.Redis.csproj" />
    <ProjectReference Include="../AspNetCoreRateLimit/AspNetCoreRateLimit.csproj" />
  </ItemGroup>
  <ItemGroup Condition="'$(Configuration)' == 'Release'">
    <PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
    <PackageReference Include="AspNetCoreRateLimit.Redis" Version="2.0.0" />
  </ItemGroup>
'@
        Set-Messy $root 'src/Demo/Program.cs' -Namespace 'Demo'
        New-Solution $root 'Fixture.sln' 'src/AspNetCoreRateLimit/AspNetCoreRateLimit.csproj', 'src/AspNetCoreRateLimit.Redis/AspNetCoreRateLimit.Redis.csproj', 'src/Demo/Demo.csproj'
    }
    Tests = $false
}
