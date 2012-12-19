// <copyright file="PowerShellJob.cs" company="Splunk">
//   Copyright (c) Joel Bennett 2012 - 2012
// </copyright>
// <summary>
//   Defines the PowerShellJob.cs for ModularPowerShell in ModularPowerShell
// </summary>
namespace Splunk.ModularInputs
{
    using System;
    using System.Management.Automation;

    using Quartz;

    /// <summary>
    /// Defines a Quartz Job that executes PowerShell scripts
    /// </summary>
    [DisallowConcurrentExecution]
    public class PowerShellJob : IJob
    {
        /// <summary>
        /// Executes the job
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        public void Execute(IJobExecutionContext context)
        {
            var data = context.JobDetail.JobDataMap;

            try
            {
                // Logging FYI:
                SplunkXmlFormatter.Write(string.Format("--- Stanza: {0} ---", context.JobDetail.Key.Name));

                var ps = PowerShell.Create();
                ////ps.RunspacePool = rsp;
                var command = data.GetString("script");
                if (command != null)
                {
                    ps = ps.AddScript(command);

                    // TODO: handle scheduling instead of just executing everything
                    ////from p in stanza.Descendants("param") select new KeyValuePair<string,string>( p.Attributes("name"), p.Value );
                    // Write the command output to splunk xml
                    SplunkXmlFormatter.WriteOutput(ps.Invoke(), context.JobDetail.Key.Name);

                    // Write out any errors from invoking the script
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

                            string msg = command + " "
                                         + (error.ErrorDetails != null
                                                ? error.ErrorDetails.Message
                                                : error.Exception.Message);

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
            catch (Exception ex)
            {
                throw new JobExecutionException("Failed to execute PowerShell Script", ex);
            }
        }
    }
}