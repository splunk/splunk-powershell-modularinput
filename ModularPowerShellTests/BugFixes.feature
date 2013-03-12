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
	Given I have a PowerShell job
	And the script outputs nulls (intermixed with real data)
	When I execute the job
	Then the job should succeed and produce real data
	And the output should have no empty events

@string
Scenario: String Objects
	Given I have an output string
	When I call ConvertToString with the output string
	Then the xml event output should not include the length property
	
@string
	Given I have a PowerShell job
	And the script calls ConvertTo-Splunk
	When I execute the job
	Then the job should succeed and produce real data
	Then the xml event output should be valid XML
	Then the xml event output should not include the length property
	Then the xml event output should not have nested event tags
