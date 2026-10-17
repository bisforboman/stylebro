#!/usr/bin/env pwsh
# Runs one trial agent in its own container (.trials/README.md): builds or updates the image, starts the container
# stylebro-trial<N>-<Name> with .trials/trial<N>/<Folder> at /work and .trials/trial<N>/logs/<Name> at /logs (both
# read-write), the package feed at /feed (read-only) and the shared NuGet volume at /nuget, installs stylebro-migrate from
# the feed, and prints how to `docker exec` into it. -Folder and -Logs are relative to .trials/trial<N> (or absolute).
#
#   ./scripts/trials/Start-TrialContainer.ps1 -Trial 29 -Name clifx [-Folder clifx] [-Logs logs/clifx] [-Feed <dir>] [-Version 0.8.0-trial29]
#                                             [-Cpus 4] [-Memory 12g] [-Windows]
#   ./scripts/trials/Start-TrialContainer.ps1 -Trial 29 -Name clifx -Stop      (stops it; running the first line again resumes it)
#   ./scripts/trials/Start-TrialContainer.ps1 -Trial 29 [-Name clifx] -Remove  (removes one, or every container of the trial)
#
# The image and the NuGet volume stay (the warm cache is the point); `docker volume rm stylebro-nuget` empties it.
param(
    [Parameter(Mandatory)][int]$Trial,
    [string]$Name,
    [string]$Folder,
    [string]$Logs,
    [string]$Feed = "$env:TEMP\stylebro-trial$Trial\feed",
    [string]$Version = "0.8.0-trial$Trial",
    [double]$Cpus = 4,
    [string]$Memory = '12g',
    [switch]$Windows,
    [switch]$Stop,
    [switch]$Remove)
$ErrorActionPreference = 'Stop'

function Docker {
    & docker @args
    if ($LASTEXITCODE -ne 0) { throw "docker $($args -join ' ') failed ($LASTEXITCODE)" }
}

$label = "stylebro-trial=$Trial"
if ($Remove) {
    $filter = if ($Name) { "name=^stylebro-trial$Trial-$Name$" } else { "label=$label" }
    $ids = docker ps -aq --filter $filter --filter "label=$label"
    if ($ids) { Docker rm -f @ids | Out-Null }
    "Removed $(@($ids).Count) container(s)."
    return
}
if (-not $Name) { throw '-Name is required (except with -Remove).' }
$container = "stylebro-trial$Trial-$Name"
if ($Stop) {
    Docker stop $container | Out-Null
    "Stopped $container."
    return
}

$engine = docker version --format '{{.Server.Os}}' 2>$null
if ($LASTEXITCODE -ne 0) { throw 'The Docker daemon is not reachable: start Docker Desktop.' }
if ($Windows -and $engine -ne 'windows') {
    throw "Docker runs $engine containers. The Windows image needs Windows-containers mode (Docker Desktop: 'Switch to Windows containers...'), which is the owner's setting; this script never switches it."
}
if (-not $Windows -and $engine -ne 'linux') { throw "Docker runs $engine containers; the Linux image needs Linux-containers mode." }

$here = $PSScriptRoot
$trialDir = [IO.Path]::GetFullPath((Join-Path $here "../../.trials/trial$Trial"))
$work = [IO.Path]::GetFullPath([IO.Path]::Combine($trialDir, $(if ($Folder) { $Folder } else { $Name })))
$logs = [IO.Path]::GetFullPath([IO.Path]::Combine($trialDir, $(if ($Logs) { $Logs } else { "logs/$Name" })))
if (-not (Test-Path $Feed)) { throw "No package feed at $Feed (pack StyleBro.Package and StyleBro.Migrate into it first)." }
New-Item -ItemType Directory -Force $work, $logs | Out-Null

if ($Windows) {
    $image = 'stylebro-trial-windows'; $dockerfile = 'Dockerfile.windows'; $volume = 'stylebro-nuget-windows'
    $c = @{ work = 'C:\work'; logs = 'C:\logs'; feed = 'C:\feed'; nuget = 'C:\nuget' }; $tools = 'C:\tools'
} else {
    $image = 'stylebro-trial'; $dockerfile = 'Dockerfile'; $volume = 'stylebro-nuget'
    $c = @{ work = '/work'; logs = '/logs'; feed = '/feed'; nuget = '/nuget' }; $tools = '/home/trial/tools'
}

# The context is this folder only: the repository root would send every trial clone to the daemon.
Docker build -q -t $image -f (Join-Path $here $dockerfile) $here | Out-Null

$state = docker ps -a --filter "name=^$container$" --format '{{.State}}'
if (-not $state) {
    Docker run -d --name $container --label $label --hostname $container `
        --cpus $Cpus --memory $Memory `
        -v "${work}:$($c.work)" -v "${logs}:$($c.logs)" -v "${Feed}:$($c.feed):ro" -v "${volume}:$($c.nuget)" `
        $image | Out-Null
    Docker exec $container dotnet tool install --tool-path $tools StyleBro.Migrate --version $Version --add-source $c.feed | Out-Null
} elseif ($state -ne 'running') {
    Docker start $container | Out-Null
}

$shell = if ($Windows) { 'pwsh' } else { 'bash' }
@"
Container $container is running ($Cpus CPUs, $Memory; image $image, NuGet volume $volume).
  $work -> $($c.work)
  $logs -> $($c.logs)
  $Feed -> $($c.feed) (read-only; use it as the package source in the repo's nuget.config)
  stylebro-migrate $Version is on the PATH ($tools).
Commands:  docker exec $container <command>      e.g. docker exec -w $($c.work)/<repo> $container stylebro-migrate init --write
Shell:     docker exec -it $container $shell
Stop:      $PSCommandPath -Trial $Trial -Name $Name -Stop
Remove:    $PSCommandPath -Trial $Trial -Name $Name -Remove
"@
