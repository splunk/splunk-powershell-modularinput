Feature: BugFixes
	In order to avoid silly mistakes
	As a developer
	I want to be sure bugs don't reappear

Scenario: Xml Output
	Given I have an output string
	When I call ConvertToString with the output string
	Then the xml event output should be valid XML

@null
Scenario: Null PSObjects
	Given I have a PowerShell Job with an Object Logger
	And my script is "Write-Output @($null,'Test Data', $null, $(Get-Item .))"
	When I execute the job
	Then the job should succeed and produce real data
	And the output should have no empty events

@string
Scenario: String Objects
	Given I have an output string
	When I call ConvertToString with the output string
	Then the xml event output should not include the length property
	
@string
Scenario: Outputting XML Should Encode it
	Given I have a PowerShell Job with an Object Logger
	And my script is ""Hello ${Env:UserName}" | ConvertTo-SplunkEventXml"
	When I execute the job
	When I call ConvertToString with the output string
	Then the job should succeed and produce real data
	Then the xml event output should be valid XML
	Then the xml event output should not include the length property
	Then the xml event output should not have nested event tags
	Then the xml event output should have "<event>" in the data

Scenario: Convert To Splunk Event Xml Should Mark it PreFormatted
	Given I have a PowerShell Job with an Object Logger
	And my script is ""Something" | ConvertTo-SplunkEventXml"
	When I execute the job
	Then the job should succeed and produce real data
	Then the output should have a SplunkPreFormatted property on it