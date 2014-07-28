function Get-LocalStoragePath {
	#.Synopsis
	#   Gets the LocalApplicationData path for the specified company\module 
	#.Description
	#   Appends Company\Module to the LocalApplicationData, and ensures that the folder exists.
	param(
		# The name of the module you want to access storage for (defaults to SplunkStanzaName)
		[Parameter(Position=0)]
		[ValidateScript({ 
			$invalid = $_.IndexOfAny([IO.Path]::GetInvalidFileNameChars())			
			if($invalid -eq -1){ 
				return $true
			} else {
				throw "Invalid character in Module Name '$_' at $invalid"
			}
		})]			
		[string]$Module = $SplunkStanzaName,

		# The name of a "company" to use in the storage path (defaults to "Splunk")
		[Parameter(Position=1)]
		[ValidateScript({ 
			$invalid = $_.IndexOfAny([IO.Path]::GetInvalidFileNameChars())			
			if($invalid -eq -1){ 
				return $true
			} else {
				throw "Invalid character in Company Name '$_' at $invalid"
			}
		})]			
		[string]$Company = "Splunk"		

	)
	end {
		if(!($path = $SplunkCheckpointPath)) {
			$path = Join-Path ([Environment]::GetFolderPath("LocalApplicationData")) $Company
		} 
		$path  = Join-Path $path $Module

		if(!(Test-Path $path -PathType Container)) {
			$null = New-Item $path -Type Directory -Force
		}
		Write-Output $path
	}
}

function Export-LocalStorage {
	#.Synopsis
	#   Saves the object to local storage with the specified name
	#.Description
	#   Persists objects to disk using Get-LocalStoragePath and Export-CliXml
	param(
		# A unique valid file name to use when persisting the object to disk
		[Parameter(Mandatory=$true, Position=0)]
		[ValidateScript({ 
			$invalid = $_.IndexOfAny([IO.Path]::GetInvalidFileNameChars())			
			if($invalid -eq -1){ 
				return $true
			} else {
				throw "Invalid character in Object Name '$_' at $invalid"
			}
		})]		
		[string]$name,

		# The object to persist to disk
		[Parameter(Mandatory=$true, Position=1, ValueFromPipeline=$true)]
		$InputObject,

		# A unique valid module name to use when persisting the object to disk (defaults to SplunkStanzaName)
		[Parameter(Position=2)]
		[ValidateScript({ 
			$invalid = $_.IndexOfAny([IO.Path]::GetInvalidFileNameChars())			
			if($invalid -eq -1){ 
				return $true
			} else {
				throw "Invalid character in Module Name '$_' at $invalid"
			}
		})]		
		[string]$Module = $SplunkStanzaName
	)
	begin {
		$path = Join-Path (Get-LocalStoragePath $Module) $Name
		if($PSBoundParameters.ContainsKey("InputObject")) {
			Write-Verbose "Clean Export"
			Export-CliXml -Path $Path -InputObject $InputObject
		} else {
			$Output = @()
		}
	}
	process {
		$Output += $InputObject
	}
	end {
		if($PSBoundParameters.ContainsKey("InputObject")) {
			Write-Verbose "Tail Export"
			# Avoid arrays when they're not needed:
			if($Output.Count -eq 1) { $Output = $Output[0] }
			Export-CliXml -Path $Path -InputObject $Output
		}
	}
}

function Import-LocalStorage {
	#.Synopsis
	#   Loads an object with the specified name from local storage 
	#.Description
	#   Retrieves objects from disk using Get-LocalStoragePath and Import-
    #   
    #   PLEASE NOTE: this will throw an exception if you call it without a DefaultValue 
    #   before you've exported values. If the data you're trying to import may not exist yet, 
    #   then you MUST provide the DefaultValue parameter or handle the exception.
	param(
		# A unique valid file name to use when persisting the object to disk
		[Parameter(Mandatory=$true, Position=0)]
		[ValidateScript({ 
			$invalid = $_.IndexOfAny([IO.Path]::GetInvalidFileNameChars())			
			if($invalid -eq -1){ 
				return $true
			} else {
				throw "Invalid character in Object Name '$_' at $invalid"
			}
		})]		
		[string]$name,

		# A unique valid module name to use when persisting the object to disk (defaults to SplunkStanzaName)
		[Parameter(Position=1)]
		[ValidateScript({ 
			$invalid = $_.IndexOfAny([IO.Path]::GetInvalidFileNameChars())			
			if($invalid -eq -1){ 
				return $true
			} else {
				throw "Invalid character in Module name '$_' at $invalid"
			}
		})]		
		[string]$Module = $SplunkStanzaName,

		# A default value (used in case there's an error importing):
		[Parameter(Position=2)]
		[Object]$DefaultValue
	)
	end {
		try {
			$path = Join-Path (Get-LocalStoragePath $Module) $Name
			Import-CliXml -Path $Path
		} catch {
			if($PSBoundParameters.ContainsKey("DefaultValue")) {
				Write-Output $DefaultValue
			} else {
				throw
			}
		}
	}
}

Export-ModuleMember -Function Import-LocalStorage, Export-LocalStorage, Get-LocalStoragePath