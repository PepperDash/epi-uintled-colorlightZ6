New-Item -ItemType Directory -Force -Path "$($Env:GITHUB_WORKSPACE)\output"
$exclusions = @(git submodule foreach --quiet 'echo $name')
Get-ChildItem -recurse -Path "$Env:GITHUB_WORKSPACE" -include @("*.clz", "*.cpz", "*.cplz") |
%{
  $allowed = $true;
  foreach($exclude in $exclusions) {
    if((Split-Path $_.FullName -Parent) -ilike $exclude) {
      $allowed = $false;
      break;
      }
    }
  if($allowed) {
    $_;
  }
} | Copy-Item -Destination $($Env:GITHUB_WORKSPACE)\output
dir $($Env:GITHUB_WORKSPACE)\output
Compress-Archive -Path "$($Env:GITHUB_WORKSPACE)\output\*" -DestinationPath "$($Env:GITHUB_WORKSPACE)\output.zip)"
dir $($Env:GITHUB_WORKSPACE)
