New-Item -ItemType Directory -Force -Path "$($Env:GITHUB_WORKSPACE)\output"
$exclusions = @(git submodule foreach --quiet 'echo $name')
Write-Output "Exclusions $($exclusions)"
Get-ChildItem -recurse -Path "$($Env:GITHUB_WORKSPACE)" -include @("*.clz", "*.cpz", "*.cplz") | ForEach-Object{
  Write-Output "checking $($_)"
  $allowed = $true;
  foreach($exclude in $exclusions) {
    if((Split-Path $_.FullName -Parent) -ilike $exclude) {
      Write-Output "excluding $($_)"
      $allowed = $false;
      break;
      }
    }
  if($allowed) {
    Write-Output "allowing $($_)"
    $_;
  }
} | Copy-Item -Destination "$($Env:GITHUB_WORKSPACE)\output"
Get-ChildItem "$($Env:GITHUB_WORKSPACE)\output"
Compress-Archive -Path "$($Env:GITHUB_WORKSPACE)\output\*" -DestinationPath "$($Env:GITHUB_WORKSPACE)\output.zip"
Get-ChildItem "$($Env:GITHUB_WORKSPACE)\"
