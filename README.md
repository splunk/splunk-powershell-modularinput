Modular Input for PowerShell
============================

A PowerShell mini host: miPowerShell.exe which is compatible with Splunk's Modular Input schema, with XML streaming output and --scheme parsing, etc.

The Modular Input for PowerShell provides a single-instance multi-threaded host, so many PowerShell stanzas can be defined and even run simultaneously, and the output will go to the correct indexes with source and sourcetypes defined. It also provides scheduling so that the scripts for those stanzas can be run on a recurring schedule with the full complexity and power of cron scheduling.

It defines a modular input stanza type for "powershell" which can be used in your inputs.conf like this:

    [powershell://RunningProcesses]
    script=Get-Process | Select-Object Handles, NPM, PM, WS, VM, Id, ProcessName, @{n="SplunkHost";e={$Env:SPLUNK_SERVER_NAME}}
    schedule=0 0/15 * ? * *
    index=main
    source=powershell
    sourcetype=PowerShell:RunningProcesses

See the [inputs.conf.spec]() for more information. Note that the schedule uses regular cron syntax and is provided by Quartz Task Scheduler. The specified script will be executed based on the cron schedule, and output will be streamed in xml format.

The host automatically converts all output to key="value" format based on public properties, and packages the output inside <data> tags and <event> tags for Splunk's use. Note: it currently requires that output objects not have any script properties.


Writing Scripts for miPowerShell
--------------------------------

When writing scripts for miPowerShell, there are a few key issues. Since this is a single-instance modular input, all scripts are being run within the same process, so the "current working directory" ($pwd) is set to the Modular Input's home.  However, the Splunk_Home envrionment variable is set, so you can easily address scripts in your specific TA by writing paths like this:

    [powershell://MSExchange_Health]
    script=${Env:SPLUNK_HOME}/etc/apps/TA-Exchange-2010/powershell/health.ps1

The miPowerShell TA for splunk includes a PowerShel Module called [LocalStorage](https://github.com/splunk/splunk-powershell-modularinput/tree/master/ModularPowerShell/Modules/LocalStorage) which exposes three cmdlets: Get-LocalStoragePath, Export-LocalStorage, and Import-LocalStorage. These cmdlets write by default to the splunk checkpoint dir for your input, and can be used to persist PowerShell objects as state between scheduled runs of your script (since nothing else should persist).

Besides the SPLUNK_HOME variable, there are several other environment variables which you should be aware of. In particular:

* SPLUNK\_SERVER\_NAME - the name configured for this machine to use when reporting data to Splunk
* SPLUNK\_HOME - the root directory for splunk's installed location (useful for appending /etc/apps/ paths to)
* SPLUNKPS\_SESSION\_KEY - the session key is the authentication token needed for accessing Splunk's REST API
* SPLUNKPS\_SERVER\_URI - the URL which can be used to access Splunk's REST API
* SPLUNKPS\_CHECKPOINT\_DIR - the location where splunk has us storing all checkpoint data
* SPLUNKPS\_SERVER\_HOST 

### Output

All properties on any objects that are output by your script will be converted to key="value" strings and output for Splunk (wrapped in data/event/stream tags). There are a few property names, however, which have special significance in miPowerShell output, and allow you to override the defaults defined in the input.conf stanza by providing them:

* SplunkIndex
* SplunkSource
* SplunkHost
* SplunkSourceType
* SplunkTime

Typically the way to add those would be as calculated expressions with Select-Object or Add-Member.

### Testing

Trying to test these scripts to verify how they run in miPowerShell can be a bit tricky if you have to involve all of Splunk, so I added a --input parameter which accepts the xml that Splunk would normally send us a file path instead. Thus, if you take the [sample_input.xml](https://github.com/splunk/splunk-powershell-modularinput/blob/master/ModularPowerShell/sample_input.xml) and pass it's path to miPowerShell.exe you should see it set up and start to run the script(s) on the specified schedule(s).