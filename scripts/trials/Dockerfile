# Linux image for StyleBro's first-run trials, the fallback for local reruns (trials run as cloud sessions) (scripts/trials/Start-TrialContainer.ps1 builds and runs it; .trials/README.md).
# .NET SDK 10 (the base, same version as the trial machine), SDKs 9 and 8 and runtimes 7 and 6 side by side, so repos that
# target net6.0-net10.0 build and test. The base image already has git, curl and PowerShell (pwsh); python3 is added.
# Nothing GUI, no secrets. Every version is pinned: raise them together with the trial machine's SDKs.
FROM mcr.microsoft.com/dotnet/sdk:10.0.402-noble

RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 \
    && rm -rf /var/lib/apt/lists/*

# Into the base SDK's own folder, so one `dotnet` sees all of them.
RUN curl -fsSL https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && for sdk in 9.0.319 8.0.426; do bash /tmp/dotnet-install.sh --version $sdk --install-dir /usr/share/dotnet --no-path; done \
    && for rt in 7.0.20 6.0.36; do \
         bash /tmp/dotnet-install.sh --runtime dotnet --version $rt --install-dir /usr/share/dotnet --no-path \
         && bash /tmp/dotnet-install.sh --runtime aspnetcore --version $rt --install-dir /usr/share/dotnet --no-path; done \
    && rm /tmp/dotnet-install.sh \
    && dotnet --list-sdks && dotnet --list-runtimes && pwsh -v && git --version && python3 --version

# Non-root user. /nuget is the mount point of the shared NuGet volume: a new named volume takes this folder's owner, so the
# user can write it. Bind mounts from Windows show up root-owned, hence safe.directory.
RUN useradd -m -u 1000 trial \
    && mkdir /nuget /work /logs \
    && chown trial:trial /nuget /work /logs
USER trial
RUN git config --global --add safe.directory '*' \
    && git config --global core.fileMode false \
    && git config --global user.name trial \
    && git config --global user.email trial@example.invalid

ENV NUGET_PACKAGES=/nuget \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DiffEngine_Disabled=true \
    PATH="/home/trial/tools:${PATH}"
WORKDIR /work
CMD ["sleep", "infinity"]
