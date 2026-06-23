# Pep.Common
Perculiar ERP Mediator.

## Create and publish package
### Perp.Nexus.Core
```powershell
$owner="peculiaerp"
$version="1.0.0"

dotnet pack Perp.Nexus.Core --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Perp.Nexus -o ..\packages

dotnet nuget push ..\packages\Perp.Nexus.Core.$version.nupkg --api-key $env:PECULIAERP_GITHUB_KEY --source "peculiar-github"
```

### Perp.Nexus.Infrastructure
```powershell
$owner="peculiaerp"
$version="1.0.0"

dotnet pack Perp.Nexus.Infrastructure --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Perp.Nexus -o ..\packages

dotnet nuget push ..\packages\Perp.Nexus.Infrastructure.$version.nupkg --api-key $env:PECULIAERP_GITHUB_KEY --source "peculiar-github"
```

## Run powershell script to update package version in all projects
```
run cd..
run .\PackageScripts.ps1
```


