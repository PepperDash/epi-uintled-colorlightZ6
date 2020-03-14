$tagCount = $(git rev-list --tags='*.*.*' --count)
if ($tagCount -eq 0) {
  $latestVersion = "0.0.0"
}
else {
  $latestVersions = $(git describe --tags $(git rev-list --tags='*.*.*' --max-count=10) --abbrev=0) 
  $latestVersion = ""
  Foreach ($version in $latestVersions) {
    Write-Output $version
    if ($version -match '^[1-9]+.\d+.\d+$') {
      $latestVersion = $version
      Write-Output "Setting latest version to: $latestVersion"
      break
    }
  }
}
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

# .\Pepperdash` Core\Pepperdash` Core\Properties\UpdateAssemblyVersion.ps1 $newVersionString

Write-Output $newVersionString
# "version=$newVersionString" | Out-File env.properties -Encoding ASCII