Feature: CronParsing
	In order to allow scheduling
	As a modular input scripter
	I want to specify the schedule as a cron string

@cron
Scenario: Execute Every 5 Minutes
	Given I have entered "0/5 * * ? * *" as my schedule
	When I parse the schedule
	Then the schedule should have 12 invocations in seconds divisible by 5

@cron
Scenario: Execute Once in 2020
	Given I have entered "0 0 0 1 1 ? 2020" as my schedule
	When I parse the schedule
	Then the schedule should be invoked only on 2020-1-1 at 0:0:0
