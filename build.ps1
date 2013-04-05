msbuild /t:Rebuild /p:Configuration=Release /p:platform=x86 /p:AllowedReferenceRelatedFileExtensions=none
msbuild /t:Publish /p:Configuration=Release /p:platform=x64 /p:AllowedReferenceRelatedFileExtensions=none
