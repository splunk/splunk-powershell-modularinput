Modular Input for PowerShell
============================

The Modular Input for PowerShell is the core of the **Splunk Add-on for PowerShell** and provides a single-instance multi-threaded host mini host
which actually implements a [Splunk "Modular Input" ](http://docs.splunk.com/Documentation/Splunk/latest/AdvancedDev/ModInputsIntro), 
supporting schema, XML configuration vis stdin, and XML streaming output, etc.

To build the modular input you need to:

* Allow NuGet to download missing packages during build.  (This is an option in Visual Studio's Options, under "Package Manager").
* Run the build.ps1 script in the root directory.

For more information about the Modular Input, it's dependencies, and the Splunk Add-on for PowerShell, please see the [README folder](https://github.com/splunk/splunk-powershell-modularinput/blob/master/Package/README)