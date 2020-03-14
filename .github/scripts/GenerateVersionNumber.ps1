Write-Output "Getting Latest Version"
$latestVersions = $(git describe --tags $(git rev-list --tags='*.*.*' --max-count=10) --abbrev=0) 
Write-Output "Latest Versions:" 
Write-Output $latestVersions
$latestVersion = ""
Foreach ($version in $latestVersions) {
  Write-Output $version
  if ($version -match '^[1-9]+.\d+.\d+$') {
    $latestVersion = $version
    Write-Output "Setting latest version to: $latestVersion"
    break
  }
}
Write-Output "Latest: $latestVersion"
Write-Output "Incrementing Version"
$newVersion = [version]$latestVersion
$phase = ""
$newVersionString = ""
switch -regex ($Env:GITHUB_REF) {
  '^origin\/master\/*.' {
    $newVersionString = "{0}.{1}.{2}" -f $newVersion.Major, $newVersion.Minor, ($newVersion.Build + 1)
  }
  '^origin\/feature\/*.' {
    $phase = 'alpha'
  }
  '^origin\/release\/*.' {
    $phase = 'rc'
  }
  '^origin\/development\/*.' {
    $phase = 'beta'
  }
  '^origin\/hotfix\/*.' {
    $phase = 'hotfix'
  }
}
$newVersionString = "{0}.{1}.{2}-{3}-{4}" -f $newVersion.Major, $newVersion.Minor, ($newVersion.Build + 1), $phase, $Env:GITHUB_RUN_NUMBER
Write-Output "Incremented Version: $newVersionString"

Write-Output "Updating Assembly Build Versions..."
# .\Pepperdash` Core\Pepperdash` Core\Properties\UpdateAssemblyVersion.ps1 $newVersionString
Write-Output "Assembly Build Versions Updated"

Write-Output "Exporting VERSION environment variable"
$Env:VERSION = $newVersionString
# "version=$newVersionString" | Out-File env.properties -Encoding ASCII