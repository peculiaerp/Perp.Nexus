$owner="peculiaerp"
$version="1.0.0"

$commitMessage="First commit"

Write-Host "--- Staging changes ---" -ForegroundColor Cyan
git add .

Write-Host "--- Committing changes ---" -ForegroundColor Cyan
git commit -m "$commitMessage"

Write-Host "--- Pushing to GitHub ---" -ForegroundColor Cyan
git push

dotnet pack Perp.Nexus.Core --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Perp.Nexus -o ..\packages

dotnet nuget push ..\packages\Perp.Nexus.Core.$version.nupkg --api-key $env:PECULIAERP_GITHUB_KEY --source "peculiar-github"

dotnet pack Perp.Nexus.Infrastructure --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Perp.Nexus -o ..\packages

dotnet nuget push ..\packages\Perp.Nexus.Infrastructure.$version.nupkg --api-key $env:PECULIAERP_GITHUB_KEY --source "peculiar-github"