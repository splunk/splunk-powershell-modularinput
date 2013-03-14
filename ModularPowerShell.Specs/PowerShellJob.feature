Feature: PowerShellJob
	In order to execute PowerShell
	As a Splunk App Developer
	I want to execute PowerShell Scripts

@mytag
Scenario: Global variables should be available in script
	Given I add ReadOnly variables:
		| Variable | Value    | Description     |
		| Super    | Duper    | Superman        |
		| Uno      | Pizzeria | Great deep dish |
	And I have a PowerShell Job with an Object Logger
	And my script is "Write-Output $Super"
	When I execute the job
	Then the job should succeed and produce real data
	And the object output should be a string "Duper"
