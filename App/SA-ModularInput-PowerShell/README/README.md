Modular Input for PowerShell
============================

The Modular Input for PowerShell (MIPoSh) provides a single-instance multi-threaded host mini host
which actually implements a [Splunk "Modular Input" ](http://docs.splunk.com/Documentation/Splunk/latest/AdvancedDev/ModInputsIntro), 
supporting schema, XML configuration vis stdin, and XML streaming output, etc.

It's multithreaded, so many PowerShell stanzas can be defined and even run simultaneously, 
and the output will go to the correct indexes with source and sourcetypes defined.
It also provides scheduling so that the scripts for those stanzas can be run on a 
recurring schedule with the full complexity and power of cron scheduling.

It defines a modular input stanza type for "powershell" which can be used in your inputs.conf like this:

    [powershell://RunningProcesses]
    script=Get-Process | Select-Object Handles, NPM, PM, WS, VM, Id, ProcessName, @{n="SplunkHost";e={$Env:SPLUNK_SERVER_NAME}}
    schedule=0 0/15 * ? * *
    index=main
    source=powershell
    sourcetype=PowerShell:RunningProcesses

See the [inputs.conf.spec]() for more information. 
Note that the schedule uses regular [cron syntax](http://quartznet.sourceforge.net/tutorial/lesson_6.html)
and our scheduling is done using the [Quartz.Net Task Scheduler](http://quartznet.sourceforge.net/).

The host automatically converts all output to key="value" format based on public properties,
and packages the output inside <data> tags and <event> tags for Splunk's use.
Note: it currently requires that output objects not have any script properties.

Requirements
------------

* MIPoSh requires PowerShell 3. 
This means it also requires .Net 4 (or higher), and will only run on platforms supported by PowerShell 3: Windows Server 2008 and Vista or newer (Windows 7, Server 2008 R2, Server 2012, Windows 8).

Writing Scripts for Modular Inputs
----------------------------------

When writing scripts for MIPosh, there are a quite a few differences from scripts you might normally run in PowerShell.
There is no actual host provided, so you shouldn't refer to $host or use Write-Host or Out-Host. 
Everything you want output should go to either Write-Output or Write-Error.

Since MIPoSh is a single-instance host running many scripts on schedules, 
all of the scripts are being run within the same process, 
and environment variables like the "current working directory" are shared between scripts.

The Modular PowerShell TA for Splunk includes a PowerShell Module called [LocalStorage](https://github.com/splunk/splunk-powershell-modularinput/tree/master/ModularPowerShell/Modules/LocalStorage) 
which is pre-loaded for you, and exposes three cmdlets: Get-LocalStoragePath, Export-LocalStorage, and Import-LocalStorage. 
These cmdlets use the splunk checkpoint directory and allow you to persist 
key-value pairs of data between scheduled runs of your script 
(since nothing else will persist from one invocation to the next).

### Specifying Paths

When running as a Modular Input (invoked by Splunk), the SplunkHome variable is set, 
so you can easily address scripts in your specific TA by writing paths like this:

    [powershell://MSExchange_Health]
    script=$SplunkHome/etc/apps/TA-Exchange-2010/powershell/health.ps1

Besides $SplunkHome, there are several other read-only constant variables:

* SplunkHome - the root directory for splunk's installed location (useful for appending /etc/apps/ paths to)
* SplunkServerName - the name configured for this machine to use in events
* SplunkServerUri - the address of Splunk's REST API
* SplunkSessionKey - the session key is the authentication token needed for accessing Splunk's REST API
* SplunkCheckpointPath - the path for storing persistent state
* SplunkServerHost - the name of the splunk server that we have to talk to
* SplunkStanzaName -  the name of the inputs.conf stanza that defined this script

### Output

MIPoSh doesn't process the output until your pipeline and runspace are finished.
This means we don't process script properties, and it also means that you should avoid long-running scripts.
Particularly, you should not write scripts which wait for things to happe unless you exit every time there is output.
It also means that all of your output essentially has the same time stamp, unless you override it (see below).

Each object that is output by your script is turned into an "event" in Splunk, and wrapped in <event> and <data> tags.
The properties of each object will be converted to key="value" strings, 
but the value can only be a quoted string, so it will be converted simply by calling .ToString(), 
which means the output must be simple, and complex nested objects should be flattened in your script before being output.

There are a few special property names which have significance for Splunk Modular Inputs, and allow you to
override the defaults defined in the input.conf stanza.  They are:

* SplunkIndex - Overrides the index that the output will be stored in
* SplunkSource - Overrides the "source" for the ouput
* SplunkHost - Overrides the "host" name for the output
* SplunkSourceType - Overrides the "sourcetype" for the output
* SplunkTime - Overrides the "time" -- if you don't specify this, all objects output by your script in a single execution will get roughly the same timestamp, because they're held for output until the script execution is finished, and then marked with the output time.

These properties will never show up in the key="value" output.

NOTE: If you wish to set these properties and override the defaults, you should either use a calculated expressions with Select-Object or use Add-Member to add a NoteProperty.

### Testing

Trying to test these scripts to verify how they run in MIPoSh can be a bit tricky 
if you have to involve all of Splunk, so I added a 
--input parameter which will accept the path to an xml file with the stanza configuration. 
Normally Splunk would stream this XML to the modular input (and you can do that too).

For example, if you take the [sample_input.xml](https://github.com/splunk/splunk-powershell-modularinput/blob/master/ModularPowerShell/sample_input.xml) 
and pass it's path to the Modular Input PowerShell.exe, you should see it set up and start to run the example script(s) on the specified schedule(s).

