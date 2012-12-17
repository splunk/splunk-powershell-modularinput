// <copyright file="PowerShellJob.cs" company="Splunk">
//   Copyright (c) Joel Bennett 2012 - 2012
// </copyright>
// <summary>
//   Defines the PowerShellJob.cs for ModularPowerShell in ModularPowerShell
// </summary>
namespace ModularPowerShell
{
    using System.Linq;
    using System.Management.Automation;
    using System.Xml.Linq;

    using Quartz;

    using Splunk.ModularInputs;

    /// <summary>
    /// Defines a Quartz Job that executes PowerShell scripts
    /// </summary>
    [DisallowConcurrentExecution]
    public class PowerShellJob : IJob
    {
        /// <summary>
        /// Gets or sets the Splunk input stanza.
        /// </summary>
        public XElement Stanza { get; set; }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        public void Execute(IJobExecutionContext context)
        {
            // Logging FYI:
            SplunkXmlFormatter.Write(string.Format("--- Stanza: {0} ---", this.Stanza.Attribute("name").Value));

            var ps = PowerShell.Create();
            ////ps.RunspacePool = rsp;
            var command = this.Stanza.Descendants("param").FirstOrDefault(p => p.Attribute("name").Value == "script");
            if (command != null)
            {
                ps = ps.AddScript(command.Value);

                // TODO: handle scheduling instead of just executing everything
                ////from p in stanza.Descendants("param") select new KeyValuePair<string,string>( p.Attributes("name"), p.Value );
                // Write the command output to splunk xml
                SplunkXmlFormatter.WriteOutput(ps.Invoke(), this.Stanza.Attribute("name").Value);

                // Write out any errorrs from invoking the script
                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        string format = string.Format(
                            "{0}: ({1}:{2}):[{3}], {4}",
                            error.CategoryInfo.Category,
                            error.CategoryInfo.TargetName,
                            error.CategoryInfo.TargetType,
                            error.CategoryInfo.Activity,
                            error.CategoryInfo.Reason);

                        string msg = command.Value + " " + (error.ErrorDetails != null ? error.ErrorDetails.Message : error.Exception.Message);

                        SplunkXmlFormatter.Write(LogLevel.Error, msg);
                        SplunkXmlFormatter.Write(LogLevel.Error, format);
                    }
                }
            }
            else
            {
                SplunkXmlFormatter.Write(LogLevel.Error, "Missing 'script' parameter.");
            }
        }
    }
}