NuGet.exe restore -PackagesDirectory packages -NonInteractive Marathon.sln
msbuild /p:Configuration=Release /p:Platform=AnyCPU Runner\Runner.csproj
NuGet.exe pack -OutputDirectory Build -Version $env:GitVersionNuGetVersion -NonInteractive -Properties Configuration=Release -Properties Platform=AnyCPU Runner\Runner.csproj

New-Item -ItemType Directory -Path $env:ARTIFACTS\Marathon\Release -Force
Copy-Item -Path Build\*  -Destination $env:ARTIFACTS\Marathon\Release\

New-Item -ItemType Directory -Path $env:ARTIFACTS\Marathon\$env:CI_BUILD_REF_NAME -Force
Copy-Item -Path Build\*  -Destination $env:ARTIFACTS\Marathon\$env:CI_BUILD_REF_NAME\

Copy-Item -Path Build\*.nupkg  -Destination $env:NUGET_PACKAGES