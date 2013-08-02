if(!$PSScriptRoot){[string]$PSScriptRoot = $PWD}
msbuild $PSScriptRoot\Package\Package.csproj /t:CopyBits /p:platform=x86 /p:Configuration=Release /p:SolutionDir=$PSScriptRoot\
msbuild $PSScriptRoot\Package\Package.csproj /t:CopyBits /p:platform=x64 /p:Configuration=Release /p:SolutionDir=$PSScriptRoot\

# For now, need to manually remove the .Net 4.5 reference from the 3.5 config file
# So we're building both x86 and x64 with /t:CopyBits (instead of /t:Package)
# And then we'll edit the files and create the .tar.gz package "by hand" here:

$AssemblyName = "SA-ModularInput-PowerShell"
$Env:OutputPath = Join-Path $PSScriptRoot "Output\Release"

$Config =  Join-Path $Env:OutputPath "${AssemblyName}\windows_x86\bin\PowerShell2.exe.config"
Set-Content $Config ($(Get-Content $Config) -NotMatch "lib/net45")

$Config =  Join-Path $Env:OutputPath "${AssemblyName}\windows_x86_64\bin\PowerShell2.exe.config"
Set-Content $Config ($(Get-Content $Config) -NotMatch "lib/net45")

Remove-Item (Join-Path ${Env:OutputPath} "${AssemblyName}.tar.gz") -Force
# Sadly, we have to shell out to cmd in order to do .tar.gz files in one step with 7z.exe
$PrePath = $Env:Path
$Env:PATH = $Env:Path += ";$(Join-Path $PSScriptRoot .nuget)"
# PowerShell can't handle binary pipes, and we want to avoid the intermediate .tar file:
cmd /V:ON /c "7za.exe a -ttar -so -x!*.vshost.exe -x!*.7z -x!*.pdb -x!*.gz -x!*.zip stdout ""!OutputPath!\*"" | 7za.exe a -si -tgzip -mx9 -bd ""!OutputPath!\${AssemblyName}.tar.gz"""
$Env:Path = $PrePath